using NGLicenses;
using NGTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine.SceneManagement;

namespace NGToolsEditor.NGAssetFinder
{
	using UnityEngine;

	public class NGAssetFinderWindow : EditorWindow, IHasCustomMenu
	{
		private sealed class SubAssetBrowser : PopupWindowContent
		{
			public const float	MaxRows = 20F;

			private Asset				mainAsset;
			private VerticalScrollbar	scrollbar;

			public	SubAssetBrowser(Asset asset)
			{
				this.mainAsset = asset;
			}

			public override Vector2	GetWindowSize()
			{
				return new Vector2(this.mainAsset.assetsFinder.position.width - 80F, Mathf.Clamp(this.mainAsset.CountOpenSubAssets() * Constants.SingleLineHeight, 0F, Constants.SingleLineHeight * SubAssetBrowser.MaxRows));
			}

			public override void	OnOpen()
			{
				base.OnOpen();

				this.scrollbar = new VerticalScrollbar(this.mainAsset.assetsFinder.position.width - 80F - 16F, 0F, this.editorWindow.position.height);
				this.scrollbar.RealHeight = this.mainAsset.CountOpenSubAssets() * Constants.SingleLineHeight;
				this.scrollbar.AddInterest(this.mainAsset.GetTargetOffset() + 8F, Color.yellow);
			}

			public override void	OnGUI(Rect rect)
			{
				this.scrollbar.SetSize(this.editorWindow.position.height);
				this.scrollbar.OnGUI();

				EditorGUI.BeginChangeCheck();
				rect.y = -this.scrollbar.Offset;
				rect.height = Constants.SingleLineHeight;
				this.mainAsset.Draw(rect);
				if (EditorGUI.EndChangeCheck() == true ||
					Event.current.type == EventType.Used)
				{
					this.scrollbar.RealHeight = this.mainAsset.CountOpenSubAssets() * Constants.SingleLineHeight;
					this.scrollbar.ClearInterests();
					this.scrollbar.AddInterest(this.mainAsset.GetTargetOffset() + 8F, Color.yellow);
				}
			}
		}

		private sealed class AssetsExtensionsPopup : PopupWindowContent
		{
			private readonly NGAssetFinderWindow	window;

			public AssetsExtensionsPopup(NGAssetFinderWindow window)
			{
				this.window = window;
			}

			public override void	OnOpen()
			{
				Undo.undoRedoPerformed += this.editorWindow.Repaint;
			}

			public override void	OnClose()
			{
				Undo.undoRedoPerformed -= this.editorWindow.Repaint;
			}

			public override Vector2	GetWindowSize()
			{
				return new Vector2(150F, 24F + (NGAssetFinderWindow.AssetsExtensions.Length * Constants.SingleLineHeight));
			}

			public override void	OnGUI(Rect r)
			{
				r.height = 24F;
				r.width *= .5F;
				if (GUI.Button(r, "All") == true)
				{
					Undo.RecordObject(this.window, "Select all extensions");
					this.window.assetContent.text = null;
					this.window.searchExtensionsMask = int.MaxValue;
					this.window.searchAssets |= SearchAssets.Asset;
					this.window.Repaint();
				}
				r.x += r.width;

				if (GUI.Button(r, "None") == true)
				{
					Undo.RecordObject(this.window, "Deselect all extensions");
					this.window.assetContent.text = null;
					this.window.searchExtensionsMask = 0;
					this.window.searchAssets |= SearchAssets.Asset;
					this.window.Repaint();
				}
				r.y += r.height;

				r.x = 0F;
				r.width *= 2F;
				r.height = Constants.SingleLineHeight;

				for (int i = 0; i < NGAssetFinderWindow.AssetsExtensions.Length; i++)
				{
					EditorGUI.BeginChangeCheck();
					NGAssetFinderWindow.AssetsExtensions[i].content.image = NGAssetFinderWindow.AssetsExtensions[i].Icon;
					GUI.Toggle(r, (this.window.searchExtensionsMask & (1 << i)) != 0, NGAssetFinderWindow.AssetsExtensions[i].content, GeneralStyles.ToolbarLeftButton);
					if (EditorGUI.EndChangeCheck() == true)
					{
						Undo.RecordObject(this.window, "Toggle extension");
						this.window.assetContent.text = null;
						this.window.searchExtensionsMask ^= 1 << i;
						this.window.searchAssets |= SearchAssets.Asset;
						this.window.Repaint();
					}
					r.y += r.height;
				}
			}
		}

		private sealed class SearchOptionsPopup : PopupWindowContent
		{
			private readonly NGAssetFinderWindow	window;

			public	SearchOptionsPopup(NGAssetFinderWindow window)
			{
				this.window = window;
			}

			public override void	OnOpen()
			{
				Undo.undoRedoPerformed += this.editorWindow.Repaint;
			}

			public override void	OnClose()
			{
				Undo.undoRedoPerformed -= this.editorWindow.Repaint;
			}

			public override Vector2	GetWindowSize()
			{
				return new Vector2(this.window.position.width, NGAssetFinderWindow.SearchOptionsBarHeight);
			}

			public override void	OnGUI(Rect r)
			{
				EditorGUI.BeginChangeCheck();
				this.window.DrawPrefabOptions(r, true);
				if (EditorGUI.EndChangeCheck() == true)
					this.window.Repaint();

				if (XGUIHighlightManager.IsHighlightRunning(NGAssetFinderWindow.Title + ".ByInstance") == true || XGUIHighlightManager.IsHighlightRunning(NGAssetFinderWindow.Title + ".ByComponentType") == true)
					this.editorWindow.Repaint();
			}
		}

		private sealed class Asset
		{
			public readonly NGAssetFinderWindow	assetsFinder;
			public readonly Object		asset;
			public readonly string		name;
			public readonly Texture2D	image;
			public readonly List<Asset>	children;

			private bool	open;

			public	Asset(NGAssetFinderWindow assetsFinder, Object asset)
			{
				this.assetsFinder = assetsFinder;
				this.asset = asset;
				if (this.asset is Component)
					this.name = this.asset.GetType().Name + " (Component)";
				else
					this.name = this.asset.name;
				this.image = Utility.GetIcon(this.asset.GetInstanceID());
				this.children = new List<Asset>();

				this.open = true;
			}

			public float	GetTargetOffset()
			{
				if (this.asset == this.assetsFinder.targetAsset)
					return 0F;

				float	y = Constants.SingleLineHeight;

				for (int i = 0; i < this.children.Count; i++)
				{
					if (this.children[i].GetTargetOffset(this.open, ref y) == true)
						return y;
				}

				return -1F;
			}

			public bool	GetTargetOffset(bool addOffset, ref float y)
			{
				if (this.asset == this.assetsFinder.targetAsset)
					return true;

				if (addOffset == true)
				{
					y += Constants.SingleLineHeight;

					for (int i = 0; i < this.children.Count; i++)
					{
						if (this.children[i].GetTargetOffset(this.open && addOffset == true, ref y) == true)
							return true;
					}
				}

				return false;
			}

			public Asset	Find(Object target)
			{
				if (this.asset == target)
					return this;

				for (int i = 0; i < this.children.Count; i++)
				{
					if (this.children[i].Find(target) != null)
						return this.children[i];
				}

				return null;
			}

			public float	GetHeight()
			{
				return this.CountOpenSubAssets() * Constants.SingleLineHeight;
			}

			public void		Draw(Rect r)
			{
				r.height = Constants.SingleLineHeight;
				r.xMin = 4F;

				float	w = r.width;

				r.width = 16F;
				if (this.children.Count > 0)
				{
					r.x += EditorGUI.indentLevel * 16F;
					if (Event.current.type == EventType.MouseUp && r.Contains(Event.current.mousePosition) == true)
					{
						this.open = !this.open;
						GUI.FocusControl(null);
						Event.current.Use();
					}

					r.x -= EditorGUI.indentLevel * 16F;
					EditorGUI.Foldout(r, this.open, string.Empty);
				}

				r.x += r.width;
				r.width = w - r.width;
				if (EditorGUI.ToggleLeft(r, string.Empty, this.asset == this.assetsFinder.targetAsset) == true)
				{
					this.assetsFinder.targetAsset = this.asset;

					if ((this.assetsFinder.targetAsset is Component) == false && (this.assetsFinder.targetAsset is MonoScript) == false)
					{
						this.assetsFinder.searchOptions &= ~SearchOptions.ByComponentType;
						this.assetsFinder.searchOptions |= SearchOptions.ByInstance;
					}

					this.assetsFinder.Repaint();
				}

				r.xMin += 30F;
				Utility.content.text = this.name;
				EditorGUI.LabelField(r, Utility.content);
				r.xMin -= 30F;

				Rect	r2 = r;
				r2.x += (EditorGUI.indentLevel + 1) * 15F;
				r2.width = r2.height;
				GUI.DrawTexture(r2, this.image);

				if (this.open == true)
				{
					r.y += r.height;

					++EditorGUI.indentLevel;
					for (int i = 0; i < this.children.Count; i++)
					{
						r.height = this.children[i].GetHeight();
						this.children[i].Draw(r);
						r.y += r.height;
					}
					--EditorGUI.indentLevel;
				}
			}

			public int	CountSubAssets()
			{
				int	c = 1;

				for (int i = 0; i < this.children.Count; i++)
					c += this.children[i].CountSubAssets();

				return c;
			}

			public int	CountOpenSubAssets()
			{
				int	c = 1;

				if (this.open == true)
				{
					for (int i = 0; i < this.children.Count; i++)
						c += this.children[i].CountOpenSubAssets();
				}

				return c;
			}
		}

		internal struct ExtensionsIcon
		{
			public string		name;
			public string		iconName;
			public Texture2D	Icon { get { return icon ?? (this.icon = InternalEditorUtility.GetIconForFile(this.extensions[0])); } }
			public string[]		extensions;

			public GUIContent	content;

			private Texture2D	icon;

