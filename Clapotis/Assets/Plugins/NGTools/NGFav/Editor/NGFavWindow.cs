using NGLicenses;
using NGTools;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;

namespace NGToolsEditor.NGFav
{
	using UnityEngine;

	public class NGFavWindow : EditorWindow, IHasCustomMenu
	{
		public const string	Title = "NG Fav";
		public static Color	TitleColor = new Color(199F / 255F, 21F / 255F, 133F / 255F, 1F);
		public const char	FavSeparator = ':';
		public const char	SaveSeparator = ',';
		public const char	SaveSeparatorCharPlaceholder = (char)4;
		public const float	FavSpacing = 5F;
		public const int	ForceRepaintRefreshTick = 100;
		public const string	DefaultSaveName = "default";

		public readonly static string[]	CacheIndexes = { "#1", "#2", "#3", "#4", "#5", "#6", "#7", "#8", "#9", "#10" };
		public readonly static string[]	CacheTooltips = { "Press Shift + F1.", "Press Shift + F2.", "Press Shift + F3.", "Press Shift + F4.", "Press Shift + F5.", "Press Shift + F6.", "Press Shift + F7.", "Press Shift + F8.", "Press Shift + F9.", "Press Shift + F10." };

		private const int				MaxFavorites = 1;
		private const int				MaxSelectionPerFavorite = 2;
		private const int				MaxAssetPerSelection = 3;
		private static readonly string	FreeAdContent = NGFavWindow.Title + " is restrained to:\n" +
														"• " + NGFavWindow.MaxFavorites + " favorites.\n" +
														"• " + NGFavWindow.MaxSelectionPerFavorite + " selections per favorite.\n" +
														"• " + NGFavWindow.MaxAssetPerSelection + " assets per selection.";

		private List<HorizontalScrollbar>	horizontalScrolls;
		private int							delayToDelete;
		private ReorderableList				list;

		[SerializeField]
		private Vector2	scrollPosition;
		private double	lastClick;
		[SerializeField]
		private int		currentSave;
		private Vector2	dragOriginPosition;

		[SerializeField]
		private Color	backgroundColor;

		private ErrorPopup	errorPopup = new ErrorPopup(NGFavWindow.Title, "An error occurred, try to reopen " + NGFavWindow.Title + ", clear the favorite or reset the settings.");

		private static bool	avoidMultiAdd;

		[MenuItem(Constants.MenuItemPath + NGFavWindow.Title, priority = Constants.MenuItemPriority + 307), Hotkey(NGFavWindow.Title)]
		public static void	Open()
		{
			Utility.OpenWindow<NGFavWindow>(NGFavWindow.Title);
		}

		#region Menu Items
		[MenuItem("Assets/Add Selection")]
		private static void	AssetAddSelection(MenuCommand menuCommand)
		{
			NGFavWindow	fav = EditorWindow.GetWindow<NGFavWindow>(NGFavWindow.Title);

			fav.CreateSelection(Selection.GetFiltered(typeof(Object), SelectionMode.Unfiltered));
		}

		[MenuItem("GameObject/Add Selection", priority = 12)]
		private static void	SceneAddSelection(MenuCommand menuCommand)
		{
			if (NGFavWindow.avoidMultiAdd == true)
				return;

			NGFavWindow.avoidMultiAdd = true;

			NGFavWindow	fav = EditorWindow.GetWindow<NGFavWindow>(NGFavWindow.Title);

			fav.CreateSelection(Selection.objects);
		}

		// Use validation to prevent multi add.
		[MenuItem("GameObject/Add Selection", true)]
		private static bool	ValidateAddSelection()
		{
			NGFavWindow.avoidMultiAdd = false;
			return Selection.activeObject is GameObject;
		}

		[MenuItem("Edit/Selection/Load " + NGFavWindow.Title + " Selection 1 #F1", priority = -999)]
		private static void	GoToFav1()
		{
			NGFavWindow.SelectFav(0);
		}

		[MenuItem("Edit/Selection/Load " + NGFavWindow.Title + " Selection 2 #F2", priority = -999)]
		private static void	GoToFav2()
		{
			NGFavWindow.SelectFav(1);
		}

		[MenuItem("Edit/Selection/Load " + NGFavWindow.Title + " Selection 3 #F3", priority = -999)]
		private static void	GoToFav3()
		{
			NGFavWindow.SelectFav(2);
		}

