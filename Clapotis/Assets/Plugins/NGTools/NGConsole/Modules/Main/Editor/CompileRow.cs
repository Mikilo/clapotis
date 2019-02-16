using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;

namespace NGToolsEditor.NGConsole
{
	using UnityEngine;

	[Serializable]
	internal class CompileRow : Row, ILogContentGetter
	{
		[Serializable]
		struct ErrorLine
		{
			public readonly int		line;
			public readonly string	error;
			public readonly string	message;
			public readonly bool	isError;

			public	ErrorLine(int line, string error, string message, bool isError)
			{
				this.line = line;
				this.error = error;
				this.message = message;
				this.isError = isError;
			}
		}

		public static Color	SubRowHighlightColor = Color.grey;

		public string	HeadMessage { get { return this.logParser.HeadMessage; } }
		public string	FullMessage { get { return this.logParser.FullMessage; } }
		public string	StackTrace { get { return this.logParser.StackTrace; } }
		public bool		HasStackTrace { get { return this.logParser.HasStackTrace; } }
		public string	Category { get { return this.logParser.Category; } }

		/// <summary>
		/// <para>An array of Frames giving parsed data.</para>
		/// <para>Is generated once on demand.</para>
		/// </summary>
		public Frame[]	Frames { get { return this.logParser.Frames; } }

		public readonly string	file;

		public bool	isOpened;
		public bool	hasError;

		[NonSerialized]
		private int	lastGoToFile;

		[NonSerialized]
		private int	selectedSubRow = -1;
		private List<ErrorLine>	errorLines;
		[NonSerialized]
		private LogConditionParser	logParser;

		public	CompileRow(ILogContentGetter logContent)
		{
			this.isOpened = true;
			this.file = logContent.Frames[0].fileName;
			this.errorLines = new List<ErrorLine>();
		}

		public override void	Init(NGConsoleWindow editor, LogEntry log)
		{
			base.Init(editor, log);

			this.logParser = new LogConditionParser(log);

			this.commands.Add(RowsDrawer.ShortCopyCommand, this.ShortCopy);
			this.commands.Add(RowsDrawer.FullCopyCommand, this.FullCopy);
			this.commands.Add(RowsDrawer.HandleKeyboardCommand, this.HandleKeyboard);
		}

		public override void	Uninit()
		{
			this.logParser.Uninit();
		}

		public override float	GetWidth()
		{
			return 0F;
		}

		public override float	GetHeight(RowsDrawer rowsDrawer)
		{
			if (this.isOpened == false)
				return HQ.Settings.Get<LogSettings>().height;
			else
				return HQ.Settings.Get<LogSettings>().height + this.errorLines.Count * HQ.Settings.Get<LogSettings>().height;
		}