			public	ExtensionsIcon(string name, string iconName, params string[] extensions)
			{
				this.name = name;
				this.iconName = iconName;
				this.extensions = extensions;
				this.icon = null;

				this.content = new GUIContent(name, null, "Extensions :\n" + string.Join("\n", extensions));
			}
		}

		[Serializable]
		public abstract class SceneFilters<T>
		{
			public string	sceneGUID;
			public bool		open;
			public List<T>	filters = new List<T>();

			[NonSerialized]
			public string	cachedFiltersLabel;

			public abstract void	UpdateCacheLabel();
		}

		public sealed class SceneGameObjectFilters : SceneFilters<FilterGameObject>
		{
			public override void	UpdateCacheLabel()
			{
				string			sceneName = string.IsNullOrEmpty(this.sceneGUID) == false ? Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(this.sceneGUID)) : "Untitled (Unsaved scene)";
				StringBuilder	buffer = Utility.GetBuffer("Search in " + sceneName + ":");
				bool			onlyExclusive = true;

				for (int i = 0, j = 0; i < this.filters.Count; i++)
				{
					if (this.filters[i].active == true && this.filters[i].GameObject != null)
					{
						if (this.filters[i].type == Filter.Type.Inclusive)
						{
							onlyExclusive = false;
							buffer.Append(" +");
						}
						else
							buffer.Append(" -");
						buffer.Append(this.filters[i].GameObject.name);
						++j;
					}
				}

				if (onlyExclusive == true)
					buffer.Insert("Search in :".Length + sceneName.Length, " All");

				if (buffer.Length <= "Search in :".Length + sceneName.Length)
					buffer.Append(" Searching in all hierarchy. (Drop Game Object here to filter.)");

				this.cachedFiltersLabel = Utility.ReturnBuffer(buffer);
			}
		}

		public sealed class SceneProjectFilters : SceneFilters<FilterText>
		{
			public override void	UpdateCacheLabel()
			{
				StringBuilder	buffer = Utility.GetBuffer("Search Paths :");
				bool			onlyExclusive = true;

				for (int i = 0, j = 0; i < this.filters.Count; i++)
				{
					if (this.filters[i].active == true)
					{
						if (this.filters[i].type == Filter.Type.Inclusive)
						{
							onlyExclusive = false;
							buffer.Append(" +");
						}
						else
							buffer.Append(" -");
						buffer.Append(Path.GetFileName(this.filters[i].text));
						++j;
					}
				}

				if (onlyExclusive == true)
					buffer.Insert("Search Paths :".Length, " All");

				if (buffer.Length <= "Search Paths :".Length)
					buffer.Append(" Searching in all project. (Drop folders here to filter.)");

				this.cachedFiltersLabel = Utility.ReturnBuffer(buffer);
			}
		}

		private enum DropState
		{
			Allowed,
			Already,
			Rejected,
			DifferentScene,
		}

		public const string				Title = "NG Asset Finder";
		public static Color				TitleColor = Color.blue;
		public const float				TargetReplaceLabelWidth = 100F;
		public const float				FindButtonWidth = 100F;
		public const float				FindButtonLeftSpacing = 5F;
		public const float				SwitchButtonWidth = 20F;
		public const float				SearchHeaderWidth = 50F;
		public const float				SearchSelectorWidth = 70F;
		public const float				Spacing = 2F;
		public const float				OptionsSpacing = 7F;
		public const float				SearchOptionsToggleWidth = 32F;
		public const float				SearchOptionsBarHeight = 18F;
		public const float				SearchSceneOrProjectButtonHeight = 32F;
		public const float				DropZoneHeight = 32F;
		public const float				FilterToggleWidth = 20F;
		public const float				FilterInclusiveWidth = 100F;
		public const float				DeleteButtonWidth = 20F;
		public const float				SelectorHeight = 18F;
		public const float				UseCacheButtonWidth = 100F;
		public const float				ResultsScrollHeight = 24F;
		public const float				ExportButtonWidth = 70F;
		public const float				ClearButtonWidth = 70F;
		public const float				Margin = 4F;
		public static readonly Color	SelectedAssetBackground = Color.black * .2F;
		public static readonly Color	HighlightBackground = Color.black * .5F;
		public static readonly Color	DisabledHeaderButton = Color.white * .8F;
		public static Color				ValidFilterRefColor { get { return Utility.GetSkinColor(0F, 1F, 0F, 1F, 0F, 1F, 0F, 1F); } }
		public static Color				MissingFilterRefColor { get { return Utility.GetSkinColor(1F, 0F, 0F, 1F, .5F, .5F, .5F, 1F); } }
		public static readonly Color	SelectedResult = Color.blue;
		public static readonly TypeMembersExclusion[]	EmptyTME = new TypeMembersExclusion[0];

		internal const int				MaxAssetReplacements = 5;
		private static readonly string	FreeAdContent = NGAssetFinderWindow.Title + " is restrained to " + NGAssetFinderWindow.MaxAssetReplacements + " replacements at once.\nYou can replace many times.\n\nThe cache when searching into the project is reserved to pro version.";

		internal static readonly ExtensionsIcon[]	AssetsExtensions = new ExtensionsIcon[]
		{
			new ExtensionsIcon("Animation", "Animation Icon", ".anim"),
			//new ExtensionsIcon("Asset", "GameManager Icon", ".asset", ".prefs"), // Moved to Scriptable Object instead.
			new ExtensionsIcon("Audio Clip", "AudioClip Icon", ".aac", ".aif", ".aiff", ".au", ".mid", ".midi", ".mp3", ".mpa", ".ra", ".ram", ".wma", ".wav", ".wave", ".ogg"),
			new ExtensionsIcon("Audio Mixer Controller", "AudioMixerController Icon", ".mixer"),
			new ExtensionsIcon("Boo Script", "boo Script Icon", ".boo"),
			new ExtensionsIcon("C# Script", "cs Script Icon", ".cs"),
			new ExtensionsIcon("CGProgram", "CGProgram Icon", ".cginc"),
			new ExtensionsIcon("Font", "Font Icon", ".ttf", ".otf", ".fon", ".fnt"),
			new ExtensionsIcon("GUISkin", "GUISkin Icon", ".guiskin"),
			new ExtensionsIcon("Javascript Script", "Js Script Icon", ".js"),
			new ExtensionsIcon("Material", "Material Icon", ".mat"),
			new ExtensionsIcon("Mesh", "Mesh Icon", ".3dm", ".3dmf", ".3ds", ".3dv", ".3dx", ".blend", ".c4d", ".lwo", ".lws", ".ma", ".max", ".mb", ".mesh", ".obj", ".vrl", ".wrl", ".wrz", ".fbx"),
			//new ExtensionsIcon("Meta File", "MetaFile Icon", ".meta"),
			new ExtensionsIcon("Movie Texture", "MovieTexture Icon", ".asf", ".asx", ".avi", ".dat", ".divx", ".dvx", ".mlv", ".m2l", ".m2t", ".m2ts", ".m2v", ".m4e", ".m4v", ".mjp", ".mov", ".movie", ".mp21", ".mp4", ".mpe", ".mpeg", ".mpg", ".mpv2", ".ogm", ".qt", ".rm", ".rmvb", ".wmw", ".xvid"),
			new ExtensionsIcon("Physic Material", "PhysicMaterial Icon", ".physicmaterial"),
			//new ExtensionsIcon("Prefab", "PrefabNormal Icon", ".prefab"),
			//new ExtensionsIcon("Scene", "SceneAsset Icon", ".unity"),
			new ExtensionsIcon("Scriptable Object", "ScriptableObject Icon", ".asset", ".prefs", ".colors", ".gradients", ".curves", ".curvesnormalized", ".particlecurves", ".particlecurvessigned", ".particledoublecurves", ".particledoublecurvessigned"),
			new ExtensionsIcon("Shader", "Shader Icon", ".shader"),
			new ExtensionsIcon("Text", "TextAsset Icon", ".txt"),
			new ExtensionsIcon("Texture", "Texture Icon", ".ai", ".apng", ".png", ".bmp", ".cdr", ".dib", ".eps", ".exif", ".gif", ".ico", ".icon", ".j", ".j2c", ".j2k", ".jas", ".jiff", ".jng", ".jp2", ".jpc", ".jpe", ".jpeg", ".jpf", ".jpg", ".jpw", ".jpx", ".jtf", ".mac", ".omf", ".qif", ".qti", ".qtif", ".tex", ".tfw", ".tga", ".tif", ".tiff", ".wmf", ".psd", ".exr", ".hdr"),
			new ExtensionsIcon("Others", "Default Icon", ".*")
		};

		public Object	TargetAsset { get { return this.targetAsset; } }

		public bool				showTargetOptions;
		public SearchOptions	searchOptions;
		public SearchAssets		searchAssets;
		public int				searchExtensionsMask = int.MaxValue;
		public List<SceneGameObjectFilters>	sceneFilters = new List<SceneGameObjectFilters>();
		public SceneProjectFilters	projectFilters = new SceneProjectFilters();

		public bool		useCache = true;
		public bool		canReplace;
		public Object	targetAsset;
		public Object	replaceAsset;

		internal TypeMembersExclusion[]	typeExclusions;
		internal Type					targetType;
		internal BindingFlags			fieldSearchFlags;
		internal BindingFlags			propertySearchFlags;

		private List<SearchResult>	results = new List<SearchResult>();
		private int					currentResult;
		private AssetFinder			assetFinder;
		[NonSerialized]
		private bool			isSearching;
		internal bool			debugAnalyzedTypes = false;

		private Asset	mainAsset;

		private HorizontalScrollbar	resultsScrollbar;

		internal DynamicOrderedArray<TypeFinder>		typeFinders;
		internal DynamicOrderedArray<ObjectFinder>	objectFinders;

