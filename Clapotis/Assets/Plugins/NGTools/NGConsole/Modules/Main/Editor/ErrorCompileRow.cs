using System;
using System.Collections.Generic;
using UnityEditor;

namespace NGToolsEditor.NGConsole
{
	using UnityEngine;

	[Serializable]
	internal sealed class ErrorCompileRow : CompileRow, ILogContentGetter
	{
		public struct FileLine
		{
			public readonly int		line;
			public readonly string	file;
			public readonly string	message;
			public readonly bool	isError;

			public	FileLine(int line, string file, string message, bool isError)
			{
				this.line = line;
				this.file = file;
				this.message = message;
				this.isError = isError;
			}
		}

		public readonly string	error;
		private readonly string	lookupError;

		[NonSerialized]
		private int	lastGoToFile;

		[NonSerialized]
		private int	selectedSubRow = -1;
		[NonSerialized]
		private List<FileLine>	fileLines;

		public	ErrorCompileRow(ILogContentGetter logContent) : base(logContent)
		{
			this.fileLines = new List<FileLine>();
			this.isOpened = true;

			string	raw = logContent.FullMessage;

			this.hasError = raw.Contains("error");

			// Handle message with no file or line as prefix.
			if (raw.StartsWith("error") || raw.StartsWith("warning") || logContent.Frames.Length == 0)
			{
				if (raw.Contains("error CS") == true || raw.Contains("warning CS") == true)
				{
					int	comma = raw.IndexOf(':');
					if (comma == -1)
						return;

					this.error = raw.Substring(0, comma);
					this.error = this.error.Substring(this.error.IndexOf(' ') + 1); // Just keep the error number.
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

				this.error = raw.Substring(0, comma);
				this.error = this.error.Substring(this.error.IndexOf(' ') + 1); // Just keep the error number.
			}

			this.lookupError = (this.hasError == true ? "error " : "warning ") + this.error + ":";
		}

		public override float	GetHeight(RowsDrawer rowsDrawer)
		{
			if (this.isOpened == false)
				return HQ.Settings.Get<LogSettings>().height;
			else
				return HQ.Settings.Get<LogSettings>().height + this.fileLines.Count * HQ.Settings.Get<LogSettings>().height;
		}

		public override void	DrawRow(RowsDrawer rowsDrawer, Rect r, int i, bool? collapse)
		{
			LogSettings	settings = HQ.Settings.Get<LogSettings>();
			float		originWidth = RowUtility.drawingWindow.position.width - rowsDrawer.verticalScrollbarWidth;

			// Draw highlight.
			r.x = 0F;
			r.width = originWidth;
			r.height = settings.height;

			this.DrawBackground(rowsDrawer, r, i);

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

			this.HandleDefaultSelection(rowsDrawer, r, i);

			// Toggle on middle-click.
			if (r.Contains(Event.current.mousePosition) == true &&
				Event.current.type == EventType.MouseDown &&
				Event.current.button == 2)
			{
				this.isOpened = !this.isOpened;
				rowsDrawer.InvalidateViewHeight();
				Event.current.Use();
			}
			else if (r.Contains(Event.current.mousePosition) == true &&
					 Event.current.type == EventType.MouseUp &&
					 Event.current.button == 0)
			{
				if (string.IsNullOrEmpty(this.file) == false &&
					RowUtility.LastClickTime + Constants.DoubleClickTime > EditorApplication.timeSinceStartup)
				{
					bool	focus = false;

					if ((Event.current.modifiers & settings.forceFocusOnModifier) != 0)
						focus = true;

					if (this.lastGoToFile < this.fileLines.Count)
					{
						RowUtility.GoToFileLine(this.file,
												this.fileLines[this.lastGoToFile].line,
												focus);

						++this.lastGoToFile;
						if (this.lastGoToFile >= this.fileLines.Count)
							this.lastGoToFile = 0;
					}
					else
						this.lastGoToFile = 0;
				}

				RowUtility.LastClickTime = EditorApplication.timeSinceStartup;
			}

			GUI.Label(r, this.error + " (" + this.fileLines.Count + ")", settings.Style);
			r.y += settings.height;

			if (this.isOpened == true)
			{
				for (int j = 0; j < this.fileLines.Count; j++)
				{
					r.x = 0F;
					r.width = originWidth;

					if (Event.current.type == EventType.Repaint &&
						this.selectedSubRow == j &&
						rowsDrawer.currentVars.CountSelection > 0 &&
						rowsDrawer.currentVars.GetSelection(0) == i)
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

								RowUtility.GoToFileLine(this.fileLines[j].file,
														this.fileLines[j].line,
														focus);
							}
							else
							{
								rowsDrawer.currentVars.ClearSelection();
								rowsDrawer.currentVars.AddSelection(i);

								this.selectedSubRow = j;

								this.log.condition = this.fileLines[j].message;
							}

							RowUtility.LastClickTime = EditorApplication.timeSinceStartup;

							Event.current.Use();
						}
					}

					// Handle inputs.
					if (this.fileLines[j].line > 0)
					{
						Utility.content.text = this.fileLines[j].line.ToString();
						r.width = settings.Style.CalcSize(Utility.content).x;
						GUI.Label(r,
								  Utility.Color(Utility.content.text, HQ.Settings.Get<StackTraceSettings>().lineColor),
								  settings.Style);
						r.x += r.width;
					}

					r.width = originWidth - r.x;
					GUI.Label(r, this.fileLines[j].message, settings.Style);

					r.y += settings.height;
				}
			}
		}

		public override void	AppendRow(Row row)
		{
			string	raw = (row as ILogContentGetter).FullMessage;

			// Handle message with no file or line as prefix.
			if (row.log.line == 0)
			{
				if (raw.Contains("error CS") == true || raw.Contains("warning CS") == true)
				{
					int	comma = raw.IndexOf(':');
					if (comma == -1)
						return;

					string	message = raw.Substring(comma + 2); // Remove the comma and its following space.

					this.fileLines.Add(new FileLine(row.log.line,
													row.log.file,
													message,
													this.hasError));
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

				string	message = raw.Substring(comma + 2); // Remove the comma and its following space.

				this.fileLines.Add(new FileLine(row.log.line,
												row.log.file,
												message,
												this.hasError));
			}
		}

		public override bool	CanAddRow(Row row)
		{
			ILogContentGetter	logContent = row as ILogContentGetter;

			return logContent.HeadMessage.Contains(this.lookupError);
		}

		public override void	Clear()
		{
			base.Clear();

			this.fileLines.Clear();
		}

		public override int		CountSubLines()
		{
			return this.fileLines.Count;
		}
	}
}