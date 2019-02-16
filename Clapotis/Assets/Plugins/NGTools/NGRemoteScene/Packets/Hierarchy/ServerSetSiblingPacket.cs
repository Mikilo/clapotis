using NGTools.Network;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Transform_ServerSetSibling)]
	internal sealed class ServerSetSiblingPacket : ResponsePacket
	{
		public int	instanceID;
		public int	instanceIDParent;
		public int	siblingIndex;

		public	ServerSetSiblingPacket(int networkId) : base(networkId)
		{
		}

		private	ServerSetSiblingPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}