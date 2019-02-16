using NGTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;

namespace NGToolsEditor.NGMissingScriptRecovery
{
	using UnityEngine;

	public class NGMissingScriptRecoveryWindow : EditorWindow, IHasCustomMenu
	{
		public enum RecoveryMode
		{
			Automatic,
			Manual
		}

		public enum Tab
		{
			Selection,
			Project,
			Recovery
		}

		public const string	NormalTitle = "NG Missing Script Recovery";
		public const string	ShortTitle = "NG Missing Scri";
		public static Color	TitleColor = new Color(255F / 255F, 192F / 255F, 203F / 255F, 1F); // Pink
		public const string	TempPrefabPath = "Assets/MissingComponentRecovery.prefab";
		public const float	MaxFieldsHeight = 100F;

		public GameObject	Target { get { return this.target; } }
		public bool			IsRecovering { get { return this.isRecovering; } }

		private event Action	PostDiagnostic;

		[SerializeField]
		internal Tab			tab = Tab.Selection;
		[SerializeField]
		private bool			openAutoRecovery;
		[SerializeField]
		private RecoveryMode	recoveryMode = RecoveryMode.Automatic;
		[SerializeField]
		private bool			useCache = true;
		[SerializeField]
		private string			recoveryLogFilePath = string.Empty;
		[SerializeField]
		private bool			supaFast = false;
		[SerializeField]
		private bool			promptOnPause = true;

		[NonSerialized]
		private GameObject			selectionTarget;
		[NonSerialized]
		private GameObject			target;
		[NonSerialized]
		private List<RawComponent>	rawComponents = new List<RawComponent>();
		private Vector2				scrollPosition;
		private Vector2				scrollPositionResult;

		private List<CachedLineFix>	cachedComponentFixes = new List<CachedLineFix>();

		private List<string>	componentIDs = new List<string>();
		private Stack<int>		gameObjects = new Stack<int>();

		[NonSerialized]
		private bool					hasResult = false;
		[NonSerialized]
		private List<MissingGameObject>	missings = new List<MissingGameObject>();
		[NonSerialized]
		private int						selectedMissing = -1;

		[NonSerialized]
		private int		currentGameObject;
		[NonSerialized]
		private bool	isRecovering;
		[NonSerialized]
		private bool	isPausing;
		[NonSerialized]
		private bool	isGUIRendered;
		[NonSerialized]
		private bool	skipFix;
		[NonSerialized]
		private Vector2	scrollPositionRecovery;

		[NonSerialized]
		private GUIContent	promptOnPauseContent = new GUIContent("Prompt On Pause", "When a case is requiring the user intervention, pop a prompt.");
		[NonSerialized]
		private GUIContent	useCacheContent = new GUIContent("Use Cache", "Each fix is cached. The process uses the cache to automatically solve future missing scripts.");
		[NonSerialized]
		private GUIContent	supaFastContent = new GUIContent("Supa Fast", "No feedback provided, Unity is unusable during the process.");

		[MenuItem(Constants.MenuItemPath + NGMissingScriptRecoveryWindow.NormalTitle, priority = Constants.MenuItemPriority + 345), Hotkey(NGMissingScriptRecoveryWindow.NormalTitle)]
		public static void	Open()
		{
			Utility.OpenWindow<NGMissingScriptRecoveryWindow>(NGMissingScriptRecoveryWindow.ShortTitle);
		}

		[MenuItem("Window/Test")]
		private static void	Test(MenuCommand command)
		{
			MetadataDatabase.Initialize();
		}

