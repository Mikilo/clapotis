using NGTools;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;

namespace NGToolsEditor.NGConsole
{
	using UnityEngine;

	[PrewarmEditorWindow]
	[Exportable(ExportableAttribute.ArrayOptions.Overwrite | ExportableAttribute.ArrayOptions.Immutable)]
	public class NGConsoleWindow : EditorWindow, IRows, ISettingExportable, IHasCustomMenu
	{
		private struct RowType
		{
			public Type						type;
			public RowLogHandlerAttribute	attribute;
		}

		public const string	Title = "NG Console";
		public static Color	TitleColor = new Color(1F, 165F / 255F, 0F, 1F); // Orange
		public const int	MaxStreams = 2;
		public const int	MaxFilters = 2;
		public const int	LowestRowGoToLineAllowed = 3;
		public const int	MaxColorMarkers = 4;

		public const int	StartModuleID = 1; // Start ID at 1, because -1 is non-visible and 0 is default(int).

		private static readonly string	FreeAdContent = NGConsoleWindow.Title + " is restrained to:\n" +
											  "• " + MainModule.MaxStreams + " streams.\n" +
											  "• " + GroupFilters.MaxFilters + " filters per stream.\n" +
											  "• " + ColorMarkersWizard.MaxColorMarkers + " color markers.\n" +
											  "• You can not reach a stack frame deeper than " + RowUtility.LowestRowGoToLineAllowed + ".";

		private static IEditorOpener[]	openers;
		public static IEditorOpener[]	Openers
		{
			get
			{
				if (NGConsoleWindow.openers == null)
				{
					List<IEditorOpener>	filteredOpeners = new List<IEditorOpener>(4);

					foreach (Type c in Utility.EachNGTAssignableFrom(typeof(IEditorOpener)))
						filteredOpeners.Add((IEditorOpener)Activator.CreateInstance(c));

					NGConsoleWindow.openers = filteredOpeners.ToArray();
				}

				return NGConsoleWindow.openers;
			}
		}

		private static Type			nativeConsoleType;
		private static FieldInfo	nativeConsoleWindowField;
		private static object		lastUnityConsoleInstance;

		private static RowType[]	rowDrawers;

		public bool	IsReady { get { return this.initialized; } }

		/// <summary>
		/// <para>Called after a new log is added to rows.</para>
		/// <para>This first pass defines if the log is consumed by one or more components.</para>
		/// <para>You should not add any Row through this call.</para>
		/// <para>Parameter int : Index of Row in NGConsole.rows[].</para>
		/// <para>Parameter Row : Log boxed in a Row.</para>
		/// </summary>
		public event Action<int, Row>	CheckNewLogConsume;
		/// <summary>
		/// <para>Called after a new log is added to NGConsole.rows[].</para>
		/// <para>Now we know if the current Row is consumed.</para>
		/// <para>Parameter int : Index of Row in NGConsole.rows[].</para>
		/// <para>Parameter Row : Log boxed with a Row.</para>
		/// </summary>
		public event Action<int, Row>	PropagateNewLog;
		/// <summary>Called when the current working module has changed.</summary>
		public event Action				WorkingModuleChanged;
		/// <summary>Called after Clear is called.</summary>
		public event Action				ConsoleCleared;
		/// <summary>Called whenever an option has changed.</summary>
		public event Action				OptionAltered;
		/// <summary>Called on Update.</summary>
		public event Action				UpdateTick;
		/// <summary>Called right after header bar has been drawn.</summary>
		public DynamicFunc<Rect>		PostOnGUIHeader;
		/// <summary>Called after all GUI has been drawn.</summary>
		public event Action				PostOnGUI;

		// GUI Events
		/// <summary>Add GUI after header's left menu. (Clear, Collapse...)</summary>
		public event Action	AfterGUIHeaderLeftMenu;
		/// <summary>Add GUI before header's right menu.</summary>
		public DynamicFunc<Rect>	BeforeGUIHeaderRightMenu;
		/// <summary>Add GUI after header's right menu.</summary>
		public DynamicFunc<Rect>	AfterGUIHeaderRightMenu;

