using NGTools;
using System;
using System.IO;
using UnityEditor;

namespace NGToolsEditor
{
	using UnityEngine;

	internal sealed class ScriptableObjectCreator
	{
		private const string	Title = "Create ScriptableObject";
		private static Object	contextObject;
		private static Type		viewType = UnityAssemblyVerifier.TryGetType(typeof(EditorWindow).Assembly, "UnityEditor.View");

		[MenuItem("Assets/Create/Scriptable Object", priority = 90)]
		private static void	CreateScriptableObject(MenuCommand menuCommand)
		{
			Metrics.UseTool(19); // ScriptableObjectCreator
			GenericTypesSelectorWizard	wizard = GenericTypesSelectorWizard.Start(ScriptableObjectCreator.Title, typeof(ScriptableObject), ScriptableObjectCreator.Create, true, true);
			wizard.FilterTypes(t => t.IsAbstract == false && typeof(Editor).IsAssignableFrom(t) == false && typeof(EditorWindow).IsAssignableFrom(t) == false && (ScriptableObjectCreator.viewType == null || ScriptableObjectCreator.viewType.IsAssignableFrom(t) == false));
			ScriptableObjectCreator.contextObject = Selection.activeObject;
		}

		private static void	Create(Type type)
		{
			try
			{
				string	path = string.Empty;

				if (ScriptableObjectCreator.contextObject != null)
				{
					path = AssetDatabase.GetAssetPath(ScriptableObjectCreator.contextObject);

					if (string.IsNullOrEmpty(path) == false &&
						Directory.Exists(path) == false)
					{
						path = Directory.GetParent(path).FullName;
					}
				}

				path = EditorUtility.SaveFilePanelInProject(LC.G("Create") + " " + type.Name, type.Name + ".asset", "asset", LC.G("ScriptableObjectCreator_EnterAssetName"), path);

				if (string.IsNullOrEmpty(path) == false)
				{
					ScriptableObject	instance = ScriptableObject.CreateInstance(type);

					AssetDatabase.CreateAsset(instance, path);
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
					Selection.activeObject = instance;
				}
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException(ex);
			}
		}
	}
}