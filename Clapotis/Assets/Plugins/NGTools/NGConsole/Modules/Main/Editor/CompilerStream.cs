using NGTools;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	[Serializable]
	[Exportable(ExportableAttribute.ArrayOptions.Overwrite | ExportableAttribute.ArrayOptions.Immutable)]
	internal sealed class CompilerStream : StreamLog, IRows
	{
		private const float	SpeedWarning = 2.5F;

		[Exportable]
		public bool	sortByError;

		[NonSerialized]
		private List<CompileRow>	compileRows;
		[NonSerialized]
		private bool				aware;
		[NonSerialized]
		private bool				isCompiling;
		[NonSerialized]
		private BgColorContentRestorer	restorer;
		[NonSerialized]
		private int					lastLogsCount;

		public	CompilerStream()
		{
			this.name = "Compiler";
			this.rowsDrawer.CanDelete = false;
		}

		/// <summary>
		/// A special Init. Requires container to be a MainModule and an IStreams.
		/// </summary>
		/// <param name="console"></param>
		/// <param name="container">An instance of both MainModule and IStreams.</param>
		public override void	Init(NGConsoleWindow console, IStreams container)
		{
			base.Init(console, container);

			this.console.syncLogs.EndNewLog += this.UpdateAwareness;

			this.compileRows = new List<CompileRow>();
			this.rowsDrawer.SetRowGetter(this);
			this.restorer = new BgColorContentRestorer();

			this.console.UpdateTick += this.DetectCompile;

			this.OptionAltered += this.UpdateUnityConsoleOptions;
		}

		public override void	Uninit()
		{
			base.Uninit();

			this.console.UpdateTick -= this.DetectCompile;

			this.OptionAltered -= this.UpdateUnityConsoleOptions;
		}

		public override Rect	OnTabGUI(Rect r, int i)
		{
			if (this.totalCount <= 0)
			{
				if (i == this.container.WorkingStream)
					this.container.FocusStream(1);

				this.aware = false;
				return r;
			}

			EditorGUI.BeginChangeCheck();
			if (this.lastTotalCount != this.totalCount)
			{
				this.lastTotalCount = this.totalCount;
				this.tabLabel.text = this.name + " (" + this.totalCount + ")";
			}

			GeneralSettings	settings = HQ.Settings.Get<GeneralSettings>();
			float	xMax = r.xMax;
			r.width = settings.MenuButtonStyle.CalcSize(this.tabLabel).x;

			if (this.aware == true)
				GUI.Toggle(r, i == this.container.WorkingStream, this.tabLabel, settings.MenuButtonStyle);
			else
			{
				using (this.restorer.Set(1F,
										 Mathf.PingPong((float)EditorApplication.timeSinceStartup * CompilerStream.SpeedWarning, 1F),
										 Mathf.PingPong((float)EditorApplication.timeSinceStartup * CompilerStream.SpeedWarning, 1F),
										 1F))
				{
					GUI.Toggle(r, i == this.container.WorkingStream, this.tabLabel, settings.MenuButtonStyle);
				}
				this.console.Repaint();
			}

			if (EditorGUI.EndChangeCheck() == true)
			{
				if (Event.current.button == 0)
					this.container.FocusStream(i);
			}

			r.x += r.width;
			r.xMax = xMax;

			return r;
		}

		public override Rect	OnGUI(Rect r)
		{
			this.aware = true;

			float	yOrigin = r.y;
			//float	maxWidth = r.width;
			float	maxHeight = r.height;

			if (HQ.Settings.Get<GeneralSettings>().drawLogTypesInHeader == false)
			{
				// Draw options.
				r = this.DrawOptions(r);
			}

			r.y += Constants.SingleLineHeight + 2F;

			// Draw filters on the right.
			//r.width = maxWidth - r.x;
			//r = this.DrawFilters(r);

			// Draw rows in a new line.
			r.x = 0F;
			//r.y += r.height + 2F;
			r.width = this.console.position.width;
			r.height = maxHeight - (r.y - yOrigin);
			return this.rowsDrawer.DrawRows(r, true);
		}

		public override float	GetOptionWidth()
		{
			GeneralSettings	settings = HQ.Settings.Get<GeneralSettings>();
			float			width = 0F;

			Utility.content.text = "Sort by Error";
			width += settings.MenuButtonStyle.CalcSize(Utility.content).x;
			width += settings.MenuButtonStyle.CalcSize(this.warningContent).x;
			width += settings.MenuButtonStyle.CalcSize(this.errorContent).x;

			return width;
		}

		public override Rect	DrawOptions(Rect r)
		{
			GeneralSettings	general = HQ.Settings.Get<GeneralSettings>();
			float			maxWidth = r.width;

			EditorGUI.BeginChangeCheck();

			Utility.content.text = "Sort by Error";
			r.width = general.MenuButtonStyle.CalcSize(Utility.content).x;
			r.height = Constants.SingleLineHeight;

			this.sortByError = GUI.Toggle(r, this.sortByError, Utility.content, general.MenuButtonStyle);
			r.x += r.width;

			if (this.lastWarningCount != this.warningCount)
			{
				this.lastWarningCount = this.warningCount;
				this.warningContent.text = this.warningCount.ToString();
			}
			r.width = general.MenuButtonStyle.CalcSize(this.warningContent).x;
			this.displayWarning = GUI.Toggle(r, this.displayWarning, this.warningContent, general.MenuButtonStyle);
			r.x += r.width;

			if (this.lastErrorCount != this.errorCount)
			{
				this.lastErrorCount = this.errorCount;
				this.errorContent.text = this.errorCount.ToString();
			}
			r.width = general.MenuButtonStyle.CalcSize(this.errorContent).x;
			this.displayError = GUI.Toggle(r, this.displayError, this.errorContent, general.MenuButtonStyle);
			r.x += r.width;

			// Update Unity Console only if required.
			if (EditorGUI.EndChangeCheck() == true)
			{
				this.Clear();
				this.RefreshFilteredRows();
				Utility.RepaintConsoleWindow();
			}

			r.width = maxWidth - r.width * 4F;

			return r;
		}

		public override void	Clear()
		{
			base.Clear();

			this.compileRows.Clear();
		}

		/// <summary>
		/// Manually reset all counters and arrays.
		/// </summary>
		public override void	RefreshFilteredRows()
		{
			this.totalCount = 0;
			this.warningCount = 0;
			this.errorCount = 0;

			for (int i = 0; i < this.compileRows.Count; i++)
				this.compileRows[i].Clear();

			// Reimport all logs.
			for (int i = 0; i < this.console.rows.Count; i++)
				this.AddLog(i, this.console.rows[i]);

			for (int i = 0; i < this.compileRows.Count; i++)
			{
				if (this.compileRows[i].CountSubLines() == 0)
				{
					this.compileRows.RemoveAt(i);
					--i;
				}
			}

			// Save current selected logs.
			foreach (var vars in this.rowsDrawer.perWindowVars.Each())
			{
				int[]	selectedLogs = vars.GetSelectionArray();

				int	logIndex = 0;
				// Try to restore selected logs.
				for (int i = 0; i < this.rowsDrawer.Count && logIndex < selectedLogs.Length; i++)
				{
					for (int j = logIndex; j < selectedLogs.Length; j++)
					{
						if (selectedLogs[j] == this.rowsDrawer[i])
						{
							vars.AddSelection(selectedLogs[j]);
							++logIndex;
							break;
						}
					}
				}
			}
		}

		public override void	ConsumeLog(int consoleIndex, Row row)
		{
			if ((row.log.mode & (Mode.ScriptCompileError | Mode.ScriptCompileWarning)) != 0)
				row.isConsumed = true;
		}

		public override void	AddLog(int consoleIndex, Row row)
		{
			// After compiling, need to clear the array before receiving brand new logs, because CompilerLog can not know when there is new logs.
			if (this.isCompiling == true)
			{
				this.Clear();
				this.isCompiling = false;
			}

			if ((row.log.mode & (Mode.ScriptCompileError | Mode.ScriptCompileWarning)) != 0)
			{
				this.CountLog(row);
				if (this.CanDisplay(row) == true)
					this.AppendLogToRow(row);
			}
		}

		/// <summary>
		/// <para>Adds a new CompileRow or appends the given <paramref name="row"/> to an existing CompileRow.</para>
		/// <para>From here, we should assume that every logs are compiler outputs, therefore we should not care much about the format and issues when parsing it.</para>
		/// </summary>
		/// <param name="row"></param>
		private void	AppendLogToRow(Row row)
		{
			try
			{
				ILogContentGetter	logContent = row as ILogContentGetter;

				InternalNGDebug.AssertFile(logContent != null, "CompilerLog has received a non-usable Row." + Environment.NewLine + row.log);

				for (int i = 0; i < this.compileRows.Count; i++)
				{
					if (this.compileRows[i].CanAddRow(row) == true)
					{
						this.compileRows[i].AppendRow(row);
						return;
					}
				}

				if (this.sortByError == false)
				{
					CompileRow	crow = new CompileRow(logContent);
					crow.Init(this.console, row.log);
					crow.AppendRow(row);
					this.compileRows.Add(crow);
				}
				else
				{
					ErrorCompileRow	crow = new ErrorCompileRow(logContent);
					crow.Init(this.console, row.log);
					crow.AppendRow(row);
					this.compileRows.Add(crow);
				}

				this.rowsDrawer.Add(this.rowsDrawer.Count);
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogFileException(ex);
			}
		}

		private void	DetectCompile()
		{
			this.isCompiling = EditorApplication.isCompiling;
		}

		Row	IRows.GetRow(int i)
		{
			return this.compileRows[i];
		}

		int	IRows.CountRows()
		{
			return this.compileRows.Count;
		}

		private void	UpdateUnityConsoleOptions()
		{
			UnityLogEntries.SetConsoleFlag((int)ConsoleFlags.LogLevelLog, this.displayLog);
			UnityLogEntries.SetConsoleFlag((int)ConsoleFlags.LogLevelWarning, this.displayWarning);
			UnityLogEntries.SetConsoleFlag((int)ConsoleFlags.LogLevelError, this.displayError);
		}

		private void	UpdateAwareness()
		{
			if (HQ.Settings.Get<MainModuleSettings>().alertOnWarning == false)
			{
				for (int j = 0; j < this.compileRows.Count; j++)
				{
					if (this.compileRows[j].hasError == true)
						return;
				}

				this.aware = true;
			}
			else if (this.lastLogsCount != this.compileRows.Count)
			{
				this.lastLogsCount = this.compileRows.Count;
				this.aware = false;
			}
		}
	}
}