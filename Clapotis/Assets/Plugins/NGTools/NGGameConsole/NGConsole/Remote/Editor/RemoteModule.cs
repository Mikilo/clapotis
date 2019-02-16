using NGLicenses;
using NGTools;
using NGTools.Network;
using NGTools.NGGameConsole;
using NGTools.UON;
using NGToolsEditor.Network;
using NGToolsEditor.NGConsole;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEditor;

namespace NGToolsEditor.NGGameConsole
{
	using UnityEngine;

	[Serializable, VisibleModule(200)]
	internal sealed class RemoteModule : Module, IStreams, IRows, INGServerConnectable, IUONSerialization
	{
		[Serializable]
		private sealed class Vars
		{
			public int	workingStream;
		}

		public sealed class CommandRequest
		{
			public int			id;
			public RemoteRow	row;

			public	CommandRequest(int id, RemoteRow row)
			{
				this.id = id;
				this.row = row;
			}
		}

		public const int	MaxCLICommandExecutions = 20;
		public const string	ProgressBarConnectingString = "Connecting Remote module";
		//private static byte[]	pollDiscBuffer = new byte[1];

		public event Action<StreamLog>	StreamAdded;
		public event Action<StreamLog>	StreamDeleted;

		private List<StreamLog>	streams;
		public List<StreamLog>	Streams { get { return this.streams; } }
		public int				WorkingStream { get { return this.perWindowVars.Get(RowUtility.drawingWindow).workingStream; } }

		public Client	Client { get { return this.client; } }

		[Exportable, SerializeField]
		private string			address = string.Empty;
		[Exportable, SerializeField]
		private int				port = AutoDetectUDPClient.DefaultPort;

		[NonSerialized]
		private List<Row>	rows;

		[NonSerialized]
		private Client	client;

		[NonSerialized]
		public RemoteCommandParser	parser;
		[NonSerialized]
		public List<CommandRequest>	pendingCommands;
		private string				command;
		[NonSerialized]
		private AbstractTcpClient[]	tcpClientProviders;
		[NonSerialized]
		private string[]				tcpClientProviderNames;
		private int						selectedTcpClientProvider;

		[NonSerialized]
		private int	idCounter = 0;

		[SerializeField]
		private PerWindowVars<Vars>	perWindowVars;
		[NonSerialized]
		private Vars				currentVars;

		[NonSerialized]
		private int	countExec = 0;

		[NonSerialized]
		private Thread	connectingThread;

		[NonSerialized]
		private string[]	requiredServices;
		[NonSerialized]
		private string[]	remoteServices;
		[NonSerialized]
		private string		servicesWarning;
		[NonSerialized]
		private bool		showServicesWarning;

		[NonSerialized]
		private bool	lastWantsMouseMoveValue;

		[NonSerialized]
		private RecycledTextEditorProxy	textEditor = new RecycledTextEditorProxy();
		[NonSerialized]
		private int	next;

		static	RemoteModule()
		{
			FreeLicenseOverlay.Append(typeof(NGConsoleWindow), "\n• " + RemoteModule.MaxCLICommandExecutions + " remote CLI command executions per session.");
		}

		public	RemoteModule()
		{
			this.name = "Remote";
			this.streams = new List<StreamLog>();
			this.streams.Add(new StreamLog());
			this.rows = new List<Row>();
			this.perWindowVars = new PerWindowVars<Vars>();

			RemoteModuleSettings	remoteSettings = HQ.Settings.Get<RemoteModuleSettings>();

			foreach (ILogFilter filter in remoteSettings.GenerateFilters())
				this.streams[0].groupFilters.filters.Add(filter);
		}

