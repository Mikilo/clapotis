using NGTools.Network;
using UnityEngine;

namespace NGTools.NGGameConsole
{
	[PacketLinkTo(GameConsolePacketId.CLI_ServerAnswerCLIAvailable)]
	internal sealed class ServerAnswerCLIAvailablePacket : ResponsePacket
	{
		public bool	hasCLI;

		public	ServerAnswerCLIAvailablePacket(int networkId) : base(networkId)
		{
		}

		private	ServerAnswerCLIAvailablePacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}