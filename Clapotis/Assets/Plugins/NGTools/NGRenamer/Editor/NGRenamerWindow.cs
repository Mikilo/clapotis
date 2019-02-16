using NGLicenses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

namespace NGToolsEditor.NGRenamer
{
	using UnityEngine;

	/// <remarks>This tool is highly inspired on Bulk Rename Utility.</remarks>
	public class NGRenamerWindow : EditorWindow, IHasCustomMenu
	{
		internal sealed class TransformedNames
		{
			public string	highlighted;
			public string	renamed;
		}

		[Serializable]
		internal sealed class PathFiles
		{
			public string	path;
			public string[]	absolutePathDirectories;
			public string[]	directories;
			public string[]	absolutePathFiles;
			public string[]	files;
			public Vector2	scrollPosition;

			public	PathFiles(string path)
			{
				this.path = path;
				this.Update();
			}

			public void	Update()
			{
				this.absolutePathDirectories = Directory.GetDirectories(this.path);
				this.directories = new string[this.absolutePathDirectories.Length];
				for (int i = 0; i < this.absolutePathDirectories.Length; i++)
					this.directories[i] = Path.GetFileName(this.absolutePathDirectories[i]);

				this.absolutePathFiles = Directory.GetFiles(this.path);
				this.files = new string[this.absolutePathFiles.Length];
				for (int i = 0; i < this.absolutePathFiles.Length; i++)
					this.files[i] = Path.GetFileName(this.absolutePathFiles[i]);
			}
		}

		public const string	Title = "NG Renamer";
		public static Color	TitleColor = new Color(1F, 99F / 255F, 71F / 255F, 1F); // Tomato

		private const int				MaxRenamerFilters = 1;
		private static readonly string	FreeAdContent = NGRenamerWindow.Title + " is restrained to the first filter in " + Constants.PackageTitle + ".";

		private static int	drawingIndex;
		public static int	DrawingIndex { get { return NGRenamerWindow.drawingIndex; } }

		private int		lastHash = 0;
		private Vector2	assetsScrollPosition;
		private Vector2	selectionScrollPosition;

		private List<TextFilter>	filters = new List<TextFilter>();
		private List<Object>		objects = new List<Object>();
		private List<PathFiles>		paths = new List<PathFiles>();
		private Dictionary<string, TransformedNames>	cachedNames = new Dictionary<string, TransformedNames>();
		private List<int>			highlightedPositions = new List<int>();

		private ErrorPopup	errorPopup = new ErrorPopup(NGRenamerWindow.Title, "An error occurred, try to reopen " + NGRenamerWindow.Title + ", change settings or disable filters.");

		[MenuItem(Constants.MenuItemPath + NGRenamerWindow.Title, priority = Constants.MenuItemPriority + 340), Hotkey(NGRenamerWindow.Title)]
		public static void	Open()
		{
			Utility.OpenWindow<NGRenamerWindow>(true, NGRenamerWindow.Title, true);
		}

		protected virtual void	OnEnable()
		{
			Utility.RestoreIcon(this, NGRenamerWindow.TitleColor);

			Metrics.UseTool(11); // NGRenamer

			NGChangeLogWindow.CheckLatestVersion(NGAssemblyInfo.Name);

			foreach (Type type in Utility.EachNGTSubClassesOf(typeof(TextFilter)))
			{
				this.filters.Add(Activator.CreateInstance(type, new object[] { this  }) as TextFilter);
				Utility.LoadEditorPref(this.filters[this.filters.Count - 1], NGEditorPrefs.GetPerProjectPrefix());
			}

			this.filters.Sort((a, b) => b.priority - a.priority);

			Selection.selectionChanged += this.UpdateSelection;
			Undo.undoRedoPerformed += this.Repaint;
		}

		protected virtual void	OnDisable()
		{
			for (int i = 0; i < this.filters.Count; i++)
				Utility.SaveEditorPref(this.filters[i], NGEditorPrefs.GetPerProjectPrefix());

			Selection.selectionChanged -= this.UpdateSelection;
			Undo.undoRedoPerformed -= this.Repaint;
		}

