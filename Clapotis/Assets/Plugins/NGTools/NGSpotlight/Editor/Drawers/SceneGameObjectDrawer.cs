using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGSpotlight
{
	public sealed class SceneGameObjectDrawer : IDrawableElement, IHasGameObject
	{
		public const float	Spacing = 2F;

		private static GUIStyle	style;

		public string		name;
		public string		lowerName;
		public GameObject	go;

		private int		lastChange = -1;
		private string	cachedHierarchy;
		private string	cachedHighlightedName;

		string		IDrawableElement.RawContent { get { return this.go != null ? Utility.GetHierarchyStringified(this.go.transform) : this.name; } }
		string		IDrawableElement.LowerStringContent { get { return this.lowerName; } }
		GameObject	IHasGameObject.GameObject { get { return this.go; } }

		public	SceneGameObjectDrawer(GameObject go)
		{
			this.go = go;
			this.name = go.name;
			this.lowerName = this.name.ToLower();
		}

		void	IDrawableElement.OnGUI(Rect r, NGSpotlightWindow window, EntryRef k, int i)
		{
			if (this.go == null)
			{
				NGSpotlightWindow.DeleteEntry(k.key, k.i);
				return;
			}

			if (SceneGameObjectDrawer.style == null)
			{
				SceneGameObjectDrawer.style = new GUIStyle(EditorStyles.label);
				SceneGameObjectDrawer.style.alignment = TextAnchor.MiddleLeft;
				SceneGameObjectDrawer.style.fontSize = 15;
				SceneGameObjectDrawer.style.richText = true;
			}

			Rect	iconR = r;
			iconR.width = iconR.height;

			GUI.Box(r, "");
			GUI.DrawTexture(iconR, UtilityResources.GameObjectIcon, ScaleMode.ScaleToFit);

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

			float	h = Mathf.Floor(r.height * .66F);
			r.height = h;
			r.xMin += iconR.width;
			//NGEditorGUILayout.ElasticLabel(r, this.cachedHighlightedName, '/', GameObjectDrawer.style);
			GUI.Label(r, this.cachedHighlightedName, SceneGameObjectDrawer.style);

			r.y += r.height - SceneGameObjectDrawer.Spacing;
			r.height = iconR.height - h;
			r.x += SceneGameObjectDrawer.Spacing + r.height + SceneGameObjectDrawer.Spacing;
			GUI.Label(r, this.go.scene.name, GeneralStyles.SmallLabel);

			r.x -= r.height + SceneGameObjectDrawer.Spacing;
			r.width = r.height;
			GUI.DrawTexture(r, UtilityResources.UnityIcon);
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