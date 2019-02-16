using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ClientPickGhostCameraAtCamera)]
	internal sealed class ClientPickGhostCameraAtCameraPacket : Packet, IGUIPacket
	{
		public int	cameraID;

		private string	cachedLabel;

		public	ClientPickGhostCameraAtCameraPacket(int cameraID)
		{
			this.cameraID = cameraID;
		}

		private	ClientPickGhostCameraAtCameraPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
				this.cachedLabel = "Picking ghost Camera at the position of Camera \"" + unityData.GetResourceName(typeof(Camera), this.cameraID) + (Conf.DebugMode != Conf.DebugState.None ? "\" (#" + this.cameraID + ")." : "\".");

			GUILayout.Label(this.cachedLabel);
		}
	}
}