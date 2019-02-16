using NGLicenses;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	[Serializable, VisibleModule(50)]
	internal sealed class MainModule : Module, IStreams
	{
		[Serializable]
		private sealed class Vars
		{
			public int	workingStream = 1; // By default, focus MainStream instead of CompilerStream.
		}

		public const int	MaxStreams = 2;

		public event Action<StreamLog>	StreamAdded;
		public event Action<StreamLog>	StreamDeleted;

		public List<StreamLog>	Streams { get { return this.streams; } }
		public int				WorkingStream { get { return this.perWindowVars.Get(RowUtility.drawingWindow).workingStream; } }

		private CompilerStream	compilerStream { get { return this.streams[0] as CompilerStream; } }
		private MainStream		mainStream { get { return this.streams[1] as MainStream; } }

		[Exportable(ExportableAttribute.ArrayOptions.Immutable)]
		private List<StreamLog>	streams;

		[SerializeField]
		private PerWindowVars<Vars>	perWindowVars;

		public	MainModule()
		{
			this.name = "Main";
			this.streams = new List<StreamLog>();
			this.streams.Add(new CompilerStream());
			this.streams.Add(new MainStream());
			this.perWindowVars = new PerWindowVars<Vars>();

			MainModuleSettings	mainSettings = HQ.Settings.Get<MainModuleSettings>();

			foreach (ILogFilter filter in mainSettings.GenerateFilters())
				this.streams[1].groupFilters.filters.Add(filter);
		}

		public override void	OnEnable(NGConsoleWindow console, int id)
		{
			base.OnEnable(console, id);

			// Prevents corrupted console settings.
			if (this.streams.Count < 2 || this.compilerStream == null || this.mainStream == null)
			{
				this.streams.Clear();
				this.streams.Add(new CompilerStream());
				this.streams.Add(new MainStream());

				MainModuleSettings	mainSettings = HQ.Settings.Get<MainModuleSettings>();

				foreach (ILogFilter filter in mainSettings.GenerateFilters())
					this.streams[1].groupFilters.filters.Add(filter);
			}

			foreach (StreamLog stream in this.streams)
			{
				stream.Init(this.console, this);
				stream.FilterAltered += this.console.SaveModules;
				stream.OptionAltered += this.console.SaveModules;
			}

			this.console.CheckNewLogConsume += this.CreateStreamForCategory;
			this.console.OptionAltered += this.UpdateFilteredRows;
			this.console.ConsoleCleared += this.Clear;
			this.console.wantsMouseMove = true;

			// Populates with default commands if missing.
			ConsoleSettings	settings = HQ.Settings.Get<ConsoleSettings>();
			settings.inputsManager.AddCommand("Navigation", ConsoleConstants.SwitchNextStreamCommand, KeyCode.Tab, true);
			settings.inputsManager.AddCommand("Navigation", ConsoleConstants.SwitchPreviousStreamCommand, KeyCode.Tab, true, true);

			if (this.perWindowVars == null)
				this.perWindowVars = new PerWindowVars<Vars>();
		}

		public override void	OnDisable()
		{
			base.OnDisable();

			this.console.CheckNewLogConsume -= this.CreateStreamForCategory;
			this.console.OptionAltered -= this.UpdateFilteredRows;
			this.console.ConsoleCleared -= this.Clear;
			this.console.wantsMouseMove = false;

			foreach (var stream in this.streams)
			{
				stream.Uninit();
				stream.FilterAltered -= this.console.SaveModules;
				stream.OptionAltered -= this.console.SaveModules;
			}
		}

		public override void	OnGUI(Rect r)
		{
			float	yOrigin = r.y;
			float	maxHeight = r.height;

			r = this.DrawStreamTabs(r);

			r.height = maxHeight - (r.y - yOrigin);
			this.streams[this.perWindowVars.Get(RowUtility.drawingWindow).workingStream].OnGUI(r);
		}

		public override void	OnEnter()
		{
			base.OnEnter();

			this.console.BeforeGUIHeaderRightMenu += this.GUIExport;
			this.console.AfterGUIHeaderRightMenu += this.OnAfterHeader;
		}

		public override void	OnLeave()
		{
			base.OnLeave();

			this.console.BeforeGUIHeaderRightMenu -= this.GUIExport;
			this.console.AfterGUIHeaderRightMenu -= this.OnAfterHeader;
		}

		public void	Clear()
		{
			foreach (var stream in this.streams)
				stream.Clear();
		}

		public void	FocusStream(int i)
		{
			if (i < 0)
				this.perWindowVars.Get(RowUtility.drawingWindow).workingStream = 0;
			else if (i >= this.streams.Count)
				this.perWindowVars.Get(RowUtility.drawingWindow).workingStream = this.streams.Count - 1;
			else
				this.perWindowVars.Get(RowUtility.drawingWindow).workingStream = i;
			this.console.SaveModules();
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

		private Rect	DrawStreamTabs(Rect r)
		{
			float	maxWidth = r.width;

			r.y += 2F;
			r.height = Constants.SingleLineHeight;

			MainModuleSettings	mainSettings = HQ.Settings.Get<MainModuleSettings>();
			ConsoleSettings		settings = HQ.Settings.Get<ConsoleSettings>();
			Vars				vars = this.perWindowVars.Get(RowUtility.drawingWindow);

			// Switch stream
			if (settings.inputsManager.Check("Navigation", ConsoleConstants.SwitchNextStreamCommand) == true)
			{
				vars.workingStream += 1;
				if (vars.workingStream >= this.streams.Count)
					vars.workingStream = 0;

				Event.current.Use();
			}
			else if (settings.inputsManager.Check("Navigation", ConsoleConstants.SwitchPreviousStreamCommand) == true)
			{
				vars.workingStream -= 1;
				// Handle CompilerStream.
				if (vars.workingStream == 0 && this.compilerStream.totalCount == 0)
					vars.workingStream = this.streams.Count - 1;
				if (vars.workingStream < 0)
					vars.workingStream = this.streams.Count - 1;

				Event.current.Use();
			}

			for (int i = 0; i < this.streams.Count; i++)
				r = this.streams[i].OnTabGUI(r, i);

			r.width = 16F;

			if (GUI.Button(r, "+", HQ.Settings.Get<GeneralSettings>().MenuButtonStyle) == true)
			{
				if (this.CheckMaxStreams(this.streams.Count - 2) == true)
				{
					StreamLog	stream = new StreamLog();
					stream.Init(this.console, this);
					stream.FilterAltered += this.console.SaveModules;
					stream.OptionAltered += this.console.SaveModules;

					foreach (ILogFilter filter in mainSettings.GenerateFilters())
						stream.groupFilters.filters.Add(filter);

					this.streams.Add(stream);

					this.console.SaveModules();

					if (this.streams.Count == 1)
						vars.workingStream = 0;

					if (this.StreamAdded != null)
						this.StreamAdded(stream);

					stream.RefreshFilteredRows();
				}
			}

			r.x += r.width;
			r.width = maxWidth - r.x;

			return r;
		}

		public static readonly Dictionary<int, string>	methodsCategories = new Dictionary<int, string>();

		private void	CreateStreamForCategory(int consoleIndex, Row row)
		{
			// StreamLog does not receive output from compiler.
			if ((row.log.mode & (Mode.ScriptCompileError | Mode.ScriptCompileWarning)) != 0)
				return;

			// Category has a priority over all rules.
			string	category;
			int		hash = row.log.condition.GetHashCode();

			if (MainModule.methodsCategories.TryGetValue(hash, out category) == false)
			{
				ILogContentGetter	log = row as ILogContentGetter;

				if (log != null)
					category = log.Category;
				else
					category = null;

				MainModule.methodsCategories.Add(hash, category);
			}

			if (category != null)
			{
				for (int j = 0; j < this.streams.Count; j++)
				{
					if (this.streams[j].onlyCategory == true && this.streams[j].name == category)
						return;
				}

				MainModuleSettings	mainSettings = HQ.Settings.Get<MainModuleSettings>();
				StreamLog			stream = new StreamLog();
				stream.onlyCategory = true;
				stream.name = category;
				stream.Init(this.console, this);

				foreach (ILogFilter filter in mainSettings.GenerateFilters())
					stream.groupFilters.filters.Add(filter);

				this.streams.Add(stream);

				if (this.StreamAdded != null)
					this.StreamAdded(stream);
			}
		}

		private void	UpdateFilteredRows()
		{
			ConsoleFlags	flags = (ConsoleFlags)UnityLogEntries.consoleFlags;

			this.mainStream.collapse = (flags & ConsoleFlags.Collapse) != 0;
			this.mainStream.displayLog = (flags & ConsoleFlags.LogLevelLog) != 0;
			this.mainStream.displayWarning = (flags & ConsoleFlags.LogLevelWarning) != 0;
			this.mainStream.displayError = (flags & ConsoleFlags.LogLevelError) != 0;
			this.mainStream.RefreshFilteredRows();
		}

		private Rect	OnAfterHeader(Rect r)
		{
			if (HQ.Settings.Get<GeneralSettings>().drawLogTypesInHeader == true)
			{
				float	x = r.xMin;
				float	w = this.Streams[this.WorkingStream].GetOptionWidth();

				r.xMin = r.xMax - w;
				this.Streams[this.WorkingStream].DrawOptions(r);

				r.xMin = x;
				r.xMax -= w;
			}
			return r;
		}

		private Rect	GUIExport(Rect r)
		{
			Vars	vars = this.perWindowVars.Get(RowUtility.drawingWindow);

			EditorGUI.BeginDisabledGroup(this.streams[vars.workingStream].rowsDrawer.Count == 0);
			{
				Utility.content.text = LC.G("MainModule_ExportStream");
				float	x = r.x;
				float	width = HQ.Settings.Get<GeneralSettings>().MenuButtonStyle.CalcSize(Utility.content).x;
				r.x = r.x + r.width - width;
				r.width = width;

				if (GUI.Button(r, Utility.content, HQ.Settings.Get<GeneralSettings>().MenuButtonStyle) == true)
				{
					List<Row>	rows = new List<Row>();

					for (int i = 0; i < this.streams[vars.workingStream].rowsDrawer.Count; i++)
						rows.Add(this.console.rows[this.streams[vars.workingStream].rowsDrawer[i]]);

					ExportLogsWindow.Export(rows);
				}

				r.width = r.x - x;
				r.x = x;
			}
			EditorGUI.EndDisabledGroup();

			return r;
		}

		private bool	CheckMaxStreams(int count)
		{
			return NGLicensesManager.Check(count < MainModule.MaxStreams, NGTools.NGConsole.NGAssemblyInfo.Name + " Pro", "Free version does not allow more than " + MainModule.MaxStreams + " streams.\n\nI'm sorry if you feel this feature is a gift from above, but consider above to be selfish sometimes. :p");
		}
	}
}