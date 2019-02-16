using NGTools.Network;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Class_NotifyFieldValueUpdated)]
	internal sealed class NotifyFieldValueUpdatedPacket : Packet
	{
		public string	fieldPath;
		public byte[]	rawValue;

		public	NotifyFieldValueUpdatedPacket(string fieldPath, byte[] rawValue)
		{
			this.fieldPath = fieldPath;
			this.rawValue = rawValue;
		}

		private	NotifyFieldValueUpdatedPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}