		[MenuItem("Edit/Selection/Load " + NGFavWindow.Title + " Selection 4 #F4", priority = -999)]
		private static void	GoToFav4()
		{
			NGFavWindow.SelectFav(3);
		}

		[MenuItem("Edit/Selection/Load " + NGFavWindow.Title + " Selection 5 #F5", priority = -999)]
		private static void	GoToFav5()
		{
			NGFavWindow.SelectFav(4);
		}

		[MenuItem("Edit/Selection/Load " + NGFavWindow.Title + " Selection 6 #F6", priority = -999)]
		private static void	GoToFav6()
		{
			NGFavWindow.SelectFav(5);
		}

		[MenuItem("Edit/Selection/Load " + NGFavWindow.Title + " Selection 7 #F7", priority = -999)]
		private static void	GoToFav7()
		{
			NGFavWindow.SelectFav(6);
		}

		[MenuItem("Edit/Selection/Load " + NGFavWindow.Title + " Selection 8 #F8", priority = -999)]
		private static void	GoToFav8()
		{
			NGFavWindow.SelectFav(7);
		}

		[MenuItem("Edit/Selection/Load " + NGFavWindow.Title + " Selection 9 #F9", priority = -999)]
		private static void	GoToFav9()
		{
			NGFavWindow.SelectFav(8);
		}

		[MenuItem("Edit/Selection/Load " + NGFavWindow.Title + " Selection 10 #F10", priority = -999)]
		private static void	GoToFav10()
		{
			NGFavWindow.SelectFav(9);
		}

		private static void	SelectFav(int i)
		{
			if (HQ.Settings == null)
				return;

			NGFavWindow[]	favs = Resources.FindObjectsOfTypeAll<NGFavWindow>();

			if (favs.Length == 0)
				return;

			NGFavWindow	fav = favs[0];
			FavSettings	settings = HQ.Settings.Get<FavSettings>();

			if (fav.currentSave >= 0 &&
				fav.currentSave < settings.favorites.Count &&
				settings.favorites[fav.currentSave].favorites.Count > i)
			{
				List<AssetsSelection>	selections = settings.favorites[fav.currentSave].favorites;

				for (int j = 0; j < selections[i].refs.Count; j++)
				{
					if (selections[i].refs[j].@object == null)
						selections[i].refs[j].TryReconnect();
				}

				List<Object>	list = new List<Object>();

				for (int j = 0; j < selections[i].refs.Count; j++)
				{
					if (selections[i].refs[j].@object != null)
						list.Add(selections[i].refs[j].@object);
				}

				if (list.Count > 0)
				{
					Selection.objects = list.ToArray();

					if (Selection.activeGameObject != null)
					{
						EditorGUIUtility.PingObject(Selection.activeGameObject);

						if (SceneView.lastActiveSceneView != null)
						{
							PrefabType	type = PrefabUtility.GetPrefabType(Selection.activeGameObject);
							if (type != PrefabType.ModelPrefab &&
								type != PrefabType.Prefab)
							{
								SceneView.lastActiveSceneView.FrameSelected();
							}
						}
					}
				}

				fav.list.index = i;
				fav.Repaint();
			}
		}
		#endregion

		protected virtual void	OnEnable()
		{
			Utility.RestoreIcon(this, NGFavWindow.TitleColor);

			Metrics.UseTool(3); // NGFav

			NGChangeLogWindow.CheckLatestVersion(NGAssemblyInfo.Name);

			this.wantsMouseMove = true;

			this.minSize = new Vector2(this.minSize.x, 0F);

			this.horizontalScrolls = new List<HorizontalScrollbar>();
			this.delayToDelete = -1;

			this.list = new ReorderableList(null, typeof(GameObject), true, false, false, false);
			this.list.showDefaultBackground = false;
			this.list.headerHeight = 0F;
			this.list.footerHeight = 0F;
			this.list.drawElementCallback = this.DrawElement;
			this.list.onReorderCallback = (ReorderableList list) => { HQ.InvalidateSettings(); };

			if (this.backgroundColor.a == 0F)
				this.backgroundColor = (Color)Utility.LoadEditorPref(this.backgroundColor, typeof(Color), "NGFav.backgroundColor");

			NGDiagnostic.DelayDiagnostic(this.Diagnose);

			HQ.SettingsChanged += this.CheckSettings;
			Utility.RegisterIntervalCallback(this.TryReconnectkNullObjects, NGFavWindow.ForceRepaintRefreshTick);
			RootGameObjectsManager.RootChanged += this.OnRootObjectsChanged;
			Undo.undoRedoPerformed += this.Repaint;

			this.CheckSettings();
		}