		/// <summary>ID of the current visible module the console is displaying.</summary>
		public int	workingModuleId = -1;

		internal List<Row>	rows;
		internal SyncLogs	syncLogs;

		internal bool		initialized;
		private NGSettings	settings;

		private Module[]	visibleModules;
		[Exportable(ExportableAttribute.ArrayOptions.Immutable)]
		private Module[]	modules;

		private bool	collapse;
		private bool	clearOnPlay;
		private bool	breakOnError;

		private Rect	r;
		private bool	hasCompiled;
		private float	autoPaddingRightHeaderMenu = 0F;

		private ErrorPopup	errorPopup = new ErrorPopup(NGConsoleWindow.Title, "An error occurred, try to reopen " + NGConsoleWindow.Title + " or reset the settings.");

		static	NGConsoleWindow()
		{
			NGConsoleWindow.nativeConsoleType = UnityAssemblyVerifier.TryGetType(typeof(EditorWindow).Assembly, "UnityEditor.ConsoleWindow");
			if (NGConsoleWindow.nativeConsoleType != null)
			{
				NGConsoleWindow.nativeConsoleWindowField = UnityAssemblyVerifier.TryGetField(NGConsoleWindow.nativeConsoleType, "ms_ConsoleWindow", BindingFlags.NonPublic | BindingFlags.Static);

				if (NGConsoleWindow.nativeConsoleWindowField != null)
					EditorApplication.update += NGConsoleWindow.AutoReplaceNativeConsole;
			}
		}

		[MenuItem(Constants.MenuItemPath + NGConsoleWindow.Title, priority = Constants.MenuItemPriority + 101), Hotkey(NGConsoleWindow.Title)]
		public static void	Open()
		{
			Utility.OpenWindow<NGConsoleWindow>(false, NGConsoleWindow.Title, true);
		}

		[Hotkey(NGConsoleWindow.Title + " Clear")]
		public static void	ClearNGConsole()
		{
			int	count = 0;

			foreach (NGConsoleWindow window in Utility.EachEditorWindows(typeof(NGConsoleWindow)))
			{
				window.Clear();
				++count;
			}

			if (count == 0)
				UnityLogEntries.Clear();
		}

		private static void	AutoReplaceNativeConsole()
		{
			if (HQ.Settings == null || HQ.Settings.Get<GeneralSettings>().autoReplaceUnityConsole == false)
				return;

			object	value = NGConsoleWindow.nativeConsoleWindowField.GetValue(null);

			if (value != null && value != NGConsoleWindow.lastUnityConsoleInstance)
			{
				NGConsoleWindow[]	consoles = Resources.FindObjectsOfTypeAll<NGConsoleWindow>();

				if (consoles.Length == 0)
				{
					EditorWindow[]	nativeConsoles = Resources.FindObjectsOfTypeAll(NGConsoleWindow.nativeConsoleType) as EditorWindow[];

					if (nativeConsoles.Length > 0)
					{
						NGConsoleWindow	window = EditorWindow.GetWindow<NGConsoleWindow>(NGConsoleWindow.Title, true, nativeConsoleType);
						window.titleContent.text = NGConsoleWindow.Title.Substring(3);
						Utility.RestoreIcon(window, NGConsoleWindow.TitleColor);
						nativeConsoles[0].Close();
					}
				}
			}

			NGConsoleWindow.lastUnityConsoleInstance = value;
		}

		[NGSettingsChanged]
		private static void	OnSettingsGenerated(ScriptableObject asset)
		{
			CustomHotkeysSettings	hotkeys = asset as CustomHotkeysSettings;

			if (hotkeys != null)
				hotkeys.hotkeys.Add(new CustomHotkeysSettings.MethodHotkey() { bind = "%W", staticMethod = typeof(NGConsoleWindow).FullName + ".ClearNGConsole" });
		}

