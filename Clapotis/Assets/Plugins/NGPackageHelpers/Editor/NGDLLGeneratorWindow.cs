#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
#define UNITY_4
#endif

using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEditorInternal;

namespace NGPackageHelpers
{
	using UnityEngine;

	public class NGDLLGeneratorWindow : EditorWindow
	{
		private enum Frameworks
		{
			UnityFullv35,
			UnitySubsetv35,
			UnityMicrov35,
			UnityWebv35,
		}

		public const string	Title = "NG DLL Generator";
		public static char	KeywordSeparator = ';';
		public const string	ShareReferencesPath = "..\\Share";
		public const string	ConfuserExConfModel = @"<project outputDir=""{1}\Output"" baseDir=""{1}"" xmlns=""http://confuser.codeplex.com"">
	<module path=""{0}.dll"">
		<rule pattern=""true"" inherit=""false"">
			<protection id=""constants"" />
			<protection id=""anti ildasm"" />
		</rule>
		<rule pattern=""{2}"" inherit=""false"">
			<protection id=""rename"" />
		</rule>
	</module>
	<probePath>{1}\" + NGDLLGeneratorWindow.ShareReferencesPath + @"</probePath>
</project>";

		public static readonly string[][]	FrameworksReferences = new string[][] {
				// Unity Full v3.5
				new string[] {
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Full v3.5\mscorlib.dll",
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Full v3.5\System.Core.dll",
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Full v3.5\System.Data.dll",
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Full v3.5\System.dll",
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Full v3.5\System.Xml.dll",
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Full v3.5\System.Xml.Linq.dll",
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Full v3.5\Boo.Lang.dll",
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Full v3.5\UnityScript.Lang.dll",
				},
				// Unity Subset v3.5
				new string[] {
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Subset v3.5\mscorlib.dll",
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Subset v3.5\System.Core.dll",
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Subset v3.5\System.Data.dll",
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Subset v3.5\System.dll",
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Subset v3.5\System.Xml.dll",
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Subset v3.5\System.Xml.Linq.dll",
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Subset v3.5\Boo.Lang.dll",
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Subset v3.5\UnityScript.Lang.dll",
				},
				// Unity Micro v3.5
				new string[] {
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Micro v3.5\mscorlib.dll",
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Micro v3.5\Boo.Lang.dll",
				},
				// Unity Web v3.5
				new string[] {
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Web v3.5\mscorlib.dll",
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Web v3.5\System.Core.dll",
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Web v3.5\System.Data.dll",
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Web v3.5\System.dll",
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Web v3.5\System.Xml.dll",
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Web v3.5\Boo.Lang.dll",
					@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Unity Web v3.5\UnityScript.Lang.dll",
				},
		};

		private readonly GUIContent	DLLNameContent = new GUIContent("DLL Name", "Name of generated DLL.");
		private readonly GUIContent	appendVersionContent = new GUIContent("Append Version", "Appends the 2 first number of the given Unity version in the format \"Name{Major}.{Minor}.dll\".");
		private readonly GUIContent	appendEditorContent = new GUIContent("Append Editor", "Appends Editor when generating editor DLL.");
		private readonly GUIContent	outputPathContent = new GUIContent("Output Path", "Destination path of generated DLL.\nIf empty, the path will be {Package Path}.");
		private readonly GUIContent	outputEditorPathContent = new GUIContent("Output Editor Path", "Destination editor path of generated DLL.\nIf empty, the path will be {Package Path}/Editor.");
		private readonly GUIContent	packagePathContent = new GUIContent("Package Path", "Relative path in Assets from where the sources will be processed.");
		private readonly GUIContent	definesContent = new GUIContent("Defines", string.Empty);
		private readonly GUIContent	editorDefinesContent = new GUIContent("Editor Defines", string.Empty);

		private ReorderableList		list;
		private Vector2				scrollPosition;
		private bool				canGenerate;

		private Dictionary<string, string>	unityInstalls = null;

		[NonSerialized]
		private List<string>	exportedCodeFiles;
		[NonSerialized]
		private List<string>	exportedEditorCodeFiles;

		private readonly List<Thread>	runningThreads = new List<Thread>();
		[NonSerialized]
		private string[]		assetPaths = null;

		[NonSerialized]
		private string[]		paths = null;

		[NonSerialized]
		private GUIStyle	wrapInput;

		[MenuItem(Constants.MenuItemPath + NGDLLGeneratorWindow.Title + " %#G", priority = 3)]
		public static void	Open()
		{
			EditorWindow.GetWindow<NGDLLGeneratorWindow>(true, NGDLLGeneratorWindow.Title, true);
		}

		protected virtual void	OnEnable()
		{
			ProfilesManager.SetProfile += this.OnSetProfile;
			if (ProfilesManager.IsReady == true)
				this.OnSetProfile();

			this.assetPaths = AssetDatabase.GetAllAssetPaths();
		}

		protected virtual void	OnDisable()
		{
			ProfilesManager.SetProfile -= this.OnSetProfile;
		}

