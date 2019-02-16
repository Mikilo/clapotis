using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;

namespace NGToolsEditor.NGConsole
{
	using UnityEngine;

	[Serializable]
	public sealed class RowsDrawer
	{
		[Serializable]
		public sealed class Vars
		{
			public event Action<Vars>	SelectionChanged;

			/// <summary>Contains stream indexes.</summary>
			public List<int>	selectedLogs = new List<int>();
			public int			CountSelection { get { return this.selectedLogs.Count; } }

			// Update all Vars when lastTotalViewHeight change
			[NonSerialized]
			private Rect	lastRect = new Rect();
			public Rect		LastRect { get { this.lastRect.y = this.yScrollOffset; return this.lastRect; } }
			public float	yScrollOffset;
			public int		lastStreamIndex;
			public float	originalScrollOffset;

			[NonSerialized]
			public bool	hasInvalidHeight = true;
			[NonSerialized]
			public bool	mustRefreshCachePosition;

			public bool		autoScroll;
			[NonSerialized]
			public bool		updateAutoScroll;

			[NonSerialized]
			public bool		smoothScrolling = false;
			[NonSerialized]
			public bool		abortSmoothScrolling;
			[NonSerialized]
			public float	targetScrollPosition;
			[NonSerialized]
			public float	originScrollPosition;
			[NonSerialized]
			public float	smoothScrollStartTime;

			[NonSerialized]
			public VerticalScrollbar	scrollbar = new VerticalScrollbar(0F, 0F, 0F, 16F, 2F) { DrawBackgroundColor = true, interceiptEvent = false, hasCustomArea = true };

			#region Row Content Variables
			[NonSerialized]
			public Vector2	scrollPositionRowContent;
			/// <summary>Defines the height of the area displaying log's content.</summary>
			public float	rowContentHeight = 70F;
			public bool		draggingSplitterBar = false;
			public float	originPositionY;
			public float	originRowContentHeight;
			#endregion

			public int	GetSelection(int streamIndex)
			{
				return this.selectedLogs[streamIndex];
			}

			public void	AddSelection(int streamIndex)
			{
				this.selectedLogs.Add(streamIndex);
				this.selectedLogs.Sort();

				if (this.SelectionChanged != null)
					this.SelectionChanged(this);
			}

			public void	AddRangeSelection(int[] range)
			{
				this.selectedLogs.AddRange(range);
				this.selectedLogs.Sort();

				if (this.SelectionChanged != null)
					this.SelectionChanged(this);
			}

			public void	WrapSelection(int streamIndex)
			{
				int	min = int.MaxValue;
				int	max = int.MinValue;

				for (int j = 0; j < this.selectedLogs.Count; j++)
				{
					if (this.selectedLogs[j] < min)
						min = this.selectedLogs[j];
					if (this.selectedLogs[j] > max)
						max = this.selectedLogs[j];
				}

				if (min > streamIndex)
					min = streamIndex;
				if (max < streamIndex)
					max = streamIndex;

				this.selectedLogs.Clear();
				for (; min <= max; min++)
					this.selectedLogs.Add(min);

				if (this.SelectionChanged != null)
					this.SelectionChanged(this);
			}

			public void	RemoveSelection(int streamIndex)
			{
				this.selectedLogs.Remove(streamIndex);

				if (this.SelectionChanged != null)
					this.SelectionChanged(this);
			}

			public bool	IsSelected(int streamIndex)
			{
				return this.selectedLogs.Contains(streamIndex);
			}

			public void	ClearSelection()
			{
				this.selectedLogs.Clear();

				if (this.SelectionChanged != null)
					this.SelectionChanged(this);
			}

			public int[]	GetSelectionArray()
			{
				return this.selectedLogs.ToArray();
			}
		}

		public const float	SmoothScrollDuration = .2F;
		public const int	MaxStringLength = 16382;
		public const string	ShortCopyCommand = "ShortCopy";
		public const string	FullCopyCommand = "FullCopy";
		public const string	CopyStackTraceCommand = "CopyStackTrace";
		public const string	HandleKeyboardCommand = "HandleKeyboard";

		public static Func<RowsDrawer, Rect, int, Row, Rect>	GlobalBeforeFoldout;
		public static Func<Rect, int, Row, Rect>	GlobalBeforeLog;
		public static Func<Rect, int, Row, Rect>	GlobalAfterLog;
		public static Action<GenericMenu, RowsDrawer, int, Row>	GlobalLogContextMenu;

		[NonSerialized]
		public Func<Rect, int, Row, Rect>	BeforeFoldout;
		[NonSerialized]
		public Func<Rect, int, Row, Rect>	BeforeLog;
		[NonSerialized]
		public Func<Rect, int, Row, Rect>	AfterLog;
		[NonSerialized]
		public Action<Rect>					AfterAllRows;
		[NonSerialized]
		public Action<GenericMenu, Row>		LogContextMenu;
		[NonSerialized]
		public Action<Rect, Row>			RowHovered;
		[NonSerialized]
		public Action<Rect, Row>			RowClicked;
		[NonSerialized]
		public Action<Row>					RowDeleted;

		private bool	canDelete;
		/// <summary>
		/// Allows or not to delete logs.
		/// </summary>
		public bool		CanDelete { get { return this.canDelete; } set { this.canDelete = value; } }

		[NonSerialized]
		public IRows	rows;

		/// <summary>Contains indexes referencing rows fetched by RowsDrawer.RowGetter().</summary>
		[NonSerialized]
		private List<int>	consoleIndexes;
		public int	Count { get { return this.consoleIndexes.Count; } }
		public int	this[int i]
		{
			get
			{
				return this.consoleIndexes[i];
			}
			set
			{
				this.consoleIndexes[i] = value;
			}
		}

