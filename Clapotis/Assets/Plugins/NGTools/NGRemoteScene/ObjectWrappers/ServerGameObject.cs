using System;
using System.Collections.Generic;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	public sealed class ServerGameObject
	{
		private static List<Component>	tempListComponents = new List<Component>();

		public readonly GameObject				gameObject;
		public readonly int						instanceID;
		public readonly List<ServerGameObject>	children = new List<ServerGameObject>();
		public readonly List<ServerComponent>	components = new List<ServerComponent>();

		public	ServerGameObject(GameObject parent, Dictionary<int, ServerGameObject> instanceIDs)
		{
			instanceIDs.Add(parent.GetInstanceID(), this);

			this.gameObject = parent;
			this.instanceID = this.gameObject.GetInstanceID();

			Transform	transform = parent.transform;

			if (transform.childCount > 0)
			{
				this.children.Capacity = transform.childCount;
				for (int i = 0; i < transform.childCount; i++)
					this.children.Add(new ServerGameObject(transform.GetChild(i).gameObject, instanceIDs));
			}
		}

		public void	RefreshChildren(Dictionary<int, ServerGameObject> instanceIDs)
		{
			this.children.Clear();

			Transform	transform = this.gameObject.transform;

			if (transform.childCount > 0)
			{
				for (int i = 0; i < transform.childCount; i++)
				{
					GameObject			child = transform.GetChild(i).gameObject;
					int					instanceID = child.GetInstanceID();
					ServerGameObject	ng;

					if (instanceIDs.TryGetValue(instanceID, out ng) == true)
					{
						ng.RefreshChildren(instanceIDs);
						this.children.Add(ng);
					}
					else
						this.children.Add(new ServerGameObject(child, instanceIDs));
				}
			}
		}

		/// <summary>
		/// Populates field components with Components converted into ServerComponents.
		/// </summary>
		public void	ProcessComponents()
		{
			if (this.components.Count == 0)
			{
				this.gameObject.GetComponents<Component>(ServerGameObject.tempListComponents);

				for (int i = ServerGameObject.tempListComponents.Count - 1; i >= 0; --i)
				{
					if (ServerGameObject.tempListComponents[i] != null)
						this.components.Add(new ServerComponent(ServerGameObject.tempListComponents[i]));
				}
			}
		}

		public ServerComponent	AddComponent(Type behaviourType)
		{
			Component	newComponent = this.gameObject.AddComponent(behaviourType);

			if (newComponent != null)
			{
				ServerComponent	component = new ServerComponent(newComponent);
				this.components.Add(component);

				return component;
			}

			return null;
		}

		public bool	RemoveComponent(int instanceID)
		{
			// If entering this function, components should be already set, no need to check.
			for (int i = 0; i < this.components.Count; i++)
			{
				if (this.components[i].instanceID == instanceID)
				{
					GameObject.DestroyImmediate(this.components[i].component);
					return this.components[i].component == false || this.components[i].component == null || object.ReferenceEquals(this.components[i].component, null) == true;
				}
			}

			return false;
		}

		public ServerComponent	GetComponent(int instanceID)
		{
			// If entering this function, components should be already set, no need to check.
			for (int i = 0; i < this.components.Count; i++)
			{
				if (this.components[i].instanceID == instanceID)
					return this.components[i];
			}

			return null;
		}
	}
}