		public override void	OnEnable(NGConsoleWindow editor, int id)
		{
			base.OnEnable(editor, id);

			this.requiredServices = new string[]
			{
				NGTools.NGAssemblyInfo.Name,
				NGTools.NGAssemblyInfo.Version,
				NGTools.NGGameConsole.NGAssemblyInfo.Name,
				NGTools.NGGameConsole.NGAssemblyInfo.Version
			};

			for (int i = 0; i < this.rows.Count; i++)
				this.rows[i].Init(this.console, this.rows[i].log);

			foreach (StreamLog stream in this.streams)
			{
				stream.Init(this.console, this);
				stream.rowsDrawer.SetRowGetter(this);
				stream.FilterAltered += this.console.SaveModules;
				stream.OptionAltered += this.console.SaveModules;
				stream.Cleared += this.console.SaveModules;

				for (int i = 0; i < this.rows.Count; i++)
					stream.AddLog(i, this.rows[i]);

				this.console.CheckNewLogConsume -= stream.ConsumeLog;
				this.console.PropagateNewLog -= stream.AddLog;
			}

			// Populate with default commands if missing.
			ConsoleSettings	settings = HQ.Settings.Get<ConsoleSettings>();
			settings.inputsManager.AddCommand("Navigation", ConsoleConstants.SwitchNextStreamCommand, KeyCode.Tab, true);
			settings.inputsManager.AddCommand("Navigation", ConsoleConstants.SwitchPreviousStreamCommand, KeyCode.Tab, true, true);

			this.parser = new RemoteCommandParser();
			this.parser.CallExec += this.Exec;
			this.pendingCommands = new List<CommandRequest>();
			this.command = string.Empty;

			this.tcpClientProviders = Utility.CreateNGTInstancesOf<AbstractTcpClient>();

			this.tcpClientProviderNames = new string[this.tcpClientProviders.Length];

			for (int i = 0; i < this.tcpClientProviders.Length; i++)
				this.tcpClientProviderNames[i] = this.tcpClientProviders[i].GetType().Name;

			if (this.selectedTcpClientProvider > this.tcpClientProviders.Length - 1)
				this.selectedTcpClientProvider = this.tcpClientProviders.Length - 1;

			ConnectionsManager.Executer.HandlePacket(GameConsolePacketId.Logger_ServerSendLog, this.OnLogReceived);
			ConnectionsManager.NewServer += this.RepaintOnServerUpdated;
			ConnectionsManager.UpdateServer += this.RepaintOnServerUpdated;
			ConnectionsManager.KillServer += this.RepaintOnServerUpdated;

			if (this.perWindowVars == null)
				this.perWindowVars = new PerWindowVars<Vars>();
		}

		public override void	OnDisable()
		{
			base.OnDisable();

			foreach (StreamLog stream in this.streams)
			{
				stream.Uninit();
				stream.FilterAltered -= this.console.SaveModules;
				stream.OptionAltered -= this.console.SaveModules;
				stream.Cleared -= this.console.SaveModules;
			}

			if (this.IsClientConnected() == true)
				this.client.Close();

			ConnectionsManager.Executer.UnhandlePacket(GameConsolePacketId.Logger_ServerSendLog, this.OnLogReceived);
			ConnectionsManager.NewServer -= this.RepaintOnServerUpdated;
			ConnectionsManager.UpdateServer -= this.RepaintOnServerUpdated;
			ConnectionsManager.KillServer -= this.RepaintOnServerUpdated;
		}

