using NGLicenses;
using NGTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditorInternal;

namespace NGToolsEditor.NGSyncFolders
{
	using UnityEngine;

	[PrewarmEditorWindow]
	public class NGSyncFoldersWindow : EditorWindow, IHasCustomMenu
	{
		private enum ButtonMode
		{
			Scan,
			//ScanAndWatch
		}

		public const string	Title = "NG Sync Folders";
		public static Color	TitleColor = Color.red;
		public const string	CachedHashedFile = "NGSyncFolders/{0}.txt";
		public const float	Spacing = 4F;
		public const float	FilterHeight = 20F;
		public const float	SlaveHeight = 38F;
		public const float	ToggleSlaveWidth = 40F;
		public const float	ToggleFilterWidth = 18F;
		public const float	FilterTypeWidth = 100F;
		public const float	DeleteButtonWidth = 20F;
		public static readonly Color	CreateColor = Color.blue;
		public static readonly Color	DeleteColor = Color.red;
		public static readonly Color	RestoreColor = Color.green;
		public static Color				ActiveFilterOutline { get { return Utility.GetSkinColor(0F, 1F, 1F, 1F, 1F, 1F, 0F, 1F); } }
		public static readonly string[]	CachedSlaveIndexes = new string[] { "#1", "#2", "#3", "#4", "#5", "#6", "#7", "#8", "#9", "#10" };

		private const int				MaxSyncFoldersSlaves = 1;
		private const int				MaxSyncFoldersProfiles = 1;
		private static readonly string	FreeAdContent = NGSyncFoldersWindow.Title + " is restrained to:\n" +
														"• " + NGSyncFoldersWindow.MaxSyncFoldersProfiles + " profiles.\n" +
														"• " + NGSyncFoldersWindow.MaxSyncFoldersSlaves + " slaves.";

		public static float	progress = 0F;
		public static int	slave = 0;

		private static string						cachePath;
		private static Dictionary<string, string[]>	cachedHashes;

		private Profile	profile;
		public int		currentProfile = 0;

		[SerializeField]
		private ButtonMode	mode = ButtonMode.Scan;

		private ReorderableList	slavesList;
		private ReorderableList	filtersList;
		private Vector2			scrollPositionScan;
		private bool			showSlaves;
		private bool			showFilters;
		[NonSerialized]
		private string			cacheFileSize;
		[NonSerialized]
		private GUIContent		cachedSlaves;
		[NonSerialized]
		private string			cachedFiltersFolderLabel;

		//static	NGSyncFoldersWindow()
		//{
		//	HQ.SettingsChanged += Preferences_SettingsChanged;
		//}

		//private static void	Preferences_SettingsChanged()
		//{
		//	if (HQ.Settings != null)
		//	{
		//		SyncFoldersSettings	settings = HQ.Settings.Get<SyncFoldersSettings>();

		//		for (int i = 0; i < settings.syncProfiles.Count; i++)
		//		{
		//			settings.syncProfiles[i].master.WatchFileChanged 
		//		}
		//	}
		//}

		[MenuItem(Constants.MenuItemPath + NGSyncFoldersWindow.Title, priority = Constants.MenuItemPriority + 380), Hotkey(NGSyncFoldersWindow.Title)]
		public static void	Open()
		{
			Utility.OpenWindow<NGSyncFoldersWindow>(NGSyncFoldersWindow.Title);
		}

		public static void	SaveCachedHashes()
		{
			StringBuilder	buffer = Utility.GetBuffer();

			foreach (var pair in NGSyncFoldersWindow.cachedHashes)
			{
				buffer.AppendLine(pair.Key); // File
				buffer.AppendLine(pair.Value[0]); // Last change time
				buffer.AppendLine(pair.Value[1]); // Hash
			}

			Directory.CreateDirectory(Path.GetDirectoryName(NGSyncFoldersWindow.cachePath));
			System.IO.File.WriteAllText(NGSyncFoldersWindow.cachePath, Utility.ReturnBuffer(buffer));
		}

		public static string	TryGetCachedHash(string file)
		{
			if (NGSyncFoldersWindow.cachedHashes == null)
				NGSyncFoldersWindow.LoadHashesCache();

			string[]	values;

			if (NGSyncFoldersWindow.cachedHashes.TryGetValue(file, out values) == true)
			{
				string	currentWriteTime = System.IO.File.GetLastWriteTime(file).Ticks.ToString();

				if (currentWriteTime != values[0])
				{
					values[0] = currentWriteTime;
					values[1] = NGSyncFoldersWindow.ProcessHash(file);
				}

				return values[1];
			}

			string	hash = NGSyncFoldersWindow.ProcessHash(file);
			NGSyncFoldersWindow.cachedHashes.Add(file, new string[] { System.IO.File.GetLastWriteTime(file).Ticks.ToString(), hash });
			return hash;
		}

