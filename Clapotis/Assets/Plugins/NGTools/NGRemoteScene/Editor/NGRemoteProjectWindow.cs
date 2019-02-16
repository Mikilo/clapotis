using NGTools;
using NGTools.Network;
using NGTools.NGRemoteScene;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;

namespace NGToolsEditor.NGRemoteScene
{
	using UnityEngine;

	public class NGRemoteProjectWindow : NGRemoteWindow, IHasCustomMenu
	{
		private sealed class OptionPopup : PopupWindowContent
		{
			private readonly NGRemoteProjectWindow	window;

			public	OptionPopup(NGRemoteProjectWindow window)
			{
				this.window = window;
			}

			public override Vector2	GetWindowSize()
			{
				return new Vector2(Mathf.Max(this.window.position.width * .5F, 175F), 19F);
			}

			public override void	OnGUI(Rect r)
			{
				Utility.content.text = LC.G("NGProject_AutoLoad");
				this.window.autoLoad = EditorGUILayout.Toggle(Utility.content, this.window.autoLoad);
			}
		}

		private sealed class Folder
		{
			public sealed class File
			{
				public static Color	HighlightSelectedFile = NGRemoteHierarchyWindow.SelectedObjectBackgroundColor;
				public static File	selectedFile;
				public static int	assetIndex;

				public bool	openSubAssets;

				public readonly int			mainAssetIndex;
				public readonly int[]		IDs;
				public readonly string[]	names;
				public readonly string[]	rawTypes;
				public readonly Type[]		types;
				public readonly string		extension;

				public	File(int mainAssetIndex, int[] IDs, string[] names, string[] types, string extension)
				{
					this.mainAssetIndex = mainAssetIndex;
					this.IDs = IDs;
					this.names = names;
					this.rawTypes = types;
					this.types = new Type[this.IDs.Length];
					for (int i = 0; i < types.Length; i++)
						this.types[i] = Type.GetType(types[i]);
					this.extension = extension;
				}
			}

			public static Texture2D	folderIcon;
			public static Dictionary<string, Texture2D>	icons = new Dictionary<string, Texture2D>();

			public readonly Folder	parent;
			public readonly string	name;

			private List<Folder>	folders;
			private List<File>		files;
			private bool			open;

			public	Folder(Folder parent, string name)
			{
				if (Folder.folderIcon == null)
					Folder.folderIcon = Utility.GetIcon(AssetDatabase.LoadAssetAtPath<Object>("Assets").GetInstanceID());

				this.parent = parent;
				this.name = name;

				if (this.parent == null)
					this.open = true;
			}

			public Folder	GenerateFolder(string name)
			{
				if (this.folders == null)
					this.folders = new List<Folder>();

				for (int i = 0; i < this.folders.Count; i++)
				{
					if (this.folders[i].name.Equals(name) == true)
						return this.folders[i];
				}

				Folder	folder = new Folder(this, name);

				this.folders.Add(folder);

				return folder;
			}

			public Folder	GetFolder(string name)
			{
				for (int i = 0; i < this.folders.Count; i++)
				{
					if (this.folders[i].name.Equals(name) == true)
						return this.folders[i];
				}

				return null;
			}

			public void	AddFile(int mainAssetIndex, int[] IDs, string[] subNames, string[] types, string extension)
			{
				if (this.files == null)
					this.files = new List<File>();

				this.files.Add(new File(mainAssetIndex, IDs, subNames, types, extension));
			}

			public float	GetHeight()
			{
				float	height = Constants.SingleLineHeight;

				if (this.parent == null)
					height = 0F;

				if (this.open == true)
				{
					if (this.folders != null)
					{
						for (int i = 0; i < this.folders.Count; i++)
						{
							if (this.folders[i].folders == null && this.folders[i].files == null)
								continue;

							height += this.folders[i].GetHeight();
						}
					}

					if (this.files != null)
					{
						int	total = this.files.Count;

						for (int i = 0; i < this.files.Count; i++)
						{
							if (this.files[i].openSubAssets == true)
								total += this.files[i].IDs.Length - 1;
						}

						height += total * Constants.SingleLineHeight;
					}
				}

				return height;
			}