		public override void	DrawRow(RowsDrawer rowsDrawer, Rect r, int streamIndex, bool? collapse)
		{
			LogSettings	settings = HQ.Settings.Get<LogSettings>();
			float		originWidth = RowUtility.drawingWindow.position.width - rowsDrawer.verticalScrollbarWidth;

			// Draw highlight.
			r.x = 0F;
			r.width = originWidth;
			r.height = settings.height;

			this.DrawBackground(rowsDrawer, r, streamIndex);

			r.x += 2F;
			r.width = 16F;

			bool	lastValue = this.isOpened;
			Color	foldoutColor = this.hasError == true ? ConsoleConstants.ErrorFoldoutColor : ConsoleConstants.WarningFoldoutColor;

			using (BgColorContentRestorer.Get(foldoutColor))
			{
				this.isOpened = EditorGUI.Foldout(r, this.isOpened, "");
			}
			if (lastValue != this.isOpened)
			{
				GUI.FocusControl(null);
				rowsDrawer.InvalidateViewHeight();
			}

			r.x -= 2F;
			r.width = 3F;

			EditorGUI.DrawRect(r, foldoutColor);

			r.width = 16F;
			r.x += r.width;
			r.width = originWidth - r.width;

			bool	isSelected = rowsDrawer.currentVars.IsSelected(streamIndex);
			this.HandleDefaultSelection(rowsDrawer, r, streamIndex);
			if (isSelected == false && rowsDrawer.currentVars.IsSelected(streamIndex) == true)
				EditorGUIUtility.PingObject(this.log.instanceID);

			// Toggle on middle-click.
			if (Event.current.type == EventType.MouseDown &&
				Event.current.button == 2 &&
				r.Contains(Event.current.mousePosition) == true)
			{
				this.isOpened = !this.isOpened;
				rowsDrawer.InvalidateViewHeight();
				Event.current.Use();
			}
			// Show menu on right click up.
			else if (Event.current.type == EventType.MouseUp &&
					 Event.current.button == 1 &&
					 r.Contains(Event.current.mousePosition) == true &&
					 rowsDrawer.currentVars.IsSelected(streamIndex) == true)
			{
				GenericMenu	menu = new GenericMenu();

				menu.AddItem(new GUIContent(LC.G("CopyCurrentError")), false, this.MenuCopyCurrentError, this);
				menu.AddItem(new GUIContent(LC.G("CopyAllErrors")), false, this.MenuCopyAllErrors, this);

				if (RowsDrawer.GlobalLogContextMenu != null)
					RowsDrawer.GlobalLogContextMenu(menu, rowsDrawer, streamIndex, this);
				if (rowsDrawer.LogContextMenu != null)
					rowsDrawer.LogContextMenu(menu, this);

				menu.ShowAsContext();

				Event.current.Use();
			}
			else if (Event.current.type == EventType.MouseUp &&
					 Event.current.button == 0 &&
					 r.Contains(Event.current.mousePosition) == true)
			{
				if (string.IsNullOrEmpty(this.file) == false &&
					RowUtility.LastClickTime + Constants.DoubleClickTime > EditorApplication.timeSinceStartup)
				{
					bool	focus = false;

					if ((Event.current.modifiers & settings.forceFocusOnModifier) != 0)
						focus = true;

					if (this.lastGoToFile < this.errorLines.Count)
					{
						RowUtility.GoToFileLine(this.file,
												this.errorLines[this.lastGoToFile].line,
												focus);

						++this.lastGoToFile;
						if (this.lastGoToFile >= this.errorLines.Count)
							this.lastGoToFile = 0;
					}
					else
						this.lastGoToFile = 0;
				}

				RowUtility.LastClickTime = EditorApplication.timeSinceStartup;
			}

			GUI.Label(r, this.file + " (" + this.errorLines.Count + ")", settings.Style);
			r.y += settings.height;

			if (this.isOpened == true)
			{
				for (int j = 0; j < this.errorLines.Count; j++)
				{
					r.x = 0F;
					r.width = originWidth;

					if (Event.current.type == EventType.Repaint &&
						this.selectedSubRow == j &&
						rowsDrawer.currentVars.CountSelection == 1 &&
						rowsDrawer.currentVars.GetSelection(0) == streamIndex)
					{
						EditorGUI.DrawRect(r, CompileRow.SubRowHighlightColor);
					}

					// Handle mouse inputs per log.
					if (r.Contains(Event.current.mousePosition) == true)
					{
						// Toggle on middle click.
						if (Event.current.type == EventType.MouseDown &&
							Event.current.button == 0)
						{
							if (string.IsNullOrEmpty(this.file) == false &&
								RowUtility.LastClickTime + Constants.DoubleClickTime > EditorApplication.timeSinceStartup)
							{
								bool	focus = false;

								if ((Event.current.modifiers & settings.forceFocusOnModifier) != 0)
									focus = true;

								RowUtility.GoToFileLine(this.file,
														this.errorLines[j].line,
														focus);
							}
							else
							{
								rowsDrawer.currentVars.ClearSelection();
								rowsDrawer.currentVars.AddSelection(streamIndex);

								this.selectedSubRow = j;

								this.log.condition = this.errorLines[j].message;
								EditorGUIUtility.PingObject(this.log.instanceID);
							}

							RowUtility.LastClickTime = EditorApplication.timeSinceStartup;

							Event.current.Use();
						}
					}

					// Handle inputs.
					if (this.errorLines[j].line > 0)
					{
						Utility.content.text = this.errorLines[j].line.ToString();
						r.width = settings.Style.CalcSize(Utility.content).x;
						GUI.Label(r,
								  Utility.Color(Utility.content.text, HQ.Settings.Get<StackTraceSettings>().lineColor),
								  settings.Style);
						r.x += r.width;
					}

					Utility.content.text = this.errorLines[j].error;
					r.width = settings.Style.CalcSize(Utility.content).x;
					GUI.Label(r,
							  Utility.Color(this.errorLines[j].error,
											this.errorLines[j].isError == true ? HQ.Settings.Get<MainModuleSettings>().errorColor : HQ.Settings.Get<MainModuleSettings>().warningColor),
							  settings.Style);

					r.x += r.width;
					r.width = originWidth - r.x;
					GUI.Label(r, this.errorLines[j].message, settings.Style);

					r.y += settings.height;
				}
			}
		}