		protected virtual void	OnDisable()
		{
			if (this.backgroundColor.a > 0F)
				Utility.DirectSaveEditorPref(this.backgroundColor, typeof(Color), "NGFav.backgroundColor");

			HQ.SettingsChanged -= this.CheckSettings;
			Utility.UnregisterIntervalCallback(this.TryReconnectkNullObjects);
			RootGameObjectsManager.RootChanged -= this.OnRootObjectsChanged;
			Undo.undoRedoPerformed -= this.Repaint;
		}

		protected virtual void	OnGUI()
		{
			if (HQ.Settings == null)
			{
				GUILayout.Label(string.Format(LC.G("RequiringConfigurationFile"), NGFavWindow.Title));
				if (GUILayout.Button(LC.G("ShowPreferencesWindow")) == true)
					Utility.ShowPreferencesWindowAt(Constants.PreferenceTitle);
				return;
			}

			FreeLicenseOverlay.First(this, NGAssemblyInfo.Name + " Pro", NGFavWindow.FreeAdContent);

			FavSettings	settings = HQ.Settings.Get<FavSettings>();

			// Guarantee there is always one in the list.
			if (settings.favorites.Count == 0)
				settings.favorites.Add(new Favorites() { name = "default" });

			this.currentSave = Mathf.Clamp(this.currentSave, 0, settings.favorites.Count - 1);

			Favorites	fav = settings.favorites[this.currentSave];

			this.list.list = fav.favorites;

			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				if (GUILayout.Button("", GeneralStyles.ToolbarDropDown, GUILayoutOptionPool.Width(20F)) == true)
				{
					GenericMenu	menu = new GenericMenu();

					for (int i = 0; i < settings.favorites.Count; i++)
						menu.AddItem(new GUIContent((i + 1) + " - " + settings.favorites[i].name), i == this.currentSave, this.SwitchFavorite, i);

					menu.AddSeparator("");
					menu.AddItem(new GUIContent(LC.G("Add")), false, this.AddFavorite);

					Rect	r = GUILayoutUtility.GetLastRect();
					r.y += 16F;
					menu.DropDown(r);
					GUI.FocusControl(null);
				}

				EditorGUI.BeginChangeCheck();
				fav.name = EditorGUILayout.TextField(fav.name, GeneralStyles.ToolbarTextField, GUILayoutOptionPool.ExpandWidthTrue);
				if (EditorGUI.EndChangeCheck() == true)
					HQ.InvalidateSettings();

				if (GUILayout.Button(LC.G("Clear"), GeneralStyles.ToolbarButton) == true && ((Event.current.modifiers & Constants.ByPassPromptModifier) != 0 || EditorUtility.DisplayDialog(LC.G("NGFav_ClearSave"), string.Format(LC.G("NGFav_ClearSaveQuestion"), fav.name), LC.G("Yes"), LC.G("No")) == true))
				{
					Undo.RecordObject(settings, "Clear favorite");
					fav.favorites.Clear();
					HQ.InvalidateSettings();
					this.Focus();
					return;
				}

				EditorGUI.BeginDisabledGroup(settings.favorites.Count <= 1);
				if (GUILayout.Button(LC.G("Erase"), GeneralStyles.ToolbarButton) == true && ((Event.current.modifiers & Constants.ByPassPromptModifier) != 0 || EditorUtility.DisplayDialog(LC.G("NGFav_EraseSave"), string.Format(LC.G("NGFav_EraseSaveQuestion"), fav.name), LC.G("Yes"), LC.G("No")) == true))
				{
					Undo.RecordObject(settings, "Erase favorite");
					settings.favorites.RemoveAt(this.currentSave);
					this.currentSave = Mathf.Clamp(this.currentSave, 0, settings.favorites.Count - 1);
					this.list.list = fav.favorites;
					HQ.InvalidateSettings();
					this.Focus();
					return;
				}
				EditorGUI.EndDisabledGroup();

				Rect	r2 = GUILayoutUtility.GetRect(40F, 16F);
				r2.x += 5F;
				this.backgroundColor = EditorGUI.ColorField(r2, this.backgroundColor);
			}
			EditorGUILayout.EndHorizontal();

