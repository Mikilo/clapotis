using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;

namespace NGToolsEditor.NGNavSelection
{
	using UnityEngine;

	[PrewarmEditorWindow]
	[InitializeOnLoad]
	public class NGNavSelectionWindow : EditorWindow, IHasCustomMenu
	{
		public const string	Title = "NG Nav Selection";
		public static Color	TitleColor = Color.cyan;
		public const string	LastHashPrefKey = "NGNavSelection_lastHash";
		public const string	AutoSavePrefKey = "NGNavSelection_historic";
		public const int	MaxHistoric = 1000;
		public const float	HighlightCursorWidth = 5F;
		public static Color	HighlightCursorBackgroundColor = new Color(.3F, .3F, .3F);
		public static Color	HighlightFocusedHistoricBackgroundColor = new Color(.7F, .7F, .1F);

		// Does not handle null entries in historic.
		public static bool	CanSelectNext { get { return NGNavSelectionWindow.historicCursor != -1 && NGNavSelectionWindow.historic.Count > 0; } }
		public static bool	CanSelectPrevious { get { return (NGNavSelectionWindow.historicCursor == -1 || NGNavSelectionWindow.historicCursor > 0) && NGNavSelectionWindow.historic.Count > 0; } }

		public static event Action	SelectionChanged;

		private static bool						hasChanged = false;
		internal static List<AssetsSelection>	historic = new List<AssetsSelection>();
		private static int						historicCursor = -1;
		private static int						lastHash = -1;
		internal static int						lastFocusedHistoric = -1;
		private static bool						isPlaging = false;
		private static bool						savedOnCompile = false;
		private static bool						buttonWasDown = false;

		private GUIListDrawer<AssetsSelection>	listDrawer;
		private Vector2		dragOriginPosition;
		private double		lastClick;

		private bool	isLocked;
		private Vector2	initialMin;
		private Vector2	initialMax;

		private ErrorPopup	errorPopup = new ErrorPopup(NGNavSelectionWindow.Title, "An error occurred, try to reopen " + NGNavSelectionWindow.Title + ".");

		static	NGNavSelectionWindow()
		{
			try
			{
				if (Application.platform == RuntimePlatform.WindowsEditor)
					EditorApplication.update += NGNavSelectionWindow.HandleMouseInputs;
				NGEditorApplication.EditorExit += NGNavSelectionWindow.SaveHistoric;
				HQ.SettingsChanged += NGNavSelectionWindow.OnSettingsChanged;

				// It must me delayed! Most Object are correctly fetched, except folders and maybe others.
				EditorApplication.delayCall += () =>
				{
					// HACK Prevents double call bug.
					if (NGNavSelectionWindow.historic.Count != 0)
						return;

					NGNavSelectionWindow.lastHash = NGEditorPrefs.GetInt(NGNavSelectionWindow.LastHashPrefKey, 0);
					NGNavSelectionWindow.isPlaging = EditorApplication.isPlaying;

					string	autoSave = NGEditorPrefs.GetString(NGNavSelectionWindow.AutoSavePrefKey, string.Empty, true);

					if (autoSave != string.Empty)
					{
						string[]	selections = autoSave.Split(',');
						int			lastHash = 0;

						for (int i = 0; i < selections.Length; i++)
						{
							string[]	IDs = selections[i].Split(';');
							int[]		array = new int[IDs.Length];

							for (int j = 0; j < IDs.Length; j++)
								array[j] = int.Parse(IDs[j]);

							AssetsSelection	selection = new AssetsSelection(array);

							if (selection.refs.Count > 0)
							{
								int	hash = selection.GetSelectionHash();

								if (lastHash != hash)
								{
									lastHash = hash;
									NGNavSelectionWindow.historic.Add(selection);
								}
							}
						}
					}
				};
			}
			catch
			{
			}
		}

		[MenuItem(Constants.MenuItemPath + NGNavSelectionWindow.Title, priority = Constants.MenuItemPriority + 315), Hotkey(NGNavSelectionWindow.Title)]
		public static void	Open()
		{
			Utility.OpenWindow<NGNavSelectionWindow>(NGNavSelectionWindow.Title);
		}