		private GUIContent	instanceContent = new GUIContent("By Instance", "Look for references of the target.");
		private GUIContent	componentTypeContent = new GUIContent("By Component Type", "Look for Component of the same type as target type.");
		private GUIContent	serializeFieldContent = new GUIContent("Serialized Field", "Include serialized fields.");
		private GUIContent	nonPublicContent = new GUIContent("Hidden Fields", "Include non-public members. This option considerably increases the search time.");
		private GUIContent	propertyContent = new GUIContent("Properties", "Include properties with get/set defined. Indexers are excluded.");

		private GUIContent	headerSceneContent = new GUIContent("Scene");
		private GUIContent	headerProjectContent = new GUIContent("Project");

		private GUIContent	assetContent = new GUIContent();
		private GUIContent	prefabContent = new GUIContent("Prefab");
		private GUIContent	sceneContent = new GUIContent("Scene");

		internal ErrorPopup	errorPopup = new ErrorPopup(NGAssetFinderWindow.Title, "An error occurred, try to reopen " + NGAssetFinderWindow.Title + ".");

		private Rect	dragRect;

		#region Menu Items
		[MenuItem(Constants.MenuItemPath + NGAssetFinderWindow.Title + "	[BETA]", priority = Constants.MenuItemPriority + 330), Hotkey(NGAssetFinderWindow.Title)]
		public static void	Open()
		{
			Utility.OpenWindow<NGAssetFinderWindow>(false, NGAssetFinderWindow.Title);
		}

		[MenuItem("CONTEXT/Component/Find all references", priority = 503)]
		private static void	SearchComponent(MenuCommand menuCommand)
		{
			Utility.OpenWindow<NGAssetFinderWindow>(NGAssetFinderWindow.Title, true, window =>
			{
				window.AssignTargetAndLoadSubAssets(menuCommand.context);
				window.currentResult = -1;

				if (PrefabUtility.GetPrefabType(menuCommand.context) == PrefabType.Prefab)
				{
					window.searchOptions &= ~SearchOptions.InCurrentScene;
					window.searchOptions |= SearchOptions.InProject;
				}
				else
				{
					window.searchOptions &= ~SearchOptions.InProject;
					window.searchOptions |= SearchOptions.InCurrentScene;
				}
			});
		}

		[MenuItem("GameObject/Find all references", priority = 12)]
		private static void	SearchGameObject(MenuCommand menuCommand)
		{
			Utility.OpenWindow<NGAssetFinderWindow>(NGAssetFinderWindow.Title, true, window =>
			{
				window.AssignTargetAndLoadSubAssets(menuCommand.context);
				window.currentResult = -1;
				window.searchOptions &= ~SearchOptions.InProject;
				window.searchOptions |= SearchOptions.InCurrentScene;
			});
		}

		[MenuItem("Assets/Find all references")]
		private static void	SearchAsset(MenuCommand menuCommand)
		{
			Utility.OpenWindow<NGAssetFinderWindow>(NGAssetFinderWindow.Title, true, window =>
			{
				window.AssignTargetAndLoadSubAssets(Selection.activeObject);
				window.currentResult = -1;
				window.searchOptions &= ~SearchOptions.InCurrentScene;
				window.searchOptions |= SearchOptions.InProject;
			});
		}
		#endregion

		protected virtual void	OnEnable()
		{
			Metrics.UseTool(7); // NGAssetFinder

			NGChangeLogWindow.CheckLatestVersion(NGAssemblyInfo.Name);

			Utility.LoadEditorPref(this, NGEditorPrefs.GetPerProjectPrefix());
			Utility.RestoreIcon(this, NGAssetFinderWindow.TitleColor);

			this.typeExclusions = Utility.CreateNGTInstancesOf<TypeMembersExclusion>();

			if (this.targetAsset != null && this.mainAsset == null)
				this.AssignTargetAndLoadSubAssets(this.targetAsset);

			this.assetFinder = new AssetFinder(this);

			this.headerSceneContent.image = UtilityResources.UnityIcon;
			this.headerProjectContent.image = UtilityResources.FolderIcon;

			this.assetContent.image = UtilityResources.AssetIcon;
			this.prefabContent.image = UtilityResources.PrefabIcon;
			this.sceneContent.image = UtilityResources.UnityIcon;

			this.typeFinders = new DynamicOrderedArray<TypeFinder>(Utility.CreateNGTInstancesOf<TypeFinder>(this));
			this.objectFinders = new DynamicOrderedArray<ObjectFinder>(Utility.CreateNGTInstancesOf<ObjectFinder>(this));

			this.resultsScrollbar = new HorizontalScrollbar(0F, 0F, this.position.width);
			this.resultsScrollbar.interceiptEvent = false;
			this.resultsScrollbar.hasCustomArea = true;

			NGEditorApplication.ChangeScene += this.VerifyScenesFilters;
			Selection.selectionChanged += this.Repaint;
			Undo.undoRedoPerformed += this.OnUndo;

			// Fake null Unity Object creates problem when replacing, raising an invalid cast due to the wrong nature of "null".
			if (this.replaceAsset == null && object.Equals(this.replaceAsset, null) == false)
				this.replaceAsset = null;

			this.VerifyScenesFilters();

			this.minSize = new Vector2(450F, 200F);

			this.wantsMouseMove = true;
		}

		protected virtual void	OnDisable()
		{
			Utility.SaveEditorPref(this, NGEditorPrefs.GetPerProjectPrefix());
			NGEditorApplication.ChangeScene -= this.VerifyScenesFilters;
			Selection.selectionChanged -= this.Repaint;
			Undo.undoRedoPerformed -= this.OnUndo;

			AssetFinderCache.SaveCache(Path.Combine(Application.persistentDataPath, Path.Combine(Constants.InternalPackageTitle, AssetFinderCache.CachePath)));
		}