		[MenuItem("CONTEXT/Component/Recover Missing Script")]
		private static void	Diagnose(MenuCommand command)
		{
			// We assume that the context menu is opened from the active GameObject.
			PrefabType	prefabType = PrefabUtility.GetPrefabType(Selection.activeGameObject);
			Object		prefab = null;

			if (prefabType == PrefabType.Prefab)
				prefab = Selection.activeGameObject;
			else if (prefabType == PrefabType.PrefabInstance)
				prefab = PrefabUtility.GetPrefabParent(Selection.activeGameObject);
			else if (prefabType == PrefabType.DisconnectedPrefabInstance)
				prefab = PrefabUtility.GetPrefabParent(Selection.activeGameObject);

			if (prefab == null)
			{
				EditorUtility.DisplayDialog(NGMissingScriptRecoveryWindow.NormalTitle, "Recover a missing script is only possible on prefab.\n\nYou may temporary create a prefab, recover and then destroy the prefab.", "OK");
				return;
			}

			NGMissingScriptRecoveryWindow	instance = EditorWindow.GetWindow<NGMissingScriptRecoveryWindow>(true, NGMissingScriptRecoveryWindow.ShortTitle);

			if (instance.isRecovering == false)
			{
				instance.Diagnose(Selection.activeGameObject);
				instance.tab = Tab.Selection;
				instance.Show();
			}
			else
				EditorUtility.DisplayDialog(NGMissingScriptRecoveryWindow.NormalTitle, "A recovery process is still running.", "OK");
		}

		[MenuItem("CONTEXT/Component/Recover Missing Script", true)]
		private static bool	ValidateDiagnose(MenuCommand command)
		{
			if (command.context != null)
			{
				Component	c = command.context as Component;

				if (c != null)
				{
					Component[]	components = c.GetComponents<Component>();

					for (int i = 0; i < components.Length; i++)
					{
						if (components[i] == null)
							return true;
					}
				}
			}
			else
			{
				if (Selection.activeGameObject != null)
				{
					Component[]	components = Selection.activeGameObject.GetComponents<Component>();

					for (int i = 0; i < components.Length; i++)
					{
						if (components[i] == null)
							return true;
					}
				}
			}

			return false;
		}

		protected virtual void	OnEnable()
		{
			Metrics.UseTool(14); // NGMissingScriptRecovery

			NGChangeLogWindow.CheckLatestVersion(NGAssemblyInfo.Name);

			this.wantsMouseMove = true;

			if (this.tab == Tab.Recovery)
				this.tab = Tab.Selection;

			Selection.selectionChanged += this.Repaint;

			Utility.LoadEditorPref(this);
			Utility.RestoreIcon(this, NGMissingScriptRecoveryWindow.TitleColor);
		}

		protected virtual void	OnDisable()
		{
			Selection.selectionChanged -= this.Repaint;
			Utility.SaveEditorPref(this);
		}

		protected virtual void	OnGUI()
		{
			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				EditorGUI.BeginDisabledGroup(this.isRecovering);
				EditorGUI.BeginChangeCheck();
				GUILayout.Toggle(this.tab == Tab.Selection, "Selection", GeneralStyles.ToolbarToggle);
				if (EditorGUI.EndChangeCheck() == true)
					this.tab = Tab.Selection;

				EditorGUI.BeginChangeCheck();
				GUILayout.Toggle(this.tab == Tab.Project, "Project", GeneralStyles.ToolbarToggle);
				if (EditorGUI.EndChangeCheck() == true)
					this.tab = Tab.Project;
				EditorGUI.EndDisabledGroup();

				if (this.isRecovering == true)
				{
					EditorGUI.BeginChangeCheck();
					GUILayout.Toggle(this.tab == Tab.Recovery, "Recovery", GeneralStyles.ToolbarToggle);
					if (EditorGUI.EndChangeCheck() == true)
						this.tab = Tab.Recovery;
				}
			}
			EditorGUILayout.EndHorizontal();

			if (EditorSettings.serializationMode != SerializationMode.ForceText)
			{
				EditorGUILayout.HelpBox(NGMissingScriptRecoveryWindow.NormalTitle + " requires asset serialization mode to be set on ForceText to recover from plain text file.", MessageType.Info);

				try
				{
					EditorGUI.BeginChangeCheck();
					SerializationMode mode = (SerializationMode)EditorGUILayout.EnumPopup("Serialization Mode", EditorSettings.serializationMode);
					if (EditorGUI.EndChangeCheck() == true)
						EditorSettings.serializationMode = mode;
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
				}

				return;
			}

