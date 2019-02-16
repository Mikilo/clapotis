using NGTools.Network;
using UnityEngine;

namespace NGTools.NGGameConsole
{
	[PacketLinkTo(GameConsolePacketId.CLI_ClientRequestCommandNodes)]
	internal sealed class ClientRequestCommandNodesPacket : Packet, IGUIPacket
	{
		public	ClientRequestCommandNodesPacket()
		{
		}

		private	ClientRequestCommandNodesPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			GUILayout.Label("Requesting command nodes.");
		}
	}
}