		public override void	OnGUI(Rect r)
		{
			this.currentVars = this.perWindowVars.Get(RowUtility.drawingWindow);

			float	yOrigin = r.y;
			float	maxHeight = r.height;

			r.y += 2F;
			r.height = Constants.SingleLineHeight + 2F; // Layout is requiring 2 more pixels.
			GUILayout.BeginArea(r);
			{
				GUILayout.BeginHorizontal(GeneralStyles.Toolbar);
				{
					if (GUILayout.Button("Clear", GeneralStyles.ToolbarButton) == true)
						this.Clear();

					List<NGServerInstance>	servers = ConnectionsManager.Servers;

					lock (ConnectionsManager.Servers)
					{
						EditorGUI.BeginDisabledGroup(this.connectingThread != null || servers.Count == 0);
						{
							if (servers.Count == 0)
								Utility.content.text = "No server";
							else if (servers.Count == 1)
								Utility.content.text = "1 server";
							else
								Utility.content.text = servers.Count + " servers";

							Rect	r2 = GUILayoutUtility.GetRect(Utility.content, GeneralStyles.ToolbarDropDown);

							if (GUI.Button(r2, Utility.content, GeneralStyles.ToolbarDropDown) == true)
								PopupWindow.Show(new Rect(0F, 16F, 0F, 0F), new ServersSelectorWindow(this));
						}
						EditorGUI.EndDisabledGroup();
					}

					EditorGUI.BeginDisabledGroup(this.connectingThread != null || this.IsClientConnected());
					{
						if (this.tcpClientProviderNames.Length > 1)
						{
							EditorGUI.BeginChangeCheck();
							this.selectedTcpClientProvider = EditorGUILayout.Popup(this.selectedTcpClientProvider, this.tcpClientProviderNames, GeneralStyles.ToolbarDropDown);
							if (EditorGUI.EndChangeCheck() == true)
								this.console.SaveModules();
						}

						EditorGUI.BeginChangeCheck();
						this.address = EditorGUILayout.TextField(this.address, GUILayoutOptionPool.MinWidth(50F));
						if (EditorGUI.EndChangeCheck() == true)
							this.console.SaveModules();

						if  (string.IsNullOrEmpty(this.address) == true)
						{
							Rect	r2 = GUILayoutUtility.GetLastRect();
							EditorGUI.LabelField(r2, LC.G("RemoteModule_Address"), GeneralStyles.TextFieldPlaceHolder);
						}

						string	port = this.port.ToString();
						if (port == "0")
							port = string.Empty;
						EditorGUI.BeginChangeCheck();
						port = EditorGUILayout.TextField(port, GUILayoutOptionPool.MaxWidth(40F));
						if (EditorGUI.EndChangeCheck() == true)
						{
							try
							{
								if (string.IsNullOrEmpty(port) == false)
									this.port = Mathf.Clamp(int.Parse(port), 0, UInt16.MaxValue - 1);
								else
									this.port = 0;
							}
							catch
							{
								this.port = 0;
								GUI.FocusControl(null);
							}

							this.console.SaveModules();
						}

						if ((port == string.Empty || port == "0") && this.port == 0)
						{
							Rect	r2 = GUILayoutUtility.GetLastRect();
							EditorGUI.LabelField(r2, LC.G("RemoteModule_Port"), GeneralStyles.TextFieldPlaceHolder);
						}
					}
					EditorGUI.EndDisabledGroup();

					GUILayout.FlexibleSpace();

					if (this.IsClientConnected() == true)
					{
						if (GUILayout.Button(LC.G("RemoteModule_Disconnect"), GeneralStyles.ToolbarButton, GUILayoutOptionPool.Width(80F)) == true)
							this.CloseClient();

						if (string.IsNullOrEmpty(this.servicesWarning) == false)
						{
							Rect	r2 = GUILayoutUtility.GetRect(0F, 16F, GUILayoutOptionPool.Width(16F));

							r2.x += 4F;
							r2.y += 2F;
							GUI.DrawTexture(r2, UtilityResources.WarningIcon);
							if (Event.current.type == EventType.MouseDown &&
								r2.Contains(Event.current.mousePosition) == true)
							{
								this.showServicesWarning = !this.showServicesWarning;
								Event.current.Use();
							}
						}
					}
					else
					{
						if (this.connectingThread == null)
						{
							if (GUILayout.Button(LC.G("RemoteModule_Connect"), GeneralStyles.ToolbarButton, GUILayoutOptionPool.Width(80F)) == true)
								this.Connect(this.address, this.port);
						}
						else
						{
							Utility.content.text = "Connecting";
							Utility.content.image = GeneralStyles.StatusWheel.image;
							GUILayout.Label(Utility.content, GeneralStyles.ToolbarButton, GUILayoutOptionPool.Width(90F));
							Utility.content.image = null;
							this.console.Repaint();

							if (GUILayout.Button("X", GeneralStyles.ToolbarButton, GUILayoutOptionPool.Width(16F)) == true)
							{
								this.connectingThread.Abort();
								this.connectingThread.Join(0);
								this.connectingThread = null;
							}
						}
					}
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndArea();
			r.y += r.height;
			r.height = Constants.SingleLineHeight;

			if (this.showServicesWarning == true &&
				string.IsNullOrEmpty(this.servicesWarning) == false)
			{
				r.height = Constants.SingleLineHeight + Constants.SingleLineHeight;
				EditorGUI.HelpBox(r, this.servicesWarning, MessageType.Warning);

				Rect	r2 = r;
				r2.xMin = r.xMax - 20F;
				r2.yMin = r.yMax - 15F;

				if (GUI.Button(r2, "X") == true)
					this.showServicesWarning = false;

				r.y += r.height;
				r.height = Constants.SingleLineHeight;
			}

			r = this.DrawStreamTabs(r);

			if (this.currentVars.workingStream < 0)
				return;

			if (this.IsClientConnected() == true)
			{
				float	streamHeight = maxHeight - (r.y - yOrigin) - Constants.SingleLineHeight;
				float	xMin = r.xMin;

				r.xMin = 0F;
				r.y += streamHeight;

				// Draw CLI before rows.
				r.height = Constants.SingleLineHeight;
				this.DrawCLI(r);
				// Shit of a hack to handle input for completion and its drawing.
				this.parser.PostGUI(r, ref this.command);

				r.xMin = xMin;
				r.y -= streamHeight;
				r.height = streamHeight;

				this.streams[this.currentVars.workingStream].OnGUI(r);
				r.y += streamHeight;

				// Redraw again to display in front.
				this.parser.PostGUI(r, ref this.command);
			}
			else
			{
				r.height = maxHeight - (r.y - yOrigin);
				this.streams[this.currentVars.workingStream].OnGUI(r);
			}
		}

		public void	Connect(string address, int port)
		{
			this.connectingThread = ConnectionsManager.OpenClient(this.console, new DefaultTcpClient(), address, port, this.OnClientConnected);
		}

		public bool	IsConnected(Client client)
		{
			return this.client == client;
		}

		public override void	OnEnter()
		{
			base.OnEnter();

			this.console.BeforeGUIHeaderRightMenu += this.GUIExport;
			this.lastWantsMouseMoveValue = this.console.wantsMouseMove;
			this.console.wantsMouseMove = true;
		}

		public override void	OnLeave()
		{
			base.OnLeave();

			this.console.BeforeGUIHeaderRightMenu -= this.GUIExport;
			this.console.wantsMouseMove = this.lastWantsMouseMoveValue;
		}

		public void	Clear()
		{
			this.rows.Clear();

			foreach (var stream in this.streams)
				stream.Clear();
		}

		public void	FocusStream(int i)
		{
			if (i < 0)
				this.currentVars.workingStream = 0;
			else if (i >= this.streams.Count)
				this.currentVars.workingStream = this.streams.Count - 1;
			else
				this.currentVars.workingStream = i;
		}

		public void	DeleteStream(int i)
		{
			StreamLog	stream = this.streams[i];

			stream.Uninit();
			stream.FilterAltered -= this.console.SaveModules;
			stream.OptionAltered -= this.console.SaveModules;
			this.streams.RemoveAt(i);

			if (this.StreamDeleted != null)
				this.StreamDeleted(stream);

			foreach (Vars var in this.perWindowVars.Each())
				var.workingStream = Mathf.Clamp(var.workingStream, 0, this.streams.Count - 1);

			this.console.SaveModules();
		}

		public void	AddRow(Row row)
		{
			this.rows.Add(row);

			int	index = this.rows.Count - 1;

			for (int i = 0; i < this.streams.Count; i++)
				this.streams[i].ConsumeLog(index, row);

			for (int i = 0; i < this.streams.Count; i++)
			{
				this.streams[i].AddLog(index, row);
				this.streams[i].rowsDrawer.UpdateAutoScroll();
			}
		}

		private Rect	DrawStreamTabs(Rect r)
		{
			ConsoleSettings	settings = HQ.Settings.Get<ConsoleSettings>();
			float			maxWidth = r.width;

			r.height = Constants.SingleLineHeight;

			// Switch stream
			if (settings.inputsManager.Check("Navigation", ConsoleConstants.SwitchNextStreamCommand) == true)
			{
				this.currentVars.workingStream += 1;
				if (this.currentVars.workingStream >= this.streams.Count)
					this.currentVars.workingStream = 0;

				Event.current.Use();
			}
			if (settings.inputsManager.Check("Navigation", ConsoleConstants.SwitchPreviousStreamCommand) == true)
			{
				this.currentVars.workingStream -= 1;
				if (this.currentVars.workingStream < 0)
					this.currentVars.workingStream = this.streams.Count - 1;

				Event.current.Use();
			}

			for (int i = 0; i < this.streams.Count; i++)
				r = this.streams[i].OnTabGUI(r, i);

			r.width = 16F;

			if (GUI.Button(r, "+", HQ.Settings.Get<GeneralSettings>().MenuButtonStyle) == true)
			{
				RemoteModuleSettings	remoteSettings = HQ.Settings.Get<RemoteModuleSettings>();
				StreamLog				stream = new StreamLog();
				stream.Init(this.console, this);
				stream.rowsDrawer.SetRowGetter(this);
				stream.FilterAltered += this.console.SaveModules;
				stream.OptionAltered += this.console.SaveModules;

				foreach (ILogFilter filter in remoteSettings.GenerateFilters())
					stream.groupFilters.filters.Add(filter);

				this.streams.Add(stream);

				if (this.StreamAdded != null)
					this.StreamAdded(stream);

				this.console.CheckNewLogConsume -= stream.ConsumeLog;
				this.console.PropagateNewLog -= stream.AddLog;
				this.console.SaveModules();

				if (this.streams.Count == 1)
					this.currentVars.workingStream = 0;
			}
			r.x += r.width;

			if (this.streams.Count > 2)
			{
				r.y += r.height + 2F;
				r.x = 0F;
				r.width = maxWidth;
			}
			else
				r.width = maxWidth - r.x;

			return r;
		}

		private Rect	DrawCLI(Rect r)
		{
			RemoteModuleSettings	settings = HQ.Settings.Get<RemoteModuleSettings>();

			r.height = Constants.SingleLineHeight;
			r.width -= settings.execButtonWidth;

			if (this.parser.Root.children.Count == 0)
				EditorGUI.LabelField(r, LC.G("RemoteModule_CLIUnavailable"), settings.CommandInputStyle);
			else
			{
				if (Event.current.type == EventType.MouseMove && this.parser.matchingCommands != null)
					this.console.Repaint();

				this.textEditor.instance = EditorGUIProxy.s_RecycledEditor;

				// Hack Tricky way to keep the focus on the text field and the cursor at the end.
				bool	postTextField = false;
				if (this.next != 0 && Event.current.type == EventType.Repaint)
				{
					this.next = 0;
					GUI.FocusControl(CommandParser.CommandTextFieldName);
					EditorGUIUtility.editingTextField = true;
					postTextField = true;
				}

				EditorGUI.BeginChangeCheck();
				this.parser.HandleKeyboard(ref this.command);
				if (EditorGUI.EndChangeCheck() == true)
				{
					if (this.textEditor.text != this.command)
					{
						this.textEditor.text = this.command;
						this.textEditor.MoveTextEnd();
					}

					this.next = EditorGUIUtility.keyboardControl;
					GUI.FocusControl(CommandParser.CommandTextFieldName);
				}

				EditorGUI.BeginChangeCheck();
				GUI.SetNextControlName(CommandParser.CommandTextFieldName);
				this.command = EditorGUI.TextField(r, this.command, settings.CommandInputStyle);

				if (postTextField == true)
				{
					this.textEditor.MoveTextEnd();
					this.console.Repaint();
				}

				if (EditorGUI.EndChangeCheck() == true)
					this.parser.UpdateMatchesAvailable(this.command);

				if (this.parser.matchingCommands != null &&
					Event.current.type == EventType.Repaint &&
					EditorGUIUtility.editingTextField == false)
				{
					this.parser.matchingCommands = null;
				}
			}

			EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(command) == true || this.parser.Root.children.Count == 0);
			{
				r.x += r.width;
				r.width = settings.execButtonWidth;
				if (GUI.Button(r, "Exec", settings.ExecButtonStyle) == true)
					this.Exec();
				r.y += r.height;
			}
			EditorGUI.EndDisabledGroup();

			return r;
		}

		private void	Exec()
		{
			string		result = string.Empty;
			ExecResult	returnValue = ExecResult.InvalidCommand;

			if (this.CheckMaxCLICommandExecutions(this.countExec++) == false)
				result = "Free version does not allow more than " + RemoteModule.MaxCLICommandExecutions + " remote executions.\nAwesomeness has a price. :D";
			else
				returnValue = this.parser.Exec(this.command, ref result);

			LogEntry	log = new LogEntry();
			log.condition = this.command;
			// Use file to display the default value from RemoteCommand while waiting for the answer.
			log.file = result;

			RemoteRow	row = new RemoteRow();
			row.Init(this.console, log);

			this.AddRow(row);

			if (returnValue == ExecResult.Success)
			{
				// Create a waiter to update the log when the server will answer.
				CommandRequest	cr = new CommandRequest(++this.idCounter, row);
				this.pendingCommands.Add(cr);

				this.client.AddPacket(new ClientSendCommandPacket(cr.id, this.command), this.OnCommandAnswered);
			}
			else
				row.error = result;

			this.command = string.Empty;
			this.parser.Clear();
		}

		private void	CloseClient()
		{
			try
			{
				this.console.Repaint();

				this.client.AddPacket(new ClientUnsubscribeLogsPacket());

				ConnectionsManager.Close(this.client, this.console);
			}
			finally
			{
				this.client = null;
			}
		}

		public bool		IsClientConnected()
		{
			return this.client != null &&
				   this.client.tcpClient.Connected == true;
		}

		private void	RepaintOnServerUpdated(NGServerInstance instance)
		{
			EditorApplication.delayCall += this.console.Repaint;
		}

		private void	OverrideAddressPort(object data)
		{
			string	server = data as string;

			if (server == this.address + ":" + this.port && this.IsClientConnected() == true)
				return;

			int	separator = server.LastIndexOf(':');

			this.address = server.Substring(0, separator);
			this.port = int.Parse(server.Substring(separator + 1));
			this.console.SaveModules();

			if (this.IsClientConnected() == true)
				this.CloseClient();

			this.Connect(this.address, this.port);
		}

		private void	OnClientConnected(Client client)
		{
			this.connectingThread = null;

			if (client == null)
				return;

			this.client = client;
			this.client.debugPrefix = "GC:";
			this.remoteServices = null;
			this.servicesWarning = string.Empty;
			this.showServicesWarning = true;

			if (HQ.Settings.Get<RemoteModuleSettings>().addBlankRowOnConnection == true)
			{
				BlankRow	row = new BlankRow(this.console.position.height);
				LogEntry	log = new LogEntry();
				row.Init(this.console, log);
				this.AddRow(row);
			}

			this.client.AddPacket(new ClientRequestServicesPacket(), this.OnServerVersionReceived);
		}

		Row		IRows.GetRow(int i)
		{
			return this.rows[i];
		}

		int		IRows.CountRows()
		{
			return this.rows.Count;
		}

		private Rect	GUIExport(Rect r)
		{
			Vars	vars = this.perWindowVars.Get(RowUtility.drawingWindow);

			EditorGUI.BeginDisabledGroup(vars.workingStream < 0 || this.streams[vars.workingStream].rowsDrawer.Count == 0);
			{
				Utility.content.text = LC.G("RemoteModule_ExportStream");
				GeneralSettings	settings = HQ.Settings.Get<GeneralSettings>();
				float	x = r.x;
				float	width = settings.MenuButtonStyle.CalcSize(Utility.content).x;
				r.x = r.x + r.width - width;
				r.width = width;

				if (GUI.Button(r, Utility.content, settings.MenuButtonStyle) == true)
				{
					List<Row>	rows = new List<Row>();

					for (int i = 0; i < this.streams[vars.workingStream].rowsDrawer.Count; i++)
						rows.Add(this.rows[this.streams[vars.workingStream].rowsDrawer[i]]);

					ExportLogsWindow.Export(rows);
				}

				r.width = r.x - x;
				r.x = x;
			}
			EditorGUI.EndDisabledGroup();

			return r;
		}

		private bool	CheckMaxCLICommandExecutions(int count)
		{
			return NGLicensesManager.Check(count < RemoteModule.MaxCLICommandExecutions, NGTools.NGGameConsole.NGAssemblyInfo.Name + " Pro");
		}

		private void	OnCommandAnswered(ResponsePacket p)
		{
			if (p.CheckPacketStatus() == true)
			{
				ServerSendCommandResponsePacket	packet = p as ServerSendCommandResponsePacket;

				for (int i = 0; i < this.pendingCommands.Count; i++)
				{
					if (this.pendingCommands[i].id == packet.requestId)
					{
						if (packet.returnValue == ExecResult.Success)
							this.pendingCommands[i].row.result = packet.response;
						else
						{
							this.pendingCommands[i].row.error = packet.response;
						}

						this.pendingCommands.RemoveAt(i);
						break;
					}
				}

				this.console.Repaint();
			}
		}

		private void	OnServerVersionReceived(ResponsePacket p)
		{
			if (p.CheckPacketStatus() == true)
			{
				ServerSendServicesPacket	packet = p as ServerSendServicesPacket;
				bool					allServicesPresent = true;
				bool					versionMismatch = false;

				this.remoteServices = new string[packet.services.Length << 1];

				for (int j = 0; j < packet.services.Length; ++j)
				{
					this.remoteServices[j << 1] = packet.services[j];
					this.remoteServices[(j << 1) + 1] = packet.versions[j];
				}

				for (int i = 0; i + 1 < this.requiredServices.Length; i += 2)
				{
					bool	serviceFound = false;

					for (int j = 0; j < packet.services.Length; j++)
					{
						if (this.requiredServices[i] == packet.services[j])
						{
							serviceFound = true;

							if (this.requiredServices[i + 1] != packet.versions[j])
								versionMismatch = true;
							break;
						}
					}

					if (serviceFound == false)
						allServicesPresent = false;
				}

				if (allServicesPresent == true)
				{
					if (versionMismatch == true)
					{
						this.servicesWarning = "Required services (" + this.StringifyServices(this.requiredServices) + ") do not fully match the server services (" + this.StringifyServices(this.remoteServices) + ").\nThe behaviour might be unstable.";
						this.showServicesWarning = true;
					}

					this.client.AddPacket(new ClientSubscribeLogsPacket());
					this.client.AddPacket(new ClientAskCLIAvailablePacket(), this.OnAskCLIAnswered);
				}
				else
				{
					this.servicesWarning = "Server does not run required services (Requiring " + this.StringifyServices(this.requiredServices) + ", has " + this.StringifyServices(this.remoteServices) + "). Disconnecting from server.";
					this.showServicesWarning = true;
					this.CloseClient();
				}
			}
			else
			{
				InternalNGDebug.LogError("Server could not provide vital services. Can not continue, disconnecting from server.");
				this.CloseClient();
			}
		}

		private string	StringifyServices(string[] services)
		{
			StringBuilder	buffer = Utility.GetBuffer();

			for (int i = 0; i + 1 < services.Length; i += 2)
			{
				if (i > 0)
					buffer.Append(',');
				buffer.Append(services[i]);
				buffer.Append(':');
				buffer.Append(services[i + 1]);
			}

			return Utility.ReturnBuffer(buffer);
		}

		private void	OnAskCLIAnswered(ResponsePacket p)
		{
			if (p.CheckPacketStatus() == true)
			{
				if ((p as ServerAnswerCLIAvailablePacket).hasCLI == true)
					this.client.AddPacket(new ClientRequestCommandNodesPacket(), this.OnCommandNodesReceived);
			}
		}

		private void	OnCommandNodesReceived(ResponsePacket p)
		{
			if (p.CheckPacketStatus() == true)
			{
				this.parser.SetRoot((p as ServerSendCommandNodesPacket).root);
				this.console.Repaint();
			}
		}

		private void	OnLogReceived(Client sender, Packet command)
		{
			LogPacket	log = command as LogPacket;

			// Simulate a Log Entry.
			LogEntry	logEntry = new LogEntry();
			logEntry.condition = log.condition + "\n" + log.stackTrace;

			if (log.logType == UnityEngine.LogType.Log)
				logEntry.mode = Mode.ScriptingLog | Mode.MayIgnoreLineNumber;
			else if (log.logType == UnityEngine.LogType.Warning)
				logEntry.mode = Mode.ScriptingWarning | Mode.MayIgnoreLineNumber;
			else if (log.logType == UnityEngine.LogType.Error)
				logEntry.mode = Mode.ScriptingError | Mode.MayIgnoreLineNumber;
			else if (log.logType == UnityEngine.LogType.Exception ||
					 log.logType == UnityEngine.LogType.Assert)
			{
				if (HQ.Settings.Get<GeneralSettings>().differentiateException == false)
					logEntry.mode = Mode.ScriptingError | Mode.MayIgnoreLineNumber;
				else
					logEntry.mode = Mode.ScriptingError | Mode.ScriptingException | Mode.Log;
			}

			if (string.IsNullOrEmpty(log.stackTrace))
				logEntry.mode |= Mode.DontExtractStacktrace;
			else
			{
				int	fileStart = log.stackTrace.IndexOf(") (at ");

				if (fileStart != -1)
				{
					int	comma = log.stackTrace.IndexOf(':', fileStart);

					if (comma != -1)
					{
						int	par = log.stackTrace.IndexOf(')', comma);

						if (par != -1)
						{
							logEntry.file = log.stackTrace.Substring(fileStart + 6, comma - fileStart - 6);
							logEntry.line = int.Parse(log.stackTrace.Substring(comma + 1, par - comma - 1));
						}
					}
				}
			}

			DefaultRow	row = new DefaultRow();
			row.Init(this.console, logEntry);

			this.AddRow(row);
		}

		/// <summary>
		/// Saves rows in a file using BinaryFormatter instead of UON for performance reason.
		/// </summary>
		void	IUONSerialization.OnSerializing()
		{
			string	path = this.GetStoreRowPath();

			try
			{
				if (this.rows.Count > 0)
					File.WriteAllBytes(path, Utility.SerializeField(this.rows));
				else
					File.Delete(path);
			}
			catch (Exception ex)
			{
				InternalNGDebug.VerboseLogException(ex);
			}
		}

		void	IUONSerialization.OnDeserialized(DeserializationData data)
		{
			try
			{
				string	path = this.GetStoreRowPath();

				if (File.Exists(path) == true)
				{
					byte[]	raw = File.ReadAllBytes(path);

					if (raw != null && raw.Length > 0)
						this.rows = Utility.DeserializeField<List<Row>>(raw);
				}
			}
			catch (Exception ex)
			{
				InternalNGDebug.VerboseLogException(ex);
			}
		}

		private string	GetStoreRowPath()
		{
			string	local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

			local = Path.Combine(local, Constants.InternalPackageTitle);
			return Path.Combine(local, "RemoteModule");
		}
	}
}