		protected virtual void	OnEnable()
		{
			Utility.RegisterWindow(this);
			Utility.RestoreIcon(this, NGConsoleWindow.TitleColor);

			Metrics.UseTool(2); // NGConsole

			if (this.initialized == true || HQ.Settings == null)
				return;

			NGChangeLogWindow.CheckLatestVersion(NGTools.NGConsole.NGAssemblyInfo.Name);

			try
			{
				//Debug.Log("StartEnable");
				int	i = 0;

				PerWindowVars.InitWindow(this, "NGConsole");

				this.syncLogs = new SyncLogs(this);
				this.syncLogs.EndNewLog += this.RepaintWithModules;
				this.syncLogs.UpdateLog += this.UpdateLog;
				this.syncLogs.NewLog += this.ConvertNewLog;
				this.syncLogs.ResetLog += this.LocalResetLogs;
				this.syncLogs.ClearLog += this.Clear;
				this.syncLogs.OptionAltered += this.UpdateConsoleFlags;

				this.rows = new List<Row>(ConsoleConstants.PreAllocatedArray);

				this.r = new Rect();

				List<RowType>	rowDrawerTypes = new List<RowType>();

				foreach (Type c in Utility.EachNGTSubClassesOf(typeof(Row)))
				{
					object[]	attributes = c.GetCustomAttributes(typeof(RowLogHandlerAttribute), false);

					if (attributes.Length == 0)
						continue;

					MethodInfo	handler = c.GetMethod(RowLogHandlerAttribute.StaticVerifierMethodName, BindingFlags.Static | BindingFlags.NonPublic);

					if (handler == null)
					{
						InternalNGDebug.LogWarning("The class \"" + c + "\" inherits from \"" + typeof(Row) + "\" and has the attribute \"" + typeof(RowLogHandlerAttribute) + "\" must implement: private static bool " + RowLogHandlerAttribute.StaticVerifierMethodName + "(UnityLogEntry log).");
						continue;
					}

					RowType	rdt = new RowType() {
						type = c,
						attribute = attributes[0] as RowLogHandlerAttribute
					};

					rdt.attribute.handler = (Func<UnityLogEntry, bool>)Delegate.CreateDelegate(typeof(Func<UnityLogEntry, bool>), handler);

					rowDrawerTypes.Add(rdt);
				}

				rowDrawerTypes.Sort((r1, r2) => r2.attribute.priority - r1.attribute.priority);
				NGConsoleWindow.rowDrawers = rowDrawerTypes.ToArray();

				List<Module>	filteredModules = new List<Module>();

				if (HQ.Settings.Get<ConsoleSettings>().serializedModules.Count > 0)
					this.modules = HQ.Settings.Get<ConsoleSettings>().serializedModules.Deserialize<Module>();

				if (this.modules == null)
				{
					foreach (Type t in Utility.EachNGTSubClassesOf(typeof(Module)))
						filteredModules.Add((Module)Activator.CreateInstance(t));
				}
				else
				{
					filteredModules.AddRange(this.modules);

					// Detect new Module.
					foreach (Type t in Utility.EachNGTSubClassesOf(typeof(Module), c => filteredModules.Exists(m => m.GetType() == c) == false))
					{
						InternalNGDebug.VerboseLogFormat("Module \"{0}\" generated.", t);
						filteredModules.Add((Module)Activator.CreateInstance(t));
					}
				}

				this.modules = filteredModules.ToArray();
				this.visibleModules = this.GetVisibleModules(filteredModules);

				// Initialize modules
				int	id = NGConsoleWindow.StartModuleID;

				for (i = 0; i < this.modules.Length; i++)
				{
					if (this.visibleModules.Contains(this.modules[i]) == true)
						this.modules[i].OnEnable(this, id++);
					else
						this.modules[i].OnEnable(this, -1);
				}

				// Do not overflow if there is removed modules.
				if (this.visibleModules.Length > 0)
				{
					if (this.workingModuleId == -1)
						this.workingModuleId = this.visibleModules[0].Id;
					else
						this.workingModuleId = Mathf.Clamp(this.workingModuleId, NGConsoleWindow.StartModuleID, this.visibleModules.Length);

					Module	module = this.GetModule(this.workingModuleId);
					if (module != null)
						module.OnEnter();
				}
				else
					this.workingModuleId = -1;

				GUI.FocusControl(null);

				Object[]	nativeConsoleInstances = Resources.FindObjectsOfTypeAll(NGConsoleWindow.nativeConsoleType);

				if (nativeConsoleInstances.Length > 0)
					NGConsoleWindow.nativeConsoleWindowField.SetValue(null, nativeConsoleInstances[nativeConsoleInstances.Length - 1]);

				this.settings = HQ.Settings;

				HQ.SettingsChanged += this.OnSettingsChanged;
				Undo.undoRedoPerformed += this.Repaint;

				EditorApplication.delayCall += () => GUICallbackWindow.Open(this.VerifySettingsStyles);

				this.initialized = true;
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException(ex);
			}
		}