			Rect	overallDropZone = this.position;

			overallDropZone.x = 0F;
			overallDropZone.y = 0F;

			if (Event.current.type == EventType.Repaint && this.backgroundColor.a > 0F)
			{
				overallDropZone.y = 16F;
				overallDropZone.height -= 16F;
				EditorGUI.DrawRect(overallDropZone, this.backgroundColor);
				overallDropZone.y = 0;
			}

			overallDropZone.height = 16F;

			// Drop zone to add a new selection.
			if (Event.current.type == EventType.Repaint &&
				DragAndDrop.objectReferences.Length > 0)
			{
				Utility.DropZone(overallDropZone, "Create new selection");
				this.Repaint();
			}
			else if (Event.current.type == EventType.DragUpdated &&
					 overallDropZone.Contains(Event.current.mousePosition) == true)
			{
				if (DragAndDrop.objectReferences.Length > 0)
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				else
					DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
			}
			else if (Event.current.type == EventType.DragPerform &&
					 overallDropZone.Contains(Event.current.mousePosition) == true)
			{
				DragAndDrop.AcceptDrag();

				this.CreateSelection(DragAndDrop.objectReferences);

				DragAndDrop.PrepareStartDrag();
				Event.current.Use();
			}

			this.errorPopup.OnGUILayout();

			if (this.currentSave >= 0)
			{
				this.scrollPosition = EditorGUILayout.BeginScrollView(this.scrollPosition);
				{
					try
					{
						while (this.horizontalScrolls.Count < fav.favorites.Count)
							this.horizontalScrolls.Add(new HorizontalScrollbar(0F, 0F, this.position.width, 4F, 0F));

						this.list.DoLayoutList();
					}
					catch (Exception ex)
					{
						this.errorPopup.exception = ex;
						InternalNGDebug.LogFileException(ex);
					}
					finally
					{
						Utility.content.tooltip = string.Empty;
					}
				}
				EditorGUILayout.EndScrollView();

				if (this.delayToDelete != -1)
				{
					Undo.RecordObject(settings, "Delete favorite");
					fav.favorites.RemoveAt(this.delayToDelete);
					HQ.InvalidateSettings();
					this.delayToDelete = -1;
				}
			}

			if (Event.current.type == EventType.MouseDown)
			{
				DragAndDrop.PrepareStartDrag();
				this.dragOriginPosition = Vector2.zero;
			}

			FreeLicenseOverlay.Last(NGAssemblyInfo.Name + " Pro");
		}

		private void	SwitchFavorite(object data)
		{
			Undo.RecordObject(HQ.Settings.Get<FavSettings>(), "Switch favorite");
			this.currentSave = Mathf.Clamp((int)data, 0, HQ.Settings.Get<FavSettings>().favorites.Count - 1);
		}

		private void	AddFavorite()
		{
			FavSettings	settings = HQ.Settings.Get<FavSettings>();

			if (this.CheckMaxFavorites(settings.favorites.Count) == true)
			{
				Undo.RecordObject(settings, "Add favorite");
				settings.favorites.Add(new Favorites() { name = "Favorite " + (settings.favorites.Count + 1) });
				this.currentSave = settings.favorites.Count - 1;
				HQ.InvalidateSettings();
			}
		}

		private void	CreateSelection(Object[] objects)
		{
			AssetsSelection	selection = new AssetsSelection(objects);

			if (selection.refs.Count > 0)
			{
				FavSettings	settings = HQ.Settings.Get<FavSettings>();

				if (this.CheckMaxAssetsPerSelection(selection.refs.Count) == true &&
					this.CheckMaxSelections(settings.favorites[this.currentSave].favorites.Count) == true)
				{
					Undo.RecordObject(settings, "Add Selection as favorite");
					settings.favorites[this.currentSave].favorites.Add(selection);
					HQ.InvalidateSettings();
				}
			}
		}

		private void	OnRootObjectsChanged()
		{
			if (HQ.Settings == null)
				return;

			FavSettings	settings = HQ.Settings.Get<FavSettings>();

			if (this.currentSave >= 0 &&
				this.currentSave < settings.favorites.Count)
			{
				List<AssetsSelection>	fav = settings.favorites[this.currentSave].favorites;

				for (int i = 0; i < fav.Count; i++)
				{
					for (int j = 0; j < fav[i].refs.Count; j++)
					{
						if (fav[i].refs[j].@object == null)
							fav[i].refs[j].TryReconnect();
					}
				}

				this.Repaint();
			}
		}

