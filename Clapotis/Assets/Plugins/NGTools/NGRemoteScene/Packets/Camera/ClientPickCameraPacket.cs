using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ClientPickCamera)]
	internal sealed class ClientPickCameraPacket : Packet, IGUIPacket
	{
		public int	cameraID;

		private string	cachedLabel;

		public	ClientPickCameraPacket(int cameraID)
		{
			this.cameraID = cameraID;
		}

		private	ClientPickCameraPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
				this.cachedLabel = "Picking Camera \"" + unityData.GetResourceName(typeof(Camera), this.cameraID) + (Conf.DebugMode != Conf.DebugState.None ? "\" (#" + this.cameraID + ")." : "\".");

			GUILayout.Label(this.cachedLabel);
		}
	}
}