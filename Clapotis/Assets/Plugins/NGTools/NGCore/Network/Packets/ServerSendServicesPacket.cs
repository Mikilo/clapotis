namespace NGTools.Network
{
	[PacketLinkTo(PacketId.ServerSendServices)]
	internal sealed class ServerSendServicesPacket : ResponsePacket
	{
		public string[]	services;
		public string[]	versions;

		public	ServerSendServicesPacket(int networkId) : base(networkId)
		{
		}

		private	ServerSendServicesPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}