using System;
using System.IO;
using UnityEditor;

namespace NGToolsEditor.NGSpotlight
{
	using UnityEngine;

	public sealed class CSharpDrawer : IDrawableElement
	{
		public const float	Spacing = 2F;
		public const float	OpenWidth = 80F;

		private static GUIStyle		style;

		public string	path;
		public string	name;
		public string	lowerName;
		public Type		type;

		private int		lastChange = -1;
		private string	cachedHighlightedName;

		string	IDrawableElement.RawContent { get { return this.path; } }
		string	IDrawableElement.LowerStringContent { get { return this.lowerName; } }

		public	CSharpDrawer(string path)
		{
			this.path = path;
			this.name = Path.GetFileName(path);
			this.lowerName = name.ToLower();
		}

		void	IDrawableElement.OnGUI(Rect r, NGSpotlightWindow window, EntryRef k, int i)
		{
			if (CSharpDrawer.style == null)
			{
				CSharpDrawer.style = new GUIStyle(EditorStyles.label);
				CSharpDrawer.style.alignment = TextAnchor.MiddleLeft;
				CSharpDrawer.style.fontSize = 15;
				CSharpDrawer.style.richText = true;
			}

			if (this.type == null)
			{
				var mn = AssetDatabase.LoadAssetAtPath<MonoScript>(this.path);
				if (mn)
				{
					this.type = mn.GetClass();
				}
			}

			Rect	iconR = r;
			iconR.width = iconR.height;

			GUI.Box(r, "");

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
					DragAndDrop.objectReferences = new Object[] { AssetDatabase.LoadAssetAtPath<Object>(this.path) };
				}
			}
			else if (Event.current.type == EventType.DragExited)
				DragAndDrop.PrepareStartDrag();

			GUI.DrawTexture(iconR, UtilityResources.CSharpIcon, ScaleMode.ScaleToFit);

			if (this.lastChange != window.changeCount)
			{
				this.lastChange = window.changeCount;
				this.cachedHighlightedName = window.HighlightWeightContent(this.lowerName, this.name, window.cleanLowerKeywords);
			}

			float	h = Mathf.Floor(r.height * .66F);
			Rect	r3 = r;
			r3.height = h;
			r3.xMin += iconR.width;
			r3.xMax -= CSharpDrawer.OpenWidth;
			GUI.Label(r3, this.cachedHighlightedName, CSharpDrawer.style);

			if (this.type != null)
			{
				r3.y += r3.height - 2F;
				r3.height = iconR.height - h + 4F;
				GUI.Label(r3, this.type.FullName, GeneralStyles.SmallLabel);
				r3.y = iconR.y;
			}

			Rect	openR = r3;
			openR.height = iconR.height;
			openR.y += (openR.height - h) * .5F;
			openR.height = h;
			openR.x += openR.width;
			openR.width = CSharpDrawer.OpenWidth;
			if (GUI.Button(openR, "Open") == true)
				EditorUtility.OpenWithDefaultApp(this.path);

			if ((Event.current.type == EventType.KeyDown && window.selectedEntry == i && Event.current.keyCode == KeyCode.Return) ||
				(Event.current.type == EventType.MouseUp && r.Contains(Event.current.mousePosition) == true && openR.Contains(Event.current.mousePosition) == false && i.Equals(DragAndDrop.GetGenericData("i")) == true))
			{
				if (Event.current.type == EventType.MouseUp && r.Contains(Event.current.mousePosition) == true && openR.Contains(Event.current.mousePosition) == false)
					DragAndDrop.PrepareStartDrag();

				if (window.selectedEntry == i || Event.current.button != 0)
				{
					NGSpotlightWindow.UseEntry(k);

					Object	file = AssetDatabase.LoadAssetAtPath<Object>(this.path);

					if (Event.current.button == 0)
						AssetDatabase.OpenAsset(file, 0);
					else
						Selection.activeObject = file;

					window.Close();
				}
				else
					window.SelectEntry(i);

				Event.current.Use();
			}
		}

		void	IDrawableElement.Select(NGSpotlightWindow window, EntryRef key)
		{
			EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(this.path));
		}

		void	IDrawableElement.Execute(NGSpotlightWindow window, EntryRef key)
		{
			NGSpotlightWindow.UseEntry(key);
			Object	file = AssetDatabase.LoadAssetAtPath<Object>(this.path);
			EditorGUIUtility.PingObject(file);
			AssetDatabase.OpenAsset(file, 0);
		}
	}
}