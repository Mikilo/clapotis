using NGTools.Network;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ServerSendRaycastResult)]
	internal sealed class ServerSendRaycastResultPacket : ResponsePacket
	{
		public int[]	instanceIDs;
		public string[]	names;

		public	ServerSendRaycastResultPacket(int networkId) : base(networkId)
		{
		}

		private	ServerSendRaycastResultPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}