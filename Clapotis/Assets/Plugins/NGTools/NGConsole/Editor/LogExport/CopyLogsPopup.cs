using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	internal sealed class CopyLogsPopup : PopupWindowContent
	{
		public const float	DefaultWidth = 500F;
		public const float	DefaultHeight = 400F;

		private LogsExporterDrawer	drawer;
		private List<Row>			rows;

		public	CopyLogsPopup(List<Row> rows)
		{
			this.rows = rows;
		}

		public override Vector2	GetWindowSize()
		{
			return new Vector2(CopyLogsPopup.DefaultWidth, CopyLogsPopup.DefaultHeight);
		}

		public override void	OnOpen()
		{
			base.OnOpen();

			this.drawer = new LogsExporterDrawer(this.editorWindow, rows);
		}

		public override void	OnClose()
		{
			base.OnClose();

			this.drawer.Save();
		}

		public override void	OnGUI(Rect rect)
		{
			rect.yMax -= 32F;
			this.drawer.OnGUI(rect);
			rect.yMax += 32F;

			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.C && Event.current.shift == true)
			{
				this.Confirm();
				Event.current.Use();
			}

			rect.xMin = rect.xMax - 190F;
			rect.width -= 75F;
			rect.yMin = rect.yMax - 32F;
			rect.height -= 2F;
			EditorGUI.HelpBox(rect, "Press Shift-C to", MessageType.Info);

			rect.width += 75F;
			rect.xMin += 115F;
			if (GUI.Button(rect, "Copy") == true)
				this.Confirm();
		}

		private void	Confirm()
		{
			EditorGUIUtility.systemCopyBuffer = this.drawer.Export();
			this.editorWindow.Close();
		}
	}
}