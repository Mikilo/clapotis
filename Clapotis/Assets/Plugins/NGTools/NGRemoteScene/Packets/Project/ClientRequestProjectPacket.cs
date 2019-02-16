using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Asset_ClientRequestProject)]
	internal sealed class ClientRequestProjectPacket : Packet, IGUIPacket
	{
		public	ClientRequestProjectPacket()
		{
		}

		private	ClientRequestProjectPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			GUILayout.Label("Requesting Project.");
		}
	}
}