		protected virtual void	OnGUI()
		{
			FreeLicenseOverlay.First(this, NGAssemblyInfo.Name + " Pro", NGRenamerWindow.FreeAdContent);

			float	halfWidth = this.position.width * .5F - 25F;

			this.errorPopup.OnGUILayout();

			for (int i = 0; i < this.filters.Count; i++)
			{
				EditorGUI.BeginDisabledGroup(NGLicensesManager.Check(i < NGRenamerWindow.MaxRenamerFilters, NGAssemblyInfo.Name + " Pro") == false);
				this.filters[i].OnHeaderGUI();
				if (this.filters[i].open == true)
				{
					try
					{
						this.filters[i].OnGUI();
					}
					catch (Exception ex)
					{
						this.errorPopup.exception = ex;
					}
				}
				EditorGUI.EndDisabledGroup();
			}

			if (this.objects.Count > 0 || this.paths.Count > 0 || Selection.activeObject != null)
			{
				using (BgColorContentRestorer.Get(GeneralStyles.HighlightActionButton))
				{
					if (GUILayout.Button("Replace All") == true)
						this.ReplaceAll();
				}

				if (this.objects.Count > 0)
				{
					EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
					{
						GUILayout.Label("Assets (" + this.objects.Count + ")", GeneralStyles.Title1);
					}
					EditorGUILayout.EndHorizontal();

					this.assetsScrollPosition = EditorGUILayout.BeginScrollView(this.assetsScrollPosition, GUILayoutOptionPool.ExpandHeightTrue);
					{
						for (int i = 0; i < this.objects.Count; i++)
						{
							NGRenamerWindow.drawingIndex = i;

							EditorGUILayout.BeginHorizontal();
							{
								Texture2D	icon = Utility.GetIcon(this.objects[i].GetInstanceID());

								Rect	r = GUILayoutUtility.GetRect(16F, 16F);
								GUI.DrawTexture(r, icon);
								r.width += halfWidth - 32F;
								if (GUI.Button(r, "", GUI.skin.label) == true)
								{
									NGEditorGUILayout.PingObject(this.objects[i]);
									return;
								}

								this.DrawElement(Path.GetFileName(this.objects[i].name), halfWidth - 32F, (s) => this.RenameAsset(this.objects[i], s));

								if (GUILayout.Button("X", GeneralStyles.ToolbarCloseButton) == true)
								{
									this.objects.RemoveAt(i);
									return;
								}
							}
							EditorGUILayout.EndHorizontal();
						}
					}
					EditorGUILayout.EndScrollView();

					GUILayout.Space(5F);
				}

				for (int i = 0; i < this.paths.Count; i++)
				{
					EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
					{
						Rect	r = GUILayoutUtility.GetRect(0F, 24F, GUILayoutOptionPool.ExpandWidthTrue);
						GUI.Label(r, "Path " + this.paths[i].path + " (" + (this.paths[i].directories.Length + this.paths[i].files.Length) + ")", GeneralStyles.Title1);

						Utility.content.text = "↻";
						Utility.content.tooltip = "Refresh folder's content";
						if (GUILayout.Button(Utility.content, GeneralStyles.ToolbarAltButton, GUILayoutOptionPool.Width(20F)) == true)
						{
							try
							{
								this.paths[i].Update();
							}
							catch (Exception ex)
							{
								this.errorPopup.exception = ex;
							}
							return;
						}
						Utility.content.tooltip = string.Empty;

						if (GUILayout.Button("X", GeneralStyles.ToolbarCloseButton, GUILayoutOptionPool.Width(20F)) == true)
						{
							this.paths.RemoveAt(i);
							return;
						}
					}
					EditorGUILayout.EndHorizontal();

					try
					{
						this.paths[i].scrollPosition = EditorGUILayout.BeginScrollView(this.paths[i].scrollPosition, GUILayoutOptionPool.ExpandHeightTrue);
						{
							for (int j = 0; j < this.paths[i].directories.Length; j++)
							{
								NGRenamerWindow.drawingIndex = j;

								this.DrawElement(Path.GetFileName(this.paths[i].directories[j]), halfWidth, (s) => {
									try
									{
										// To ensure different letter case will work.
										string	tmp = Path.Combine(this.paths[i].path, Path.GetRandomFileName());
										Directory.Move(this.paths[i].absolutePathDirectories[j], tmp);
										Directory.Move(tmp, Path.Combine(this.paths[i].path, s));
										this.paths[i].directories[j] = s;
									}
									catch (Exception ex)
									{
										Debug.LogException(ex);
									}
								});
							}

							for (int j = 0; j < this.paths[i].files.Length; j++)
							{
								NGRenamerWindow.drawingIndex = j;

								this.DrawElement(Path.GetFileName(this.paths[i].files[j]), halfWidth, (s) => {
									try
									{
										File.Move(this.paths[i].absolutePathFiles[j], Path.Combine(this.paths[i].path, s));
										this.paths[i].files[j] = s;
									}
									catch (Exception ex)
									{
										Debug.LogException(ex);
									}
								});
							}
						}
						EditorGUILayout.EndScrollView();
					}
					catch (Exception ex)
					{
						this.paths.RemoveAt(i);
						Debug.LogException(ex);
					}

					GUILayout.Space(5F);
				}

				if (Selection.objects.Length > 0)
				{
					EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
					{
						GUILayout.Label("Selection", GeneralStyles.Title1);
					}
					EditorGUILayout.EndHorizontal();

					this.selectionScrollPosition = EditorGUILayout.BeginScrollView(this.selectionScrollPosition, GUILayoutOptionPool.ExpandHeightTrue);
					{
						for (int i = 0; i < Selection.objects.Length; i++)
						{
							NGRenamerWindow.drawingIndex = i;

							this.DrawElement(Selection.objects[i].name, halfWidth, (s) => this.RenameAsset(Selection.objects[i], s));
						}
					}
					EditorGUILayout.EndScrollView();
				}

				using (BgColorContentRestorer.Get(GeneralStyles.HighlightActionButton))
				{
					if (GUILayout.Button("Replace All") == true)
						this.ReplaceAll();
				}

				GUILayout.FlexibleSpace();
			}
			else
			{
				Rect	r = GUILayoutUtility.GetRect(this.position.width, 16F, GUILayoutOptionPool.ExpandHeightTrue);

				r.x += 2F;
				r.y += 2F;
				r.width -= 4F;
				r.height -= 4F;

				if (Event.current.type == EventType.Repaint)
					Utility.DrawUnfillRect(r, Color.grey);

				GUI.Label(r, "Select assets from Hierarchy or Project\n\nDrop any files or folders from\n" + (Application.platform == RuntimePlatform.WindowsEditor ? "Explorer, " : (Application.platform == RuntimePlatform.OSXEditor ? "Finder, " : "")) + "Hierarchy or Project", GeneralStyles.CenterText);
			}

			if (Event.current.type == EventType.DragUpdated)
			{
				if (DragAndDrop.objectReferences.Length > 0 || DragAndDrop.paths.Length > 0)
					DragAndDrop.visualMode = DragAndDropVisualMode.Move;
				else
					DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
			}
			else if (Event.current.type == EventType.DragPerform)
			{
				DragAndDrop.AcceptDrag();

				try
				{
					if (DragAndDrop.objectReferences.Length > 0)
					{
						for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
						{
							Object	obj	= DragAndDrop.objectReferences[i];

							if (typeof(Behaviour).IsAssignableFrom(obj.GetType()) == true)
								obj = (obj as Behaviour).gameObject;

							if (this.objects.Contains(obj) == false)
								this.objects.Add(obj);
						}
					}
					else if (DragAndDrop.paths.Length > 0)
					{
						for (int i = 0; i < DragAndDrop.paths.Length; i++)
						{
							string	path = DragAndDrop.paths[i];
							if (Directory.Exists(path) == false)
								path = new DirectoryInfo(path).Parent.FullName;

							if (string.IsNullOrEmpty(path) == false && this.paths.Exists((p) => p.path == path) == false)
								this.paths.Add(new PathFiles(path));
						}
					}
				}
				catch (Exception ex)
				{
					this.errorPopup.exception = ex;
				}

				Event.current.Use();
			}
			else if (Event.current.type == EventType.Repaint && DragAndDrop.visualMode == DragAndDropVisualMode.Move)
				Utility.DropZone(new Rect(0F, 0F, this.position.width, this.position.height), "Drop folder or asset to rename");

			FreeLicenseOverlay.Last(NGAssemblyInfo.Name + " Pro");
		}

