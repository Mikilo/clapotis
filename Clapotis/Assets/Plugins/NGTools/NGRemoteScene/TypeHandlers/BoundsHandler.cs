using System;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[Priority(75)]
	internal sealed class BoundsHandler : TypeHandler
	{
		public	BoundsHandler() : base(typeof(Bounds))
		{
		}

		public override bool	CanHandle(Type type)
		{
			return type == typeof(Bounds);
		}

		public override void	Serialize(ByteBuffer buffer, Type fieldType, object instance)
		{
			Bounds	v = (Bounds)instance;

			buffer.Append(v.center.x);
			buffer.Append(v.center.y);
			buffer.Append(v.center.z);
			buffer.Append(v.extents.x);
			buffer.Append(v.extents.y);
			buffer.Append(v.extents.z);
		}

		public override object	Deserialize(ByteBuffer buffer, Type fieldType)
		{
			return new Bounds(new Vector3(buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle()), new Vector3(buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle()));
		}
	}
}