		private static void	OnSettingsChanged()
		{
			Selection.selectionChanged -= NGNavSelectionWindow.UpdateSelection;
#if !UNITY_2017_2_OR_NEWER
			EditorApplication.playmodeStateChanged -= NGNavSelectionWindow.OnPlayStateChanged;
#else
			EditorApplication.playModeStateChanged -= NGNavSelectionWindow.OnPlayStateChanged;
#endif

			if (HQ.Settings != null)
			{
				if (HQ.Settings.Get<NavSettings>().enable == true)
				{
					Selection.selectionChanged += NGNavSelectionWindow.UpdateSelection;
#if !UNITY_2017_2_OR_NEWER
					EditorApplication.playmodeStateChanged += NGNavSelectionWindow.OnPlayStateChanged;
#else
					EditorApplication.playModeStateChanged += NGNavSelectionWindow.OnPlayStateChanged;
#endif
				}
			}
		}

		[NGSettings(NGNavSelectionWindow.Title)]
		private static void	OnGUISettings()
		{
			if (HQ.Settings == null)
				return;

			NavSettings	settings = HQ.Settings.Get<NavSettings>();

			if (Application.platform != RuntimePlatform.WindowsEditor)
				EditorGUILayout.LabelField(LC.G("NGNavSelection_OnlyAvailableOnWindows"), GeneralStyles.WrapLabel);

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.Space();
			using (BgColorContentRestorer.Get(settings.enable == true ? Color.green : Color.red))
			{
				EditorGUILayout.BeginVertical("ButtonLeft");
				{
					EditorGUILayout.BeginHorizontal();
					{
						settings.enable = NGEditorGUILayout.Switch(LC.G("Enable"), settings.enable);
					}
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.LabelField(LC.G("NGNavSelection_EnableDescription"), GeneralStyles.WrapLabel);
				}
				EditorGUILayout.EndVertical();
			}

			if (EditorGUI.EndChangeCheck() == true)
			{
				if (settings.enable == false)
				{
					Selection.selectionChanged -= NGNavSelectionWindow.UpdateSelection;
#if !UNITY_2017_2_OR_NEWER
					EditorApplication.playmodeStateChanged -= NGNavSelectionWindow.OnPlayStateChanged;
#else
					EditorApplication.playModeStateChanged -= NGNavSelectionWindow.OnPlayStateChanged;
#endif
				}
				else
				{
					Selection.selectionChanged += NGNavSelectionWindow.UpdateSelection;
#if !UNITY_2017_2_OR_NEWER
					EditorApplication.playmodeStateChanged += NGNavSelectionWindow.OnPlayStateChanged;
#else
					EditorApplication.playModeStateChanged += NGNavSelectionWindow.OnPlayStateChanged;
#endif
				}
				HQ.InvalidateSettings();
			}

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField(LC.G("NGNavSelection_MaxHistoricDescription"), GeneralStyles.WrapLabel);
			settings.maxHistoric = EditorGUILayout.IntField(LC.G("NGNavSelection_MaxHistoric"), settings.maxHistoric);
			if (EditorGUI.EndChangeCheck() == true)
			{
				settings.maxHistoric = Mathf.Clamp(settings.maxHistoric, 1, NGNavSelectionWindow.MaxHistoric);
				HQ.InvalidateSettings();
			}

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField(LC.G("NGNavSelection_MaxDisplayHierarchyDescription"), GeneralStyles.WrapLabel);
			settings.maxDisplayHierarchy = EditorGUILayout.IntField(LC.G("NGNavSelection_MaxDisplayHierarchy"), settings.maxDisplayHierarchy);
			if (EditorGUI.EndChangeCheck() == true)
			{
				if (settings.maxDisplayHierarchy < -1)
					settings.maxDisplayHierarchy = -1;
				HQ.InvalidateSettings();
			}
		}

