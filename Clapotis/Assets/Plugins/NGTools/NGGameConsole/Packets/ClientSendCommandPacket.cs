using NGTools.Network;
using UnityEngine;

namespace NGTools.NGGameConsole
{
	[PacketLinkTo(GameConsolePacketId.CLI_ClientSendCommand)]
	internal sealed class ClientSendCommandPacket : Packet, IGUIPacket
	{
		public int		requestId;
		public string	command;

		public	ClientSendCommandPacket(int requestId, string command)
		{
			this.requestId = requestId;
			this.command = command;
		}

		private	ClientSendCommandPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			GUILayout.Label("Sending command (" + this.requestId + ") \"" + this.command + "\".");
		}
	}
}