		protected virtual void	OnGUI()
		{
			FreeLicenseOverlay.First(this, NGAssetFinderWindow.Title + " Pro", NGAssetFinderWindow.FreeAdContent);

			Rect	r = this.position;
			r.x = 0F;
			r.y = 0F;

			if (this.errorPopup.exception != null)
			{
				r.height = this.errorPopup.boxHeight;
				this.errorPopup.OnGUIRect(r);
				r.y += r.height;
			}

			r.height = Constants.SingleLineHeight;

			EditorGUI.BeginDisabledGroup(this.isSearching);
			{
				if (this.mainAsset != null && this.mainAsset.children.Count > 0)
				{
					r.width = 20F;
					r.x = NGAssetFinderWindow.TargetReplaceLabelWidth - r.width;
					if (GUI.Button(r, "☰") == true)
						PopupWindow.Show(r, new SubAssetBrowser(this.mainAsset));
				}

				using (LabelWidthRestorer.Get(NGAssetFinderWindow.TargetReplaceLabelWidth))
				{
					r.x = 0F;
					r.width = this.position.width - NGAssetFinderWindow.FindButtonWidth - NGAssetFinderWindow.FindButtonLeftSpacing;
					EditorGUI.BeginChangeCheck();
					Object	newTarget = EditorGUI.ObjectField(r, "Find Asset", this.targetAsset, typeof(Object), true);
					if (EditorGUI.EndChangeCheck() == true)
						this.AssignTargetAndLoadSubAssets(newTarget);
					r.y += r.height + NGAssetFinderWindow.Spacing;
				}

				r.width = NGAssetFinderWindow.TargetReplaceLabelWidth;
				this.canReplace = GUI.Toggle(r, this.canReplace, "Replace Asset");

				r.x += r.width;
				r.width = this.position.width - r.x - NGAssetFinderWindow.SwitchButtonWidth - NGAssetFinderWindow.FindButtonWidth - NGAssetFinderWindow.FindButtonLeftSpacing;
				EditorGUI.BeginDisabledGroup(!this.canReplace);
				{
					bool	allowSceneObjects = true;

					if (this.targetAsset != null)
					{
						PrefabType	targetPrefabType = PrefabUtility.GetPrefabType(this.targetAsset);
						allowSceneObjects = targetPrefabType == PrefabType.None || (targetPrefabType != PrefabType.Prefab && targetPrefabType != PrefabType.ModelPrefab);
					}

					this.replaceAsset = EditorGUI.ObjectField(r, string.Empty, this.replaceAsset, typeof(Object), allowSceneObjects);
				}
				EditorGUI.EndDisabledGroup();

				r.x += r.width;
				r.width = NGAssetFinderWindow.SwitchButtonWidth;
				if (GUI.Button(r, "⇅", GeneralStyles.BigFontToolbarButton) == true)
				{
					Object	tmp = this.targetAsset;
					this.AssignTargetAndLoadSubAssets(this.replaceAsset);
					this.replaceAsset = tmp;
				}

				r.yMin -= r.height + NGAssetFinderWindow.Spacing;
				r.width = NGAssetFinderWindow.FindButtonWidth;
				r.x = this.position.width - NGAssetFinderWindow.FindButtonWidth;

				EditorGUI.BeginDisabledGroup(this.targetAsset == null || this.isSearching == true);
				{
					using (BgColorContentRestorer.Get(GeneralStyles.HighlightActionButton))
					{
						if (GUI.Button(r, "Find") == true)
						    this.FindReferences();
					}
				}
				EditorGUI.EndDisabledGroup();

				r.y += r.height + NGAssetFinderWindow.Spacing + NGAssetFinderWindow.OptionsSpacing;
			}
			EditorGUI.EndDisabledGroup();

			r.x = 0F;
			r.width = (this.position.width - NGAssetFinderWindow.SearchOptionsToggleWidth) * .5F;
			r.height = NGAssetFinderWindow.SearchSceneOrProjectButtonHeight;

			EditorGUI.BeginChangeCheck();
			using (BgColorContentRestorer.Get((this.searchOptions & SearchOptions.InCurrentScene) == 0, NGAssetFinderWindow.DisabledHeaderButton))
				GUI.Button(r, this.headerSceneContent, "ButtonRight");
			if (EditorGUI.EndChangeCheck() == true)
			{
				Undo.RecordObject(this, "Change search option");

				if ((this.searchOptions & SearchOptions.InCurrentScene) != 0)
				{
					this.showTargetOptions = !this.showTargetOptions;
					this.searchOptions &= ~SearchOptions.InProject;
				}
				else
				{
					this.searchOptions &= ~SearchOptions.InProject;
					this.searchOptions |= SearchOptions.InCurrentScene;
				}
			}
			r.x += r.width + NGAssetFinderWindow.SearchOptionsToggleWidth;

			EditorGUI.BeginChangeCheck();
			using (BgColorContentRestorer.Get((this.searchOptions & SearchOptions.InProject) == 0, NGAssetFinderWindow.DisabledHeaderButton))
				GUI.Button(r, this.headerProjectContent, "ButtonLeft");
			if (EditorGUI.EndChangeCheck() == true)
			{
				Undo.RecordObject(this, "Change search option");

				if ((this.searchOptions & SearchOptions.InProject) != 0)
				{
					this.showTargetOptions = !this.showTargetOptions;
					this.searchOptions &= ~SearchOptions.InCurrentScene;
				}
				else
				{
					this.searchOptions &= ~SearchOptions.InCurrentScene;
					this.searchOptions |= SearchOptions.InProject;
				}
			}

			r.x -= NGAssetFinderWindow.SearchOptionsToggleWidth;
			r.width = NGAssetFinderWindow.SearchOptionsToggleWidth;

			if (GUI.Button(r, this.showTargetOptions ? "▼" : "▲", GeneralStyles.BigCenterText) == true)
			{
				this.showTargetOptions = !this.showTargetOptions;
				Event.current.Use();
			}
			r.y += r.height + NGAssetFinderWindow.OptionsSpacing;

			if (this.showTargetOptions == true)
			{
				Object[]	draggedObjects = DragAndDrop.objectReferences;

				dragRect = r;
				dragRect.x = 0F;
				dragRect.width = this.position.width;

				if ((this.searchOptions & SearchOptions.InCurrentScene) != 0)
				{
					r.x = 0F;
					r.width = this.position.width;
					r.height = NGAssetFinderWindow.SearchOptionsBarHeight;
					r = this.DrawPrefabOptions(r, false);

					bool	displayDragTip = true;

					for (int i = 0; i < this.sceneFilters.Count; i++)
					{
						if (this.sceneFilters[i].filters.Count > 0)
						{
							displayDragTip = false;
							break;
						}
					}

					for (int k = 0; k < EditorSceneManager.sceneCount; k++)
					{
						Scene					scene = EditorSceneManager.GetSceneAt(k);
						string					guid = AssetDatabase.AssetPathToGUID(scene.path);
						SceneGameObjectFilters	sceneFilters = this.sceneFilters.Find(s => s.sceneGUID == guid);

						if (sceneFilters == null)
						{
							sceneFilters = new SceneGameObjectFilters() { sceneGUID = guid };
							this.sceneFilters.Add(sceneFilters);
						}

						r.x = 0F;
						r.width = this.position.width;
						if (sceneFilters.open == true && sceneFilters.filters.Count > 0)
							r.height = NGAssetFinderWindow.Margin + Constants.SingleLineHeight + sceneFilters.filters.Count * (Constants.SingleLineHeight + NGAssetFinderWindow.Spacing) + NGAssetFinderWindow.Margin + NGAssetFinderWindow.Margin;
						else
							r.height = NGAssetFinderWindow.Margin + Constants.SingleLineHeight + NGAssetFinderWindow.Spacing;
						GUI.Box(r, GUIContent.none, "Button");

						dragRect = r;

						r.y += NGAssetFinderWindow.Margin;

						if (sceneFilters.cachedFiltersLabel == null)
							sceneFilters.UpdateCacheLabel();

						EditorGUI.BeginDisabledGroup(sceneFilters.filters.Count == 0);
						{
							r.x = NGAssetFinderWindow.Margin;
							r.width = this.position.width;
							r.height = Constants.SingleLineHeight;
							sceneFilters.open = EditorGUI.Foldout(r, sceneFilters.open, GUIContent.none, true);
							r.width = this.position.width - r.x;
							r.xMin += 12F;
						}
						EditorGUI.EndDisabledGroup();

						GUI.Label(r, sceneFilters.cachedFiltersLabel);

						if (displayDragTip == true && sceneFilters.cachedFiltersLabel.EndsWith("All") == true)
						{
							r.xMin = r.xMax - 100F;
							GUI.Label(r, "Drag & drop", GeneralStyles.WrapLabel);
							r.x -= 10F;
							Utility.DrawUnfillRect(r, Color.grey);
							displayDragTip = false;
						}

						r.y += r.height + NGAssetFinderWindow.Margin;

						if (sceneFilters.open == true && sceneFilters.filters.Count > 0)
						{
							for (int j = 0; j < sceneFilters.filters.Count; j++)
							{
								FilterGameObject	filter = sceneFilters.filters[j];

								r.x = NGAssetFinderWindow.Margin;
								r.width = NGAssetFinderWindow.FilterToggleWidth;
								EditorGUI.BeginChangeCheck();
								filter.active = EditorGUI.Toggle(r, filter.active);
								if (EditorGUI.EndChangeCheck() == true)
								{
									Undo.RecordObject(this, "Toggle GameObject");
									sceneFilters.cachedFiltersLabel = null;
								}
								r.x += r.width;

								using (BgColorContentRestorer.Get(filter.GameObject == null || filter.active == true, filter.GameObject == null ? NGAssetFinderWindow.MissingFilterRefColor : NGAssetFinderWindow.ValidFilterRefColor))
								{
									Type	t = typeof(GameObject);

									if (draggedObjects.Length > 0 &&
										this.CanDropSceneDrag(sceneFilters, draggedObjects) != DropState.Allowed)
									{
										t = typeof(void);
									}

									r.width = this.position.width - NGAssetFinderWindow.FilterToggleWidth - NGAssetFinderWindow.FilterInclusiveWidth - NGAssetFinderWindow.DeleteButtonWidth - NGAssetFinderWindow.Margin - NGAssetFinderWindow.Margin;
									EditorGUI.BeginChangeCheck();
									GameObject	asset = EditorGUI.ObjectField(r, filter.GameObject, t, true) as GameObject;
									if (EditorGUI.EndChangeCheck() == true)
									{
										if (sceneFilters.sceneGUID == AssetDatabase.AssetPathToGUID(asset.scene.path) &&
											sceneFilters.filters.Exists(f => f.GameObject == asset) == false &&
											AssetDatabase.GetAssetPath(asset) == string.Empty)
										{
											Undo.RecordObject(this, "Change GameObject");
											sceneFilters.cachedFiltersLabel = null;
											filter.GameObject = asset;
										}
									}

									if (filter.GameObject == null)
									{
										r.y += 2F;
										r.height -= 4F;
										r.x += 16F;
										r.width -= 16F + 20F;
										EditorGUI.DrawRect(r, NGAssetFinderWindow.MissingFilterRefColor);

										r.y -= 2F;
										r.height += 4F;
										if (EditorSceneManager.sceneCount > 1)
											GUI.Label(r, Path.GetFileNameWithoutExtension(filter.scenePath) + ":" + filter.hierarchyPath);
										else
											GUI.Label(r, filter.hierarchyPath);
										r.x += 20F;
									}
									r.x += r.width;
								}

								EditorGUI.BeginChangeCheck();
								r.width = NGAssetFinderWindow.FilterInclusiveWidth;
								GUI.Toggle(r, filter.type == Filter.Type.Inclusive, filter.type == Filter.Type.Inclusive ? "Inclusive" : "Exclusive", GeneralStyles.ToolbarToggle);
								if (EditorGUI.EndChangeCheck() == true)
								{
									filter.type = (Filter.Type)(((int)filter.type + 1) & 1);
									sceneFilters.cachedFiltersLabel = null;
								}
								r.x += r.width;

								r.width = NGAssetFinderWindow.DeleteButtonWidth;
								if (GUI.Button(r, "X", GeneralStyles.ToolbarCloseButton) == true)
								{
									Undo.RecordObject(this, "Delete GameObject");
									sceneFilters.cachedFiltersLabel = null;
									sceneFilters.filters.RemoveAt(j);
									return;
								}

								r.y += r.height + NGAssetFinderWindow.Spacing;
							}

							r.y += NGAssetFinderWindow.Margin + NGAssetFinderWindow.Margin;
						}

						if (draggedObjects.Length > 0)
						{
							dragRect.height = 24F;

							if (Event.current.type == EventType.DragUpdated &&
								dragRect.Contains(Event.current.mousePosition) == true)
							{
								if (this.CanDropSceneDrag(sceneFilters, draggedObjects) == DropState.Allowed)
									DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
								else
									DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
							}
							else if (Event.current.type == EventType.DragPerform &&
									 dragRect.Contains(Event.current.mousePosition) == true)
							{
								DragAndDrop.AcceptDrag();

								Undo.RecordObject(this, "Drop GameObject");

								for (int j = 0; j < draggedObjects.Length; j++)
								{
									GameObject	gameObject = draggedObjects[j] as GameObject;
									if (gameObject == null)
									{
										Component	component = draggedObjects[j] as Component;
										if (component != null)
											gameObject = component.gameObject;
									}

									if (gameObject != null &&
										sceneFilters.filters.Exists(f => f.GameObject == gameObject as GameObject) == false)
									{
										sceneFilters.filters.Add(new FilterGameObject() { active = true, GameObject = gameObject });
									}
								}

								this.showTargetOptions = true;
								sceneFilters.open = true;
								sceneFilters.cachedFiltersLabel = null;

								DragAndDrop.PrepareStartDrag();
								Event.current.Use();
								this.Repaint();
							}

							if (Event.current.type == EventType.Repaint)
							{
								DropState	state = this.CanDropSceneDrag(sceneFilters, draggedObjects);

								if (state == DropState.Allowed)
									Utility.DropZone(dragRect, "Drop in " + scene.name);
								else if (state == DropState.Already)
									Utility.DropZone(dragRect, "GameObject already in filters", Color.red);
								else if (state == DropState.DifferentScene)
									Utility.DropZone(dragRect, "Different scene", Color.red);
								else if (state == DropState.Rejected)
									Utility.DropZone(dragRect, "Must contain GameObject from Hierarchy", Color.red);

								this.Repaint();
							}
						}
					}
				}

				if ((this.searchOptions & SearchOptions.InProject) != 0)
				{
					r.x = 0F;
					r.width = this.position.width;
					if (this.projectFilters.open == true && this.projectFilters.filters.Count > 0)
						r.height = NGAssetFinderWindow.Margin + NGAssetFinderWindow.SelectorHeight + NGAssetFinderWindow.Spacing + Constants.SingleLineHeight + this.projectFilters.filters.Count * (Constants.SingleLineHeight + NGAssetFinderWindow.Spacing) + NGAssetFinderWindow.Margin + NGAssetFinderWindow.Margin;
					else
						r.height = NGAssetFinderWindow.Margin + NGAssetFinderWindow.SelectorHeight + NGAssetFinderWindow.Spacing + Constants.SingleLineHeight + NGAssetFinderWindow.Spacing;
					GUI.Box(r, GUIContent.none, "Button");

					dragRect = r;

					r.x = NGAssetFinderWindow.Margin;
					r.y += NGAssetFinderWindow.Margin;
					r.width = NGAssetFinderWindow.SearchSelectorWidth - NGAssetFinderWindow.Margin - NGAssetFinderWindow.Margin;
					r.height = NGAssetFinderWindow.SelectorHeight;
					GUI.Label(r, "Selector :");
					r.x += r.width;

					r.width = (this.position.width - r.x - NGAssetFinderWindow.Margin)  / 3F;
					Rect	dropdownRect = r;

					dropdownRect.xMin += 5F;
					dropdownRect.width = 20F;
					dropdownRect.y -= 1F;
					if (Event.current.type == EventType.MouseDown &&
						dropdownRect.Contains(Event.current.mousePosition) == true)
					{
						PopupWindow.Show(r, new AssetsExtensionsPopup(this));
						Event.current.Use();
					}

					if (string.IsNullOrEmpty(this.assetContent.text) == true)
					{
						int	totalActiveExtensions = 0;

						for (int j = 0; j < NGAssetFinderWindow.AssetsExtensions.Length; j++)
						{
							if ((this.searchExtensionsMask & (1 << j)) != 0)
								++totalActiveExtensions;
						}

						if (totalActiveExtensions != NGAssetFinderWindow.AssetsExtensions.Length)
							this.assetContent.text = "Asset (" + totalActiveExtensions + "/" + NGAssetFinderWindow.AssetsExtensions.Length + ")";
						else
							this.assetContent.text = "Asset";
						this.Repaint();
					}

					Rect	r2 = r;
					EditorGUI.BeginChangeCheck();
					GUI.Toggle(r2, (this.searchAssets & SearchAssets.Asset) != 0, GUIContent.none, GeneralStyles.ToolbarToggle);
					if (EditorGUI.EndChangeCheck() == true)
					{
						Undo.RecordObject(this, "Toggle search option");
						this.searchAssets ^= SearchAssets.Asset;
					}
					XGUIHighlightManager.DrawHighlight(NGAssetFinderWindow.Title + ".Asset", this, r2);

					if (this.assetContent.text != "Asset")
					{
						r2.width = r2.height;
						r2.x += dropdownRect.width + 5F + NGAssetFinderWindow.Spacing;
						GUI.DrawTexture(r2, this.assetContent.image, ScaleMode.ScaleToFit);
						r2.x += r2.width;
						r2.width = r.width;
						r2.y -= 4F;
						GUI.Label(r2, this.assetContent.text);
						r2.y += 13F;

						int	totalActiveExtensions = 0;

						for (int j = 0; j < NGAssetFinderWindow.AssetsExtensions.Length; j++)
						{
							if ((this.searchExtensionsMask & (1 << j)) != 0)
								++totalActiveExtensions;
						}

						r2.width = Mathf.Min(8F, (r.width - (dropdownRect.width + 5F + NGAssetFinderWindow.Spacing + r2.height)) / totalActiveExtensions);
						r2.height = r2.width;
						for (int j = 0; j < NGAssetFinderWindow.AssetsExtensions.Length; j++)
						{
							if ((this.searchExtensionsMask & (1 << j)) != 0)
							{
								if (NGAssetFinderWindow.AssetsExtensions[j].Icon != null)
								{
									GUI.DrawTexture(r2, NGAssetFinderWindow.AssetsExtensions[j].Icon);
									r2.x += r2.width;
								}
							}
						}
					}
					else
						GUI.Label(r2, this.assetContent, GeneralStyles.CenterText);
					r.x += r.width;

					GUI.Button(dropdownRect, string.Empty, "DropDown");

					dropdownRect = r;
					dropdownRect.xMin += 5F;
					dropdownRect.width = 20F;
					dropdownRect.y -= 1F;
					if (Event.current.type == EventType.MouseDown &&
						dropdownRect.Contains(Event.current.mousePosition) == true)
					{
						dropdownRect.x = 0F;
						PopupWindow.Show(dropdownRect, new SearchOptionsPopup(this));
						Event.current.Use();
					}

					r2 = r;
					EditorGUI.BeginChangeCheck();
					GUI.Toggle(r2, (this.searchAssets & SearchAssets.Prefab) != 0, GUIContent.none, GeneralStyles.ToolbarToggle);
					if (EditorGUI.EndChangeCheck() == true)
					{
						Undo.RecordObject(this, "Toggle search option");
						this.searchAssets ^= SearchAssets.Prefab;
					}
					XGUIHighlightManager.DrawHighlight(NGAssetFinderWindow.Title + ".Prefab", this, r2);

					r2.width = r2.height;
					r2.x += dropdownRect.width + 5F + NGAssetFinderWindow.Spacing;
					GUI.DrawTexture(r2, this.prefabContent.image, ScaleMode.ScaleToFit);
					r2.x += r2.width;
					r2.width = r.width;
					r2.y -= 4F;
					GUI.Label(r2, this.prefabContent.text);
					r2.y += 10F;

					r2.height = 14F;
					GUI.Label(r2, "F: "
						+ (((this.searchOptions & SearchOptions.ByInstance) != 0) ? "I," : string.Empty)
						+ (((this.searchOptions & SearchOptions.ByComponentType) != 0) ? "CT," : string.Empty)
						+ (((this.searchOptions & SearchOptions.SerializeField) != 0) ? "SF," : string.Empty)
						+ (((this.searchOptions & SearchOptions.NonPublic) != 0) ? "HF," : string.Empty)
						+ (((this.searchOptions & SearchOptions.Property) != 0) ? "P," : string.Empty)
						+ (this.debugAnalyzedTypes == true ? "DBG" : string.Empty), GeneralStyles.SmallLabel);
					r.x += r.width;

					GUI.Button(dropdownRect, string.Empty, "DropDown");
					XGUIHighlightManager.DrawHighlight(NGAssetFinderWindow.Title + ".ByInstance", this, dropdownRect, false);
					XGUIHighlightManager.DrawHighlight(NGAssetFinderWindow.Title + ".ByComponentType", this, dropdownRect, false);


					if (GUI.enabled == true)
					{
						EditorGUI.BeginDisabledGroup(EditorSettings.serializationMode != SerializationMode.ForceText || (this.targetAsset != null && AssetDatabase.GetAssetPath(this.targetAsset) == string.Empty && (this.targetAsset is MonoBehaviour) == false));
						{
							if (GUI.enabled == false)
							{
								Utility.content.tooltip = string.Empty;
								if (EditorSettings.serializationMode != SerializationMode.ForceText)
									Utility.content.tooltip = "SerializationMode must be set on ForceText to enable this option.\n";
								if (AssetDatabase.GetAssetPath(this.targetAsset) == string.Empty)
									Utility.content.tooltip += "Not an asset, but might be a scene asset.";
								GUI.Toggle(r, false, this.sceneContent, GeneralStyles.ToolbarToggle);
								Utility.content.tooltip = string.Empty;
							}
							else
							{
								EditorGUI.BeginChangeCheck();
								GUI.Toggle(r, (this.searchAssets & SearchAssets.Scene) != 0, this.sceneContent, GeneralStyles.ToolbarToggle);
								if (EditorGUI.EndChangeCheck() == true)
								{
									Undo.RecordObject(this, "Toggle search option");
									this.searchAssets ^= SearchAssets.Scene;
								}
								XGUIHighlightManager.DrawHighlight(NGAssetFinderWindow.Title + ".Scene", this, r);
							}
						}
						EditorGUI.EndDisabledGroup();
					}
					else
					{
						if (EditorSettings.serializationMode != SerializationMode.ForceText)
							GUI.Toggle(r, false, Utility.content, GeneralStyles.ToolbarToggle);
						else
							GUI.Toggle(r, (this.searchAssets & SearchAssets.Scene) != 0, this.sceneContent, GeneralStyles.ToolbarToggle);
					}
					r.y += r.height + NGAssetFinderWindow.Spacing;

					r.xMin = r.xMax - NGAssetFinderWindow.UseCacheButtonWidth;
					r.x -= NGAssetFinderWindow.Spacing + 20F;
					r.height = Constants.SingleLineHeight;
					EditorGUI.BeginChangeCheck();
					r.width -= 16F;
					GUI.Button(r, GUIContent.none, "ButtonLeft");
					if (EditorGUI.EndChangeCheck() == true)
					{
						if (NGLicensesManager.IsPro(NGAssemblyInfo.Name + " Pro") == false)
						{
							EditorUtility.DisplayDialog(NGAssemblyInfo.Name + " Pro", "Using the cache reduces the search time to a second, it is reserved to NG Tools Pro or " + NGAssemblyInfo.Name + " Pro.", "OK");
							this.useCache = false;
						}
						else
							this.useCache = !this.useCache;
					}

					r.x += 1F;
					GUI.Toggle(r, this.useCache, "Use Cache");
					r.x += r.width - 1F;

					r.width = 16F;
					if (GUI.Button(r, "▾", "ButtonRight") == true)
					{
						GenericMenu	menu = new GenericMenu();
						string		path = AssetFinderCache.GetCachePath();
						string		cacheFileSize;

						if (File.Exists(path) == true)
						{
							long	size = new FileInfo(path).Length;

							if (size >= 1024L * 1024L)
								cacheFileSize = ((float)(size / (1024F * 1024F))).ToString("N2") + " MiB";
							else if (size >= 1024L)
								cacheFileSize = ((float)(size / 1024F)).ToString("N2") + " KiB";
							else
								cacheFileSize = size + " B";
						}
						else
						{
							if (AssetFinderCache.usages != null)
								cacheFileSize = "In memory";
							else
								cacheFileSize = "Empty";
						}

						menu.AddDisabledItem(new GUIContent("Cache Size: " + cacheFileSize));
						menu.AddSeparator(string.Empty);
						if (cacheFileSize != "Empty")
						{
							GenericMenu.MenuFunction	func = null;

							if (File.Exists(AssetFinderCache.GetCachePath()) == true)
								func = new GenericMenu.MenuFunction(() => EditorUtility.RevealInFinder(AssetFinderCache.GetCachePath()));

							menu.AddItem(new GUIContent("Open"), false, func);
							menu.AddItem(new GUIContent("Clear"), false, AssetFinderCache.ClearCache);
						}
						else
						{
							menu.AddDisabledItem(new GUIContent("Open"));
							menu.AddDisabledItem(new GUIContent("Clear"));
						}

						menu.DropDown(r);
					}
					r.x += r.width + NGAssetFinderWindow.Spacing;

					r.width = 20F;
					Utility.DrawUnfillRect(r, Color.gray);
					GUI.Label(r, " ?");
					if (Event.current.type == EventType.Repaint && r.Contains(Event.current.mousePosition) == true)
					{
						Utility.content.text = "Filters use StartsWith comparison to discard paths.";
						r.width = GeneralStyles.SmallLabel.CalcSize(Utility.content).x;
						r.x = this.position.width - r.width;
						r.y -= r.height;
						EditorGUI.DrawRect(r, Color.gray);
						GUI.Label(r, Utility.content, GeneralStyles.SmallLabel);
						r.y += r.height;
						this.Repaint();
					}

					if (this.projectFilters.cachedFiltersLabel == null)
						this.projectFilters.UpdateCacheLabel();

					EditorGUI.BeginDisabledGroup(this.projectFilters.filters.Count == 0);
					{
						r.x = NGAssetFinderWindow.Margin;
						r.width = this.position.width - NGAssetFinderWindow.UseCacheButtonWidth - NGAssetFinderWindow.Spacing - 20F;
						this.projectFilters.open = EditorGUI.Foldout(r, this.projectFilters.open, GUIContent.none, true);
						r.x += 12F;
						r.width -= NGAssetFinderWindow.Margin + 14F;
					}
					EditorGUI.EndDisabledGroup();

					GUI.Label(r, this.projectFilters.cachedFiltersLabel);

					if (this.projectFilters.filters.Count == 0)
					{
						r.xMin = r.xMax - 100F;
						GUI.Label(r, "Drag & drop", GeneralStyles.WrapLabel);
						r.x -= 10F;
						Utility.DrawUnfillRect(r, Color.grey);
					}

					r.y += r.height + NGAssetFinderWindow.Margin;

					if (this.projectFilters.open == true && this.projectFilters.filters.Count > 0)
					{
						for (int j = 0; j < this.projectFilters.filters.Count; j++)
						{
							FilterText	filter = this.projectFilters.filters[j];

							EditorGUI.BeginChangeCheck();
							r.x = NGAssetFinderWindow.Margin;
							r.width = NGAssetFinderWindow.FilterToggleWidth;
							GUI.Toggle(r, filter.active, string.Empty);
							if (EditorGUI.EndChangeCheck() == true)
							{
								Undo.RecordObject(this, "Toggle folder");
								filter.active = !filter.active;
								this.projectFilters.cachedFiltersLabel = null;
								GUI.FocusControl(null);
							}
							r.x += r.width;

							EditorGUI.BeginChangeCheck();
							r.width = this.position.width - NGAssetFinderWindow.Margin - NGAssetFinderWindow.FilterToggleWidth - NGAssetFinderWindow.Spacing - NGAssetFinderWindow.FilterInclusiveWidth - NGAssetFinderWindow.DeleteButtonWidth - NGAssetFinderWindow.Margin;
							string	text = EditorGUI.TextField(r, filter.text);
							if (EditorGUI.EndChangeCheck() == true)
							{
								Undo.RecordObject(this, "Toggle folder");
								filter.text = text;
								this.projectFilters.cachedFiltersLabel = null;
							}

							if (filter.active == true)
							{
								r.x -= 1F;
								r.y -= 1F;
								r.width += 2F;
								r.height += 2F;
								Utility.DrawUnfillRect(r, Color.green);
								r.x += 1F;
								r.y += 1F;
								r.width -= 2F;
								r.height -= 2F;
							}

							r.x += r.width + NGAssetFinderWindow.Spacing;

							EditorGUI.BeginChangeCheck();
							r.width = NGAssetFinderWindow.FilterInclusiveWidth;
							GUI.Toggle(r, filter.type == Filter.Type.Inclusive, filter.type == Filter.Type.Inclusive ? "Inclusive" : "Exclusive", GeneralStyles.ToolbarToggle);
							if (EditorGUI.EndChangeCheck() == true)
							{
								filter.type = (Filter.Type)(((int)filter.type + 1) & 1);
								this.projectFilters.cachedFiltersLabel = null;
							}
							r.x += r.width;

							r.width = NGAssetFinderWindow.DeleteButtonWidth;
							if (GUI.Button(r, "X", GeneralStyles.ToolbarCloseButton) == true)
							{
								Undo.RecordObject(this, "Delete folder");
								this.projectFilters.cachedFiltersLabel = null;
								this.projectFilters.filters.RemoveAt(j);
								break;
							}

							r.y += r.height + NGAssetFinderWindow.Spacing;
						}

						r.y += NGAssetFinderWindow.Margin + NGAssetFinderWindow.Margin;
					}

					if (draggedObjects.Length > 0)
					{
						if (Event.current.type == EventType.DragUpdated &&
							dragRect.Contains(Event.current.mousePosition) == true)
						{
							if (this.CanDropProjectDrag(this.projectFilters, draggedObjects) == DropState.Allowed)
								DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
							else
								DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
						}
						else if (Event.current.type == EventType.DragPerform &&
								 dragRect.Contains(Event.current.mousePosition) == true)
						{
							DragAndDrop.AcceptDrag();

							Undo.RecordObject(this, "Drop folder");

							for (int j = 0; j < draggedObjects.Length; j++)
							{
								string	path = AssetDatabase.GetAssetPath(draggedObjects[j]);

								if (Directory.Exists(path) == false)
									path = Path.GetDirectoryName(path);

								if (this.projectFilters.filters.Exists((e) => e.text == path) == false)
									this.projectFilters.filters.Add(new FilterText() { active = true, text = path });
							}

							this.showTargetOptions = true;
							this.projectFilters.open = true;
							this.projectFilters.cachedFiltersLabel = null;

							DragAndDrop.PrepareStartDrag();
							Event.current.Use();
							this.Repaint();
						}

						if (Event.current.type == EventType.Repaint)
						{
							DropState	state = this.CanDropProjectDrag(this.projectFilters, draggedObjects);

							if (state == DropState.Allowed)
								Utility.DropZone(dragRect, "Drop Folder");
							else if (state == DropState.Already)
								Utility.DropZone(dragRect, "Folder already in filters", Color.red);
							else
								Utility.DropZone(dragRect, "Must contain an asset from Project", Color.red);

							this.Repaint();
						}
					}
				}

				r.y += NGAssetFinderWindow.Margin;
			}

			if (this.results.Count > 1)
			{
				float	totalWidth = NGAssetFinderWindow.Spacing + NGAssetFinderWindow.Spacing;

				for (int i = 0; i < this.results.Count; i++)
					totalWidth += NGAssetFinderWindow.Margin + Constants.SingleLineHeight + this.results[i].buttonWidth + NGAssetFinderWindow.Spacing;

				this.resultsScrollbar.allowedMouseArea.y = r.y + resultsScrollbar.MaxHeight;
				this.resultsScrollbar.allowedMouseArea.width = this.position.width;
				this.resultsScrollbar.allowedMouseArea.height = 24F;
				this.resultsScrollbar.SetPosition(0F, r.y);
				this.resultsScrollbar.SetSize(this.position.width);
				this.resultsScrollbar.RealWidth = totalWidth;
				this.resultsScrollbar.OnGUI();

				r.y += this.resultsScrollbar.MaxHeight;

				r.x = NGAssetFinderWindow.Spacing - this.resultsScrollbar.Offset;
				r.height = NGAssetFinderWindow.ResultsScrollHeight;

				for (int i = 0; i < this.results.Count; i++)
				{
					r.width = NGAssetFinderWindow.Margin + Constants.SingleLineHeight + this.results[i].buttonWidth + NGAssetFinderWindow.Spacing;
					if (GUI.Button(r, GUIContent.none) == true)
					{
						if (Event.current.button != 2)
							this.SelectResult(i);
						else
						{
							this.ClearResult(i);
							return;
						}
					}

					r.x += NGAssetFinderWindow.Margin;

					r.width = Constants.SingleLineHeight;
					if (this.results[i].targetAssetIcon != null)
						GUI.DrawTexture(r, this.results[i].targetAssetIcon, ScaleMode.ScaleToFit);

					r.width = 12F;
					r.height = 12F;
					r.y -= 5F;
					r.x -= 5F;
					if ((this.results[i].searchOptions & SearchOptions.InCurrentScene) != 0)
						GUI.DrawTexture(r, this.headerSceneContent.image, ScaleMode.ScaleToFit);
					else
						GUI.DrawTexture(r, this.headerProjectContent.image, ScaleMode.ScaleToFit);
					r.y += 5F;
					r.x += 5F;

					r.height = NGAssetFinderWindow.ResultsScrollHeight;
					r.x += Constants.SingleLineHeight;

					if (i == this.currentResult)
						GeneralStyles.VerticalCenterLabel.normal.textColor = NGAssetFinderWindow.SelectedResult;

					r.width = this.results[i].buttonWidth;
					r.y -= 5F;
					GUI.Label(r, this.results[i].targetAssetName, GeneralStyles.VerticalCenterLabel);
					r.y += 12F;
					GUI.Label(r, this.results[i].matchesCount, GeneralStyles.SmallLabel);
					r.y -= 12F - 5F;

					GeneralStyles.VerticalCenterLabel.normal.textColor = EditorStyles.label.normal.textColor;

					r.x += r.width + NGAssetFinderWindow.Spacing;
				}

				r.y += r.height + NGAssetFinderWindow.Margin;
			}

			r.height = Constants.SingleLineHeight;

			if (this.currentResult >= 0 && this.currentResult < this.results.Count)
			{
				r.x = 0F;
				r.width = this.position.width;
				GUI.Box(r, GUIContent.none, GeneralStyles.Toolbar);
				{
					r.x = Margin;
					GUI.Label(r, "Result");

					r.x = -Margin + this.position.width - NGAssetFinderWindow.ExportButtonWidth - NGAssetFinderWindow.ClearButtonWidth;
					using (BgColorContentRestorer.Get(GeneralStyles.HighlightActionButton))
					{
						r.width = NGAssetFinderWindow.ExportButtonWidth;
						if (GUI.Button(r, "Export", GeneralStyles.ToolbarButton) == true)
						{
							this.ExportResults();
							return;
						}
						r.x += r.width + Spacing;
					}

					using (BgColorContentRestorer.Get(GeneralStyles.HighlightResultButton))
					{
						r.width = NGAssetFinderWindow.ClearButtonWidth;
						if (GUI.Button(r, "Clear", GeneralStyles.ToolbarButton) == true)
						{
							this.ClearResult(this.currentResult);
							return;
						}
						r.y += r.height + Spacing;
					}
				}

				r.x = 0F;
				r.width = this.position.width;
				r.height = this.position.height - r.y;
				this.results[this.currentResult].Draw(r);
			}

			FreeLicenseOverlay.Last(NGAssetFinderWindow.Title + " Pro");
		}

