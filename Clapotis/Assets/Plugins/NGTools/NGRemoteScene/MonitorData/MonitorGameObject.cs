using NGTools.Network;
using System.Collections.Generic;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	internal sealed class MonitorGameObject : MonitorData
	{
		private static List<MonitorData>	updatedData = new List<MonitorData>();

		private ServerGameObject		gameObject;
		private int						gameObjectInstanceID;
		private List<ServerComponent>	newComponents = new List<ServerComponent>();

		public	MonitorGameObject(ServerGameObject gameObject) : base(gameObject.gameObject.GetInstanceID().ToString(), () => gameObject)
		{
			this.gameObject = gameObject;
			this.gameObjectInstanceID = this.gameObject.instanceID;

			this.children = new List<MonitorData>(this.gameObject.components.Count + 5)
			{
				new MonitorProperty(this.path + NGServerScene.ValuePathSeparator + NGServerScene.SpecialGameObjectSeparator + "active", () => this.gameObject.gameObject, typeof(GameObject).GetProperty("activeSelf")),
				new MonitorString(this.path + NGServerScene.ValuePathSeparator + NGServerScene.SpecialGameObjectSeparator + "name", () => this.gameObject.gameObject, new PropertyModifier(typeof(GameObject).GetProperty("name"))),
				new MonitorProperty(this.path + NGServerScene.ValuePathSeparator + NGServerScene.SpecialGameObjectSeparator + "isStatic", () => this.gameObject.gameObject, typeof(GameObject).GetProperty("isStatic")),
				new MonitorString(this.path + NGServerScene.ValuePathSeparator + NGServerScene.SpecialGameObjectSeparator + "tag", () => this.gameObject.gameObject, new PropertyModifier(typeof(GameObject).GetProperty("tag"))),
				new MonitorProperty(this.path + NGServerScene.ValuePathSeparator + NGServerScene.SpecialGameObjectSeparator + "layer", () => this.gameObject.gameObject, typeof(GameObject).GetProperty("layer"))
			};

			this.gameObject.ProcessComponents();

			for (int i = 0; i < this.gameObject.components.Count; i++)
				this.children.Add(new MonitorComponent(this.path + NGServerScene.ValuePathSeparator + this.gameObject.components[i].instanceID, this.gameObject.components[i]));
		}

		public override void	CollectUpdates(List<MonitorData> updates)
		{
			if (this.gameObject.gameObject == null)
			{
				updates.Add(this);
				return;
			}

			// Remove destroyed components.
			for (int i = 5; i < this.children.Count; i++)
			{
				if (this.children[i].ToDelete == true)
					this.children.RemoveAt(i--);
			}

			bool	inList = false;

			// Add new components.
			Component[]	components = this.gameObject.gameObject.GetComponents<Component>();
			for (int i = 0; i < components.Length; i++)
			{
				int	j = 0;

				for (; j < this.children.Count; j++)
				{
					if (object.Equals(components[i], this.children[j].Instance) == true)
						break;
				}

				if (j == this.children.Count)
				{
					ServerComponent	newComponent = new ServerComponent(components[i]);
					this.gameObject.components.Add(newComponent);
					this.newComponents.Add(newComponent);

					this.children.Add(new MonitorComponent(this.path + NGServerScene.ValuePathSeparator + components[i].GetInstanceID().ToString(), newComponent));

					inList = true;
				}
			}

			if (inList == true)
				updates.Add(this);

			for (int i = 0; i < this.children.Count; i++)
				this.children[i].CollectUpdates(updates);
		}

		public override void	Update()
		{
		}

		public override Packet[]	CreateUpdatePackets()
		{
			if (this.newComponents.Count > 0)
			{
				Packet[]	p = new Packet[this.newComponents.Count];

				for (int i = 0; i < this.newComponents.Count; i++)
					p[i] = new NotifyComponentAddedPacket(this.gameObjectInstanceID, this.newComponents[i]);

				this.newComponents.Clear();
				return p;
			}

			this.ToDelete = true;
			return new Packet[] { new NotifyGameObjectsDeletedPacket(this.gameObjectInstanceID) };
		}

		public void	UpdateValues(List<Client> clients)
		{
			MonitorGameObject.updatedData.Clear();

			this.CollectUpdates(MonitorGameObject.updatedData);
			//InternalNGDebug.Log("WatcherCollect=" + MonitorGameObject.updatedData.Count);

			for (int i = 0; i < MonitorGameObject.updatedData.Count; i++)
			{
				MonitorGameObject.updatedData[i].Update();

				Packet[]	packets = MonitorGameObject.updatedData[i].CreateUpdatePackets();
				if (packets == null)
					continue;

				for (int j = 0; j < clients.Count; j++)
				{
					for (int k = 0; k < packets.Length; k++)
						clients[j].AddPacket(packets[k]);
				}
			}
		}

		public void	DeleteComponent(int instanceID)
		{
			for (int i = 0; i < this.children.Count; i++)
			{
				MonitorComponent	component = this.children[i] as MonitorComponent;

				if (component != null)
				{
					if (component.InstanceID == instanceID)
					{
						this.children.RemoveAt(i);
						break;
					}
				}
			}
		}
	}
}