		private static void	OnPlayStateChanged(
#if UNITY_2017_2_OR_NEWER
			PlayModeStateChange state
#endif
			)
		{
			if (NGNavSelectionWindow.isPlaging == true && EditorApplication.isPlayingOrWillChangePlaymode == false)
			{
				NGNavSelectionWindow.isPlaging = false;
				NGNavSelectionWindow.SaveHistoric();
			}
			else if (EditorApplication.isCompiling == true && NGNavSelectionWindow.savedOnCompile == false)
			{
				NGNavSelectionWindow.savedOnCompile = true;
				NGNavSelectionWindow.SaveHistoric();
			}
		}

		private static void	UpdateSelection()
		{
			int	hash = NGNavSelectionWindow.GetCurrentSelectionHash();

			if (NGNavSelectionWindow.lastHash == hash || hash == 0)
				return;

			if (Selection.objects.Length > 0)
			{
				// Prevent adding a new selection if the user just selected it through NG Nav Selection.
				if (0 <= NGNavSelectionWindow.lastFocusedHistoric && NGNavSelectionWindow.lastFocusedHistoric < NGNavSelectionWindow.historic.Count && NGNavSelectionWindow.historic[NGNavSelectionWindow.lastFocusedHistoric].GetSelectionHash() == hash)
					return;

				NGNavSelectionWindow.hasChanged = true;

				// Add a new selection or update the last one.
				if (NGNavSelectionWindow.historicCursor != -1)
				{
					NGNavSelectionWindow.historic.RemoveRange(NGNavSelectionWindow.historicCursor + 1, NGNavSelectionWindow.historic.Count - NGNavSelectionWindow.historicCursor - 1);
					NGNavSelectionWindow.historicCursor = -1;
				}

				// Detect a change in the selection only if user selects ONE Object.
				if (Selection.objects.Length == 1)
					NGNavSelectionWindow.historic.Add(new AssetsSelection(Selection.objects, Selection.instanceIDs));
				else if (NGNavSelectionWindow.historic.Count >= 1)
					NGNavSelectionWindow.historic[NGNavSelectionWindow.historic.Count - 1] = new AssetsSelection(Selection.objects, Selection.instanceIDs);

				NavSettings	settings = HQ.Settings.Get<NavSettings>();

				if (settings.maxHistoric > 0 && NGNavSelectionWindow.historic.Count > settings.maxHistoric)
					NGNavSelectionWindow.historic.RemoveRange(0, NGNavSelectionWindow.historic.Count - settings.maxHistoric);
			}
			else
				NGNavSelectionWindow.historicCursor = -1;

			NGNavSelectionWindow.lastFocusedHistoric = -1;

			NGNavSelectionWindow.lastHash = hash;
			if (NGNavSelectionWindow.SelectionChanged != null)
				NGNavSelectionWindow.SelectionChanged();
		}

		private static void	HandleMouseInputs()
		{
			if (EditorApplication.isCompiling == true)
			{
				if (NGNavSelectionWindow.savedOnCompile == false)
				{
					NGNavSelectionWindow.savedOnCompile = true;
					NGNavSelectionWindow.SaveHistoric();
				}

				return;
			}

			IntPtr	activeWindow = NativeMethods.GetActiveWindow();

			if (activeWindow != IntPtr.Zero)
			{
				// Go to previous selection.
				if (NativeMethods.GetAsyncKeyState(5) != 0)
				{
					if (NGNavSelectionWindow.buttonWasDown == false)
					{
						NGNavSelectionWindow.buttonWasDown = true;
						NGNavSelectionWindow.SelectPreviousSelection();
					}
				}
				// Go to next selection.
				else if (NativeMethods.GetAsyncKeyState(6) != 0)
				{
					if (NGNavSelectionWindow.buttonWasDown == false)
					{
						NGNavSelectionWindow.buttonWasDown = true;
						NGNavSelectionWindow.SelectNextSelection();
					}
				}
				else
					NGNavSelectionWindow.buttonWasDown = false;
			}
		}