		protected virtual void	OnDisable()
		{
			Utility.UnregisterWindow(this);

			if (this.initialized == false)
				return;

			this.initialized = false;

			NGConsoleWindow.nativeConsoleWindowField.SetValue(null, null);

			if (this.syncLogs != null)
			{
				this.syncLogs.EndNewLog -= this.RepaintWithModules;
				this.syncLogs.UpdateLog -= this.UpdateLog;
				this.syncLogs.NewLog -= this.ConvertNewLog;
				this.syncLogs.ResetLog -= this.LocalResetLogs;
				this.syncLogs.ClearLog -= this.Clear;
				this.syncLogs.OptionAltered -= this.UpdateConsoleFlags;
			}

			if (this.modules != null)
			{
				try
				{
					Module	module = this.GetModule(this.workingModuleId);
					if (module != null)
						module.OnLeave();

					for (int i = 0; i < this.modules.Length; i++)
						this.modules[i].OnDisable();
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException(LC.G("Console_IssueEncountered"), ex);
					if (Conf.DebugMode == Conf.DebugState.None)
					{
						InternalNGDebug.LogError("NG Console has aborted its uninitialization and has closed for safety. Relaunch NG Console and contact the author using the Contact form through Preferences.");
						this.Close();
					}
				}

				this.modules = null;
			}

			HQ.SettingsChanged -= this.OnSettingsChanged;
			Undo.undoRedoPerformed -= this.Repaint;

			this.settings = null;
		}

		protected virtual void	OnGUI()
		{
			if (this.initialized == false)
			{
				GUILayout.Label(string.Format(LC.G("RequiringConfigurationFile"), NGConsoleWindow.Title));
				if (GUILayout.Button(LC.G("ShowPreferencesWindow")) == true)
					Utility.ShowPreferencesWindowAt(Constants.PreferenceTitle);
				return;
			}

			FreeLicenseOverlay.First(this, NGTools.NGConsole.NGAssemblyInfo.Name + " Pro", NGConsoleWindow.FreeAdContent);

			RowUtility.drawingWindow = this;

			r.x = 0;
			r.y = 0;
			r.width = this.position.width;
			r.height = this.position.height;

			GeneralSettings	settings = HQ.Settings.Get<GeneralSettings>();

			if (settings.consoleBackground.a > 0F)
				EditorGUI.DrawRect(r, settings.consoleBackground);

			r.height = settings.menuHeight;
			r = this.DrawHeader(r);

			if (this.PostOnGUIHeader != null)
			{
				r.x = 0F;
				r.width = this.position.width;
				r.height = settings.menuHeight;

				try
				{
					r = this.PostOnGUIHeader.Invoke(r);
				}
				catch (Exception ex)
				{
					this.errorPopup.exception = ex;
				}
			}

			if (this.workingModuleId >= NGConsoleWindow.StartModuleID)
			{
				try
				{
					r.x = 0F;
					r.width = this.position.width;
					r.height = this.position.height - r.y;
					this.GetModule(this.workingModuleId).OnGUI(r);
				}
				catch (ExitGUIException)
				{
				}
				catch (Exception ex)
				{
					this.errorPopup.exception = ex;
				}
			}

			try
			{
				if (this.PostOnGUI != null)
					this.PostOnGUI();
			}
			catch (Exception ex)
			{
				this.errorPopup.exception = ex;
			}

			FreeLicenseOverlay.Last(NGTools.NGConsole.NGAssemblyInfo.Name + " Pro");
		}

