using NGTools.Network;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Scene_ServerSetActiveScene)]
	internal sealed class ServerSetActiveScenePacket : ResponsePacket
	{
		public int	index;

		public	ServerSetActiveScenePacket(int networkId) : base(networkId)
		{
		}

		private	ServerSetActiveScenePacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}