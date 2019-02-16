using NGTools;
using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	[Serializable]
	[RowLogHandler(30)]
	internal sealed class JSONRow : Row, ILogContentGetter, IRowDotColored
	{
		internal const char					JSONValueSeparator = ',';
		internal const char					JSONKeySeparator = ':';
		internal const string				CopyActualExplodedJSONCommand = "CopyActualExplodedJSON";
		internal const string				CopyFullExplodedJSONCommand = "CopyFullExplodedJSON";
		internal static readonly string[]	Openers = new string[] { "{", "[" };
		internal static readonly string[]	Closers = new string[] { "}", "]" };
		internal static readonly string[]	OpenClose = new string[] { "{ … }", "[ … ]" };
		internal static readonly string[]	OpenCloseEmpty = new string[] { "{ }", "[ ]" };
		internal static readonly Color		FoldColor = Color.magenta * .4F;

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

		/// <summary>Defines if the Row is ready to be used. Do never use a main value (Head message, full message, stack trace) from non-ready Row! IsParsed is used to delay or to skip non-essential Row from computation when receiving massive logs.</summary>
		public bool	isParsed;

		private LogConditionParser	logParser;

		private string		firstLine;
		private string		json;
		private IJSONObject	root;

		private static bool	CanDealWithIt(UnityLogEntry log)
		{
			return log.condition[0] == InternalNGDebug.JSONStartChar;
		}

		public override void	Init(NGConsoleWindow editor, LogEntry log)
		{
			base.Init(editor, log);

			this.logParser = new LogConditionParser(this.log);

			this.commands.Add(RowsDrawer.ShortCopyCommand, this.ShortCopy);
			this.commands.Add(RowsDrawer.FullCopyCommand, this.FullCopy);
			this.commands.Add(RowsDrawer.CopyStackTraceCommand, this.CopyStackTrace);
			this.commands.Add(RowsDrawer.HandleKeyboardCommand, this.HandleKeyboard);
			this.commands.Add(JSONRow.CopyActualExplodedJSONCommand, this.CopyActualExplodedJSON);
			this.commands.Add(JSONRow.CopyFullExplodedJSONCommand, this.FullCopy);

			this.isParsed = false;
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
			if (rowsDrawer.rowsData.ContainsKey(this) == true && this.root != null)
				return HQ.Settings.Get<LogSettings>().height + this.root.GetHeight();
			return HQ.Settings.Get<LogSettings>().height;
		}

		public override void	DrawRow(RowsDrawer rowsDrawer, Rect r, int streamIndex, bool? collapse)
		{
			if (this.isParsed == false)
				this.ParseLog();

			float		originWidth = RowUtility.drawingWindow.position.width - rowsDrawer.verticalScrollbarWidth;
			LogSettings	settings = HQ.Settings.Get<LogSettings>();

			// Draw highlight.
			r.x = 0F;
			r.width = originWidth;
			r.height = settings.height;

			this.DrawBackground(rowsDrawer, r, streamIndex);

			r.width = 16F;
			EditorGUI.DrawRect(r, JSONRow.FoldColor);
			bool	isOpened = rowsDrawer.rowsData.ContainsKey(this);
			EditorGUI.BeginChangeCheck();
			EditorGUI.Foldout(r, isOpened, "");
			if (EditorGUI.EndChangeCheck() == true)
			{
				if (isOpened == false)
					rowsDrawer.rowsData.Add(this, true);
				else
					rowsDrawer.rowsData.Remove(this);

				isOpened = !isOpened;
				rowsDrawer.InvalidateViewHeight();
			}
			r.x = r.width;
			r.width = originWidth - r.x;

			// Handle mouse inputs.
			if (r.Contains(Event.current.mousePosition) == true)
			{
				// Toggle on middle click.
				if (Event.current.type == EventType.MouseDown &&
					Event.current.button == 2)
				{
					if (rowsDrawer.currentVars.IsSelected(streamIndex) == false)
					{
						if (Event.current.control == false)
							rowsDrawer.currentVars.ClearSelection();

						rowsDrawer.currentVars.AddSelection(streamIndex);

						if (Event.current.control == false)
							rowsDrawer.FitFocusedLogInScreen(streamIndex);
					}

					if (isOpened == false)
						rowsDrawer.rowsData.Add(this, true);
					else
						rowsDrawer.rowsData.Remove(this);
					isOpened = !isOpened;

					rowsDrawer.InvalidateViewHeight();
					Event.current.Use();
				}
				// Show menu on right click up.
				else if (Event.current.type == EventType.MouseUp &&
						 Event.current.button == 1 &&
						 rowsDrawer.currentVars.IsSelected(streamIndex) == true)
				{
					GenericMenu	menu = new GenericMenu();

					menu.AddItem(new GUIContent(LC.G("CopyLine")), false, rowsDrawer.MenuCopyLine, this);
					menu.AddItem(new GUIContent(LC.G("CopyActualExplodedJSON")), false, this.CopyAllActualExplodedJSON, rowsDrawer);
					menu.AddItem(new GUIContent(LC.G("CopyFullExplodedJSON")), false, this.CopyAllFullExplodedJSON, rowsDrawer);
					menu.AddItem(new GUIContent(LC.G("ViewJSON")), false, this.ViewJSON);

					if (string.IsNullOrEmpty(this.StackTrace) == false)
						menu.AddItem(new GUIContent(LC.G("CopyStackTrace")), false, rowsDrawer.MenuCopyStackTrace, this);

					menu.AddItem(new GUIContent("Advance Copy	Shift+C"), false, rowsDrawer.OpenAdvanceCopy);

					if (rowsDrawer.currentVars.CountSelection >= 2)
						menu.AddItem(new GUIContent(LC.G("ExportSelection")), false, rowsDrawer.MenuExportSelection, this);

					if (RowsDrawer.GlobalLogContextMenu != null)
						RowsDrawer.GlobalLogContextMenu(menu, rowsDrawer, streamIndex, this);
					if (rowsDrawer.LogContextMenu != null)
						rowsDrawer.LogContextMenu(menu, this);

					menu.ShowAsContext();

					Event.current.Use();
				}
				// Focus on right click down.
				else if (Event.current.type == EventType.MouseDown &&
						 Event.current.button == 1)
				{
					// Handle multi-selection.
					if (Event.current.control == true)
					{
						if (rowsDrawer.currentVars.IsSelected(streamIndex) == false)
							rowsDrawer.currentVars.AddSelection(streamIndex);
					}
					else
					{
						if (rowsDrawer.currentVars.IsSelected(streamIndex) == false)
						{
							rowsDrawer.currentVars.ClearSelection();
							rowsDrawer.currentVars.AddSelection(streamIndex);
						}

						rowsDrawer.FitFocusedLogInScreen(streamIndex);
					}

					Event.current.Use();
				}
				// Focus on left click.
				else if (Event.current.type == EventType.MouseDown &&
						 Event.current.button == 0)
				{
					// Set the selection to the log's object if available.
					if (this.log.instanceID != 0 &&
						(Event.current.modifiers & settings.selectObjectOnModifier) != 0)
					{
						Selection.activeInstanceID = this.log.instanceID;
					}
					// Go to line if force focus is available.
					else if ((Event.current.modifiers & settings.forceFocusOnModifier) != 0)
						RowUtility.GoToLine(this, this.log, true);

					// Handle multi-selection.
					if (Event.current.control == true &&
						rowsDrawer.currentVars.IsSelected(streamIndex) == true)
					{
						rowsDrawer.currentVars.RemoveSelection(streamIndex);
					}
					else
					{
						if (Event.current.shift == true)
							rowsDrawer.currentVars.WrapSelection(streamIndex);
						else if (Event.current.control == false)
						{
							if (settings.alwaysDisplayLogContent == false && rowsDrawer.currentVars.CountSelection != 1)
								rowsDrawer.bodyRect.height -= rowsDrawer.currentVars.rowContentHeight + ConsoleConstants.RowContentSplitterHeight;
							else
							{
								// Reset last click when selection changes.
								if (rowsDrawer.currentVars.IsSelected(streamIndex) == false)
									RowUtility.LastClickTime = 0;
							}
							rowsDrawer.currentVars.ClearSelection();
						}

						if (Event.current.shift == false)
							rowsDrawer.currentVars.AddSelection(streamIndex);

						if (Event.current.control == false)
							rowsDrawer.FitFocusedLogInScreen(streamIndex);

						GUI.FocusControl(null);
						Event.current.Use();
					}
				}
				// Handle normal behaviour on left click up.
				else if (Event.current.type == EventType.MouseUp &&
						 Event.current.button == 0)
				{
					// Go to line on double click.
					if (RowUtility.LastClickTime + Constants.DoubleClickTime > EditorApplication.timeSinceStartup &&
						RowUtility.LastClickIndex == streamIndex &&
						rowsDrawer.currentVars.IsSelected(streamIndex) == true)
					{
						bool	focus = false;

						if ((Event.current.modifiers & settings.forceFocusOnModifier) != 0)
							focus = true;

						RowUtility.GoToLine(this, this.log, focus);
					}
					// Ping on simple click.
					else if (this.log.instanceID != 0)
						EditorGUIUtility.PingObject(this.log.instanceID);

					RowUtility.LastClickTime = EditorApplication.timeSinceStartup;
					RowUtility.LastClickIndex = streamIndex;

					RowUtility.drawingWindow.Repaint();
					Event.current.Use();
				}
			}

			r = this.DrawPreLogData(rowsDrawer, r);
			r.width = originWidth - r.x;

			GUI.Label(r, this.firstLine, settings.Style);

			if (isOpened == true)
			{
				if (this.root == null)
					this.ParseJSON();

				r.y += r.height;
				r.x = 16F;
				r.width = originWidth - 16F;
				r.height = this.root.GetHeight();

				EditorGUI.BeginChangeCheck();
				this.root.Draw(r);
				if (EditorGUI.EndChangeCheck() == true)
					rowsDrawer.InvalidateViewHeight();

				if (Event.current.type == EventType.MouseMove)
					this.editor.Repaint();
			}
		}

		private object	ShortCopy(object row)
		{
			return this.firstLine;
		}

		private object	FullCopy(object row)
		{
			if (this.root == null)
				this.ParseJSON();

			StringBuilder	buffer = Utility.GetBuffer();

			this.root.Copy(buffer, true);

			return Utility.ReturnBuffer(buffer);
		}

		private object	CopyStackTrace(object row)
		{
			return this.logParser.StackTrace;
		}

		private object	HandleKeyboard(object data)
		{
			RowsDrawer	rowsDrawer = data as RowsDrawer;

			if (Event.current.type == EventType.KeyDown)
			{
				if (HQ.Settings.Get<ConsoleSettings>().inputsManager.Check("Navigation", ConsoleConstants.CloseLogCommand) == true)
				{
					if (rowsDrawer.rowsData.ContainsKey(this) == true)
					{
						rowsDrawer.rowsData.Remove(this);
						rowsDrawer.InvalidateViewHeight();
						RowUtility.drawingWindow.Repaint();
					}
				}
				else if (HQ.Settings.Get<ConsoleSettings>().inputsManager.Check("Navigation", ConsoleConstants.OpenLogCommand) == true)
				{
					if (rowsDrawer.rowsData.ContainsKey(this) == false)
					{
						rowsDrawer.rowsData.Add(this, true);
						rowsDrawer.InvalidateViewHeight();
						RowUtility.drawingWindow.Repaint();
					}
				}
			}

			return null;
		}

		private void	CopyAllActualExplodedJSON(object _rowsDrawer)
		{
			RowsDrawer		rowsDrawer = _rowsDrawer as RowsDrawer;
			StringBuilder	buffer = Utility.GetBuffer();

			rowsDrawer.currentVars.selectedLogs.Sort();
			foreach (int streamIndex in rowsDrawer.currentVars.selectedLogs)
			{
				Row	r = rowsDrawer.rows.GetRow(rowsDrawer[streamIndex]);
				buffer.AppendLine(r.Command(JSONRow.CopyActualExplodedJSONCommand, r).ToString());
			}

			if (buffer.Length > Environment.NewLine.Length)
				buffer.Length -= Environment.NewLine.Length;

			EditorGUIUtility.systemCopyBuffer = (string)Utility.ReturnBuffer(buffer);
		}

		private void	CopyAllFullExplodedJSON(object _rowsDrawer)
		{
			RowsDrawer		rowsDrawer = _rowsDrawer as RowsDrawer;
			StringBuilder	buffer = Utility.GetBuffer();

			rowsDrawer.currentVars.selectedLogs.Sort();
			foreach (int streamIndex in rowsDrawer.currentVars.selectedLogs)
			{
				Row	r = rowsDrawer.rows.GetRow(rowsDrawer[streamIndex]);
				buffer.AppendLine(r.Command(JSONRow.CopyFullExplodedJSONCommand, r).ToString());
			}

			if (buffer.Length > Environment.NewLine.Length)
				buffer.Length -= Environment.NewLine.Length;

			EditorGUIUtility.systemCopyBuffer = (string)Utility.ReturnBuffer(buffer);
		}

		private object	CopyActualExplodedJSON(object row)
		{
			if (this.root == null)
				this.ParseJSON();

			StringBuilder	buffer = Utility.GetBuffer();

			this.root.Copy(buffer, false);

			return Utility.ReturnBuffer(buffer);
		}

		private void	ViewJSON()
		{
			ViewTextWindow.Start((string)this.FullCopy(null));
		}

		private void	ParseJSON()
		{
			if (this.root == null)
			{
				try
				{
					this.root = new JSONRoot(this.json);
				}
				catch
				{
					this.root = new JSONRoot(string.Empty);
					throw;
				}
			}
		}

		/// <summary>
		/// Prepares the row by parsing its log.
		/// </summary>
		private void	ParseLog()
		{
			InternalNGDebug.AssertFile(this.isParsed == false, "Parsed Row is being parsed again.");

			this.isParsed = true;

			int	n = this.log.condition.IndexOf(InternalNGDebug.JSONStartChar);
			int	m = this.log.condition.IndexOf(InternalNGDebug.JSONSeparator);
			int	o = this.log.condition.IndexOf(InternalNGDebug.JSONEndChar);

			if (n != -1 && m != -1)
				this.firstLine = this.log.condition.Substring(n + 1, m - n - 1);
			if (m != -1)
			{
				if (o != -1)
					this.json = this.log.condition.Substring(m + 1, o - m - 1);
				else
					this.json = this.log.condition.Substring(m + 1);
			}
			else
				this.json = string.Empty;
			if (string.IsNullOrEmpty(this.firstLine) == true)
				this.firstLine = this.json;
		}

		Color	IRowDotColored.GetColor()
		{
			return JSONRow.FoldColor;
		}
	}
}