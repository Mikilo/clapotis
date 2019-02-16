using NGLicenses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace NGToolsEditor.NGScenes
{
	using UnityEngine;

	public class NGScenesWindow : EditorWindow, IHasCustomMenu
	{
		private class Scene
		{
			public string	path;
			public string	name;
			public Object	asset;

			public	Scene(string path)
			{
				this.path = path;
				this.name = Path.GetFileNameWithoutExtension(path);
				this.asset = AssetDatabase.LoadAssetAtPath(this.path, typeof(Object));
			}
		}

		private sealed class OptionPopup : PopupWindowContent
		{
			public static Color	HighlightColor = Color.yellow;

			private readonly NGScenesWindow	window;
			private ColorContentAnimator	anim;
			private int						animOn = -1;

			public	OptionPopup(NGScenesWindow window)
			{
				this.window = window;
			}

			public override void	OnOpen()
			{
				this.anim = new ColorContentAnimator(new UnityAction(this.editorWindow.Repaint), 0F, 1F);
			}

			public override Vector2	GetWindowSize()
			{
				return new Vector2(Mathf.Max(this.window.position.width * .5F, 200F), 4F + (HQ.Settings.Get<ScenesSettings>().profiles.Count + 1) * 19F);
			}

			public override void	OnGUI(Rect r)
			{
				ScenesSettings	settings = HQ.Settings.Get<ScenesSettings>();

				for (int i = 0; i < settings.profiles.Count; i++)
				{
					GUILayout.BeginHorizontal();
					{
						using (BgColorContentRestorer.Get(i == this.animOn && this.anim.af.isAnimating, Color.Lerp(GUI.contentColor, OptionPopup.HighlightColor, this.anim.Value)))
						{
							if (GUILayout.Button("Save In", GeneralStyles.ToolbarButton, GUILayoutOptionPool.ExpandWidthFalse) == true)
							{
								this.animOn = i;
								this.anim.Start();

								settings.profiles[i].Save();
								HQ.InvalidateSettings();
								this.window.Repaint();
							}

							EditorGUI.BeginChangeCheck();
							string	name = EditorGUILayout.TextField(settings.profiles[i].name);
							if (EditorGUI.EndChangeCheck() == true)
							{
								settings.profiles[i].name = name;
								HQ.InvalidateSettings();
							}
						}

						if (GUILayout.Button("X", GeneralStyles.ToolbarCloseButton, GUILayoutOptionPool.ExpandWidthFalse) == true)
						{
							settings.profiles.RemoveAt(i);
							HQ.InvalidateSettings();
							return;
						}
					}
					GUILayout.EndHorizontal();
				}

				if (GUILayout.Button("Add") == true &&
					this.window.CheckMaxBuildSceneProfiles(settings.profiles.Count) == true)
				{
					Profile	p = new Profile() { name = "New" };

					p.Save();

					settings.profiles.Add(p);

					r.height = (settings.profiles.Count + 1) * 19F;
					this.editorWindow.position = r;
				}
			}
		}

		public const string	Title = "NG Scenes";
		public static Color	TitleColor = new Color(70F / 255F, 130F / 255F, 180F / 255F, 1F); // Steelblue
		public const string	RecentScenesKey = "NGScenes_RecentScenes";
		public const char	SceneSeparator = ',';

		private const int				MaxBuildScenesProfiles = 1;
		private static readonly string	FreeAdContent = NGScenesWindow.Title + " is restrained to " + NGScenesWindow.MaxBuildScenesProfiles + " build profiles.";

		private GUIListDrawer<Scene>	recentListDrawer;
		private GUIListDrawer<Scene>	allListDrawer;
		private GUIListDrawer<EditorBuildSettingsScene>	buildListDrawer;
		
		private int		enabledScenesCounter = 0;
		private double	lastClick;

		private static List<Scene>	list = new List<Scene>();
		private static Scene[]		allScenes;

		private Dictionary<string, string>	shrinkedPaths = new Dictionary<string, string>(8);

		static	NGScenesWindow()
		{
			NGEditorApplication.ChangeScene += NGScenesWindow.UpdateLastScenes;

			// Force update allScenes at next restart.
			// TODO Unity <5.6 backward compatibility?
			MethodInfo	ResetAllScenesMethod = typeof(NGScenesWindow).GetMethod("ResetAllScenes", BindingFlags.Static | BindingFlags.NonPublic);

			try
			{
				EventInfo	projectChangedEvent = typeof(EditorApplication).GetEvent("projectChanged");
				projectChangedEvent.AddEventHandler(null, Delegate.CreateDelegate(projectChangedEvent.EventHandlerType, null, ResetAllScenesMethod));
				//EditorApplication.projectChanged += NGScenesWindow.ResetAllScenes;
			}
			catch
			{
				FieldInfo	projectWindowChangedField = UnityAssemblyVerifier.TryGetField(typeof(EditorApplication), "projectWindowChanged", BindingFlags.Static | BindingFlags.Public);
				if (projectWindowChangedField != null)
					projectWindowChangedField.SetValue(null, Delegate.Combine((Delegate)projectWindowChangedField.GetValue(null), Delegate.CreateDelegate(projectWindowChangedField.FieldType, null, ResetAllScenesMethod)));
				//EditorApplication.projectWindowChanged += NGScenesWindow.ResetAllScenes;
			}
		}

		[MenuItem("File/" + NGScenesWindow.Title, priority = 0)]
		[MenuItem(Constants.MenuItemPath + NGScenesWindow.Title, priority = Constants.MenuItemPriority + 317)]
		[Hotkey(NGScenesWindow.Title)]
		public static void	Open()
		{
			Utility.OpenWindow<NGScenesWindow>(true, NGScenesWindow.Title, true);
		}

		private static void	UpdateLastScenes()
		{
			string	lastScene = EditorSceneManager.GetActiveScene().path;
			if (string.IsNullOrEmpty(lastScene) == true)
				return;

			string	rawScenes = NGEditorPrefs.GetString(NGScenesWindow.RecentScenesKey, string.Empty, true);

			if (string.IsNullOrEmpty(rawScenes) == false)
			{
				string[]		scenes = rawScenes.Split(NGScenesWindow.SceneSeparator);
				List<string>	list = new List<string>(scenes.Length + 1);

				list.Add(lastScene);
				for (int i = 0; i < scenes.Length; i++)
				{
					if (scenes[i] != lastScene && File.Exists(scenes[i]) == true)
						list.Add(scenes[i]);
				}

				NGEditorPrefs.SetString(NGScenesWindow.RecentScenesKey, string.Join(NGScenesWindow.SceneSeparator.ToString(), list.ToArray()), true);
			}
			else
				NGEditorPrefs.SetString(NGScenesWindow.RecentScenesKey, EditorSceneManager.GetActiveScene().name, true);
		}

		[NGSettingsChanged]
		private static void	OnSettingsGenerated(ScriptableObject settings)
		{
			CustomHotkeysSettings	hotkeys = settings as CustomHotkeysSettings;

			if (hotkeys != null)
				hotkeys.hotkeys.Add(new CustomHotkeysSettings.MethodHotkey() { bind = "%G", staticMethod = typeof(NGScenesWindow).FullName + ".Open" });
		}

		private static void	ResetAllScenes()
		{
			NGScenesWindow.allScenes = null;
		}

		protected virtual void	OnEnable()
		{
			Utility.RestoreIcon(this, NGScenesWindow.TitleColor);

			Metrics.UseTool(10); // NGScenes

			NGChangeLogWindow.CheckLatestVersion(NGAssemblyInfo.Name);

			if (NGScenesWindow.allScenes == null)
			{
				string[]	allScenes = AssetDatabase.GetAllAssetPaths();

				list.Clear();
				for (int i = 0; i < allScenes.Length; i++)
				{
					if (allScenes[i].EndsWith(".unity", StringComparison.OrdinalIgnoreCase) == true)
						list.Add(new Scene(allScenes[i]));
				}

				list.Sort((a, b) => a.name.CompareTo(b.name));
				NGScenesWindow.allScenes = list.ToArray();
			}

			this.allListDrawer = new GUIListDrawer<Scene>();
			this.allListDrawer.array = NGScenesWindow.allScenes;
			this.allListDrawer.ElementGUI = this.DrawSceneRow;

			this.recentListDrawer = new GUIListDrawer<Scene>();
			this.recentListDrawer.ElementGUI = this.DrawSceneRow;

			this.UpdateRecentScenes();

			this.buildListDrawer = new GUIListDrawer<EditorBuildSettingsScene>();
			this.buildListDrawer.drawBackgroundColor = true;
			this.buildListDrawer.handleSelection = true;
			this.buildListDrawer.handleDrag = true;
			this.buildListDrawer.ElementGUI = this.DrawBuildSceneRow;
			this.buildListDrawer.PostGUI = this.DropScene;
			this.buildListDrawer.DeleteSelection = this.DeleteBuildScenes;
			this.buildListDrawer.ArrayReordered = (l) => EditorBuildSettings.scenes = l.array;

			NGEditorApplication.ChangeScene += this.UpdateRecentScenes;

			this.wantsMouseMove = true;
		}

		protected virtual void	OnDestroy()
		{
			NGEditorApplication.ChangeScene -= this.UpdateRecentScenes;
		}

		protected virtual void	OnGUI()
		{
			if (this.maxSize == this.minSize)
			{
				if (GUI.Button(new Rect(this.position.width - 20F, 0F, 20F, 20F), "X") == true)
					this.Close();
			}

			FreeLicenseOverlay.First(this, NGAssemblyInfo.Name + " Pro", NGScenesWindow.FreeAdContent);

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(5F);

				GUILayout.BeginVertical(GUILayoutOptionPool.MinWidth(200F));
				{
					GUILayout.Label("Recent Scenes:", GeneralStyles.Title1);
					this.recentListDrawer.OnGUI(GUILayoutUtility.GetRect(0F, 100F));

					GUILayout.Label("All Scenes:", GeneralStyles.Title1);
					this.allListDrawer.OnGUI(GUILayoutUtility.GetRect(0F, 0F, GUILayoutOptionPool.ExpandHeightTrue));
				}
				GUILayout.EndVertical();

				GUILayout.Space(2F);

				GUILayout.BeginVertical();
				{
					GUILayout.BeginHorizontal();
					{
						GUILayout.Label("Build Scenes:", GeneralStyles.Title1);

						if (HQ.Settings != null)
						{
							Utility.content.text = "Load profile";
							Rect	r = GUILayoutUtility.GetRect(Utility.content, "ToolbarDropDown");
							if (GUI.Button(r, Utility.content, "ToolbarDropDown") == true)
							{
								GenericMenu		menu = new GenericMenu();
								ScenesSettings	settings = HQ.Settings.Get<ScenesSettings>();

								if (settings.profiles.Count > 0)
								{
									for (int i = 0; i < settings.profiles.Count; i++)
										menu.AddItem(new GUIContent((i + 1) + " - " + settings.profiles[i].name + " (" + settings.profiles[i].scenes.Count + ")"), false, this.LoadProfile, i);
								}
								else
									menu.AddItem(new GUIContent("No profile available."), false, null);

								menu.DropDown(r);
							}

							Utility.content.text = "☰";
							r = GUILayoutUtility.GetRect(Utility.content, "GV Gizmo DropDown", GUILayoutOptionPool.ExpandWidthFalse);
							if (GUI.Button(r, Utility.content, "GV Gizmo DropDown") == true)
								PopupWindow.Show(r, new OptionPopup(this));

							if (this.maxSize == this.minSize)
								GUILayout.Space(20F);
						}
					}
					GUILayout.EndHorizontal();

					this.enabledScenesCounter = 0;
					this.buildListDrawer.array = EditorBuildSettings.scenes;
					this.buildListDrawer.OnGUI(GUILayoutUtility.GetRect(0F, 0F, GUILayoutOptionPool.ExpandHeightTrue));

					GUILayout.Space(5F);
				}
				GUILayout.EndVertical();

				GUILayout.Space(5F);
			}
			GUILayout.EndHorizontal();

			if (Event.current.type == EventType.MouseDown)
			{
				if (this.lastClick + Constants.DoubleClickTime > EditorApplication.timeSinceStartup)
					this.Close();
				this.lastClick = EditorApplication.timeSinceStartup;
				Event.current.Use();
			}

			FreeLicenseOverlay.Last(NGAssemblyInfo.Name + " Pro");
		}

		private void	LoadProfile(object data)
		{
			int	i = (int)data;

			HQ.Settings.Get<ScenesSettings>().profiles[i].Load();
			this.Repaint();
		}

		private void	DrawSceneRow(Rect r, Scene scene, int i)
		{
			if (r.Contains(Event.current.mousePosition) == false)
				GUI.Label(r, scene.name, GeneralStyles.ToolbarButtonLeft);
			else
			{
				if (Event.current.type == EventType.MouseDrag &&
					Utility.position2D != Vector2.zero &&
					(Utility.position2D - Event.current.mousePosition).sqrMagnitude >= Constants.MinStartDragDistance)
				{
					DragAndDrop.StartDrag("Drag Object");
					// Dragging from a Button does some hidden stuff, messing up the Drag&Drop.
					EditorGUIUtility.hotControl = 0;
					Event.current.Use();
				}
				else if (Event.current.type == EventType.MouseDown)
				{
					Utility.position2D = Event.current.mousePosition;
					DragAndDrop.PrepareStartDrag();
					DragAndDrop.paths = new string[] { scene.path };
					DragAndDrop.objectReferences = new Object[] { scene.asset };
				}

				r.width -= 60F;
				if (GUI.Button(r, scene.name, GeneralStyles.ToolbarButtonLeft) == true && File.Exists(scene.path) == true)
				{
					OpenSceneMode	mode = OpenSceneMode.Single;

					if (Event.current.control == true)
						mode = OpenSceneMode.Additive;
					else if (Event.current.alt == true)
						mode = OpenSceneMode.AdditiveWithoutLoading;

					if (EditorApplication.isPlaying == true)
						SceneManager.LoadScene(scene.name, (LoadSceneMode)mode);
					else if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == true)
						EditorSceneManager.OpenScene(scene.path, mode);
				}

				r.x += r.width;
				r.width = 60F;
				if (GUI.Button(r, "+", EditorStyles.toolbarDropDown) == true)
				{
					GenericMenu	menu = new GenericMenu();

					menu.AddItem(new GUIContent("Load single"), false, this.LoadScene, scene);
					menu.AddItem(new GUIContent("Load additive"), false, this.LoadSceneAdditive, scene);

					if (EditorApplication.isPlaying == false)
						menu.AddItem(new GUIContent("Load additive without loading"), false, this.LoadSceneAdditiveWithoutLoading, scene);

					menu.AddItem(new GUIContent("Ping"), false, this.PingScene, scene);

					menu.DropDown(r);
				}

				if (Event.current.type == EventType.MouseMove)
					this.Repaint();
			}
		}

		private void	DrawBuildSceneRow(Rect r, EditorBuildSettingsScene scene, int i)
		{
			float	w = r.width - 4F;

			if (Event.current.type == EventType.Repaint && r.Contains(Event.current.mousePosition) == true)
			{
				if (DragAndDrop.visualMode == DragAndDropVisualMode.Move)
				{
					float	h = r.height;
					r.height = 1F;
					EditorGUI.DrawRect(r, Color.green);
					r.height = h;
				}
			}
			else if (Event.current.type == EventType.DragUpdated && r.Contains(Event.current.mousePosition) == true)
			{
				bool	one = false;

				for (int j = 0; j < DragAndDrop.paths.Length; j++)
				{
					if (DragAndDrop.paths[j].EndsWith(".unity", StringComparison.OrdinalIgnoreCase) == true)
					{
						one = true;
						break;
					}
				}

				if (one == true)
					DragAndDrop.visualMode = DragAndDropVisualMode.Move;
				else
					DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
			}
			else if (Event.current.type == EventType.DragPerform && r.Contains(Event.current.mousePosition) == true)
			{
				if (DragAndDrop.paths.Length > 0)
				{
					DragAndDrop.AcceptDrag();

					List<EditorBuildSettingsScene>	scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

					for (int j = 0; j < DragAndDrop.paths.Length; j++)
					{
						if (DragAndDrop.paths[j].EndsWith(".unity", StringComparison.OrdinalIgnoreCase) == true)
							scenes.Insert(i++, new EditorBuildSettingsScene(DragAndDrop.paths[j], true));
					}

					EditorBuildSettings.scenes = scenes.ToArray();

					Event.current.Use();
				}
			}

			EditorGUI.BeginDisabledGroup(!File.Exists(scene.path));
			{
				r.x += 4F;
				r.width = 20F;
				EditorGUI.BeginChangeCheck();
				bool	enabled = GUI.Toggle(r, scene.enabled, string.Empty);
				if (EditorGUI.EndChangeCheck() == true)
				{
					EditorBuildSettingsScene[]	scenes = EditorBuildSettings.scenes.Clone() as EditorBuildSettingsScene[];

					scenes[i] = new EditorBuildSettingsScene(scene.path, enabled);
					EditorBuildSettings.scenes = scenes;
				}

				string	path;
				
				if (this.shrinkedPaths.TryGetValue(scene.path, out path) == false)
				{
					int	start = 0;
					int	length = scene.path.Length;

					if (scene.path.StartsWith("Assets/") == true)
					{
						start = "Assets/".Length;
						length -= start;
					}

					if (scene.path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) == true)
						length -= ".unity".Length;

					path = scene.path.Substring(start, length);
					this.shrinkedPaths.Add(scene.path, path);
				}

				r.x += r.width;
				if (scene.enabled == true)
				{
					Utility.content.text = this.enabledScenesCounter.ToCachedString();
					float	indexWidth = GUI.skin.label.CalcSize(Utility.content).x;
					r.width = w - r.x - indexWidth;
					NGEditorGUILayout.ElasticLabel(r, path, '/');

					r.x += r.width;
					r.width = indexWidth;
					GUI.Label(r, this.enabledScenesCounter.ToCachedString());
					++this.enabledScenesCounter;
				}
				else
				{
					r.width = w - r.x;
					GUI.Label(r, path);
				}
			}
			EditorGUI.EndDisabledGroup();
		}

		private void	DropScene(GUIListDrawer<EditorBuildSettingsScene> list)
		{
			if (Event.current.type == EventType.DragUpdated)
			{
				bool	one = false;

				for (int i = 0; i < DragAndDrop.paths.Length; i++)
				{
					if (DragAndDrop.paths[i].EndsWith(".unity", StringComparison.OrdinalIgnoreCase) == true)
					{
						one = true;
						break;
					}
				}

				if (one == true)
					DragAndDrop.visualMode = DragAndDropVisualMode.Move;
				else
					DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;

				Event.current.Use();
			}
			else if (Event.current.type == EventType.DragPerform)
			{
				DragAndDrop.AcceptDrag();

				List<EditorBuildSettingsScene>	scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

				for (int i = 0; i < DragAndDrop.paths.Length; i++)
				{
					if (DragAndDrop.paths[i].EndsWith(".unity", StringComparison.OrdinalIgnoreCase) == true)
						scenes.Add(new EditorBuildSettingsScene(DragAndDrop.paths[i], true));
				}

				EditorBuildSettings.scenes = scenes.ToArray();

				Event.current.Use();
			}
		}

		private void	DeleteBuildScenes(GUIListDrawer<EditorBuildSettingsScene> list)
		{
			List<EditorBuildSettingsScene>	scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

			for (int i = 0; i < list.selection.Count; i++)
				scenes[list.selection[i]] = null;

			for (int i = 0; i < scenes.Count; i++)
			{
				if (scenes[i] == null)
				{
					scenes.RemoveAt(i);
					--i;
				}
			}

			EditorBuildSettings.scenes = scenes.ToArray();
		}

		private void	LoadScene(object data)
		{
			Scene	scene = data as Scene;
			bool	exist = File.Exists(scene.path);

			if (exist == true)
			{
				if (EditorApplication.isPlaying == true)
					SceneManager.LoadScene(scene.name, LoadSceneMode.Single);
				else if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == true)
					EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
			}
		}

		private void	LoadSceneAdditive(object data)
		{
			Scene	scene = data as Scene;
			bool	exist = File.Exists(scene.path);

			if (exist == true)
			{
				if (EditorApplication.isPlaying == true)
					SceneManager.LoadScene(scene.name, LoadSceneMode.Additive);
				else
					EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive);
			}
		}

		private void	LoadSceneAdditiveWithoutLoading(object data)
		{
			Scene	scene = data as Scene;
			bool	exist = File.Exists(scene.path);

			if (exist == true)
				EditorSceneManager.OpenScene(scene.path, OpenSceneMode.AdditiveWithoutLoading);
		}

		private void	PingScene(object data)
		{
			EditorGUIUtility.PingObject((data as Scene).asset);
		}

		private void	UpdateRecentScenes()
		{
			string	rawScenes = NGEditorPrefs.GetString(NGScenesWindow.RecentScenesKey, string.Empty, true);
			if (string.IsNullOrEmpty(rawScenes) == true)
			{
				this.recentListDrawer.array = new Scene[0];
				return;
			}

			string[]	scenes = rawScenes.Split(NGScenesWindow.SceneSeparator);

			list.Clear();
			for (int i = 0; i < scenes.Length; i++)
				list.Add(new Scene(scenes[i]));

			this.recentListDrawer.array = list.ToArray();
		}

		private bool	CheckMaxBuildSceneProfiles(int count)
		{
			return NGLicensesManager.Check(count < NGScenesWindow.MaxBuildScenesProfiles, NGAssemblyInfo.Name + " Pro", "Free version does not allow more than " + NGScenesWindow.MaxBuildScenesProfiles + " profiles.\n\n");
		}

		void	IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			Utility.AddNGMenuItems(menu, this, NGScenesWindow.Title, NGAssemblyInfo.WikiURL, true);
		}
	}
}