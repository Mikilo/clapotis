using NGTools.Network;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_NotifyCameraData)]
	internal sealed class NotifyCameraDataPacket : Packet
	{
		public byte		moduleID;
		public float	time;
		public byte[]	data;

		public	NotifyCameraDataPacket(byte moduleID, float time, byte[] data)
		{
			this.moduleID = moduleID;
			this.time = time;
			this.data = data;
		}

		private	NotifyCameraDataPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}