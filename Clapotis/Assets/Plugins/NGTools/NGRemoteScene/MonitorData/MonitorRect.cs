using System;
using System.Collections.Generic;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	internal sealed class MonitorRect : CustomMonitorData
	{
		private static bool	CanMonitorType(Type type)
		{
			return type == typeof(Rect);
		}

		public	MonitorRect(string path, Func<object> getInstance, IValueGetter valueGetter) : base(path, getInstance)
		{
			Func<object>	subGetInstance = () => valueGetter.GetValue<Rect>(getInstance());

			this.children = new List<MonitorData>(4)
			{
				new MonitorProperty(this.path + NGServerScene.ValuePathSeparator + 'x', subGetInstance, valueGetter.Type.GetProperty("x")),
				new MonitorProperty(this.path + NGServerScene.ValuePathSeparator + 'y', subGetInstance, valueGetter.Type.GetProperty("y")),
				new MonitorProperty(this.path + NGServerScene.ValuePathSeparator + "width", subGetInstance, valueGetter.Type.GetProperty("width")),
				new MonitorProperty(this.path + NGServerScene.ValuePathSeparator + "height", subGetInstance, valueGetter.Type.GetProperty("height"))
			};
		}

		public override void	CollectUpdates(List<MonitorData> updates)
		{
			//Debug.Log("R"+this.fieldInfo.Name + "=" + this.value + " <> "+ this.fieldInfo.GetValue(this.instance));
			for (int i = 0; i < this.children.Count; i++)
				this.children[i].CollectUpdates(updates);
		}

		public override void	Update()
		{
		}
	}
}