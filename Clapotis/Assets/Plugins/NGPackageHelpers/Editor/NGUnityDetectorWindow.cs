using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NGPackageHelpers
{
	public class NGUnityDetectorWindow : EditorWindow
	{
		public const string	Title = "NG Unity Detector";
		public const string	UnityInstallPathsPrefKey = "NGUnityDetector_UnityInstallPaths";
		public const char	Separator = ';';

		public static Action	UnityInstallsChanged;

		[NonSerialized]
		private List<string>				unityInstallsPaths = new List<string>();
		[NonSerialized]
		private Dictionary<string, string>	unityInstallsDetected = new Dictionary<string, string>();
		private Vector2						scrollPosition;

		[MenuItem(Constants.MenuItemPath + NGUnityDetectorWindow.Title, priority = 102)]
		private static void	Open()
		{
			EditorWindow.GetWindow<NGUnityDetectorWindow>(true, NGUnityDetectorWindow.Title, true);
		}

		protected virtual void	OnEnable()
		{
			string	rawPaths = EditorPrefs.GetString(NGUnityDetectorWindow.UnityInstallPathsPrefKey);

			if (string.IsNullOrEmpty(rawPaths) == false)
			{
				string[]	paths = rawPaths.Split(NGUnityDetectorWindow.Separator);

				if (paths.Length > 0)
				{
					this.unityInstallsPaths.AddRange(paths);
					EditorApplication.delayCall += this.RefreshUnityInstalls;
				}
			}
		}

		protected virtual void	OnDisable()
		{
			if (this.unityInstallsPaths.Count > 0)
				EditorPrefs.SetString(NGUnityDetectorWindow.UnityInstallPathsPrefKey, string.Join(NGUnityDetectorWindow.Separator.ToString(), this.unityInstallsPaths.ToArray()));
		}

		protected virtual void	OnGUI()
		{
			EditorGUILayout.LabelField("Unity Installations Paths");

			if (this.unityInstallsPaths.Count == 0 || this.unityInstallsDetected.Count == 0)
				EditorGUILayout.HelpBox("Your Unity folders must end by their version. (e.g. \"Unity 4.2.1f3\", \"Unity5.1.3p4\")", MessageType.Info);

			for (int i = 0; i < this.unityInstallsPaths.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				{
					EditorGUI.BeginChangeCheck();
					this.unityInstallsPaths[i] = EditorGUILayout.TextField(this.unityInstallsPaths[i]);
					if (EditorGUI.EndChangeCheck() == true)
						this.RefreshUnityInstalls();

					if (GUILayout.Button("Browse", GUILayout.Width(60F)) == true)
					{
						string	path = this.unityInstallsPaths[i];
						if (string.IsNullOrEmpty(path) == false)
							path = Path.GetDirectoryName(path);

						string	projectPath = EditorUtility.OpenFolderPanel("Folder with Unity installations ending by A.B.C[abfpx]NN", path, string.Empty);

						if (string.IsNullOrEmpty(projectPath) == false)
						{
							this.unityInstallsPaths[i] = projectPath;
							this.RefreshUnityInstalls();
							GUI.FocusControl(null);
						}
					}

					if (GUILayout.Button("X", GUILayout.Width(16F)) == true)
					{
						this.unityInstallsPaths.RemoveAt(i);
						this.RefreshUnityInstalls();
						--i;
					}
				}
				EditorGUILayout.EndHorizontal();
			}

			if (GUILayout.Button("Add installation path") == true)
			{
				if (this.unityInstallsPaths.Count > 0)
					this.unityInstallsPaths.Add(this.unityInstallsPaths[this.unityInstallsPaths.Count - 1]);
				else
					this.unityInstallsPaths.Add(string.Empty);
			}

			this.scrollPosition = EditorGUILayout.BeginScrollView(this.scrollPosition);
			{
				foreach (var pair in this.unityInstallsDetected)
					EditorGUILayout.LabelField(pair.Value + " [" + pair.Key + "]");
			}
			EditorGUILayout.EndScrollView();
		}

		private void	RefreshUnityInstalls()
		{
			this.OnDisable();

			this.unityInstallsDetected.Clear();

			for (int i = 0; i < this.unityInstallsPaths.Count; i++)
				NGUnityDetectorWindow.ExtractUnityInstalls(this.unityInstallsPaths[i], this.unityInstallsDetected);

			if (NGUnityDetectorWindow.UnityInstallsChanged != null)
				NGUnityDetectorWindow.UnityInstallsChanged();
		}

		public static void	GetInstalls(Dictionary<string, string> installs)
		{
			installs.Clear();

			string	rawPaths = EditorPrefs.GetString(NGUnityDetectorWindow.UnityInstallPathsPrefKey);

			if (string.IsNullOrEmpty(rawPaths) == false)
			{
				string[]	paths = rawPaths.Split(NGUnityDetectorWindow.Separator);

				if (paths.Length > 0)
				{
					for (int i = 0; i < paths.Length; i++)
						NGUnityDetectorWindow.ExtractUnityInstalls(paths[i], installs);
				}
			}
		}

		public static string	GetUnityExecutable(Dictionary<string, string> unityInstalls, string unityVersion)
		{
			string	path;

			if (unityInstalls.TryGetValue(unityVersion, out path) == true)
				return Path.Combine(path, @"Editor\Unity.exe");
			return null;
		}

		private static void	ExtractUnityInstalls(string path, Dictionary<string, string> installs)
		{
			if (Directory.Exists(path) == false)
				return;

			string[]	dirs = Directory.GetDirectories(path);

			for (int j = 0; j < dirs.Length; j++)
			{
				path = Path.Combine(dirs[j], @"Editor\Uninstall.exe");
				if (File.Exists(path) == true)
				{
					string	version = Utility.GetUnityVersion(dirs[j]);

					if (installs.ContainsKey(version) == false)
						installs.Add(version, dirs[j]);
					else
						installs[version] = dirs[j];
				}
			}
		}
	}
}