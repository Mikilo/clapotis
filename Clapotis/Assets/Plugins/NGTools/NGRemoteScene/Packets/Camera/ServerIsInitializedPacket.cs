using NGTools.Network;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ServerIsInitialized)]
	internal sealed class ServerIsInitializedPacket : ResponsePacket
	{
		public int		width;
		public int		height;
		public byte[]	modules;

		public	ServerIsInitializedPacket(int networkId) : base(networkId)
		{
		}

		private	ServerIsInitializedPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}