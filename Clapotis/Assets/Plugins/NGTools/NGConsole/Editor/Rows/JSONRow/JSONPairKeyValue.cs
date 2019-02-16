using System.Text;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	internal class JSONPairKeyValue : IJSONObject
	{
		private bool	open;
		public bool		Open
		{
			get
			{
				return this.open;
			}
			set
			{
				this.open = value;
				this.height = -1F;

				if (this.child != null && Event.current != null && Event.current.alt == true)
					this.child.Open = value;
			}
		}

		private float		height = -1F;
		private bool		hovering;
		private string		content;
		private IJSONObject	child;

		public	JSONPairKeyValue(string raw, ref int i, int max)
		{
			for (; i < max; i++)
			{
				if (JSONParseUtility.IsBlankChar(raw[i]) == true ||
					raw[i] == JSONRow.JSONKeySeparator)
				{
					continue;
				}

				if (raw[i] == JSONRow.JSONValueSeparator)
					break;

				if (JSONParseUtility.IsCloser(raw[i]) != -1)
				{
					--i;
					break;
				}

				int	j = JSONParseUtility.IsOpener(raw[i]);

				if (j != -1)
				{
					++i;
					this.child = new JSONObject(raw, ref i, max, j);
				}
				else
				{
					int	startOffset = JSONParseUtility.DigestString(raw, ref i, max);

					if (this.content == null)
						this.content = raw.Substring(startOffset, i - startOffset + 1);
					else
					{
						this.child = new JSONValue(raw.Substring(startOffset, i - startOffset + 1));
						break;
					}
				}
			}
		}

		float	IJSONObject.GetHeight()
		{
			if (this.height < 0F)
			{
				if (this.content != null)
				{
					if (this.child != null)
						this.height = this.child.GetHeight();
					else
						this.height = Constants.SingleLineHeight;
				}
				else
					this.height = this.child.GetHeight();
			}

			return this.height;
		}

		void	IJSONObject.Draw(Rect r, float offset)
		{
			r.height = Constants.SingleLineHeight;

			if (this.content != null)
			{
				Utility.content.text = this.content;
				float	restoreWdith = r.width;
				float	width = HQ.Settings.Get<LogSettings>().Style.CalcSize(Utility.content).x;

				r.width = width;

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
					r.x -= 1F;
					r.width += 3F;
					EditorGUI.TextField(r, this.content);
					r.width -= 3F;
					r.x += 1F;
				}
				else
					GUI.Label(r, this.content, HQ.Settings.Get<LogSettings>().Style);

				if (this.child != null)
				{
					r.x += r.width;
					GUI.Label(r, ":", HQ.Settings.Get<LogSettings>().Style);
					r.x -= r.width;

					width += 8F;

					EditorGUI.BeginChangeCheck();
					r.width = restoreWdith;
					r.height = this.child.GetHeight();
					this.child.Draw(r, width);
					if (EditorGUI.EndChangeCheck() == true)
						this.height = -1F;
				}
			}
			else
			{
				EditorGUI.BeginChangeCheck();
				this.child.Draw(r, offset);
				if (EditorGUI.EndChangeCheck() == true)
					this.height = -1F;
			}
		}

		void	IJSONObject.Copy(StringBuilder buffer, bool forceFullExploded, string indent, bool skipIndent)
		{
			if (this.content != null)
			{
				if (this.child != null)
				{
					buffer.Append(indent + this.content + " " + JSONRow.JSONKeySeparator + " ");
					this.child.Copy(buffer, forceFullExploded, indent, true);
				}
				else
					buffer.Append((skipIndent == false ? indent : string.Empty) + this.content);
			}
			else
				this.child.Copy(buffer, forceFullExploded);
		}

		public override string	ToString()
		{
			return this.content + (this.child != null ? "=" + this.child : "(0)");
		}
	}
}