using NGTools;
using NGTools.NGRemoteScene;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditorInternal;

namespace NGToolsEditor.NGRemoteScene
{
	using UnityEngine;

	public class EmbedAssetsBrowserWindow : EditorWindow
	{
		private class Folder
		{
			public class File
			{
				public readonly bool	isValid;
				public readonly string	path;
				public readonly string	name;

				public bool	referenced;

				public	File(string path, string name)
				{
					this.isValid = path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) == false;
					this.path = path;
					this.name = name;
				}

				public override string	ToString()
				{
					return this.name;
				}
			}

			public const string	EnableFolderPrefKey = "NGServerScene_";
			public const string	AlertNullAssetPrefKey = "NGRemoteScene_AlertEmbeddingNullAsset";

			public static Texture2D	folderIcon;

			public readonly Folder			parent;
			public readonly string			name;
			public readonly List<Folder>	folders = new List<Folder>();
			public readonly List<File>		files = new List<File>();

			private bool open;
			public bool	Open
			{
				get
				{
					return this.open;
				}
				set
				{
					this.open = value;
					this.height = -1F;

					if (this.parent != null)
						this.parent.Open = this.parent.open;
				}
			}

			public bool	referenced;

			private float	height = -1F;

			public	Folder(Folder parent, string name)
			{
				if (Folder.folderIcon == null)
					Folder.folderIcon = Utility.GetIcon(AssetDatabase.LoadAssetAtPath<Object>("Assets").GetInstanceID());

				this.parent = parent;
				this.name = name;
				this.Open = EditorPrefs.GetBool(EmbedAssetsBrowserWindow.Folder.EnableFolderPrefKey + this.GetHierarchyPath());
			}

			public float	GetHeight()
			{
				if (this.height < 0F)
				{
					this.height = Constants.SingleLineHeight;

					if (this.open == true)
					{
						for (int i = 0; i < this.folders.Count; i++)
						{
							if (this.folders[i].folders.Count == 0 && this.folders[i].files.Count == 0)
								continue;

							this.height += this.folders[i].GetHeight();
						}

						this.height += this.files.Count * Constants.SingleLineHeight;
					}
				}

				return this.height;
			}

			public void	Draw(Rect r, float viewYMin, float viewYMax)
			{
				float	x = r.x;
				float	xMax = r.xMax;

				r.height = Constants.SingleLineHeight;
				r.width = r.height;

				if (r.yMax > viewYMin)
				{
					EditorGUI.showMixedValue = this.HasMixedRefs();
					EditorGUI.BeginChangeCheck();
					EditorGUI.ToggleLeft(r, string.Empty, this.referenced);
					if (EditorGUI.EndChangeCheck() == true)
					{
						if (this.referenced == true)
							this.Unreference();
						else
							this.Reference();
					}
					r.x += r.width + r.width;

					EditorGUI.showMixedValue = false;

					GUI.DrawTexture(r, Folder.folderIcon);
					r.x += r.width;
					r.xMax = xMax;
					r.xMin -= r.height + r.height;

					EditorGUI.BeginChangeCheck();
					EditorGUI.Foldout(r, this.Open, "     " + this.name, true);
					if (EditorGUI.EndChangeCheck() == true)
					{
						this.Open = !this.open;

						if (Event.current.alt == true)
						{
							Queue<Folder>	queue = new Queue<Folder>(128);

							queue.Enqueue(this);

							while (queue.Count > 0)
							{
								Folder	current = queue.Dequeue();

								for (int i = 0; i < current.folders.Count; i++)
									queue.Enqueue(current.folders[i]);

								current.open = this.open;
								current.height = -1F;
								EditorPrefs.SetBool(EmbedAssetsBrowserWindow.Folder.EnableFolderPrefKey + current.GetHierarchyPath(), this.open);
							}
						}
					}
				}
				else
					r.xMax = xMax;

				if (this.open == true)
				{
					r.x = x + 16F;
					r.y += r.height;

					for (int i = 0; i < this.folders.Count; i++)
					{
						if (this.folders[i].folders.Count == 0 && this.folders[i].files.Count == 0)
							continue;

						r.height = this.folders[i].GetHeight();
						this.folders[i].Draw(r, viewYMin, viewYMax);
						r.y += r.height;
					}

					if (r.yMin > viewYMax)
						return;

					r.height = Constants.SingleLineHeight;

					for (int i = 0; i < this.files.Count; i++)
					{
						if (r.yMax > viewYMin)
						{
							EditorGUI.BeginChangeCheck();

							Texture2D	icon = Utility.GetIcon(AssetDatabase.LoadMainAssetAtPath(this.files[i].path));
							if (icon == null)
								icon = InternalEditorUtility.GetIconForFile(this.files[i].path);
							r.width = r.height;
							r.x += r.width;
							GUI.DrawTexture(r, icon);
							r.x -= r.width;

							EditorGUI.BeginDisabledGroup(!this.files[i].isValid);
							{
								r.xMax = xMax;
								this.files[i].referenced = EditorGUI.ToggleLeft(r, "     " + this.files[i].name, this.files[i].referenced);
								if (EditorGUI.EndChangeCheck() == true)
								{
									EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(this.files[i].path));

									if (this.files[i].referenced == false || EmbedAssetsBrowserWindow.CheckEmbeddableFile(this.files[i]) == true)
										this.Update();
									else
									{
										if (NGEditorPrefs.GetBool(Folder.AlertNullAssetPrefKey) == false &&
											EditorUtility.DisplayDialog(Constants.PackageTitle, "Asset at \"" + this.files[i].path + "\" contains null assets.\nFix them and reference again.", "OK", "Don't show again") == false)
										{
											NGEditorPrefs.SetBool(Folder.AlertNullAssetPrefKey, true);
										}

										this.files[i].referenced = false;
									}
								}
							}
							EditorGUI.EndDisabledGroup();
						}

						r.y += r.height;

						if (r.yMin > viewYMax)
							return;
					}
				}
			}

