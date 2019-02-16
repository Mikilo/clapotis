using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEditor;
using UnityEditorInternal;

namespace NGPackageHelpers
{
	using UnityEngine;

	// Thanks to sartoris @ http://stackoverflow.com/questions/17887211/c-sharp-get-process-window-titles
	public static class MyEnumWindows
	{
		private delegate bool EnumWindowsProc(IntPtr windowHandle, IntPtr lParam);

		[DllImport("user32")]
		private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern bool EnumChildWindows(IntPtr hWndStart, EnumWindowsProc callback, IntPtr lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

		private static List<string> windowTitles = new List<string>();

		public static List<string> GetWindowTitles(bool includeChildren)
		{
			windowTitles.Clear();
			EnumWindows(MyEnumWindows.EnumWindowsCallback, includeChildren ? (IntPtr)1 : IntPtr.Zero);
			return MyEnumWindows.windowTitles;
		}

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

		private static bool EnumWindowsCallback(IntPtr testWindowHandle, IntPtr includeChildren)
		{
			string	title = MyEnumWindows.GetWindowTitle(testWindowHandle);

			if (MyEnumWindows.TitleMatches(title))
			{
				try
				{
					uint	id;
					GetWindowThreadProcessId(testWindowHandle, out id);
					Process	p = Process.GetProcessById((int)id);

					// For the moment, I found no workaround to get main Unity processes only.
					if (p.ProcessName == "Unity")
						MyEnumWindows.windowTitles.Add(title);
				}
				catch
				{
				}
			}

			if (includeChildren.Equals(IntPtr.Zero) == false)
				MyEnumWindows.EnumChildWindows(testWindowHandle, MyEnumWindows.EnumWindowsCallback, IntPtr.Zero);

			return true;
		}

		public static string GetWindowTitle(IntPtr windowHandle)
		{
			uint	SMTO_ABORTIFHUNG = 0x0002;
			uint	WM_GETTEXT = 0xD;
			int		MAX_STRING_SIZE = 32768;
			IntPtr	result;
			string	title = string.Empty;
			IntPtr	memoryHandle = Marshal.AllocCoTaskMem(MAX_STRING_SIZE);
			Marshal.Copy(title.ToCharArray(), 0, memoryHandle, title.Length);
			MyEnumWindows.SendMessageTimeout(windowHandle, WM_GETTEXT, (IntPtr)MAX_STRING_SIZE, memoryHandle, SMTO_ABORTIFHUNG, (uint)1000, out result);
			title = Marshal.PtrToStringAuto(memoryHandle);
			Marshal.FreeCoTaskMem(memoryHandle);
			return title;
		}

		private static bool TitleMatches(string title)
		{
			return title.Contains("e");
		}
	}

	public class NGPackageDeployerWindow : EditorWindow
	{
		public const string	Title = "NG Package Deployer";

		private Dictionary<string, int>	deployingProjects = new Dictionary<string, int>();

		private ReorderableList	list;
		private Vector2			scrollPosition;
		private Dictionary<string, string>	unityInstalls = new Dictionary<string, string>();
		private List<string>	unityProcessesDetected = new List<string>();
		private string			packageFolderError;
		private GUIContent		packageFolderContent = new GUIContent("Package Path", "Path to package inside Assets. e.g A/B/DummyPackage");

		private Dictionary<string, string>	errors = new Dictionary<string, string>();

		private GUIStyle	textStyle;
		private GUIStyle	labelStyle;

		private List<string>	exported;
		private object			compileLock = new object();

		[MenuItem(Constants.MenuItemPath + NGPackageDeployerWindow.Title + " %#D", priority = 2)]
		private static void	Open()
		{
			EditorWindow.GetWindow<NGPackageDeployerWindow>(true, NGPackageDeployerWindow.Title, true);
		}

		protected virtual void	OnEnable()
		{
			NGUnityDetectorWindow.GetInstalls(this.unityInstalls);
			NGUnityDetectorWindow.UnityInstallsChanged += this.UpdateUnityProjectState;

			ProfilesManager.SetProfile += this.OnSetProfile;
			if (ProfilesManager.IsReady == true)
				this.OnSetProfile();

			Utility.RegisterIntervalCallback(this.UpdateProcesses, 500);
			EditorApplication.delayCall += this.UpdateProcesses;
		}