		private Rect	DrawPrefabOptions(Rect r, bool fromUtility)
		{
			GUI.Box(r, GUIContent.none, GeneralStyles.Toolbar);

			r.x = NGAssetFinderWindow.Margin;
			r.width = NGAssetFinderWindow.SearchHeaderWidth;
			r.y += 1F;
			GUI.Label(r, "Filters :");
			r.y -= 1F;
			r.x += r.width;

			if (this.targetAsset is Component || this.targetAsset is MonoScript)
			{
				bool	blockSceneAsset = (this.searchOptions & SearchOptions.InProject) != 0 && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(this.targetAsset));

				EditorGUI.BeginDisabledGroup(blockSceneAsset);
				{
					EditorGUI.BeginChangeCheck();
					r.width = GeneralStyles.ToolbarToggle.CalcSize(this.instanceContent).x;

					string	restoreTooltip = this.instanceContent.tooltip;

					if (blockSceneAsset == true)
						this.instanceContent.tooltip += "\n\n/!\\ You must look into the scene, not the project.";

					GUI.Toggle(r, (this.searchOptions & SearchOptions.ByInstance) != 0, this.instanceContent, GeneralStyles.ToolbarToggle);
					if (EditorGUI.EndChangeCheck() == true)
					{
						Undo.RecordObject(this, "Change search option");
						this.searchOptions ^= SearchOptions.ByInstance;
					}
					XGUIHighlightManager.DrawHighlight(NGAssetFinderWindow.Title + ".ByInstance", this, r, XGUIHighlightManager.Highlights.Glow | (fromUtility == false ? XGUIHighlightManager.Highlights.Wave : 0));

					r.x += r.width;
					this.instanceContent.tooltip = restoreTooltip;
				}
				EditorGUI.EndDisabledGroup();

				EditorGUI.BeginChangeCheck();
				r.width = GeneralStyles.ToolbarToggle.CalcSize(this.componentTypeContent).x;
				GUI.Toggle(r, (this.searchOptions & SearchOptions.ByComponentType) != 0, this.componentTypeContent, GeneralStyles.ToolbarToggle);
				if (EditorGUI.EndChangeCheck() == true)
				{
					Undo.RecordObject(this, "Change search option");
					this.searchOptions ^= SearchOptions.ByComponentType;
				}
				XGUIHighlightManager.DrawHighlight(NGAssetFinderWindow.Title + ".ByComponentType", this, r, XGUIHighlightManager.Highlights.Glow | (fromUtility == false ? XGUIHighlightManager.Highlights.Wave : 0));
				r.x += r.width;
			}