		[NonSerialized]
		public Vars	currentVars;

		/// <summary>
		/// Defines the area where the rows drawer can draw its rows. It excludes the area where log's content is displayed.
		/// </summary>
		[NonSerialized]
		internal Rect	bodyRect;

		public float	lastMaxViewHeight;
		[NonSerialized]
		public Rect		viewRect;
		[NonSerialized]
		public float	verticalScrollbarWidth;

		[NonSerialized]
		private Vector2	scrollPosition;

		[NonSerialized]
		private NGConsoleWindow		console;

		[NonSerialized]
		public Dictionary<Row, object>	rowsData = new Dictionary<Row, object>();

		[NonSerialized]
		private Rect	lastWindowPosition;
		//[NonSerialized]
		//private bool	mustOpenCopyPopup;

		public PerWindowVars<Vars>	perWindowVars;

		/// <summary>Contains console indexes.</summary>
		[NonSerialized]
		private List<int>	differentRowHeights = new List<int>();

		public	RowsDrawer()
		{
			this.consoleIndexes = new List<int>(ConsoleConstants.PreAllocatedArray);
			this.canDelete = true;
			this.perWindowVars = new PerWindowVars<Vars>();
		}

		/// <summary>
		/// Initializes RowsDrawer. Must be called before doing anything.
		/// </summary>
		/// <param name="editor">An instance of NGConsole to work on.</param>
		/// <param name="rows">A method to fetch a Row from an index. Because Rows are almost always shared between modules.</param>
		public void	Init(NGConsoleWindow editor, IRows rows)
		{
			this.console = editor;

			InputsManager	inputs = HQ.Settings.Get<ConsoleSettings>().inputsManager;
			// Populate with default commands if missing.
			inputs.AddCommand("Navigation", ConsoleConstants.OpenLogCommand, KeyCode.RightArrow);
			inputs.AddCommand("Navigation", ConsoleConstants.CloseLogCommand, KeyCode.LeftArrow);
			inputs.AddCommand("Navigation", ConsoleConstants.FocusTopLogCommand, KeyCode.Home);
			inputs.AddCommand("Navigation", ConsoleConstants.FocusBottomLogCommand, KeyCode.End);
			inputs.AddCommand("Navigation", ConsoleConstants.MoveUpLogCommand, KeyCode.UpArrow);
			inputs.AddCommand("Navigation", ConsoleConstants.MoveDownLogcommand, KeyCode.DownArrow);
			inputs.AddCommand("Navigation", ConsoleConstants.LongMoveUpLogCommand, KeyCode.PageUp);
			inputs.AddCommand("Navigation", ConsoleConstants.LongMoveDownLogCommand, KeyCode.PageDown);
			inputs.AddCommand("Navigation", ConsoleConstants.DeleteLogCommand, KeyCode.Delete);
			inputs.AddCommand("Navigation", ConsoleConstants.GoToLineCommand, KeyCode.Return);

			this.rows = rows;

			this.console.ConsoleCleared += this.ClearUpdateAutoScroll;
			this.console.syncLogs.EndNewLog += this.UpdateAutoScroll;

			if (this.perWindowVars == null)
				this.perWindowVars = new PerWindowVars<Vars>();

			this.perWindowVars.VarsAdded += this.InitializeVars;

			foreach (Vars vars in this.perWindowVars.Each())
				this.InitializeVars(vars);

			this.UpdateAutoScroll();
		}

		public void	Uninit()
		{
			this.console.ConsoleCleared -= this.ClearUpdateAutoScroll;
			this.console.syncLogs.EndNewLog -= this.UpdateAutoScroll;
			this.perWindowVars.VarsAdded -= this.InitializeVars;
		}

		public void	SetRowGetter(IRows rows)
		{
			this.rows = rows;
		}

		public void	AddDiffRowHeight(int consoleIndex)
		{
			for (int i = 0; i < this.differentRowHeights.Count; i++)
			{
				if (consoleIndex < this.differentRowHeights[i])
				{
					this.differentRowHeights.Insert(i, consoleIndex);
					return;
				}
			}

			this.differentRowHeights.Add(consoleIndex);
		}

		public void	RemoveDiffRowHeight(int consoleIndex)
		{
			this.differentRowHeights.Remove(consoleIndex);
		}

		public void	Add(int consoleIndex)
		{
			this.consoleIndexes.Add(consoleIndex);

			Row		row = this.rows.GetRow(consoleIndex);
			float	height = row.GetHeight(this);

			this.lastMaxViewHeight += height;

			float	standardHeight = HQ.Settings.Get<LogSettings>().height;
			if (height != standardHeight)
				this.differentRowHeights.Add(consoleIndex);
		}

		public void	AddRange(IEnumerable<int> range)
		{
			this.consoleIndexes.AddRange(range);

			float	standardHeight = HQ.Settings.Get<LogSettings>().height;

			foreach (var i in range)
			{
				Row		row = this.rows.GetRow(i);
				float	height = row.GetHeight(this);

				this.lastMaxViewHeight += height;

				if (height != standardHeight)
					this.differentRowHeights.Add(i);
			}
		}

		public void	RemoveAt(int streamIndex)
		{
			int		consoleIndex = this.consoleIndexes[streamIndex];
			Row		row = this.rows.GetRow(consoleIndex);
			float	rowHeight = row.GetHeight(this);
			float	yOffset = this.GetOffsetAtIndex(consoleIndex);

			foreach (Vars vars in this.perWindowVars.Each())
			{
				foreach (ListPointOfInterest list in vars.scrollbar.EachListInterests())
				{
					if (list.RemoveId(consoleIndex, rowHeight) == false)
						list.InsertOffset(yOffset, -rowHeight);
				}
			}

			this.lastMaxViewHeight -= rowHeight;

			foreach (Vars vars in this.perWindowVars.Each())
				vars.RemoveSelection(consoleIndex);

			this.differentRowHeights.Remove(consoleIndex);
			this.consoleIndexes.RemoveAt(streamIndex);

			if (this.RowDeleted != null)
				this.RowDeleted(row);
		}

