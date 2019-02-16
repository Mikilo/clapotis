using NGTools.Network;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ServerStickGhostCamera)]
	internal sealed class ServerStickGhostCameraPacket : ResponsePacket
	{
		public int	transformInstanceID;

		public	ServerStickGhostCameraPacket(int networkId) : base(networkId)
		{
		}

		private	ServerStickGhostCameraPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}