		private void	DrawElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			FavSettings		settings = HQ.Settings.Get<FavSettings>();
			AssetsSelection	assetsSelection = settings.favorites[this.currentSave].favorites[index];
			float			x = rect.x;
			float			width = rect.width;

			if (rect.Contains(Event.current.mousePosition) == true)
			{
				float	totalWidth = 0F;

				for (int i = 0; i < assetsSelection.refs.Count; i++)
				{
					SelectionItem	selection = assetsSelection.refs[i];

					if (selection.@object == null)
					{
						if (selection.hierarchy.Count > 0)
							Utility.content.text = (string.IsNullOrEmpty(selection.resolverAssemblyQualifiedName) == false ? "(R)" : "") + selection.hierarchy[selection.hierarchy.Count - 1];
						else
							Utility.content.text = "Unknown";
					}
					else
					{
						Utility.content.text = selection.@object.name;

						if (Utility.GetIcon(selection.@object.GetInstanceID()) != null)
							totalWidth += rect.height;
					}

					totalWidth += GUI.skin.label.CalcSize(Utility.content).x + NGFavWindow.FavSpacing;
				}

				if (index < 10)
				{
					if (index < 9)
						totalWidth += 24F;
					else
						totalWidth += 22F; // Number 10 is centralized.
				}

				if (assetsSelection.refs.Count > 1)
				{
					Utility.content.text = "(" + assetsSelection.refs.Count + ")";
					totalWidth += GUI.skin.label.CalcSize(Utility.content).x;
				}

				this.horizontalScrolls[index].RealWidth = totalWidth;
				this.horizontalScrolls[index].SetPosition(rect.x, rect.y);
				this.horizontalScrolls[index].SetSize(rect.width);
				this.horizontalScrolls[index].OnGUI();

				if (Event.current.type == EventType.MouseMove)
					this.Repaint();

				rect.width = 20F;
				rect.x = x + width - rect.width;
				rect.height -= 4F;

				if (GUI.Button(rect, "X") == true)
				{
					this.delayToDelete = index;
					return;
				}

				rect.height += 4F;
				rect.x = x;
				rect.width = width;
			}

			rect.x -= this.horizontalScrolls[index].Offset;

			if (index <= 9)
			{
				rect.width = 24F;
				Utility.content.text = NGFavWindow.CacheIndexes[index];
				Utility.content.tooltip = NGFavWindow.CacheTooltips[index];
				if (index <= 8)
					EditorGUI.LabelField(rect, Utility.content, GeneralStyles.HorizontalCenteredText);
				else
				{
					rect.width = 27F;
					rect.x -= 4F;
					EditorGUI.LabelField(rect, Utility.content, GeneralStyles.HorizontalCenteredText);
					rect.x += 4F;
				}
				Utility.content.tooltip = string.Empty;
				rect.x += rect.width;
				rect.width = width - rect.x;
			}

			if (assetsSelection.refs.Count >= 2)
			{
				Utility.content.text = "(" + assetsSelection.refs.Count + ")";
				rect.width = GeneralStyles.HorizontalCenteredText.CalcSize(Utility.content).x;
				GUI.Label(rect, Utility.content, GeneralStyles.HorizontalCenteredText);
				rect.x += rect.width;
				rect.width = width - rect.x;
			}

			Rect	dropZone = rect;
			dropZone.xMin = dropZone.xMax - dropZone.width / 3F;

