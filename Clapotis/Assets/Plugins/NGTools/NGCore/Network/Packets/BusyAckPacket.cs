namespace NGTools.Network
{
	[PacketLinkTo(PacketId.BusyAck)]
	public class BusyAckPacket : ResponsePacket
	{
		public	BusyAckPacket(int networkId) : base(networkId)
		{
		}

		protected	BusyAckPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}