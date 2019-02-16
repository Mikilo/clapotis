using NGTools.Network;
using System;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.StaticClass_ServerSendInspectableTypes)]
	internal sealed class ServerSendInspectableTypesPacket : ResponsePacket
	{
		public Type[]	inspectableTypes;
		public string[]	inspectableNames;
		public string[]	inspectableNamespaces;

		public	ServerSendInspectableTypesPacket(int networkId) : base(networkId)
		{
		}

		private	ServerSendInspectableTypesPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override void	Out(ByteBuffer buffer)
		{
			if (this.OutResponseStatus(buffer) == true)
			{
				buffer.Append(this.inspectableTypes.Length);

				for (int i = 0; i < this.inspectableTypes.Length; i++)
				{
					buffer.AppendUnicodeString(this.inspectableTypes[i].Name);
					buffer.AppendUnicodeString(this.inspectableTypes[i].Namespace);
				}
			}
		}

		public override void	In(ByteBuffer buffer)
		{
			if (this.InResponseStatus(buffer) == true)
			{
				this.inspectableNames = new string[buffer.ReadInt32()];
				this.inspectableNamespaces = new string[this.inspectableNames.Length];

				for (int i = 0; i < this.inspectableNames.Length; i++)
				{
					this.inspectableNames[i] = buffer.ReadUnicodeString();
					this.inspectableNamespaces[i] = buffer.ReadUnicodeString();
				}
			}
		}
	}
}