		public void	Clear()
		{
			this.consoleIndexes.Clear();
			this.differentRowHeights.Clear();

			this.lastMaxViewHeight = 0;

			foreach (Vars vars in this.perWindowVars.Each())
			{
				vars.ClearSelection();
				vars.scrollbar.ClearAllInterests();
			}

			this.ClearUpdateAutoScroll();
		}

		public int[]	GetRowsArray()
		{
			return this.consoleIndexes.ToArray();
		}

		public void	ClearUpdateAutoScroll()
		{
			foreach (Vars vars in this.perWindowVars.Each())
			{
				vars.updateAutoScroll = true;
				vars.hasInvalidHeight = true;
				vars.lastStreamIndex = 0;
				vars.yScrollOffset = 0F;
			}
		}

		public void	UpdateAutoScroll()
		{
			foreach (Vars vars in this.perWindowVars.Each())
				vars.updateAutoScroll = true;
		}

		public float	GetOffsetAtIndex(int maxConsoleIndex = int.MaxValue)
		{
			float	offset = 0F;
			float	standardHeight = HQ.Settings.Get<LogSettings>().height;

			if (maxConsoleIndex == int.MaxValue)
			{
				offset = (this.consoleIndexes.Count - this.differentRowHeights.Count) * standardHeight;

				for (int i = 0; i < this.differentRowHeights.Count; i++)
				{
					Row	row = this.rows.GetRow(this.differentRowHeights[i]);
					offset += row.GetHeight(this);
				}
			}
			else
			{
				int	streamIndex = maxConsoleIndex;

				if (maxConsoleIndex < this.consoleIndexes.Count)
				{
					if (this.consoleIndexes[maxConsoleIndex] != maxConsoleIndex)
						streamIndex = this.consoleIndexes.IndexOf(maxConsoleIndex);
				}
				else
					streamIndex = this.consoleIndexes.IndexOf(maxConsoleIndex);

				//int	countIndex = maxIndex;

				//if (maxIndex >= this.rowIndexes.Count)
				//{
				//	maxIndex = this.rowIndexes[this.rowIndexes.Count - 1];
				//}
				//else
				//{
				//	maxIndex = this.rowIndexes[maxIndex];
				//	if (maxIndex == -1)
				//		maxIndex = this.rowIndexes[this.rowIndexes.Count - 1];
				//}

				int	overflow = 0;

				for (int i = 0; i < this.differentRowHeights.Count; i++)
				{
					if (this.differentRowHeights[i] <= maxConsoleIndex)
					{
						++overflow;

						// Get offset, not height, therefore adding itself is skipped.
						if (this.differentRowHeights[i] != maxConsoleIndex)
						{
							Row	row = this.rows.GetRow(this.differentRowHeights[i]);
							offset += row.GetHeight(this);
						}
						else
							--overflow;
					}
					else
						break;
				}

				offset += (streamIndex - overflow) * standardHeight;
			}

			return offset;
		}

