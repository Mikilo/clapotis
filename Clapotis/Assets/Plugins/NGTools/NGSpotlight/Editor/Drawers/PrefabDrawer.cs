using System.IO;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGSpotlight
{
	public sealed class PrefabDrawer : IDrawableElement, IHasGameObject
	{
		public const float	Spacing = 2F;

		private static GUIStyle		style;

		public string		path;
		public string		name;
		public string		lowerName;
		public GameObject	go;

		private int		lastChange = -1;
		private string	cachedHierarchy;
		private string	cachedHighlightedName;

		string		IDrawableElement.RawContent { get { return this.path; } }
		string		IDrawableElement.LowerStringContent { get { return this.lowerName; } }
		GameObject	IHasGameObject.GameObject { get { return this.go; } }

		public	PrefabDrawer(string path)
		{
			this.path = path;
			this.name = Path.GetFileName(path);
			this.lowerName = this.name.ToLower();
		}

		void	IDrawableElement.OnGUI(Rect r, NGSpotlightWindow window, EntryRef k, int i)
		{
			// Init once.
			if (this.lastChange == -1)
				this.go = AssetDatabase.LoadAssetAtPath<GameObject>(this.path);

			if (this.go == null)
			{
				NGSpotlightWindow.DeleteEntry(k.key, k.i);
				return;
			}

			if (PrefabDrawer.style == null)
			{
				PrefabDrawer.style = new GUIStyle(EditorStyles.label);
				PrefabDrawer.style.alignment = TextAnchor.MiddleLeft;
				PrefabDrawer.style.fontSize = 15;
				PrefabDrawer.style.richText = true;
			}

			Rect	iconR = r;
			iconR.width = iconR.height;

			GUI.Box(r, "");
			GUI.DrawTexture(iconR, UtilityResources.PrefabIcon, ScaleMode.ScaleToFit);

			if (this.lastChange != window.changeCount)
			{
				this.lastChange = window.changeCount;

				if (this.cachedHierarchy == null)
				{
					if (this.go != null && this.go.transform.parent != null)
						//this.cachedHierarchy = Utility.GetHierarchyStringified(this.go.transform.parent) + '/';
						this.cachedHierarchy = "<color=teal><size=9>" + this.go.transform.parent.name + '/' + "</size></color>";
					else
						this.cachedHierarchy = string.Empty;
				}

				this.cachedHighlightedName = this.cachedHierarchy + window.HighlightWeightContent(this.lowerName, this.name, window.cleanLowerKeywords);
				//this.cachedHighlightedName = window.HighlightWeightContent(this.lowerName, this.name, window.cleanLowerKeywords);
			}

			if (Event.current.type == EventType.Repaint)
			{
				if (r.Contains(Event.current.mousePosition) == true)
					Utility.DrawUnfillRect(r, HQ.Settings != null ? HQ.Settings.Get<SpotlightSettings>().hoverSelectionColor : NGSpotlightWindow.HighlightedEntryColor);
				else if (window.selectedEntry == i)
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
					DragAndDrop.objectReferences = new Object[] { this.go };
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
					Selection.activeGameObject = this.go;
					window.Close();
				}
				else
					window.SelectEntry(i);

				Event.current.Use();
			}

			r.xMin += iconR.width;
			GUI.Label(r, this.cachedHighlightedName, PrefabDrawer.style);
		}

		void	IDrawableElement.Select(NGSpotlightWindow window, EntryRef key)
		{
			EditorGUIUtility.PingObject(this.go);
		}

		void	IDrawableElement.Execute(NGSpotlightWindow window, EntryRef key)
		{
			NGSpotlightWindow.UseEntry(key);
			EditorGUIUtility.PingObject(this.go);
			Selection.activeGameObject = this.go;
		}
	}
}