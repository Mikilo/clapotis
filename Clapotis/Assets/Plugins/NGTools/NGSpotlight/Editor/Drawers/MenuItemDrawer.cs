using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGSpotlight
{
	public sealed class MenuItemDrawer : IDrawableElement
	{
		private static GUIStyle	style;

		private string	path;
		private string	lowerPath;

		private int		lastChange;
		private string	cachedHighlightedName;

		string	IDrawableElement.RawContent { get { return this.path; } }
		string	IDrawableElement.LowerStringContent { get { return this.lowerPath; } }

		public	MenuItemDrawer(string path)
		{
			this.path = path;
			this.lowerPath = path.ToLower();
		}

		void	IDrawableElement.OnGUI(Rect r, NGSpotlightWindow window, EntryRef k, int i)
		{
			if (MenuItemDrawer.style == null)
			{
				MenuItemDrawer.style = new GUIStyle(EditorStyles.label);
				MenuItemDrawer.style.alignment = TextAnchor.MiddleLeft;
				MenuItemDrawer.style.padding.left = 32;
				MenuItemDrawer.style.fontSize = 15;
				MenuItemDrawer.style.richText = true;
			}

			GUI.Box(r, "");

			if (Event.current.type == EventType.Repaint)
			{
				if (r.Contains(Event.current.mousePosition) == true)
					Utility.DrawUnfillRect(r, HQ.Settings != null ? HQ.Settings.Get<SpotlightSettings>().hoverSelectionColor : NGSpotlightWindow.HighlightedEntryColor);
				else if (window.selectedEntry == i)
					Utility.DrawUnfillRect(r, HQ.Settings != null ? HQ.Settings.Get<SpotlightSettings>().outlineSelectionColor : NGSpotlightWindow.SelectedEntryColor);
			}

			if (this.lastChange != window.changeCount)
			{
				this.lastChange = window.changeCount;
				this.cachedHighlightedName = window.HighlightWeightContent(this.lowerPath, this.path, window.cleanLowerKeywords);
			}

			//GUI.DrawTexture(iconR, this.icon, ScaleMode.ScaleToFit);
			GUI.Label(r, this.cachedHighlightedName, MenuItemDrawer.style);

			if ((Event.current.type == EventType.KeyDown && window.selectedEntry == i && Event.current.keyCode == KeyCode.Return) ||
				(Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition) == true))
			{
				if (EditorApplication.ExecuteMenuItem(this.path) == true)
					NGSpotlightWindow.UseEntry(k);
				window.Close();
				Event.current.Use();
			}
		}

		void	IDrawableElement.Select(NGSpotlightWindow window, EntryRef key)
		{
		}

		void	IDrawableElement.Execute(NGSpotlightWindow window, EntryRef key)
		{
			if (EditorApplication.ExecuteMenuItem(this.path) == true)
				NGSpotlightWindow.UseEntry(key);
		}
	}
}