		protected virtual void	Update()
		{
			if (this.initialized == false)
			{
				this.OnDisable();
				this.OnEnable();
				return;
			}

			try
			{
				if (EditorApplication.isCompiling == true)
					this.hasCompiled = true;
				else if (this.hasCompiled == true)
				{
					this.hasCompiled = false;

					// Reset all internal logs and resync' them all. Not well optimized, but it fits well for now.
					this.LocalResetLogs();
					this.syncLogs.LocalClear();
					this.syncLogs.Sync();
				}
				else
					this.syncLogs.Sync();
			}
			catch (Exception ex)
			{
				this.errorPopup.exception = ex;
			}

			if (this.UpdateTick != null)
				this.UpdateTick();
		}

		public void	SaveModules()
		{
			this.SaveModules(false);
		}

		public void	SaveModules(bool directSave)
		{
			this.settings.Get<ConsoleSettings>().serializedModules.Serialize(this.modules);
			HQ.InvalidateSettings(this.settings, directSave);
		}

		/// <summary>
		/// Sets the focus on the module associated with the given <paramref name="id"/>.
		/// </summary>
		/// <param name="id"></param>
		public void		SetModule(int id)
		{
			this.GetModule(this.workingModuleId).OnLeave();
			this.workingModuleId = id;
			this.autoPaddingRightHeaderMenu = 0F;
			this.GetModule(this.workingModuleId).OnEnter();
			if (this.WorkingModuleChanged != null)
				this.WorkingModuleChanged();
		}

		/// <summary>
		/// Gets a module by its name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Module	GetModule(string name)
		{
			foreach (var t in this.modules)
			{
				if (t.name == name)
					return t;
			}
			return null;
		}

		/// <summary>
		/// Gets a module by its ID.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Module	GetModule(int id)
		{
			foreach (var t in this.modules)
			{
				if (t.Id == id)
					return t;
			}
			return null;
		}

		public void	Clear()
		{
			if (this.initialized == false)
				return;

			for (int i = 0; i < this.rows.Count; i++)
				this.rows[i].Uninit();
			this.rows.Clear();

			this.syncLogs.Clear();

			if (this.ConsoleCleared != null)
				this.ConsoleCleared();

			this.RepaintWithModules();
		}

		private void	UpdateLog(int consoleIndex, UnityLogEntry unityLog)
		{
			if (consoleIndex < this.rows.Count)
			{
				// Just update the bare necessities.
				this.rows[consoleIndex].log.collapseCount = unityLog.collapseCount;
			}
		}

