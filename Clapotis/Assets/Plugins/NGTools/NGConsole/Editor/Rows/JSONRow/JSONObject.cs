using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	internal class JSONObject : IJSONObject
	{
		public const float	IndentSpace = 10F;
		public const string	IndentChar = " ";

		private static readonly GUIContent	Error = new GUIContent("Error detected");
		private static readonly GUIContent	ErrorTooltip = new GUIContent("Parsing the JSON element did not completed properly.");
		private static GUIStyle	style;

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

				if (this.children != null && Event.current != null && Event.current.alt == true)
				{
					for (int i = 0; i < this.children.Count; i++)
						this.children[i].Open = value;
				}
			}
		}

		private bool				invalidJSON;
		private float				height = -1F;
		private int					closerCharIndex = -1;
		private List<IJSONObject>	children;

		public	JSONObject(string raw, ref int i, int max, int closerCharIndex)
		{
			this.closerCharIndex = closerCharIndex;

			for (; i < max; i++)
			{
				if (JSONParseUtility.IsBlankChar(raw[i]) == true ||
					raw[i] == JSONRow.JSONKeySeparator ||
					raw[i] == JSONRow.JSONValueSeparator)
				{
					continue;
				}

				if (raw[i] == JSONRow.Closers[this.closerCharIndex][0])
					break;

				int	j = JSONParseUtility.IsOpener(raw[i]);

				if (j != -1)
				{
					if (this.children == null)
						this.children = new List<IJSONObject>();

					++i;
					this.children.Add(new JSONObject(raw, ref i, max, j));
				}
				else
				{
					if (this.children == null)
						this.children = new List<IJSONObject>();

					j = i;
					JSONPairKeyValue	element = new JSONPairKeyValue(raw, ref i, max);
					if (i > j)
						this.children.Add(element);
					else
						break;
				}
			}

			if (i >= max)
				this.invalidJSON = true;
		}

		float	IJSONObject.GetHeight()
		{
			if (this.height < 0F)
			{
				this.height = Constants.SingleLineHeight;

				if (this.Open == true && this.children != null)
				{
					this.height += Constants.SingleLineHeight;

					for (int i = 0; i < this.children.Count; i++)
						this.height += this.children[i].GetHeight();
				}
			}

			return this.height;
		}

		void	IJSONObject.Draw(Rect r, float offset)
		{

   			bool	mouseContained = r.yMin < Event.current.mousePosition.y && Event.current.mousePosition.y < r.yMax;

			r.height = Constants.SingleLineHeight;

			if (offset > 0F)
				r.xMin += offset;

			if (this.Open == false || this.children == null)
			{
				if (this.children != null)
					Utility.content.text = JSONRow.OpenClose[this.closerCharIndex];
				else
					Utility.content.text = JSONRow.OpenCloseEmpty[this.closerCharIndex];
			}
			else
				Utility.content.text = JSONRow.Openers[this.closerCharIndex];

			JSONObject.style = new GUIStyle(HQ.Settings.Get<LogSettings>().Style);

			Color	restoreColor = JSONObject.style.normal.textColor;

			if (mouseContained == true)
				JSONObject.style.normal.textColor = Color.yellow;
			else if (this.invalidJSON == true)
				JSONObject.style.normal.textColor = Color.red;

			GUI.Label(r, Utility.content, JSONObject.style);

			if (this.invalidJSON == true)
			{
				float	openerWidth = GUI.skin.label.CalcSize(Utility.content).x;
				float	errorWidth = GeneralStyles.SmallLabel.CalcSize(JSONObject.Error).x;
				float	restoreWidth = r.width;

				r.xMin += openerWidth;
				r.width = errorWidth;
				GUI.Label(r, JSONObject.Error, GeneralStyles.SmallLabel);

				if (r.Contains(Event.current.mousePosition) == true)
				{
					r.x += errorWidth;
					r.width = GeneralStyles.SmallLabel.CalcSize(JSONObject.ErrorTooltip).x;
					EditorGUI.DrawRect(r, Color.red);
					GUI.Label(r, JSONObject.ErrorTooltip, GeneralStyles.SmallLabel);
					r.x -= errorWidth;
				}

				r.width = restoreWidth;
				r.xMin -= openerWidth;
			}

			if (offset > 0F)
				r.xMin -= offset;

			if (this.children != null)
			{
				r.x -= 10F;
				EditorGUI.BeginChangeCheck();
				EditorGUI.Foldout(r, this.Open, string.Empty, true);
				if (EditorGUI.EndChangeCheck() == true)
					this.Open = !this.Open;
				r.x += 10F;
			}

			r.y += r.height;

			if (this.Open == true && this.children != null)
			{
				float	yMin = r.yMin;

				r.xMin += JSONObject.IndentSpace;

				EditorGUI.BeginChangeCheck();
				for (int i = 0; i < this.children.Count; i++)
				{
					r.height = this.children[i].GetHeight();
					this.children[i].Draw(r);
					r.y += r.height;
				}
				if (EditorGUI.EndChangeCheck() == true)
					this.height = -1F;

				r.xMin -= JSONObject.IndentSpace;

				if (mouseContained == true)
					JSONObject.style.normal.textColor = Color.yellow;
				else if (this.invalidJSON == true)
					JSONObject.style.normal.textColor = Color.red;
				else
					JSONObject.style.normal.textColor = GUI.skin.label.normal.textColor;

				GUI.Label(r, JSONRow.Closers[this.closerCharIndex], JSONObject.style);

				if (mouseContained == true &&
					Event.current.type == EventType.Repaint)
				{
					EditorGUI.DrawRect(new Rect(r.x - 4F, yMin, 1F, r.yMin + Constants.SingleLineHeight - yMin - 8F), Color.grey);
					EditorGUI.DrawRect(new Rect(r.x - 4F, r.yMin + Constants.SingleLineHeight - 8F, 5F, 1F), Color.grey);
				}
			}

			JSONObject.style.normal.textColor = restoreColor;
		}

		void	IJSONObject.Copy(StringBuilder buffer, bool forceFullExploded, string indent, bool skipIndent)
		{
			buffer.AppendLine((skipIndent == false ? indent : string.Empty) + JSONRow.Openers[this.closerCharIndex]);

			if ((forceFullExploded == true || this.Open == true) && this.children != null)
			{
				for (int i = 0; i < this.children.Count; i++)
				{
					if (i > 0)
					{
						buffer.Append(JSONRow.JSONValueSeparator);
						buffer.AppendLine();
					}

					this.children[i].Copy(buffer, forceFullExploded, indent + JSONObject.IndentChar);
				}

				buffer.AppendLine();
			}

			buffer.Append(indent + JSONRow.Closers[this.closerCharIndex]);
		}

		public override string	ToString()
		{
			return (this.closerCharIndex == 0 ? "Object" : "Array") + (this.children != null ? "(" + this.children.Count.ToString() + ")" : "(0)");
		}
	}
}