		protected virtual void	OnDisable()
		{
			ProfilesManager.SetProfile -= this.OnSetProfile;

			Utility.UnregisterIntervalCallback(this.UpdateProcesses);
			NGUnityDetectorWindow.UnityInstallsChanged -= this.UpdateUnityProjectState;
		}

		protected virtual void	OnGUI()
		{
			ProfilesManager.OnProfilesBarGUI();
			if (ProfilesManager.IsReady == false)
				return;

			if (this.textStyle == null)
			{
				this.textStyle = new GUIStyle(GUI.skin.textField);
				this.textStyle.alignment = TextAnchor.MiddleLeft;
				this.labelStyle = new GUIStyle(GUI.skin.label);
				this.labelStyle.alignment = TextAnchor.MiddleLeft;
			}

			this.scrollPosition = EditorGUILayout.BeginScrollView(this.scrollPosition);
			{
				EditorGUI.BeginChangeCheck();
				ProfilesManager.Profile.packagePath = EditorGUILayout.TextField(this.packageFolderContent, ProfilesManager.Profile.packagePath);
				Rect	r2 = GUILayoutUtility.GetLastRect();
				r2.x += 107F;
				GUI.Label(r2, "Assets/");
				if (EditorGUI.EndChangeCheck() == true || this.packageFolderError == null)
				{
					if (string.IsNullOrEmpty(ProfilesManager.Profile.packagePath) == true || Directory.Exists("Assets/" + ProfilesManager.Profile.packagePath) == false)
						this.packageFolderError = "Package at \"Assets/" + ProfilesManager.Profile.packagePath + "\" was not found.";
					else
						this.packageFolderError = string.Empty;

					ProfilesManager.Save();
				}

				if (this.packageFolderError != string.Empty)
					EditorGUILayout.HelpBox(this.packageFolderError, MessageType.Warning);

				EditorGUI.BeginChangeCheck();
				ProfilesManager.Profile.deployMeta = EditorGUILayout.Toggle("Copy Meta", ProfilesManager.Profile.deployMeta);
				if (EditorGUI.EndChangeCheck() == true)
					ProfilesManager.Save();

				GUILayout.Space(10F);

				EditorGUILayout.LabelField("Unity Projects:");
				EditorGUILayout.BeginHorizontal();
				{
					if (GUILayout.Button("Detect Projects") == true)
					{
						string	path = EditorUtility.OpenFolderPanel("Folder with Unity Projects ending by A.B.C[abfpx]NN", string.Empty, string.Empty);

						if (string.IsNullOrEmpty(path) == false)
							this.DetectProjects(path);
					}

					if (GUILayout.Button("Generate Projects") == true)
					{
						GenerateProjectsWindow	window = EditorWindow.GetWindow<GenerateProjectsWindow>(true, GenerateProjectsWindow.Title, true);

						window.packagePath = ProfilesManager.Profile.packagePath;
						window.projects = ProfilesManager.Profile.projects;
						window.unityInstallsDetected = this.unityInstalls;
					}

					EditorGUI.BeginDisabledGroup(this.packageFolderError != string.Empty);
					if (GUILayout.Button("Deploy All") == true && EditorUtility.DisplayDialog("Deploy All", "Package is gonna be erased before deploying in all projects. May takes few minutes.", "Yes", "No") == true)
						this.AsyncDeployAll();
					EditorGUI.EndDisabledGroup();
				}
				EditorGUILayout.EndHorizontal();

				if (ProfilesManager.Profile.projects.Count == 0)
					EditorGUILayout.HelpBox("Path must be \"C:\\Path\\To\\Your\\Project A.B.C[abfpx]NN\" with A.B.C[abfpx]NN the Unity version. (e.g. 4.5.0f2, 5.3.5b14)", MessageType.Info);

				this.list.DoLayoutList();

				if (this.unityProcessesDetected.Count >= 2)
				{
					EditorGUILayout.LabelField("Unity processes:");
					for (int i = 0; i < this.unityProcessesDetected.Count; i++)
						EditorGUILayout.LabelField(this.unityProcessesDetected[i]);
				}
			}
			EditorGUILayout.EndScrollView();

			if (this.unityProcessesDetected.Count >= 2)
				EditorGUILayout.HelpBox("Many Unity processes detected. You should not launch more than one Unity Editor when deploying, to avoid log collision, since they all share the same editor.log.", MessageType.Warning);
		}

