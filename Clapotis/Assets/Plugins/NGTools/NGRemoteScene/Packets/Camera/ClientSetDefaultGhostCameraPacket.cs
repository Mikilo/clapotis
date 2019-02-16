using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ClientSetDefaultGhostCamera)]
	internal sealed class ClientSetDefaultGhostCameraPacket : Packet, IGUIPacket
	{
		public	ClientSetDefaultGhostCameraPacket()
		{
		}

		private	ClientSetDefaultGhostCameraPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			GUILayout.Label("Setting default ghost Camera.");
		}
	}
}