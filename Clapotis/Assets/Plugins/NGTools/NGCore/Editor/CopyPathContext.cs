using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor
{
	internal static class CopyPathContext
	{
		[MenuItem("Assets/Copy/Relative Path")]
		private static void	AssetsRelativeCopy(MenuCommand menuCommand)
		{
			StringBuilder	buffer = Utility.GetBuffer();

			for (int i = 0; i < Selection.objects.Length; i++)
			{
				if (Selection.objects[i] != null)
					buffer.AppendLine(AssetDatabase.GetAssetPath(Selection.objects[i]));
			}

			if (buffer.Length > Environment.NewLine.Length)
				buffer.Length -= Environment.NewLine.Length;

			EditorGUIUtility.systemCopyBuffer = Utility.ReturnBuffer(buffer);
		}

		[MenuItem("Assets/Copy/Absolute Path")]
		private static void	AssetsAbsoluteCopy(MenuCommand menuCommand)
		{
			StringBuilder	buffer = Utility.GetBuffer();

			for (int i = 0; i < Selection.objects.Length; i++)
			{
				if (Selection.objects[i] != null)
				{
					buffer.Append(Application.dataPath, 0, Application.dataPath.Length - "Assets".Length);
					buffer.AppendLine(AssetDatabase.GetAssetPath(Selection.objects[i]));
				}
			}

			if (buffer.Length > Environment.NewLine.Length)
				buffer.Length -= Environment.NewLine.Length;

			EditorGUIUtility.systemCopyBuffer = Utility.ReturnBuffer(buffer);
		}

		[MenuItem("Assets/Copy/Asset Name")]
		[MenuItem("GameObject/Copy/Asset Name", priority = 12)]
		private static void AssetsOrGameObjectCopyAssetName(MenuCommand menuCommand)
		{
			StringBuilder	buffer = Utility.GetBuffer();

			for (int i = 0; i < Selection.objects.Length; i++)
			{
				if (Selection.objects[i] != null)
					buffer.AppendLine(Selection.objects[i].name);
			}

			if (buffer.Length > Environment.NewLine.Length)
				buffer.Length -= Environment.NewLine.Length;

			EditorGUIUtility.systemCopyBuffer = Utility.ReturnBuffer(buffer);
		}

		private static Stack<string>	hierarchy = new Stack<string>(4);

		[MenuItem("GameObject/Copy/Hierarchy")]
		private static void	GameObjectCopyHierarchy(MenuCommand menuCommand)
		{
			StringBuilder	buffer = Utility.GetBuffer();

			CopyPathContext.hierarchy.Clear();

			for (int i = 0; i < Selection.transforms.Length; i++)
			{
				if (Selection.transforms[i] != null)
				{
					Transform	t = Selection.transforms[i];

					while (t != null)
					{
						CopyPathContext.hierarchy.Push(t.name);
						t = t.parent;
					}

					while (CopyPathContext.hierarchy.Count > 0)
					{
						buffer.Append(CopyPathContext.hierarchy.Pop());
						buffer.Append('/');
					}

					buffer.Length -= 1;
					buffer.Append(Environment.NewLine);
				}
			}

			if (buffer.Length > Environment.NewLine.Length)
				buffer.Length -= Environment.NewLine.Length;

			EditorGUIUtility.systemCopyBuffer = Utility.ReturnBuffer(buffer);
		}
	}
}