		private void	DrawElement(string input, float halfWidth, Action<string> renamer)
		{
			EditorGUILayout.BeginHorizontal();
			{
				TransformedNames	names;

				if (this.cachedNames.TryGetValue(input, out names) == false)
				{
					try
					{
						names = new TransformedNames();
						names.highlighted = this.HighlightPattern(input);
						names.renamed = this.Rename(input);
						this.cachedNames.Add(input, names);
					}
					catch (Exception ex)
					{
						this.errorPopup.exception = ex;
						names.highlighted = input;
						names.renamed = "ERROR";
					}
				}

				GUILayout.Label(names.highlighted, GeneralStyles.RichLabel, GUILayoutOptionPool.Width(halfWidth));

				if (GUILayout.Button("->", GUILayoutOptionPool.Width(30F)) == true)
				{
					renamer(names.renamed);
					return;
				}

				GUILayout.Label(names.renamed, GUILayoutOptionPool.Width(halfWidth));
			}
			EditorGUILayout.EndHorizontal();
		}

		private void	RenameAsset(Object asset, string name)
		{
			Undo.RecordObject(asset, "Rename asset");
			if (AssetDatabase.Contains(asset) == true)
			{
				if (AssetDatabase.IsMainAsset(asset) == true)
					AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(asset), name);
				else
				{
					asset.name = name;
					AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(asset), ImportAssetOptions.ForceUpdate);
				}
			}
			else
				asset.name = name;
			EditorUtility.SetDirty(asset);
		}

		private void	ReplaceAll()
		{
			try
			{
				for (int i = 0; i < Selection.objects.Length; i++)
					this.RenameAsset(Selection.objects[i], this.GetCachedValue(Selection.objects[i].name));

				for (int i = 0; i < this.objects.Count; i++)
					this.RenameAsset(this.objects[i], this.GetCachedValue(this.objects[i].name));

				for (int i = 0; i < this.paths.Count; i++)
				{
					for (int j = 0; j < this.paths[i].files.Length; j++)
						File.Move(this.paths[i].files[j], this.GetCachedValue(this.paths[i].files[j]));
				}
			}
			catch (Exception ex)
			{
				this.errorPopup.exception = ex;
			}
		}

		private string	GetCachedValue(string input)
		{
			TransformedNames	names;

			if (this.cachedNames.TryGetValue(input, out names) == false)
				throw new Exception("Cached value for " + input + " was not found.");

			return names.renamed;
		}

		private void	UpdateSelection()
		{
			int	hash = this.GetCurrentSelectionHash();

			if (this.lastHash == hash)
				return;

			this.lastHash = hash;

			this.Repaint();
		}

		private int	GetCurrentSelectionHash()
		{
			for (int i = 0; i < Selection.instanceIDs.Length; i++)
			{
				if (EditorUtility.InstanceIDToObject(Selection.instanceIDs[i]) != null)
				{
					// Yeah, what? Is there a problem with my complex anti-colisionning hash function?
					int	hash = 0;

					for (int j = 0; j < Selection.instanceIDs.Length; j++)
						hash += Selection.instanceIDs[j];

					return hash;
				}
			}

			return 0;
		}

		private string	Rename(string input)
		{
			for (int i = 0; i < this.filters.Count; i++)
			{
				if (this.filters[i].enable == false)
					continue;

				input = this.filters[i].Filter(input);
			}

			return input;
		}

		private string	HighlightPattern(string input)
		{
			highlightedPositions.Clear();

			for (int i = 0; i < this.filters.Count; i++)
			{
				if (this.filters[i].enable == false)
					continue;

				this.filters[i].Highlight(input, highlightedPositions);
			}

			return this.HighlightPattern(input, highlightedPositions);
		}

		public void	Invalidate()
		{
			this.cachedNames.Clear();
		}

		private string	HighlightPattern(string input, List<int> highlightedPositions)
		{
			StringBuilder	buffer = Utility.GetBuffer(input);
			bool			closed = false;

			for (int i = input.Length - 1; i >= 0; --i)
			{
				if (closed == false)
				{
					if (highlightedPositions.Contains(i) == true)
					{
						closed = true;
						buffer.Insert(i + 1, "</color>");
					}
				}
				else
				{
					if (highlightedPositions.Contains(i) == false)
					{
						closed = false;
						buffer.Insert(i + 1, "<color=green>");
					}
				}
			}

			if (closed == true)
				buffer.Insert(0, "<color=green>");

			return Utility.ReturnBuffer(buffer);
		}

		void	IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			Utility.AddNGMenuItems(menu, this, NGRenamerWindow.Title, NGAssemblyInfo.WikiURL, true);
		}
	}
}