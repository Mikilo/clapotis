using NGLicenses;
using NGTools;
using NGTools.Network;
using NGTools.NGRemoteScene;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace NGToolsEditor.NGRemoteScene
{
	public class NGRemoteInspectorWindow : NGRemoteWindow, IHasCustomMenu, IDataDrawerTool
	{
		public const string	NormalTitle = "NG Remote Inspector";
		public const string	ShortTitle = "NG R Inspector";
		public const int	ForceRepaintRefreshTick = 10;
		public const float	ComponentSpacing = 2F;
		public const float	AddComponentButtonWidth = 200F;
		public const float	AddComponentButtonHeight = 50F;
		public static Color	HeaderBackgroundColor { get { return Utility.GetSkinColor(64F / 255F, 64F / 255F, 64F / 255F, 1F, 210F / 255F, 210F / 255F, 210F / 255F, 1F); } }
		public static Color	MaterialHeaderBackgroundColor { get { return Utility.GetSkinColor(62F / 255F, 62F / 255F, 62F / 255F, 1F, 162F / 255F, 162F / 255F, 162F / 255F, 1F); } }

		public Vector2	ScrollPosition { get { return this.scrollPosition; } }
		public Rect		BodyRect { get { return this.bodyRect; } }

		private Vector2	scrollPosition;
		private Rect	bodyRect;
		private Rect	viewRect;
		private Rect	r;

		private bool				isLock;
		private ClientGameObject	target;
		private Vector2				scrollBatchPosition;
		private int					selectedWindow;
		private int					selectedBatch;
		private List<int>			renderingMaterials = new List<int>();
		private int					lastMaterialsHash = -1;

		private BgColorContentAnimator	animActive;
		private BgColorContentAnimator	animName;
		private BgColorContentAnimator	animIsStatic;
		private BgColorContentAnimator	animTag;
		private BgColorContentAnimator	animLayer;

		private TypeHandler	booleanHandler;
		private TypeHandler	stringHandler;
		private TypeHandler	intHandler;
		
		private ErrorPopup	errorPopup = new ErrorPopup(NGRemoteInspectorWindow.NormalTitle, "An error occurred, try to reopen " + NGRemoteInspectorWindow.NormalTitle + ", change GameObject, toggle Component, change any values. Unfortunately exceptions raised here require you to contact the author.");

		[MenuItem(Constants.MenuItemPath + NGRemoteInspectorWindow.NormalTitle, priority = Constants.MenuItemPriority + 215), Hotkey(NGRemoteInspectorWindow.NormalTitle)]
		public static void	Open()
		{
			Utility.OpenWindow<NGRemoteInspectorWindow>(NGRemoteInspectorWindow.ShortTitle);
		}

		protected override void	OnEnable()
		{
			base.OnEnable();

			this.animActive = new BgColorContentAnimator(this.Repaint, 1F, 0F);
			this.animName = new BgColorContentAnimator(this.Repaint, 1F, 0F);
			this.animIsStatic = new BgColorContentAnimator(this.Repaint, 1F, 0F);
			this.animTag = new BgColorContentAnimator(this.Repaint, 1F, 0F);
			this.animLayer = new BgColorContentAnimator(this.Repaint, 1F, 0F);

			this.booleanHandler = TypeHandlersManager.GetTypeHandler<bool>();
			this.stringHandler = TypeHandlersManager.GetTypeHandler<string>();
			this.intHandler = TypeHandlersManager.GetTypeHandler<int>();

			this.minSize = new Vector2(275F, this.minSize.y);

			this.bodyRect = new Rect();
			this.viewRect = new Rect();
			this.r = new Rect();

			this.selectedWindow = 0;

			this.lastMaterialsHash = -1;
		}

		protected override void	OnHierarchyConnected()
		{
			base.OnHierarchyConnected();

			Utility.RegisterIntervalCallback(this.Repaint, NGRemoteInspectorWindow.ForceRepaintRefreshTick);
		}

		protected override void	OnHierarchyDisconnected()
		{
			base.OnHierarchyDisconnected();

			this.isLock = false;

			Utility.UnregisterIntervalCallback(this.Repaint);
		}

		protected override void	OnGUIHeader()
		{
			this.errorPopup.OnGUILayout();

			EditorGUIUtility.wideMode = true;

			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				if (GUILayout.Toggle(this.selectedWindow == 0, LC.G("NGInspector_Inspector"), GeneralStyles.ToolbarToggle) == true)
					this.selectedWindow = 0;

				if (this.Hierarchy.Client != null)
				{
					if (this.Hierarchy.Client.batchMode == Client.BatchMode.On)
					{
						if (GUILayout.Toggle(this.selectedWindow == 1, LC.G("NGInspector_Batch"), GeneralStyles.ToolbarToggle) == true)
							this.selectedWindow = 1;
					}
				}

				GUILayout.FlexibleSpace();

				EditorGUI.BeginDisabledGroup(!this.Hierarchy.CanUndo());
				{
					if (GUILayout.Button("Undo", GeneralStyles.ToolbarToggle) == true)
						this.Hierarchy.UndoChange();
				}
				EditorGUI.EndDisabledGroup();

				EditorGUI.BeginDisabledGroup(!this.Hierarchy.CanRedo());
				{
					if (GUILayout.Button("Redo", GeneralStyles.ToolbarToggle) == true)
						this.Hierarchy.RedoChange();
				}
				EditorGUI.EndDisabledGroup();

				GUILayout.FlexibleSpace();

				if (GUILayout.Button(LC.G("NGInspector_Historic"), GeneralStyles.ToolbarToggle) == true)
					PacketsHistoricWindow.Open(this.Hierarchy);

				if (this.Hierarchy.Client != null)
				{
					EditorGUI.BeginChangeCheck();
					GUILayout.Toggle(this.Hierarchy.Client.batchMode == Client.BatchMode.On, LC.G("NGInspector_Batch"), GeneralStyles.ToolbarToggle);
					if (EditorGUI.EndChangeCheck() == true)
					{
						if (this.Hierarchy.Client.batchMode == Client.BatchMode.On && this.Hierarchy.Client.batchedPackets.Count > 0)
						{
							if ((Event.current.modifiers & Constants.ByPassPromptModifier) != 0 || EditorUtility.DisplayDialog(LC.G("NGInspector_Batch"), LC.G("NGHierarchy_RequesttoSendCurrentBatch"), LC.G("Yes"), LC.G("No")) == true)
								this.Hierarchy.Client.ExecuteBatch();
						}

						this.Hierarchy.Client.batchMode = this.Hierarchy.Client.batchMode == Client.BatchMode.On ? Client.BatchMode.Off : Client.BatchMode.On;

						if (this.Hierarchy.Client.batchMode == Client.BatchMode.Off)
							this.selectedWindow = 0;
					}

					EditorGUI.BeginDisabledGroup(this.target == null);
					if (GUILayout.Button(LC.G("NGInspector_Refresh"), GeneralStyles.ToolbarButton) == true)
						this.target.RequestComponents(this.Hierarchy.Client);
					EditorGUI.EndDisabledGroup();
				}
				else
				{
					EditorGUI.BeginDisabledGroup(true);
					GUILayout.Toggle(false, LC.G("NGInspector_Batch"), GeneralStyles.ToolbarToggle);
					GUILayout.Button(LC.G("NGInspector_Refresh"), GeneralStyles.ToolbarButton);
					EditorGUI.EndDisabledGroup();
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		protected override void	OnGUIConnected()
		{
			if (this.Hierarchy.Client.batchMode == Client.BatchMode.On &&
				this.selectedWindow == 1)
			{
				this.DrawBatch();
				return;
			}

			if (this.isLock == false)
			{
				ClientGameObject[]	selection = this.Hierarchy.GetSelectedGameObjects();

				if (selection.Length > 0)
				{
					if (this.target != selection[0])
					{
						this.target = selection[0];
						this.target.RequestComponents(this.Hierarchy.Client);
						this.Hierarchy.WatchGameObject(this, this.target);
					}
				}
				else
				{
					if (this.target != null)
					{
						this.target = null;
						this.Hierarchy.WatchGameObject(this, null);
					}
				}
			}

			if (NGLicensesManager.IsPro(NGTools.NGRemoteScene.NGAssemblyInfo.Name + " Pro") == false)
				EditorGUILayout.HelpBox("NG Remote Inspector is read-only. You can only toggle the GameObject's active state below.", MessageType.Info);

			if (this.target == null)
				return;

			this.DrawHeader();

			if (this.target.components == null)
			{
				EditorGUILayout.LabelField(LC.G("NGInspector_ComponentNotLoadedYet"));
				return;
			}

			this.bodyRect.y = GUILayoutUtility.GetLastRect().yMax + NGRemoteInspectorWindow.ComponentSpacing;
			this.bodyRect.width = this.position.width;
			this.bodyRect.height = this.position.height - this.bodyRect.y;
			this.viewRect.height = NGRemoteInspectorWindow.AddComponentButtonHeight;

			for (int i = 0; i < this.target.components.Count; i++)
			{
				try
				{
					this.viewRect.height += this.target.components[i].GetHeight(this) + NGRemoteInspectorWindow.ComponentSpacing;
				}
				catch
				{
					this.viewRect.height += 16F + NGRemoteInspectorWindow.ComponentSpacing;
				}
			}

			this.PopulateMaterials(this.renderingMaterials);

			for (int i = 0; i < this.renderingMaterials.Count; i++)
				this.viewRect.height += ClientMaterial.GetHeight(this.Hierarchy, this.renderingMaterials[i]);

			this.scrollPosition = GUI.BeginScrollView(this.bodyRect, this.scrollPosition, this.viewRect);
			{
				this.r = this.bodyRect;
				this.r.y = 0F;

				if (this.viewRect.height >= this.bodyRect.height)
					this.r.width -= 15F;

				for (int i = 0; i < this.target.components.Count; i++)
				{
					try
					{
						float	height = this.target.components[i].GetHeight(this);

						this.r.height = height;
						if (this.r.y + height <= this.scrollPosition.y)
							continue;

						this.target.components[i].OnGUI(this.r, this);
					}
					catch (Exception ex)
					{
						if (Event.current.type == EventType.Repaint)
							EditorGUI.DrawRect(this.r, Color.red * .5F);

						this.errorPopup.exception = ex;
						this.errorPopup.customMessage = "Component " + this.target.components[i].name + " (" + i + ") failed to render.";
					}
					finally
					{
						this.r.y += this.r.height + NGRemoteInspectorWindow.ComponentSpacing;
					}

					if (this.r.y - this.scrollPosition.y > this.bodyRect.height)
						break;
				}

				if (this.renderingMaterials.Count > 0 && this.r.y - this.scrollPosition.y < this.bodyRect.height)
					this.DrawMaterials(this.renderingMaterials);

				this.r.height = 1F;
				this.r.y += 1F;
				EditorGUI.DrawRect(this.r, Color.black);

				this.r.y += 14F;
				this.r.height = 24F;

				this.r.x += this.r.width * .5F - NGRemoteInspectorWindow.AddComponentButtonWidth * .5F;
				this.r.width = NGRemoteInspectorWindow.AddComponentButtonWidth;

				if (GUI.Button(this.r, "Add Component") == true)
					PopupWindow.Show(this.r, new ComponentsBrowserWindow(this.Hierarchy, this.target.instanceID));
			}
			GUI.EndScrollView();

			int	hash = 0;

			for (int i = 0; i < this.renderingMaterials.Count; i++)
				hash += this.renderingMaterials[i];

			if (hash != this.lastMaterialsHash)
			{
				this.lastMaterialsHash = hash;
				this.Hierarchy.WatchMaterials(this, this.renderingMaterials.ToArray());
			}
		}

		private void	PopulateMaterials(List<int> materials)
		{
			materials.Clear();

			for (int i = 0; i < this.target.components.Count; i++)
			{
				if (this.target.components[i].type != null &&
					(typeof(Renderer).IsAssignableFrom(this.target.components[i].type) == true || // Handle all renderers.
					 (typeof(Behaviour).IsAssignableFrom(this.target.components[i].type) == true && // And those bastards like Projector.
					  typeof(MonoBehaviour).IsAssignableFrom(this.target.components[i].type) == false) ||
					 typeof(MaskableGraphic).IsAssignableFrom(this.target.components[i].type) == true))
				{
					for (int j = 0; j < this.target.components[i].fields.Length; j++)
					{
						if (this.target.components[i].fields[j].name.Equals("sharedMaterials") == true)
						{
							ArrayData		array = this.target.components[i].fields[j].value as ArrayData;
							UnityObject[]	sharedMaterials = array.array as UnityObject[];

							for (int k = 0; k < sharedMaterials.Length; k++)
							{
								// Happens when resizing array.
								if (sharedMaterials[k] == null)
									continue;

								if (sharedMaterials[k].instanceID != 0 &&
									materials.Contains(sharedMaterials[k].instanceID) == false)
								{
									materials.Add(sharedMaterials[k].instanceID);
								}
							}
						}
						else if (this.target.components[i].fields[j].name.Equals("material") == true)
						{
							UnityObject	material = this.target.components[i].fields[j].value as UnityObject;

							if (material.instanceID != 0 &&
								materials.Contains(material.instanceID) == false)
							{
								materials.Add(material.instanceID);
							}
						}
					}
				}
			}
		}

		private void	DrawMaterials(List<int> materials)
		{
			this.Hierarchy.LoadResources(typeof(Material));

			for (int i = 0; i < materials.Count; i++)
			{
				this.r.height = ClientMaterial.GetHeight(this.Hierarchy, materials[i]);

				ClientMaterial.Draw(this.r, this.Hierarchy, materials[i]);

				this.r.y += this.r.height;
			}
		}

		private void	DrawHeader()
		{
			EditorGUILayout.BeginHorizontal();
			{
				this.r = EditorGUILayout.GetControlRect();

				Rect	r2 = this.r;
				r2.x = 0F;
				r2.width = this.position.width;
				r2.height += 3F;
				EditorGUI.DrawRect(r2, NGRemoteInspectorWindow.HeaderBackgroundColor);

				this.r.x += 32F;

				float	w = this.r.width - (50F + 32F);
				this.r.width = 16F;

				if (this.Hierarchy.GetUpdateNotification(this.target.instanceID.ToString() + NGServerScene.ValuePathSeparator + NGServerScene.SpecialGameObjectSeparator + "active") != NotificationPath.None)
					this.animActive.Start();

				using (this.animActive.Restorer(0F, .8F + this.animActive.Value, 0F, 1F))
				{
					EditorGUI.BeginChangeCheck();
					bool	active = GUI.Toggle(this.r, this.target.active, GUIContent.none);
					if (EditorGUI.EndChangeCheck() == true)
						this.Hierarchy.Client.AddPacket(new ClientUpdateFieldValuePacket(this.target.instanceID.ToString() + NGServerScene.ValuePathSeparator + NGServerScene.SpecialGameObjectSeparator + "active", this.booleanHandler.Serialize(active), this.booleanHandler), this.OnGameObjectActiveUpdated);
				}

				if (this.Hierarchy.GetUpdateNotification(this.target.instanceID.ToString() + NGServerScene.ValuePathSeparator + NGServerScene.SpecialGameObjectSeparator + "name") != NotificationPath.None)
					this.animName.Start();

				this.r.x += 16F;
				this.r.width = w - 16F;

				using (this.animName.Restorer(0F, .8F + this.animName.Value, 0F, 1F))
				{
					EditorGUI.BeginChangeCheck();
					string	name = EditorGUI.TextField(this.r, this.target.name);
					if (EditorGUI.EndChangeCheck() == true)
					{
						this.Hierarchy.AddPacket(new ClientUpdateFieldValuePacket(this.target.instanceID.ToString() + NGServerScene.ValuePathSeparator + NGServerScene.SpecialGameObjectSeparator + "name", this.stringHandler.Serialize(name), this.stringHandler), p =>
						{
							if (p.CheckPacketStatus() == true)
							{
								ByteBuffer	buffer = Utility.GetBBuffer((p as ServerUpdateFieldValuePacket).rawValue);

								this.target.name = this.stringHandler.Deserialize(buffer, typeof(string)) as string;
								Utility.RestoreBBuffer(buffer);
							}
						});
					}
				}

				if (this.Hierarchy.GetUpdateNotification(this.target.instanceID.ToString() + NGServerScene.ValuePathSeparator + NGServerScene.SpecialGameObjectSeparator + "isStatic") != NotificationPath.None)
					this.animIsStatic.Start();

				this.r.x += this.r.width;
				this.r.width = 50F;

				using (this.animIsStatic.Restorer(0F, .8F + this.animIsStatic.Value, 0F, 1F))
				{
					EditorGUI.BeginChangeCheck();
					bool	isStatic = GUI.Toggle(this.r, this.target.isStatic, "Static");
					if (EditorGUI.EndChangeCheck() == true)
					{
						this.Hierarchy.AddPacket(new ClientUpdateFieldValuePacket(this.target.instanceID.ToString() + NGServerScene.ValuePathSeparator + NGServerScene.SpecialGameObjectSeparator + "isStatic", this.booleanHandler.Serialize(isStatic), this.booleanHandler), p =>
						{
							if (p.CheckPacketStatus() == true)
							{
								ByteBuffer	buffer = Utility.GetBBuffer((p as ServerUpdateFieldValuePacket).rawValue);

								this.target.isStatic = (bool)this.booleanHandler.Deserialize(buffer, typeof(bool));
								Utility.RestoreBBuffer(buffer);
							}
						});
					}
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			{
				this.r = EditorGUILayout.GetControlRect();

				Rect	r2 = this.r;
				r2.x = 0F;
				r2.width = this.position.width;
				r2.height += 4F;
				EditorGUI.DrawRect(r2, NGRemoteInspectorWindow.HeaderBackgroundColor);

				r2.x += 2F;
				r2.y -= 20F;
				r2.width = 24F;
				r2.height = 24F;
				GUI.DrawTexture(r2, UtilityResources.GameObjectIcon);

				this.r.x += 18F;

				float	w = (this.r.width - 18F) * .5F;

				this.r.width = w;

				if (this.target.tag != null)
				{
					if (this.Hierarchy.GetUpdateNotification(this.target.instanceID.ToString() + NGServerScene.ValuePathSeparator + NGServerScene.SpecialGameObjectSeparator + "tag") != NotificationPath.None)
						this.animTag.Start();

					using (this.animTag.Restorer(0F, .8F + this.animTag.Value, 0F, 1F))
					{
						using (LabelWidthRestorer.Get(30F))
						{
							if (this.Hierarchy.syncTags == true)
							{
								EditorGUI.BeginChangeCheck();
								string	tag = EditorGUI.TagField(this.r, "Tag", this.target.tag);
								if (EditorGUI.EndChangeCheck() == true)
								{
									this.Hierarchy.AddPacket(new ClientUpdateFieldValuePacket(this.target.instanceID.ToString() + NGServerScene.ValuePathSeparator + NGServerScene.SpecialGameObjectSeparator + "tag", this.stringHandler.Serialize(tag), this.stringHandler), p =>
									{
										if (p.CheckPacketStatus() == true)
										{
											ByteBuffer	buffer = Utility.GetBBuffer((p as ServerUpdateFieldValuePacket).rawValue);

											this.target.tag = this.stringHandler.Deserialize(buffer, typeof(string)) as string;
											Utility.RestoreBBuffer(buffer);
										}
									});
								}
							}
							else
							{
								EditorGUI.BeginChangeCheck();
								string	tag = EditorGUI.TextField(this.r, "Tag", this.target.tag);
								if (EditorGUI.EndChangeCheck() == true)
								{
									this.Hierarchy.AddPacket(new ClientUpdateFieldValuePacket(this.target.instanceID.ToString() + NGServerScene.ValuePathSeparator + NGServerScene.SpecialGameObjectSeparator + "tag", this.stringHandler.Serialize(tag), this.stringHandler), p =>
									{
										if (p.CheckPacketStatus() == true)
										{
											ByteBuffer	buffer = Utility.GetBBuffer((p as ServerUpdateFieldValuePacket).rawValue);

											this.target.tag = this.stringHandler.Deserialize(buffer, typeof(string)) as string;
											Utility.RestoreBBuffer(buffer);
										}
									});
								}
							}
						}
					}
				}

				this.r.x += w;

				if (this.target.layer > -1)
				{
					if (this.Hierarchy.GetUpdateNotification(this.target.instanceID.ToString() + NGServerScene.ValuePathSeparator + NGServerScene.SpecialGameObjectSeparator + "layer") != NotificationPath.None)
						this.animLayer.Start();

					using (this.animLayer.Restorer(0F, .8F + this.animLayer.Value, 0F, 1F))
					{
						using (LabelWidthRestorer.Get(40F))
						{
							if (this.Hierarchy.Layers != null)
							{
								EditorGUI.BeginChangeCheck();
								int	layer = EditorGUI.Popup(this.r, "Layer", this.target.layer, this.Hierarchy.Layers);
								if (EditorGUI.EndChangeCheck() == true)
								{
									this.Hierarchy.AddPacket(new ClientUpdateFieldValuePacket(this.target.instanceID.ToString() + NGServerScene.ValuePathSeparator + NGServerScene.SpecialGameObjectSeparator + "layer", this.intHandler.Serialize(layer), this.intHandler), p =>
									{
										if (p.CheckPacketStatus() == true)
										{
											ByteBuffer	buffer = Utility.GetBBuffer((p as ServerUpdateFieldValuePacket).rawValue);

											this.target.layer = (int)this.intHandler.Deserialize(buffer, typeof(int));
											Utility.RestoreBBuffer(buffer);
										}
									});
								}
							}
						}
					}
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		private void	OnGameObjectActiveUpdated(ResponsePacket p)
		{
			if (p.CheckPacketStatus() == true)
			{
				ByteBuffer	buffer = Utility.GetBBuffer((p as ServerUpdateFieldValuePacket).rawValue);

				this.target.active = (bool)this.booleanHandler.Deserialize(buffer, typeof(bool));
				Utility.RestoreBBuffer(buffer);
			}
		}

		private void	DebugDrawResent(Packet packet)
		{
			if (GUILayout.Button(LC.G("NGInspector_Resend"), GUILayoutOptionPool.Width(60F)) == true)
				this.Hierarchy.Client.AddPacket(packet);
		}

		public void	DrawBatch()
		{
			this.bodyRect.y = Constants.SingleLineHeight; // Header
			this.bodyRect.width = this.position.width;
			this.bodyRect.height = this.position.height;
			this.viewRect.height = this.Hierarchy.Client.batchedPackets.Count * Constants.SingleLineHeight;
			this.r.x = 0F;
			this.r.y = Constants.SingleLineHeight;
			this.r.height = Constants.SingleLineHeight;
			this.r.width = this.position.width;

			GUILayout.BeginArea(this.r);
			{
				EditorGUILayout.BeginHorizontal();
				{
					if (GUILayout.Button(LC.G("Execute")) == true)
						this.Hierarchy.Client.ExecuteBatch();

					if (GUILayout.Button(LC.G("Save"), GUILayoutOptionPool.MaxWidth(100F)) == true)
						PromptWindow.Start("Noname", this.PromptSaveBatch, null);
				}
				EditorGUILayout.EndHorizontal();
				this.bodyRect.y += Constants.SingleLineHeight;
			}
			GUILayout.EndArea();

			if (this.Hierarchy.Client.BatchNames.Length > 0)
			{
				this.r.y += this.r.height;
				GUILayout.BeginArea(this.r);
				{
					EditorGUILayout.BeginHorizontal();
					{
						this.selectedBatch = EditorGUILayout.Popup(this.selectedBatch, this.Hierarchy.Client.BatchNames);
						if (GUILayout.Button(LC.G("Load"), GUILayoutOptionPool.MaxWidth(100F)) == true)
							this.Hierarchy.Client.LoadBatch(this.selectedBatch);
					}
					EditorGUILayout.EndHorizontal();
					this.bodyRect.y += Constants.SingleLineHeight;
					this.bodyRect.height -= this.bodyRect.y;
				}
				GUILayout.EndArea();
			}

			this.scrollBatchPosition = GUI.BeginScrollView(this.bodyRect, this.scrollBatchPosition, this.viewRect);
			{
				if (this.viewRect.height >= this.bodyRect.height)
					this.r.width -= 16F;

				this.r.y = 0F;
				this.r.height = Constants.SingleLineHeight;

				for (int i = 0; i < this.Hierarchy.Client.batchedPackets.Count; i++)
				{
					GUILayout.BeginArea(r);
					{
						GUILayout.BeginHorizontal();
						{
							IGUIPacket	clientPacket = this.Hierarchy.Client.batchedPackets[i].packet as IGUIPacket;

							if (clientPacket != null)
								clientPacket.OnGUI(this.Hierarchy);

							if (GUILayout.Button("X", GUILayoutOptionPool.Width(20F)) == true)
							{
								this.Hierarchy.Client.batchedPackets.RemoveAt(i);
								return;
							}
						}
						GUILayout.EndHorizontal();
					}
					GUILayout.EndArea();

					this.r.y += this.r.height;
				}
			}
			GUI.EndScrollView();
		}

		private void	PromptSaveBatch(object data, string name)
		{
			if (this.Hierarchy != null &&
				this.Hierarchy.Client != null &&
				string.IsNullOrEmpty(name) == false)
			{
				this.Hierarchy.Client.SaveBatch(name);
			}
		}

		protected virtual void	ShowButton(Rect r)
		{
			this.isLock = GUI.Toggle(r, this.isLock, GUIContent.none, GeneralStyles.LockButton);
		}

		private void	ToggleLocker()
		{
			this.isLock = !this.isLock;
		}

		void	IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			if (this.Hierarchy != null)
				this.Hierarchy.AddTabMenus(menu, this);
			menu.AddItem(new GUIContent("Lock"), this.isLock, new GenericMenu.MenuFunction(this.ToggleLocker));
			menu.AddSeparator("");
			Utility.AddNGMenuItems(menu, this, NGRemoteInspectorWindow.NormalTitle, Constants.WikiBaseURL + "#markdown-header-132-ng-remote-inspector");
		}
	}
}