using NGTools;
using System;
using UnityEditor;

namespace NGToolsEditor.NGConsole
{
	using UnityEngine;

	[Serializable]
	[RowLogHandler(20)]
	internal sealed class DataRow : Row, ILogContentGetter
	{
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

		private string	firstLine;
		private string	fields;
		private float	fieldsHeight;

		private static bool	CanDealWithIt(UnityLogEntry log)
		{
			return log.condition[0] == InternalNGDebug.DataStartChar;
		}

		public override void	Init(NGConsoleWindow editor, LogEntry log)
		{
			base.Init(editor, log);

			this.logParser = new LogConditionParser(this.log);

			this.commands.Add(RowsDrawer.ShortCopyCommand, this.ShortCopy);
			this.commands.Add(RowsDrawer.FullCopyCommand, this.FullCopy);
			this.commands.Add(RowsDrawer.CopyStackTraceCommand, this.CopyStackTrace);
			this.commands.Add(RowsDrawer.HandleKeyboardCommand, this.HandleKeyboard);

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
			if (rowsDrawer.rowsData.ContainsKey(this) == true)
				return HQ.Settings.Get<LogSettings>().height + this.fieldsHeight;
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
			EditorGUI.DrawRect(r, Color.gray);
			bool	isOpened = rowsDrawer.rowsData.ContainsKey(this);
			EditorGUI.BeginChangeCheck();
			EditorGUI.Foldout(r, isOpened, "");
			if (EditorGUI.EndChangeCheck() == true && this.fields != string.Empty)
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

					if (this.fields != string.Empty)
					{
						if (isOpened == false)
							rowsDrawer.rowsData.Add(this, true);
						else
							rowsDrawer.rowsData.Remove(this);
					}
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
					menu.AddItem(new GUIContent(LC.G("CopyFields")), false, rowsDrawer.MenuCopyLog, this);

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

			GUI.Label(r, this.firstLine);

			if (isOpened == true)
			{
				r.y += r.height;
				r.x = 16F;
				r.width = originWidth - 16F;
				r.height = this.fieldsHeight;
				EditorGUI.TextArea(r, this.fields, GeneralStyles.RichTextArea);
			}
		}

		private object	ShortCopy(object row)
		{
			return this.firstLine;
		}

		private object	FullCopy(object row)
		{
			return this.fields;
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

		/// <summary>
		/// Prepares the row by parsing its log.
		/// </summary>
		private void	ParseLog()
		{
			InternalNGDebug.AssertFile(this.isParsed == false, "Parsed Row is being parsed again.");

			this.isParsed = true;

			int		end = this.log.condition.IndexOf(InternalNGDebug.DataEndChar);
			string	raw = this.log.condition.Substring(1, end - 1);
			int		n = raw.IndexOf(InternalNGDebug.DataSeparator);

			if (n != -1)
			{
				this.firstLine = raw.Substring(0, n).Replace(InternalNGDebug.DataSeparatorReplace, InternalNGDebug.DataSeparator);
				this.fields = raw.Substring(n + 1).Replace(InternalNGDebug.DataSeparatorReplace, InternalNGDebug.DataSeparator);

				Utility.content.text = this.fields;
				this.fieldsHeight = GeneralStyles.RichTextArea.CalcHeight(Utility.content, 0F);
			}
			else
			{
				this.firstLine = raw.Replace(InternalNGDebug.DataSeparatorReplace, InternalNGDebug.DataSeparator);
				this.fields = string.Empty;
				this.fieldsHeight = 0F;
			}
		}
	}
}