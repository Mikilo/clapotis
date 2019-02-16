using NGTools.Network;
using System;
using System.Collections.Generic;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Asset_ServerSendUserAssets)]
	internal sealed class ServerSendUserAssetsPacket : ResponsePacket
	{
		public Type				type;
		public List<string>		names = new List<string>();
		public List<int>		instanceIDs = new List<int>();
		public List<string[]>	data = new List<string[]>();

		public	ServerSendUserAssetsPacket(int networkId) : base(networkId)
		{
		}

		private	ServerSendUserAssetsPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}