			public void	Draw(Rect r, bool forceOpen = false)
			{
				if (forceOpen == false)
				{
					this.open = EditorGUI.Foldout(r, this.open, string.Empty);

					r.x += 28F;
					EditorGUI.LabelField(r, this.name);
					r.x -= 28F;

					Rect	r2 = r;
					r2.x += 12F + EditorGUI.indentLevel * 15F;
					r2.width = r.height;
					GUI.DrawTexture(r2, Folder.folderIcon);
					++EditorGUI.indentLevel;

					r.y += r.height;
				}

				if (forceOpen == true || this.open == true)
				{
					if (this.folders != null)
					{
						for (int i = 0; i < this.folders.Count; i++)
						{
							if (this.folders[i].folders == null && this.folders[i].files == null)
								continue;

							this.folders[i].Draw(r);
							r.y += this.folders[i].GetHeight();
						}
					}

					if (this.files != null)
					{
						bool	consumeMouseDownEvent = false;

						for (int i = 0; i < this.files.Count; i++)
						{
							if (Event.current.type == EventType.Repaint && this.files[i] == File.selectedFile && File.assetIndex == this.files[i].mainAssetIndex)
								EditorGUI.DrawRect(r, File.HighlightSelectedFile);

							if (r.Contains(Event.current.mousePosition) == true)
							{
								if (Event.current.type == EventType.MouseDown)
								{
									UnityObject	unityObject = new UnityObject(this.files[i].types[this.files[i].mainAssetIndex], this.files[i].IDs[this.files[i].mainAssetIndex]);

									NGRemoteProjectWindow.dragOriginPosition = Event.current.mousePosition;

									// Initialize drag data.
									DragAndDrop.PrepareStartDrag();

									DragAndDrop.objectReferences = new Object[0];
									DragAndDrop.SetGenericData("r", unityObject);

									File.selectedFile = this.files[i];
									File.assetIndex = this.files[i].mainAssetIndex;

									consumeMouseDownEvent = true;
								}
								else if (Event.current.type == EventType.MouseDrag && (NGRemoteProjectWindow.dragOriginPosition - Event.current.mousePosition).sqrMagnitude >= Constants.MinStartDragDistance)
								{
									DragAndDrop.StartDrag("Dragging Game Object");

									Event.current.Use();
								}
								else if (Event.current.type == EventType.DragUpdated)
								{
									DragAndDrop.visualMode = DragAndDropVisualMode.Move;

									Event.current.Use();
								}
							}

							Texture2D	icon;
							if (Folder.icons.TryGetValue(this.files[i].extension, out icon) == false)
							{
								icon = this.files[i].extension == ".asset" ? InternalEditorUtility.GetIconForFile(".colors") : InternalEditorUtility.GetIconForFile(this.files[i].extension);
								Folder.icons.Add(this.files[i].extension, icon);
							}

							Rect	r2 = r;
							r2.x += 12F + EditorGUI.indentLevel * 15F;
							r2.width = r.height;
							if (icon != null)
								GUI.DrawTexture(r2, icon, ScaleMode.ScaleToFit);

							if (this.files[i].types.Length == 1)
							{
								EditorGUI.indentLevel += 2;
								r.x -= 2F;
								EditorGUI.LabelField(r, this.files[i].names[this.files[i].mainAssetIndex]);
								r.x += 2F;
								EditorGUI.indentLevel -= 2;
							}
							else
							{
								this.files[i].openSubAssets = EditorGUI.Foldout(r, this.files[i].openSubAssets, string.Empty);

								r.x += 28F;
								EditorGUI.LabelField(r, this.files[i].names[this.files[i].mainAssetIndex]);
								r.x -= 28F;
							}

							if (this.files[i].openSubAssets == true)
							{
								EditorGUI.indentLevel += 1;
								for (int j = 0; j < this.files[i].IDs.Length; j++)
								{
									if (j == this.files[i].mainAssetIndex)
										continue;

									r.y += r.height;

									// TODO: Refactor this whole code. Sub assets can have more than 1 depth. Rebuild File for that purpose.

									if (Event.current.type == EventType.Repaint && this.files[i] == File.selectedFile && File.assetIndex == j)
										EditorGUI.DrawRect(r, File.HighlightSelectedFile);

									if (r.Contains(Event.current.mousePosition) == true)
									{
										if (Event.current.type == EventType.MouseDown)
										{
											UnityObject	unityObject = new UnityObject(this.files[i].types[j], this.files[i].IDs[j]);

											NGRemoteProjectWindow.dragOriginPosition = Event.current.mousePosition;

											// Initialize drag data.
											DragAndDrop.PrepareStartDrag();

											DragAndDrop.objectReferences = new Object[0];
											DragAndDrop.SetGenericData("r", unityObject);

											File.selectedFile = this.files[i];
											File.assetIndex = j;

											Event.current.Use();
										}
										else if (Event.current.type == EventType.MouseDrag && (NGRemoteProjectWindow.dragOriginPosition - Event.current.mousePosition).sqrMagnitude >= Constants.MinStartDragDistance)
										{
											DragAndDrop.StartDrag("Dragging Game Object");

											Event.current.Use();
										}
										else if (Event.current.type == EventType.DragUpdated)
										{
											DragAndDrop.visualMode = DragAndDropVisualMode.Move;

											Event.current.Use();
										}
									}

									r.x += 28F;
									EditorGUI.LabelField(r, this.files[i].names[j]);
									r.x -= 28F;

									if (Folder.icons.TryGetValue(this.files[i].rawTypes[j], out icon) == false)
									{
										icon = AssetPreview.GetMiniTypeThumbnail(this.files[i].types[j]);
										if (icon == null)
										{
											if (this.files[i].rawTypes[j].Contains("CSharp") == true)
												icon = UtilityResources.CSharpIcon;
											else if (this.files[i].rawTypes[j].Contains("UnityScript") == true)
												icon = UtilityResources.JavascriptIcon;

											if (icon == null)
												icon = this.files[i].extension == ".asset" ? InternalEditorUtility.GetIconForFile(".colors") : InternalEditorUtility.GetIconForFile(this.files[i].extension);
										}

										Folder.icons.Add(this.files[i].rawTypes[j], icon);
									}

									r2 = r;
									r2.x += 12F + EditorGUI.indentLevel * 15F;
									r2.width = r.height;
									if (icon != null)
										GUI.DrawTexture(r2, icon, ScaleMode.ScaleToFit);
								}
								EditorGUI.indentLevel -= 1;
							}

							r.y += r.height;

							if (consumeMouseDownEvent == true)
							{
								consumeMouseDownEvent = false;
								Event.current.Use();
							}
						}
					}
				}

				if (forceOpen == false)
					--EditorGUI.indentLevel;
			}
		}

