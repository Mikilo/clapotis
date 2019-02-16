using NGTools.Network;
using UnityEngine;

namespace NGTools.NGGameConsole
{
	[PacketLinkTo(GameConsolePacketId.Logger_ClientSubscribeLogs)]
	internal sealed class ClientSubscribeLogsPacket : Packet, IGUIPacket
	{
		public	ClientSubscribeLogsPacket()
		{
		}

		private	ClientSubscribeLogsPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			GUILayout.Label("Subscribing logs.");
		}
	}
}