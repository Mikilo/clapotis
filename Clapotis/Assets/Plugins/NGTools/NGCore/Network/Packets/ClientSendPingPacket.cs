using UnityEngine;

namespace NGTools.Network
{
	[PacketLinkTo(PacketId.ClientSendPing)]
	internal sealed class ClientSendPingPacket : Packet, IGUIPacket
	{
		public	ClientSendPingPacket()
		{
		}

		private	ClientSendPingPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			GUILayout.Label("Client send ping.");
		}
	}
}