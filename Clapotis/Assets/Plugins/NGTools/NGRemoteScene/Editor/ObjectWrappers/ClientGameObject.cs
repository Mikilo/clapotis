using NGTools;
using NGTools.Network;
using NGTools.NGRemoteScene;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	public sealed class ClientGameObject
	{
		private const float	MinTimeBetweenRefresh = 5F;

		public static ClientGameObject[]	EmptyGameObjectArray = {};

		private ClientGameObject	parent;
		public ClientGameObject		Parent
		{
			get
			{
				return this.parent;
			}
			set
			{
				if (this.parent == value)
					return;

				if (this.parent != null)
					this.parent.children.Remove(this);
				else
					this.scene.roots.Remove(this);

				if (this.destroyed == false)
				{
					this.parent = value;

					if (this.parent != null)
						this.parent.children.Add(this);
					else
						this.scene.roots.Add(this);
				}
			}
		}

		public bool	ActiveInHierarchy
		{
			get
			{
				if (active == false)
					return false;
				if (this.parent != null)
					return this.parent.ActiveInHierarchy;
				return true;
			}
		}

		#region Editor
		public bool	fold = false;
		#endregion

		public ClientScene						scene;
		public bool								active;
		public string							name;
		public readonly int						instanceID;
		public readonly List<ClientGameObject>	children;
		public List<ClientComponent>			components;

		#region Additionnal Data
		public string	tag;
		public int		layer = -1;
		public bool		isStatic;
		#endregion

		private bool	hasSelection;
		/// <summary>
		/// Defines if this game object is selected or contains selected children.
		/// </summary>
		public bool		HasSelection { get { return this.hasSelection; } }

		private bool	selected;
		public bool		Selected
		{
			get
			{
				return this.selected;
			}
			set
			{
				if (this.selected != value)
				{
					this.selected = value;
					this.UpdateHasSelection();
				}
			}
		}

		/// <summary>Used to discard deleted GameObject on the server-side when this is not updated.</summary>
		public int	lastHierarchyUpdate;

		private readonly IUnityData	unityData;
		private float				requestingComponents = 0F;

		private Action<ClientGameObject>	gameObjectDataReceivedCallback;

		private bool	destroyed;

		public	ClientGameObject()
		{
			this.children = new List<ClientGameObject>();
		}

		public	ClientGameObject(ClientScene scene, ClientGameObject parent, NetGameObject remote, IUnityData unityData)
		{
			this.unityData = unityData;
			this.scene = scene;
			this.active = remote.active;
			this.name = remote.name;
			this.instanceID = remote.instanceID;
			this.Parent = parent;

			int	length = remote.children.Length;

			this.children = new List<ClientGameObject>(remote.children.Length);
			for (int i = 0; i < length; i++)
				new ClientGameObject(scene, this, remote.children[i], this.unityData);
		}

		public void	Destroy()
		{
			this.Selected = false;
			this.destroyed = true;

			if (this.parent != null)
				this.Parent = null;
			else
				this.scene.roots.Remove(this);
		}

		public void	UpdateHierarchy(ClientGameObject parent, NetGameObject remote)
		{
			this.active = remote.active;
			this.name = remote.name;

			bool	isSelected = this.selected;
			this.Selected = false;
			this.Parent = parent;
			this.Selected = isSelected;
		}

		public void	RequestComponents(Client client, Action<ClientGameObject> onGameObjectDataReceived = null)
		{
			if (this.components == null && this.requestingComponents < Time.realtimeSinceStartup)
			{
				this.requestingComponents = Time.realtimeSinceStartup + ClientGameObject.MinTimeBetweenRefresh;
				this.gameObjectDataReceivedCallback = onGameObjectDataReceived;
				client.AddPacket(new ClientRequestGameObjectDataPacket(this.instanceID), this.OnGameObjectDataReceived);
			}
		}

		public void	AddComponent(NetComponent netComponent)
		{
			this.components.Add(new ClientComponent(this, netComponent, this.unityData));
		}

		/// <summary></summary>
		/// <param name="instanceID"></param>
		/// <exception cref="MissingComponentException">Thrown when there is no component with the given instanceID.</exception>
		public void	RemoveComponent(int instanceID)
		{
			for (int i = 0; i < this.components.Count; i++)
			{
				if (this.components[i].instanceID == instanceID)
				{
					this.components.RemoveAt(i);
					return;
				}
			}

			throw new MissingComponentException(instanceID);
		}

		public ClientComponent	GetComponent(int instanceID)
		{
			if (this.components == null)
				return null;

			// If entering this function, components should be already set, no need to check.
			for (int i = 0; i < this.components.Count; i++)
			{
				if (this.components[i].instanceID == instanceID)
					return this.components[i];
			}

			return null;
		}

		public void	ClearSelection()
		{
			if (this.selected == true || this.hasSelection == true)
			{
				this.selected = false;
				this.hasSelection = false;

				for (int i = 0; i < this.children.Count; i++)
					this.children[i].ClearSelection();
			}
		}

		public int	GetSiblingIndex()
		{
			if (this.parent != null)
				return this.parent.children.IndexOf(this);
			else
				return this.scene.roots.IndexOf(this);
		}

		public void	SetSiblingIndex(int index)
		{
			if (this.parent != null)
			{
				this.parent.children.Remove(this);
				if (index > this.parent.children.Count)
					index = this.parent.children.Count;
				this.parent.children.Insert(index, this);
			}
			else
			{
				this.scene.roots.Remove(this);
				if (index > this.scene.roots.Count)
					index = this.scene.roots.Count;
				this.scene.roots.Insert(index, this);
			}
		}

		private void	UpdateHasSelection()
		{
			this.hasSelection = this.selected;

			// If not selected, look into children.
			if (this.hasSelection == false)
			{
				for (int i = 0; i < this.children.Count; i++)
				{
					if (this.children[i].hasSelection == true)
					{
						this.hasSelection = true;
						this.fold = true;
						break;
					}
				}
			}
			else
				this.fold = true;

			// Propagate through hierarchy.
			if (this.Parent != null)
				this.Parent.UpdateHasSelection();
			else
				this.scene.UpdateHasSelection();
		}

		private void	OnGameObjectDataReceived(ResponsePacket p)
		{
			if (p.CheckPacketStatus() == true)
			{
				ServerSendGameObjectDataPacket	packet = p as ServerSendGameObjectDataPacket;

				if (packet.gameObjectData.Length == 1)
				{
					this.tag = packet.gameObjectData[0].tag;
					this.layer = packet.gameObjectData[0].layer;
					this.isStatic = packet.gameObjectData[0].isStatic;

					if (this.components == null)
						this.components = new List<ClientComponent>();
					else
						this.components.Clear();

					for (int j = 0; j < packet.gameObjectData[0].components.Length; j++)
						this.components.Add(new ClientComponent(this, packet.gameObjectData[0].components[j], this.unityData));
				}
				else
					InternalNGDebug.Log(Errors.Scene_GameObjectNotFound, "GameObject (" + this.instanceID + ") was not found. Failed to update its data.");
			}

			if (this.gameObjectDataReceivedCallback != null)
				this.gameObjectDataReceivedCallback(this);
		}
	}
}