			EditorGUI.BeginChangeCheck();
			r.width = GeneralStyles.ToolbarToggle.CalcSize(this.serializeFieldContent).x;
			GUI.Toggle(r, (this.searchOptions & SearchOptions.SerializeField) != 0, this.serializeFieldContent, GeneralStyles.ToolbarToggle);
			if (EditorGUI.EndChangeCheck() == true)
			{
				Undo.RecordObject(this, "Change search option");
				this.searchOptions ^= SearchOptions.SerializeField;
			}
			r.x += r.width;

			EditorGUI.BeginChangeCheck();
			r.width = GeneralStyles.ToolbarToggle.CalcSize(this.nonPublicContent).x;
			GUI.Toggle(r, (this.searchOptions & SearchOptions.NonPublic) != 0, this.nonPublicContent, GeneralStyles.ToolbarToggle);
			if (EditorGUI.EndChangeCheck() == true)
			{
				Undo.RecordObject(this, "Change search option");
				this.searchOptions ^= SearchOptions.NonPublic;
			}
			r.x += r.width;

			EditorGUI.BeginChangeCheck();
			r.width = GeneralStyles.ToolbarToggle.CalcSize(this.propertyContent).x;
			GUI.Toggle(r, (this.searchOptions & SearchOptions.Property) != 0, this.propertyContent, GeneralStyles.ToolbarToggle);
			if (EditorGUI.EndChangeCheck() == true)
			{
				Undo.RecordObject(this, "Change search option");
				this.searchOptions ^= SearchOptions.Property;
			}
			r.x += r.width;