			private string	GetHierarchyPath()
			{
				StringBuilder	buffer = Utility.GetBuffer();
				Folder			f = this;

				while (f != null)
				{
					buffer.Insert(0, f.name);
					buffer.Insert(0, '/');

					f = f.parent;
				}

				return Utility.ReturnBuffer(buffer);
			}

			public bool	HasMixedRefs()
			{
				bool	referencing = false;
				int		j = 0;

				for (int i = 0; i < this.folders.Count; i++)
				{
					if (this.folders[i].folders.Count == 0 && this.folders[i].files.Count == 0)
						continue;

					if (this.folders[i].referenced == true)
					{
						if (j > 0 && referencing == false)
							return true;

						referencing = true;
					}
					else if (referencing == true)
						return true;

					if (this.folders[i].HasMixedRefs() == true)
						return true;

					 ++j;
				}

				for (int i = 0, k = 0; i < this.files.Count; i++)
				{
					if (this.files[i].isValid == false)
					{
						++k;
						continue;
					}

					if (this.files[i].referenced == true)
					{
						if ((j > 0 || i - k > 0) && referencing == false)
							return true;

						referencing = true;
					}
					else if (referencing == true)
						return true;
				}

				return false;
			}

			public void	Reference()
			{
				this.referenced = true;

				for (int i = 0; i < this.folders.Count; i++)
					this.folders[i].Reference();

				for (int i = 0; i < this.files.Count; i++)
				{
					if (this.files[i].isValid == true && EmbedAssetsBrowserWindow.CheckEmbeddableFile(this.files[i]) == true)
						this.files[i].referenced = true;
				}

				if (this.parent != null)
					this.parent.Update();
			}

			public void	Unreference()
			{
				this.referenced = false;

				for (int i = 0; i < this.folders.Count; i++)
					this.folders[i].Unreference();

				for (int i = 0; i < this.files.Count; i++)
					this.files[i].referenced = false;

				if (this.parent != null)
					this.parent.Update();
			}

			public IEnumerable<File>	EachFileReferenced()
			{
				for (int i = 0; i < this.folders.Count; i++)
				{
					foreach (File file in this.folders[i].EachFileReferenced())
						yield return file;
				}

				for (int i = 0; i < this.files.Count; i++)
				{
					if (this.files[i].referenced == true)
						yield return this.files[i];
				}
			}

			public void	Update(bool ascendentUpdate = true)
			{
				if (this.HasMixedRefs() == false)
				{
					for (int i = 0; i < this.folders.Count; i++)
					{
						if (this.folders[i].folders.Count == 0 && this.folders[i].files.Count == 0)
							continue;
						this.referenced = this.folders[0].referenced;
						goto skipFile;
					}

					if (this.files.Count > 0)
						this.referenced = this.files[0].referenced;
				}

				skipFile:
				if (ascendentUpdate == true && this.parent != null)
					this.parent.Update();
			}

