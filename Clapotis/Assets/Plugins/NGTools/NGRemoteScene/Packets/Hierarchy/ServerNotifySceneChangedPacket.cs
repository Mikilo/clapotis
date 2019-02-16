using NGTools.Network;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Scene_NotifySceneChanged)]
	internal sealed class ServerNotifySceneChangedPacket : Packet
	{
		public	ServerNotifySceneChangedPacket()
		{
		}

		private	ServerNotifySceneChangedPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}