		public Rect	DrawRows(Rect r, bool showCollapse)
		{
			bool	collapse = showCollapse;

			if (showCollapse == true)
			{
				ConsoleFlags	flags = (ConsoleFlags)UnityLogEntries.consoleFlags;
				collapse = (flags & ConsoleFlags.Collapse) != 0;
			}

			this.currentVars = this.perWindowVars.Get(RowUtility.drawingWindow);

			this.viewRect.width = 0;

			LogSettings	settings = HQ.Settings.Get<LogSettings>();
			float		rowHeight = 0F;
			Row			row;

			if (this.currentVars.hasInvalidHeight == false)
				rowHeight = this.lastMaxViewHeight;
			else
			{
				rowHeight = this.GetOffsetAtIndex();
				this.lastMaxViewHeight = rowHeight;
				if (Event.current.type == EventType.Layout)
					this.currentVars.hasInvalidHeight = false;
			}

			this.viewRect.height = rowHeight;

			if (settings.alwaysDisplayLogContent == true || this.CanDrawRowContent() == true)
				r.height -= this.currentVars.rowContentHeight + ConsoleConstants.RowContentSplitterHeight;

			this.verticalScrollbarWidth = (this.viewRect.height > r.height) ? ConsoleConstants.VerticalScrollbarWidth : 0F;

			r.x = 0F;
			this.bodyRect = r;

			if (this.viewRect.height > r.height)
			{
				this.scrollPosition.y = -this.currentVars.scrollbar.Offset;
				if (-this.scrollPosition.y >= this.viewRect.height - r.height)
				{
					this.currentVars.scrollbar.Offset = this.viewRect.height - r.height;
					this.scrollPosition.y = -this.currentVars.scrollbar.Offset;
				}
			}
			else
				this.scrollPosition.y = 0;

			if (Event.current.type != EventType.Layout &&
				Mathf.Approximately(this.currentVars.scrollbar.MaxHeight, r.height) == false)
			{
				if (r.height > this.currentVars.scrollbar.MaxHeight)
					this.UpdateCachedIndexAndHeight(this.currentVars);

				this.currentVars.scrollbar.SetSize(r.height);
			}

			this.currentVars.scrollbar.SetPosition(r.x + r.width - 15F, r.y);
			this.currentVars.scrollbar.RealHeight = this.viewRect.height;
			this.currentVars.scrollbar.allowedMouseArea = r;

			if (this.currentVars.scrollbar.MaxWidth > 0F)
			{
				this.currentVars.scrollbar.OnGUI();
			}

			// Width in bodyRect does not represent the sum of all controls. It does not calculate width from events beforeFoldout, beforeLog, and afterLog.
			GUI.BeginClip(r, this.scrollPosition, Vector2.zero, false);
			{
				if (this.currentVars.updateAutoScroll == true)
				{
					this.currentVars.updateAutoScroll = false;

					if (this.currentVars.autoScroll == true || this.currentVars.smoothScrolling == true)
					{
						if (HQ.Settings.Get<GeneralSettings>().smoothScrolling == false)
							this.currentVars.scrollbar.Offset = float.MaxValue;
						else
							this.StartSmoothScroll(this.viewRect.height - r.height);
					}
				}

				if (this.currentVars.smoothScrolling == false)
					this.currentVars.autoScroll = (this.viewRect.height - this.currentVars.scrollbar.Offset - r.height) < .01F;

				bool	firstFound = false;
				int		streamIndex;

				if (this.currentVars.mustRefreshCachePosition == false)
				{
					streamIndex = this.currentVars.lastStreamIndex;
					r = this.currentVars.LastRect;
				}
				else
				{
					streamIndex = 0;
					r.y = 0F;
					this.currentVars.mustRefreshCachePosition = false;
				}

				r.width = this.bodyRect.width;

				// Display logs.
				for (; streamIndex < this.consoleIndexes.Count; streamIndex++)
				{
					row = this.rows.GetRow(this.consoleIndexes[streamIndex]);
					rowHeight = row.GetHeight(this);

					if (r.y + rowHeight <= this.currentVars.scrollbar.Offset)
					{
						r.y += rowHeight;
						continue;
					}

					if (firstFound == false)
					{
						firstFound = true;
						this.currentVars.lastStreamIndex = streamIndex;
						this.currentVars.yScrollOffset = r.y;
					}

					r.height = rowHeight;
					row.DrawRow(this, r, streamIndex, collapse);

					if (this.currentVars.hasInvalidHeight == true)
					{
						float	newHeight = row.GetHeight(this);

						if (newHeight != settings.height)
							this.AddDiffRowHeight(this.consoleIndexes[streamIndex]);
						else
							this.RemoveDiffRowHeight(this.consoleIndexes[streamIndex]);

						float	deltaHeight = newHeight - rowHeight;

						if (Mathf.Approximately(deltaHeight, 0F) == false)
						{
							foreach (Vars vars in this.perWindowVars.Each())
							{
								foreach (ListPointOfInterest list in vars.scrollbar.EachListInterests())
									list.InsertOffset(r.y + 8F, deltaHeight, this.consoleIndexes[streamIndex]);
							}

							this.lastMaxViewHeight += deltaHeight;
						}

						this.currentVars.hasInvalidHeight = false;
					}

					r.y += r.height;

					// Check if out of view rect.
					if (r.y - this.currentVars.scrollbar.Offset > this.bodyRect.height)
						break;
				}

				this.HandleKeyboard();
			}
			GUI.EndClip();

			// Restore height if selected logs have changed during the drawing.
			if (settings.alwaysDisplayLogContent == false && this.CanDrawRowContent() == false)
				this.bodyRect.height += this.currentVars.rowContentHeight + ConsoleConstants.RowContentSplitterHeight;

			r = this.bodyRect;
			r.y += r.height;

			if (settings.alwaysDisplayLogContent == true || this.CanDrawRowContent() == true)
				r = this.DrawRowContent(r);

			if (this.AfterAllRows != null)
				this.AfterAllRows(this.bodyRect);

			return r;
		}

		private bool	CanDrawRowContent()
		{
			return this.currentVars.selectedLogs.Count == 1 &&
				   this.rows.GetRow(this.consoleIndexes[this.currentVars.selectedLogs[0]]) is ILogContentGetter;
		}

