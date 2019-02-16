using NGTools.Network;
using System;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Asset_ServerSendResources)]
	internal sealed class ServerSendResourcesPacket : ResponsePacket
	{
		public Type		type;
		public string[]	resourceNames;
		public int[]	instanceIDs;

		public	ServerSendResourcesPacket(int networkId) : base(networkId)
		{
		}

		private	ServerSendResourcesPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}