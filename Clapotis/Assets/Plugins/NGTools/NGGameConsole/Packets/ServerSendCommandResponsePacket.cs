using NGTools.Network;

namespace NGTools.NGGameConsole
{
	[PacketLinkTo(GameConsolePacketId.CLI_ServerSendCommandResponse)]
	internal sealed class ServerSendCommandResponsePacket : ResponsePacket
	{
		public int			requestId;
		public ExecResult	returnValue;
		public string		response;

		public	ServerSendCommandResponsePacket(int networkId) : base(networkId)
		{
		}

		private	ServerSendCommandResponsePacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}