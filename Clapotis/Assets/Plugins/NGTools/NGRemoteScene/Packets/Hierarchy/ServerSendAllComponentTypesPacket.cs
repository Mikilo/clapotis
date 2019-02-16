using NGTools.Network;
using System;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Unity_ServerSendAllComponentTypes)]
	internal sealed class ServerSendAllComponentTypesPacket : ResponsePacket
	{
		public string[]	types;

		public	ServerSendAllComponentTypesPacket(int networkId) : base(networkId)
		{
		}

		private	ServerSendAllComponentTypesPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override void	Out(ByteBuffer buffer)
		{
			if (this.OutResponseStatus(buffer) == true)
			{
				int	count = 0;
				int	lengthPosition = buffer.Length;

				buffer.Append(0);

				foreach (Type type in Utility.EachAllSubClassesOf(typeof(Component)))
				{
					buffer.AppendUnicodeString(type.GetShortAssemblyType());
					++count;
				}

				int	restoreLength = buffer.Length;

				buffer.Length = lengthPosition;
				buffer.Append(count);
				buffer.Length = restoreLength;
			}
		}

		public override void	In(ByteBuffer buffer)
		{
			if (this.InResponseStatus(buffer) == true)
			{
				this.types = new string[buffer.ReadInt32()];

				for (int i = 0; i < this.types.Length; i++)
					this.types[i] = buffer.ReadUnicodeString();
			}
		}
	}
}