		private static void	LoadHashesCache()
		{
			if (System.IO.File.Exists(NGSyncFoldersWindow.cachePath) == true)
			{
				try
				{
					if (NGSyncFoldersWindow.cachedHashes == null)
						NGSyncFoldersWindow.cachedHashes = new Dictionary<string, string[]>(64);

					using (FileStream fs = System.IO.File.Open(NGSyncFoldersWindow.cachePath, FileMode.Open, FileAccess.Read, FileShare.Read))
					using (BufferedStream bs = new BufferedStream(fs))
					using (StreamReader sr = new StreamReader(bs))
					{
						string	line;

						while ((line = sr.ReadLine()) != null)
							NGSyncFoldersWindow.cachedHashes.Add(line, new string[] { sr.ReadLine(), sr.ReadLine() });
					}
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException(ex);
					NGSyncFoldersWindow.cachedHashes.Clear();
				}
			}
			else if (NGSyncFoldersWindow.cachedHashes == null)
				NGSyncFoldersWindow.cachedHashes = new Dictionary<string, string[]>();
		}

		private static string	ProcessHash(string path)
		{
			if (System.IO.File.Exists(path) == false)
				return string.Empty;

			try
			{
				using (var md5 = MD5.Create())
				using (var stream = System.IO.File.OpenRead(path))
					return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-","‌​").ToLower();
			}
			catch
			{
			}

			return string.Empty;
		}

		protected virtual void	OnEnable()
		{
			Metrics.UseTool(17); // NGSyncFolders

			NGChangeLogWindow.CheckLatestVersion(NGAssemblyInfo.Name);

			Utility.RegisterWindow(this);
			Utility.LoadEditorPref(this, NGEditorPrefs.GetPerProjectPrefix());
			Utility.RestoreIcon(this, NGSyncFoldersWindow.TitleColor);

			this.slavesList = new ReorderableList(null, typeof(Project), true, true, this.showSlaves, false);
			this.slavesList.drawHeaderCallback = this.OnDrawHeaderSlaves;
			this.slavesList.elementHeight = this.showSlaves == true ? NGSyncFoldersWindow.SlaveHeight : 0F;
			this.slavesList.drawElementCallback = this.OnDrawSlave;
			this.slavesList.onAddCallback = this.OnAddSlave;

			this.filtersList = new ReorderableList(null, typeof(string), true, true, this.showFilters, false);
			this.filtersList.drawHeaderCallback = this.OnDrawHeaderFilters;
			this.filtersList.elementHeight = this.showFilters == true ? NGSyncFoldersWindow.FilterHeight : 0F;
			this.filtersList.drawElementCallback = this.OnDrawFilter;
			this.filtersList.onAddCallback = this.OnAddFilter;

			//this.profile.master.WatchFileChanged += this.ReplicateOnSlaves;

			this.minSize = new Vector2(370F, 200F);
			this.wantsMouseMove = true;

			HQ.SettingsChanged += this.Repaint;
			Utility.RegisterIntervalCallback(this.Repaint, 250);
			Undo.undoRedoPerformed += this.RepaintAndClearCaches;
		}

		protected virtual void	OnDisable()
		{
			Utility.UnregisterWindow(this);
			//this.profile.master.WatchFileChanged -= this.ReplicateOnSlaves;

			//this.profile.master.Dispose();
			//for (int i = 0; i < this.profile.slaves.Count; i++)
			//	this.profile.slaves[i].Dispose();

			Utility.SaveEditorPref(this, NGEditorPrefs.GetPerProjectPrefix());
			HQ.SettingsChanged -= this.Repaint;
			Utility.UnregisterIntervalCallback(this.Repaint);
			Undo.undoRedoPerformed -= this.RepaintAndClearCaches;
		}