		private Rect	DrawRowContent(Rect r)
		{
			r.x = 0F;

			LogSettings	settings = HQ.Settings.Get<LogSettings>();
			Row			row = null;
			float		contentHeight = 0F;

			if (this.currentVars.selectedLogs.Count == 1 && this.CanDrawRowContent() == true)
			{
				row = this.rows.GetRow(this.consoleIndexes[this.currentVars.selectedLogs[0]]);
				Utility.content.text = row.log.condition;
				if (Utility.content.text.Length > RowsDrawer.MaxStringLength)
					Utility.content.text = Utility.content.text.Substring(0, RowsDrawer.MaxStringLength);

				contentHeight = settings.ContentStyle.CalcHeight(Utility.content, r.width);
			}
			else
				Utility.content.text = string.Empty;

			// Handle splitter bar.
			r.height = ConsoleConstants.RowContentSplitterHeight;
			GUI.Box(r, "");
			EditorGUIUtility.AddCursorRect(r, MouseCursor.ResizeVertical);

			if (Event.current.type == EventType.MouseDrag)
			{
				if (this.currentVars.draggingSplitterBar == true)
				{
					this.currentVars.rowContentHeight = Mathf.Clamp(this.currentVars.originRowContentHeight + this.currentVars.originPositionY - Event.current.mousePosition.y,
																	ConsoleConstants.MinRowContentHeight, RowUtility.drawingWindow.position.height - ConsoleConstants.MaxRowContentHeightLeft);
					Utility.RegisterIntervalCallback(this.console.SaveModules, 200, 1);
					Event.current.Use();
				}
			}
			else if (Event.current.type == EventType.MouseDown)
			{
				if (r.Contains(Event.current.mousePosition) == true)
				{
					this.currentVars.originPositionY = Event.current.mousePosition.y;
					this.currentVars.originRowContentHeight = this.currentVars.rowContentHeight;
					this.currentVars.draggingSplitterBar = true;
					Event.current.Use();
				}
			}
			else if (this.currentVars.draggingSplitterBar == true &&
					 Event.current.type == EventType.MouseUp)
			{
				// Auto adjust height on left click or double click.
				if (r.Contains(Event.current.mousePosition) == true &&
					(Event.current.button == 1 ||
					 (RowUtility.LastClickTime + Constants.DoubleClickTime > EditorApplication.timeSinceStartup &&
					  Mathf.Abs(this.currentVars.originPositionY - Event.current.mousePosition.y) < 5F)))
				{
					// 7F of margin, dont know why it is required. CalcHeight seems to give bad result.
					this.currentVars.rowContentHeight = Mathf.Clamp(contentHeight + 7F, ConsoleConstants.MinRowContentHeight, RowUtility.drawingWindow.position.height - ConsoleConstants.MaxRowContentHeightLeft);
					this.console.SaveModules();
				}

				RowUtility.LastClickTime = EditorApplication.timeSinceStartup;
				this.currentVars.draggingSplitterBar = false;
				Event.current.Use();
			}

			r.y += r.height;

			// Write log's content.
			r.height = this.currentVars.rowContentHeight;

			if (r.height > RowUtility.drawingWindow.position.height - ConsoleConstants.MaxRowContentHeightLeft)
				this.currentVars.rowContentHeight = RowUtility.drawingWindow.position.height - ConsoleConstants.MaxRowContentHeightLeft;

			// Smoothly stay at the minimum if not critical under the critical threshold.
			if (this.currentVars.rowContentHeight < ConsoleConstants.MinRowContentHeight)
				this.currentVars.rowContentHeight = RowUtility.drawingWindow.position.height - ConsoleConstants.MaxRowContentHeightLeft;

			// Prevent reaching non-restorable value.
			if (this.currentVars.rowContentHeight < ConsoleConstants.CriticalMinimumContentHeight)
				this.currentVars.rowContentHeight = ConsoleConstants.CriticalMinimumContentHeight;

			if (row != null)
			{
				this.viewRect.height = contentHeight;
				if (this.viewRect.height > r.height)
					this.viewRect.height = settings.ContentStyle.CalcHeight(Utility.content, r.width - 16F);

				this.currentVars.scrollPositionRowContent = GUI.BeginScrollView(r, this.currentVars.scrollPositionRowContent, this.viewRect);
				{
					GUI.SetNextControlName(ConsoleConstants.CopyControlName);
					Rect	selectableRect = r;
					selectableRect.x = 0F;
					selectableRect.y = 0F;
					selectableRect.width = r.width - (this.viewRect.height > r.height ? 16F : 0F);
					selectableRect.height = this.viewRect.height;
					EditorGUI.SelectableLabel(selectableRect, Utility.content.text, settings.ContentStyle);
				}
				GUI.EndScrollView();
			}

			return r;
		}

		public void	InvalidateViewHeight()
		{
			this.perWindowVars.Get(RowUtility.drawingWindow).hasInvalidHeight = true;
		}

		/// <summary>Scrolls to keep the focused log in the view rect.</summary>
		public void	FitFocusedLogInScreen(int targetStreamIndex)
		{
			if (targetStreamIndex <= this.currentVars.lastStreamIndex)
			{
				if (targetStreamIndex == 0)
				{
					this.currentVars.lastStreamIndex = 0;
					this.currentVars.scrollbar.Offset = 0F;
					this.currentVars.yScrollOffset = 0F;
				}
				else
				{
					float	y = this.currentVars.yScrollOffset;

					while (this.currentVars.lastStreamIndex > 0 && targetStreamIndex < this.currentVars.lastStreamIndex)
					{
						--this.currentVars.lastStreamIndex;
						y -= this.rows.GetRow(this.consoleIndexes[this.currentVars.lastStreamIndex]).GetHeight(this);
					}

					this.currentVars.scrollbar.Offset = y;
					this.currentVars.yScrollOffset = y;
				}
			}
			else
			{
				LogSettings	settings = HQ.Settings.Get<LogSettings>();
				float		y = this.currentVars.yScrollOffset;

				for (int i = this.currentVars.lastStreamIndex; i < this.consoleIndexes.Count; i++)
				{
					if (i == targetStreamIndex)
						break;

					y += this.rows.GetRow(this.consoleIndexes[i]).GetHeight(this);
				}

				if (y + settings.height > this.bodyRect.height + this.currentVars.scrollbar.Offset)
				{
					this.currentVars.scrollbar.Offset = y + settings.height - (this.bodyRect.height);
				}
			}
		}

		public void	MenuCopyLine(object data)
		{
			StringBuilder	buffer = Utility.GetBuffer();

			this.currentVars.selectedLogs.Sort();
			for (int i = 0; i < this.currentVars.selectedLogs.Count; i++)
			{
				Row	r = this.rows.GetRow(this.consoleIndexes[this.currentVars.selectedLogs[i]]);
				buffer.AppendLine(r.Command(RowsDrawer.ShortCopyCommand, r).ToString());
			}

			if (buffer.Length > Environment.NewLine.Length)
				buffer.Length -= Environment.NewLine.Length;

			EditorGUIUtility.systemCopyBuffer = Utility.ReturnBuffer(buffer);
		}