			public override string	ToString()
			{
				return this.name;
			}
		}

		public const string	Title = "Assets Browser";

		public SerializedObject	serializedObject;
		public ListingAssets	origin;

		private Folder			root;
		private HashSet<string>	existingList;
		private Vector2			scrollPosition;

		protected virtual void	OnGUI()
		{
			if (this.existingList == null)
				this.Init();

			Rect	r = this.position;
			Rect	viewRect = default(Rect);

			r.x = 0F;
			r.y = 0F;

			float	yMax = r.yMax;

			Utility.content.text = "NG Remote Project Assets (" + this.origin.assets.Length + ")";
			r.height = GeneralStyles.Title1.CalcSize(Utility.content).y;
			GUI.Label(r, Utility.content, GeneralStyles.Title1);
			r.y += r.height;

			using (BgColorContentRestorer.Get(GeneralStyles.HighlightActionButton))
			{
				r.height = Constants.SingleLineHeight;
				if (GUI.Button(r, "Reference Resources") == true)
					this.ReferenceAssets();
				r.y += r.height + 3F;
			}

			viewRect.height = this.root.GetHeight();

			r.yMax = yMax;
			this.scrollPosition = GUI.BeginScrollView(r, this.scrollPosition, viewRect);
			{
				float	bodyHeight = r.height;

				r.y = 0F;
				r.height = this.root.GetHeight();
				this.root.Draw(r, this.scrollPosition.y, bodyHeight + this.scrollPosition.y);
			}
			GUI.EndScrollView();
		}

		protected virtual void	Update()
		{
			if (EditorApplication.isCompiling == true)
				this.Close();

			try
			{
				this.serializedObject.Update();
			}
			catch (NullReferenceException)
			{
				this.Close();
			}
		}

		private void	ReferenceAssets()
		{
			List<ListingAssets.AssetReferences>	list = new List<ListingAssets.AssetReferences>();

			foreach (Folder.File file in this.root.EachFileReferenced())
			{
				if (file.isValid == false)
					continue;

				Object[]	references;
				Object		mainAsset;

				if (EmbedAssetsBrowserWindow.CheckEmbeddableFile(file, out references, out mainAsset) == false)
				{
					file.referenced = false;
					continue;
				}

				ListingAssets.AssetReferences	newReference = new ListingAssets.AssetReferences()
				{
					asset = file.path,
					mainAssetIndex = -1,
					references = references
				};
				list.Add(newReference);

				for (int i = 0; i < references.Length; i++)
				{
					if (references[i] == null)
						continue;

					if (AssetDatabase.IsMainAsset(references[i]) == true)
						newReference.mainAssetIndex = i;
				}

				// Manually embed the main asset, for simplification sake.
				if (newReference.mainAssetIndex == -1)
				{
					Array.Resize<Object>(ref newReference.references, newReference.references.Length + 1);
					newReference.mainAssetIndex = newReference.references.Length - 1;
					newReference.references[newReference.references.Length - 1] = mainAsset;
				}
			}

			try
			{
				Undo.RecordObject(this.serializedObject.targetObject, "Embed assets");
				this.origin.assets = list.ToArray();
				this.serializedObject.Update();
				EditorUtility.SetDirty(this.serializedObject.targetObject);
			}
			catch (NullReferenceException)
			{
				this.Close();
			}
		}

		private static bool	CheckEmbeddableFile(Folder.File file)
		{
			Object[]	references;
			Object		mainAsset;

			return CheckEmbeddableFile(file, out references, out mainAsset);
		}

		private static bool	CheckEmbeddableFile(Folder.File file, out Object[] references, out Object mainAsset)
		{
			bool	containsNull = false;

			references = AssetDatabase.LoadAllAssetsAtPath(file.path);
			mainAsset = AssetDatabase.LoadMainAssetAtPath(file.path);

			for (int i = 0; i < references.Length; i++)
			{
				if (references[i] == null)
				{
					containsNull = true;
					break;
				}
			}

			if (containsNull == true)
			{
				InternalNGDebug.LogWarning("Asset at \"" + file.path + "\" contains null assets. Fix them and reference again.", mainAsset);
				return false;
			}

			if (references.Length == 0)
			{
				if (mainAsset != null)
					references = new Object[] { mainAsset };
				else
				{
					InternalNGDebug.LogWarning("Assets at \"" + file.path + "\" can not be embedded.");
					return false;
				}
			}

			return true;
		}
		
		private void	Init()
		{
			this.existingList = new HashSet<string>();

			for (int i = 0; i < this.origin.assets.Length; i++)
				this.existingList.Add(this.origin.assets[i].asset);

			string[]	assets = AssetDatabase.GetAllAssetPaths();

			this.root = new Folder(null, "Assets");

			for (int i = 0; i < assets.Length; i++)
			{
				if (assets[i].StartsWith("Assets/") == true &&
					File.Exists(assets[i]) == true)
				{
					this.Generate(assets[i]);
				}
			}
		}

		private void	Generate(string path)
		{
			string[]	paths = path.Split('/');
			Folder		folder = this.root;

			for (int i = 1; i < paths.Length - 1; i++)
			{
				int	j = 0;
				int	max = folder.folders.Count;

				for (; j < max; j++)
				{
					if (folder.folders[j].name == paths[i])
					{
						folder = folder.folders[j];
						break;
					}
				}

				if (j >= max)
				{
					Folder	newFolder = new Folder(folder, paths[i]);
					folder.folders.Add(newFolder);
					folder.folders.Sort((a, b) => a.name.CompareTo(b.name));
					folder = newFolder;
				}
			}

			Folder.File	f = new Folder.File(path, paths[paths.Length - 1]);
			if (existingList.Contains(f.path) == true)
				f.referenced = true;
			folder.files.Add(f);
			folder.files.Sort((a, b) => a.name.CompareTo(b.name));

			folder.Update(true);
		}
	}
}