using System;
using System.Collections.Generic;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	internal sealed class MonitorQuaternion : CustomMonitorData
	{
		private static bool	CanMonitorType(Type type)
		{
			return type == typeof(Quaternion);
		}

		public	MonitorQuaternion(string path, Func<object> getInstance, IValueGetter valueGetter) : base(path, getInstance)
		{
			Func<object>	subGetInstance = () => valueGetter.GetValue<Quaternion>(getInstance());

			this.children = new List<MonitorData>(4)
			{
				new MonitorField(this.path + NGServerScene.ValuePathSeparator + 'x', subGetInstance, valueGetter.Type.GetField("x")),
				new MonitorField(this.path + NGServerScene.ValuePathSeparator + 'y', subGetInstance, valueGetter.Type.GetField("y")),
				new MonitorField(this.path + NGServerScene.ValuePathSeparator + 'z', subGetInstance, valueGetter.Type.GetField("z")),
				new MonitorField(this.path + NGServerScene.ValuePathSeparator + 'w', subGetInstance, valueGetter.Type.GetField("w"))
			};
		}

		public override void	CollectUpdates(List<MonitorData> updates)
		{
			//Debug.Log("F"+this.fieldInfo.Name + "=" + this.value + " <> "+ this.fieldInfo.GetValue(this.instance));
			for (int i = 0; i < this.children.Count; i++)
				this.children[i].CollectUpdates(updates);
		}

		public override void	Update()
		{
		}
	}
}