		protected virtual void	OnGUI()
		{
			if (HQ.Settings == null)
			{
				GUILayout.Label(string.Format(LC.G("RequiringConfigurationFile"), NGSyncFoldersWindow.Title));
				if (GUILayout.Button(LC.G("ShowPreferencesWindow")) == true)
					Utility.ShowPreferencesWindowAt(Constants.PreferenceTitle);
				return;
			}

			FreeLicenseOverlay.First(this, NGAssemblyInfo.Name + " Pro", NGSyncFoldersWindow.FreeAdContent);

			SyncFoldersSettings	settings = HQ.Settings.Get<SyncFoldersSettings>();

			// Guarantee there is always one in the list.
			if (settings.syncProfiles.Count == 0)
				settings.syncProfiles.Add(new Profile() { name = "Profile 1" });

			this.currentProfile = Mathf.Clamp(this.currentProfile, 0, settings.syncProfiles.Count - 1);
			this.profile = settings.syncProfiles[this.currentProfile];

			Rect	r = default(Rect);

			if (NGSyncFoldersWindow.cachePath == null)
				NGSyncFoldersWindow.cachePath = Path.Combine(Application.persistentDataPath, Path.Combine(Constants.InternalPackageTitle, string.Format(NGSyncFoldersWindow.CachedHashedFile, this.currentProfile)));

			this.slavesList.list = this.profile.slaves;
			this.filtersList.list = this.profile.filters;

			r.width = this.position.width;
			r.height = Constants.SingleLineHeight;
			GUI.Box(r, string.Empty, GeneralStyles.Toolbar);

			r.width = 20F;
			if (GUI.Button(r, "", GeneralStyles.ToolbarDropDown) == true)
			{
				GenericMenu	menu = new GenericMenu();

				for (int i = 0; i < settings.syncProfiles.Count; i++)
					menu.AddItem(new GUIContent((i + 1) + " - " + settings.syncProfiles[i].name), i == this.currentProfile, this.SwitchProfile, i);

				menu.AddSeparator("");
				menu.AddItem(new GUIContent(LC.G("Add")), false, this.AddProfile);
				menu.DropDown(r);

				GUI.FocusControl(null);
			}
			r.x += r.width + 4F;

			r.width = this.position.width - 20F - 100F - 4F;
			EditorGUI.BeginChangeCheck();
			r.y += 2F;
			string	name = EditorGUI.TextField(r, this.profile.name, GeneralStyles.ToolbarTextField);
			if (EditorGUI.EndChangeCheck() == true)
			{
				Undo.RecordObject(settings, "Rename profile");
				this.profile.name = name;
				HQ.InvalidateSettings();
			}
			r.y -= 2F;
			r.x += r.width;

			r.width = 100F;
			EditorGUI.BeginDisabledGroup(settings.syncProfiles.Count <= 1);
			{
				if (GUI.Button(r, LC.G("Erase"), GeneralStyles.ToolbarButton) == true &&
					((Event.current.modifiers & Constants.ByPassPromptModifier) != 0 ||
						EditorUtility.DisplayDialog(LC.G("NGSyncFolders_EraseSave"), string.Format(LC.G("NGSyncFolders_EraseSaveQuestion"), this.profile.name), LC.G("Yes"), LC.G("No")) == true))
				{
					this.EraseProfile();
					this.Focus();
					return;
				}
			}
			EditorGUI.EndDisabledGroup();

			r.x = 0F;
			r.y += r.height + 5F;
			r.width = this.position.width;

			using (LabelWidthRestorer.Get(85F))
			{
				EditorGUI.BeginChangeCheck();
				string	folderPath = NGEditorGUILayout.OpenFolderField(r, "Master Folder", this.profile.master.folderPath);
				if (EditorGUI.EndChangeCheck() == true)
				{
					Undo.RecordObject(settings, "Alter master path");
					this.profile.master.folderPath = folderPath;
					HQ.InvalidateSettings();
				}
				r.y += r.height + NGSyncFoldersWindow.Spacing;

				EditorGUI.BeginChangeCheck();
				string	relativePath = NGEditorGUILayout.OpenFolderField(r, "Relative Path", this.profile.relativePath, this.profile.master.folderPath, NGEditorGUILayout.FieldButtons.Open);
				if (EditorGUI.EndChangeCheck() == true)
				{
					Undo.RecordObject(settings, "Alter relative path");
					this.profile.relativePath = relativePath;
					HQ.InvalidateSettings();
				}
				r.y += r.height + NGSyncFoldersWindow.Spacing;

				this.PreviewPath(r, this.profile.master.GetFullPath(this.profile.relativePath));
				r.y += r.height + NGSyncFoldersWindow.Spacing;

				r.height = this.slavesList.GetHeight();
				this.slavesList.DoList(r);

				if (this.showSlaves == false)
					r.y -= 16F;

				r.y += r.height + NGSyncFoldersWindow.Spacing;
			}

			r.height = this.filtersList.GetHeight();
			this.filtersList.DoList(r);

			if (this.showFilters == false)
			{
				if (Event.current.type == EventType.Repaint)
				{
					Rect	r2 = r;
					r2.y += r2.height - 14F;
					r2.height = 16F;
					r2.width = 16F;
					EditorGUI.DrawRect(r2, EditorGUIUtility.isProSkin == true ? new Color(55F / 255F, 55F / 255F, 55F / 255F, 1F) : new Color(162F / 255F, 162F / 255F, 162F / 255F, 1F));
				}

				r.y -= 16F;
			}

			r.y += r.height + NGSyncFoldersWindow.Spacing;

			if (string.IsNullOrEmpty(this.cacheFileSize) == true)
				this.UpdateCacheFileSize();

			EditorGUI.BeginChangeCheck();
			r.width -= 200F;
			r.height = 20F;
			bool	useCache = EditorGUI.Toggle(r, this.cacheFileSize, this.profile.useCache);
			if (EditorGUI.EndChangeCheck() == true)
			{
				Undo.RecordObject(settings, "Toggle use cache");
				this.profile.useCache = useCache;
				HQ.InvalidateSettings();
			}

			if (this.cacheFileSize != "Use Cache")
			{
				r.x += r.width;

				r.width = 100F;
				if (GUI.Button(r, "Open", "ButtonLeft") == true)
					EditorUtility.RevealInFinder(NGSyncFoldersWindow.cachePath);
				r.x += r.width;
				if (GUI.Button(r, "Clear", "ButtonRight") == true)
				{
					System.IO.File.Delete(NGSyncFoldersWindow.cachePath);
					this.UpdateCacheFileSize();
				}
			}
			r.y += r.height + NGSyncFoldersWindow.Spacing;

			bool	hasActiveSlaves = false;

			for (int i = 0; i < this.profile.slaves.Count; i++)
			{
				if (this.profile.slaves[i].active == true)
				{
					hasActiveSlaves = true;
					break;
				}
			}

			EditorGUI.BeginDisabledGroup(hasActiveSlaves == false);
			{
				using (BgColorContentRestorer.Get(GeneralStyles.HighlightActionButton))
				{
					r.x = 10F;
					r.width = 150F;
					r.height = 32F;
					if (GUI.Button(r, this.mode == ButtonMode.Scan ? "Scan" : "Scan & Watch", "Button"/*"ButtonLeft"*/) == true)
						this.Scan();

					//if (GUILayout.Button("☰", "ButtonRight", GUILayoutOptionPool.Height(32F), GUILayoutOptionPool.ExpandWidthFalse) == true)
					//{
					//	GenericMenu	menu = new GenericMenu();
					//	menu.AddItem(new GUIContent("Scan"), this.mode == ButtonMode.Scan, () => this.mode = ButtonMode.Scan);
					//	menu.AddItem(new GUIContent("Scan and Watch"), this.mode == ButtonMode.ScanAndWatch, () => this.mode = ButtonMode.ScanAndWatch);
					//	menu.ShowAsContext();
					//}
				}
			}
			EditorGUI.EndDisabledGroup();

			if (this.profile.master.IsScanned == true)
			{
				using (BgColorContentRestorer.Get(GeneralStyles.HighlightResultButton))
				{
					r.x = this.position.width - r.width - 10F;
					if (GUI.Button(r, "Sync All", "Button") == true)
						this.SyncAll();
					r.y += r.height + NGSyncFoldersWindow.Spacing;
				}
			}

			//if (this.mode == ButtonMode.ScanAndWatch)
			//	EditorGUILayout.HelpBox("Scan and Watch may induce huge freeze after a compilation when watching a lot of files. This mode is not recommended for programmers.", MessageType.Warning);
			 
			if (this.profile.master.IsScanned == true)
			{
				r.x = 0F;
				r.width = this.position.width;

				Rect	bodyRect = r;
				bodyRect.height = this.position.height - r.y;

				Rect	viewRect = new Rect { height = this.profile.master.GetHeight() };

				for (int i = 0; i < this.profile.slaves.Count; i++)
				{
					if (this.profile.slaves[i].active == true)
						viewRect.height += this.profile.slaves[i].GetHeight(this.profile.master);
				}

				this.scrollPositionScan = GUI.BeginScrollView(bodyRect, this.scrollPositionScan, viewRect);
				{
					r.y = 0F;
					r.height = this.profile.master.GetHeight();

					if (viewRect.height > r.height)
						r.width -= 15F;

					if (r.yMax > this.scrollPositionScan.y)
						this.profile.master.OnGUI(r, 0, this.scrollPositionScan.y, this.scrollPositionScan.y + bodyRect.height);

					r.y += r.height;

					for (int i = 0; i < this.profile.slaves.Count; i++)
					{
						if (this.profile.slaves[i].active == false)
							continue;

						r.height = this.profile.slaves[i].GetHeight(this.profile.master);

						if (r.yMax > this.scrollPositionScan.y)
							this.profile.slaves[i].OnGUI(r, i + 1, this.scrollPositionScan.y, this.scrollPositionScan.y + bodyRect.height, this.profile.master);

						r.y += r.height;

						if (r.y - this.scrollPositionScan.y > bodyRect.height)
							break;
					}
				}
				GUI.EndScrollView();
			}

			FreeLicenseOverlay.Last(NGAssemblyInfo.Name + " Pro");
		}

