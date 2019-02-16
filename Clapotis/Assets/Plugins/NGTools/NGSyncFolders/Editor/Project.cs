using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGSyncFolders
{
	[Serializable]
	public sealed class Project
	{
		//public event Action<File>	WatchFileChanged;

		public bool		IsScanned { get { return this.root != null; } }

		public bool		active = true;
		public string	folderPath = string.Empty;

		private File				root;
		private FileSystemWatcher	watcher;
		private GUIStyle			rightToLeft;

		private string	fullPath;
		private string	folderPathForFullPath;
		private string	relativePath;

		public void	Clear()
		{
			if (this.watcher != null)
			{
				this.watcher.Dispose();
				this.watcher = null;
			}

			this.root = null;
		}

		public void	Scan(/*bool watch, */string relativePath, List<FilterText> filters)
		{
			this.Clear();

			string	fullPath = this.GetFullPath(relativePath);

			this.root = new File(null, fullPath, Path.GetFileName(fullPath), true, true, InitialState.Origin, MasterState.Same);
			this.root.ScanFolder(fullPath, filters);

			//if (watch == true)
			//	this.StartWatch(fullPath);
		}

		public void	ScanDiff(/*bool watch, */Project master, string relativePath, List<FilterText> filters)
		{
			this.Clear();

			if (string.IsNullOrEmpty(this.folderPath) == true || Directory.Exists(this.folderPath) == false)
			{
				this.root = null;
				return;
			}

			string	fullPath = this.GetFullPath(relativePath);

			this.root = new File(null, fullPath, Path.GetFileName(fullPath), false, true, InitialState.Origin, SlaveState.Exist);
			this.root.ScanDiff(master.root, fullPath, filters);

			//if (watch == true)
			//	this.StartWatch(fullPath);
		}

		public float	GetHeight(Project master = null)
		{
			if (this.root == null || (master == null && this.watcher == null))
				return 0F;

			if (this.rightToLeft == null)
			{
				this.rightToLeft = new GUIStyle(GUI.skin.label);
				this.rightToLeft.alignment = TextAnchor.MiddleRight;
			}

			if (master != null)
				return 16F + this.root.GetHeight(master.root) + (this.root.CanDisplay == false ? 16F : 0F);
			else
				return 16F + this.root.GetHeight(null) + (this.root.CanDisplay == false ? 16F : 0F);
		}

		public void	OnGUI(Rect r, int i, float minY, float maxY, Project master = null)
		{
			if (this.root == null || (master == null && this.watcher == null))
				return;

			GUI.Box(r, GUIContent.none, GeneralStyles.Toolbar);
			float	w = r.width;

			r.height = 16F;

			Utility.content.text = master == null ? "[Master]" : "[Slave " + i + "]";
			r.width = GUI.skin.label.CalcSize(Utility.content).x;
			GUI.Label(r, Utility.content);
			r.x += r.width;

			r.width = w - r.width;
			if (master != null)
				r.width -= 75F;
			NGEditorGUILayout.ElasticLabel(r, this.root.path, '/');

			using (BgColorContentRestorer.Get(GeneralStyles.HighlightResultButton))
			{
				EditorGUI.BeginDisabledGroup(this.root.CanDisplay == false);
				{
					if (master != null)
					{
						r.x += r.width;
						r.width = 75F;
						if (GUI.Button(r, "Sync", GeneralStyles.ToolbarButton) == true)
							this.SyncAll(master);
					}
				}
				EditorGUI.EndDisabledGroup();
			}

			r.x = 0F;
			r.y += r.height;
			r.width = w;

			if (master != null)
				this.root.OnGUI(r, minY, maxY, master.root);
			else
				this.root.OnGUI(r, minY, maxY, null);
		}

		public void	Reset()
		{
			this.root.ResetState();
		}

		public void	SyncAll(Project master)
		{
			this.root.SyncAll(master.root);
		}

		public File	Generate(string path)
		{
			return this.root.Generate(path);
		}

		public File	FindClosest(string path)
		{
			return this.root.FindClosest(path);
		}

		public string	GetFullPath(string relativePath)
		{
			if (this.relativePath != relativePath ||
				this.folderPathForFullPath != this.folderPath)
			{
				this.folderPathForFullPath = this.folderPath;
				this.relativePath = relativePath;

				if (string.IsNullOrEmpty(folderPath) == false)
					this.fullPath = folderPath + Path.DirectorySeparatorChar + relativePath;
				else
					this.fullPath = relativePath;

				if (string.IsNullOrEmpty(this.fullPath) == true)
					this.fullPath = ".";

				try
				{
					this.fullPath = Path.GetFullPath(this.fullPath).Replace('/', '\\');
				}
				catch (ArgumentException)
				{
				}
			}

			return this.fullPath;
		}

		/*private void	StartWatch(string fullPath)
		{
			this.watcher = new FileSystemWatcher(fullPath);

			this.watcher.Filter = "*";
			this.watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.CreationTime | NotifyFilters.Attributes | NotifyFilters.LastAccess | NotifyFilters.Size | NotifyFilters.Security;

			this.watcher.Changed += this.OnMasterChanged;
			this.watcher.Created += this.OnMasterCreated;
			this.watcher.Deleted += this.OnMasterDeleted;
			this.watcher.Renamed += this.OnMasterRenamed;
			this.watcher.Error += this.ErrorEventHandler;

			this.watcher.InternalBufferSize = 1024 * 1024 * 2; // 2MB buffer, should be enough to handle big spike.
			this.watcher.IncludeSubdirectories = false;

			// Begin watching.
			this.watcher.EnableRaisingEvents = true;
		}

		private void	OnMasterDeleted(object source, FileSystemEventArgs e)
		{
			lock (this)
			{
				try
				{
					File	file = this.root.FindClosest(e.FullPath);

					if (Conf.DebugMode == Conf.DebugState.Verbose)
						Debug.Log(e.ChangeType + " " + e.FullPath + ", working on " + file);

					if (file != null && file.path == e.FullPath)
					{
						file.Delete();

						if (this.WatchFileChanged != null)
							this.WatchFileChanged(file);
					}
					else if (Conf.DebugMode == Conf.DebugState.Verbose)
						Debug.LogWarning("File \"" + e.FullPath + "\" does not exist.");
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException(ex);
				}
			}
		}

		private void	OnMasterCreated(object source, FileSystemEventArgs e)
		{
			lock (this)
			{
				try
				{
					if (Directory.Exists(e.FullPath) == true)
					{
						if (Conf.DebugMode == Conf.DebugState.Verbose)
							InternalNGDebug.VerboseLog("Skip " + e.ChangeType + " folder " + e.FullPath);
						return;
					}

					File	file = this.root.FindClosest(e.FullPath);

					if (Conf.DebugMode == Conf.DebugState.Verbose)
						InternalNGDebug.VerboseLog(e.ChangeType + " " + e.FullPath + ", working on " + file);

					if (file != null)
					{
						file = file.Generate(e.FullPath);
						if (this.WatchFileChanged != null)
							this.WatchFileChanged(file);
					}
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException(ex);
				}
			}
		}

		private void	OnMasterChanged(object source, FileSystemEventArgs e)
		{
			lock (this)
			{
				try
				{
					File	file = this.root.FindClosest(e.FullPath);

					if (Conf.DebugMode == Conf.DebugState.Verbose)
						InternalNGDebug.VerboseLog(e.ChangeType + " " + e.FullPath + ", working on " + file);

					if (file != null)
					{
						if (Directory.Exists(e.FullPath) == false && System.IO.File.Exists(e.FullPath) == false)
							file.Delete();
						else
							file.Generate(e.FullPath);

						if (this.WatchFileChanged != null)
							this.WatchFileChanged(file);
					}
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException(ex);
				}
			}
		}

		private void	OnMasterRenamed(object source, RenamedEventArgs e)
		{
			lock (this)
			{
				if (Conf.DebugMode == Conf.DebugState.Verbose)
					InternalNGDebug.VerboseLog(e.ChangeType + " " + e.FullPath + " <- " + e.OldFullPath + " " + e.OldName);
			}
		}

		private void	OnSlaveDeleted(object source, FileSystemEventArgs e)
		{
			lock (this)
			{
				try
				{
					File	file = this.root.FindClosest(e.FullPath);

					if (Conf.DebugMode == Conf.DebugState.Verbose)
						InternalNGDebug.VerboseLog(e.ChangeType + " " + e.FullPath + ", working on " + file);

					if (file != null)
					{
						if (file.path == e.FullPath)
							file.Delete();
					}
					else if (Conf.DebugMode == Conf.DebugState.Verbose)
						InternalNGDebug.VerboseError("File \"" + e.FullPath + "\" does not exist.");
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException(ex);
				}
			}
		}

		private void	OnSlaveCreated(object source, FileSystemEventArgs e)
		{
			lock (this)
			{
				if (Directory.Exists(e.FullPath) == true)
				{
					if (Conf.DebugMode == Conf.DebugState.Verbose)
						InternalNGDebug.VerboseLog("Skip " + e.ChangeType + " folder " + e.FullPath);
					return;
				}

				try
				{
					File	file = this.root.FindClosest(e.FullPath);

					if (Conf.DebugMode == Conf.DebugState.Verbose)
						InternalNGDebug.VerboseLog(e.ChangeType + " " + e.FullPath + ", working on " + file);

					if (file != null)
						file.Generate(e.FullPath);
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException(ex);
				}
			}
		}

		private void	OnSlaveChanged(object source, FileSystemEventArgs e)
		{
			lock (this)
			{
				try
				{
					File	file = this.root.FindClosest(e.FullPath);

					if (Conf.DebugMode == Conf.DebugState.Verbose)
						InternalNGDebug.VerboseLog(e.ChangeType + " " + e.FullPath + ", working on " + file);

					if (file != null)
					{
						if (Directory.Exists(e.FullPath) == false && System.IO.File.Exists(e.FullPath) == false)
						{
							if (file.path == e.FullPath)
								file.Delete();
						}
						else
							file.Generate(e.FullPath);
					}
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException(ex);
				}
			}
		}

		private void	OnSlaveRenamed(object source, RenamedEventArgs e)
		{
			lock (this)
			{
				if (Conf.DebugMode == Conf.DebugState.Verbose)
					InternalNGDebug.VerboseLog(e.ChangeType + " " + e.FullPath + " <- " + e.OldFullPath + " " + e.OldName);
			}
		}

		private void	ErrorEventHandler(object sender, ErrorEventArgs e)
		{
			InternalNGDebug.VerboseLog("Error " + e);
		}*/
	}
}