		private void	AsyncDeployAll()
		{
			for (int i = 0; i < ProfilesManager.Profile.projects.Count; i++)
			{
				try
				{
					if (Directory.Exists(ProfilesManager.Profile.projects[i]) == true)
					{
						string	unityVersion = Utility.GetUnityVersion(ProfilesManager.Profile.projects[i]);
						if (string.IsNullOrEmpty(unityVersion) == false)
						{
							string	unityExe = NGUnityDetectorWindow.GetUnityExecutable(this.unityInstalls, unityVersion);

							if (File.Exists(unityExe) == true)
								this.AsyncDeploy(ProfilesManager.Profile.projects[i]);
						}
					}
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException("Deployment at \"" + ProfilesManager.Profile.projects[i] + "\" failed.", ex);
				}
			}
		}

		private void	GenerateExportedAssets()
		{
			if (this.exported != null)
				return;

			string		packagePath = "Assets/" + ProfilesManager.Profile.packagePath + "/";
			string[]	paths = AssetDatabase.GetAllAssetPaths();

			this.exported = new List<string>(1024);

			for (int i = 0; i < paths.Length; i++)
			{
				if (paths[i].StartsWith(packagePath) == true && Directory.Exists(paths[i]) == false)
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
					for (; j < ProfilesManager.Profile.excludeKeywords.Count; j++)
					{
						if (paths[i].Contains(ProfilesManager.Profile.excludeKeywords[j]) == true)
							break;
					}

					if (j < ProfilesManager.Profile.excludeKeywords.Count)
						continue;

					this.exported.Add(paths[i]);
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

					this.exported.Add(paths[i]);
				}
			}
		}

		private bool	CopyPackage(string path)
		{
			lock (this.deployingProjects)
			{
				this.deployingProjects.Add(path, 0);
			}

			try
			{
				string	unityVersion = Utility.GetUnityVersion(path);
				bool	isEditorRooted = unityVersion.StartsWith("20") == false && unityVersion.CompareTo("5.2.2") < 0;
				string	package = Path.Combine("Assets", ProfilesManager.Profile.packagePath);
				string	fullPath = Path.Combine(path, package);

				if (package.StartsWith(@"Assets\Plugins") == false)
					isEditorRooted = false;

				if (Directory.Exists(fullPath) == true)
					Directory.Delete(fullPath, true);

				if (isEditorRooted == true)
				{
					string	pathEditor = fullPath.Replace("Plugins", Path.Combine("Plugins", "Editor"));
					if (Directory.Exists(pathEditor) == true)
						Directory.Delete(pathEditor, true);
				}

				Directory.CreateDirectory(fullPath);

				string	packagePath = "Assets/" + ProfilesManager.Profile.packagePath + "/";

				for (int i = 0; i < this.exported.Count; i++)
				{
					string	destination = Path.Combine(fullPath, this.exported[i].Remove(0, packagePath.Length)).Replace('\\', '/');

					if (isEditorRooted == true)
					{
						int	n = destination.IndexOf("/Editor/");
						if (n != -1)
						{
							destination = destination.Remove(n, "/Editor/".Length - 1).Replace("Plugins/", "Plugins/Editor/");
						}
					}

					Directory.CreateDirectory(Path.GetDirectoryName(destination));
					File.Copy(this.exported[i], destination);

					if (ProfilesManager.Profile.deployMeta == true)
						File.Copy(this.exported[i] + ".meta", destination + ".meta");

					lock (this.deployingProjects)
					{
						this.deployingProjects[path] = (int)((float)i / (float)this.exported.Count * 100F);
					}
				}

				InternalNGDebug.Log("Copy into \"" + fullPath + "\" completed." + (isEditorRooted == true ? " Unity < 5.2.2 detected, Editor files are correctly copied into Plugins/Editor." : ""));

				return true;
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException("Deployment at \"" + path + "\" failed.", ex);
			}
			finally
			{
				lock (this.deployingProjects)
				{
					this.deployingProjects.Remove(path);
				}
			}

			return false;
		}

