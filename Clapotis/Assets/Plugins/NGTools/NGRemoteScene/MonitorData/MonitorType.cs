using NGTools.Network;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace NGTools.NGRemoteScene
{
	internal sealed class MonitorType : MonitorData
	{
		private static List<MonitorData>	updatedData = new List<MonitorData>();

		private Type	type;

		public	MonitorType(string path, Type type) : base(path, () => null)
		{
			this.type = type;

			if (this.type.IsGenericTypeDefinition == true)
			{
				this.children = new List<MonitorData>(0);
				return;
			}

			FieldInfo[]		fields = this.type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			PropertyInfo[]	properties = this.type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

			this.children = new List<MonitorData>(fields.Length + properties.Length);

			for (int i = 0; i < fields.Length; i++)
			{
				FieldModifier		modifier = new FieldModifier(fields[i]);
				CustomMonitorData	customMonitor = MonitorDataManager.CreateMonitorData(fields[i].FieldType, this.path + NGServerScene.ValuePathSeparator + fields[i].Name, getInstance, modifier);

				if (customMonitor != null)
					this.children.Add(customMonitor);
				else if (fields[i].FieldType.IsUnityArray() == true)
					this.children.Add(new MonitorArray(this.path + NGServerScene.ValuePathSeparator + fields[i].Name, modifier, getInstance));
				else
					this.children.Add(new MonitorField(this.path + NGServerScene.ValuePathSeparator + fields[i].Name, getInstance, fields[i]));
			}

			for (int i = 0; i < properties.Length; i++)
			{
				if (properties[i].GetGetMethod() == null)
					continue;

				PropertyModifier	modifier = new PropertyModifier(properties[i]);
				CustomMonitorData	customMonitor = MonitorDataManager.CreateMonitorData(properties[i].PropertyType, this.path + NGServerScene.ValuePathSeparator + properties[i].Name, getInstance, modifier);

				if (customMonitor != null)
					this.children.Add(customMonitor);
				else if (properties[i].PropertyType.IsUnityArray() == true)
					this.children.Add(new MonitorArray(this.path + NGServerScene.ValuePathSeparator + properties[i].Name, modifier, getInstance));
				else
				{
					try
					{
						this.children.Add(new MonitorProperty(this.path + NGServerScene.ValuePathSeparator + properties[i].Name, getInstance, properties[i]));
					}
					catch (Exception ex)
					{
						InternalNGDebug.LogException("Monitoring Type \"" + type + "\" failed at property \"" + properties[i].Name + "\".", ex);
					}
				}
			}
		}

		public override void	CollectUpdates(List<MonitorData> updates)
		{
			for (int i = 0; i < this.children.Count; i++)
				this.children[i].CollectUpdates(updates);
		}

		public override void	Update()
		{
		}

		public void	UpdateValues(List<Client> clients)
		{
			MonitorType.updatedData.Clear();

			this.CollectUpdates(MonitorType.updatedData);

			for (int i = 0; i < MonitorType.updatedData.Count; i++)
			{
				MonitorType.updatedData[i].Update();

				Packet[]	packets = MonitorType.updatedData[i].CreateUpdatePackets();
				if (packets == null)
					continue;

				for (int j = 0; j < clients.Count; j++)
				{
					for (int k = 0; k < packets.Length; k++)
						clients[j].AddPacket(packets[k]);
				}
			}
		}
	}
}