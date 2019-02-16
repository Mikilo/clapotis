using NGTools.Network;
using UnityEngine;

namespace NGTools.NGGameConsole
{
	[PacketLinkTo(GameConsolePacketId.Logger_ClientUnsubscribeLogs)]
	internal sealed class ClientUnsubscribeLogsPacket : Packet, IGUIPacket
	{
		public	ClientUnsubscribeLogsPacket()
		{
		}

		private	ClientUnsubscribeLogsPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			GUILayout.Label("Unsubscribing logs.");
		}
	}
}