		private bool	CheckProjectCompile(string projectPath)
		{
			lock (this.deployingProjects)
			{
				this.deployingProjects.Add(projectPath, 101);
			}

			try
			{
				string	unityVersion = Utility.GetUnityVersion(projectPath);
				if (string.IsNullOrEmpty(unityVersion) == true)
					return false;

				string	unityExe = NGUnityDetectorWindow.GetUnityExecutable(this.unityInstalls, unityVersion);
				if (File.Exists(unityExe) == false)
					return false;

				string	token = Guid.NewGuid().ToString();
				int		time;
				string	tempLogPath = Path.GetTempPath() + token + ".log";
				
				lock (this.compileLock)
				{
					time = Environment.TickCount;
					lock (this.deployingProjects)
					{
						this.deployingProjects[projectPath] = 102;
					}

					InternalNGDebug.Log("Compiler Token " + token + " for \"" + projectPath + '"');

					Process	unityProcess = new Process();
					unityProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
					unityProcess.StartInfo.CreateNoWindow = true;
					unityProcess.StartInfo.UseShellExecute = false;
					unityProcess.StartInfo.FileName = unityExe;
					unityProcess.StartInfo.Arguments = "-nographics -batchmode -logFile \"" + tempLogPath + "\" -quit -projectPath \"" + projectPath + '"';

					if (unityProcess.Start() == false)
					{
						InternalNGDebug.Log("Process Unity stopped with code " + unityProcess.ExitCode + ".");
						return false;
					}

					unityProcess.WaitForExit();

					if (unityProcess.ExitCode == 1)
					{
						for (int j = 0; j < this.unityProcessesDetected.Count; j++)
						{
							if (this.unityProcessesDetected[j].Contains(unityVersion) == true)
							{
								InternalNGDebug.LogWarning("Unity " + unityVersion + " is running, deployment can not compile the project.");
								return false;
							}
						}

						InternalNGDebug.LogError("Unity " + unityVersion + " has aborted.");
						return false;
					}
				}
				
				using (FileStream fs = File.Open(tempLogPath, FileMode.Open, FileAccess.Read, FileShare.Read))
				using (BufferedStream bs = new BufferedStream(fs))
				using (StreamReader sr = new StreamReader(bs))
				{
					string	line;

					while ((line = sr.ReadLine()) != null)
					{
						// Token spotted.
						if (line.Contains(token) == true)
						{
							bool	hasCompiled = false;

							while ((line = sr.ReadLine()) != null)
							{
								if (line.Contains("- starting compile") == true)
									hasCompiled = true;

								// Check compilation result.
								if (line.Contains("compilationhadfailure") == true)
								{
									if (line.Contains("True") == true)
										InternalNGDebug.LogWarning("Project \"" + projectPath + "\" did not compile correctly. (" + ((Environment.TickCount - time) / 1000F).ToString("#.##") + "s)");
									else
										InternalNGDebug.LogWarning("Project \"" + projectPath + "\" compiled but have warnings. (" + ((Environment.TickCount - time) / 1000F).ToString("#.##") + "s)");

									// Stop at errors.
									while ((line = sr.ReadLine()) != null)
									{
										if (line.Contains("CompilerOutput:-stderr") == true)
											break;
									}

									while ((line = sr.ReadLine()) != null)
									{
										if (line.Length == 0)
											continue;

										if (line.Contains("EndCompilerOutput") == true)
											break;

										if (line[0] == '-')
											continue;

										if (line.Contains("): warning CS"))
											Debug.LogWarning(line);
										else
											Debug.LogError(line);
									}

									return true;
								}
							}

							if (hasCompiled == true)
								InternalNGDebug.Log("Project \"" + projectPath + "\" compiled. (" + ((Environment.TickCount - time) / 1000F).ToString("#.##") + "s)");
							else
								InternalNGDebug.Log("Project \"" + projectPath + "\" has not changed. (" + ((Environment.TickCount - time) / 1000F).ToString("#.##") + "s)");
							return true;
						}
					}
				}

				InternalNGDebug.LogWarning("Project \"" + projectPath + "\" did not found the token. (" + ((Environment.TickCount - time) / 1000F).ToString("#.##") + "s)");
				return true;
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException(ex);
			}
			finally
			{
				lock (this.deployingProjects)
				{
					this.deployingProjects.Remove(projectPath);
				}
			}

			return false;
		}

