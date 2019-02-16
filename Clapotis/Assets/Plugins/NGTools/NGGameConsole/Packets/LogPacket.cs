using NGTools.Network;
using UnityEngine;

namespace NGTools.NGGameConsole
{
	[PacketLinkTo(GameConsolePacketId.Logger_ServerSendLog)]
	internal sealed class LogPacket : Packet
	{
		public string	condition;
		public string	stackTrace;
		public LogType	logType;

		public	LogPacket(string condition, string stackTrace, LogType logType)
		{
			this.condition = condition;
			this.stackTrace = stackTrace;
			this.logType = logType;
		}

		private	LogPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}