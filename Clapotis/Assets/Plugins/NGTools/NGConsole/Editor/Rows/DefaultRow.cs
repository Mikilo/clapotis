using System;
using UnityEditor;
using UnityEditorInternal;

namespace NGToolsEditor.NGConsole
{
	using UnityEngine;

	[Serializable]
	[RowLogHandler(0)]
	internal class DefaultRow : Row, ILogContentGetter
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

		[NonSerialized]
		protected LogConditionParser	logParser;

		[NonSerialized]
		private Texture2D	icon;

		private static bool	CanDealWithIt(UnityLogEntry log)
		{
			return true;
		}

		public override void	Init(NGConsoleWindow editor, LogEntry log)
		{
			base.Init(editor, log);

			this.logParser = new LogConditionParser(this.log);

			this.commands.Add(RowsDrawer.ShortCopyCommand, this.ShortCopy);
			this.commands.Add(RowsDrawer.FullCopyCommand, this.FullCopy);
			this.commands.Add(RowsDrawer.CopyStackTraceCommand, this.CopyStackTrace);
			this.commands.Add(RowsDrawer.HandleKeyboardCommand, this.HandleKeyboard);
		}

		public override void	Uninit()
		{
			this.logParser.Uninit();

			this.icon = null;
		}

		public override float	GetWidth()
		{
			return 0F;
		}

		public override float	GetHeight(RowsDrawer rowsDrawer)
		{
			if (rowsDrawer.rowsData.ContainsKey(this) == true)
				return HQ.Settings.Get<LogSettings>().height + HQ.Settings.Get<StackTraceSettings>().height * this.Frames.Length;
			return HQ.Settings.Get<LogSettings>().height;
		}

		public override void	DrawRow(RowsDrawer rowsDrawer, Rect r, int streamIndex, bool? collapse)
		{
			LogSettings	settings = HQ.Settings.Get<LogSettings>();
			float		originWidth = r.width - rowsDrawer.verticalScrollbarWidth/* + rowsDrawer.currentVars.scrollX*/;

			// Draw highlight.
			//r.x = rowsDrawer.currentVars.scrollX;
			r.width = originWidth;
			r.height = settings.height;

			this.DrawBackground(rowsDrawer, r, streamIndex);

			// Handle row events.
			if (r.Contains(Event.current.mousePosition) == true)
			{
				if (Event.current.type == EventType.MouseMove ||
					Event.current.type == EventType.MouseDrag)
				{
					if (Event.current.type == EventType.MouseDrag &&
						Utility.position2D != Vector2.zero &&
						DragAndDrop.GetGenericData(Utility.DragObjectDataName) != null &&
						(Utility.position2D - Event.current.mousePosition).sqrMagnitude >= Constants.MinStartDragDistance)
					{
						Utility.position2D = Vector2.zero;
						DragAndDrop.StartDrag("Drag Object");
					}

					if (rowsDrawer.RowHovered != null)
					{
						r.y -= rowsDrawer.currentVars.scrollbar.Offset;
						rowsDrawer.RowHovered(r, this);
						r.y += rowsDrawer.currentVars.scrollbar.Offset;
					}
				}
				else if (Event.current.type == EventType.MouseDown)
				{
					if (rowsDrawer.RowClicked != null)
					{
						r.y -= rowsDrawer.currentVars.scrollbar.Offset;
						rowsDrawer.RowClicked(r, this);
						r.y += rowsDrawer.currentVars.scrollbar.Offset;
					}
				}
			}
			//r.x = 0F;

			if (RowsDrawer.GlobalBeforeFoldout != null)
			{
				//r.width = originWidth - r.x;
				r = RowsDrawer.GlobalBeforeFoldout(rowsDrawer, r, streamIndex, this);
			}
			if (rowsDrawer.BeforeFoldout != null)
			{
				r.width = originWidth - r.x;
				r = rowsDrawer.BeforeFoldout(r, streamIndex, this);
			}

			// The drag position needs to be often reset. To ensure no drag start.
			if (Event.current.type == EventType.MouseUp)
				Utility.position2D = Vector2.zero;

			if (Event.current.type == EventType.MouseDown)
			{
				Utility.position2D = Vector2.zero;

				if (Event.current.button == 0 &&
					r.Contains(Event.current.mousePosition) == true)
				{
					Utility.position2D = Event.current.mousePosition;

					if (this.log.instanceID != 0)
					{
						DragAndDrop.objectReferences = new Object[] { EditorUtility.InstanceIDToObject(this.log.instanceID) };
						DragAndDrop.SetGenericData(Utility.DragObjectDataName, this.log.instanceID);
					}
				}
			}

			Color	foldoutColor = Color.white;
			bool	isDefaultLog = false;

			if ((this.log.mode & Mode.ScriptingException) != 0)
			{
				if (HQ.Settings.Get<GeneralSettings>().differentiateException == false)
					foldoutColor = ConsoleConstants.ErrorFoldoutColor;
				else
					foldoutColor = ConsoleConstants.ExceptionFoldoutColor;
			}
			else if ((this.log.mode & (Mode.ScriptCompileError | Mode.ScriptingError | Mode.Fatal | Mode.Error | Mode.Assert | Mode.AssetImportError | Mode.ScriptingAssertion)) != 0)
				foldoutColor = ConsoleConstants.ErrorFoldoutColor;
			else if ((this.log.mode & (Mode.ScriptCompileWarning | Mode.ScriptingWarning | Mode.AssetImportWarning)) != 0)
				foldoutColor = ConsoleConstants.WarningFoldoutColor;
			else
				isDefaultLog = true;

			bool	isOpened = rowsDrawer.rowsData.ContainsKey(this);

			// Draw foldout.
			r.x += 2F;
			r.width = 16F;
			EditorGUI.BeginDisabledGroup(this.HasStackTrace == false);
			{
				using (BgColorContentRestorer.Get(!isDefaultLog, foldoutColor))
				{
					EditorGUI.BeginChangeCheck();
					EditorGUI.Foldout(r, (bool)isOpened, "");
					if (EditorGUI.EndChangeCheck() == true)
					{
						if (isOpened == false)
							rowsDrawer.rowsData.Add(this, true);
						else
							rowsDrawer.rowsData.Remove(this);

						isOpened = !isOpened;
						GUI.FocusControl(null);
						rowsDrawer.InvalidateViewHeight();
					}
				}

				r.x -= 2F;
				r.width = 3F;

				if (isDefaultLog == false)
					EditorGUI.DrawRect(r, foldoutColor);

				r.width = 16F;
			}
			EditorGUI.EndDisabledGroup();

			r.x = r.width;

			if (this.log.instanceID != 0)
			{
				if (this.icon == null)
				{
					this.icon = Utility.GetIcon(this.log.instanceID);
					if (this.icon == null)
						this.icon = InternalEditorUtility.GetIconForFile(this.log.file);
				}

				if (icon != null)
				{
					r.width = settings.height;
					GUI.DrawTexture(r, icon);
					r.x += r.width;
				}
			}

			if (RowsDrawer.GlobalBeforeLog != null)
			{
				r.width = originWidth - r.x;
				r = RowsDrawer.GlobalBeforeLog(r, streamIndex, this);
			}
			if (rowsDrawer.BeforeLog != null)
			{
				r.width = originWidth - r.x;
				r = rowsDrawer.BeforeLog(r, streamIndex, this);
			}

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

						GUI.FocusControl(null);
					}