		public const string	NormalTitle = "NG Remote Project";
		public const string	ShortTitle = "NG R Project";
		public static Color	AssetTypesBackgroundColor = new Color(21F / 255F, 17F / 255F, 21F / 255F, .2F);

		private static Vector2	dragOriginPosition;

		public bool	autoLoad;

		private Vector2	scrollPosition;
		private Rect	bodyRect = default(Rect);
		private Folder	root;

		private ListingAssets.AssetReferences[]	projectAssets;

		[MenuItem(Constants.MenuItemPath + NGRemoteProjectWindow.NormalTitle, priority = Constants.MenuItemPriority + 219), Hotkey(NGRemoteProjectWindow.NormalTitle)]
		public static void	Open()
		{
			Utility.OpenWindow<NGRemoteProjectWindow>(NGRemoteProjectWindow.ShortTitle);
		}

		protected override void	OnHierarchyConnected()
		{
			base.OnHierarchyConnected();

			this.projectAssets = null;
		}

		protected override void	OnGUIHeader()
		{
			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				if (GUILayout.Button("☰", "GV Gizmo DropDown") == true)
					PopupWindow.Show(new Rect(0F, 16F, 0F, 0F), new OptionPopup(this));

				GUILayout.FlexibleSpace();

				bool	isConnected = this.Hierarchy.IsClientConnected();
				EditorGUI.BeginDisabledGroup(!isConnected);
				if (this.projectAssets == null && (this.autoLoad == true || GUILayout.Button(LC.G("NGProject_Load"), GeneralStyles.ToolbarButton, GUILayoutOptionPool.Width(40F)) == true))
				{
					if (isConnected == true && this.Hierarchy.BlockRequestChannel(this.GetHashCode()) == true)
						this.Hierarchy.Client.AddPacket(new ClientRequestProjectPacket(), this.OnProjectReceived);
				}
				EditorGUI.EndDisabledGroup();
			}
			EditorGUILayout.EndHorizontal();
		}