		public void	MenuCopyLog(object data)
		{
			StringBuilder	buffer = Utility.GetBuffer();

			this.currentVars.selectedLogs.Sort();
			for (int i = 0; i < this.currentVars.selectedLogs.Count; i++)
			{
				Row	r = this.rows.GetRow(this.consoleIndexes[this.currentVars.selectedLogs[i]]);
				buffer.AppendLine(r.Command(RowsDrawer.FullCopyCommand, r).ToString());
			}

			if (buffer.Length > Environment.NewLine.Length)
				buffer.Length -= Environment.NewLine.Length;

			EditorGUIUtility.systemCopyBuffer = Utility.ReturnBuffer(buffer);
		}

		public void	MenuCopyStackTrace(object data)
		{
			StringBuilder	buffer = Utility.GetBuffer();

			this.currentVars.selectedLogs.Sort();
			for (int i = 0; i < this.currentVars.selectedLogs.Count; i++)
			{
				Row	r = this.rows.GetRow(this.consoleIndexes[this.currentVars.selectedLogs[i]]);
				buffer.AppendLine(r.Command(RowsDrawer.CopyStackTraceCommand, r).ToString());
			}

			if (buffer.Length > Environment.NewLine.Length)
				buffer.Length -= Environment.NewLine.Length;

			EditorGUIUtility.systemCopyBuffer = Utility.ReturnBuffer(buffer);
		}

		public void	MenuExportSelection(object data)
		{
			Vars		vars = this.perWindowVars.Get(RowUtility.drawingWindow);
			List<Row>	rows = new List<Row>();

			for (int i = 0; i < vars.CountSelection; i++)
				rows.Add(this.console.rows[this.consoleIndexes[vars.GetSelection(i)]]);

			ExportLogsWindow.Export(rows);
		}

		private void	InitializeVars(Vars vars)
		{
			vars.scrollbar.OffsetChanged += this.OnScrollUpdated;
			vars.SelectionChanged += this.RefreshSelectionInScrollbar;
		}

		private void	GetHeightAndIndex(ref int i, ref float height)
		{
			float	standardHeight = HQ.Settings.Get<LogSettings>().height;
			int		maxIndexRange = Mathf.CeilToInt((height - this.currentVars.scrollbar.Offset) / standardHeight);

			for (int j = this.differentRowHeights.Count - 1; j >= 0; --j)
			{
				if (this.differentRowHeights[j] <= i && this.differentRowHeights[j] > i - maxIndexRange)
				{
					Row	row = this.rows.GetRow(this.differentRowHeights[j]);
					height -= (i - this.differentRowHeights[j]) * standardHeight + row.GetHeight(this);
					maxIndexRange -= (i - this.differentRowHeights[j]);
					i = this.differentRowHeights[j] - 1;
				}
			}

			i -= maxIndexRange;
			height -= maxIndexRange * standardHeight;
		}

		private void	RefreshSelectionInScrollbar(Vars vars)
		{
			LogSettings	settings = HQ.Settings.Get<LogSettings>();

			vars.scrollbar.ClearInterests();

			for (int j = 0; j < vars.selectedLogs.Count; j++)
			{
				if (vars.selectedLogs[j] >= this.consoleIndexes.Count)
					continue;

				float	maxHeight = this.GetOffsetAtIndex(this.consoleIndexes[vars.selectedLogs[j]]);

				vars.scrollbar.AddInterest(8F + maxHeight, settings.selectedBackground);
			}
		}

