using System;
using System.Collections.Generic;
using UnityEditor;

namespace NGToolsEditor.NGConsole
{
	using UnityEngine;

	[Serializable]
	internal sealed class ToggleTagsFilter : ILogFilter
	{
		public string	Name { get { return "By log tags"; } }
		[Exportable]
		private bool	enabled;
		public bool		Enabled { get { return this.enabled; } set { if (this.enabled != value) { this.enabled = value; if (this.ToggleEnable != null) this.ToggleEnable(); } } }

		public event Action	ToggleEnable;

		[Exportable]
		private List<string>	acceptedTags = new List<string>();
		private string			newTag = string.Empty;

		public FilterResult	CanDisplay(Row row)
		{
			MultiTagsRow	tagsRows = row as MultiTagsRow;
			if (tagsRows == null)
				return FilterResult.Refused;

			if (tagsRows.isParsed == false)
				tagsRows.ParseLog();

			for (int i = 0; i < tagsRows.tags.Length; i++)
			{
				for (int j = 0; j < this.acceptedTags.Count; j++)
				{
					if (tagsRows.tags[i] == this.acceptedTags[j])
						return FilterResult.Accepted;
				}
			}

			return FilterResult.Refused;
		}

		public Rect	OnGUI(Rect r, bool compact)
		{
			float	xMax = r.xMax;

			if (compact == false)
			{
				GUI.Box(r, string.Empty, GeneralStyles.Toolbar);
				Utility.content.text = LC.G("AcceptedTags");
				r.width = GUI.skin.label.CalcSize(Utility.content).x + 10F; // Add a small margin for clarity.
				GUI.Label(r, Utility.content);
				r.x += r.width;
			}

			for (int i = 0; i < this.acceptedTags.Count; i++)
			{
				Utility.content.text = this.acceptedTags[i];
				r.width = GeneralStyles.ToolbarButton.CalcSize(Utility.content).x;
				if (GUI.Button(r, Utility.content, GeneralStyles.ToolbarButton) == true)
				{
					this.acceptedTags.RemoveAt(i);
					break;
				}
				r.x += r.width;
			}

			r.width = xMax - r.x - 16F;
			this.newTag = EditorGUI.TextField(r, this.newTag);
			r.x += r.width;

			r.width = 16F;
			if (GUI.Button(r, "+", GeneralStyles.ToolbarButton) == true && string.IsNullOrEmpty(this.newTag) == false)
			{
				this.acceptedTags.Add(this.newTag);
				this.newTag = string.Empty;
			}
			r.y += r.height + 2F;

			return r;
		}

		public void	ContextMenu(GenericMenu menu, Row row, int i)
		{
		}
	}
}