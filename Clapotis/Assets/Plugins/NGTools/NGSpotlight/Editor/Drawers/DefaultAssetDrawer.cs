using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;

namespace NGToolsEditor.NGSpotlight
{
	using UnityEngine;

	public sealed class DefaultAssetDrawer : IDrawableElement
	{
		public const float	Spacing = 2F;

		private static GUIStyle		style;
		private static Texture2D	defaultIcon;

		public string	path;
		public Type		type;
		public Object	asset;
		public string	name;
		public string	lowerName;

		private Texture2D	icon;
		private int			lastChange = -1;
		private string		cachedHighlightedName;
		private bool		iconIsPreview;

		string	IDrawableElement.RawContent { get { return this.path; } }
		string	IDrawableElement.LowerStringContent { get { return this.lowerName; } }

		public	DefaultAssetDrawer(string path, Type type, bool iconIsPreview = false)
		{
			this.path = path;
			this.type = type;
			this.name = Path.GetFileName(path);
			this.lowerName = this.name.ToLower();
			this.iconIsPreview = iconIsPreview;
		}

		void	IDrawableElement.OnGUI(Rect r, NGSpotlightWindow window, EntryRef k, int i)
		{
			// Init once.
			if (this.lastChange == -1)
				this.asset = AssetDatabase.LoadAssetAtPath(this.path, typeof(Object));

			if (this.asset == null)
			{
				NGSpotlightWindow.DeleteEntry(k.key, k.i);
				return;
			}

			if (DefaultAssetDrawer.style == null)
			{
				DefaultAssetDrawer.style = new GUIStyle(EditorStyles.label);
				DefaultAssetDrawer.style.alignment = TextAnchor.MiddleLeft;
				DefaultAssetDrawer.style.fontSize = 15;
				DefaultAssetDrawer.style.richText = true;

				DefaultAssetDrawer.defaultIcon = InternalEditorUtility.GetIconForFile(".png");
			}

			if (this.iconIsPreview == true && this.asset != null && (this.icon == null || this.icon == DefaultAssetDrawer.defaultIcon))
			{
				this.icon = AssetPreview.GetAssetPreview(this.asset);
				window.Repaint();
			}

			if (this.icon == null)
				this.icon = DefaultAssetDrawer.defaultIcon;

			Rect	iconR = r;
			iconR.width = iconR.height;

			GUI.Box(r, "");
			GUI.DrawTexture(iconR, this.icon, ScaleMode.ScaleToFit);

			if (this.lastChange != window.changeCount)
			{
				this.lastChange = window.changeCount;
				this.cachedHighlightedName = window.HighlightWeightContent(this.lowerName, this.name, window.cleanLowerKeywords);
			}

			if (Event.current.type == EventType.Repaint)
			{
				if (r.Contains(Event.current.mousePosition) == true)
					Utility.DrawUnfillRect(r, HQ.Settings != null ? HQ.Settings.Get<SpotlightSettings>().hoverSelectionColor : NGSpotlightWindow.HighlightedEntryColor);
				if (window.selectedEntry == i)
					Utility.DrawUnfillRect(r, HQ.Settings != null ? HQ.Settings.Get<SpotlightSettings>().outlineSelectionColor : NGSpotlightWindow.SelectedEntryColor);
			}
			else if (Event.current.type == EventType.MouseDrag)
			{
				if (i.Equals(DragAndDrop.GetGenericData("i")) == true)
				{
					DragAndDrop.StartDrag("Drag Asset");
					Event.current.Use();
				}
			}
			else if (Event.current.type == EventType.MouseDown)
			{
				if (r.Contains(Event.current.mousePosition) == true)
				{
					DragAndDrop.PrepareStartDrag();
					DragAndDrop.SetGenericData("i", i);
					DragAndDrop.objectReferences = new Object[] { this.asset };
				}
			}
			else if (Event.current.type == EventType.DragExited)
				DragAndDrop.PrepareStartDrag();

			if ((Event.current.type == EventType.KeyDown && window.selectedEntry == i && Event.current.keyCode == KeyCode.Return) ||
				(Event.current.type == EventType.MouseUp && r.Contains(Event.current.mousePosition) == true && i.Equals(DragAndDrop.GetGenericData("i")) == true))
			{
				if (Event.current.type == EventType.MouseUp && r.Contains(Event.current.mousePosition) == true)
					DragAndDrop.PrepareStartDrag();

				if (window.selectedEntry == i || Event.current.button != 0)
				{
					NGSpotlightWindow.UseEntry(k);
					Selection.activeObject = this.asset;
					window.Close();
				}
				else
					window.SelectEntry(i);

				Event.current.Use();
			}

			r.xMin += iconR.width;
			GUI.Label(r, this.cachedHighlightedName, DefaultAssetDrawer.style);
		}

		void	IDrawableElement.Select(NGSpotlightWindow window, EntryRef key)
		{
			EditorGUIUtility.PingObject(this.asset);
		}

		void	IDrawableElement.Execute(NGSpotlightWindow window, EntryRef key)
		{
			NGSpotlightWindow.UseEntry(key);
			EditorGUIUtility.PingObject(this.asset);
			Selection.activeObject = this.asset;
		}

		public override string ToString()
		{
			return this.path;
		}
	}
}