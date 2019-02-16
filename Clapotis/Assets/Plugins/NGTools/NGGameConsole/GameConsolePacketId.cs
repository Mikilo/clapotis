using NGTools.Network;

namespace NGTools.NGGameConsole
{
	[RegisterPacketIds]
	internal static class GameConsolePacketId
	{
		public const int	Logger_ClientSubscribeLogs = 1000;
		public const int	Logger_ClientUnsubscribeLogs = 1001;
		public const int	Logger_ServerSendLog = 1002;

		public const int	CLI_ClientAskCLIAvailable = 2000;
		public const int	CLI_ServerAnswerCLIAvailable = 2001;
		public const int	CLI_ClientRequestCommandNodes = 2002;
		public const int	CLI_ServerSendCommandNodes = 2003;
		public const int	CLI_ClientSendCommand = 2004;
		public const int	CLI_ServerSendCommandResponse = 2005;
	}
}