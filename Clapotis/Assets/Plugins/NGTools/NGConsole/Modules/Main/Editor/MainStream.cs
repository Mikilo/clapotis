using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	[Serializable]
	[Exportable(ExportableAttribute.ArrayOptions.Overwrite | ExportableAttribute.ArrayOptions.Immutable)]
	public class MainStream : StreamLog
	{
		[Exportable]
		public bool	collapse;

		public	MainStream()
		{
			this.name = "Main";
			this.addConsumedLog = true;
		}

		public override void	Init(NGConsoleWindow console, IStreams container)
		{
			base.Init(console, container);

			this.OptionAltered += this.UpdateUnityConsoleOptions;
		}

		public override void	Uninit()
		{
			base.Uninit();

			this.OptionAltered -= this.UpdateUnityConsoleOptions;
		}

		public override Rect	OnTabGUI(Rect r, int i)
		{
			if (this.lastTotalCount != this.totalCount)
			{
				this.lastTotalCount = this.totalCount;
				this.tabLabel.text = this.name + " (" + this.totalCount + ")";
			}

			GeneralSettings	settings = HQ.Settings.Get<GeneralSettings>();
			float	xMax = r.xMax;
			r.width = settings.MenuButtonStyle.CalcSize(this.tabLabel).x;

			EditorGUI.BeginChangeCheck();
			GUI.Toggle(r, i == this.container.WorkingStream, this.tabLabel, settings.MenuButtonStyle);
			if (EditorGUI.EndChangeCheck() == true)
			{
				if (Event.current.button == 0)
					this.container.FocusStream(i);
			}

			r.x += r.width;
			r.xMax = xMax;

			return r;
		}

		public override Rect	OnGUI(UnityEngine.Rect r)
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
			return this.rowsDrawer.DrawRows(r, true);
		}

		public override float	GetOptionWidth()
		{
			GeneralSettings	settings = HQ.Settings.Get<GeneralSettings>();
			float			width = 0F;

			width += settings.MenuButtonStyle.CalcSize(this.logContent).x;
			width += settings.MenuButtonStyle.CalcSize(this.warningContent).x;
			width += settings.MenuButtonStyle.CalcSize(this.errorContent).x;
			if (settings.differentiateException == true)
				width += settings.MenuButtonStyle.CalcSize(this.exceptionContent).x;

			return width;
		}

		public override Rect	DrawOptions(Rect r)
		{
			GeneralSettings	general = HQ.Settings.Get<GeneralSettings>();
			float			maxWidth = r.xMax;

			EditorGUI.BeginChangeCheck();
			if (this.lastLogCount != this.logCount)
			{
				this.lastLogCount = this.logCount;
				this.logContent.text = this.logCount.ToString();
			}
			r.height = Constants.SingleLineHeight;
			r.width = general.MenuButtonStyle.CalcSize(this.logContent).x;
			this.displayLog = GUI.Toggle(r, this.displayLog, this.logContent, general.MenuButtonStyle);
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

			if (general.differentiateException == true)
			{
				if (this.lastExceptionCount != this.exceptionCount)
				{
					this.lastExceptionCount = this.exceptionCount;
					this.exceptionContent.text = this.exceptionCount.ToString();
				}
				r.width = general.MenuButtonStyle.CalcSize(this.exceptionContent).x;
				using (ColorContentRestorer.Get(ConsoleConstants.ExceptionFoldoutColor))
				{
					this.displayException = GUI.Toggle(r, this.displayException, this.exceptionContent, general.MenuButtonStyle);
				}
				r.x += r.width;
			}

			// Update Unity Console only if required.
			if (EditorGUI.EndChangeCheck() == true)
			{
				this.RefreshFilteredRows();
				this.OnOptionAltered();

				Utility.RepaintConsoleWindow();
			}

			r.width = maxWidth - r.x;

			return r;
		}

		public override void	ConsumeLog(int consoleIndex, Row row)
		{
		}

		public override void	AddLog(int consoleIndex, Row row)
		{
			// MainLog does not receive output from compiler.
			if ((row.log.mode & (Mode.ScriptCompileError | Mode.ScriptCompileWarning)) != 0)
				return;

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
				}
			}
		}

		private void	UpdateUnityConsoleOptions()
		{
			UnityLogEntries.SetConsoleFlag((int)ConsoleFlags.Collapse, this.collapse);
			UnityLogEntries.SetConsoleFlag((int)ConsoleFlags.LogLevelLog, this.displayLog);
			UnityLogEntries.SetConsoleFlag((int)ConsoleFlags.LogLevelWarning, this.displayWarning);
			UnityLogEntries.SetConsoleFlag((int)ConsoleFlags.LogLevelError, this.displayError);
		}
	}
}