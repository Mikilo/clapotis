namespace NGTools.Network
{
	[PacketLinkTo(PacketId.Ack)]
	public class AckPacket : ResponsePacket
	{
		public	AckPacket(int networkId) : base(networkId)
		{
		}

		public	AckPacket(int networkId, int errorCode, string errorMessage) : base(networkId, errorCode, errorMessage)
		{
		}

		protected	AckPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}