		protected virtual void	OnGUI()
		{
			ProfilesManager.OnProfilesBarGUI();
			if (ProfilesManager.IsReady == false)
				return;

			if (this.wrapInput == null)
			{
				this.wrapInput = new GUIStyle(GUI.skin.textArea)
				{
					wordWrap = true
				};
			}
			this.canGenerate = true;

			this.scrollPosition = EditorGUILayout.BeginScrollView(this.scrollPosition);
			{
				EditorGUILayout.BeginHorizontal("Toolbar");
				{
					EditorGUILayout.LabelField("Parameters");
				}
				EditorGUILayout.EndHorizontal();

				EditorGUI.BeginChangeCheck();
				EditorGUI.BeginChangeCheck();
				ProfilesManager.Profile.packagePath = EditorGUILayout.TextField(this.packagePathContent, ProfilesManager.Profile.packagePath);
				Rect	r2 = GUILayoutUtility.GetLastRect();
				r2.x += 107F;
				GUI.Label(r2, "Assets/");
				if (EditorGUI.EndChangeCheck() == true)
				{
					this.exportedCodeFiles = null;
					this.exportedEditorCodeFiles = null;
				}

				if (Directory.Exists("Assets/" + ProfilesManager.Profile.packagePath) == false)
				{
					EditorGUILayout.HelpBox("Package at \"Assets/" + ProfilesManager.Profile.packagePath + "\" was not found.", MessageType.Warning);
					this.canGenerate = false;
				}

				GUILayout.Space(10F);

				EditorGUILayout.BeginHorizontal("Toolbar");
				{
					EditorGUILayout.LabelField("Dynamic Link Library");
				}
				EditorGUILayout.EndHorizontal();
			
				using (LabelWidthRestorer.Get(110F))
				{
					ProfilesManager.Profile.DLLName = EditorGUILayout.TextField(this.DLLNameContent, ProfilesManager.Profile.DLLName);
					if ((!string.IsNullOrEmpty(ProfilesManager.Profile.DLLName) &&
						 ProfilesManager.Profile.DLLName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0) == false)
					{
						EditorGUILayout.HelpBox("DLL Name has an invalid filename.", MessageType.Warning);
					}

					ProfilesManager.Profile.appendVersion = EditorGUILayout.Toggle(this.appendVersionContent, ProfilesManager.Profile.appendVersion);
					ProfilesManager.Profile.appendEditor = EditorGUILayout.Toggle(this.appendEditorContent, ProfilesManager.Profile.appendEditor);
					ProfilesManager.Profile.outputPath = EditorGUILayout.TextField(this.outputPathContent, ProfilesManager.Profile.outputPath);
					ProfilesManager.Profile.outputEditorPath = EditorGUILayout.TextField(this.outputEditorPathContent, ProfilesManager.Profile.outputEditorPath);

					ProfilesManager.Profile.defines = EditorGUILayout.TextField(this.definesContent, ProfilesManager.Profile.defines);
					ProfilesManager.Profile.editorDefines = EditorGUILayout.TextField(this.editorDefinesContent, ProfilesManager.Profile.editorDefines);

					EditorGUILayout.BeginHorizontal();
					{
						ProfilesManager.Profile.showReferences = EditorGUILayout.Foldout(ProfilesManager.Profile.showReferences, "DLLs References");

						GUILayout.FlexibleSpace();

						EditorGUI.BeginChangeCheck();
						int	framework = EditorGUILayout.Popup(0, new string[] { "Framework", "Unity Full v3.5", "Unity Subset v3.5", "Unity Micro v3.5", "Unity Web v3.5" });
						if (EditorGUI.EndChangeCheck() == true)
						{
							for (int i = 0; i < NGDLLGeneratorWindow.FrameworksReferences.Length; i++)
							{
								for (int j = 0; j < NGDLLGeneratorWindow.FrameworksReferences[i].Length; j++)
									ProfilesManager.Profile.references.RemoveAll(r => r.path == NGDLLGeneratorWindow.FrameworksReferences[i][j]);
							}

							for (int i = 0; i < NGDLLGeneratorWindow.FrameworksReferences[framework - 1].Length; i++)
								ProfilesManager.Profile.references.Insert(0, new Reference() { path = NGDLLGeneratorWindow.FrameworksReferences[framework - 1][i] });
						}
					}
					EditorGUILayout.EndHorizontal();

					if (ProfilesManager.Profile.showReferences == true)
					{
						//EditorGUI.BeginDisabledGroup(true);
						//{
						//	for (int i = 0; i < ProfilesManager.Profile.unityReferences.Count; i++)
						//		EditorGUILayout.TextField(ProfilesManager.Profile.unityReferences[i]);
						//}
						//EditorGUI.EndDisabledGroup();

						for (int i = 0; i < ProfilesManager.Profile.references.Count; i++)
						{
							EditorGUILayout.BeginHorizontal();
							{
								if (GUILayout.Button("Browse", GUILayout.Width(60F)) == true)
								{
									string	path = ProfilesManager.Profile.references[i].path;
									if (string.IsNullOrEmpty(path) == false)
										path = Path.GetDirectoryName(path);

#if UNITY_4 || UNITY_5_0 || UNITY_5_1
									string	projectPath = EditorUtility.OpenFilePanel("DLL Reference", path, "dll");
#else
									string	projectPath = EditorUtility.OpenFilePanelWithFilters("DLL Reference", path, new string[] { "DLL", "dll" });
#endif

									if (string.IsNullOrEmpty(projectPath) == false)
									{
										ProfilesManager.Profile.references[i].path = projectPath;
										GUI.FocusControl(null);
									}
								}

								ProfilesManager.Profile.references[i].localProfile = GUILayout.Toggle(ProfilesManager.Profile.references[i].localProfile, "Local Project", "ToolbarButton", GUILayout.ExpandWidth(false));

								if (ProfilesManager.Profile.references[i].localProfile == true)
								{
									if (GUILayout.Button("Set", GUILayout.ExpandWidth(false)) == true)
									{
										GenericMenu	menu = new GenericMenu();

										for (int j = 0; j < ProfilesManager.Profiles.Count; j++)
											menu.AddItem(new GUIContent("#" + (j + 1) + " " + ProfilesManager.Profiles[j].name), false, this.SetReferenceProfile, new object[] { i, ProfilesManager.Profiles[j].name });

										menu.ShowAsContext();
									}

									if (string.IsNullOrEmpty(ProfilesManager.Profile.references[i].nameProfile) == false)
									{
										if (ProfilesManager.GetProfile(ProfilesManager.Profile.references[i].nameProfile) == null)
											GUILayout.Label(ProfilesManager.Profile.references[i].nameProfile + " does not exist");
										else
											GUILayout.Label(ProfilesManager.Profile.references[i].nameProfile);
									}
									else
										GUILayout.FlexibleSpace();

									ProfilesManager.Profile.references[i].localCopy = GUILayout.Toggle(ProfilesManager.Profile.references[i].localCopy, "Local Copy", "ToolbarButton", GUILayout.ExpandWidth(false));
									ProfilesManager.Profile.references[i].editor = GUILayout.Toggle(ProfilesManager.Profile.references[i].editor, "Is Editor", "ToolbarButton", GUILayout.ExpandWidth(false));
								}
								else
								{
									ProfilesManager.Profile.references[i].path = EditorGUILayout.TextField(ProfilesManager.Profile.references[i].path);
									ProfilesManager.Profile.references[i].localCopy = GUILayout.Toggle(ProfilesManager.Profile.references[i].localCopy, "Local Copy", "ToolbarButton", GUILayout.ExpandWidth(false));
									EditorGUI.BeginDisabledGroup(ProfilesManager.Profile.references[i].localCopy == false);
									{
										ProfilesManager.Profile.references[i].editor = GUILayout.Toggle(ProfilesManager.Profile.references[i].editor, "Is Editor", "ToolbarButton", GUILayout.ExpandWidth(false));
									}
									EditorGUI.EndDisabledGroup();
								}

								if (GUILayout.Button("X", GUILayout.Width(16F)) == true)
								{
									ProfilesManager.Profile.references.RemoveAt(i);
									ProfilesManager.Save();
									return;
								}
							}
							EditorGUILayout.EndHorizontal();
						}

						if (GUILayout.Button("Add DLL reference") == true)
						{
							if (ProfilesManager.Profile.references.Count > 0)
							{
								Reference	last = ProfilesManager.Profile.references[ProfilesManager.Profile.references.Count - 1];
								ProfilesManager.Profile.references.Add(new Reference() { localProfile = last.localProfile, nameProfile = last.nameProfile, path = last.path, editor = last.editor, localCopy = last.localCopy });
							}
							else
								ProfilesManager.Profile.references.Add(new Reference());
							ProfilesManager.Save();
						}
					}
				}

				using (LabelWidthRestorer.Get(240F))
				{
					ProfilesManager.Profile.unityEngineDLLRequiredForEditor = EditorGUILayout.Toggle("UnityEditor DLL requires UnityEngine DLL", ProfilesManager.Profile.unityEngineDLLRequiredForEditor);
					ProfilesManager.Profile.generateDocumentation = EditorGUILayout.Toggle("Generate Documentation", ProfilesManager.Profile.generateDocumentation);
					ProfilesManager.Profile.generateProgramDatabase = EditorGUILayout.Toggle("Generate Program Database (PDB)", ProfilesManager.Profile.generateProgramDatabase);
				}

				GUILayout.Space(10F);

				EditorGUILayout.BeginHorizontal("Toolbar");
				{
					EditorGUILayout.LabelField("Obfuscation");
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				{
					ProfilesManager.Profile.confuserExPath = EditorGUILayout.TextField("ConfuserEx CLI Exe Path", ProfilesManager.Profile.confuserExPath);

					if (GUILayout.Button("Browse", "ButtonLeft", GUILayout.Width(60F)) == true)
					{
						string	projectPath = EditorUtility.OpenFilePanel("ConfuserEx CLI Executable", string.IsNullOrEmpty(ProfilesManager.Profile.confuserExPath) == false ? Path.GetDirectoryName(ProfilesManager.Profile.confuserExPath) : string.Empty, "exe");

						if (string.IsNullOrEmpty(projectPath) == false)
						{
							ProfilesManager.Profile.confuserExPath = projectPath;
							GUI.FocusControl(null);
						}
					}

					if (GUILayout.Button("Open", "ButtonRight", GUILayout.Width(50F)) == true)
						Utility.ShowExplorer(ProfilesManager.Profile.confuserExPath);
				}
				EditorGUILayout.EndHorizontal();

				if (string.IsNullOrEmpty(ProfilesManager.Profile.confuserExPath) == false && File.Exists(ProfilesManager.Profile.confuserExPath) == false)
					EditorGUILayout.HelpBox("ConfuserEx was not found.", MessageType.Warning);

				ProfilesManager.Profile.showObfuscationFilters = EditorGUILayout.Foldout(ProfilesManager.Profile.showObfuscationFilters, "Obfuscation Filters");
				if (ProfilesManager.Profile.showObfuscationFilters == true)
					ProfilesManager.Profile.obfuscateFilters = EditorGUILayout.TextArea(ProfilesManager.Profile.obfuscateFilters, this.wrapInput, GUILayout.MaxWidth(this.position.width));

				GUILayout.Space(10F);

				EditorGUILayout.BeginHorizontal("Toolbar");
				{
					EditorGUILayout.LabelField("Resources");
				}
				EditorGUILayout.EndHorizontal();

				ProfilesManager.Profile.showForceResourcesKeywords = EditorGUILayout.Foldout(ProfilesManager.Profile.showForceResourcesKeywords, "Force Resources Keywords");
				if (ProfilesManager.Profile.showForceResourcesKeywords == true)
				{
					EditorGUILayout.HelpBox("Any path containing a keyword will be discarded from the DLL sources and becomes a resource.", MessageType.Info);

					for (int i = 0; i < ProfilesManager.Profile.forceResourcesKeywords.Count; i++)
					{
						EditorGUILayout.BeginHorizontal();
						{
							ProfilesManager.Profile.forceResourcesKeywords[i] = EditorGUILayout.TextField(ProfilesManager.Profile.forceResourcesKeywords[i]);
							if (GUILayout.Button("X", GUILayout.Width(20F)) == true)
							{
								ProfilesManager.Profile.forceResourcesKeywords.RemoveAt(i);
								break;
							}
						}
						EditorGUILayout.EndHorizontal();
					}

					EditorGUILayout.BeginHorizontal();
					{
						if (GUILayout.Button("Add keyword") == true)
							ProfilesManager.Profile.forceResourcesKeywords.Add(string.Empty);

						if (GUILayout.Button("Save") == true)
						{
							StringBuilder	buffer = Utility.GetBuffer();

							for (int i = 0; i < ProfilesManager.Profile.forceResourcesKeywords.Count; i++)
							{
								if (ProfilesManager.Profile.forceResourcesKeywords[i] != string.Empty)
								{
									buffer.Append(ProfilesManager.Profile.forceResourcesKeywords[i]);
									buffer.Append(NGDLLGeneratorWindow.KeywordSeparator);
								}
							}

							if (buffer.Length > 0)
								--buffer.Length;

							this.exportedCodeFiles = null;
							this.exportedEditorCodeFiles = null;

							ProfilesManager.Save();
						}
					}
					EditorGUILayout.EndHorizontal();
				}

				ProfilesManager.Profile.copyMeta = EditorGUILayout.Toggle("Copy Meta", ProfilesManager.Profile.copyMeta);

				if (EditorGUI.EndChangeCheck() == true)
					ProfilesManager.Save();

				GUILayout.Space(10F);

				using (LabelWidthRestorer.Get(90F))
				{
					this.list.DoLayoutList();
				}

				EditorGUILayout.BeginHorizontal();
				{
					EditorGUI.BeginDisabledGroup(!this.canGenerate);
					{
						if (GUILayout.Button("All UnityEngine", "ButtonLeft") == true)
							this.AsyncGenerateDLLOnAllProjects();

						EditorGUI.BeginDisabledGroup(File.Exists(ProfilesManager.Profile.confuserExPath) == false);
						{
							if (GUILayout.Button("Obfuscate", "ButtonRight") == true)
								this.AsyncObfuscateDLLOnAllProjects();
						}
						EditorGUI.EndDisabledGroup();
					}
					EditorGUI.EndDisabledGroup();

					EditorGUI.BeginDisabledGroup(!this.canGenerate);
					{
						if (GUILayout.Button("All UnityEditor", "ButtonLeft") == true)
							this.AsyncGenerateDLLEditorOnAllProjects();

						EditorGUI.BeginDisabledGroup(File.Exists(ProfilesManager.Profile.confuserExPath) == false);
						{
							if (GUILayout.Button("Obfuscate", "ButtonRight") == true)
								this.AsyncObfuscateDLLEditorOnAllProjects();
						}
						EditorGUI.EndDisabledGroup();
					}
					EditorGUI.EndDisabledGroup();

					EditorGUI.BeginDisabledGroup(!this.canGenerate);
					{
						if (GUILayout.Button("All Copy Resources") == true)
							this.CopyResourcesOnAllProjects();

						if (GUILayout.Button("All") == true)
						{
							this.CopyResourcesOnAllProjects();

							this.AsyncGenerateDLLOnAllProjects();

							while (this.runningThreads.Count > 0)
							{
								for (int i = 0; i < this.runningThreads.Count; i++)
								{
									if (this.runningThreads[i].IsAlive == false)
										this.runningThreads.RemoveAt(i--);
								}
							}

							this.AsyncGenerateDLLEditorOnAllProjects();

							while (this.runningThreads.Count > 0)
							{
								for (int i = 0; i < this.runningThreads.Count; i++)
								{
									if (this.runningThreads[i].IsAlive == false)
										this.runningThreads.RemoveAt(i--);
								}
							}

							this.AsyncObfuscateDLLEditorOnAllProjects();

							while (this.runningThreads.Count > 0)
							{
								for (int i = 0; i < this.runningThreads.Count; i++)
								{
									if (this.runningThreads[i].IsAlive == false)
										this.runningThreads.RemoveAt(i--);
								}
							}

							this.AsyncObfuscateDLLOnAllProjects();
						}
					}
					EditorGUI.EndDisabledGroup();
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndScrollView();
		}

		private void	DrawUnityProject(Rect r, int i, bool isActive, bool isFocused)
		{
			float	w = r.x + r.width;

			r.width = 60F;
			r.height = 16F;
			if (GUI.Button(r, "Browse", "ButtonLeft") == true)
			{
				string	projectPath = EditorUtility.OpenFolderPanel("Unity Project", ProfilesManager.Profile.unityProjectPaths[i], string.Empty);

				if (string.IsNullOrEmpty(projectPath) == false)
				{
					ProfilesManager.Profile.unityProjectPaths[i] = projectPath;
					GUI.FocusControl(null);
				}
			}
			r.x += r.width;

			r.width = 50F;
			if (GUI.Button(r, "Open", "ButtonMid") == true)
				Utility.ShowExplorer(ProfilesManager.Profile.unityProjectPaths[i]);
			r.x += r.width;

			string	unityVersion;

			EditorGUI.BeginDisabledGroup((unityVersion = Utility.GetUnityVersion(ProfilesManager.Profile.unityProjectPaths[i])) == string.Empty || Utility.IsUnityProject(ProfilesManager.Profile.unityProjectPaths[i]) == false);
			{
				if (GUI.Button(r, "Unity", "ButtonRight") == true)
				{
					string	unityExe;

					this.PrepareUnityInstalls();
					if (this.unityInstalls.TryGetValue(unityVersion, out unityExe) == true)
					{
						unityExe = Path.Combine(unityExe, @"Editor\Unity.exe");

						if (File.Exists(unityExe) == true)
							Process.Start(unityExe, "-projectPath \"" + ProfilesManager.Profile.unityProjectPaths[i] + "\"");
					}
				}
				r.x += r.width;
			}
			EditorGUI.EndDisabledGroup();

			Utility.content.text = unityVersion;
			float	versionWidth = GUI.skin.label.CalcSize(Utility.content).x;

			r.width = w - r.x - versionWidth - 2F;
			ProfilesManager.Profile.unityProjectPaths[i] = EditorGUI.TextField(r, ProfilesManager.Profile.unityProjectPaths[i]);
			r.x += r.width + 2F;

			r.width = versionWidth;
			EditorGUI.LabelField(r, unityVersion);

			r.y += r.height + 2F;

			r.x = 0F;
			if (Utility.IsUnityProject(ProfilesManager.Profile.unityProjectPaths[i]) == false)
			{
				r.width = w;
				EditorGUI.HelpBox(r, "Unity Project does not point to a genuine Unity project.", MessageType.Warning);
			}
			else
			{
				string	dllPath = this.GetRealOutputPath(ProfilesManager.Profile, i, unityVersion, false);
				float	obfuscateWidth = 70F;
				float	copyResourcesWidth = 130F;
				float	generateWidth = ((w - copyResourcesWidth) / 2F) - 2F;
				bool	confuserExeExist = File.Exists(ProfilesManager.Profile.confuserExPath);

				r.x += 3F;
				r.width = generateWidth - obfuscateWidth;
				EditorGUI.BeginDisabledGroup(!this.canGenerate);
				if (GUI.Button(r, "Generate UnityEngine", "ButtonLeft") == true)
					this.AsyncGenerateDLL(ProfilesManager.Profile, i, false, false, ref this.exportedCodeFiles);
				r.x += r.width;

				r.width = obfuscateWidth;
				EditorGUI.BeginDisabledGroup(confuserExeExist == false || File.Exists(dllPath) == false);
				if (GUI.Button(r, "Obfuscate", "ButtonRight") == true)
				{
					List<string>	unityReferences = new List<string>();

					this.DetectUnityReferences(i, unityReferences);
					this.ObfuscateDLL(dllPath, ProfilesManager.Profile, i, unityReferences, new string[] { });
				}
				EditorGUI.EndDisabledGroup();
				r.x += r.width + 2F;
				EditorGUI.EndDisabledGroup();

				r.width = generateWidth - obfuscateWidth;
				EditorGUI.BeginDisabledGroup(this.canGenerate == false || (ProfilesManager.Profile.unityEngineDLLRequiredForEditor == true && File.Exists(dllPath) == false));
				{
					Utility.content.text = "Generate UnityEditor";
					if (ProfilesManager.Profile.unityEngineDLLRequiredForEditor == true)
						Utility.content.tooltip = "DLL UnityEditor requires DLL UnityEngine to be compiled first.";
					if (GUI.Button(r, Utility.content, "ButtonLeft") == true)
						this.AsyncGenerateDLL(ProfilesManager.Profile, i, true, false, ref this.exportedEditorCodeFiles);
					Utility.content.tooltip = string.Empty;
				}
				r.x += r.width;

				string	editorDllPath = this.GetRealOutputPath(ProfilesManager.Profile, i, unityVersion, true);

				r.width = obfuscateWidth;
				EditorGUI.BeginDisabledGroup(confuserExeExist == false || File.Exists(editorDllPath) == false);
				if (GUI.Button(r, "Obfuscate", "ButtonRight") == true)
				{
					List<string>	unityReferences = new List<string>();

					this.DetectUnityReferences(i, unityReferences);
					this.ObfuscateDLL(editorDllPath, ProfilesManager.Profile, i, unityReferences, ProfilesManager.Profile.unityEngineDLLRequiredForEditor == false ? new string[] { } : new string[] { dllPath });
				}
				EditorGUI.EndDisabledGroup();
				r.x += r.width + 2F;
				EditorGUI.EndDisabledGroup();

				r.width = copyResourcesWidth;
				EditorGUI.BeginDisabledGroup(!this.canGenerate);
				if (GUI.Button(r, "Copy Resources") == true)
					this.CopyResources(ProfilesManager.Profile, i);
				EditorGUI.EndDisabledGroup();
			}
		}

		private void	PrepareUnityInstalls()
		{
			if (this.unityInstalls == null)
			{
				this.unityInstalls = new Dictionary<string, string>();
				NGUnityDetectorWindow.GetInstalls(this.unityInstalls);
			}
		}

		internal void	DetectUnityReferences(int unityProjectIndex, List<string> unityReferences)
		{
			unityReferences.Clear();

			string	unityVersion = Utility.GetUnityVersion(ProfilesManager.Profile.unityProjectPaths[unityProjectIndex]);
			string	unityExe;

			this.PrepareUnityInstalls();
			if (this.unityInstalls.TryGetValue(unityVersion, out unityExe) == true)
			{
				string	unityLibraryPath = Path.Combine(unityExe, @"Editor\Data\Managed");

				if (Directory.Exists(unityLibraryPath) == true)
				{
					string[]	files = Directory.GetFiles(unityLibraryPath, "*.dll");

					for (int i = 0; i < files.Length; i++)
						unityReferences.Add(files[i]);

					if (unityReferences.Count == 0)
						ProfilesManager.Profile.showReferences = true;
				}

				string	unityExtensionsPath = Path.Combine(unityExe, @"Editor\Data\UnityExtensions\Unity");

				if (Directory.Exists(unityExtensionsPath) == true)
				{
					string[]	files = Directory.GetFiles(unityExtensionsPath, "ivy.xml", SearchOption.AllDirectories);

					for (int i = 0; i < files.Length; i++)
					{
						using (FileStream fs = File.Open(files[i], FileMode.Open, FileAccess.Read, FileShare.Read))
						using (BufferedStream bs = new BufferedStream(fs))
						using (StreamReader sr = new StreamReader(bs))
						{
							string	line;

							while ((line = sr.ReadLine()) != null)
							{
								if (line.Contains("artifact") == true)
								{
									int		n = line.IndexOf("name=\"") + "name=\"".Length;
									int		closingQuote = line.IndexOf('"', n);
									string	libraryPath = line.Substring(n, closingQuote - n);

									n = line.IndexOf("ext=\"") + "ext=\"".Length;
									closingQuote = line.IndexOf('"', n);
									libraryPath += '.' + line.Substring(n, closingQuote - n);

									unityReferences.Add(Path.Combine(Path.GetDirectoryName(files[i]), libraryPath));
								}
							}
						}
					}

					if (unityReferences.Count == 0)
						ProfilesManager.Profile.showReferences = true;
				}
			}
		}

		private bool	IsAssetInPackage(string assetPath, string packagePath)
		{
			if (assetPath.Length - "Assets/".Length <= packagePath.Length)
				return false;

			int	i = "Assets/".Length;

			for (int j = 0; i < assetPath.Length && j < packagePath.Length; i++, j++)
			{
				if (assetPath[i] == packagePath[j])
					continue;

				if ((assetPath[i] != Path.DirectorySeparatorChar && assetPath[i] != Path.AltDirectorySeparatorChar) ||
					(packagePath[j] != Path.DirectorySeparatorChar && packagePath[j] != Path.AltDirectorySeparatorChar))
				{
					return false;
				}
			}

			return assetPath[i] == Path.DirectorySeparatorChar || assetPath[i] == Path.AltDirectorySeparatorChar;
		}

		private void	SetReferenceProfile(object raw)
		{
			object[]	array = raw as object[];
			int			referenceIndex = (int)array[0];
			string		name = array[1] as string;

			ProfilesManager.Profile.references[referenceIndex].nameProfile = name;
		}

		internal void	CopyResources(Profile profile, int unityProjectIndex)
		{
			if (this.paths == null)
				this.paths = AssetDatabase.GetAllAssetPaths();

			List<string>	exportedResources = new List<string>(64);
			List<string>	exportedExternalResources = new List<string>(32);

			for (int i = 0; i < paths.Length; i++)
			{
				int	j = 0;

				for (; j < profile.forceResourcesKeywords.Count; j++)
				{
					if (paths[i].Contains(profile.forceResourcesKeywords[j]) == true && Directory.Exists(paths[i]) == false)
						break;
				}

				if (j < profile.forceResourcesKeywords.Count)
				{
					exportedExternalResources.Add(paths[i]);
					continue;
				}

				if (this.IsAssetInPackage(paths[i], profile.packagePath) == true &&
					Directory.Exists(paths[i]) == false &&
					paths[i].EndsWith(".cs") == false)
				{
					for (j = 0; j < NGPackageExcluderWindow.DefaultKeywords.Length; j++)
					{
						if (paths[i].Contains(NGPackageExcluderWindow.DefaultKeywords[j]) == true)
							break;
					}

					if (j < NGPackageExcluderWindow.DefaultKeywords.Length)
						continue;

					j = 0;

					for (; j < profile.excludeKeywords.Count; j++)
					{
						if (paths[i].Contains(profile.excludeKeywords[j]) == true)
							break;
					}

					if (j < profile.excludeKeywords.Count)
						continue;

					exportedResources.Add(paths[i]);
				}
			}

			string	unityVersion = Utility.GetUnityVersion(profile.unityProjectPaths[unityProjectIndex]);
			string	finalPath = Path.Combine(profile.unityProjectPaths[unityProjectIndex], Path.GetDirectoryName(this.GetRealOutputPath(profile, unityProjectIndex, unityVersion, false)));

			if (Directory.Exists(finalPath) == true)
			{
				Directory.Delete(finalPath, true);
				InternalNGDebug.Log("Resources folder \"" + finalPath + "\" erased.");
			}

			if (exportedResources.Count > 0 || exportedExternalResources.Count > 0)
			{
				Directory.CreateDirectory(finalPath);

				for (int i = 0; i < exportedResources.Count; i++)
				{
					string	relativeDestPath = exportedResources[i].Substring(profile.packagePath.Length + "Assets/".Length + 1);
					string	absoluteDestPath = Path.Combine(finalPath, relativeDestPath).Replace('\\', '/');

					Directory.CreateDirectory(Path.GetDirectoryName(absoluteDestPath));
					File.Copy(exportedResources[i], absoluteDestPath);

					if (profile.copyMeta == true)
						File.Copy(exportedResources[i] + ".meta", absoluteDestPath + ".meta");
				}

				for (int i = 0; i < exportedExternalResources.Count; i++)
				{
					string	absoluteDestPath = Path.Combine(profile.unityProjectPaths[unityProjectIndex], exportedExternalResources[i]).Replace('\\', '/');

					Directory.CreateDirectory(Path.GetDirectoryName(absoluteDestPath));
					File.Copy(exportedExternalResources[i], absoluteDestPath, true);

					if (profile.copyMeta == true)
						File.Copy(exportedExternalResources[i] + ".meta", absoluteDestPath + ".meta", true);
				}

				InternalNGDebug.Log("Copy resources to \"" + finalPath + "\" completed.");
			}
			else
				InternalNGDebug.Log("No resources for \"" + profile.name + "\".");
		}

		internal void	AsyncObfuscateDLL(Profile profile, int unityProjectIndex, string dllPath, string[] references)
		{
			List<string>	unityReferences = new List<string>();

			this.DetectUnityReferences(unityProjectIndex, unityReferences);

			Thread	thread = new Thread(() => this.ObfuscateDLL(dllPath, profile, unityProjectIndex, unityReferences, references))
			{
				Name = "Obfuscate " + unityProjectIndex
			};
			thread.Start();

			this.runningThreads.Add(thread);
		}

		internal void	AsyncGenerateDLL(Profile profile, int unityProjectIndex, bool isEditor, bool obfuscate, ref List<string> exportedFiles)
		{
			this.GenerateExportedFiles(profile, isEditor, ref exportedFiles);

			List<string>	unityReferences = new List<string>();

			this.DetectUnityReferences(unityProjectIndex, unityReferences);

			List<string>	exportedFilesLocalVarTrick = exportedFiles;

			Thread	thread = new Thread(() => this.GenerateDLL(profile, unityProjectIndex, isEditor, obfuscate, exportedFilesLocalVarTrick, unityReferences))
			{
				Name = "Generate " + unityProjectIndex
			};
			thread.Start();

			this.runningThreads.Add(thread);
		}

		internal bool	GenerateDLL(Profile profile, int unityProjectIndex, bool isEditor, bool obfuscate, List<string> exportedFiles, List<string> unityReferences)
		{
			if (unityReferences.Count == 0)
			{
				InternalNGDebug.LogError("Project \"" + profile.unityProjectPaths[unityProjectIndex] + "\" might not have its good Unity version installed.");
				return false;
			}

			if (exportedFiles.Count == 0)
			{
				InternalNGDebug.LogWarning("\"" + profile.name + (isEditor == true ? " Editor" : string.Empty) + "\" does not contain any source files.");
				return true;
			}

			string	tempPath = profile.name.Replace('>', '_').Replace(' ', '_') + (isEditor == true ? "Editor" : string.Empty);
			//string	tempPath = @"C:\T\" + profile.name.Replace('>', '_').Replace(' ', '_') + Guid.NewGuid().ToString().Substring(0, 3);
			tempPath = Path.GetTempPath() + tempPath/*.Substring(0, Mathf.Min(tempPath.Length, 7))*/;

			if (Directory.Exists(tempPath) == true)
			{
				Directory.Delete(tempPath, true);
				InternalNGDebug.Log("Sources folder \"" + tempPath + "\" erased.");
			}

			string	unityVersion = Utility.GetUnityVersion(profile.unityProjectPaths[unityProjectIndex]);

			try
			{
				Directory.CreateDirectory(tempPath);

				for (int i = 0; i < exportedFiles.Count; i++)
				{
					string	relativeDestPath = exportedFiles[i].Substring(profile.packagePath.Length + 8).Replace('/', '\\');
					//relativeDestPath = Guid.NewGuid().ToString().Substring(0, 5) + ".cs";
					string	absoluteDestPath = Path.Combine(tempPath, relativeDestPath);
					//string	absoluteDestPath;

					//do
					//{
					//	absoluteDestPath = Path.Combine(tempPath, Guid.NewGuid().ToString().Substring(0, 4) + ".cs");
					//}
					//while (File.Exists(absoluteDestPath) == true);

					Directory.CreateDirectory(Path.GetDirectoryName(absoluteDestPath));

					File.Copy(exportedFiles[i], absoluteDestPath);
				}

				for (int i = 0; i < profile.references.Count; i++)
				{
					if (profile.references[i].localCopy == false)
						continue;

					string	absoluteDestPath;

					if (profile.references[i].localProfile == true)
					{
						Profile	dependentProfile = ProfilesManager.GetProfile(profile.references[i].nameProfile);

						if (dependentProfile == null)
						{
							InternalNGDebug.LogError("Dependency profile \"" + profile.references[i].nameProfile  + "\" is missing.");
							return false;
						}

						string	dependencyDLL = this.GetRealOutputPath(dependentProfile, unityProjectIndex, unityVersion, profile.references[i].editor);

						if (File.Exists(dependencyDLL) == false)
						{
							InternalNGDebug.LogError("Dependency profile \"" + profile.references[i].nameProfile  + "\" is not generated.");
							return false;
						}

						if (profile.references[i].editor == false)
							absoluteDestPath = Path.Combine(profile.unityProjectPaths[unityProjectIndex], Path.Combine(profile.outputPath, Path.GetFileName(dependencyDLL)));
						else
							absoluteDestPath = Path.Combine(profile.unityProjectPaths[unityProjectIndex], Path.Combine(profile.outputEditorPath, Path.GetFileName(dependencyDLL)));

						Directory.CreateDirectory(Path.GetDirectoryName(absoluteDestPath));
						File.Copy(dependencyDLL, absoluteDestPath, true);
					}
					else
					{
						if (File.Exists(profile.references[i].path) == false)
						{
							InternalNGDebug.LogError("Dependency profile \"" + profile.references[i].nameProfile + "\" is missing.");
							return false;
						}

						if (profile.references[i].editor == false)
							absoluteDestPath = Path.Combine(profile.unityProjectPaths[unityProjectIndex], Path.Combine(profile.outputPath, Path.GetFileName(profile.references[i].path)));
						else
							absoluteDestPath = Path.Combine(profile.unityProjectPaths[unityProjectIndex], Path.Combine(profile.outputEditorPath, Path.GetFileName(profile.references[i].path)));

						if (File.Exists(absoluteDestPath) == false)
						{
							Directory.CreateDirectory(Path.GetDirectoryName(absoluteDestPath));
							File.Copy(profile.references[i].path, absoluteDestPath);
						}
					}
				}

				InternalNGDebug.Log("Copy " + profile.name + " sources into \"" + tempPath + "\" completed.");
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException("Copy " + profile.name + " sources into \"" + tempPath + "\" failed.", ex);
				return false;
			}

			string		destinationDLL;
			string[]	references = { };

			destinationDLL = this.GetRealOutputPath(profile, unityProjectIndex, unityVersion, false);
			if (isEditor == true)
			{
				if (profile.unityEngineDLLRequiredForEditor == true)
					references = new string[] { destinationDLL };

				destinationDLL = this.GetRealOutputPath(profile, unityProjectIndex, unityVersion, true);
			}

			if (this.CompileDLL(tempPath, destinationDLL,
								isEditor == false ? profile.defines : profile.defines + ';' + profile.editorDefines + ';' + this.GetUnityVersionPreprocess(unityVersion),
								unityReferences, references,
								profile,
								unityProjectIndex, obfuscate) == false)
			{
				InternalNGDebug.LogError("Compilation of \"" + Path.GetFileNameWithoutExtension(destinationDLL) + "\" failed.");
				return false;
			}
			else
			{
				InternalNGDebug.Log("Compilation of \"" + Path.GetFileNameWithoutExtension(destinationDLL) + "\" completed.");
				if (profile.generateDocumentation == true)
					InternalNGDebug.Log("Generated documentation at \"" + destinationDLL.Replace(".dll", ".xml") + "\".");
				if (profile.generateProgramDatabase == true)
					InternalNGDebug.Log("Generated symbols at \"" + destinationDLL.Replace(".dll", ".pdb") + "\".");
			}

			EditorApplication.delayCall += this.Repaint;

			return true;
		}

		private string	GetUnityVersionPreprocess(string unityVersion)
		{
			string[]	parts = unityVersion.Split('.');

			return "UNITY_" + parts[0] + ";UNITY_" + parts[0] + '_' + parts[1] + ";UNITY_" + parts[0] + '_' + parts[1] + '_' + parts[2][0] + ";UNITY_" + parts[0] + '_' + parts[1] + "_OR_NEWER";
		}

		internal bool	CompileDLL(string sourcePath, string dllDestinationPath, string defines, List<string> unityReferences, string[] references, Profile profile, int unityProjectIndex, bool obfuscate)
		{
			try
			{
				if (File.Exists(dllDestinationPath) == true)
				{
					File.Delete(dllDestinationPath);
					InternalNGDebug.Log("DLL \"" + dllDestinationPath + "\" deleted.");
				}

				Dictionary<string, string>	providerOptions = new Dictionary<string, string>() { { "CompilerVersion", "v3.5" } };
				CSharpCodeProvider			codeDom = new CSharpCodeProvider(providerOptions);
				CompilerParameters			arg = new CompilerParameters()
				{
					OutputAssembly = Path.GetFileName(dllDestinationPath),
					TreatWarningsAsErrors = true,
					GenerateInMemory = false,
					WarningLevel = 0
				};

				string	pluginsPath = Path.GetDirectoryName(dllDestinationPath);
				if (Directory.Exists(pluginsPath) == false)
					Directory.CreateDirectory(pluginsPath);

				string	unityVersion = Utility.GetUnityVersion(profile.unityProjectPaths[unityProjectIndex]);

				if (profile.generateDocumentation == true)
				{
					string	XMLPath = dllDestinationPath.Replace(".dll", ".xml");

					if (File.Exists(XMLPath) == true)
					{
						File.Delete(XMLPath);
						InternalNGDebug.Log("XML \"" + XMLPath + "\" deleted.");
					}

					arg.CompilerOptions += "/doc:\"" + XMLPath + "\" ";
				}

				if (profile.generateProgramDatabase == true)
				{
					string	PDBPath = dllDestinationPath.Replace(".dll", ".pdb");
					if (File.Exists(PDBPath) == true)
					{
						File.Delete(PDBPath);
						InternalNGDebug.Log("PDB \"" + PDBPath + "\" deleted.");
					}

					arg.IncludeDebugInformation = true;
				}

				arg.CompilerOptions += "/filealign:512 /noconfig /define:TRACE;DEBUG;" + this.GetUnityVersionDefines(unityVersion) + ";" + defines;

				for (int i = 0; i < unityReferences.Count; i++)
					arg.ReferencedAssemblies.Add(unityReferences[i]);

				arg.ReferencedAssemblies.AddRange(references);

				for (int i = 0; i < profile.references.Count; i++)
				{
					if (profile.references[i].localProfile == true)
					{
						Profile	dependentProfile = ProfilesManager.GetProfile(profile.references[i].nameProfile);

						if (dependentProfile == null)
						{
							InternalNGDebug.LogError("Compilation reference profile \"" + profile.references[i].nameProfile  + "\" is missing.");
							return false;
						}

						arg.ReferencedAssemblies.Add(this.GetRealOutputPath(dependentProfile, unityProjectIndex, unityVersion, profile.references[i].editor));
					}
					else
						arg.ReferencedAssemblies.Add(profile.references[i].path);
				}

				List<string>	sourceFiles = new List<string>(Directory.GetFiles(sourcePath, "*.cs", SearchOption.AllDirectories));
				CompilerResults	result = codeDom.CompileAssemblyFromFile(arg, sourceFiles.ToArray());

				if (result.Errors.Count == 0)
				{
					if (File.Exists(arg.OutputAssembly) == true)
					{
						File.Move(arg.OutputAssembly, dllDestinationPath);
						if (obfuscate == true)
							return this.ObfuscateDLL(dllDestinationPath, profile, unityProjectIndex, unityReferences, references);
					}
					else
						InternalNGDebug.LogError("Couldn't copy newly generated assembly to \"" + dllDestinationPath + "\".");
					return true;
				}

				InternalNGDebug.LogError("Compilation Exit code: " + result.NativeCompilerReturnValue);
				InternalNGDebug.Log("Output:");
				for (int i = 0; i < result.Output.Count; i++)
					InternalNGDebug.Log(result.Output[i]);
				InternalNGDebug.Log("CompilerOptions: " + arg.CompilerOptions);
				InternalNGDebug.LogWarning("The failure might be due to a compilation error. Start Unity and manually check the errors.");

				return false;
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException(ex);
			}

			return false;
		}

		internal bool	ObfuscateDLL(string dllPath, Profile profile, int unityProjectIndex, List<string> unityReferences, string[] references)
		{
			try
			{
				string	targetDLLName = Path.GetFileNameWithoutExtension(dllPath);
				string	obfuscationFolder = profile.unityProjectPaths[unityProjectIndex] + @"\ConfuserEx\" + Path.GetFileNameWithoutExtension(dllPath);
				string	crprojPath = Path.Combine(obfuscationFolder, targetDLLName + ".crproj");
				string	relativePath = obfuscationFolder + @"\" + targetDLLName + ".dll";
				string	unityVersion = Utility.GetUnityVersion(profile.unityProjectPaths[unityProjectIndex]);

				// Assume files within are valid if already existing.
				if (Directory.Exists(obfuscationFolder) == false)
				{
					Directory.CreateDirectory(obfuscationFolder);
					InternalNGDebug.Log("Folder ConfuserEx at \"" + obfuscationFolder + "\" generated.");
				}

				if (Directory.Exists(obfuscationFolder + @"\Output") == false)
				{
					Directory.CreateDirectory(obfuscationFolder + @"\Output");
					InternalNGDebug.Log("Folder ConfuserEx at \"" + obfuscationFolder + "\"\\Output generated.");
				}

				string	shareReferencesPath = Path.Combine(obfuscationFolder, NGDLLGeneratorWindow.ShareReferencesPath);

				if (Directory.Exists(shareReferencesPath) == false)
				{
					Directory.CreateDirectory(shareReferencesPath);
					InternalNGDebug.Log("Folder ConfuserEx at \"" + shareReferencesPath + "\" generated.");
				}

				for (int i = 0; i < unityReferences.Count; i++)
				{
					string	refPath = Path.Combine(shareReferencesPath, Path.GetFileName(unityReferences[i]));
					if (File.Exists(refPath) == false)
						File.Copy(unityReferences[i], refPath);
				}

				for (int i = 0; i < profile.references.Count; i++)
				{
					if (profile.references[i].localProfile == true)
					{
						Profile	dependentProfile = ProfilesManager.GetProfile(profile.references[i].nameProfile);

						if (dependentProfile == null)
						{
							InternalNGDebug.LogError("Obfuscation dependency profile \"" + profile.references[i].nameProfile  + "\" is missing.");
							return false;
						}

						string	dependencyDLL = this.GetRealOutputPath(dependentProfile, unityProjectIndex, unityVersion, profile.references[i].editor);

						if (File.Exists(dependencyDLL) == false)
						{
							InternalNGDebug.LogError("Obfuscation dependency profile \"" + profile.references[i].nameProfile + "\" is not generated.");
							return false;
						}

						string	referenceTargetPath = Path.Combine(obfuscationFolder, Path.GetFileName(dependencyDLL));
						File.Copy(dependencyDLL, referenceTargetPath, true);
						InternalNGDebug.Log("Copied \"" + dependencyDLL + "\" to \"" + referenceTargetPath + "\".");
					}
					else
					{
						if (File.Exists(profile.references[i].path) == false)
						{
							InternalNGDebug.LogError("Obfuscation dependency profile \"" + profile.references[i].nameProfile + "\" is missing.");
							return false;
						}

						string	referenceTargetPath = Path.Combine(shareReferencesPath, Path.GetFileName(profile.references[i].path));

						if (File.Exists(referenceTargetPath) == false)
						{
							File.Copy(profile.references[i].path, referenceTargetPath);
							InternalNGDebug.Log("Copied \"" + profile.references[i].path + "\" to \"" + referenceTargetPath + "\".");
						}
					}
				}

				for (int i = 0; i < references.Length; i++)
				{
					string	referenceTargetPath = Path.Combine(obfuscationFolder, Path.GetFileName(references[i]));
					File.Copy(references[i], referenceTargetPath, true);
					InternalNGDebug.Log("Copied \"" + references[i] + "\" to \"" + referenceTargetPath + "\".");
				}

				File.Copy(dllPath, relativePath, true);
				InternalNGDebug.Log("DLL \"" + targetDLLName + "\" copied into ConfuserEx.");

				string	CRProjContent = string.Format(NGDLLGeneratorWindow.ConfuserExConfModel,
													  targetDLLName,
													  obfuscationFolder,
													  string.IsNullOrEmpty(profile.obfuscateFilters) == false ? profile.obfuscateFilters : "true");

				File.WriteAllText(crprojPath, CRProjContent);
				InternalNGDebug.Log("ConfuserEx .crproj generated at \"" + crprojPath + "\".");

				Process	process = new Process();
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true; 
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.FileName = profile.confuserExPath;
				process.StartInfo.Arguments = "-n \"" + crprojPath + "\"";

				if (process.Start() == false)
				{
					InternalNGDebug.LogError("Process ConfuserEx stopped with code " + process.ExitCode + ".");
					return false;
				}

				string	stdoutx = process.StandardOutput.ReadToEnd();
				string	stderrx = process.StandardError.ReadToEnd();
				process.WaitForExit();

				if (process.ExitCode != 0)
				{
					InternalNGDebug.LogError("ConfuserEx Exit code: " + process.ExitCode);
					InternalNGDebug.Log("Stdout: " + stdoutx);
					InternalNGDebug.Log("Stderr: " + stderrx);
					InternalNGDebug.Log("Argument: " + process.StartInfo.Arguments);

					return false;
				}

				if (File.Exists(dllPath) == true)
					File.Delete(dllPath);
				File.Copy(obfuscationFolder + @"\Output\" + targetDLLName + ".dll", dllPath, true);

				InternalNGDebug.Log("DLL \"" + targetDLLName + "\" obfuscated.");

				return true;
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException(ex);
			}

			return false;
		}

		internal void	GenerateExportedFiles(Profile profile, bool isEditor, ref List<string> exportedFiles)
		{
			if (exportedFiles != null)
				return;

			exportedFiles = new List<string>(1024);

			for (int i = 0; i < this.assetPaths.Length; i++)
			{
				if (this.IsAssetInPackage(this.assetPaths[i], profile.packagePath) == true && this.assetPaths[i].Contains("/Editor/") == isEditor && this.assetPaths[i].EndsWith(".cs") == true)
				{
					int	j = 0;

					for (; j < profile.forceResourcesKeywords.Count; j++)
					{
						if (this.assetPaths[i].Contains(profile.forceResourcesKeywords[j]) == true)
							break;
					}

					if (j < profile.forceResourcesKeywords.Count)
						continue;

					for (j = 0; j < NGPackageExcluderWindow.DefaultKeywords.Length; j++)
					{
						if (this.assetPaths[i].Contains(NGPackageExcluderWindow.DefaultKeywords[j]) == true)
							break;
					}

					if (j < NGPackageExcluderWindow.DefaultKeywords.Length)
						continue;

					j = 0;

					for (; j < profile.excludeKeywords.Count; j++)
					{
						if (this.assetPaths[i].Contains(profile.excludeKeywords[j]) == true)
							break;
					}

					if (j < profile.excludeKeywords.Count)
						continue;

					exportedFiles.Add(this.assetPaths[i]);
				}
			}

			exportedFiles.Sort();
		}

		//private bool	ExportPackageFromProject(string unityProjectPath)
		//{
		//	try
		//	{
		//		Process	myProcess = new Process();
		//		myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
		//		myProcess.StartInfo.CreateNoWindow = true;
		//		myProcess.StartInfo.UseShellExecute = false;
		//		myProcess.StartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
		//		//myProcess.StartInfo.Arguments = "-nographics -batchmode -quit -executeMethod NGToolsEditor.PackageExporterWindow.ExportPackage -projectPath \"" + this.unitProjectPath + "\"";
		//		myProcess.StartInfo.Arguments = @"-nographics -batchmode -quit -exportPackage ""Assets\NGTools"" ""Assets\Plugins"" ""C:\Users\H1137\Documents\Unity 5\NGToolsDLL5.3.1p1\Package.unitypackage"" -projectPath """ + unityProjectPath + "\"";

		//		if (myProcess.Start() == false)
		//		{
		//			InternalNGDebug.Log("Process Unity stopped with code " + myProcess.ExitCode + ".");
		//			return false;
		//		}

		//		myProcess.WaitForExit();
		//		return myProcess.ExitCode == 0;
		//	}
		//	catch (Exception e)
		//	{
		//		InternalNGDebug.LogException(e);
		//	}
		//	return false;
		//}

		private string	GetUnityVersionDefines(string unityVersion)
		{
			string[]	parts = unityVersion.Split('.');
			Regex		re = new Regex(@"\d+");
			Match		m = re.Match(parts[2]);
			if (m.Success == true)
				return "UNITY_" + parts[0] + ";UNITY_" + parts[0] + "_" + parts[1] + ";UNITY_" + parts[0] + "_" + parts[1] + "_" + m.Value;
			return "UNITY_" + parts[0] + ";UNITY_" + parts[0] + "_" + parts[1] + ";UNITY_" + parts[0] + "_" + parts[1] + "_" + parts[2];
		}

		private void	CopyResourcesOnAllProjects()
		{
			for (int i = 0; i < ProfilesManager.Profile.unityProjectPaths.Count; i++)
				this.CopyResources(ProfilesManager.Profile, i);
		}

		private void	AsyncGenerateDLLOnAllProjects()
		{
			for (int i = 0; i < ProfilesManager.Profile.unityProjectPaths.Count; i++)
				this.AsyncGenerateDLL(ProfilesManager.Profile, i, false, false, ref this.exportedCodeFiles);
		}

		private void	AsyncGenerateDLLEditorOnAllProjects()
		{
			for (int i = 0; i < ProfilesManager.Profile.unityProjectPaths.Count; i++)
			{
				string	unityVersion = Utility.GetUnityVersion(ProfilesManager.Profile.unityProjectPaths[i]);

				if (unityVersion == string.Empty)
					continue;

				if (ProfilesManager.Profile.unityEngineDLLRequiredForEditor == true && File.Exists(this.GetRealOutputPath(ProfilesManager.Profile, i, unityVersion, false)) == true)
					this.AsyncGenerateDLL(ProfilesManager.Profile, i, true, false, ref this.exportedEditorCodeFiles);
				else
					InternalNGDebug.LogWarning("Project \"" + ProfilesManager.Profile.unityProjectPaths[i] + "\" requires DLL for UnityEngine.");
			}
		}

		private void	AsyncObfuscateDLLEditorOnAllProjects()
		{
			for (int i = 0; i < ProfilesManager.Profile.unityProjectPaths.Count; i++)
			{
				string	unityVersion = Utility.GetUnityVersion(ProfilesManager.Profile.unityProjectPaths[i]);

				if (unityVersion == string.Empty)
					continue;

				string	editorDllPath = this.GetRealOutputPath(ProfilesManager.Profile, i, unityVersion, true);

				if (File.Exists(editorDllPath) == false)
					continue;

				this.AsyncObfuscateDLL(ProfilesManager.Profile, i,
									   editorDllPath,
									   ProfilesManager.Profile.unityEngineDLLRequiredForEditor == false ? new string[] { } : new string[] { this.GetRealOutputPath(ProfilesManager.Profile, i, unityVersion, false) });
			}
		}

		private void	AsyncObfuscateDLLOnAllProjects()
		{
			for (int i = 0; i < ProfilesManager.Profile.unityProjectPaths.Count; i++)
			{
				string	unityVersion = Utility.GetUnityVersion(ProfilesManager.Profile.unityProjectPaths[i]);

				if (unityVersion == string.Empty)
					continue;

				string	dllPath = this.GetRealOutputPath(ProfilesManager.Profile, i, unityVersion, false);

				if (File.Exists(dllPath) == true)
					this.AsyncObfuscateDLL(ProfilesManager.Profile, i, dllPath, new string[] { });
			}
		}

		internal string	GetRealOutputPath(Profile profile, int projectIndex, string unityVersion, bool isEditor)
		{
			string	path;

			if (isEditor == false)
			{
				if (string.IsNullOrEmpty(profile.outputPath) == true)
					path = Path.Combine("Assets", profile.packagePath);
				else
					path = profile.outputPath;
			}
			else
			{
				if (string.IsNullOrEmpty(profile.outputEditorPath) == true)
					path = Path.Combine("Assets", Path.Combine(profile.packagePath, "Editor"));
				else
					path = profile.outputEditorPath;
			}

			string	appendVersion = profile.appendVersion ? unityVersion.Substring(0, unityVersion.LastIndexOf(".")) : string.Empty;
			string	dllName = profile.DLLName + (isEditor == true && profile.appendEditor == true ? "Editor" : string.Empty) + appendVersion + ".dll";

			if (isEditor == true && unityVersion.StartsWith("20") == false && unityVersion.CompareTo("5.2.2") < 0)
				return Path.Combine(profile.unityProjectPaths[projectIndex], @"Assets\Plugins\Editor\" + profile.DLLName + @"\" + dllName);
			else
				return Path.Combine(profile.unityProjectPaths[projectIndex], Path.Combine(path, dllName));
		}

		private void	OnSetProfile()
		{
			this.exportedCodeFiles = null;
			this.exportedEditorCodeFiles = null;

			this.list = new ReorderableList(ProfilesManager.Profile.unityProjectPaths, typeof(string), true, true, true, true)
			{
				drawHeaderCallback = r => GUI.Label(r, "Unity DLL Projects"),
				elementHeight = 36F,
				drawElementCallback = this.DrawUnityProject,
				onAddCallback = rl =>
				{
					if (Event.current.button == 1)
					{
						string path = string.Empty;

						if (ProfilesManager.Profile.unityProjectPaths.Count > 0)
							path = ProfilesManager.Profile.unityProjectPaths[ProfilesManager.Profile.unityProjectPaths.Count - 1];

						path = EditorUtility.OpenFolderPanel(NGDLLGeneratorWindow.Title, path, string.Empty);

						if (string.IsNullOrEmpty(path) == false)
						{
							for (int i = 0; i < ProfilesManager.Profiles.Count; i++)
								ProfilesManager.Profiles[i].unityProjectPaths.Add(path);
							ProfilesManager.Save();
						}
					}
					else
					{
						if (ProfilesManager.Profile.unityProjectPaths.Count > 0)
							ProfilesManager.Profile.unityProjectPaths.Add(ProfilesManager.Profile.unityProjectPaths[ProfilesManager.Profile.unityProjectPaths.Count - 1]);
						else
							ProfilesManager.Profile.unityProjectPaths.Add(string.Empty);
						ProfilesManager.Save();
					}
				},
				onRemoveCallback = rl => ProfilesManager.Profile.unityProjectPaths.RemoveAt(rl.index),
				onCanRemoveCallback = r => true
			};
		}
	}
}