		private void	DetectProjects(string path)
		{
			string[]	dirs = Directory.GetDirectories(path.Replace('/', '\\'));

			for (int i = 0; i < dirs.Length; i++)
			{
				string	unityVersion = Utility.GetUnityVersion(dirs[i]);
				if (string.IsNullOrEmpty(unityVersion) == true)
					continue;

				//string	unityExe = this.GetUnityExecutable(unityVersion);
				//if (string.IsNullOrEmpty(unityExe) == true)
				//	continue;

				//if (File.Exists(unityExe) == false)
				//{
				//	InternalNGDebug.LogWarning("Unity \"" + unityExe + "\" was not found for project \"" + dirs[i] + "\" (Version \"" + unityVersion + "\").");
				//	continue;
				//}

				if (ProfilesManager.Profile.projects.Contains(dirs[i]) == false)
				{
					ProfilesManager.Profile.projects.Add(dirs[i]);
					this.CheckUnityProject(dirs[i]);
				}
			}
		}

		private void	Draw(Rect rect, int index, bool isActive, bool isFocused)
		{
			string	unityVersion = Utility.GetUnityVersion(ProfilesManager.Profile.projects[index]);
			float	width = rect.width;

			rect.width = 55F;
			if (GUI.Button(rect, "Browse", "ButtonLeft") == true)
			{
				string	projectPath = EditorUtility.OpenFolderPanel("Open Unity Project", ProfilesManager.Profile.projects[index], string.Empty);

				if (string.IsNullOrEmpty(projectPath) == false)
				{
					ProfilesManager.Profile.projects[index] = projectPath;
					this.CheckUnityProject(ProfilesManager.Profile.projects[index]);
					GUI.FocusControl(null);
				}
			}

			rect.x += rect.width;
			rect.width = 40F;
			if (GUI.Button(rect, "Open", "ButtonMid") == true)
				Utility.ShowExplorer(ProfilesManager.Profile.projects[index]);

			string	error = null;
			if (this.packageFolderError != string.Empty)
			{
				error = this.packageFolderError;
				Utility.content.tooltip = this.packageFolderError;
			}
			else if (this.errors.TryGetValue(ProfilesManager.Profile.projects[index], out error) == true)
				Utility.content.tooltip = error;

			rect.x += rect.width;
			rect.width = 40F;
			EditorGUI.BeginDisabledGroup(error != null);
			Utility.content.text = "Unity";
			if (GUI.Button(rect, Utility.content, "ButtonRight") == true)
			{
				if (Utility.IsUnityProject(ProfilesManager.Profile.projects[index]) == true)
				{
					if (string.IsNullOrEmpty(unityVersion) == false)
					{
						string	unityExe = NGUnityDetectorWindow.GetUnityExecutable(this.unityInstalls, unityVersion);

						if (File.Exists(unityExe) == true)
							Process.Start(unityExe, "-projectPath \"" + ProfilesManager.Profile.projects[index] + "\"");
					}
				}
			}
			EditorGUI.EndDisabledGroup();

			Utility.content.text = unityVersion;

			rect.x += rect.width;
			rect.width = width - 195F - GUI.skin.label.CalcSize(Utility.content).x;
			EditorGUI.BeginChangeCheck();
			ProfilesManager.Profile.projects[index] = EditorGUI.TextField(rect, ProfilesManager.Profile.projects[index], this.textStyle);
			if (EditorGUI.EndChangeCheck() == true)
			{
				this.CheckUnityProject(ProfilesManager.Profile.projects[index]);
				ProfilesManager.Save();
			}

			rect.x += rect.width;
			rect.width = GUI.skin.label.CalcSize(Utility.content).x;

			GUI.Label(rect, unityVersion, this.labelStyle);

			rect.x += rect.width;
			rect.width = 60F;

			int	v = -1;

			lock (this.deployingProjects)
			{
				if (this.deployingProjects.TryGetValue(ProfilesManager.Profile.projects[index], out v) == true)
				{
					if (v == 101)
						Utility.content.text = "Pending";
					else if (v == 102)
						Utility.content.text = "Compiling";
					else
						Utility.content.text = v + "%";

					this.Repaint();
				}
				else
					Utility.content.text = "Deploy";
			}

			EditorGUI.BeginDisabledGroup(error != null || Utility.content.text != "Deploy");
			if (GUI.Button(rect, Utility.content) == true)
				this.AsyncDeploy(ProfilesManager.Profile.projects[index]);
			EditorGUI.EndDisabledGroup();

			if (error != null)
				Utility.content.tooltip = string.Empty;
		}

