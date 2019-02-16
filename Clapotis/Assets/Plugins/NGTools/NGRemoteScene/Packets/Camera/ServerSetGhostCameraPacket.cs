using NGTools.Network;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ServerSetGhostCamera)]
	internal sealed class ServerSetGhostCameraPacket : ResponsePacket
	{
		public int	ghostCameraID;

		public	ServerSetGhostCameraPacket(int networkId) : base(networkId)
		{
		}

		private	ServerSetGhostCameraPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}