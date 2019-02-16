namespace NGTools.Network
{
	[PacketLinkTo(PacketId.ServerAnswerPing)]
	internal sealed class ServerAnswerPingPacket : ResponsePacket
	{
		public	ServerAnswerPingPacket(int networkId) : base(networkId)
		{
		}

		private	ServerAnswerPingPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}