		private void	HandleKeyboard()
		{
			if (Event.current.type == EventType.KeyDown)
			{
				InputsManager	inputs = HQ.Settings.Get<ConsoleSettings>().inputsManager;

				if (Event.current.keyCode == KeyCode.F)
				{
					if (this.currentVars.selectedLogs.Count > 0)
					{
						for (int i = 0; i < this.currentVars.selectedLogs.Count; i++)
							EditorGUIUtility.PingObject(this.rows.GetRow(this.consoleIndexes[this.currentVars.selectedLogs[i]]).log.instanceID);

						if (this.currentVars.selectedLogs.Count > 1)
							this.FitFocusedLogInScreen(this.currentVars.selectedLogs[this.currentVars.selectedLogs.Count - 1]);
						this.FitFocusedLogInScreen(this.currentVars.selectedLogs[0]);
						RowUtility.drawingWindow.Repaint();
					}
				}

				if (this.CanDelete == true &&
					inputs.Check("Navigation", ConsoleConstants.DeleteLogCommand) == true)
				{
					if (this.currentVars.selectedLogs.Count > 0)
					{
						this.currentVars.selectedLogs.Sort();

						int	firstSelected = this.currentVars.selectedLogs[0];

						for (int i = this.currentVars.selectedLogs.Count - 1; i >= 0; --i)
							this.RemoveAt(this.currentVars.selectedLogs[i]);

						if (firstSelected < this.consoleIndexes.Count)
							this.currentVars.selectedLogs.Add(firstSelected);
						else if (this.consoleIndexes.Count > 0)
							this.currentVars.selectedLogs.Add(this.consoleIndexes.Count - 1);

						RowUtility.drawingWindow.Repaint();
						Event.current.Use();
					}
				}
				else if (inputs.Check("Navigation", ConsoleConstants.FocusTopLogCommand) == true)
				{
					this.currentVars.ClearSelection();

					if (this.consoleIndexes.Count > 0)
					{
						this.currentVars.AddSelection(0);
						this.FitFocusedLogInScreen(0);

						RowUtility.drawingWindow.Repaint();
					}

					Event.current.Use();
				}
				else if (inputs.Check("Navigation", ConsoleConstants.FocusBottomLogCommand) == true)
				{
					this.currentVars.ClearSelection();

					if (this.consoleIndexes.Count > 0)
					{
						this.currentVars.autoScroll = true;
						this.currentVars.AddSelection(this.consoleIndexes.Count - 1);
						this.FitFocusedLogInScreen(this.consoleIndexes.Count - 1);
						RowUtility.drawingWindow.Repaint();
					}

					Event.current.Use();
				}
				else if (inputs.Check("Navigation", ConsoleConstants.MoveDownLogcommand) == true)
				{
					if (this.currentVars.selectedLogs.Count == 0)
					{
						this.currentVars.AddSelection(0);
						this.FitFocusedLogInScreen(0);
						RowUtility.drawingWindow.Repaint();
					}
					else
					{
						int	highest = int.MinValue;

						foreach (int i in this.currentVars.selectedLogs)
						{
							if (i > highest)
								highest = i;
						}

						++highest;
						if (highest < this.consoleIndexes.Count)
						{
							this.currentVars.ClearSelection();
							this.currentVars.AddSelection(highest);
							this.FitFocusedLogInScreen(highest);
							RowUtility.drawingWindow.Repaint();
						}

						Event.current.Use();
					}
				}
				else if (inputs.Check("Navigation", ConsoleConstants.MoveUpLogCommand) == true)
				{
					if (this.currentVars.selectedLogs.Count == 0)
					{
						this.currentVars.AddSelection(0);
						this.FitFocusedLogInScreen(0);
						RowUtility.drawingWindow.Repaint();
					}
					else
					{
						int	lowest = int.MaxValue;

						foreach (int i in this.currentVars.selectedLogs)
						{
							if (i < lowest)
								lowest = i;
						}

						--lowest;
						if (lowest >= 0)
						{
							this.currentVars.ClearSelection();
							this.currentVars.AddSelection(lowest);
							this.FitFocusedLogInScreen(lowest);
							RowUtility.drawingWindow.Repaint();
						}

						Event.current.Use();
					}
				}
				else if (inputs.Check("Navigation", ConsoleConstants.LongMoveDownLogCommand) == true)
				{
					if (this.currentVars.selectedLogs.Count == 0)
					{
						this.currentVars.AddSelection(0);
						this.FitFocusedLogInScreen(0);
						RowUtility.drawingWindow.Repaint();
					}
					else
					{
						int	highest = int.MinValue;

						foreach (int i in this.currentVars.selectedLogs)
						{
							if (i > highest)
								highest = i;
						}

						float	y = 0F;

						while (y < this.bodyRect.height && highest <= this.consoleIndexes.Count - 1)
							y += this.rows.GetRow(highest++).GetHeight(this);

						if (highest >= this.consoleIndexes.Count)
							highest = this.consoleIndexes.Count - 1;

						this.currentVars.ClearSelection();
						this.currentVars.AddSelection(highest);
						this.FitFocusedLogInScreen(highest);
						RowUtility.drawingWindow.Repaint();
						GUI.FocusControl("L" + highest);
						Event.current.Use();
					}
				}
				else if (inputs.Check("Navigation", ConsoleConstants.LongMoveUpLogCommand) == true)
				{
					if (this.currentVars.selectedLogs.Count == 0)
					{
						this.currentVars.AddSelection(0);
						this.FitFocusedLogInScreen(0);
						RowUtility.drawingWindow.Repaint();
					}
					else
					{
						int	lowest = int.MaxValue;

						foreach (int i in this.currentVars.selectedLogs)
						{
							if (i < lowest)
								lowest = i;
						}

						float	y = 0F;

						while (y < this.bodyRect.height && lowest >= 0)
							y += this.rows.GetRow(lowest--).GetHeight(this);

						if (lowest < 0)
							lowest = 0;

						this.currentVars.ClearSelection();
						this.currentVars.AddSelection(lowest);
						this.FitFocusedLogInScreen(lowest);
						RowUtility.drawingWindow.Repaint();

						Event.current.Use();
					}
				}
				else if (Event.current.keyCode == KeyCode.C && Event.current.shift == true)
				{
					this.OpenAdvanceCopy();
					Event.current.Use();
				}
			}
			else if (Event.current.type == EventType.ValidateCommand)
			{
				if (Event.current.commandName == "SelectAll")
				{
					if (this.IsKeyboardFocusBusy() == false)
					{
						this.currentVars.ClearSelection();
						for (int i = 0; i < this.consoleIndexes.Count; i++)
							this.currentVars.AddSelection(i);
						RowUtility.drawingWindow.Repaint();
					}
				}
				else if (GUI.GetNameOfFocusedControl() != ConsoleConstants.CopyControlName && Event.current.commandName == "Copy")
					Event.current.Use();
			}
			// Copy head message on Ctrl + C, copy full message on double Ctr + C.
			else if (Event.current.type == EventType.ExecuteCommand &&
					 GUI.GetNameOfFocusedControl() != ConsoleConstants.CopyControlName &&
					 Event.current.commandName == "Copy")
			{
				if (this.IsKeyboardFocusBusy() == false)
				{
					if (RowUtility.LastKeyTime + Constants.DoubleClickTime > EditorApplication.timeSinceStartup)
						this.MenuCopyLog(null);
					else
						this.MenuCopyLine(null);

					RowUtility.LastKeyTime = EditorApplication.timeSinceStartup;
					Event.current.Use();
				}
			}

			if (Event.current.type == EventType.Repaint)
				this.lastWindowPosition = RowUtility.drawingWindow.position;

			LogSettings	settings = HQ.Settings.Get<LogSettings>();

			for (int i = 0; i < this.currentVars.selectedLogs.Count; i++)
			{
				if (this.currentVars.selectedLogs[i] >= this.consoleIndexes.Count)
				{
					this.currentVars.RemoveSelection(i);
					continue;
				}

				int		consoleIndex = this.consoleIndexes[this.currentVars.selectedLogs[i]];
				Row		row = this.rows.GetRow(consoleIndex);
				float	rowHeight = row.GetHeight(this);

				row.Command(RowsDrawer.HandleKeyboardCommand, this);

				if (this.currentVars.hasInvalidHeight == true)
				{
					float	newHeight = row.GetHeight(this);
					float	yOffset = this.GetOffsetAtIndex(consoleIndex);

					if (newHeight != settings.height)
						this.AddDiffRowHeight(consoleIndex);
					else
						this.RemoveDiffRowHeight(consoleIndex);

					float	deltaHeight = newHeight - rowHeight;

					if (Mathf.Approximately(deltaHeight, 0F) == false)
					{
						foreach (Vars vars in this.perWindowVars.Each())
						{
							foreach (ListPointOfInterest list in vars.scrollbar.EachListInterests())
								list.InsertOffset(yOffset + 8F, deltaHeight, consoleIndex);
						}

						this.lastMaxViewHeight += deltaHeight;
					}

					this.currentVars.hasInvalidHeight = false;
				}
			}
		}

