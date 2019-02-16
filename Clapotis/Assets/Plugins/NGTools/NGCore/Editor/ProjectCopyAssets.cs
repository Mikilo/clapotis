using NGTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor
{
	internal static class ProjectCopyAssets
	{
		public static readonly Color	CutColor = Color.black;
		public static readonly Color	CopyColor = Color.cyan;

		private static bool			skipEvent = false;
		private static bool			cut = false;
		private static List<string>	copyAssetPaths = new List<string>(8);
		private static List<string>	GUIDs = new List<string>(8);

		private static Type			ProjectBrowserType = UnityAssemblyVerifier.TryGetType(typeof(Editor).Assembly, "UnityEditor.ProjectBrowser");
		private static MethodInfo	GetActiveFolderPathMethod = UnityAssemblyVerifier.TryGetMethod(ProjectCopyAssets.ProjectBrowserType, "GetActiveFolderPath", BindingFlags.NonPublic | BindingFlags.Instance);
		private static FieldInfo	s_LastInteractedProjectBrowserField = UnityAssemblyVerifier.TryGetField(ProjectCopyAssets.ProjectBrowserType, "s_LastInteractedProjectBrowser", BindingFlags.Public | BindingFlags.Static);

		/// <summary>Is registered from HQ.</summary>
		/// <param name="guid"></param>
		/// <param name="selectionRect"></param>
		internal static void	OnProjectElementGUI(string guid, Rect selectionRect)
		{
			if (Event.current.type == EventType.Repaint)
			{
				if (ProjectCopyAssets.GUIDs.Contains(guid) == true)
				{
					selectionRect.x -= 1F;
					selectionRect.width = 1F;
					selectionRect.y += 1F;
					selectionRect.height -= 2F;

					if (ProjectCopyAssets.cut == true)
						EditorGUI.DrawRect(selectionRect, ProjectCopyAssets.CutColor);
					else
						EditorGUI.DrawRect(selectionRect, ProjectCopyAssets.CopyColor);
				}
			}
			else if (Event.current.type == EventType.ValidateCommand)
			{
				if (ProjectCopyAssets.skipEvent == false)
				{
					ProjectCopyAssets.skipEvent = true;

					if (Event.current.commandName == "Copy")
					{
						if (ProjectCopyAssets.ValidateCopyCutAssets() == true)
						{
							ProjectCopyAssets.CopyAssets(null);
							Event.current.Use();
						}
					}
					else if (Event.current.commandName == "Cut")
					{
						if (ProjectCopyAssets.ValidateCopyCutAssets() == true)
						{
							ProjectCopyAssets.CutAssets(null);
							Event.current.Use();
						}
					}
					else if (Event.current.commandName == "Paste")
					{
						if (ProjectCopyAssets.ValidatePasteAssets() == true)
						{
							ProjectCopyAssets.PasteAssets(null);
							Event.current.Use();
						}
					}
				}
			}
			else
				ProjectCopyAssets.skipEvent = false;
		}

		[MenuItem("Assets/Cut Assets")]
		private static void		CutAssets(MenuCommand menuCommand)
		{
			ProjectCopyAssets.cut = true;
			ProjectCopyAssets.ExtractAssetsFromSelection();
		}

		[MenuItem("Assets/Copy Assets")]
		private static void		CopyAssets(MenuCommand menuCommand)
		{
			ProjectCopyAssets.cut = false;
			ProjectCopyAssets.ExtractAssetsFromSelection();
		}

		[MenuItem("Assets/Cut Assets", true)]
		[MenuItem("Assets/Copy Assets", true)]
		private static bool		ValidateCopyCutAssets()
		{
			return Selection.objects.Length > 0;
		}

		[MenuItem("Assets/Paste Assets")]
		private static void		PasteAssets(MenuCommand menuCommand)
		{
			string	destination = ProjectCopyAssets.GetDestinationPath();

			if (File.Exists(destination) == true)
				destination = Path.GetDirectoryName(destination);

			if (Directory.Exists(destination) == true)
			{
				for (int i = 0; i < ProjectCopyAssets.copyAssetPaths.Count; i++)
				{
					string	fullPath = Path.Combine(destination, Path.GetFileName(ProjectCopyAssets.copyAssetPaths[i]));
					bool	incrementalCopy = false;

					if (File.Exists(fullPath) == true)
						incrementalCopy = true;

					if (incrementalCopy == false)
					{
						if (ProjectCopyAssets.cut == false)
						{
							if (AssetDatabase.CopyAsset(ProjectCopyAssets.copyAssetPaths[i], fullPath) == false)
								InternalNGDebug.Log("Copy of \"" + ProjectCopyAssets.copyAssetPaths[i] + "\" into \"" + destination + "\" failed.");
						}
						else
						{
							string	error = AssetDatabase.MoveAsset(ProjectCopyAssets.copyAssetPaths[i], fullPath);

							if (string.IsNullOrEmpty(error) == false)
								InternalNGDebug.Log(error);
							else
								ProjectCopyAssets.copyAssetPaths[i] = fullPath;
						}
					}
					else
					{
						string	filename = Path.GetFileNameWithoutExtension(ProjectCopyAssets.copyAssetPaths[i]);
						string	extension = Path.GetExtension(ProjectCopyAssets.copyAssetPaths[i]);
						int		n = 0;
						int		j = filename.Length - 1;

						// Extract increment if it exists.
						while (j >= 0 && '0' <= filename[j] && filename[j] <= '9')
						{
							n = n * 10 + filename[j] - '0';
							--j;
						}

						if (n > 0)
							filename = filename.Substring(0, j + 1);
						else
						{
							n = 1;
							filename += ' ';
						}

						fullPath = Path.Combine(destination, filename + n + extension);

						while (File.Exists(fullPath) == true)
						{
							++n;
							fullPath = Path.Combine(destination, filename + n + extension);
						}

						if (ProjectCopyAssets.cut == false)
						{
							if (AssetDatabase.CopyAsset(ProjectCopyAssets.copyAssetPaths[i], fullPath) == false)
								InternalNGDebug.Log("Copy of \"" + ProjectCopyAssets.copyAssetPaths[i] + "\" into \"" + destination + "\" failed.");
						}
						else
						{
							string	error = AssetDatabase.MoveAsset(ProjectCopyAssets.copyAssetPaths[i], fullPath);

							if (string.IsNullOrEmpty(error) == false)
								InternalNGDebug.Log(error);
							ProjectCopyAssets.copyAssetPaths[i] = fullPath;
						}
					}

					AssetDatabase.Refresh();
				}
			}
		}

		[MenuItem("Assets/Paste Assets", true)]
		private static bool		ValidatePasteAssets()
		{
			for (int i = 0; i < ProjectCopyAssets.copyAssetPaths.Count; i++)
			{
				if (File.Exists(ProjectCopyAssets.copyAssetPaths[i]) == false &&
					Directory.Exists(ProjectCopyAssets.copyAssetPaths[i]) == false)
				{
					ProjectCopyAssets.copyAssetPaths.RemoveAt(i--);
					ProjectCopyAssets.GUIDs.RemoveAt(i--);
				}
			}

			return ProjectCopyAssets.copyAssetPaths.Count > 0 && ProjectCopyAssets.GetDestinationPath() != null;
		}

		private static string	GetDestinationPath()
		{
			string	path = null;

			if (Selection.activeObject != null)
			{
				path = AssetDatabase.GetAssetPath(Selection.activeObject);

				if (Directory.Exists(path) == true)
					path = Path.GetDirectoryName(path);
			}

			if (path == null &&
				ProjectCopyAssets.s_LastInteractedProjectBrowserField != null &&
				ProjectCopyAssets.GetActiveFolderPathMethod != null)
			{
				object	lastProjectBrowser = ProjectCopyAssets.s_LastInteractedProjectBrowserField.GetValue(null);

				if (lastProjectBrowser != null)
					path = ProjectCopyAssets.GetActiveFolderPathMethod.Invoke(lastProjectBrowser, null) as string;
			}

			return path;
		}

		private static void		ExtractAssetsFromSelection()
		{
			ProjectCopyAssets.copyAssetPaths.Clear();
			ProjectCopyAssets.GUIDs.Clear();

			for (int i = 0; i < Selection.objects.Length; i++)
			{
				if (Selection.objects[i] != null)
				{
					string	path = AssetDatabase.GetAssetPath(Selection.objects[i]);

					if (string.IsNullOrEmpty(path) == false)
					{
						ProjectCopyAssets.copyAssetPaths.Add(path);
						ProjectCopyAssets.GUIDs.Add(AssetDatabase.AssetPathToGUID(path));
					}
				}
			}
		}
	}
}