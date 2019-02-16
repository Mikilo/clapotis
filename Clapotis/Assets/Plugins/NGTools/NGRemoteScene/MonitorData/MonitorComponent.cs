using NGTools.Network;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	internal sealed class MonitorComponent : MonitorData
	{
		public int	InstanceID { get { return this.instanceID; } }

		private ServerComponent	component;
		private int				gameObjectInstanceID;
		private int				instanceID;

		public	MonitorComponent(string path, ServerComponent component) : base(path, MonitorComponent.GetClosureComponent(component.component))
		{
			this.component = component;
			this.gameObjectInstanceID = this.component.component.gameObject.GetInstanceID();
			this.instanceID = this.component.instanceID;

			this.children = new List<MonitorData>(this.component.fields.Length);

			for (int i = 0; i < this.component.fields.Length; i++)
			{
				CustomMonitorData	customMonitor = MonitorDataManager.CreateMonitorData(this.component.fields[i].Type, this.path + NGServerScene.ValuePathSeparator + i, getInstance, this.component.fields[i]);

				if (customMonitor != null)
					this.children.Add(customMonitor);
				else if (this.component.fields[i].Type.IsUnityArray() == true)
					this.children.Add(new MonitorArray(this.path + NGServerScene.ValuePathSeparator + i, this.component.fields[i], getInstance));
				else if (this.component.fields[i] is FieldModifier)
					this.children.Add(new MonitorField(this.path + NGServerScene.ValuePathSeparator + i, getInstance, (this.component.fields[i] as FieldModifier).fieldInfo));
				else if (this.component.fields[i] is PropertyModifier)
					this.children.Add(new MonitorProperty(this.path + NGServerScene.ValuePathSeparator + i, getInstance, (this.component.fields[i] as PropertyModifier).propertyInfo));
			}
		}

		public override void	CollectUpdates(List<MonitorData> updates)
		{
			if (this.component.component == null)
			{
				updates.Add(this);
				this.ToDelete = true;
				return;
			}

			if (this.children != null)
			{
				for (int i = 0; i < this.children.Count; i++)
					this.children[i].CollectUpdates(updates);
			}
		}

		public override void	Update()
		{
		}

		public override Packet[]	CreateUpdatePackets()
		{
			return new Packet[] { new NotifyDeletedComponentsPacket(this.gameObjectInstanceID, this.instanceID) };
		}

		private static Func<object>	GetClosureComponent(Component c)
		{
			return () => c;
		}
	}
}