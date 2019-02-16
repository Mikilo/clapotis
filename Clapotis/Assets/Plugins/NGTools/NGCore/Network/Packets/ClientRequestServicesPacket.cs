using UnityEngine;

namespace NGTools.Network
{
	[PacketLinkTo(PacketId.ClientRequestServices)]
	internal sealed class ClientRequestServicesPacket : Packet, IGUIPacket
	{
		public	ClientRequestServicesPacket()
		{
		}

		private	ClientRequestServicesPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			GUILayout.Label("Requesting Version.");
		}
	}
}