		public static void	SelectPreviousSelection()
		{
			if (NGNavSelectionWindow.historicCursor == 0)
				return;

			do
			{
				if (NGNavSelectionWindow.historicCursor == -1 && NGNavSelectionWindow.historic.Count >= 2)
				{
					if (Selection.activeInstanceID == 0)
						NGNavSelectionWindow.historicCursor = NGNavSelectionWindow.historic.Count - 1;
					else
						NGNavSelectionWindow.historicCursor = NGNavSelectionWindow.historic.Count - 2;
					NGNavSelectionWindow.lastHash = NGNavSelectionWindow.historic[NGNavSelectionWindow.historicCursor].GetSelectionHash();
				}
				else if (NGNavSelectionWindow.historicCursor > 0)
				{
					--NGNavSelectionWindow.historicCursor;
					NGNavSelectionWindow.lastHash = NGNavSelectionWindow.historic[NGNavSelectionWindow.historicCursor].GetSelectionHash();

					if (NGNavSelectionWindow.historicCursor == 0 && NGNavSelectionWindow.lastHash == 0)
						break;
				}
				else
					break;
			}
			while (NGNavSelectionWindow.lastHash == 0);

			if (0 <= NGNavSelectionWindow.historicCursor &&
				NGNavSelectionWindow.historicCursor < NGNavSelectionWindow.historic.Count)
			{
				NGNavSelectionWindow.historic[NGNavSelectionWindow.historicCursor].Select();
				NGNavSelectionWindow.lastFocusedHistoric = -1;
				if (NGNavSelectionWindow.SelectionChanged != null)
					NGNavSelectionWindow.SelectionChanged();
			}
		}

		public static void	SelectNextSelection()
		{
			int	lastNotNullSelection = NGNavSelectionWindow.historicCursor;

			do
			{
				if (NGNavSelectionWindow.historicCursor >= 0 && NGNavSelectionWindow.historicCursor < NGNavSelectionWindow.historic.Count - 1)
				{
					++NGNavSelectionWindow.historicCursor;
					NGNavSelectionWindow.lastHash = NGNavSelectionWindow.historic[NGNavSelectionWindow.historicCursor].GetSelectionHash();
					if (NGNavSelectionWindow.lastHash != 0)
						lastNotNullSelection = NGNavSelectionWindow.historicCursor;

					if (NGNavSelectionWindow.historicCursor >= NGNavSelectionWindow.historic.Count - 1)
						NGNavSelectionWindow.historicCursor = -1;
				}
				else
					break;
			}
			while (NGNavSelectionWindow.lastHash == 0);

			if (0 <= lastNotNullSelection &&
				lastNotNullSelection < NGNavSelectionWindow.historic.Count)
			{
				NGNavSelectionWindow.historic[lastNotNullSelection].Select();
				NGNavSelectionWindow.lastFocusedHistoric = -1;
				if (NGNavSelectionWindow.SelectionChanged != null)
					NGNavSelectionWindow.SelectionChanged();
			}
			else if (lastNotNullSelection != -1)
			{
				NGNavSelectionWindow.historic[NGNavSelectionWindow.historic.Count - 1].Select();
				NGNavSelectionWindow.lastFocusedHistoric = -1;
				if (NGNavSelectionWindow.SelectionChanged != null)
					NGNavSelectionWindow.SelectionChanged();
			}
		}

		private static int	GetCurrentSelectionHash()
		{
			// Yeah, what? Is there a problem with my complex anti-colisionning hash function?
			int	hash = 0;

			for (int j = 0; j < Selection.instanceIDs.Length; j++)
				hash += Selection.instanceIDs[j];

			return hash;
		}

		private static void	SaveHistoric()
		{
			if (NGNavSelectionWindow.hasChanged == true)
			{
				NGNavSelectionWindow.hasChanged = false;

				StringBuilder	buffer = Utility.GetBuffer();

				for (int i = 0; i < NGNavSelectionWindow.historic.Count; ++i)
				{
					for (int j = 0; j < NGNavSelectionWindow.historic[i].refs.Count; j++)
					{
						Object	o = NGNavSelectionWindow.historic[i][j];

						if (o != null)
						{
							buffer.Append(o.GetInstanceID());
							buffer.Append(';');
						}
					}

					// Should never happens, except if the save is corrupted.
					if (buffer.Length > 0)
					{
						buffer.Length -= 1;
						buffer.Append(',');
					}
				}

				if (buffer.Length > 0)
					buffer.Length -= 1;

				NGEditorPrefs.SetString(NGNavSelectionWindow.AutoSavePrefKey, Utility.ReturnBuffer(buffer), true);
				NGEditorPrefs.SetInt(NGNavSelectionWindow.LastHashPrefKey, NGNavSelectionWindow.lastHash);
			}
		}