		protected override void	OnGUIConnected()
		{
			if (this.projectAssets == null)
			{
				if (this.Hierarchy.IsChannelBlocked(this.GetHashCode()) == true)
				{
					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.FlexibleSpace();
						GUILayout.Label(GeneralStyles.StatusWheel, GUILayoutOptionPool.Width(20F));
						GUILayout.Label("Loading assets...");
						GUILayout.FlexibleSpace();
					}
					EditorGUILayout.EndHorizontal();
				}
				return;
			}

			Rect	viewRect = new Rect(0F, 0F, 0F, this.root.GetHeight());

			bodyRect.x = 0F;
			bodyRect.y = Constants.SingleLineHeight + 2;
			bodyRect.width = this.position.width;
			bodyRect.height = this.position.height - bodyRect.y;

			if (Folder.File.selectedFile != null)
				bodyRect.height -= Constants.SingleLineHeight;

			float	height = bodyRect.y + bodyRect.height;

			this.scrollPosition = GUI.BeginScrollView(bodyRect, this.scrollPosition, viewRect);
			{
				bodyRect.y = 0F;
				bodyRect.height = Constants.SingleLineHeight;

				this.root.Draw(bodyRect, true);
			}
			GUI.EndScrollView();

			if (Folder.File.selectedFile != null)
			{
				bodyRect.y = height;
				EditorGUI.DrawRect(bodyRect, NGRemoteProjectWindow.AssetTypesBackgroundColor);
				EditorGUI.LabelField(bodyRect, Folder.File.selectedFile.rawTypes[Folder.File.assetIndex]);
			}

			if (Event.current.type == EventType.MouseDown)
			{
				Folder.File.selectedFile = null;
				Event.current.Use();
			}
		}

		private void	GeneratePath(ListingAssets.AssetReferences asset)
		{
			try
			{
				string[]	dirs = Path.GetDirectoryName(asset.asset).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				string		extension = Path.GetExtension(asset.asset);
				Folder		folder = this.root;

				// Skip Assets folder.
				for (int i = 1; i < dirs.Length; i++)
					folder = folder.GenerateFolder(dirs[i]);

				folder.AddFile(asset.mainAssetIndex, asset.IDs, asset.subNames, asset.types, extension);
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException("Embedded asset \"" + asset.asset + "\" contains an error.", ex);
			}
		}

		private void	OnProjectReceived(ResponsePacket p)
		{
			this.Hierarchy.UnblockRequestChannel(this.GetHashCode());

			if (p.CheckPacketStatus() == true)
			{
				ServerSendProjectPacket	packet = p as ServerSendProjectPacket;

				this.projectAssets = packet.assets;
				this.root = new Folder(null, "Assets");

				for (int i = 0; i < this.projectAssets.Length; i++)
					this.GeneratePath(this.projectAssets[i]);

				this.Repaint();
			}
		}

		void	IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			if (this.Hierarchy != null)
				this.Hierarchy.AddTabMenus(menu, this);
			Utility.AddNGMenuItems(menu, this, NGRemoteProjectWindow.NormalTitle, Constants.WikiBaseURL + "#markdown-header-133-ng-remote-project");
		}
	}
}