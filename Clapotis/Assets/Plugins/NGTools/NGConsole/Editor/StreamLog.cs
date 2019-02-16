using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	[Serializable]
	[Exportable(ExportableAttribute.ArrayOptions.Add)]
	public class StreamLog : ISettingExportable
	{
		private readonly static string		PacMan = "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";
		private readonly static Row[]		EmptyRows = new Row[0];
		private readonly static List<Row>	CurrentRows = new List<Row>();
		private readonly static List<Row[]>	BackupRows = new List<Row[]>();

		/// <summary>Called whenever a filter's setting is altered, enabled or disabled.</summary>
		public event Action	FilterAltered;
		/// <summary>Called whenever a type is toggled (Normal, warning, error, exception).</summary>
		public event Action	OptionAltered;
		public event Action	Cleared;
		public event Action<StreamLog, Row, int>	RowAdded;

		[Exportable]
		public string	name;
		[Exportable]
		public bool		onlyCategory;
		[Exportable]
		public bool		pauseOnLog;
		[Exportable]
		public bool		displayLog;
		[Exportable]
		public bool		displayWarning;
		[Exportable]
		public bool		displayError;
		[Exportable]
		public bool		displayException;
		[Exportable]
		public bool		consumeLog;

		[NonSerialized]
		public int	totalCount;
		[NonSerialized]
		public int	logCount;
		[NonSerialized]
		public int	warningCount;
		[NonSerialized]
		public int	errorCount;
		[NonSerialized]
		public int	exceptionCount;

		public RowsDrawer	rowsDrawer;
		[Exportable(ExportableAttribute.ArrayOptions.Immutable)]
		public GroupFilters	groupFilters;

		[NonSerialized]
		protected NGConsoleWindow	console;
		[NonSerialized]
		protected IStreams	container;
		[NonSerialized]
		protected bool		addConsumedLog;

		[NonSerialized]
		protected GUIContent	logContent;
		[NonSerialized]
		protected int			lastLogCount = -1;
		[NonSerialized]
		protected GUIContent	warningContent;
		[NonSerialized]
		protected int			lastWarningCount = -1;
		[NonSerialized]
		protected GUIContent	errorContent;
		[NonSerialized]
		protected int			lastErrorCount = -1;
		[NonSerialized]
		protected GUIContent	exceptionContent;
		[NonSerialized]
		protected int			lastExceptionCount = -1;
		[NonSerialized]
		protected GUIContent	consumeLogContent;

		[NonSerialized]
		protected GUIContent	tabLabel;
		[NonSerialized]
		protected int			lastTotalCount = -1;

		[NonSerialized]
		private int	lastIndexConsummed = -1;
		[NonSerialized]
		private int	lastConsoleIndexDeleted = -1;

		[NonSerialized]
		private List<int>	consumedLogs = new List<int>();

		public	StreamLog()
		{
			this.name = "New stream";
			this.displayLog = true;
			this.displayWarning = true;
			this.displayError = true;
			this.displayException = true;
			this.addConsumedLog = false;
			this.consumeLog = false;

			this.rowsDrawer = new RowsDrawer();
			this.groupFilters = new GroupFilters();
		}

		public virtual void	Init(NGConsoleWindow console, IStreams container)
		{
			this.console = console;
			this.container = container;

			this.console.CheckNewLogConsume += this.ConsumeLog;
			this.console.PropagateNewLog += this.AddLog;

			this.rowsDrawer.Init(this.console, this.console);
			this.rowsDrawer.RowDeleted += this.DecrementCounts;
			this.rowsDrawer.LogContextMenu += this.FillContextMenu;
			this.rowsDrawer.Clear();

			this.groupFilters.FilterAltered += this.RefreshFilteredRows;

			this.totalCount = 0;
			this.logCount = 0;
			this.warningCount = 0;
			this.errorCount = 0;
			this.exceptionCount = 0;

			this.logContent = new GUIContent(UtilityResources.InfoIcon, "Log");
			this.warningContent = new GUIContent(UtilityResources.WarningIcon, "Warning");
			this.errorContent = new GUIContent(UtilityResources.ErrorIcon, "Error");
			this.exceptionContent = new GUIContent(this.errorContent.image, "Exception");
			this.tabLabel = new GUIContent();

			Texture2D	pacman = new Texture2D(0, 0, TextureFormat.RGBA32, false);
			pacman.LoadImage(Convert.FromBase64String(StreamLog.PacMan));
			this.consumeLogContent = new GUIContent(pacman);
		}

		public virtual void	Uninit()
		{
			this.groupFilters.FilterAltered -= this.RefreshFilteredRows;
			this.rowsDrawer.LogContextMenu -= this.FillContextMenu;
			this.rowsDrawer.RowDeleted -= this.DecrementCounts;
			this.rowsDrawer.Uninit();

			if (this.console != null)
			{
				this.console.CheckNewLogConsume -= this.ConsumeLog;
				this.console.PropagateNewLog -= this.AddLog;
			}
		}

		public virtual Rect	OnTabGUI(Rect r, int i)
		{
			if (this.lastTotalCount != this.totalCount)
			{
				this.lastTotalCount = this.totalCount;
				this.tabLabel.text = (this.onlyCategory == true ? "[" : string.Empty) + this.name + (this.onlyCategory == true ? "]" : string.Empty) + " (" + this.totalCount + ")";
			}

			GeneralSettings	settings = HQ.Settings.Get<GeneralSettings>();
			float			xMax = r.xMax;
			r.width = settings.MenuButtonStyle.CalcSize(this.tabLabel).x;

			EditorGUI.BeginChangeCheck();
			GUI.Toggle(r, i == this.container.WorkingStream, this.tabLabel, settings.MenuButtonStyle);
			if (EditorGUI.EndChangeCheck() == true)
			{
				if (Event.current.button == 0)
					this.container.FocusStream(i);
				else
				{
					// Show context menu on right click.
					if (Event.current.button == 1)
					{
						GenericMenu	menu = new GenericMenu();
						menu.AddItem(new GUIContent(LC.G("MainModule_ChangeName")), false, this.ChangeStreamName);
						menu.AddItem(new GUIContent(LC.G("MainModule_ToggleCategory")), this.onlyCategory, this.ToggleCategory);
						menu.AddItem(new GUIContent(LC.G("MainModule_PauseOnLog")), this.pauseOnLog, this.TogglePauseOnLog);
						menu.AddItem(new GUIContent(LC.G("Delete")), false, this.DeleteStream, i);

						if (this.rowsDrawer.Count > 0)
							menu.AddItem(new GUIContent(LC.G("Clear")), false, this.ClearStream);
						else
							menu.AddDisabledItem(new GUIContent(LC.G("Clear")));

						menu.DropDown(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0));
					}
					else if (Event.current.button == 2)
						this.container.DeleteStream(i);
				}
			}

			r.x += r.width;
			r.xMax = xMax;

			return r;
		}

		public virtual Rect	OnGUI(Rect r)
		{
			float	yOrigin = r.y;
			float	maxWidth = r.width;
			float	maxHeight = r.height;

			if (HQ.Settings.Get<GeneralSettings>().drawLogTypesInHeader == false)
			{
				// Draw options.
				r = this.DrawOptions(r);
			}

			// Draw filters on the right.
			r.width = maxWidth - r.x;
			r = this.DrawFilters(r);

			// Draw rows in a new line.
			r.x = 0F;
			r.width = this.console.position.width;
			r.height = maxHeight - (r.y - yOrigin);
			r = this.rowsDrawer.DrawRows(r, true);
			return r;
		}

		/// <summary>Checks whether Row is displayed regarding its log type.</summary>
		/// <param name="row"></param>
		/// <returns></returns>
		public virtual bool	CanDisplay(Row row)
		{
			if ((row.log.mode & Mode.ScriptingException) != 0)
			{
				if (HQ.Settings.Get<GeneralSettings>().differentiateException == false)
				{
					if (this.displayError == false)
						return false;
				}
				else if (this.displayException == false)
					return false;
			}
			else if ((row.log.mode & (Mode.ScriptCompileError | Mode.ScriptingError | Mode.Fatal | Mode.Error | Mode.Assert | Mode.AssetImportError | Mode.ScriptingAssertion)) != 0)
			{
				if (this.displayError == false)
					return false;
			}
			else if ((row.log.mode & (Mode.ScriptCompileWarning | Mode.ScriptingWarning | Mode.AssetImportWarning)) != 0)
			{
				if (this.displayWarning == false)
					return false;
			}
			else if (this.displayLog == false)
				return false;

			return true;
		}

		/// <summary>
		/// If this stream consumes incoming logs, it checks if the given <paramref name="row"/> is filtered and consumes it.
		/// </summary>
		/// <param name="consoleIndex"></param>
		/// <param name="row"></param>
		public virtual void	ConsumeLog(int consoleIndex, Row row)
		{
			// StreamLog does not receive output from compiler.
			if ((row.log.mode & (Mode.ScriptCompileError | Mode.ScriptCompileWarning)) != 0)
				return;

			if (this.consumeLog == true)
				//(this.addConsumedLog == true ||
				// row.isConsumed == false) &&
			{
				// Category has a priority over all rules.
				string	category;
				int		hash = row.log.fileHash + row.log.line;

				if (MainModule.methodsCategories.TryGetValue(hash, out category) == false)
				{
					ILogContentGetter	log = row as ILogContentGetter;

					if (log != null)
						category = log.Category;
					else
						category = null;

					MainModule.methodsCategories.Add(hash, category);
				}

				if (this.onlyCategory == true)
				{
					if (string.IsNullOrEmpty(category) == false && this.name == category)
					{
						row.isConsumed = true;
						this.consumedLogs.Add(consoleIndex);
						this.lastIndexConsummed = consoleIndex;
					}
				}
				else if (this.groupFilters.Filter(row) == true)
				{
					row.isConsumed = true;
					this.consumedLogs.Add(consoleIndex);
					this.lastIndexConsummed = consoleIndex;
				}
			}
		}

		public virtual void	AddLog(int consoleIndex, Row row)
		{
			// StreamLog does not receive output from compiler.
			if ((row.log.mode & (Mode.ScriptCompileError | Mode.ScriptCompileWarning)) != 0)
				return;

			// Skip if index is older than the last cleared index.
			if (consoleIndex <= this.lastConsoleIndexDeleted)
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

			//Utility.AssertFile(true, "I=" + i + " " + this.addConsumedLog + " && " + row.isConsumed + " && " + i + " != " + this.lastIndexConsummed);
			if (this.addConsumedLog == false)
			{
				if (row.isConsumed == true &&
					//this.consumedLogs.Contains(i) == false)
					consoleIndex != this.lastIndexConsummed)
				{
					return;
				}
			}

			if (string.IsNullOrEmpty(category) == false || this.onlyCategory == true)
			{
				if (this.name == category)
				{
					//Utility.AssertFile(true, "added " + i);

					// Count row, but do not display if it is not options compliant.
					this.CountLog(row);
					if (this.CanDisplay(row) == true)
					{
						this.rowsDrawer.Add(consoleIndex);
						this.OnRowAdded(this, row, consoleIndex);

						if (this.pauseOnLog == true && EditorApplication.isPlaying == true)
							EditorApplication.isPaused = true;
					}
				}
				return;
			}

			// Exclude filtered row.
			if (this.groupFilters.Filter(row) == true)
			{
				//Utility.AssertFile(true, "added " + i);

				// Count row, but do not display if it is not options compliant.
				this.CountLog(row);
				if (this.CanDisplay(row) == true)
				{
					this.rowsDrawer.Add(consoleIndex);
					this.OnRowAdded(this, row, consoleIndex);

					if (this.pauseOnLog == true && EditorApplication.isPlaying == true)
						EditorApplication.isPaused = true;
				}
			}
		}

		public virtual void	RefreshFilteredRows()
		{
			StreamLog.BackupRows.Clear();

			foreach (RowsDrawer.Vars vars in this.rowsDrawer.perWindowVars.Each())
			{
				if (vars.selectedLogs.Count > 0)
				{
					StreamLog.CurrentRows.Clear();

					for (int i = 0; i < vars.selectedLogs.Count; i++)
						StreamLog.CurrentRows.Add(this.rowsDrawer.rows.GetRow(this.rowsDrawer[vars.selectedLogs[i]]));

					StreamLog.BackupRows.Add(CurrentRows.ToArray());
				}
				else
					StreamLog.BackupRows.Add(StreamLog.EmptyRows);
			}

			int	lastIndexDeleted = this.lastConsoleIndexDeleted;
			this.Clear();
			this.lastConsoleIndexDeleted = lastIndexDeleted;

			// Reimport all logs.
			for (int i = 0; i < this.rowsDrawer.rows.CountRows(); i++)
			{
				this.lastIndexConsummed = i;
				this.AddLog(i, this.rowsDrawer.rows.GetRow(i));
			}

			int	logIndex = 0;
			int	l = 0;

			// Try to restore selected logs.
			foreach (RowsDrawer.Vars vars in this.rowsDrawer.perWindowVars.Each())
			{
				for (int j = 0; j < BackupRows[l].Length; j++)
				{
					for (int i = logIndex; i < this.rowsDrawer.Count && logIndex < BackupRows[l].Length; i++)
					{
						if (BackupRows[l][j] == this.rowsDrawer.rows.GetRow(this.rowsDrawer[i]))
						{
							vars.AddSelection(i);
							++logIndex;
							break;
						}
					}
				}

				++l;
			}

			this.console.Repaint();
			this.console.SaveModules();
		}

		/// <summary>
		/// Resets counters and clears all Rows in this stream.
		/// </summary>
		public virtual void	Clear()
		{
			this.consumedLogs.Clear();
			this.lastIndexConsummed = -1;
			this.lastConsoleIndexDeleted = -1;
			this.totalCount = 0;
			this.logCount = 0;
			this.warningCount = 0;
			this.errorCount = 0;
			this.exceptionCount = 0;
			this.rowsDrawer.Clear();

			foreach (RowsDrawer.Vars vars in this.rowsDrawer.perWindowVars.Each())
			{
				foreach (ListPointOfInterest list in vars.scrollbar.EachListInterests())
					list.Clear();
			}

			if (this.Cleared != null)
				this.Cleared();
		}

		public void	FillContextMenu(GenericMenu menu, Row row)
		{
			menu.AddSeparator("");

			for (int i = 0; i < this.groupFilters.filters.Count; i++)
				this.groupFilters.filters[i].ContextMenu(menu, row, i);
		}

		public virtual float	GetOptionWidth()
		{
			GeneralSettings	settings = HQ.Settings.Get<GeneralSettings>();
			float			width = 0F;

			if (HQ.Settings.Get<MainModuleSettings>().displayClearStreamButton == true)
			{
				Utility.content.text = LC.G("Clear");
				width += settings.MenuButtonStyle.CalcSize(Utility.content).x;
			}

			width += settings.MenuButtonStyle.CalcSize(this.logContent).x;
			width += settings.MenuButtonStyle.CalcSize(this.warningContent).x;
			width += settings.MenuButtonStyle.CalcSize(this.errorContent).x;
			if (HQ.Settings.Get<GeneralSettings>().differentiateException == true)
				width += settings.MenuButtonStyle.CalcSize(this.exceptionContent).x;
			width += 30F;

			return width;
		}

		public virtual Rect	DrawOptions(Rect r)
		{
			GeneralSettings	settings = HQ.Settings.Get<GeneralSettings>();
			float			xMax = r.xMax;

			r.height = Constants.SingleLineHeight;

			if (HQ.Settings.Get<MainModuleSettings>().displayClearStreamButton == true)
			{
				Utility.content.text = LC.G("Clear");
				r.width = settings.MenuButtonStyle.CalcSize(Utility.content).x;
				if (GUI.Button(r, Utility.content, settings.MenuButtonStyle) == true)
					this.ClearStream();
				r.x += r.width;
			}

			EditorGUI.BeginChangeCheck();
			if (this.lastLogCount != this.logCount)
			{
				this.lastLogCount = this.logCount;
				this.logContent.text = this.logCount.ToString();
			}
			r.width = settings.MenuButtonStyle.CalcSize(this.logContent).x;
			this.displayLog = GUI.Toggle(r, this.displayLog, this.logContent, settings.MenuButtonStyle);
			r.x += r.width;

			if (this.lastWarningCount != this.warningCount)
			{
				this.lastWarningCount = this.warningCount;
				this.warningContent.text = this.warningCount.ToString();
			}
			r.width = settings.MenuButtonStyle.CalcSize(this.warningContent).x;
			this.displayWarning = GUI.Toggle(r, this.displayWarning, this.warningContent, settings.MenuButtonStyle);
			r.x += r.width;

			if (this.lastErrorCount != this.errorCount)
			{
				this.lastErrorCount = this.errorCount;
				this.errorContent.text = this.errorCount.ToString();
			}
			r.width = settings.MenuButtonStyle.CalcSize(this.errorContent).x;
			this.displayError = GUI.Toggle(r, this.displayError, this.errorContent, settings.MenuButtonStyle);
			r.x += r.width;

			if (HQ.Settings.Get<GeneralSettings>().differentiateException == true)
			{
				if (this.lastExceptionCount != this.exceptionCount)
				{
					this.lastExceptionCount = this.exceptionCount;
					this.exceptionContent.text = this.exceptionCount.ToString();
				}
				r.width = settings.MenuButtonStyle.CalcSize(this.exceptionContent).x;
				using (ColorContentRestorer.Get(ConsoleConstants.ExceptionFoldoutColor))
				{
					this.displayException = GUI.Toggle(r, this.displayException, this.exceptionContent, settings.MenuButtonStyle);
				}
				r.x += r.width;
			}

			this.consumeLogContent.tooltip = LC.G("StreamLog_ConsumeLogTooltip");
			r.width = 30F;
			this.consumeLog = GUI.Toggle(r, this.consumeLog, this.consumeLogContent, settings.MenuButtonStyle);
			r.x += r.width;

			r.width = xMax - r.x;

			// Update Unity Console only if required.
			if (EditorGUI.EndChangeCheck() == true)
			{
				this.RefreshFilteredRows();
				this.OnOptionAltered();
				Utility.RepaintConsoleWindow();
			}

			return r;
		}

		protected virtual Rect	DrawFilters(Rect r)
		{
			EditorGUI.BeginChangeCheck();

			//r.height += 2F; // Layout overflows, this 2F fixes the error margin.
			r = this.groupFilters.OnGUI(r);

			if (this.groupFilters.filters.Count > 0)
			{
				bool	compact = true;

				if (this.console.position.width - r.x < GroupFilters.MinFilterWidth)
				{
					compact = false;
					r.x = 0F;
					r.y += r.height;
					r.width = this.console.position.width;
				}
				else
					r.width = this.console.position.width - r.x;

				r.height = Constants.SingleLineHeight;

				ILogFilter	selectedFilter = this.groupFilters.SelectedFilter;

				if (selectedFilter != null)
				{
					selectedFilter.OnGUI(r, compact);
					r.y += r.height + 2F;
				}

				//for (int i = 0; i < this.groupFilters.filters.Count; i++)
				//{
				//	r = this.groupFilters.filters[i].OnGUI(r, compact);
				//	r.x = 0F;
				//	r.width = this.console.position.width;
				//	r.height = Constants.SingleLineHeight;
				//}
			}
			else
				r.y += r.height + 2F;

			if (EditorGUI.EndChangeCheck() == true)
			{
				this.RefreshFilteredRows();

				if (this.FilterAltered != null)
					this.FilterAltered();
			}

			return r;
		}

		protected void	OnOptionAltered()
		{
			if (this.OptionAltered != null)
				this.OptionAltered();
		}

		protected void	OnRowAdded(StreamLog stream, Row row, int consoleIndex)
		{
			if (this.RowAdded != null)
				this.RowAdded(stream, row, consoleIndex);
		}

		private void	ClearStream()
		{
			if (this.rowsDrawer.Count > 0)
			{
				int	lastIndex = this.rowsDrawer[this.rowsDrawer.Count - 1];
				this.Clear();
				this.lastConsoleIndexDeleted = lastIndex;
			}
		}

		private void	DeleteStream(object data)
		{
			this.container.DeleteStream((int)data);
		}

		private void	ChangeStreamName()
		{
			PromptWindow.Start(this.name, this.RenameStream, null);
		}

		private void	ToggleCategory()
		{
			this.onlyCategory = !this.onlyCategory;
			this.lastTotalCount = -1;
			this.console.SaveModules();
		}

		private void	TogglePauseOnLog()
		{
			this.pauseOnLog = !this.pauseOnLog;
			this.console.SaveModules();
		}

		private void	RenameStream(object data, string newName)
		{
			if (string.IsNullOrEmpty(newName) == false)
			{
				this.name = newName;
				this.lastTotalCount = -1;
				this.console.SaveModules();
			}
		}

		protected void	CountLog(Row row)
		{
			if ((row.log.mode & Mode.ScriptingException) != 0)
			{
				if (HQ.Settings.Get<GeneralSettings>().differentiateException == false)
					++this.errorCount;
				else
					++this.exceptionCount;
			}
			else if ((row.log.mode & (Mode.ScriptCompileError | Mode.ScriptingError | Mode.Fatal | Mode.Error | Mode.Assert | Mode.AssetImportError | Mode.ScriptingAssertion)) != 0)
				++this.errorCount;
			else if ((row.log.mode & (Mode.ScriptCompileWarning | Mode.ScriptingWarning | Mode.AssetImportWarning)) != 0)
				++this.warningCount;
			else
				++this.logCount;
			++this.totalCount;
		}

		private void	DecrementCounts(Row row)
		{
			if ((row.log.mode & Mode.ScriptingException) != 0)
			{
				if (HQ.Settings.Get<GeneralSettings>().differentiateException == false)
					--this.errorCount;
				else
					--this.exceptionCount;
			}
			else if ((row.log.mode & (Mode.ScriptCompileError | Mode.ScriptingError | Mode.Fatal | Mode.Error | Mode.Assert | Mode.AssetImportError | Mode.ScriptingAssertion)) != 0)
				--this.errorCount;
			else if ((row.log.mode & (Mode.ScriptCompileWarning | Mode.ScriptingWarning | Mode.AssetImportWarning)) != 0)
				--this.warningCount;
			else
				--this.logCount;
			--this.totalCount;
		}

		public void	PreExport()
		{
		}

		public void	PreImport()
		{
		}

		public void	PostImport()
		{
			this.OnOptionAltered();
			Utility.RepaintConsoleWindow();
		}
	}
}