		protected virtual void	OnEnable()
		{
			Utility.RegisterWindow(this);
			Utility.RestoreIcon(this, NGNavSelectionWindow.TitleColor);

			Metrics.UseTool(9); // NGNavSelection

			NGChangeLogWindow.CheckLatestVersion(NGAssemblyInfo.Name);

			this.minSize = new Vector2(140F, Constants.SingleLineHeight);

			this.listDrawer = new GUIListDrawer<AssetsSelection>();
			this.listDrawer.list = NGNavSelectionWindow.historic;
			this.listDrawer.ElementGUI = this.DrawSelection;
			this.listDrawer.reverseList = true;

			HQ.SettingsChanged += this.Repaint;
			NGNavSelectionWindow.SelectionChanged += this.Repaint;
		}

		protected virtual void	OnDisable()
		{
			Utility.UnregisterWindow(this);
			HQ.SettingsChanged -= this.Repaint;
			NGNavSelectionWindow.SelectionChanged -= this.Repaint;
		}

		protected virtual void	OnGUI()
		{
			if (HQ.Settings == null)
			{
				GUILayout.Label(string.Format(LC.G("RequiringConfigurationFile"), NGNavSelectionWindow.Title));
				if (GUILayout.Button(LC.G("ShowPreferencesWindow")) == true)
					Utility.ShowPreferencesWindowAt(Constants.PreferenceTitle);
				return;
			}

			Rect	r = this.position;

			r.x = 0F;
			r.y = 0F;

			if (this.errorPopup.exception != null)
			{
				r.height = this.errorPopup.boxHeight;
				this.errorPopup.OnGUIRect(r);
				r.y += r.height;

				r.height = this.position.height - r.height;
			}

			try
			{
				this.listDrawer.OnGUI(r);
			}
			catch (Exception ex)
			{
				this.errorPopup.exception = ex;
			}
		}

		public static int	GetHistoricCursor()
		{
			return NGNavSelectionWindow.historicCursor;
		}

		public static void	SetHistoricCursor(int i)
		{
			NGNavSelectionWindow.historicCursor = i;

			if (i >= 0)
			{
				NGNavSelectionWindow.historic[NGNavSelectionWindow.historicCursor].Select();
				NGNavSelectionWindow.lastHash = NGNavSelectionWindow.GetCurrentSelectionHash();
			}
		}

