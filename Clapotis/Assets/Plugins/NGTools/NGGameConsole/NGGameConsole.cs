using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NGTools.NGGameConsole
{
	using UnityEngine;

	public class NGGameConsole : MonoBehaviour
	{
		public class GroupDataConsole : List<DataConsole>
		{
			public bool	freezeUpdate = false;

			public bool	HasUpdate { get; private set; }

			public void	Init()
			{
				this.HasUpdate = false;

				for (int i = 0; i < this.Count; i++)
				{
					if (this[i].HasUpdateData() == true)
					{
						this.HasUpdate = true;
						break;
					}
				}
			}
		}

		public enum Tab
		{
			Logs,
			//CommandLine,
			Data,
			Settings,
			Save
		}

		public enum TerminalMode
		{
			RawText,
			Log
		}

		public const string	Title = "NG Game Console";
		public const string	SendDataKeyPref = "NGGameConsole_SendData";
		public const char	SendDataSeparator = ((char)3);
		public const int	MinUniqueKeyLength = 5;
		public const float	LogTypeSquareWidth = 5F;
		public const string	WindowStylesGroup = "<color=green>[Window Styles]</color>";
		public const string	LogModeGroup = "<color=green>[Log Mode]</color>";
		public const string	RawTextModeGroup = "<color=green>[Raw Text Mode]</color>";
		public const int	MaxChars = (UInt16.MaxValue >> 2) - 1;
		public const int	MaxSendLogSize = 1000 * 1000 * 3; // 3 Mo
		public const string	DefaultServerLog = "https://unityapi.ngtools.tech/nggameconsole.php?k=";

		private static List<NGGameConsole>	instances = new List<NGGameConsole>();

		public Action<bool>	GameConsoleEnableChanged;

		[Header("Destroy the game console in production build.")]
		public bool	autoDestroyInProduction = true;
		[Header("Keep game console alive between scenes.")]
		public bool	dontDestroyOnLoad = true;
		[Header("Show the game console on Awake.")]
		public bool	visibleOnAwake = true;

		private Tab			displayTab = Tab.Logs;
		public Tab			tab = Tab.Logs;
		public TerminalMode	terminalMode = TerminalMode.RawText;

		public DataConsole[]	dataConsole;

		[Header("Allow to drag the console.")]
		public bool			movable = true;
		[Header("Allow to resize the console.")]
		public bool			resizable = true;
		//public Texture2D	resizeCursor;
		public Vector2		minWindowSize = new Vector2(100F, 100F);
		public Rect			windowSize = new Rect(0F, 0F, 400F, 400F);
		public float		windowDragAreaHeight = 20F;
		public Texture2D	windowBackground;

		[Group("Log", true)]
		public bool		unused;
		[Group("Log"), GHeader("Name format of log file in the persistent data path. Is parsed by .NET DateTime.")]
		public string	logFilenameFormat = @"\l\o\g\s_HHmmss.\t\x\t";
		[Group("Log")]
		[GHeader("URL to the server which will receive a HTTP request with data as POST.")]
		[GHeader("Leave it empty to use NG Tools server. (" + NGGameConsole.DefaultServerLog + ")")]
		[GHeader("Don't send too many bigs logs to NG Tools server please, I pay for it ;)")]
		public string	serverLog = string.Empty;
		//[Group("Log")]
		[Header("Unique identifier of minimum 5 characters, use it to manage your logs."), Header("Required if you use NG Tools server.")]
		public string	uniqueKey = string.Empty;

		[Group(NGGameConsole.LogModeGroup, true)]
		public bool		unused2;
		[Group(NGGameConsole.RawTextModeGroup)]
		public GUIStyle	rawLogsBackgroundStyle;
		[Group(NGGameConsole.RawTextModeGroup)]
		public bool		appendStackTrace = true;
		[Group(NGGameConsole.LogModeGroup), GHeader("Maximum logs displayed by the console.")]
		public int		maxLogs = 1000;
		[Group(NGGameConsole.LogModeGroup), GHeader("Format of the time used by DateTime. Let empty to avoid time.")]
		public string	timeFormat = "HH:mm:ss.fff";
		[Group(NGGameConsole.LogModeGroup)]
		public bool		displayTime = true;
		[Group(NGGameConsole.LogModeGroup)]
		public bool		displayNormalLog = true;
		[Group(NGGameConsole.LogModeGroup)]
		public bool		displayWarningLog = true;
		[Group(NGGameConsole.LogModeGroup)]
		public bool		displayErrorLog = true;
		[Group(NGGameConsole.LogModeGroup)]
		public GUIContent	collapseButton;
		[Group(NGGameConsole.LogModeGroup)]
		public GUIContent	normalLogButton;
		[Group(NGGameConsole.LogModeGroup)]
		public GUIContent	warningLogButton;
		[Group(NGGameConsole.LogModeGroup)]
		public GUIContent	errorLogButton;
		[Group(NGGameConsole.LogModeGroup)]
		public GUIStyle		timeStyle;
		[Group(NGGameConsole.LogModeGroup)]
		public GUIStyle		logStyle;

		[Group(NGGameConsole.LogModeGroup)]
		public Texture2D	evenBackgroundLog;
		[Group(NGGameConsole.LogModeGroup)]
		public Texture2D	oddBackgroundLog;

		[Group(NGGameConsole.WindowStylesGroup)]
		public GUIStyle	titleStyle;
		[Group(NGGameConsole.WindowStylesGroup)]
		public GUIStyle	tabButtonStyle;
		[Group(NGGameConsole.WindowStylesGroup)]
		public GUIStyle	closeButtonStyle;
		[Group(NGGameConsole.WindowStylesGroup)]
		public GUIStyle	dataConsoleGroupStyle;
		[Group(NGGameConsole.WindowStylesGroup)]
		public GUIStyle	dataConsoleShortStyle;
		[Group(NGGameConsole.WindowStylesGroup)]
		public GUIStyle	dataConsoleFullStyle;
		[Group(NGGameConsole.WindowStylesGroup)]
		public GUIStyle	dataConsoleSubTitleStyle;

		private int			normalLogsCount;
		private int			warningLogsCount;
		private int			errorLogsCount;
		private List<GameLog>	logs;
		private Vector2		logScrollPosition;
		private Vector2		shortDataScrollPosition;
		private Vector2		fullDataScrollPosition;

		private LogType	unityDisplayLogType;
		private bool	autoLogScroll;

		private bool	resizing = false;
		private Vector2	startDragPosition;
		private Rect	initialWindowArea;

		private Dictionary<string, GroupDataConsole>	groupedData;

		private string[]	modeNames;

		private GUIStyle	windowStyle;
		private GUIStyle	evenRowScrollView;
		private GUIStyle	oddRowScrollView;
		private GUIStyle	dragStyle;

		private GUIStyle	normalLogStyle;
		private GUIStyle	warningLogStyle;
		private GUIStyle	errorLogStyle;

		private GUIStyle	resizeStyle;

		private Rect	windowDragArea;
		private Rect	titleRect;
		private Rect	saveRect;
		private Rect	tabRect;
		private Rect	settingsButtonRect;
		private Rect	closeButtonRect;
		private Rect	copyAllRect;

		private int	lastDataConsoleHashCode;

		private float	reservedHeadSpace;
		private float	reservedFootSpace;

		private GUIContent	collapseContent = new GUIContent();
		private string			logsText = string.Empty;
		private StringBuilder	logsBuffer = new StringBuilder(1 << 16);

		private bool	isScrollStickingBottom;

		private string	requestFeedback = string.Empty;

		private Dictionary<string, List<Action>>	settings = new Dictionary<string, List<Action>>();

		protected virtual void	Reset()
		{
			if (this.timeStyle == null)
				this.timeStyle = new GUIStyle();
			this.timeStyle.fixedWidth = 80;
			if (this.logStyle == null)
				this.logStyle = new GUIStyle();
			this.logStyle.richText = true;
		}

		private void	DelayRestore()
		{
			this.enabled = this.visibleOnAwake;
		}

		protected virtual void	Awake()
		{
			if (Debug.isDebugBuild == false && this.autoDestroyInProduction == true)
			{
				Object.DestroyImmediate(this.gameObject);
				return;
			}

			PlayerPrefs.SetInt("NGT_UsedTools", 1 << 31);
			this.enabled = false;

			if (this.enabled != visibleOnAwake)
				this.Invoke("DelayRestore", 1F);

			if (this.logs != null)
				return;

			for (int i = 0; i < NGGameConsole.instances.Count; i++)
			{
				if (NGGameConsole.instances[i].GetType() == this.GetType())
				{
					Object.Destroy(this.gameObject);
					return;
				}
			}

			this.logs = new List<GameLog>(this.maxLogs);

			if (this.displayNormalLog == true)
				this.unityDisplayLogType |= (LogType)(1 << (int)LogType.Log);
			if (this.displayWarningLog == true)
				this.unityDisplayLogType |= (LogType)(1 << (int)LogType.Warning);
			if (this.displayErrorLog == true)
				this.unityDisplayLogType |= (LogType)(1 << (int)LogType.Assert | 1 << (int)LogType.Error | 1 << (int)LogType.Exception);

			this.groupedData = new Dictionary<string, GroupDataConsole>();

			this.modeNames = Enum.GetNames(typeof(Tab));

			this.windowSize.x = PlayerPrefs.GetFloat("NGGameConsole.x", this.windowSize.x);
			this.windowSize.y = PlayerPrefs.GetFloat("NGGameConsole.y", this.windowSize.y);
			this.windowSize.width = PlayerPrefs.GetFloat("NGGameConsole.w", this.windowSize.width);
			this.windowSize.height = PlayerPrefs.GetFloat("NGGameConsole.h", this.windowSize.height);

			this.windowDragArea = new Rect(0F, 0F, this.windowSize.width, this.windowDragAreaHeight);

			this.normalLogStyle = new GUIStyle();
			this.normalLogStyle.alignment = TextAnchor.MiddleCenter;
			this.normalLogStyle.normal.background = new Texture2D(1, 1);
			this.normalLogStyle.normal.background.SetPixel(0, 0, Color.white);
			this.normalLogStyle.normal.background.Apply();
			this.warningLogStyle = new GUIStyle();
			this.warningLogStyle.alignment = TextAnchor.MiddleCenter;
			this.warningLogStyle.normal.background = new Texture2D(1, 1);
			this.warningLogStyle.normal.background.SetPixel(0, 0, Color.yellow);
			this.warningLogStyle.normal.background.Apply();
			this.errorLogStyle = new GUIStyle();
			this.errorLogStyle.alignment = TextAnchor.MiddleCenter;
			this.errorLogStyle.normal.background = new Texture2D(1, 1);
			this.errorLogStyle.normal.background.SetPixel(0, 0, Color.red);
			this.errorLogStyle.normal.background.Apply();

			this.titleRect = new Rect(10F, 0F, 150F, this.windowDragArea.height);
			this.tabRect = new Rect(160F, 0F, 100F, this.windowDragArea.height);
			this.saveRect = new Rect(270F, 0F, 100F, this.windowDragArea.height);
			this.settingsButtonRect = new Rect(this.windowSize.width - this.windowDragArea.height - this.windowDragArea.height, 0F, this.windowDragArea.height, this.windowDragArea.height);
			this.closeButtonRect = new Rect(this.windowSize.width - this.windowDragArea.height, 0F, this.windowDragArea.height, this.windowDragArea.height);
			this.copyAllRect = new Rect(5F, 0F, 100F, 16F);

			if (this.dontDestroyOnLoad == true)
			{
				NGGameConsole.instances.Add(this);
				Object.DontDestroyOnLoad(this.transform.root.gameObject);
			}

			Application.logMessageReceived += this.HandleLog;
		}

		protected virtual void	OnEnable()
		{
			string	sendDataRaw = PlayerPrefs.GetString(NGGameConsole.SendDataKeyPref);

			if (string.IsNullOrEmpty(sendDataRaw) == false)
				this.sendData.AddRange(sendDataRaw.Split(NGGameConsole.SendDataSeparator));

			if (this.GameConsoleEnableChanged != null)
				this.GameConsoleEnableChanged(true);
		}

		protected virtual void	OnDisable()
		{
			PlayerPrefs.SetString(NGGameConsole.SendDataKeyPref, string.Join(NGGameConsole.SendDataSeparator.ToString(), this.sendData.ToArray()));

			if (this.GameConsoleEnableChanged != null)
				this.GameConsoleEnableChanged(false);
		}

		protected virtual void	OnDestroy()
		{
			Application.logMessageReceived -= this.HandleLog;

			PlayerPrefs.SetFloat("NGGameConsole.x", this.windowSize.x);
			PlayerPrefs.SetFloat("NGGameConsole.y", this.windowSize.y);
			PlayerPrefs.SetFloat("NGGameConsole.w", this.windowSize.width);
			PlayerPrefs.SetFloat("NGGameConsole.h", this.windowSize.height);
		}

		protected virtual void	OnGUI()
		{
			int	hash = 0;

			for (int i = 0; i < this.dataConsole.Length; i++)
			{
				if (this.dataConsole[i] != null)
					hash += this.dataConsole[i].GetHashCode();
			}

			if (this.lastDataConsoleHashCode != hash)
			{
				this.lastDataConsoleHashCode = hash;
				this.UpdateGroupedData();
			}

			if (this.windowStyle == null)
			{
				this.windowStyle = new GUIStyle();
				this.windowStyle.normal.background = this.windowBackground;
				this.windowStyle.padding = new RectOffset(5, 5, 5, 18);
				this.evenRowScrollView = new GUIStyle();
				this.evenRowScrollView.normal.background = this.evenBackgroundLog;
				this.oddRowScrollView = new GUIStyle();
				this.oddRowScrollView.normal.background = this.oddBackgroundLog;
				this.dragStyle = new GUIStyle();
				this.dragStyle.normal.background = this.oddBackgroundLog;
			}

			this.windowSize.width = Mathf.Clamp(this.windowSize.width, 1F, Screen.width);
			this.windowSize.height = Mathf.Clamp(this.windowSize.height, 1F, Screen.height);

			this.windowSize.x = Mathf.Clamp(this.windowSize.x, 0F, Screen.width - this.windowSize.width);
			this.windowSize.y = Mathf.Clamp(this.windowSize.y, 0F, Screen.height - this.windowSize.height);

			this.windowDragArea.width = this.windowSize.width;
			this.windowDragArea.height = this.windowDragAreaHeight;

			this.titleRect.height = this.windowDragArea.height;
			this.tabRect.height = this.windowDragArea.height;
			this.saveRect.height = this.windowDragArea.height;
			this.closeButtonRect.x = this.windowSize.width - this.windowDragArea.height;
			this.closeButtonRect.height = this.windowDragArea.height;
			this.settingsButtonRect.x = this.closeButtonRect.x - this.windowDragArea.height;
			this.settingsButtonRect.height = this.windowDragArea.height;
			this.copyAllRect.y = this.windowSize.height - 18F;

			this.windowSize = GUILayout.Window(1, this.windowSize, this.DrawOnGUI, GUIContent.none, this.windowStyle);
		}

		private void	OnValidate()
		{
			if (this.maxLogs < 1)
				this.maxLogs = 1;

			if (this.minWindowSize.x < 1F)
				this.minWindowSize.x = 1F;

			if (this.minWindowSize.y < 1F)
				this.minWindowSize.y = 1F;

			if (this.windowSize.width < 1F)
				this.windowSize.width = 1F;
			if (this.windowSize.height < 1F)
				this.windowSize.height = 1F;
			if (this.windowSize.x < 0)
				this.windowSize.x = 0F;
			if (this.windowSize.y < 0)
				this.windowSize.y = 0F;
		}

		private void	DrawOnGUI(int id)
		{
			if (this.movable == true)
				GUI.Label(this.windowDragArea, "", this.dragStyle);

			GUI.Label(this.titleRect, "NG Game Console", this.titleStyle);

			if (this.settings.Count > 0)
			{
				if (GUI.Button(this.settingsButtonRect, "@", this.closeButtonStyle) == true)
				{
					this.tab = Tab.Settings;
					return;
				}
			}

			if (GUI.Button(this.closeButtonRect, "X", this.closeButtonStyle) == true)
			{
				this.enabled = false;
				return;
			}

			if (GUI.Button(this.saveRect, "Save", this.tabButtonStyle) == true)
			{
				this.tab = Tab.Save;
				return;
			}

			if (this.dataConsole.Length > 0)
			{
				if (GUI.Button(this.tabRect, this.modeNames[(int)this.displayTab], this.tabButtonStyle) == true)
				{
					if (this.tab == Tab.Save)
						this.tab = this.displayTab;
					else if (this.displayTab == Tab.Data)
					{
						this.displayTab = Tab.Logs;
						this.tab = Tab.Logs;
					}
					else if (this.tab == Tab.Logs)
					{
						this.displayTab = Tab.Data;
						this.tab = Tab.Data;
					}
					else if (this.tab == Tab.Settings)
						this.tab = Tab.Logs;
					return;
				}
			}

			GUI.Button(this.closeButtonRect, "X", this.closeButtonStyle);

			if (this.resizable == true)
				this.HandleResize();

			GUILayout.Space(-this.windowStyle.padding.top + this.windowDragAreaHeight + this.reservedHeadSpace);

			if (this.tab == Tab.Data)
				this.DrawData();
			else if (this.tab == Tab.Logs)
			{
				this.shortDataScrollPosition = GUILayout.BeginScrollView(this.shortDataScrollPosition);
				{
					GUILayout.BeginHorizontal();
					{
						for (int i = 0; i < this.dataConsole.Length; i++)
						{
							if (this.dataConsole[i] != null)
							{
								this.dataConsole[i].UpdateData();
								this.dataConsole[i].ShortGUI();
							}
						}

						GUILayout.FlexibleSpace();
					}
					GUILayout.EndHorizontal();
				}
				GUILayout.EndScrollView();

				GUILayout.Space(2F);

				this.DrawLogs();
			}
			else if (this.tab == Tab.Settings)
			{
				foreach (var pair in this.settings)
				{
					GUILayout.Label(pair.Key, this.dataConsoleGroupStyle, GUILayout.ExpandWidth(true));
					for (int i = 0; i < pair.Value.Count; i++)
						pair.Value[i]();
				}
			}
			else if (this.tab == Tab.Save)
				this.DrawSave();

			if (this.movable == true)
				GUI.DragWindow(this.windowDragArea);
		}

		public void	SwitchNextMode()
		{
			if (this.tab == Tab.Data)
				this.tab = 0;
			else
				this.tab += 1;
		}

		public void	SwitchPreviousMode()
		{
			if (this.tab == Tab.Logs)
				this.tab = Tab.Data;
			else
				this.tab -= 1;
		}

		public void	AddSetting(string title, Action callback)
		{
			List<Action>	section;

			if (this.settings.TryGetValue(title, out section) == false)
			{
				section = new List<Action>();
				this.settings.Add(title, section);
			}

			section.Add(callback);
		}

		public void	RemoveSetting(string title, Action callback)
		{
			List<Action>	section;

			if (this.settings.TryGetValue(title, out section) == true)
			{
				section.Remove(callback);

				if (section.Count == 0)
					this.settings.Remove(title);
			}
		}

		public void	AddGameLog(GameLog log)
		{
			if (this.autoLogScroll == true)
				this.logScrollPosition.y = float.MaxValue;

			// Stack logs even if stackTrace or log type is different.
			if (this.logs.Count > 0 &&
				this.logs[this.logs.Count - 1].condition == log.condition)
			{
				++this.logs[this.logs.Count - 1].count;
			}
			else
				this.logs.Add(log);

			if (log.type == (LogType)(1 << (int)LogType.Log))
				++this.normalLogsCount;
			else if (log.type == (LogType)(1 << (int)LogType.Warning))
				++this.warningLogsCount;
			else if (log.type == (LogType)(1 << (int)LogType.Error) || log.type == (LogType)(1 << (int)LogType.Exception) || log.type == (LogType)(1 << (int)LogType.Assert))
				++this.errorLogsCount;

			if (this.logs.Count > this.maxLogs)
			{
				for (int i = 0; i < this.logs.Count - this.maxLogs; i++)
				{
					if (log.type == (LogType)(1 << (int)LogType.Log))
						--this.normalLogsCount;
					else if (log.type == (LogType)(1 << (int)LogType.Warning))
						--this.warningLogsCount;
					else if (log.type == (LogType)(1 << (int)LogType.Error) || log.type == (LogType)(1 << (int)LogType.Exception) || log.type == (LogType)(1 << (int)LogType.Assert))
						--this.errorLogsCount;
				}

				this.logs.RemoveRange(0, this.logs.Count - this.maxLogs);
			}

			this.logsBuffer.Append(Environment.NewLine);
			this.logsBuffer.Append(log.condition);

			if (log.opened == true)
			{
				this.logsBuffer.Append(Environment.NewLine);
				this.logsBuffer.Append(log.stackTrace);
			}

			if (this.logsBuffer.Length > NGGameConsole.MaxChars)
				this.logsBuffer.Remove(0, this.logsBuffer.Length - NGGameConsole.MaxChars);

			this.logsText = this.logsBuffer.ToString();

			if (this.isScrollStickingBottom == true)
				this.logScrollPosition.y = Mathf.Infinity;
		}

		public void	ReserveHeadSpace(float height)
		{
			this.reservedHeadSpace += height;
		}

		public void	ReserveFootSpace(float height)
		{
			this.reservedFootSpace += height;
		}

		private void	DrawData()
		{
			this.fullDataScrollPosition = GUILayout.BeginScrollView(this.fullDataScrollPosition);
			{
				foreach (var group in this.groupedData)
				{
					GUILayout.Space(10F);

					GUILayout.BeginHorizontal();
					{
						GUILayout.Label(group.Key, this.dataConsoleGroupStyle, GUILayout.ExpandWidth(false));

						if (group.Value.HasUpdate == true)
						{
							group.Value.freezeUpdate = GUILayout.Toggle(group.Value.freezeUpdate, "Freeze", GUILayout.ExpandWidth(false));
							if (GUILayout.Button("Update", GUILayout.ExpandWidth(false)) == true)
							{
								group.Value.freezeUpdate = true;
								for (int i = 0; i < group.Value.Count; i++)
									group.Value[i].UpdateData();
							}
						}

						if (GUILayout.Button("Copy", GUILayout.Width(50F)) == true)
						{
							StringBuilder	buffer = Utility.GetBuffer();

							for (int i = 0; i < group.Value.Count; i++)
							{
								string	copy = group.Value[i].Copy();

								if (string.IsNullOrEmpty(copy) == false)
									buffer.AppendLine(copy);
							}

							if (buffer.Length >= Environment.NewLine.Length)
								buffer.Length -= Environment.NewLine.Length;

							GUIUtility.systemCopyBuffer = Utility.ReturnBuffer(buffer);
						}
					}
					GUILayout.EndHorizontal();

					for (int i = 0; i < group.Value.Count; i++)
					{
						if (group.Value.freezeUpdate == false)
							group.Value[i].UpdateData();
						group.Value[i].FullGUI();
					}
				}
			}
			GUILayout.EndScrollView();

			GUILayout.BeginHorizontal();
			{
				if (GUILayout.Button("Copy all") == true)
					this.CopyAllIntoClipboard();

				GUILayout.Space(10F);

				if (GUILayout.Button("Freeze all") == true)
					this.FreezeAll();

				GUILayout.Space(10F);

				if (GUILayout.Button("Update all") == true)
					this.UpdateAll();
			}
			GUILayout.EndHorizontal();
		}

		private bool			sendLogs = true;
		private List<string>	sendData = new List<string>(8);

		private void	DrawSave()
		{
			GUILayout.Space(5F);

			if (GUILayout.Button("Send") == true)
				this.StartCoroutine(this.RequestServer());
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Server :", GUILayout.Width(50F));
				string	url = string.IsNullOrEmpty(this.serverLog) ? NGGameConsole.DefaultServerLog + this.uniqueKey : this.serverLog;
				GUILayout.Label(url);
			}
			GUILayout.EndHorizontal();

			if (string.IsNullOrEmpty(this.serverLog) == true && this.uniqueKey.Length < NGGameConsole.MinUniqueKeyLength)
			{
				Color	tmp = GUI.color;
				GUI.color = Color.red;
				GUILayout.Label("You are using NG Tools server. Unique Key of minimum " + NGGameConsole.MinUniqueKeyLength + " characters is REQUIRED!");
				GUI.color = tmp;
			}

			if (GUILayout.Button("Save") == true)
				this.SaveLocalCopy();

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Folder :", GUILayout.Width(50F));
				GUILayout.Label(this.GetLogFilePath());
			}
			GUILayout.EndHorizontal();

			if (string.IsNullOrEmpty(this.requestFeedback) == false)
				GUILayout.TextField(this.requestFeedback);

			GUILayout.Space(10F);

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Include :", GUILayout.ExpandWidth(false));

				if (GUILayout.Button("All") == true)
				{
					this.sendLogs = true;
					this.sendData.Clear();

					foreach (var group in this.groupedData)
						this.sendData.Add(group.Key);
				}

				if (GUILayout.Button("None") == true)
				{
					this.sendLogs = false;
					this.sendData.Clear();
				}
			}
			GUILayout.EndHorizontal();

			this.sendLogs = GUILayout.Toggle(this.sendLogs, "Logs");

			GUILayout.Space(5F);
			GUILayout.Label("Data :");
			foreach (var group in this.groupedData)
			{
				bool	send = this.sendData.Contains(group.Key);
				bool	newSend = GUILayout.Toggle(send, group.Key);

				if (newSend != send)
				{
					if (newSend == true)
						this.sendData.Add(group.Key);
					else
						this.sendData.Remove(group.Key);
				}
			}
		}

		private void	DrawLogs()
		{
			Rect	rTotal = GUILayoutUtility.GetRect(0F, 16F);
			GUILayout.FlexibleSpace();
			rTotal.height = 16F;

			rTotal.x = 4F;
			rTotal.width = this.windowSize.width - rTotal.x - rTotal.x;

			Rect	r = rTotal;

			if (this.terminalMode == TerminalMode.Log)
			{
				int	mask = 1 << (int)LogType.Log;

				r.width /= 3F;

				this.normalLogButton.text = "Log (" + this.normalLogsCount.ToString() + ")";
				bool	logType = GUI.Toggle(r, ((int)this.unityDisplayLogType & mask) != 0, this.normalLogButton, GUI.skin.button);
				r.x += r.width;
				if (logType == true)
					this.unityDisplayLogType |= (LogType)mask;
				else
					this.unityDisplayLogType = (LogType)((int)this.unityDisplayLogType & ~mask);

				this.warningLogButton.text = "Warning (" + this.warningLogsCount.ToString() + ")";
				mask = 1 << (int)LogType.Warning;
				logType = GUI.Toggle(r, ((int)this.unityDisplayLogType & mask) != 0, this.warningLogButton, GUI.skin.button);
				r.x += r.width;
				if (logType == true)
					this.unityDisplayLogType |= (LogType)mask;
				else
					this.unityDisplayLogType = (LogType)((int)this.unityDisplayLogType & ~mask);

				this.errorLogButton.text = "Error (" + this.errorLogsCount.ToString() + ")";
				mask = (1 << (int)LogType.Assert) | (1 << (int)LogType.Error) | (1 << (int)LogType.Exception);
				logType = GUI.Toggle(r, ((int)this.unityDisplayLogType & mask) != 0, this.errorLogButton, GUI.skin.button);
				r.x = rTotal.x;
				r.y += r.height + 2F;
				if (logType == true)
					this.unityDisplayLogType |= (LogType)mask;
				else
					this.unityDisplayLogType = (LogType)((int)this.unityDisplayLogType & ~mask);
			}

			Rect	bodyRect = r;
			Rect	viewRect = new Rect();

			r.height = 32F;

			bodyRect.width = rTotal.width;
			bodyRect.height = this.windowSize.height - r.y - this.reservedFootSpace;

			if (this.terminalMode == TerminalMode.RawText)
			{
				if (this.logsText.Length >= NGGameConsole.MaxChars)
					this.collapseContent.text = this.logsText.Substring(this.logsText.Length - NGGameConsole.MaxChars, NGGameConsole.MaxChars);
				else
					this.collapseContent.text = this.logsText;

				viewRect.height = this.rawLogsBackgroundStyle.CalcHeight(this.collapseContent, bodyRect.width);

				this.logScrollPosition = GUI.BeginScrollView(bodyRect, this.logScrollPosition, viewRect);
				{
					viewRect.width = bodyRect.width;

					if (viewRect.height < bodyRect.height)
						viewRect.height = bodyRect.height;

					GUI.TextArea(viewRect, this.collapseContent.text, this.rawLogsBackgroundStyle);
				}
				GUI.EndScrollView();

				this.isScrollStickingBottom = viewRect.height - this.logScrollPosition.y <= bodyRect.height;
			}
			else if (this.terminalMode == TerminalMode.Log)
			{
				for (int i = 0; i < this.logs.Count; i++)
				{
					if ((this.logs[i].type & this.unityDisplayLogType) == 0)
						continue;

					float	width = rTotal.width - NGGameConsole.LogTypeSquareWidth;

					if (this.displayTime == true && string.IsNullOrEmpty(this.logs[i].time) == false)
						width -= this.timeStyle.fixedWidth;

					if (this.logs[i].opened == true)
					{
						viewRect.height += logStyle.CalcHeight(new GUIContent(this.logs[i].condition), width);
						viewRect.height += logStyle.CalcHeight(new GUIContent(this.logs[i].stackTrace), width);
					}
					else
						viewRect.height += logStyle.CalcHeight(new GUIContent(this.logs[i].firstLine), width);
				}

				float	scrollOn = viewRect.height >= bodyRect.height ? 15F : 0F;

				GUI.Box(bodyRect, " ", this.rawLogsBackgroundStyle);

				this.logScrollPosition = GUI.BeginScrollView(bodyRect, this.logScrollPosition, viewRect);
				{
					r.y = 0F;

					this.autoLogScroll = true;

					for (int i = 0, j = 0; i < this.logs.Count; i++)
					{
						if ((this.logs[i].type & this.unityDisplayLogType) == 0)
							continue;

						float	width = rTotal.width - scrollOn - NGGameConsole.LogTypeSquareWidth;

						if (string.IsNullOrEmpty(this.logs[i].time) == false)
							width -= this.timeStyle.fixedWidth;

						if (this.logs[i].opened == true)
						{
							r.height = logStyle.CalcHeight(new GUIContent(this.logs[i].condition), width);
							r.height += logStyle.CalcHeight(new GUIContent(this.logs[i].stackTrace), width);
						}
						else
						{
							r.height = logStyle.CalcHeight(new GUIContent(this.logs[i].firstLine), width);
						}

						if (r.y + r.height <= this.logScrollPosition.y)
						{
							r.y += r.height;
							++j;
							continue;
						}

						r.x = 0F;
						r.width = rTotal.width;
						GUI.Label(r, "", ((j++ & 1) == 1) ? this.evenRowScrollView : this.oddRowScrollView);

						r.width = 5F;
						if (this.logs[i].type == (LogType)(1 << (int)LogType.Log))
							GUI.Label(r, " ", this.normalLogStyle);
						else if (this.logs[i].type == (LogType)(1 << (int)LogType.Warning))
							GUI.Label(r, " ", this.warningLogStyle);
						else
							GUI.Label(r, " ", this.errorLogStyle);

						r.x = NGGameConsole.LogTypeSquareWidth + 2F;

						if (this.displayTime == true && string.IsNullOrEmpty(this.logs[i].time) == false)
						{
							r.width = this.timeStyle.fixedWidth;
							GUI.Label(r, this.logs[i].time, this.timeStyle);
							r.x += r.width;
						}

						if (this.logs[i].opened == true)
						{
							r.width = rTotal.width - r.x;
							r.height = logStyle.CalcHeight(new GUIContent(this.logs[i].condition), r.width);
							if (GUI.Button(r, this.logs[i].condition, logStyle) == true)
								this.logs[i].opened = false;
							r.y += r.height;

							r.height = logStyle.CalcHeight(new GUIContent(this.logs[i].stackTrace), r.width);
							if (GUI.Button(r, this.logs[i].stackTrace, logStyle) == true)
								this.logs[i].opened = false;
						}
						else
						{
							r.width = rTotal.width - r.x;
							r.height = logStyle.CalcHeight(new GUIContent(this.logs[i].firstLine), r.width);
							if (GUI.Button(r, this.logs[i].firstLine, logStyle) == true)
								this.logs[i].opened = true;
						}

						r.y += r.height;

						this.collapseContent.text = "(" + this.logs[i].count + ")";
						Vector2	size = GUI.skin.label.CalcSize(this.collapseContent);
						r.width = size.x;
						r.height = size.y;
						r.x = rTotal.width - scrollOn - NGGameConsole.LogTypeSquareWidth - r.width;
						r.y -= r.height;
						GUI.Label(r, this.collapseContent.text);
						r.y += r.height;

						if (r.y - this.logScrollPosition.y > bodyRect.height)
						{
							this.autoLogScroll = false;
							break;
						}
					}
				}
				GUI.EndScrollView();
			}
		}

		private	void	HandleLog(string condition, string stackTrace, LogType type)
		{
			this.AddGameLog(new GameLog(condition, stackTrace, type, this.timeFormat) { opened = (this.terminalMode == TerminalMode.RawText && this.appendStackTrace == true) });
		}

		private void	HandleResize()
		{
			if (this.resizeStyle == null)
			{
				this.resizeStyle = new GUIStyle();
				this.resizeStyle.fontSize = 16;
				this.resizeStyle.alignment = TextAnchor.MiddleCenter;
			}

			Rect	resizeArea = new Rect(this.windowSize.width - 16,
										  this.windowSize.height - 16,
										  16, 16);

			GUI.Label(resizeArea, "➷", this.resizeStyle);

			// BUG Cause the mouse moving by itself in the editor...
			if (resizeArea.Contains(Event.current.mousePosition) == true)
			{
				//Cursor.SetCursor(this.resizeCursor, Vector2.zero, CursorMode.Auto);
				if (Event.current.type == EventType.MouseDown)
				{
					this.resizing = true;
					this.startDragPosition = Event.current.mousePosition;
					this.initialWindowArea = this.windowSize;
					Event.current.Use();
				}
			}
			//else
			//	Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

			if (this.resizing == true)
			{
				if (Event.current.type == EventType.MouseUp)
				{
					this.resizing = false;
					Event.current.Use();
				}
				else
				{
					this.windowSize.width = Mathf.Clamp(this.initialWindowArea.width + Event.current.mousePosition.x - this.startDragPosition.x,
														this.minWindowSize.x,
														Screen.width - this.windowSize.x);
					this.windowSize.height = Mathf.Clamp(this.initialWindowArea.height + Event.current.mousePosition.y - this.startDragPosition.y,
														 this.minWindowSize.y,
														 Screen.height - this.windowSize.y);
				}
			}
		}

		private void	UpdateGroupedData()
		{
			this.groupedData.Clear();

			for (int i = 0; i < this.dataConsole.Length; i++)
			{
				if (this.dataConsole[i] == null)
				{
					InternalNGDebug.LogWarning(Errors.GameConsole_NullDataConsole, "DataConsole #" + i + " is null.", this);
					continue;
				}

				this.dataConsole[i].shortStyle = this.dataConsoleShortStyle;
				this.dataConsole[i].fullStyle = this.dataConsoleFullStyle;
				this.dataConsole[i].subTitleStyle = this.dataConsoleSubTitleStyle;
				this.dataConsole[i].InitOnGUI();

				if (this.groupedData.ContainsKey(this.dataConsole[i].group) == false)
					this.groupedData.Add(this.dataConsole[i].group, new GroupDataConsole());

				this.groupedData[this.dataConsole[i].group].Add(this.dataConsole[i]);
			}

			foreach (GroupDataConsole group in this.groupedData.Values)
				group.Init();
		}

		private string	CopyAll()
		{
			StringBuilder	buffer = Utility.GetBuffer();

			foreach (var group in this.groupedData)
			{
				if (buffer.Length > 0)
					buffer.AppendLine();
				buffer.Append(group.Key);

				for (int i = 0; i < group.Value.Count; i++)
				{
					string	copy = group.Value[i].Copy();

					if (string.IsNullOrEmpty(copy) == false)
					{
						buffer.AppendLine();
						buffer.Append(copy);
					}
				}

				buffer.AppendLine();
			}

			if (buffer.Length > Environment.NewLine.Length)
				buffer.Length -= Environment.NewLine.Length;

			return Utility.ReturnBuffer(buffer);
		}

		private void	CopyAllIntoClipboard()
		{
			GUIUtility.systemCopyBuffer = this.CopyAll();
		}

		private void	FreezeAll()
		{
			foreach (var group in this.groupedData)
				group.Value.freezeUpdate = true;
		}

		private void	UpdateAll()
		{
			foreach (var group in this.groupedData)
			{
				group.Value.freezeUpdate = true;
				for (int i = 0; i < group.Value.Count; i++)
					group.Value[i].UpdateData();
			}
		}

		private string	GetLogFilePath()
		{
			return Path.Combine(Application.persistentDataPath, DateTime.Now.ToString(this.logFilenameFormat));
		}

		private void	SaveLocalCopy()
		{
			StringBuilder	buffer = Utility.GetBuffer(this.GetSaveContent());
			string			path = this.GetLogFilePath();

			File.WriteAllText(path, Utility.ReturnBuffer(buffer));

			this.requestFeedback = "Log saved at \"" + path + "\".";
			InternalNGDebug.Log(this.requestFeedback);
		}

		private IEnumerator	RequestServer()
		{
			if (string.IsNullOrEmpty(this.serverLog) == true && this.uniqueKey.Length < NGGameConsole.MinUniqueKeyLength)
			{
				this.requestFeedback = "You are using NG Tools server, Unique Key is required!";
				InternalNGDebug.Log(this.requestFeedback);
				yield break;
			}

			StringBuilder	buffer = Utility.GetBuffer(this.GetSaveContent());
			string			url = string.IsNullOrEmpty(this.serverLog) ? NGGameConsole.DefaultServerLog + this.uniqueKey : this.serverLog;

			if (buffer.Length > NGGameConsole.MaxSendLogSize)
			{
				buffer.Length = NGGameConsole.MaxSendLogSize;
				this.requestFeedback = "Can not send more than " + NGGameConsole.MaxSendLogSize  + " bytes. Data has been truncated." + Environment.NewLine;
			}
			else
				this.requestFeedback = string.Empty;

			using (WWW w = new WWW(url, Encoding.UTF8.GetBytes(Utility.ReturnBuffer(buffer))))
			{
				this.requestFeedback += "Sending log at \"" + url + "\".";

				yield return w;

				if (string.IsNullOrEmpty(w.error) == false)
					this.requestFeedback += Environment.NewLine + "Request failed: " + w.error;
				else if (w.text.Length < 3)
					this.requestFeedback += Environment.NewLine + "Log sent, but request failed: " + w.text;
				else
					this.requestFeedback += Environment.NewLine + "Log sent, reach it at https://ngtools.tech/nggc.php?" + w.text;

				InternalNGDebug.Log(this.requestFeedback);
			}
		}

		private string	GetSaveContent()
		{
			StringBuilder	buffer = Utility.GetBuffer();

			foreach (var group in this.groupedData)
			{
				if (this.sendData.Contains(group.Key) == false)
					continue;

				buffer.Append(group.Key);

				for (int i = 0; i < group.Value.Count; i++)
				{
					group.Value[i].FullGUI();
					string	copy = group.Value[i].Copy();

					if (string.IsNullOrEmpty(copy) == false)
					{
						buffer.AppendLine();
						buffer.Append(copy);
					}
				}

				buffer.AppendLine();
				buffer.AppendLine("###");
			}

			if (this.sendLogs == true)
			{
				for (int i = 0; i < this.logs.Count; i++)
				{
					if (i > 0)
						buffer.AppendLine();

					int	b = -1;

					for (int j = (int)this.logs[i].type; j > 0; j >>= 1, ++b);

					buffer.AppendLine(Enum.GetName(typeof(LogType), (LogType)b));
					if (string.IsNullOrEmpty(this.logs[i].time) == false)
						buffer.AppendLine(this.logs[i].time);
					buffer.AppendLine(this.logs[i].condition);
					buffer.Append(this.logs[i].stackTrace);
				}
			}

			return Utility.ReturnBuffer(buffer);
		}
	}
}