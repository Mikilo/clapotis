using UnityEngine;

namespace NGTools.Network
{
	[PacketLinkTo(PacketId.ClientHasDisconnect)]
	internal sealed class ClientHasDisconnectedPacket : Packet, IGUIPacket
	{
		public	ClientHasDisconnectedPacket()
		{
		}

		private	ClientHasDisconnectedPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			GUILayout.Label("Disconnecting client.");
		}
	}
}