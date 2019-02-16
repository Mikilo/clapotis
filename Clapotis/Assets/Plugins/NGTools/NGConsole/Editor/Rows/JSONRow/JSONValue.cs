using System.Text;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	internal class JSONValue : IJSONObject
	{
		internal static object	SelectedValue;

		public bool	Open { get; set; }

		private string	content;
		private bool	hovering;

		public	JSONValue(string content)
		{
			this.content = content;
		}

		float	IJSONObject.GetHeight()
		{
			return Constants.SingleLineHeight;
		}

		void	IJSONObject.Draw(Rect r, float offset)
		{
			if (offset > 0F)
				r.xMin += offset;

			if (Event.current.type == EventType.MouseDown)
			{
				if (r.Contains(Event.current.mousePosition) == true)
					JSONValue.SelectedValue = this;
			}
			else if (Event.current.type == EventType.MouseMove)
			{
				if (r.Contains(Event.current.mousePosition) == true)
				{
					if (hovering == false)
					{
						hovering = true;
						GUI.FocusControl(null);
					}
				}
				else
					hovering = false;
			}

			if (hovering == true || JSONValue.SelectedValue == this)
			{
				r.xMin -= 1F;
				EditorGUI.TextField(r, this.content);
			}
			else
				GUI.Label(r, this.content, HQ.Settings.Get<LogSettings>().Style);
		}

		void	IJSONObject.Copy(StringBuilder buffer, bool forceFullExploded, string indent, bool skipIndent)
		{
			buffer.Append((skipIndent == false ? indent : string.Empty) + this.content);
		}

		public override string	ToString()
		{
			return this.content;
		}
	}
}