		private void	AsyncDeploy(string project)
		{
			this.GenerateExportedAssets();

			Thread	thread = new Thread(new ParameterizedThreadStart(this.Deploy));

			thread.Name = "Deploy " + project;
			thread.Start(project);
		}

		private void	Deploy(object data)
		{
			string	project = (string)data;

			if (this.CopyPackage(project) == true)
				this.CheckProjectCompile(project);
		}

		private void	CheckUnityProject(string path)
		{
			string	error = null;

			if (Utility.IsUnityProject(path) == false)
				error = "Path is not a valid Unity project.";
			else
			{
				string	unityVersion = Utility.GetUnityVersion(path);
				if (string.IsNullOrEmpty(unityVersion) == true)
					error = "Can't extract Unity version from \"" + path + "\".";
				else
				{
					string	unityExe = NGUnityDetectorWindow.GetUnityExecutable(this.unityInstalls, unityVersion);

					if (File.Exists(unityExe) == false)
						error = "Can't find Unity Editor for version \"" + unityVersion + "\".";
				}
			}

			if (error == null)
			{
				if (this.errors.ContainsKey(path) == true)
					this.errors.Remove(path);
			}
			else
			{
				if (this.errors.ContainsKey(path) == true)
					this.errors[path] = error;
				else
					this.errors.Add(path, error);
			}
		}

		private void	UpdateProcesses()
		{
			//var	pp = Process.GetProcesses();
			//int	max = 10000;
			//foreach (var item in pp)
			//{
			//	if (item.MainModule.FileName.Contains("Unity.exe") == true)
			//	{
			//		int	i = 0;
			//		while (!item.HasExited && ++i < max)
			//		{
			//			item.Refresh();
			//			if (item.MainWindowHandle.ToInt32() != 0)
			//			{
			//				//item.MainWindowHandle;
			//				item.Refresh();
			//				Debug.Log(item + "	" + item.MainWindowHandle + "	" + item.Handle + "	-" + item.MainWindowTitle);
			//				break;
			//			}
			//		}
			//	}

			//}
			//return;

			new Thread(() =>
			{
				List<string>	processes = MyEnumWindows.GetWindowTitles(false);

				// TODO If processes has 0, it failed. Need to find a workaround.
				if (processes.Count > 0)
				{
					this.unityProcessesDetected.Clear();

					for (int i = 0; i < processes.Count; i++)
					{
						if (processes[i].Contains("Unity ") == true)
							this.unityProcessesDetected.Add(processes[i]);
					}

					EditorApplication.delayCall += this.Repaint;
				}
			}).Start();
		}

		private void	UpdateUnityProjectState()
		{
			NGUnityDetectorWindow.GetInstalls(this.unityInstalls);

			for (int i = 0; i < ProfilesManager.Profile.projects.Count; i++)
				this.CheckUnityProject(ProfilesManager.Profile.projects[i]);

			this.Repaint();
		}

		private void	OnSetProfile()
		{
			this.exported = null;

			for (int i = 0; i < ProfilesManager.Profile.projects.Count; i++)
				this.CheckUnityProject(ProfilesManager.Profile.projects[i]);

			this.list = new ReorderableList(ProfilesManager.Profile.projects, typeof(string), true, false, true, true);
			this.list.headerHeight = 0F;
			this.list.drawElementCallback += this.Draw;
			this.list.onAddCallback += (rl) =>
			{
				if (ProfilesManager.Profile.projects.Count > 0)
					ProfilesManager.Profile.projects.Add(ProfilesManager.Profile.projects[ProfilesManager.Profile.projects.Count - 1]);
				else
					ProfilesManager.Profile.projects.Add(string.Empty);
			};
			this.list.onRemoveCallback += (rl) => ProfilesManager.Profile.projects.RemoveAt(rl.index);
			this.list.onCanRemoveCallback += (r) => true;
		}
	}
}