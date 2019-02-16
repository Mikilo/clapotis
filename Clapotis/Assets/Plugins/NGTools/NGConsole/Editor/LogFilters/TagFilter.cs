using System;
using UnityEditor;

namespace NGToolsEditor.NGConsole
{
	using UnityEngine;

	[Serializable]
	internal sealed class TagFilter : ILogFilter
	{
		public string	Name { get { return "By tag"; } }
		[Exportable]
		private bool	enabled;
		public bool		Enabled { get { return this.enabled; } set { if (this.enabled != value) { this.enabled = value; if (this.ToggleEnable != null) this.ToggleEnable(); } } }

		public event Action	ToggleEnable;

		[Exportable]
		private string		tag;

		public FilterResult	CanDisplay(Row row)
		{
			if (string.IsNullOrEmpty(this.tag) == true)
				return FilterResult.None;

			ILogContentGetter	logContent = row as ILogContentGetter;

			if (logContent == null)
				return FilterResult.None;

			GameObject	gameObject = EditorUtility.InstanceIDToObject(row.log.instanceID) as GameObject;

			if (gameObject != null && gameObject.CompareTag(this.tag) == true)
				return FilterResult.Accepted;
			return FilterResult.None;
		}

		public Rect	OnGUI(Rect r, bool compact)
		{
			GUI.Box(r, string.Empty, GeneralStyles.Toolbar);
			r.xMax -= 16F;
			r.y += 2F;
			this.tag = EditorGUI.TextField(r, compact == false ? LC.G("Tag") : string.Empty, this.tag, GeneralStyles.ToolbarSearchTextField);

			if (compact == true && string.IsNullOrEmpty(this.tag) == true)
			{
				r.x += 15F;
				EditorGUI.LabelField(r, LC.G("Tag"), GeneralStyles.TextFieldPlaceHolder);
				r.x -= 15F;
			}

			r.x += r.width;

			r.width = 16F;
			if (GUI.Button(r, GUIContent.none, GeneralStyles.ToolbarSearchCancelButton) == true)
			{
				this.tag = string.Empty;
				GUI.FocusControl(null);
			}
			r.y += r.height;
			return r;
		}

		public void	ContextMenu(GenericMenu menu, Row row, int i)
		{
			if (row is ILogContentGetter)
			{
				GameObject	gameObject = EditorUtility.InstanceIDToObject(row.log.instanceID) as GameObject;

				if (gameObject != null)
					menu.AddItem(new GUIContent("#" + i + " " + LC.G("FilterByTag")), false, this.ActiveFilter, gameObject);
			}
		}

		private void	ActiveFilter(object data)
		{
			GameObject	gameObject = data as GameObject;

			this.tag = gameObject.tag;
			this.Enabled = true;
		}
	}
}