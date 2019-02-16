using NGTools.Network;
using System;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Asset_ServerAcknowledgeUserTexture)]
	internal sealed class ServerAcknowledgeUserTexturePacket : ResponsePacket
	{
		public Type		type;
		public string	name;
		public int		instanceID;
		public string[]	data;

		public	ServerAcknowledgeUserTexturePacket(int networkId) : base(networkId)
		{
		}

		private	ServerAcknowledgeUserTexturePacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}