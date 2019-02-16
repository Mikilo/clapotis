using NGTools.Network;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Material_NotifyMaterialPropertyUpdated)]
	internal sealed class NotifyMaterialPropertyUpdatedPacket : Packet
	{
		public int		instanceID;
		public string	propertyName;
		public byte[]	rawValue;

		public	NotifyMaterialPropertyUpdatedPacket(int instanceID, string propertyName, byte[] rawValue)
		{
			this.instanceID = instanceID;
			this.propertyName = propertyName;
			this.rawValue = rawValue;
		}

		private	NotifyMaterialPropertyUpdatedPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}