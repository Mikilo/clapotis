using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NGPackageHelpers
{
	public class NGPackageExporterWindow : EditorWindow
	{
		public const string Title = "NG Package Exporter";

		public static Action<NGPackageExporterWindow>	BeforeExport;
		public static Func<string>	GetVersion;

		public string	version;

		private string	finalExportPath;

		[MenuItem(Constants.MenuItemPath + NGPackageExporterWindow.Title + " %#e", priority = 1)]
		private static void	ExportForAssetStore()
		{
			EditorWindow.GetWindow<NGPackageExporterWindow>(true, NGPackageExporterWindow.Title, true);
		}

		protected virtual void	OnEnable()
		{
			if (NGPackageExporterWindow.GetVersion != null)
				this.version = NGPackageExporterWindow.GetVersion();
		}

		protected virtual void	OnGUI()
		{
			ProfilesManager.OnProfilesBarGUI();
			if (ProfilesManager.IsReady == false)
				return;

			using (LabelWidthRestorer.Get(143F))
			{
				EditorGUI.BeginChangeCheck();
				ProfilesManager.Profile.packagePath = EditorGUILayout.TextField("Package Path", ProfilesManager.Profile.packagePath);
				Rect	r = GUILayoutUtility.GetLastRect();
				r.x += 100F;
				GUI.Label(r, "Assets/");

				if (Directory.Exists("Assets/" + ProfilesManager.Profile.packagePath) == false)
				{
					EditorGUILayout.HelpBox("Package at \"Assets/" + ProfilesManager.Profile.packagePath + "\" was not found.", MessageType.Warning);
				}
			}

			using (LabelWidthRestorer.Get(100F))
			{
				this.version = EditorGUILayout.TextField("Version", this.version);

				EditorGUILayout.BeginHorizontal();
				{
					ProfilesManager.Profile.exportPath = EditorGUILayout.TextField("Export Path", ProfilesManager.Profile.exportPath);
					if (GUILayout.Button("Browse", "ButtonLeft", GUILayout.Width(60F)) == true)
					{
						string	exportPath = EditorUtility.OpenFolderPanel("Export folder", ProfilesManager.Profile.exportPath, string.Empty);

						if (string.IsNullOrEmpty(exportPath) == false)
						{
							ProfilesManager.Profile.exportPath = exportPath;
							GUI.FocusControl(null);
						}
					}

					if (GUILayout.Button("Open", "ButtonRight", GUILayout.Width(50F)) == true)
						Utility.ShowExplorer(ProfilesManager.Profile.exportPath);
				}
				EditorGUILayout.EndHorizontal();

				if (Directory.Exists(ProfilesManager.Profile.exportPath) == false)
					EditorGUILayout.HelpBox("Export path is not a folder.", MessageType.Warning);

				ProfilesManager.Profile.devPrefix = EditorGUILayout.TextField("Full Export Prefix", ProfilesManager.Profile.devPrefix);
				ProfilesManager.Profile.nameFormat = EditorGUILayout.TextField("Name Format", ProfilesManager.Profile.nameFormat);
				EditorGUILayout.HelpBox("{0} = Date of the day, {1} = Version", MessageType.Info);

				EditorGUILayout.BeginHorizontal();
				{
					this.finalExportPath = EditorGUILayout.TextField(this.finalExportPath);
					if (EditorGUI.EndChangeCheck() == true || string.IsNullOrEmpty(this.finalExportPath) == true)
						this.finalExportPath = Path.Combine(ProfilesManager.Profile.exportPath, this.GetExportName());

					if (File.Exists(this.finalExportPath) == true && GUILayout.Button("Show", GUILayout.Width(50F)) == true)
						Utility.ShowExplorer(this.finalExportPath);
				}
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();
			{
				if (GUILayout.Button("Export Package") == true)
					this.Export();

				if (GUILayout.Button("Full Export Package") == true)
					this.ExportForDev();
			}
			EditorGUILayout.EndHorizontal();
		}

		private void	Export()
		{
			string[]		paths = AssetDatabase.GetAllAssetPaths();
			List<string>	exported = new List<string>(512);
			string			packagePath = "Assets/" + ProfilesManager.Profile.packagePath + "/";

			for (int i = 0; i < paths.Length; i++)
			{
				if (paths[i].StartsWith(packagePath) == true)
				{
					int	j = 0;

					for (; j < NGPackageExcluderWindow.DefaultKeywords.Length; j++)
					{
						if (paths[i].Contains(NGPackageExcluderWindow.DefaultKeywords[j]) == true)
							break;
					}

					if (j < NGPackageExcluderWindow.DefaultKeywords.Length)
						continue;

					j = 0;
					for (; j < ProfilesManager.Profile.includeKeywords.Count; j++)
					{
						if (paths[i].Contains(ProfilesManager.Profile.includeKeywords[j]) == true)
							break;
					}

					if (j < ProfilesManager.Profile.includeKeywords.Count)
						continue;

					exported.Add(paths[i]);
				}
				else if (ProfilesManager.Profile.includeKeywords.Count > 0)
				{
					int	j = 0;

					for (; j < ProfilesManager.Profile.includeKeywords.Count; j++)
					{
						if (paths[i].Contains(ProfilesManager.Profile.includeKeywords[j]) == true)
							break;
					}

					if (j >= ProfilesManager.Profile.includeKeywords.Count)
						continue;

					exported.Add(paths[i]);
				}
			}

			if (NGPackageExporterWindow.BeforeExport != null)
				NGPackageExporterWindow.BeforeExport(this);

			AssetDatabase.ExportPackage(exported.ToArray(), Path.Combine(ProfilesManager.Profile.exportPath, this.GetExportName()), ExportPackageOptions.Interactive);
		}

		private void	ExportForDev()
		{
			string[]		paths = AssetDatabase.GetAllAssetPaths();
			List<string>	exported = new List<string>(512);
			string			packagePath = "Assets/" + ProfilesManager.Profile.packagePath + "/";

			for (int i = 0; i < paths.Length; i++)
			{
				if (paths[i].StartsWith(packagePath) == true)
					exported.Add(paths[i]);
			}

			AssetDatabase.ExportPackage(exported.ToArray(), Path.Combine(ProfilesManager.Profile.exportPath, ProfilesManager.Profile.devPrefix + this.GetExportName()), ExportPackageOptions.Interactive);
		}

		private string	GetExportName()
		{
			if (string.IsNullOrEmpty(ProfilesManager.Profile.nameFormat) == false)
				return string.Format(ProfilesManager.Profile.nameFormat, DateTime.Now.ToString("yyyyMMdd"), this.version) + ".unitypackage";
			return ".unitypackage";
		}
	}
}