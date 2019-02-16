using System;
using UnityEditor;

namespace NGToolsEditor.NGConsole
{
	using UnityEngine;

	[Serializable]
	internal sealed class NameHierarchyFilter : ILogFilter
	{
		public string	Name { get { return "By name"; } }
		[Exportable]
		private bool	enabled;
		public bool		Enabled { get { return this.enabled; } set { if (this.enabled != value) { this.enabled = value; if (this.ToggleEnable != null) this.ToggleEnable(); } } }

		public event Action	ToggleEnable;

		[Exportable]
		private string	name;

		public FilterResult	CanDisplay(Row row)
		{
			if (row.log.instanceID == 0 ||
				string.IsNullOrEmpty(this.name) == true)
			{
				return FilterResult.None;
			}

			Object	@object = EditorUtility.InstanceIDToObject(row.log.instanceID);

			if (@object != null && @object.name.Contains(this.name))
				return FilterResult.Accepted;
			return FilterResult.None;
		}

		public Rect	OnGUI(Rect r, bool compact)
		{
			GUI.Box(r, string.Empty, GeneralStyles.Toolbar);
			r.xMax -= 16F;
			r.y += 2F;
			this.name = EditorGUI.TextField(r, compact == false ? LC.G("GameObjectWithName") : string.Empty, this.name, GeneralStyles.ToolbarSearchTextField);

			if (compact == true && string.IsNullOrEmpty(this.name) == true)
			{
				r.x += 15F;
				EditorGUI.LabelField(r, LC.G("GameObjectWithName"), GeneralStyles.TextFieldPlaceHolder);
				r.x -= 15F;
			}

			r.x += r.width;

			r.width = 16F;
			if (GUI.Button(r, GUIContent.none, GeneralStyles.ToolbarSearchCancelButton) == true)
			{
				this.name = string.Empty;
				GUI.FocusControl(null);
			}

			r.y += r.height;
			return r;
		}

		public void	ContextMenu(GenericMenu menu, Row row, int i)
		{
			if (row.log.instanceID != 0)
				menu.AddItem(new GUIContent("#" + i + " " + LC.G("FilterByThisObjectName")), false, this.ActiveFilter, row);
		}

		private void	ActiveFilter(object data)
		{
			Row	row = data as Row;
			this.name = EditorUtility.InstanceIDToObject(row.log.instanceID).name;
			this.Enabled = true;
		}
	}
}