			if (Conf.DebugMode == Conf.DebugState.Verbose)
			{
				r.width = 70F;
				this.debugAnalyzedTypes = GUI.Toggle(r, this.debugAnalyzedTypes, "DBG Types", GeneralStyles.ToolbarToggle);
			}

			r.y += r.height + NGAssetFinderWindow.Spacing;

			return r;
		}

		private void	SelectResult(int i)
		{
			this.currentResult = i;
			this.targetAsset = this.results[i].targetAsset;
			this.replaceAsset = this.results[i].replaceAsset;
			this.searchOptions = this.results[i].searchOptions;
			this.searchAssets = this.results[i].searchAssets;
			this.searchExtensionsMask = this.results[i].searchExtensionsMask;
			this.useCache = this.results[i].useCache;
		}

		private void	AssignTargetAndLoadSubAssets(Object target)
		{
			this.mainAsset = null;
			this.targetAsset = target;

			if ((this.targetAsset is Component) == false && (this.targetAsset is MonoScript) == false)
			{
				this.searchOptions &= ~SearchOptions.ByComponentType;
				this.searchOptions |= SearchOptions.ByInstance;
			}

			if (this.targetAsset != null)
			{
				string	assetPath = AssetDatabase.GetAssetPath(this.targetAsset);

				if (string.IsNullOrEmpty(assetPath) == false && assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) == false)
				{
					Object[]	allAssets;

					if (assetPath == "Resources/unity_builtin_extra")
					{
						allAssets = new Object[] { target };
						this.mainAsset = new Asset(this, this.targetAsset);
					}
					else
						allAssets = Utility.SafeLoadAllAssetsAtPath(assetPath);

					List<Asset>	pendingList = new List<Asset>(allAssets.Length);

					// Pre-create and get the main Asset.
					for (int i = 0; i < allAssets.Length; i++)
					{
						pendingList.Add(new Asset(this, allAssets[i]));

						// Create Main Asset.
						if (AssetDatabase.IsMainAsset(allAssets[i]) == true)
							this.mainAsset = pendingList[i];
					}

					// In the case of a DLL, LoadAllAssetsAtPath fetches all assets except the main one.
					if (this.mainAsset == null)
					{
						Object	anyAsset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(this.targetAsset), typeof(Object));

						if (anyAsset != null)
							this.mainAsset = new Asset(this, anyAsset);
						// In a very rare case, targetAsset might be destroyed during initialization.
						else
							return;
					}

					for (int i = 0; i < pendingList.Count; i++)
					{
						if (pendingList[i] == this.mainAsset)
							continue;

						if (AssetDatabase.IsSubAsset(allAssets[i]) == true)
						{
							this.mainAsset.children.Add(pendingList[i]);
							continue;
						}

						Asset	current = pendingList[i];
						{
							Component	component = current.asset as Component;

							if (component != null)
							{
								for (int j = 0; j < pendingList.Count; j++)
								{
									if (pendingList[j].asset == component.gameObject)
									{
										pendingList[j].children.Add(pendingList[i]);
										current = null;
										break;
									}
								}

								InternalNGDebug.Assert(current == null, "Component \"" + component + "\" has no Game Object affiliated.", component);
								continue;
							}
						}

						{
							GameObject	gameObject = current.asset as GameObject;

							if (gameObject != null)
							{
								GameObject	parent = gameObject.transform.parent.gameObject;

								for (int j = 0; j < pendingList.Count; j++)
								{
									if (pendingList[j].asset == parent)
									{
										pendingList[j].children.Add(pendingList[i]);
										current = null;
										break;
									}
								}

								InternalNGDebug.Assert(current == null, "Game Object \"" + gameObject + "\" has no parent affiliated.", gameObject);
								continue;
							}
						}
					}
				}
				else
				{
					GameObject	go = this.targetAsset as GameObject;

					if (go != null)
					{
						this.mainAsset = new Asset(this, this.targetAsset);

						Component[]	components = go.GetComponents<Component>();

						for (int i = 0; i < components.Length; i++)
							this.mainAsset.children.Add(new Asset(this, components[i]));
					}
					else
					{
						Component	c = this.targetAsset as Component;

						if (c != null)
						{
							this.mainAsset = new Asset(this, c.gameObject);

							Component[]	components = c.gameObject.GetComponents<Component>();

							for (int i = 0; i < components.Length; i++)
								this.mainAsset.children.Add(new Asset(this, components[i]));
						}
					}
				}

				if (this.replaceAsset != null && this.CheckAssetCompatibility() == false)
					this.replaceAsset = null;
			}
			else
				this.replaceAsset = null;
		}

		private bool	CheckAssetCompatibility()
		{
			if (this.targetAsset == null || this.replaceAsset == null)
				return true;

			Type	targetType = this.targetAsset.GetType();
			Type	replaceType = this.replaceAsset.GetType();

			if ((targetType != replaceType && replaceType.IsSubclassOf(targetType) == false) ||
				PrefabUtility.GetPrefabType(this.targetAsset) != PrefabUtility.GetPrefabType(this.replaceAsset))
			{
				return false;
			}

			return true;
		}

		private bool	CanReplace(AssetMatches assetMatches, Match match, Object asset)
		{
			if (asset is GameObject)
			{
				PrefabType	type = PrefabUtility.GetPrefabType(asset);
				return type != PrefabType.Prefab && type != PrefabType.ModelPrefab && type != PrefabType.None;
			}
			else if (asset is Component)
			{
				PrefabType	type = PrefabUtility.GetPrefabType((asset as Component).gameObject);
				return type != PrefabType.Prefab && type != PrefabType.ModelPrefab && type != PrefabType.None;
			}
			else
			{
				return true;
			}
		}

		private void	ScanAssets()
		{
			try
			{
				this.isSearching = true;
				this.results.Insert(0, this.assetFinder.ScanAssets());
			}
			finally
			{
				this.currentResult = 0;
				this.isSearching = false;
			}
		}

		private void	FindReferences()
		{
			MonoScript		script = this.targetAsset as MonoScript;

			if (script != null)
				this.targetType = script.GetClass();
			else
				this.targetType = this.targetAsset.GetType();

			if (this.targetType == null)
			{
				InternalNGDebug.LogWarning("Search aborted. The given script \"" + script + "\" contains no valid type.");
				return;
			}

			if ((this.searchOptions & (SearchOptions.InProject | SearchOptions.InCurrentScene)) == 0)
			{
				EditorUtility.DisplayDialog(NGAssetFinderWindow.Title, "You must search into the scene or the project.", "OK");
				return;
			}

			if ((this.searchOptions & (SearchOptions.ByInstance | SearchOptions.ByComponentType)) == 0)
			{
				this.showTargetOptions = true;
				XGUIHighlightManager.Highlight(NGAssetFinderWindow.Title + ".ByInstance", NGAssetFinderWindow.Title + ".ByComponentType");
				EditorUtility.DisplayDialog(NGAssetFinderWindow.Title, "You must search by Instance or by Component Type.", "OK");
				return;
			}

			if ((this.searchOptions & SearchOptions.InProject) != 0 && (this.searchAssets & (SearchAssets.Asset | SearchAssets.Prefab | SearchAssets.Scene)) == 0)
			{
				this.showTargetOptions = true;
				XGUIHighlightManager.Highlight(NGAssetFinderWindow.Title + ".Asset", NGAssetFinderWindow.Title + ".Prefab", NGAssetFinderWindow.Title + ".Scene");
				EditorUtility.DisplayDialog(NGAssetFinderWindow.Title, "You must select at least one between Asset, Prefab and Scene.", "OK");
				return;
			}

			// Seeking an instance of Object from a scene in Project, which is impossible.
			if ((this.searchOptions & (SearchOptions.ByInstance | SearchOptions.ByComponentType)) == SearchOptions.ByInstance &&
				(this.searchOptions & (SearchOptions.InProject | SearchOptions.InCurrentScene)) == SearchOptions.InProject &&
				string.IsNullOrEmpty(AssetDatabase.GetAssetPath(this.targetAsset)) == true)
			{
				EditorUtility.DisplayDialog(NGAssetFinderWindow.Title, "You can not search an Asset from the scene in the Project.", "OK");
				return;
			}

			if (EditorApplication.isPlaying == true)
				EditorApplication.isPaused = false;

			this.fieldSearchFlags = (this.searchOptions & (SearchOptions.NonPublic | SearchOptions.SerializeField)) != 0 ? BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance : BindingFlags.Public | BindingFlags.Instance;
			this.propertySearchFlags = (this.searchOptions & SearchOptions.NonPublic) != 0 ? BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance : BindingFlags.Public | BindingFlags.Instance;

			this.ScanAssets();
		}

		private void	ClearResult(int resulIndex)
		{
			this.results.RemoveAt(resulIndex);

			this.currentResult = Mathf.Clamp(this.currentResult, 0, this.results.Count - 1);

			if (this.currentResult >= 0 && this.currentResult < this.results.Count)
				this.SelectResult(this.currentResult);
		}

		private void	ExportResults()
		{
			ViewTextWindow.Start(this.results[this.currentResult].Export());
		}

		private GameObject	FindHierarchy(Scene s, string hierarchy)
		{
			if (s.IsValid() == true)
			{
				GameObject[]	roots = s.GetRootGameObjects();

				for (int k = 0; k < roots.Length; k++)
				{
					if (roots[k].name == hierarchy)
						return roots[k];

					Transform	t = roots[k].transform.Find(hierarchy.Substring(hierarchy.IndexOf('/') + 1));
					if (t != null)
						return t.gameObject;
				}
			}

			return null;
		}

		private DropState	CanDropSceneDrag(SceneGameObjectFilters scene, Object[] draggedObjects)
		{
			int			i = 0;
			DropState	reject = DropState.Rejected;

			for (; i < draggedObjects.Length; i++)
			{
				GameObject	gameObject = draggedObjects[i] as GameObject;
				if (gameObject == null)
				{
					Component	component = draggedObjects[i] as Component;
					if (component != null)
						gameObject = component.gameObject;
				}

				if (gameObject != null && PrefabUtility.GetPrefabType(gameObject) != PrefabType.Prefab)
				{
					string	path = AssetDatabase.GUIDToAssetPath(scene.sceneGUID);

					if (gameObject.scene.path == path)
					{
						if (scene.filters.Exists(f => f.GameObject == gameObject) == false)
							break;

						return DropState.Already;
					}
					else
						reject = DropState.DifferentScene;
				}
			}

			if (i < draggedObjects.Length)
				return DropState.Allowed;
			return reject;
		}

		private DropState	CanDropProjectDrag(SceneProjectFilters scene, Object[] draggedObjects)
		{
			int		i = 0;

			for (; i < draggedObjects.Length; i++)
			{
				string	path = AssetDatabase.GetAssetPath(draggedObjects[i]);

				if (path != string.Empty)
				{
					if (Directory.Exists(path) == false)
						path = Path.GetDirectoryName(path);

					if (scene.filters.Exists((e) => e.text == path) == false)
						break;

					return DropState.Already;
				}
			}

			if (i < draggedObjects.Length)
				return DropState.Allowed;
			return DropState.Rejected;
		}

		private void	OnUndo()
		{
			for (int i = 0; i < this.sceneFilters.Count; i++)
				this.sceneFilters[i].cachedFiltersLabel = null;
			this.projectFilters.cachedFiltersLabel = null;
			this.Repaint();
		}

		private void	VerifyScenesFilters()
		{
			// Remove non-existing scene.
			for (int i = 0; i < this.sceneFilters.Count; i++)
			{
				string	path = AssetDatabase.GUIDToAssetPath(this.sceneFilters[i].sceneGUID);
				if (string.IsNullOrEmpty(path) == true || AssetDatabase.LoadAssetAtPath(path, typeof(Object)) == null)
					this.sceneFilters.RemoveAt(i--);
			}

			for (int k = 0; k < EditorSceneManager.sceneCount; k++)
			{
				Scene	scene = EditorSceneManager.GetSceneAt(k);
				string	guid = AssetDatabase.AssetPathToGUID(scene.path);

				InternalNGDebug.AssertFormat(string.IsNullOrEmpty(guid) == false, "Deserializing scene filters with GUID \"{0}\" at path \"{1}\" failed.", guid, scene.path);

				int	n = this.sceneFilters.FindIndex(s => s.sceneGUID == guid);

				if (n == -1)
					this.sceneFilters.Add(new SceneGameObjectFilters() { sceneGUID = guid });
				else
				{
					for (int i = 0; i < this.sceneFilters[n].filters.Count; i++)
					{
						if (this.sceneFilters[n].filters[i].GameObject != null)
							continue;

						string		hierarchy = this.sceneFilters[n].filters[i].hierarchyPath;
						GameObject	gameObject = GameObject.Find(hierarchy);
						if (gameObject == null)
							gameObject = this.FindHierarchy(scene, hierarchy);

						if (gameObject != null)
							this.sceneFilters[n].filters[i].GameObject = gameObject;
					}
				}
			}

			this.Repaint();
		}

		void	IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			Utility.AddNGMenuItems(menu, this, NGAssetFinderWindow.Title, Constants.WikiBaseURL + NGAssemblyInfo.WikiURL);
		}
	}
}