using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ClientRequestAllCameras)]
	internal sealed class ClientRequestAllCamerasPacket : Packet, IGUIPacket
	{
		public	ClientRequestAllCamerasPacket()
		{
		}

		private	ClientRequestAllCamerasPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			GUILayout.Label("Requesting all Camera.");
		}
	}
}