		private void	SyncAll()
		{
			try
			{
				float	countScannedSlaves = 0F;

				for (int i = 0; i < this.profile.slaves.Count; i++)
				{
					if (this.profile.slaves[i].IsScanned == true)
						++countScannedSlaves;
				}

				for (int i = 0; i < this.profile.slaves.Count; i++)
				{
					if (this.profile.slaves[i].IsScanned == true)
					{
						EditorUtility.DisplayProgressBar(NGSyncFoldersWindow.Title, "Syncing slave " + (i + 1) + "...", (float)i / countScannedSlaves);
						this.profile.slaves[i].SyncAll(this.profile.master);
					}
				}

				this.profile.master.Reset();
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		private void	Scan()
		{
			try
			{
				if (this.profile.useCache == false)
				{
					if (NGSyncFoldersWindow.cachedHashes == null)
						NGSyncFoldersWindow.cachedHashes = new Dictionary<string, string[]>();
					else
						NGSyncFoldersWindow.cachedHashes.Clear();
				}
				else
				{
					if (EditorUtility.DisplayCancelableProgressBar(NGSyncFoldersWindow.Title, "Loading cache...", 0F) == true)
						return;

					NGSyncFoldersWindow.cachedHashes = null;
					NGSyncFoldersWindow.LoadHashesCache();
				}

				float	countActiveSlaves = 2F; // Cache + master

				for (int i = 0; i < this.profile.slaves.Count; i++)
				{
					if (this.profile.slaves[i].active == true)
						++countActiveSlaves;
				}

				NGSyncFoldersWindow.progress = 1F / countActiveSlaves;

				this.profile.master.Scan(/*this.mode == ButtonMode.ScanAndWatch, */this.profile.relativePath, this.profile.filters);
				for (int i = 0; i < this.profile.slaves.Count; i++)
				{
					if (this.profile.slaves[i].active == true)
					{
						NGSyncFoldersWindow.progress = (i + 2F) / countActiveSlaves;
						NGSyncFoldersWindow.slave = i + 1;

						this.profile.slaves[i].ScanDiff(/*this.mode == ButtonMode.ScanAndWatch, */this.profile.master, this.profile.relativePath, this.profile.filters);
					}
					//else
					//	this.profile.slaves[i].Dispose();
				}

				if (this.profile.useCache == true)
				{
					NGSyncFoldersWindow.SaveCachedHashes();
					this.UpdateCacheFileSize();
				}
			}
			catch (AbortScanException)
			{
				this.profile.master.Clear();

				for (int i = 0; i < this.profile.slaves.Count; i++)
					this.profile.slaves[i].Clear();
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		private void	PreviewPath(Rect r, string fullPath)
		{
			bool	exists = Directory.Exists(fullPath);

			if (exists == false)
				r.xMin += 34F;
			else
				r.xMin += 16F;

			Color	restore = GeneralStyles.SmallLabel.normal.textColor;
			if (exists == false)
				GeneralStyles.SmallLabel.normal.textColor = Color.yellow;

			NGEditorGUILayout.ElasticLabel(r, fullPath, '/', GeneralStyles.SmallLabel);

			GeneralStyles.SmallLabel.normal.textColor = restore;

			r.width = 16F;

			if (exists == false)
			{
				r.x -= 16F;

				GUI.DrawTexture(r, UtilityResources.WarningIcon);
			}

			r.y -= 5F;
			r.x = 5F;

			GUI.Label(r, "↳", GeneralStyles.Title1);
		}

		private void	UpdateCacheFiles(int currentProfile)
		{
			string	directory = Path.GetDirectoryName(NGSyncFoldersWindow.cachePath);

			System.IO.File.Delete(NGSyncFoldersWindow.cachePath);

			List<string>	files = new List<string>(Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly));

			try
			{
				// Make sure they are ordered, to downgrade file by file.
				files.Sort();

				for (int i = 0; i < files.Count; i++)
				{
					string	number = Path.GetFileNameWithoutExtension(files[i].Substring(directory.Length));
					int		n = int.Parse(number);

					if (n >= currentProfile)
						System.IO.File.Move(files[i], Path.Combine(directory, --n + ".txt"));
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		private void	UpdateCacheFileSize()
		{
			if (System.IO.File.Exists(NGSyncFoldersWindow.cachePath) == false)
				this.cacheFileSize = "Use Cache";
			else
			{
				long	size = new FileInfo(NGSyncFoldersWindow.cachePath).Length;

				if (size >= 1000L * 1000L)
					this.cacheFileSize = "Use Cache (" + ((float)(size / (1024F * 1024F))).ToString("N2") + " MiB)";
				else if (size >= 1000L)
					this.cacheFileSize = "Use Cache (" + ((float)(size / 1024F)).ToString("N2") + " KiB)";
				else
					this.cacheFileSize = "Use Cache (" + size + " B)";
			}
		}

		//private void	ReplicateOnSlaves(File master)
		//{
		//	for (int i = 0; i < this.profile.slaves.Count; i++)
		//	{
		//		if (this.profile.slaves[i].IsScanned == false)
		//			continue;

		//		string	slavePath = master.path.Replace(this.profile.master.folderPath, this.profile.slaves[i].folderPath);
		//		File	slave = this.profile.slaves[i].FindClosest(slavePath);

		//		if (master.masterState == MasterState.Deleted)
		//		{
		//			if (slave.initialState != InitialState.Origin)
		//				slave.Delete();
		//		}
		//		else
		//			this.profile.slaves[i].Generate(master.path);
		//	}
		//}

		private void	SwitchProfile(object data)
		{
			Undo.RecordObject(this, "Switch profile");
			this.currentProfile = Mathf.Clamp((int)data, 0, HQ.Settings.Get<SyncFoldersSettings>().syncProfiles.Count - 1);
			this.cachedSlaves = null;
			this.cacheFileSize = null;
			this.cachedFiltersFolderLabel = null;
			NGSyncFoldersWindow.cachePath = null;
		}

		private void	AddProfile()
		{
			SyncFoldersSettings	settings = HQ.Settings.Get<SyncFoldersSettings>();

			if (this.CheckMaxSyncFoldersProfiles(settings.syncProfiles.Count) == true)
			{
				Undo.RecordObjects(new Object[] { settings, this }, "Add profile");
				settings.syncProfiles.Add(new Profile() { name = "Profile " + (settings.syncProfiles.Count + 1) });
				this.currentProfile = settings.syncProfiles.Count - 1;
				this.cachedSlaves = null;
				this.cacheFileSize = null;
				this.cachedFiltersFolderLabel = null;
				NGSyncFoldersWindow.cachePath = null;
				HQ.InvalidateSettings();
			}
		}

		private void	EraseProfile()
		{
			SyncFoldersSettings	settings = HQ.Settings.Get<SyncFoldersSettings>();

			Undo.RecordObjects(new Object[] { settings, this }, "Erase profile");
			settings.syncProfiles.RemoveAt(this.currentProfile);
			this.UpdateCacheFiles(this.currentProfile);
			this.currentProfile = Mathf.Clamp(this.currentProfile, 0, settings.syncProfiles.Count - 1);
			this.cachedSlaves = null;
			this.cacheFileSize = null;
			this.cachedFiltersFolderLabel = null;
			NGSyncFoldersWindow.cachePath = null;
			HQ.InvalidateSettings();
		}

		private void	OnAddSlave(ReorderableList r)
		{
			Undo.RecordObject(HQ.Settings.Get<SyncFoldersSettings>(), "Add slave");
			this.profile.slaves.Add(new Project() { folderPath = this.profile.slaves.Count > 0 ? this.profile.slaves[this.profile.slaves.Count - 1].folderPath : string.Empty });
			HQ.InvalidateSettings();
			this.cachedSlaves = null;
		}

		private void	OnAddFilter(ReorderableList r)
		{
			Undo.RecordObject(HQ.Settings.Get<SyncFoldersSettings>(), "Add filter");
			r.list.Add(new FilterText());
			HQ.InvalidateSettings();
		}

		private void	OnDrawHeaderSlaves(Rect r)
		{
			if (this.cachedSlaves == null)
			{
				int	countActive = 0;

				for (int i = 0; i < this.profile.slaves.Count; i++)
				{
					if (this.profile.slaves[i].active == true)
						++countActive;
				}

				this.cachedSlaves = new GUIContent("Slaves (" + countActive + " / " + this.profile.slaves.Count + ")");
			}

			r.width -= 100F;
			EditorGUI.BeginChangeCheck();
			this.showSlaves = EditorGUI.Foldout(r, this.showSlaves, this.cachedSlaves, true);
			if (EditorGUI.EndChangeCheck() == true)
			{
				this.slavesList.elementHeight = this.showSlaves == true ? NGSyncFoldersWindow.SlaveHeight : 0F;
				this.slavesList.displayAdd = this.showSlaves;
			}
			r.x += r.width;

			r.width = 50F;
			if (GUI.Button(r, "All") == true)
			{
				Undo.RecordObject(HQ.Settings.Get<SyncFoldersSettings>(), "Enable all slaves");
				for (int i = 0; i < this.profile.slaves.Count; i++)
					this.profile.slaves[i].active = true;
				HQ.InvalidateSettings();
				this.cachedSlaves = null;
			}
			r.x += r.width;

			if (GUI.Button(r, "None") == true)
			{
				Undo.RecordObject(HQ.Settings.Get<SyncFoldersSettings>(), "Disable all slaves");
				for (int i = 0; i < this.profile.slaves.Count; i++)
					this.profile.slaves[i].active = false;
				HQ.InvalidateSettings();
				this.cachedSlaves = null;
			}
		}

		private void	OnDrawHeaderFilters(Rect r)
		{
			if (string.IsNullOrEmpty(this.cachedFiltersFolderLabel) == true)
			{
				StringBuilder	buffer = Utility.GetBuffer("Filters (");
				bool			onlyExclusive = true;

				for (int i = 0, j = 0; i < this.profile.filters.Count; i++)
				{
					if (this.profile.filters[i].active == true &&
						string.IsNullOrEmpty(this.profile.filters[i].text) == false)
					{
						if (j > 0)
							buffer.Append(' ');

						if (this.profile.filters[i].type == Filter.Type.Inclusive)
						{
							onlyExclusive = false;
							buffer.Append("+");
						}
						else
							buffer.Append("-");
						buffer.Append(this.profile.filters[i].text);
						++j;
					}
				}

				if (buffer.Length <= "Filters (".Length)
					buffer.Append("No filtering)");
				else
				{
					if (onlyExclusive == true)
						buffer.Insert("Filters (".Length, "All ");

					buffer.Append(")");
				}

				this.cachedFiltersFolderLabel = Utility.ReturnBuffer(buffer);
			}

			EditorGUI.BeginChangeCheck();
			this.showFilters = EditorGUI.Foldout(r, this.showFilters, this.cachedFiltersFolderLabel, true);
			if (EditorGUI.EndChangeCheck() == true)
			{
				this.filtersList.elementHeight = this.showFilters == true ? NGSyncFoldersWindow.FilterHeight : 0F;
				this.filtersList.displayAdd = this.showFilters;
			}
		}

		private void	OnDrawSlave(Rect r, int index, bool isActive, bool isFocused)
		{
			if (this.showSlaves == false)
				return;

			SyncFoldersSettings	settings = HQ.Settings.Get<SyncFoldersSettings>();
			Project				slave = this.profile.slaves[index];
			float				x = r.x;
			float				width = r.width;

			EditorGUI.BeginChangeCheck();
			r.y += 2F;
			r.width = NGSyncFoldersWindow.ToggleSlaveWidth;
			r.height = Constants.SingleLineHeight;

			string	content = index < NGSyncFoldersWindow.CachedSlaveIndexes.Length ? NGSyncFoldersWindow.CachedSlaveIndexes[index] : "#" + (index + 1);
			bool	active = EditorGUI.ToggleLeft(r, content, slave.active);
			if (EditorGUI.EndChangeCheck() == true)
			{
				Undo.RecordObject(settings, "Toggle slave");
				slave.active = active;
				HQ.InvalidateSettings();
				this.cachedSlaves = null;
			}
			r.x += r.width;

			EditorGUI.BeginDisabledGroup(slave.active == false);
			EditorGUI.BeginChangeCheck();
			r.width = width - NGSyncFoldersWindow.ToggleSlaveWidth - NGSyncFoldersWindow.DeleteButtonWidth - 5F;
			string	folderPath = NGEditorGUILayout.OpenFolderField(r, "", slave.folderPath);
			if (EditorGUI.EndChangeCheck() == true)
			{
				Undo.RecordObject(settings, "Alter slave path");
				slave.folderPath = folderPath;
				HQ.InvalidateSettings();
			}
			EditorGUI.EndDisabledGroup();
			r.x += r.width + 5F;

			r.width = NGSyncFoldersWindow.DeleteButtonWidth;
			r.y -= 1F;
			r.height += 1F;
			if (GUI.Button(r, "X") == true)
			{
				Undo.RecordObject(settings, "Delete slave");
				this.profile.slaves.RemoveAt(index);
				HQ.InvalidateSettings();
				this.cachedSlaves = null;
				EditorGUIUtility.ExitGUI();
			}

			r.x = 0F;
			r.y += r.height + 2F;
			r.width = x + width;
			r.height = Constants.SingleLineHeight;

			this.PreviewPath(r, slave.GetFullPath(this.profile.relativePath));
		}

		private void	OnDrawFilter(Rect r, int index, bool isActive, bool isFocused)
		{
			if (this.showFilters == false)
				return;

			SyncFoldersSettings	settings = HQ.Settings.Get<SyncFoldersSettings>();
			FilterText			filter = this.profile.filters[index];
			float				width = r.width;

			r.width = NGSyncFoldersWindow.ToggleFilterWidth;
			EditorGUI.BeginChangeCheck();
			GUI.Toggle(r, filter.active, string.Empty);
			if (EditorGUI.EndChangeCheck() == true)
			{
				Undo.RecordObject(settings, "Toggle filter");
				filter.active = !filter.active;
				HQ.InvalidateSettings();
				this.cachedFiltersFolderLabel = null;
				GUI.FocusControl(null);
			}
			r.x += r.width;

			r.height -= 2F;

			EditorGUI.BeginChangeCheck();
			r.width = width - NGSyncFoldersWindow.ToggleFilterWidth - 2F - NGSyncFoldersWindow.FilterTypeWidth - NGSyncFoldersWindow.DeleteButtonWidth;
			string	text = EditorGUI.TextField(r, filter.text);
			if (EditorGUI.EndChangeCheck() == true)
			{
				Undo.RecordObject(settings, "Alter filter");
				filter.text = text;
				HQ.InvalidateSettings();
				this.cachedFiltersFolderLabel = null;
			}

			if (filter.active == true)
			{
				r.x -= 1F;
				r.y -= 1F;
				r.width += 2F;
				r.height += 2F;
				Utility.DrawUnfillRect(r, NGSyncFoldersWindow.ActiveFilterOutline);
				r.x += 1F;
				r.y += 1F;
				r.width -= 2F;
				r.height -= 2F;
			}

			r.x += r.width + 2F;

			EditorGUI.BeginChangeCheck();
			r.width = NGSyncFoldersWindow.FilterTypeWidth;
			GUI.Toggle(r, filter.type == Filter.Type.Inclusive, filter.type == Filter.Type.Inclusive ? "Inclusive" : "Exclusive", GeneralStyles.ToolbarToggle);
			if (EditorGUI.EndChangeCheck() == true)
			{
				Undo.RecordObject(settings, "Toggle filter");
				filter.type = (Filter.Type)(((int)filter.type + 1) & 1);
				HQ.InvalidateSettings();
				this.cachedFiltersFolderLabel = null;
			}
			r.x += r.width;

			r.width = NGSyncFoldersWindow.DeleteButtonWidth;
			if (GUI.Button(r, "X", GeneralStyles.ToolbarCloseButton) == true)
			{
				Undo.RecordObject(settings, "Delete filter");
				this.profile.filters.RemoveAt(index);
				HQ.InvalidateSettings();
				this.cachedFiltersFolderLabel = null;
				EditorGUIUtility.ExitGUI();
			}
		}

		private void	RepaintAndClearCaches()
		{
			this.cachedSlaves = null;
			this.cachedFiltersFolderLabel = null;
			this.Repaint();
		}

		private bool	CheckMaxSyncFoldersProfiles(int count)
		{
			return NGLicensesManager.Check(count < NGSyncFoldersWindow.MaxSyncFoldersProfiles, NGAssemblyInfo.Name + " Pro", "Free version does not allow more than " + NGSyncFoldersWindow.MaxSyncFoldersProfiles + " profiles.\n\n");
		}

		private bool	CheckMaxSyncFoldersSlaves(int count)
		{
			return NGLicensesManager.Check(count < NGSyncFoldersWindow.MaxSyncFoldersSlaves, NGAssemblyInfo.Name + " Pro", "Free version does not allow more than " + NGSyncFoldersWindow.MaxSyncFoldersSlaves + " slaves.\n\n");
		}

		void	IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			Utility.AddNGMenuItems(menu, this, NGSyncFoldersWindow.Title, NGAssemblyInfo.WikiURL);
		}
	}
}