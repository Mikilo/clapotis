using NGTools.NGGameConsole;
using NGToolsEditor.NGConsole;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGGameConsole
{
	[Serializable]
	internal sealed class RemoteRow : Row
	{
		private static readonly GUIContent	LogPrefix = new GUIContent(">");
		private static float				LogPrefixHeight = -1F;

		public string	error;
		public string	result;

		private float	subHeight;

		public	RemoteRow()
		{
			this.error = null;
			this.result = null;
		}

		public override float	GetWidth()
		{
			return 0F;
		}

		public override float	GetHeight(RowsDrawer rowsDrawer)
		{
			LogSettings	settings = HQ.Settings.Get<LogSettings>();
			float		height = settings.height;

			// An error occurred.
			if (this.error != null)
				this.subHeight = settings.ContentStyle.CalcSize(new GUIContent(this.error)).y;
			// Waiting for answer.
			else if (this.result == null)
				this.subHeight = settings.ContentStyle.CalcSize(new GUIContent(this.log.file)).y;
			// Answer received.
			else
				this.subHeight = settings.ContentStyle.CalcSize(new GUIContent(this.result)).y;

			height += this.subHeight;

			return height;
		}

		public override void	DrawRow(RowsDrawer rowsDrawer, Rect r, int i, bool? collapse)
		{
			LogSettings	settings = HQ.Settings.Get<LogSettings>();

			if (RemoteRow.LogPrefixHeight < 0F)
				RemoteRow.LogPrefixHeight = settings.Style.CalcSize(RemoteRow.LogPrefix).x;

			float	originWidth = RowUtility.drawingWindow.position.width - rowsDrawer.verticalScrollbarWidth;

			// Draw highlight.
			r.x = 0F;
			r.width = originWidth;
			r.height = settings.height;

			this.DrawBackground(rowsDrawer, r, i);

			this.HandleDefaultSelection(rowsDrawer, r, i);

			GUI.Label(r, this.log.condition, settings.Style);
			r.y += r.height;

			r.width = RemoteRow.LogPrefixHeight;

			// An error occurred.
			if (this.error != null)
			{
				GUI.Label(r, "<color=" + NGCLI.ErrorCommandColor + ">></color>", settings.Style);

				r.x += r.width;
				r.width = originWidth - r.x;
				r.height = this.subHeight;
				EditorGUI.SelectableLabel(r, this.error, settings.ContentStyle);
			}
			// Waiting for answer.
			else if (this.result == null)
			{
				GUI.Label(r, "<color=" + NGCLI.PendingCommandColor + ">></color>", settings.Style);

				r.x += r.width;
				r.width = originWidth - r.x;
				r.height = this.subHeight;
				// Use file to display the default value from RemoteCommand while waiting for the answer.
				EditorGUI.SelectableLabel(r, this.log.file, settings.ContentStyle);
			}
			// Answer received.
			else
			{
				GUI.Label(r, "<color=" + NGCLI.ReceivedCommandColor + ">></color>", settings.Style);

				r.x += r.width;
				r.width = originWidth - r.x;
				r.height = this.subHeight;
				EditorGUI.SelectableLabel(r, this.result, settings.ContentStyle);
			}
		}
	}
}