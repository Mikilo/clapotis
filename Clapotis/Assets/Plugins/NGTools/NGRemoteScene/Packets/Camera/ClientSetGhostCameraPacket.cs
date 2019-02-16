using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ClientSetGhostCamera)]
	internal sealed class ClientSetGhostCameraPacket : Packet, IGUIPacket
	{
		public int		cameraID;
		public int[]	componentsID;
		public bool[]	componentsIncluded;

		public	ClientSetGhostCameraPacket(int modelCameraID, int[] componentsID, bool[] componentsIncluded)
		{
			this.cameraID = modelCameraID;
			this.componentsID = componentsID;
			this.componentsIncluded = componentsIncluded;
		}

		private	ClientSetGhostCameraPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			GUILayout.Label("Setting ghost Camera.");
		}
	}
}