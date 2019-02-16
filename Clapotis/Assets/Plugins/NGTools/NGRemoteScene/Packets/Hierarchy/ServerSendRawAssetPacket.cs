using NGTools.Network;
using System;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Asset_ServerSendRawAsset)]
	internal sealed class ServerSendRawAssetPacket : ResponsePacket
	{
		public Type		realType;
		public byte[]	data;

		public	ServerSendRawAssetPacket(int networkId) : base(networkId)
		{
		}

		private	ServerSendRawAssetPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}