		private bool	IsKeyboardFocusBusy()
		{
			bool	focusIsGrabbed = false;/* EditorGUIUtility.editingTextField;*/

			//Debug.Log("Copy" + EditorGUIUtility.editingTextField + " " + EditorGUIUtility.hotControl + " " + EditorGUIUtility.keyboardControl + " " + GUIUtility.keyboardControl + " " + GUIUtility.hotControl);
			if (/*focusIsGrabbed == false && */EditorGUIUtility.keyboardControl != 0)
			{
				//TextEditor	textEditor = EditorGUIUtility.GetStateObject(typeof(TextEditor), EditorGUIUtility.keyboardControl) as TextEditor;

				//if (textEditor.controlID != 0)
					focusIsGrabbed = true;
				//NGDebug.Snapshot(textEditor);
			}

			return focusIsGrabbed;
		}

		public void	OpenAdvanceCopy()
		{
			GUICallbackWindow.Open(() =>
			{
				List<Row>	rows = new List<Row>();

				for (int i = 0; i < this.currentVars.CountSelection; i++)
					rows.Add(this.console.rows[this.consoleIndexes[this.currentVars.GetSelection(i)]]);

				float	x = this.lastWindowPosition.x + ((this.lastWindowPosition.width - CopyLogsPopup.DefaultWidth) / 2F);
				float	y = this.lastWindowPosition.y + ((this.lastWindowPosition.height - CopyLogsPopup.DefaultHeight) / 2F);
				PopupWindow.Show(new Rect(x, y, 1F, 1F), new CopyLogsPopup(rows));
			});
		}

		private void	StartSmoothScroll(float target)
		{
			if (this.viewRect.height < this.bodyRect.height)
				return;

			Vars	vars = this.perWindowVars.Get(RowUtility.drawingWindow);

			vars.targetScrollPosition = target;
			vars.originScrollPosition = vars.scrollbar.Offset;
			vars.smoothScrollStartTime = Time.realtimeSinceStartup;

			if (vars.smoothScrolling == false)
			{
				vars.smoothScrolling = true;
				vars.abortSmoothScrolling = false;

				EditorApplication.CallbackFunction	smoothScroll = null;

				smoothScroll = delegate()
				{
					if (vars.abortSmoothScrolling == true || vars.smoothScrolling == false)
					{
						vars.smoothScrolling = false;
						vars.autoScroll = false;
						EditorApplication.update -= smoothScroll;
						return;
					}

					float	rate = (Time.realtimeSinceStartup - vars.smoothScrollStartTime) / RowsDrawer.SmoothScrollDuration;

					if (rate >= 1F)
					{
						vars.scrollbar.Offset = vars.targetScrollPosition;
						vars.originalScrollOffset = vars.scrollbar.Offset;
						vars.smoothScrolling = false;
						vars.autoScroll = true;
						EditorApplication.update -= smoothScroll;
					}
					else
					{
						vars.scrollbar.Offset = Mathf.Lerp(vars.originScrollPosition, vars.targetScrollPosition, rate);
						vars.abortSmoothScrolling = false;
						vars.originalScrollOffset = vars.scrollbar.Offset;
					}

					Utility.RepaintEditorWindow(typeof(NGConsoleWindow));
				};

				EditorApplication.update += smoothScroll;
			}
		}

		private void	UpdateCachedIndexAndHeight(Vars vars)
		{
			// Seek backward.
			if (vars.scrollbar.Offset < vars.originalScrollOffset)
			{
				// Just force the process from the beginning if the new offset is lower than the half, due to potentially less calculus. Dichotomy things... You know.
				if (vars.scrollbar.Offset <= vars.originalScrollOffset * .5)
					vars.mustRefreshCachePosition = true;
				else
				{
					Row	row = this.rows.GetRow(this.consoleIndexes[vars.lastStreamIndex]);
					vars.yScrollOffset += row.GetHeight(this);

					this.GetHeightAndIndex(ref vars.lastStreamIndex, ref vars.yScrollOffset);

					if (vars.lastStreamIndex < 0)
						vars.mustRefreshCachePosition = true;
					else
						++vars.lastStreamIndex;
				}
			}
			// Else, the algorithm will automatically found the latest index.
		}

		private void	OnScrollUpdated(float offset)
		{
			Vars	vars = perWindowVars.Get(RowUtility.drawingWindow);

			vars.abortSmoothScrolling = true;

			if (vars.lastStreamIndex < this.consoleIndexes.Count)
				this.UpdateCachedIndexAndHeight(vars);
			else
				vars.mustRefreshCachePosition = true;

			vars.originalScrollOffset = vars.scrollbar.Offset;
		}
	}
}