		private void	ConvertNewLog(int consoleIndex, UnityLogEntry unityLog)
		{
			LogSettings	settings = HQ.Settings.Get<LogSettings>();

			for (int j = 0; j < NGConsoleWindow.rowDrawers.Length; j++)
			{
				if ((bool)NGConsoleWindow.rowDrawers[j].attribute.handler(unityLog) == true)
				{
					Row			row = Activator.CreateInstance(NGConsoleWindow.rowDrawers[j].type) as Row;
					LogEntry	log = new LogEntry();

					log.Set(unityLog);
					log.frameCount = Time.frameCount;
					log.renderedFrameCount = Time.renderedFrameCount;

					if (settings.displayTime == true)
					{
						try
						{
							log.time = DateTime.Now.ToString(settings.timeFormat);
						}
						catch
						{
							log.time = "00:00:00";
						}
					}

					row.Init(this, log);

					if (rows.Count <= consoleIndex)
						rows.Add(row);
					else
					{
						InternalNGDebug.LogFile(rows.Count + " < " + consoleIndex);
						rows[consoleIndex] = row;
					}

					if (this.CheckNewLogConsume != null)
						this.CheckNewLogConsume(consoleIndex, row);
					if (this.PropagateNewLog != null)
						this.PropagateNewLog(consoleIndex, row);

					return;
				}
			}

			InternalNGDebug.LogFile("No Row can handle this log:" + unityLog);
		}

		/// <summary>
		/// Displays the top menus.
		/// </summary>
		/// <param name="r"></param>
		/// <returns></returns>
		private Rect	DrawHeader(Rect r)
		{
			GeneralSettings	settings = HQ.Settings.Get<GeneralSettings>();
			float			width = r.width;

			// Draw Unity native features. This way is better because using style Toolbar in BeginHorizontal creates an unwanted left margin.
			GUI.Box(r, GUIContent.none, settings.ToolbarStyle);

			if (string.IsNullOrEmpty(settings.clearLabel) == false)
			{
				Utility.content.text = settings.clearLabel;
				Utility.content.tooltip = Utility.content.text != "Clear" ? "Clear" : string.Empty;
				r.width = settings.MenuButtonStyle.CalcSize(Utility.content).x;
				if (GUI.Button(r, Utility.content, settings.MenuButtonStyle) == true)
					this.Clear();
				r.x += r.width + 5F;
			}

			EditorGUI.BeginChangeCheck();

			ConsoleFlags	flags = (ConsoleFlags)UnityLogEntries.consoleFlags;

			if (string.IsNullOrEmpty(settings.collapseLabel) == false)
			{
				Utility.content.text = settings.collapseLabel;
				Utility.content.tooltip = Utility.content.text != "Collapse" ? "Collapse" : string.Empty;
				r.width = settings.MenuButtonStyle.CalcSize(Utility.content).x;
				this.collapse = GUI.Toggle(r, (flags & ConsoleFlags.Collapse) != 0, Utility.content, settings.MenuButtonStyle);
				r.x += r.width;
			}

			if (string.IsNullOrEmpty(settings.clearOnPlayLabel) == false)
			{
				Utility.content.text = settings.clearOnPlayLabel;
				Utility.content.tooltip = Utility.content.text != "Clear on Play" ? "Clear on Play" : string.Empty;
				r.width = settings.MenuButtonStyle.CalcSize(Utility.content).x;
				this.clearOnPlay = GUI.Toggle(r, (flags & ConsoleFlags.ClearOnPlay) != 0, Utility.content, settings.MenuButtonStyle);
				r.x += r.width;
			}

			if (string.IsNullOrEmpty(settings.errorPauseLabel) == false)
			{
				Utility.content.text = settings.errorPauseLabel;
				Utility.content.tooltip = Utility.content.text != "Error Pause" ? "Error Pause" : string.Empty;
				r.width = settings.MenuButtonStyle.CalcSize(Utility.content).x;
				this.breakOnError = GUI.Toggle(r, (flags & ConsoleFlags.ErrorPause) != 0, Utility.content, settings.MenuButtonStyle);
				r.x += r.width;
			}

			Utility.content.tooltip = string.Empty;

			if (EditorGUI.EndChangeCheck() == true)
			{
				UnityLogEntries.SetConsoleFlag((int)ConsoleFlags.Collapse, this.collapse);
				UnityLogEntries.SetConsoleFlag((int)ConsoleFlags.ClearOnPlay, this.clearOnPlay);
				UnityLogEntries.SetConsoleFlag((int)ConsoleFlags.ErrorPause, this.breakOnError);

				if (this.OptionAltered != null)
					this.OptionAltered();

				Utility.RepaintConsoleWindow();
			}

			try
			{
				if (this.AfterGUIHeaderLeftMenu != null)
					this.AfterGUIHeaderLeftMenu();
			}
			catch (Exception ex)
			{
				this.errorPopup.exception = ex;
			}

			r.x += 5F;

			// Draw tabs menu.
			if (this.visibleModules.Length > 1)
			{
				for (int i = 0; i < this.visibleModules.Length; i++)
					r = this.visibleModules[i].DrawMenu(r, this.workingModuleId);
			}

			r.x += 5F;
			r.width = width - r.x;
			r.xMax += autoPaddingRightHeaderMenu;

			try
			{
				if (this.AfterGUIHeaderRightMenu != null)
					r = this.AfterGUIHeaderRightMenu.Invoke(r);
			}
			catch (Exception ex)
			{
				this.errorPopup.exception = ex;
			}

			// Display right menus.
			try
			{
				if (this.BeforeGUIHeaderRightMenu != null)
					r = this.BeforeGUIHeaderRightMenu.Invoke(r);
			}
			catch (Exception ex)
			{
				this.errorPopup.exception = ex;
			}

			if (r.width < 0F)
				autoPaddingRightHeaderMenu += -r.width;
			else if (r.width > 0F)
			{
				if (autoPaddingRightHeaderMenu > 0F)
				{
					this.Repaint();
					autoPaddingRightHeaderMenu -= 1F;
					if (autoPaddingRightHeaderMenu < 0F)
						autoPaddingRightHeaderMenu = 0F;
				}
			}

			r.y += settings.menuHeight;

			if (this.errorPopup.exception != null)
			{
				r.x = 0F;
				r.width = width;
				r.height = this.errorPopup.boxHeight;
				this.errorPopup.OnGUIRect(r);

				r.y += r.height;
			}

			return r;
		}

