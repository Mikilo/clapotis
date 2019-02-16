using NGTools.Network;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	internal sealed class MonitorColor : CustomMonitorData
	{
		private IValueGetter	valueGetter;
		private TypeHandler		typeHandler;

		private static bool	CanMonitorType(Type type)
		{
			return type == typeof(Color);
		}

		public	MonitorColor(string path, Func<object> getInstance, IValueGetter valueGetter) : base(path, getInstance)
		{
			this.valueGetter = valueGetter;
			this.typeHandler = TypeHandlersManager.GetTypeHandler(valueGetter.Type);
			this.value = valueGetter.GetValue<Color>(this.getInstance());
		}

		public override void	CollectUpdates(List<MonitorData> updates)
		{
			//Debug.Log("C"+this.fieldInfo.Name + "=" + this.value + " <> "+ this.fieldInfo.GetValue(this.instance));
			if ((Color)this.value != valueGetter.GetValue<Color>(this.getInstance()))
				updates.Add(this);
		}

		public override void	Update()
		{
			this.value = valueGetter.GetValue<Color>(this.getInstance());
		}

		public override Packet[]	CreateUpdatePackets()
		{
			return new Packet[] { new NotifyFieldValueUpdatedPacket(this.path, this.typeHandler.Serialize(this.value)) };
		}
	}
}