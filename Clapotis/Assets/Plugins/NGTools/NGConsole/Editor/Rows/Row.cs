using System;
using System.Collections.Generic;
using UnityEditor;

namespace NGToolsEditor.NGConsole
{
	using UnityEngine;

	[Serializable]
	public abstract class Row
	{
		/// <summary>Defines if one or more components have consumed this Row.</summary>
		public bool	isConsumed;

		[Exportable]
		public LogEntry	log;

		[NonSerialized]
		protected NGConsoleWindow	editor;
		[NonSerialized]
		protected Dictionary<string, Func<object, object>>	commands;

		public virtual void	Init(NGConsoleWindow editor, LogEntry log)
		{
			this.editor = editor;
			this.log = log;

			this.commands = new Dictionary<string, Func<object, object>>();
		}

		public virtual void	Uninit()
		{
		}

		/// <summary>
		/// Returns the current width of the <paramref name="row"/>.
		/// </summary>
		/// <param name="row"></param>
		/// <returns></returns>
		public abstract float	GetWidth();

		/// <summary>
		/// Returns the current height of the <paramref name="row"/>.
		/// </summary>
		/// <param name="rowsDrawer">The drawer.</param>
		/// <param name="row"></param>
		/// <returns></returns>
		public abstract float	GetHeight(RowsDrawer rowsDrawer);

		/// <summary>
		/// </summary>
		/// <param name="rowsDrawer">The drawer.</param>
		/// <param name="rect">The area to work on.</param>
		/// <param name="streamIndex">Index from the RowsDrawer.rows[].</param>
		/// <param name="collapse">Defines if row should displayed its collapse label. Do not used it.</param>
		public abstract void	DrawRow(RowsDrawer rowsDrawer, Rect rect, int streamIndex, bool? collapse);

		public virtual object	Command(string commandName, object data)
		{
			Func<object, object>	callback;

			if (this.commands.TryGetValue(commandName, out callback) == true)
				return callback(data) ?? string.Empty;
			return string.Empty;
		}

		/// <summary>
		/// Draws background when focus, even or odd.
		/// </summary>
		/// <param name="rowsDrawer">The drawer.</param>
		/// <param name="r">The area to work on.</param>
		/// <param name="streamIndex">Index from the RowsDrawer.rows[].</param>
		protected void	DrawBackground(RowsDrawer rowsDrawer, Rect r, int streamIndex)
		{
			if (Event.current.type != EventType.Repaint)
				return;

			if (rowsDrawer.currentVars.IsSelected(streamIndex) == true)
			{
				if (EditorGUIUtility.keyboardControl != 0)
					EditorGUI.DrawRect(r, HQ.Settings.Get<LogSettings>().selectedBackground * .8F);
				else
					EditorGUI.DrawRect(r, HQ.Settings.Get<LogSettings>().selectedBackground);
			}
			else if ((streamIndex & 1) == 0)
				EditorGUI.DrawRect(r, HQ.Settings.Get<LogSettings>().evenBackground);
			else
				EditorGUI.DrawRect(r, HQ.Settings.Get<LogSettings>().oddBackground);
		}

		/// <summary>
		/// Handles events to toggle Row from the selection.
		/// </summary>
		/// <param name="rowsDrawer">The drawer.</param>
		/// <param name="r">The area to work on.</param>
		/// <param name="streamIndex">Index from the RowsDrawer.rows[].</param>
		protected void	HandleDefaultSelection(RowsDrawer rowsDrawer, Rect r, int streamIndex)
		{
			if (r.Contains(Event.current.mousePosition) == true)
			{
				// Focus on left click.
				if (Event.current.type == EventType.MouseDown &&
					Event.current.button == 0)
				{
					// Handle multi-selection.
					if (Event.current.control == true &&
						rowsDrawer.currentVars.IsSelected(streamIndex) == true)
					{
						rowsDrawer.currentVars.RemoveSelection(streamIndex);
					}
					else
					{
						if (Event.current.control == false)
							rowsDrawer.currentVars.ClearSelection();

						rowsDrawer.currentVars.AddSelection(streamIndex);

						if (Event.current.control == false)
							rowsDrawer.FitFocusedLogInScreen(streamIndex);
					}

					RowUtility.drawingWindow.Repaint();

					Event.current.Use();
				}
			}
		}

		protected Rect	DrawPreLogData(RowsDrawer rowsDrawer, Rect r)
		{
			LogSettings	settings = HQ.Settings.Get<LogSettings>();

			// Draw time.
			if (settings.displayTime == true)
			{
				Utility.content.text = this.log.time;
				r.width = settings.TimeStyle.CalcSize(Utility.content).x;
				GUI.Label(r, Utility.content, settings.TimeStyle);
				r.x += r.width;
			}

			// Draw frame count.
			if (settings.displayFrameCount == true)
			{
				Utility.content.text = this.log.frameCount.ToString();
				r.width = settings.TimeStyle.CalcSize(Utility.content).x;
				GUI.Label(r, Utility.content, settings.TimeStyle);
				r.x += r.width;
			}

			// Draw rendered frame count.
			if (settings.displayRenderedFrameCount == true)
			{
				Utility.content.text = this.log.renderedFrameCount.ToString();
				r.width = settings.TimeStyle.CalcSize(Utility.content).x;
				GUI.Label(r, Utility.content, settings.TimeStyle);
				r.x += r.width;
			}

			return r;
		}
	}
}