		private void	DrawSelection(Rect r, AssetsSelection selection, int i)
		{
			if (Event.current.type == EventType.Repaint)
			{
				if (i == NGNavSelectionWindow.historicCursor ||
					(NGNavSelectionWindow.historicCursor == -1 && i == NGNavSelectionWindow.historic.Count - 1 && Selection.activeObject == NGNavSelectionWindow.historic[NGNavSelectionWindow.historic.Count - 1][0]))
				{
					float	w = r.width;
					r.width = NGNavSelectionWindow.HighlightCursorWidth;
					EditorGUI.DrawRect(r, NGNavSelectionWindow.HighlightCursorBackgroundColor);
					r.x += r.width;
					r.width = w - r.width;
				}

				if (i == NGNavSelectionWindow.lastFocusedHistoric)
				{
					float	w = r.width;
					r.width = NGNavSelectionWindow.HighlightCursorWidth;
					EditorGUI.DrawRect(r, NGNavSelectionWindow.HighlightFocusedHistoricBackgroundColor);
					r.x += r.width;
					r.width = w - r.width;
				}
			}

			if (Event.current.type == EventType.MouseDrag &&
				(this.dragOriginPosition - Event.current.mousePosition).sqrMagnitude >= Constants.MinStartDragDistance &&
				i.Equals(DragAndDrop.GetGenericData(Utility.DragObjectDataName)) == true)
			{
				DragAndDrop.StartDrag("Drag Object");
				Event.current.Use();
			}
			else if (Event.current.type == EventType.MouseDown &&
						r.Contains(Event.current.mousePosition) == true)
			{
				this.dragOriginPosition = Event.current.mousePosition;

				if (Event.current.button == 0)
				{
					DragAndDrop.PrepareStartDrag();
					DragAndDrop.objectReferences = new UnityEngine.Object[] { selection.refs[0].@object };
					DragAndDrop.SetGenericData(Utility.DragObjectDataName, i);
				}
			}

			if (selection.refs.Count == 1)
				Utility.content.text = this.GetHierarchy(selection.refs[0].@object);
			else
				Utility.content.text = "(" + selection.refs.Count + ") " + this.GetHierarchy(selection.refs[0].@object);
			Utility.content.image = Utility.GetIcon(selection.refs[0].instanceID);

			if (GUI.Button(r, Utility.content, GeneralStyles.ToolbarButtonLeft))
			{
				if (Event.current.button == 1 || this.lastClick + Constants.DoubleClickTime > EditorApplication.timeSinceStartup)
				{
					selection.Select();
					NGNavSelectionWindow.lastFocusedHistoric = i;
				}
				else if (Event.current.button == 0)
					EditorGUIUtility.PingObject(selection.refs[0].instanceID);

				this.lastClick = EditorApplication.timeSinceStartup;
			}

			Utility.content.image = null;
		}

		private string	GetHierarchy(Object obj)
		{
			if (obj == null)
				return "NULL";

			NavSettings	settings = HQ.Settings.Get<NavSettings>();

			if (settings.maxDisplayHierarchy == 0)
				return obj.name;

			GameObject	go = obj as GameObject;

			if (go != null)
			{
				StringBuilder	buffer = Utility.GetBuffer();
				Transform		transform = go.transform;

				buffer.Insert(0, transform.gameObject.name);
				buffer.Insert(0, '/');
				transform = transform.parent;

				for (int i = 0; (settings.maxDisplayHierarchy == -1 || i < settings.maxDisplayHierarchy) && transform != null; i++)
				{
					buffer.Insert(0, transform.gameObject.name);
					buffer.Insert(0, '/');
					transform = transform.parent;
				}

				buffer.Remove(0, 1);

				return Utility.ReturnBuffer(buffer);
			}
			else
			{
				string	path = AssetDatabase.GetAssetPath(obj);

				if (string.IsNullOrEmpty(path) == false)
				{
					if (settings.maxDisplayHierarchy == -1)
						return path;

					for (int i = path.Length - 1, j = 0; i >= 0; --i)
					{
						if (path[i] == '/')
						{
							++j;

							if (j - 1 == settings.maxDisplayHierarchy)
								return path.Substring(i + 1);
						}
					}

					return path;
				}
			}

			return obj.name;
		}

		protected virtual void	ShowButton(Rect r)
		{
			Utility.content.text = string.Empty;
			Utility.content.tooltip = "Lock the size of this window when moving neighbor windows. It does not prevent from changing the size if you resize it.";
			EditorGUI.BeginChangeCheck();
			this.isLocked = GUI.Toggle(r, this.isLocked, Utility.content, GeneralStyles.LockButton);
			Utility.content.tooltip = string.Empty;
			if (EditorGUI.EndChangeCheck() == true)
			{
				if (this.isLocked == true)
				{
					this.initialMin = this.minSize;
					this.initialMax = this.maxSize;
					this.minSize = new Vector2(this.position.width, this.position.height);
					this.maxSize = this.minSize;
				}
				else
				{
					this.minSize = this.initialMin;
					this.maxSize = this.initialMax;
				}
			}
		}

		void	IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			Utility.AddNGMenuItems(menu, this, NGNavSelectionWindow.Title, NGAssemblyInfo.WikiURL);
		}
	}
}