			for (int i = 0; i < assetsSelection.refs.Count; i++)
			{
				SelectionItem	selectionItem = assetsSelection.refs[i];;
				if (selectionItem.@object == null)
					selectionItem.TryReconnect();

				Texture	icon = null;

				Utility.content.tooltip = selectionItem.resolverFailedError;

				EditorGUI.BeginDisabledGroup(selectionItem.@object == null);
				{
					if (selectionItem.@object == null)
					{
						if (selectionItem.hierarchy.Count > 0)
							Utility.content.text = (string.IsNullOrEmpty(selectionItem.resolverAssemblyQualifiedName) == false ? "(R)" : "") + selectionItem.hierarchy[selectionItem.hierarchy.Count - 1];
						else
							Utility.content.text = "Unknown";
					}
					else
					{
						Utility.content.text = selectionItem.@object.name;
						icon = Utility.GetIcon(selectionItem.@object.GetInstanceID());
					}

					if (icon != null)
					{
						rect.width = rect.height;
						GUI.DrawTexture(rect, icon);
						rect.x += rect.width;
					}

					rect.width = GeneralStyles.HorizontalCenteredText.CalcSize(Utility.content).x;

					if (string.IsNullOrEmpty(selectionItem.resolverFailedError) == false)
						GeneralStyles.HorizontalCenteredText.normal.textColor = Color.red;

					GUI.Label(rect, Utility.content, GeneralStyles.HorizontalCenteredText);
					Utility.content.tooltip = string.Empty;
					GeneralStyles.HorizontalCenteredText.normal.textColor = EditorStyles.label.normal.textColor;

					if (icon != null)
						rect.xMin -= rect.height;
				}
				EditorGUI.EndDisabledGroup();

				if (selectionItem.@object != null)
				{
					if (Event.current.type == EventType.MouseDrag &&
						(this.dragOriginPosition - Event.current.mousePosition).sqrMagnitude >= Constants.MinStartDragDistance &&
						DragAndDrop.GetGenericData("t") as Object == selectionItem.@object)
					{
						DragAndDrop.StartDrag("Drag favorite");
						Event.current.Use();
					}
					else if (Event.current.type == EventType.MouseDown &&
							 rect.Contains(Event.current.mousePosition) == true)
					{
						this.dragOriginPosition = Event.current.mousePosition;

						DragAndDrop.PrepareStartDrag();
						// Add this data to force drag on this object only because user has click down on it.
						DragAndDrop.SetGenericData("t", selectionItem.@object);
						DragAndDrop.objectReferences = new Object[] { selectionItem.@object };

						Event.current.Use();
					}
					else if (Event.current.type == EventType.MouseUp &&
							 rect.Contains(Event.current.mousePosition) == true)
					{
						DragAndDrop.PrepareStartDrag();

						if (Event.current.button == 0 && (int)Event.current.modifiers == ((int)settings.deleteModifiers >> 1))
						{
							Undo.RecordObject(settings, "Delete element in favorite");
							assetsSelection.refs.RemoveAt(i);

							if (assetsSelection.refs.Count == 0)
								this.delayToDelete = index;
						}
						else if (Event.current.button == 1 ||
								 settings.changeSelection == FavSettings.ChangeSelection.SimpleClick ||
								 ((settings.changeSelection == FavSettings.ChangeSelection.DoubleClick || settings.changeSelection == FavSettings.ChangeSelection.ModifierOrDoubleClick) &&
								  this.lastClick + Constants.DoubleClickTime > EditorApplication.timeSinceStartup) ||
								 ((settings.changeSelection == FavSettings.ChangeSelection.Modifier || settings.changeSelection == FavSettings.ChangeSelection.ModifierOrDoubleClick) &&
								 // HACK We need to shift the event modifier's value. Bug ref #720211_8cg6m8s7akdbf1r5
								  (int)Event.current.modifiers == ((int)settings.selectModifiers >> 1)))
						{
							NGFavWindow.SelectFav(index);
						}
						else
							EditorGUIUtility.PingObject(assetsSelection.refs[i].@object);

						this.lastClick = EditorApplication.timeSinceStartup;

						this.list.index = index;
						this.Repaint();

						Event.current.Use();
					}
				}
				else
				{
					// Clean drag on null object, to prevent starting a drag when passing over non-null one without click down.
					if (Event.current.type == EventType.MouseDown &&
						rect.Contains(Event.current.mousePosition) == true &&
						HQ.Settings != null)
					{
						if ((int)Event.current.modifiers == ((int)settings.deleteModifiers >> 1))
						{
							Undo.RecordObject(settings, "Delete element in favorite");
							assetsSelection.refs.RemoveAt(i);

							if (assetsSelection.refs.Count == 0)
								this.delayToDelete = index;

							Event.current.Use();
						}

						DragAndDrop.PrepareStartDrag();
					}
				}

				rect.x += rect.width + NGFavWindow.FavSpacing;

				if (rect.x >= this.position.width)
					break;
			}

			rect.x = x;
			rect.width = width;

