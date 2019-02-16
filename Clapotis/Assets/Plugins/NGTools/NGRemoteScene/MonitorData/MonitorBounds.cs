using System;
using System.Collections.Generic;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	internal sealed class MonitorBounds : CustomMonitorData
	{
		private static bool	CanMonitorType(Type type)
		{
			return type == typeof(Bounds);
		}

		public	MonitorBounds(string path, Func<object> getInstance, IValueGetter valueGetter) : base(path, getInstance)
		{
			Func<object>	subGetInstance = () => valueGetter.GetValue<Bounds>(getInstance());

			this.children = new List<MonitorData>(2)
			{
				new MonitorProperty(this.path + NGServerScene.ValuePathSeparator + "extents", subGetInstance, valueGetter.Type.GetProperty("extents")),
				new MonitorProperty(this.path + NGServerScene.ValuePathSeparator + "center", subGetInstance, valueGetter.Type.GetProperty("center"))
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