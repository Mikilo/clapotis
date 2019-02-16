namespace NGTools.Network
{
	[PacketLinkTo(PacketId.ServerHasDisconnect)]
	internal sealed class ServerHasDisconnectedPacket : Packet
	{
		public	ServerHasDisconnectedPacket()
		{
		}

		private	ServerHasDisconnectedPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}