		public virtual void	AppendRow(Row row)
		{
			if (this.hasError == false &&
				(row.log.mode & Mode.ScriptCompileError) != 0)
			{
				this.hasError = true;
			}

			string	raw = (row as ILogContentGetter).FullMessage;

			// Handle message with no file or line as prefix.
			if (row.log.line == 0)
			{
				if (raw.Contains("error CS") == true || raw.Contains("warning CS") == true)
				{
					int	comma = raw.IndexOf(':');
					if (comma == -1)
						return;

					string	error = raw.Substring(0, comma);
					string	message = raw.Substring(comma + 2); // Remove the comma and its following space.

					this.errorLines.Add(new ErrorLine(row.log.line,
													  error.Substring(error.IndexOf(' ') + 1), // Just keep the error number.
													  message,
													  error.Contains("error")));
				}
			}
			else
			{
				int	comma = raw.IndexOf(':') + 2;

				// Totaly empty log is possible! Yes it is! No stacktrace, no file, no line, no log at all!
				// Bug ref #718608_dh6en6gdivrpuv0q
				if (comma == 1)
					return;

				raw = raw.Substring(comma); // Skip ':' and ' ' after "File(Line,Col)"

				// Separate the error number and the message.
				comma = raw.IndexOf(':');

				if (comma == -1)
					return;

				string	error = raw.Substring(0, comma);
				string	message = raw.Substring(comma + 2); // Remove the comma and its following space.

				this.errorLines.Add(new ErrorLine(row.log.line,
												  error.Substring(error.IndexOf(' ') + 1), // Just keep the error number.
												  message,
												  error.Contains("error")));
			}
		}

		public virtual bool	CanAddRow(Row row)
		{
			ILogContentGetter	logContent = row as ILogContentGetter;

			return this.file == logContent.Frames[0].fileName;
		}

		public virtual void	Clear()
		{
			this.errorLines.Clear();
		}

		public virtual int	CountSubLines()
		{
			return this.errorLines.Count;
		}

		private object	ShortCopy(object row)
		{
			if (this.selectedSubRow < 0 || this.selectedSubRow >= this.errorLines.Count)
				return string.Empty;

			ErrorLine	error = this.errorLines[this.selectedSubRow];

			return this.file + " " + error.line + " " + error.error + ' ' + error.message;
		}

		private object	FullCopy(object row)
		{
			StringBuilder	buffer = Utility.GetBuffer();

			buffer.AppendLine(this.file);

			for (int i = 0; i < this.errorLines.Count; i++)
				buffer.AppendLine(this.errorLines[i].line + " " + this.errorLines[i].error + ' ' + this.errorLines[i].message);

			buffer.Length -= Environment.NewLine.Length;

			return Utility.ReturnBuffer(buffer);
		}

		private void	MenuCopyCurrentError(object row)
		{
			EditorGUIUtility.systemCopyBuffer = this.ShortCopy(null) as string;
		}

		private void	MenuCopyAllErrors(object row)
		{
			EditorGUIUtility.systemCopyBuffer = this.FullCopy(null) as string;
		}

		private object	HandleKeyboard(object data)
		{
			RowsDrawer	rowsDrawer = data as RowsDrawer;

			if (Event.current.type == EventType.KeyDown)
			{
				ConsoleSettings	settings = HQ.Settings.Get<ConsoleSettings>();

				if (settings.inputsManager.Check("Navigation", ConsoleConstants.CloseLogCommand) == true)
				{
					if (this.isOpened == true)
					{
						this.isOpened = false;
						rowsDrawer.InvalidateViewHeight();
						RowUtility.drawingWindow.Repaint();
					}
				}
				else if (settings.inputsManager.Check("Navigation", ConsoleConstants.OpenLogCommand) == true)
				{
					if (this.isOpened == false)
					{
						this.isOpened = true;
						rowsDrawer.InvalidateViewHeight();
						RowUtility.drawingWindow.Repaint();
					}
				}
				else if (settings.inputsManager.Check("Navigation", ConsoleConstants.GoToLineCommand) == true &&
						 rowsDrawer.currentVars.CountSelection == 1 &&
						 this.Frames.Length > 0)
				{
					string	fileName = this.Frames[0].fileName;
					int		line = this.Frames[0].line;
					bool	focus = (Event.current.modifiers & HQ.Settings.Get<LogSettings>().forceFocusOnModifier) != 0;

					RowUtility.GoToFileLine(fileName, line, focus);
					RowUtility.drawingWindow.Repaint();
					Event.current.Use();
				}
			}

			return null;
		}
	}
}