		private void	LocalResetLogs()
		{
			for (int i = 0; i < this.rows.Count; i++)
				this.rows[i].Uninit();
			this.rows.Clear();

			if (this.ConsoleCleared != null)
				this.ConsoleCleared();
			this.Repaint();
		}

		private void	RepaintWithModules()
		{
			this.Repaint();
			Utility.RepaintEditorWindow(typeof(ModuleWindow));
		}

		private void	UpdateConsoleFlags()
		{
			if (this.OptionAltered != null)
				this.OptionAltered();
		}

		/// <summary>
		/// Modifies the given list to fetch only the visible modules.
		/// </summary>
		/// <param name="modules"></param>
		/// <returns></returns>
		private Module[]	GetVisibleModules(List<Module> modules)
		{
			for (int i = 0; i < modules.Count; i++)
			{
				if (modules[i].GetType().IsDefined(typeof(VisibleModuleAttribute), false) == false)
				{
					modules.RemoveAt(i);
					--i;
				}
			}

			modules.Sort(this.SortModules);

			return modules.ToArray();
		}

		private int	SortModules(Module a, Module b)
		{
			VisibleModuleAttribute	aAttribute = a.GetType().GetCustomAttributes(typeof(VisibleModuleAttribute), false)[0] as VisibleModuleAttribute;
			VisibleModuleAttribute	bAttribute = b.GetType().GetCustomAttributes(typeof(VisibleModuleAttribute), false)[0] as VisibleModuleAttribute;
			return aAttribute.position - bAttribute.position;
		}

		private void	OnSettingsChanged()
		{
			if (this.initialized == true)
			{
				this.OnDisable();
				this.OnEnable();
				this.Repaint();
			}
		}