			// Drop zone to append new Object to the current selection.
			if (Event.current.type == EventType.Repaint &&
				DragAndDrop.objectReferences.Length > 0 &&
				rect.Contains(Event.current.mousePosition) == true)
			{
				Utility.DropZone(dropZone, "Add to selection");
			}
			else if (Event.current.type == EventType.DragUpdated &&
					 dropZone.Contains(Event.current.mousePosition) == true)
			{
				if (DragAndDrop.objectReferences.Length > 0)
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				else
					DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
			}
			else if (Event.current.type == EventType.DragPerform &&
					 dropZone.Contains(Event.current.mousePosition) == true)
			{
				DragAndDrop.AcceptDrag();

				for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
				{
					int	j = 0;

					for (; j < assetsSelection.refs.Count; j++)
					{
						if (assetsSelection.refs[j].@object == DragAndDrop.objectReferences[i])
							break;
					}

					if (j == assetsSelection.refs.Count)
					{
						if (this.CheckMaxAssetsPerSelection(assetsSelection.refs.Count) == true)
						{
							Undo.RecordObject(settings, "Add to favorite");
							assetsSelection.refs.Add(new SelectionItem(DragAndDrop.objectReferences[i]));
							HQ.InvalidateSettings();
						}
						else
							break;
					}
				}

				DragAndDrop.PrepareStartDrag();
				Event.current.Use();
			}

			// Just draw the button in front.
			if (rect.Contains(Event.current.mousePosition) == true)
			{
				rect.width = 20F;
				rect.x = x + width - rect.width;
				rect.height -= 4F;

				GUI.Button(rect, "X");
			}
		}

		private void	TryReconnectkNullObjects()
		{
			if (HQ.Settings == null)
				return;

			FavSettings	settings = HQ.Settings.Get<FavSettings>();

			if (this.currentSave >= 0 &&
				this.currentSave < settings.favorites.Count)
			{
				List<AssetsSelection>	selections = settings.favorites[this.currentSave].favorites;

				for (int i = 0; i < selections.Count; i++)
				{
					for (int j = 0; j < selections[i].refs.Count; j++)
					{
						if (selections[i].refs[j].@object == null)
						{
							if (selections[i].refs[j].hasObject == true)
							{
								selections[i].refs[j].hasObject = false;
								this.Repaint();
							}

							if (selections[i].refs[j].TryReconnect() == true)
								this.Repaint();
						}
					}
				}
			}
		}

		private void	CheckSettings()
		{
			this.Repaint();

			if (HQ.Settings != null)
			{
				FavSettings	settings = HQ.Settings.Get<FavSettings>();

				// Guarantee there is always one in the list.
				if (settings.favorites.Count == 0)
					settings.favorites.Add(new Favorites() { name = "default" });
			}
		}

		private void	Diagnose()
		{
			NGDiagnostic.Log(NGFavWindow.Title, "CurrentFav", this.currentSave);

			FavSettings	settings = HQ.Settings.Get<FavSettings>();

			for (int i = 0; i < settings.favorites.Count; i++)
				NGDiagnostic.Log(NGFavWindow.Title, "Fav[" + i + "]", JsonUtility.ToJson(settings.favorites[i]));
		}

		private bool	CheckMaxFavorites(int count)
		{
			return NGLicensesManager.Check(count < NGFavWindow.MaxFavorites, NGAssemblyInfo.Name + " Pro", "Free version does not allow more than " + NGFavWindow.MaxFavorites + " favorites.\n\nI have to be honest, it is sufficient for me. ;)");
		}

		private bool	CheckMaxSelections(int count)
		{
			return NGLicensesManager.Check(count < NGFavWindow.MaxSelectionPerFavorite, NGAssemblyInfo.Name + " Pro", "Free version does not allow more than " + NGFavWindow.MaxSelectionPerFavorite + " slots.\n\nMaybe you should buy it, might be useful one day. :)");
		}

		private bool	CheckMaxAssetsPerSelection(int count)
		{
			return NGLicensesManager.Check(count <= NGFavWindow.MaxAssetPerSelection, NGAssemblyInfo.Name + " Pro", "Free version does not allow more than " + NGFavWindow.MaxAssetPerSelection + " assets per selection.\n\nHey, don't abuse of this! X)");
		}

		void	IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			Utility.AddNGMenuItems(menu, this, NGFavWindow.Title, NGAssemblyInfo.WikiURL);
		}
	}
}