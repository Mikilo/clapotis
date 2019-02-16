using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NGToolsEditor.NGSpotlight
{
	public sealed class SceneDrawer : IDrawableElement
	{
		public const float	OpenWidth = 80F;
		public const float	DropdownWidth = 20F;

		private static GUIStyle		style;
		private static GUIStyle		buttonStyle;
		private static GUIStyle		menuStyle;

		public string	path;
		public string	name;
		public string	lowerName;

		private int		lastChange = -1;
		private string	cachedHighlightedName;

		string	IDrawableElement.RawContent { get { return this.path; } }
		string	IDrawableElement.LowerStringContent { get { return this.lowerName; } }

		public	SceneDrawer(string path)
		{
			this.path = path;
			this.name = Path.GetFileName(path);
			this.lowerName = name.ToLower();
		}

		void	IDrawableElement.OnGUI(Rect r, NGSpotlightWindow window, EntryRef k, int i)
		{
			if (SceneDrawer.style == null)
			{
				SceneDrawer.style = new GUIStyle(EditorStyles.label);
				SceneDrawer.style.alignment = TextAnchor.MiddleLeft;
				SceneDrawer.style.padding.left = 32;
				SceneDrawer.style.fontSize = 15;
				SceneDrawer.style.richText = true;
				SceneDrawer.buttonStyle = new GUIStyle("ButtonLeft");
				SceneDrawer.menuStyle = new GUIStyle("DropDownButton");
				SceneDrawer.menuStyle.fixedHeight = NGSpotlightWindow.RowHeight / 1.5F;
				SceneDrawer.menuStyle.padding.left = 0;
				SceneDrawer.menuStyle.margin.left = 0;
				SceneDrawer.menuStyle.border.left = 0;
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

			GUI.DrawTexture(iconR, UtilityResources.UnityIcon, ScaleMode.ScaleToFit);

			if (this.lastChange != window.changeCount)
			{
				this.lastChange = window.changeCount;
				this.cachedHighlightedName = window.HighlightWeightContent(this.lowerName, this.name, window.cleanLowerKeywords);
			}

			r.width -= SceneDrawer.OpenWidth + SceneDrawer.DropdownWidth;
			GUI.Label(r, this.cachedHighlightedName, SceneDrawer.style);

			if ((Event.current.type == EventType.KeyDown && window.selectedEntry == i && Event.current.keyCode == KeyCode.Return) ||
				(Event.current.type == EventType.MouseUp && r.Contains(Event.current.mousePosition) == true && i.Equals(DragAndDrop.GetGenericData("i")) == true))
			{
				if (Event.current.type == EventType.MouseUp && r.Contains(Event.current.mousePosition) == true)
					DragAndDrop.PrepareStartDrag();

				if (window.selectedEntry == i || Event.current.button != 0)
				{
					NGSpotlightWindow.UseEntry(k);

					if (Event.current.button == 0)
						this.LoadScene(window, this.path, 0);
					else
						Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(this.path);

					window.Close();
				}
				else
					window.SelectEntry(i);

				Event.current.Use();
			}

			r.height = Mathf.Floor(r.height * .66F);
			r.y += (iconR.height - r.height) * .5F;
			r.x += r.width;
			r.width = SceneDrawer.OpenWidth;
			if (GUI.Button(r, "Open", SceneDrawer.buttonStyle) == true)
				this.LoadScene(window, this.path, 0);

			r.x += r.width;
			r.width = SceneDrawer.DropdownWidth;
			if (GUI.Button(r, "", SceneDrawer.menuStyle) == true)
			{
				GenericMenu	menu = new GenericMenu();

				menu.AddItem(new GUIContent("Load single"), false, this.LoadScene, window);
				menu.AddItem(new GUIContent("Load additive"), false, this.LoadSceneAdditive, window);
				menu.AddItem(new GUIContent("Load additive without loading"), false, this.LoadSceneAdditiveWithoutLoading, window);
				if (AssetDatabase.LoadAssetAtPath(this.path, typeof(Object)) != null)
					menu.AddItem(new GUIContent("Ping"), false, this.PingScene);

				menu.DropDown(r);
			}
		}

		void	IDrawableElement.Select(NGSpotlightWindow window, EntryRef key)
		{
			EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(this.path));
		}

		void	IDrawableElement.Execute(NGSpotlightWindow window, EntryRef key)
		{
			NGSpotlightWindow.UseEntry(key);
			EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(this.path));
			this.LoadScene(window, this.path, 0);
		}

		private void	LoadScene(object w)
		{
			this.LoadScene(w as NGSpotlightWindow, this.path, 0);
		}

		private void	LoadSceneAdditive(object w)
		{
			this.LoadScene(w as NGSpotlightWindow, this.path, 1);
		}

		private void	LoadSceneAdditiveWithoutLoading(object w)
		{
			this.LoadScene(w as NGSpotlightWindow, this.path, 2);
		}

		private void	LoadScene(EditorWindow window, string path, int mode)
		{
			bool	exist = File.Exists(path);

			window.RemoveNotification();

			if (exist == true)
			{
				if (EditorApplication.isPlaying == true)
				{
					Scene	scene = SceneManager.GetSceneByPath(path);

					if (scene.IsValid() == true)
						SceneManager.LoadScene(scene.name, (LoadSceneMode)Mathf.Min(mode, 1));
					else
					{
						EditorBuildSettingsScene[]	buildScenes = EditorBuildSettings.scenes;

						for (int i = 0; i < buildScenes.Length; i++)
						{
							if (buildScenes[i].path == scene.path)
							{
								window.ShowNotification(new GUIContent("Scene \"" + scene.name + "\" is not one of the original scenes."));
								return;
							}
						}

						window.ShowNotification(new GUIContent("Scene \"" + scene.name + "\" must be\nin build settings\nor loaded from an AssetBundle."));
					}
				}
				else if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == true)
					EditorSceneManager.OpenScene(path, (OpenSceneMode)mode);
			}
			else
				window.ShowNotification(new GUIContent("Scene at \"" + path + "\" does not exist."));
		}

		private void	PingScene()
		{
			EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(this.path, typeof(Object)));
		}
	}
}