		private void	VerifySettingsStyles()
		{
			GeneralSettings		generalSettings = this.settings.Get<GeneralSettings>();
			LogSettings			logSettings = this.settings.Get<LogSettings>();
			StackTraceSettings	stackTraceSettings = this.settings.Get<StackTraceSettings>();
			int					totalNull = 0;

			if (generalSettings.MenuButtonStyle.normal.background == null)
				++totalNull;
			if (generalSettings.MenuButtonStyle.active.background == null)
				++totalNull;
			if (generalSettings.MenuButtonStyle.onNormal.background == null)
				++totalNull;
			if (generalSettings.MenuButtonStyle.onActive.background == null)
				++totalNull;
			if (generalSettings.MenuButtonStyle.font == null)
				++totalNull;

			if (generalSettings.ToolbarStyle.normal.background == null)
				++totalNull;
			if (generalSettings.ToolbarStyle.font == null)
				++totalNull;

			if (logSettings.Style.font == null)
				++totalNull;
			if (logSettings.TimeStyle.font == null)
				++totalNull;
			if (logSettings.CollapseLabelStyle.normal == null)
				++totalNull;
			if (logSettings.ContentStyle.font == null)
				++totalNull;

			if (stackTraceSettings.Style.font == null)
				++totalNull;
			if (stackTraceSettings.PreviewSourceCodeStyle.font == null)
				++totalNull;

			if (totalNull >= 10) // This number is based on nothing, well hakuna matata...
			{
				if (EditorGUIUtility.isProSkin == true)
					new DarkTheme().SetTheme(this.settings);
				else
					new LightTheme().SetTheme(this.settings);

				InternalNGDebug.LogError(NGConsoleWindow.Title + " has detected a potential corruption of GUIStyles. Recovered by applying the " + (EditorGUIUtility.isProSkin == true ? "Dark theme" : "Light theme") + ".");
			}
		}

		Row	IRows.GetRow(int consoleIndex)
		{
			if (consoleIndex >= this.rows.Count)
			{
				this.errorPopup.exception = new Exception("Overflow " + consoleIndex + " < " + this.rows.Count);
				InternalNGDebug.LogFile("Overflow " + consoleIndex + " < " + this.rows.Count);
			}
			return this.rows[consoleIndex];
		}

		int	IRows.CountRows()
		{
			return this.rows.Count;
		}

		void	IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			Utility.AddNGMenuItems(menu, this, NGConsoleWindow.Title, NGTools.NGConsole.NGAssemblyInfo.WikiURL);

			if (Application.platform == RuntimePlatform.OSXEditor)
				menu.AddItem(new GUIContent("Open Player Log"), false, new GenericMenu.MenuFunction(InternalEditorUtility.OpenPlayerConsole));
			menu.AddItem(new GUIContent("Open Editor Log"), false, new GenericMenu.MenuFunction(InternalEditorUtility.OpenEditorConsole));

			Array	stackTraceLogTypeValues = Enum.GetValues(typeof(StackTraceLogType));
			Array	logTypeValues = Enum.GetValues(typeof(UnityEngine.LogType));

			try
			{
				for (int i = 0; i < logTypeValues.Length; i++)
				{
					UnityEngine.LogType	logType = (UnityEngine.LogType)((int)logTypeValues.GetValue(i));

					for (int j = 0; j < stackTraceLogTypeValues.Length; j++)
					{
						//while (enumerator.MoveNext())
						StackTraceLogType	stackTraceLogType = (StackTraceLogType)((int)stackTraceLogTypeValues.GetValue(j));
						menu.AddItem(new GUIContent("Stack Trace Logging/" + logType + "/" + stackTraceLogType), Application.GetStackTraceLogType(logType) == stackTraceLogType,
						() => {
							Application.SetStackTraceLogType(logType, stackTraceLogType);
						});
					}
				}
			}
			finally
			{
				IDisposable	disposable = stackTraceLogTypeValues as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}
		}

		#region Export Settings
		void	ISettingExportable.PreExport()
		{
		}

		void	ISettingExportable.PreImport()
		{
			this.OnDisable();
		}

		void	ISettingExportable.PostImport()
		{
			this.OnEnable();
		}
		#endregion
	}
}