using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NGPackageHelpers
{
	public class GenerateProjectsWindow : EditorWindow
	{
		public const string	Title = "Generate Projects";

		public string		packagePath = "Path/To/Package";
		public string		folder = "Path/To/TargetFolder";
		public List<string>	projects = new List<string>();
		public Dictionary<string, string>	unityInstallsDetected = new Dictionary<string, string>();

		private Vector2	scrollPosition;

		protected virtual void	OnEnable()
		{
			Utility.LoadEditorPref(this, Utility.GetPerProjectPrefix());
		}

		protected virtual void	OnDisable()
		{
			Utility.SaveEditorPref(this, Utility.GetPerProjectPrefix());
		}

		protected virtual void	OnGUI()
		{
			EditorGUILayout.BeginHorizontal();
			{
				this.folder = EditorGUILayout.TextField("Target Folder", this.folder);
				if (GUILayout.Button("Browse", "ButtonLeft", GUILayout.ExpandWidth(false)) == true)
				{
					string	projectPath = EditorUtility.OpenFolderPanel("Folder with Unity installations ending by A.B.C[abfpx]NN", this.folder, string.Empty);

					if (string.IsNullOrEmpty(projectPath) == false)
					{
						this.folder = projectPath;
						GUI.FocusControl(null);
					}
				}

				if (GUILayout.Button("Open", "ButtonRight", GUILayout.ExpandWidth(false)) == true)
					Utility.ShowExplorer(this.folder);
			}
			EditorGUILayout.EndHorizontal();

			string	folderName = new DirectoryInfo(this.packagePath).Name;

			this.scrollPosition = EditorGUILayout.BeginScrollView(this.scrollPosition);
			{
				foreach (var version in this.unityInstallsDetected.Keys)
				{
					EditorGUILayout.BeginHorizontal();
					{
						string	project = this.folder + Path.DirectorySeparatorChar + folderName + " " + version;

						if (GUILayout.Button("Open", GUILayout.ExpandWidth(false)) == true)
							Utility.ShowExplorer(project);

						EditorGUILayout.TextField(project);

						EditorGUI.BeginDisabledGroup(Directory.Exists(project));
						if (GUILayout.Button("Generate", GUILayout.ExpandWidth(false)) == true)
							this.GenerateProject(project);
						EditorGUI.EndDisabledGroup();
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			EditorGUILayout.EndScrollView();
		}

		private void	GenerateProject(string project)
		{
			string	path = project;
			string	package = Path.Combine("Assets", this.packagePath);

			path = Path.Combine(path, package);

			this.DirectoryCopy(Path.Combine(Directory.GetCurrentDirectory(), Path.Combine("Assets", this.packagePath)), Path.Combine(project, Path.Combine("Assets", this.packagePath)), true);
			this.DirectoryCopy(Path.Combine(Directory.GetCurrentDirectory(), "ProjectSettings"), Path.Combine(project, "ProjectSettings"), false);

			ProfilesManager.Profile.projects.Add(project);
		}

		// Thanks to Microsoft @ https://msdn.microsoft.com/fr-fr/library/bb762914(v=vs.110).aspx
		private void	DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
		{
			// Get the subdirectories for the specified directory.
			DirectoryInfo	dir = new DirectoryInfo(sourceDirName);

			if (!dir.Exists)
			{
				throw new DirectoryNotFoundException(
					"Source directory does not exist or could not be found: "
					+ sourceDirName);
			}

			DirectoryInfo[] dirs = dir.GetDirectories();
			// If the destination directory doesn't exist, create it.
			if (!Directory.Exists(destDirName))
			{
				Directory.CreateDirectory(destDirName);
			}

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files)
			{
				string	temppath = Path.Combine(destDirName, file.Name);
				file.CopyTo(temppath, false);
			}

			// If copying subdirectories, copy them and their contents to new location.
			if (copySubDirs)
			{
				foreach (DirectoryInfo subdir in dirs)
				{
					string	temppath = Path.Combine(destDirName, subdir.Name);
					DirectoryCopy(subdir.FullName, temppath, copySubDirs);
				}
			}
		}
	}
}