					if (rowsDrawer.rowsData.ContainsKey(this) == false)
						rowsDrawer.rowsData.Add(this, true);
					else
						rowsDrawer.rowsData.Remove(this);

					//this.isOpened = !this.isOpened;

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
					menu.AddItem(new GUIContent(LC.G("CopyLog")), false, rowsDrawer.MenuCopyLog, this);

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

						GUI.FocusControl(null);
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

					GUI.FocusControl(null);
					RowUtility.drawingWindow.Repaint();
					Event.current.Use();
				}
			}

			r = this.DrawPreLogData(rowsDrawer, r);
			r.width = originWidth - r.x;

			this.DrawLog(r, streamIndex);

			r = this.DrawCollapseLabel(r, collapse);

			if (RowsDrawer.GlobalAfterLog != null)
				r = RowsDrawer.GlobalAfterLog(r, streamIndex, this);
			if (rowsDrawer.AfterLog != null)
				r = rowsDrawer.AfterLog(r, streamIndex, this);

			r.y += r.height;

			if (isOpened == true)
			{
				r.x = 0F;
				r.width = originWidth;
				RowUtility.DrawStackTrace(this, rowsDrawer, r, streamIndex, this);
			}
		}

		public virtual void	DrawLog(Rect r, int i)
		{
			GUI.Button(r, this.HeadMessage, HQ.Settings.Get<LogSettings>().Style);
		}

		private Rect	DrawCollapseLabel(Rect r, bool? collapse)
		{
			if (collapse.HasValue == true && collapse.Value == true)
			{
				LogSettings	settings = HQ.Settings.Get<LogSettings>();
				// Draw collapse count.
				Utility.content.text = this.log.collapseCount.ToString();
				Vector2	vector = settings.CollapseLabelStyle.CalcSize(Utility.content);

				r.x += r.width - vector.x;
				r.width = vector.x;
				GUI.Label(r, Utility.content, settings.CollapseLabelStyle);
			}
			return r;
		}

		private object	ShortCopy(object row)
		{
			return this.HeadMessage;
		}

		private object	FullCopy(object row)
		{
			return this.FullMessage;
		}

		private object	CopyStackTrace(object row)
		{
			return this.StackTrace;
		}

		private object	HandleKeyboard(object data)
		{
			RowsDrawer	rowsDrawer = data as RowsDrawer;

			if (Event.current.type == EventType.KeyDown)
			{
				InputsManager	inputsManager = HQ.Settings.Get<ConsoleSettings>().inputsManager;

				if (inputsManager.Check("Navigation", ConsoleConstants.CloseLogCommand) == true)
				{
					if (rowsDrawer.rowsData.ContainsKey(this) == true)
					{
						rowsDrawer.rowsData.Remove(this);
						rowsDrawer.InvalidateViewHeight();
						RowUtility.drawingWindow.Repaint();
						RowUtility.ClearPreview();
					}
				}
				else if (inputsManager.Check("Navigation", ConsoleConstants.OpenLogCommand) == true)
				{
					if (rowsDrawer.rowsData.ContainsKey(this) == false)
					{
						rowsDrawer.rowsData.Add(this, true);
						rowsDrawer.InvalidateViewHeight();
						RowUtility.drawingWindow.Repaint();
						RowUtility.ClearPreview();
					}
				}
				else if (inputsManager.Check("Navigation", ConsoleConstants.GoToLineCommand) == true &&
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