			if (this.tab == Tab.Selection)
				this.DrawSelection();
			else if (this.tab == Tab.Project)
				this.DrawProject();
			else if (this.tab == Tab.Recovery)
				this.DrawRecovery();

			this.isGUIRendered = true;
		}

		protected virtual void	Update()
		{
			if (EditorApplication.isCompiling == true)
			{
				if (this.isRecovering == true)
				{
					InternalNGDebug.Log("Recovery aborted due to compilation.");
					this.isRecovering = false;
				}
			}
		}

		public void		Diagnose(GameObject	gameObject)
		{
			this.rawComponents.Clear();

			PrefabType	prefabType = PrefabUtility.GetPrefabType(gameObject);

			if (prefabType == PrefabType.Prefab)
				this.target = gameObject;
			else if (prefabType == PrefabType.PrefabInstance || prefabType == PrefabType.DisconnectedPrefabInstance)
				this.target = PrefabUtility.GetPrefabParent(gameObject) as GameObject;
			else
				this.target = null;

			// Look into a prefab.
			if (this.target != null)
			{
				Component[]		components = this.target.GetComponents<Component>();
				string			prefabPath = AssetDatabase.GetAssetPath(this.target);
				string[]		lines = File.ReadAllLines(prefabPath);
				List<string>	matchFields = new List<string>();

				this.ExtractComponentsIDs(lines, this.target, prefabPath);

				for (int k = 0; k < this.componentIDs.Count; k++)
				{
					RawComponent	rawComponent = new RawComponent(this.componentIDs[k]);

					this.rawComponents.Add(rawComponent);

					for (int i = 0; i < lines.Length; i++)
					{
						if (lines[i].StartsWith("---") == true && lines[i].EndsWith(this.componentIDs[k]) == true)
						{
							++i;
							if (components != null && components[k] != null)
							{
								rawComponent.asset = components[k];
								rawComponent.name = rawComponent.asset.GetType().Name;
								break;
							}
							else
								rawComponent.name = lines[i].Remove(lines[i].Length - 1);

							if (lines[i] == "MonoBehaviour:")
							{
								bool	inFields = false;

								++i;
								for (; i < lines.Length; i++)
								{
									if (lines[i].StartsWith("  m_Script") == true)
										rawComponent.line = lines[i];
									else if (inFields == true && lines[i].StartsWith("---") == true)
										break;
									else if (lines[i].StartsWith("  m_EditorClassIdentifier") == true)
										inFields = true;
									else if (inFields == true)
									{
										if (lines[i][3] != ' ' && lines[i][3] != '-')
											rawComponent.fields.Add(lines[i].Substring(2, lines[i].IndexOf(':') - 2));
									}
								}
							}

							break;
						}
					}

					if (rawComponent.fields.Count == 0)
						continue;

					foreach (Type type in Utility.EachAllSubClassesOf(typeof(MonoBehaviour)))
					{
						if (type.IsAbstract == true || type.IsGenericType == true)
							continue;

						matchFields.Clear();

						for (int l = 0; l < rawComponent.fields.Count; l++)
						{
							if (type.GetField(rawComponent.fields[l], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null)
								matchFields.Add(rawComponent.fields[l]);
						}

						if (matchFields.Count > 0)
						{
							FieldInfo[]	fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
							int			l = 0;
							int			matchingFieldsCount = matchFields.Count;

							matchFields.Clear();

							for (int i = 0; i < fields.Length; i++)
							{
								if ((fields[i].IsPublic == false && fields[i].IsDefined(typeof(SerializeField), true) == false) ||
									fields[i].IsDefined(typeof(NonSerializedAttribute), true) == true ||
									NGTools.Utility.CanExposeTypeInInspector(fields[i].FieldType) == false)
								{
									continue;
								}

								matchFields.Add(fields[i].Name);
							}

							for (; l < rawComponent.potentialTypes.Count; l++)
							{
								if (rawComponent.potentialTypes[l].matchingFields < matchingFieldsCount ||
									(rawComponent.potentialTypes[l].matchingFields == matchingFieldsCount &&
									 (rawComponent.potentialTypes[l].fields.Length == rawComponent.potentialTypes[l].matchingFields ? rawComponent.potentialTypes[l].matchingFields - rawComponent.fields.Count : rawComponent.potentialTypes[l].fields.Length - rawComponent.potentialTypes[l].matchingFields) >
									 (matchFields.Count == matchingFieldsCount ? matchingFieldsCount - rawComponent.fields.Count : matchFields.Count - matchingFieldsCount)))
								{
									rawComponent.potentialTypes.Insert(l, new PotentialType(type, matchingFieldsCount, matchFields.ToArray()));
									break;
								}
							}

							if (l >= rawComponent.potentialTypes.Count)
								rawComponent.potentialTypes.Add(new PotentialType(type, matchingFieldsCount, matchFields.ToArray()));
						}
					}
				}
			}
			// Look into a scene.
			else
			{
				// TODO Implement looking into scene.
			}

			if (this.PostDiagnostic != null)
				this.PostDiagnostic();
		}

		internal void	AddCachedComponentFixes(CachedLineFix line)
		{
			if (this.useCache == true)
				this.cachedComponentFixes.Add(line);
		}

		internal CachedLineFix	FindCachedComponentFixes(string line)
		{
			if (this.useCache == true)
			{
				for (int i = 0; i < this.cachedComponentFixes.Count; i++)
				{
					if (this.cachedComponentFixes[i].brokenLine == line)
						return this.cachedComponentFixes[i];
				}
			}

			return null;
		}

		private void	DrawSelection()
		{
			if (Selection.activeGameObject != this.selectionTarget)
			{
				this.selectionTarget = Selection.activeGameObject;

				if (Selection.activeGameObject != null)
					this.Diagnose(Selection.activeGameObject);
				else
					this.target = null;
			}

			if (this.target == null)
			{
				if (this.selectionTarget == null)
					GUILayout.Label("Select a prefab (or an instance of it) to diagnose", GeneralStyles.BigCenterText, GUILayoutOptionPool.ExpandHeightTrue);
				else
					GUILayout.Label("No diagnostic available", GeneralStyles.BigCenterText, GUILayoutOptionPool.ExpandHeightTrue);
				return;
			}

			if (this.selectedMissing >= 0)
			{
				EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
				{
					EditorGUI.BeginDisabledGroup(this.selectedMissing <= 0);
					{
						if (GUILayout.Button("<<", GeneralStyles.ToolbarButton, GUILayoutOptionPool.MaxWidth(100F)) == true)
						{	
							--this.selectedMissing;
							this.Diagnose(this.missings[this.selectedMissing].gameObject);
						}
					}
					EditorGUI.EndDisabledGroup();

					NGEditorGUILayout.PingObject(this.missings[this.selectedMissing].path, this.missings[this.selectedMissing].gameObject, GeneralStyles.ToolbarButton);

					EditorGUI.BeginDisabledGroup(this.selectedMissing >= this.missings.Count - 1);
					{
						if (GUILayout.Button(">>", GeneralStyles.ToolbarButton, GUILayoutOptionPool.MaxWidth(100F)) == true)
						{
							++this.selectedMissing;
							this.Diagnose(this.missings[this.selectedMissing].gameObject);
						}
					}
					EditorGUI.EndDisabledGroup();
				}
				EditorGUILayout.EndHorizontal();
			}
			else
			{
				Utility.content.text = this.target.name;
				Utility.content.image = Utility.GetIcon(this.target.GetInstanceID());
				EditorGUILayout.LabelField(Utility.content);
				Utility.content.image = null;
			}

			this.scrollPosition = EditorGUILayout.BeginScrollView(this.scrollPosition);
			{
				for (int i = 0; i < this.rawComponents.Count; i++)
					this.rawComponents[i].Draw(this);
			}
			EditorGUILayout.EndScrollView();
		}

		private void	DrawProject()
		{
			GUILayout.Space(5F);

			EditorGUILayout.BeginVertical("ButtonLeft");
			{
				this.openAutoRecovery = EditorGUILayout.Foldout(this.openAutoRecovery, "Recovery Settings (" + Enum.GetName(typeof(RecoveryMode), this.recoveryMode) + ")");

				if (this.openAutoRecovery == true)
				{
					this.recoveryMode = (RecoveryMode)EditorGUILayout.EnumPopup("Recovery Mode", this.recoveryMode);

					if (this.recoveryMode == RecoveryMode.Automatic)
					{
						this.promptOnPause = EditorGUILayout.Toggle(this.promptOnPauseContent, this.promptOnPause);
						this.useCache = EditorGUILayout.Toggle(this.useCacheContent, this.useCache);
						this.supaFast = EditorGUILayout.Toggle(supaFastContent, this.supaFast);
						this.recoveryLogFilePath = NGEditorGUILayout.SaveFileField("Recovery Log File", this.recoveryLogFilePath);
					}

					GUILayout.Space(5F);
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			{
				using (BgColorContentRestorer.Get(GeneralStyles.HighlightActionButton))
				{
					if (GUILayout.Button("Scan", GeneralStyles.BigButton) == true)
					{
						string[]	assets = AssetDatabase.GetAllAssetPaths();

						this.hasResult = true;
						this.selectedMissing = -1;
						this.missings.Clear();

						try
						{
							for (int i = 0; i < assets.Length; i++)
							{
								if (assets[i].EndsWith(".prefab") == false)
									continue;

								if (EditorUtility.DisplayCancelableProgressBar(NGMissingScriptRecoveryWindow.NormalTitle, "Scanning " + assets[i], (float)i / (float)assets.Length) == true)
									break;

								Object[]	content = AssetDatabase.LoadAllAssetsAtPath(assets[i]);

								for (int j = 0; j < content.Length; j++)
								{
									GameObject	go = content[j] as GameObject;

									if (go != null)
									{
										Component[]	components = go.GetComponents<Component>();

										for (int k = 0; k < components.Length; k++)
										{
											if (components[k] == null)
											{
												this.missings.Add(new MissingGameObject(go));
												break;
											}
										}
									}
								}
							}
						}
						finally
						{
							EditorUtility.ClearProgressBar();
						}

						return;
					}
				}

				GUILayout.FlexibleSpace();

				if (this.cachedComponentFixes.Count > 0)
				{
					if (GUILayout.Button("Clear Recovery Cache (" + this.cachedComponentFixes.Count + " elements)", GeneralStyles.BigButton) == true)
						this.cachedComponentFixes.Clear();
				}
			}
			EditorGUILayout.EndHorizontal();

			if (this.hasResult == true)
			{
				EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
				{
					GUILayout.Label("Result");

					GUILayout.FlexibleSpace();

					if (GUILayout.Button("X", GeneralStyles.ToolbarCloseButton) == true)
						this.hasResult = false;
				}
				EditorGUILayout.EndHorizontal();

				if (this.missings.Count == 0)
				{
					GUILayout.Label("No missing script found.");
				}
				else
				{
					this.scrollPositionResult = EditorGUILayout.BeginScrollView(this.scrollPositionResult);
					{
						for (int i = 0; i < this.missings.Count; i++)
						{
							EditorGUILayout.BeginHorizontal();
							{
								NGEditorGUILayout.PingObject(this.missings[i].path, this.missings[i].gameObject, GeneralStyles.LeftButton);

								if (GUILayout.Button("Fix", GUILayoutOptionPool.Width(75F)) == true)
								{
									this.selectedMissing = i;
									this.tab = 0;
									this.Diagnose(this.missings[i].gameObject);
								}
							}
							EditorGUILayout.EndHorizontal();
						}
					}
					EditorGUILayout.EndScrollView();

					GUILayout.FlexibleSpace();

					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.FlexibleSpace();

						if (this.recoveryMode == RecoveryMode.Automatic)
							EditorGUILayout.HelpBox("Backup your project before\ndoing any automatic recovery.", MessageType.Warning);

						using (BgColorContentRestorer.Get(GeneralStyles.HighlightResultButton))
						{
							if (GUILayout.Button("Start Recovery", GeneralStyles.BigButton) == true)
								Utility.StartBackgroundTask(this.RecoveryTask());
						}
					}
					EditorGUILayout.EndHorizontal();
				}
			}
		}

		private void	DrawRecovery()
		{
			if (this.currentGameObject >= this.missings.Count)
			{
				EditorGUILayout.HelpBox("Recovery finished.", MessageType.Info);
				return;
			}

			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				if (GUILayout.Button((this.currentGameObject + 1) + " / " + this.missings.Count + " ☰", "GV Gizmo DropDown", GUILayoutOptionPool.ExpandWidthFalse) == true)
				{
					GenericMenu	menu = new GenericMenu();

					for (int i = 0; i < this.missings.Count; i++)
						menu.AddItem(new GUIContent(this.missings[i].path), false, (n) => this.currentGameObject = (int)n, i);

					menu.ShowAsContext();
				}

				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Stop", GeneralStyles.ToolbarButton, GUILayoutOptionPool.Width(100F)) == true)
				{
					this.isRecovering = false;
					this.tab = Tab.Project;
					return;
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				EditorGUI.BeginDisabledGroup(this.currentGameObject <= 0);
				{
					if (GUILayout.Button("<<", GeneralStyles.ToolbarButton, GUILayoutOptionPool.Width(50F)) == true)
					{	
						--this.currentGameObject;
						this.isPausing = false;
						this.skipFix = true;
					}
				}
				EditorGUI.EndDisabledGroup();

				NGEditorGUILayout.PingObject(this.missings[this.currentGameObject].path, this.missings[this.currentGameObject].gameObject, GeneralStyles.ToolbarButton);

				EditorGUI.BeginDisabledGroup(this.currentGameObject >= this.missings.Count - 1);
				{
					if (GUILayout.Button(">>", GeneralStyles.ToolbarButton, GUILayoutOptionPool.Width(50F)) == true)
					{
						++this.currentGameObject;
						this.isPausing = false;
						this.skipFix = true;
					}

					if (GUILayout.Button("Skip", GeneralStyles.ToolbarButton, GUILayoutOptionPool.Width(100F)) == true)
					{
						++this.currentGameObject;
						this.isPausing = false;
					}
				}
				EditorGUI.EndDisabledGroup();
			}
			EditorGUILayout.EndHorizontal();

			this.PostDiagnostic += this.OnPostDiagnosticManualRecovery;
			this.scrollPositionRecovery = EditorGUILayout.BeginScrollView(this.scrollPositionRecovery);
			{
				for (int i = 0; i < this.rawComponents.Count; i++)
					this.rawComponents[i].Draw(this);
			}
			EditorGUILayout.EndScrollView();
			this.PostDiagnostic -= this.OnPostDiagnosticManualRecovery;
		}

		private IEnumerator	RecoveryTask()
		{
			Selection.activeObject = null;

			this.currentGameObject = 0;
			this.isRecovering = true;
			this.tab = Tab.Recovery;

			StringBuilder	buffer = null;
			DateTime		start = DateTime.Now;

			if (string.IsNullOrEmpty(this.recoveryLogFilePath) == false)
			{
				buffer = Utility.GetBuffer();
				InternalNGDebug.Log("Started recovery at " + start.ToLongTimeString() + ".");
				buffer.AppendLine("Started recovery at " + start.ToLongTimeString() + ".");
			}

			this.PostDiagnostic += this.OnPostDiagnosticAutomaticRecovery;

			while (this.currentGameObject < this.missings.Count && this.isRecovering == true)
			{
				this.Diagnose(this.missings[this.currentGameObject].gameObject);

				if (this.recoveryMode == RecoveryMode.Automatic && this.skipFix == false)
				{
					EditorUtility.DisplayProgressBar(NGMissingScriptRecoveryWindow.NormalTitle + " (" + this.currentGameObject + " / " + this.missings.Count + ")", "Recovering " + this.missings[this.currentGameObject].path, (float)this.currentGameObject / (float)this.missings.Count);

					InternalNGDebug.Log("Recovering GameObject " + this.missings[this.currentGameObject].path + ".");
					if (buffer != null)
						buffer.AppendLine("Recovering GameObject " + this.missings[this.currentGameObject].path + ".");

					for (int i = 0; i < this.rawComponents.Count && this.isRecovering == true; ++i)
					{
						if (this.rawComponents[i].fields.Count == 0)
							continue;

						if (this.useCache == true)
						{
							// Fix from cache.
							CachedLineFix	lineFix = this.cachedComponentFixes.Find(c => c.brokenLine == this.rawComponents[i].line);
							if (lineFix != null)
							{
								this.rawComponents[i].FixLine(this, this.rawComponents[i].componentID, lineFix.type);
								this.Repaint();

								InternalNGDebug.Log("Recovered Component \"" + this.rawComponents[i].name + "\" (" + i + ") from cache (Type \"" + lineFix.type + "\").");
								if (buffer != null)
									buffer.AppendLine("Recovered Component \"" + this.rawComponents[i].name + "\" (" + i + ") from cache (Type \"" + lineFix.type + "\").");

								continue;
							}
						}

						int		perfectMatches = 0;
						Type	matchType = null;

						for (int j = 0; j < this.rawComponents[i].potentialTypes.Count; j++)
						{
							if (this.rawComponents[i].potentialTypes[j].matchingFields == this.rawComponents[i].fields.Count &&
								this.rawComponents[i].fields.Count == this.rawComponents[i].potentialTypes[j].fields.Length)
							{
								++perfectMatches;
								matchType = this.rawComponents[i].potentialTypes[j].type;
							}
						}

						if (perfectMatches == 1)
						{
							this.rawComponents[i].FixMissingComponent(this, this.rawComponents[i].componentID, matchType);

							InternalNGDebug.Log("Recovered Component \"" + this.rawComponents[i].name + "\" (" + i + ") with Type \"" + matchType.FullName + "\".");
							if (buffer != null)
								buffer.AppendLine("Recovered Component \"" + this.rawComponents[i].name + "\" (" + i + ") with Type \"" + matchType.FullName + "\".");
						}
						else if (this.rawComponents[i].potentialTypes.Count == 0)
						{
							InternalNGDebug.Log("Component \"" + this.rawComponents[i].name + "\" (" + i + ") has no potential type.");
							if (buffer != null)
								buffer.AppendLine("Component \"" + this.rawComponents[i].name + "\" (" + i + ") has no potential type.");
						}
						else // Ask the user to fix it.
						{
							if (this.promptOnPause == true)
							{
								if (EditorUtility.DisplayDialog(NGMissingScriptRecoveryWindow.NormalTitle, "Recovery requires your attention.", "OK", "Skip alert for next cases") == false)
									this.promptOnPause = false;
							}

							this.Diagnose(this.missings[this.currentGameObject].gameObject);
							this.isPausing = true;
							EditorUtility.ClearProgressBar();

							InternalNGDebug.Log("Paused on Component \"" + this.rawComponents[i].name + "\" (" + i + ").");
							if (buffer != null)
								buffer.AppendLine("Paused on Component \"" + this.rawComponents[i].name + "\" (" + i + ").");

							break;
						}
					}

					if (this.isPausing == false)
						this.currentGameObject++;
				}
				else
					this.isPausing = true;

				this.skipFix = false;
				this.isGUIRendered = false;

				while (this.isPausing == this.isRecovering)
					yield return null;

				while (this.supaFast == false && this.isGUIRendered == false)
				{
					this.Repaint();
					yield return null;
				}
			}

			this.PostDiagnostic -= this.OnPostDiagnosticAutomaticRecovery;

			EditorUtility.ClearProgressBar();

			DateTime	end = DateTime.Now;
			TimeSpan	duration = end - start;

			InternalNGDebug.Log("Ended recovery at " + end.ToLongTimeString() + " (" + duration.TotalSeconds + "s).");

			if (buffer != null)
			{
				buffer.AppendLine("Ended recovery at " + end.ToLongTimeString() + " (" + duration.TotalSeconds + "s).");
				buffer.AppendLine();
				File.AppendAllText(this.recoveryLogFilePath, Utility.ReturnBuffer(buffer));
				InternalNGDebug.Log("Recovery log saved at \"" + this.recoveryLogFilePath + "\".");
			}

			AssetDatabase.Refresh();

			this.target = null;
			this.hasResult = false;

			this.Repaint();

			this.isRecovering = false;
		}

		private void	OnPostDiagnosticManualRecovery()
		{
			if (this.IsRecoverable() == false)
			{
				this.currentGameObject++;
				if (this.currentGameObject < this.missings.Count)
					this.Diagnose(this.missings[this.currentGameObject].gameObject);
				else
					this.isRecovering = false;
			}
		}

		private void	OnPostDiagnosticAutomaticRecovery()
		{
			this.isPausing = false;
		}

		private void	ExtractComponentsIDs(string[] lines, GameObject target, string path)
		{
			string	lineName = "  m_Name: " + target.name;
			int		countSameName = 0;
			int		lastSameName = 0;

			this.componentIDs.Clear();
			this.gameObjects.Clear();

			// Look for GameObject sharing the same name.
			for (int i = 0; i < lines.Length; i++)
			{
				if (lines[i].StartsWith("GameObject:") == true)
					this.gameObjects.Push(i);
				else if (lines[i] == lineName)
				{
					++countSameName;
					lastSameName = i;
				}
			}

			// If no duplicate, then we got it!
			if (countSameName == 1)
			{
				for (; lastSameName >= 0; --lastSameName)
				{
					if (lines[lastSameName].StartsWith("GameObject:") == true)
						break;
				}

				for (int i = lastSameName + 1; i < lines.Length; ++i)
				{
					if (lines[i].StartsWith("  m_Component:") == true)
					{
						// Finally, extract the Component.
						for (i += 1; i < lines.Length; i++)
						{
							if (lines[i].StartsWith("  -") == false)
								return;

							string	id = lines[i].Substring(lines[i].IndexOf("fileID: ") + 8);
							this.componentIDs.Add(id.Remove(id.Length - 1));
						}

						return;
					}
				}
			}
			else if (countSameName > 1)
			{
				string	realName = target.name;
				string	tempName = "__RECOVERY_TOKEN__" + DateTime.Now.Ticks.ToString();
				target.name = tempName;
				EditorUtility.SetDirty(target);
				AssetDatabase.SaveAssets();

				lines = File.ReadAllLines(path);
				this.ExtractComponentsIDs(lines, target, path);

				target.name = realName;
				EditorUtility.SetDirty(target);
				AssetDatabase.SaveAssets();
				return;
			}

			// Look for the root GameObject.
			for (int i = 0; i < lines.Length; i++)
			{
				// Detect the root line.
				if (lines[i].StartsWith("  m_Father: {fileID: 0}") == true)
				{
					// Seek upward for Transform ID
					for (; i >= 1; --i)
					{
						if (lines[i].EndsWith("Transform:") == true)
						{
							// Restart with ID.
							--i;

							int	ampersand = lines[i].IndexOf('&');

							if (ampersand == -1)
								return;

							string	id = ' ' + lines[i].Substring(ampersand + 1) + '}';

							for (i = 0; i < lines.Length; i++)
							{
								// Find the line.
								if (lines[i].EndsWith(id) == true)
								{
									// Move upward to ensure we get all the Component.
									for (; i >= 0; --i)
									{
										if (lines[i].StartsWith("  m_Component:") == true)
										{
											// Finally, extract the Component.
											for (i += 1; i < lines.Length; i++)
											{
												if (lines[i].StartsWith("  -") == false)
													return;

												id = lines[i].Substring(lines[i].IndexOf("fileID: ") + 8);
												this.componentIDs.Add(id.Remove(id.Length - 1));
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		private bool	IsRecoverable()
		{
			for (int i = 0; i < this.rawComponents.Count; i++)
			{
				if (this.rawComponents[i].fields.Count > 0)
					return true;
			}

			return false;
		}

		void	IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			Utility.AddNGMenuItems(menu, this, NGMissingScriptRecoveryWindow.NormalTitle, NGAssemblyInfo.WikiURL);
		}
	}
}