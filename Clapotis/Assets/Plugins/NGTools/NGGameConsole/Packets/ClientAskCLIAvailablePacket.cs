using NGTools.Network;
using UnityEngine;

namespace NGTools.NGGameConsole
{
	[PacketLinkTo(GameConsolePacketId.CLI_ClientAskCLIAvailable)]
	internal sealed class ClientAskCLIAvailablePacket : Packet, IGUIPacket
	{
		public	ClientAskCLIAvailablePacket()
		{
		}

		private	ClientAskCLIAvailablePacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			GUILayout.Label("Asking if a CLI is available.");
		}
	}
}