using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ClientResetCameraSettings)]
	internal sealed class ClientResetCameraSettingsPacket : Packet, IGUIPacket
	{
		public int	cameraID;

		private string	cachedLabel;

		public	ClientResetCameraSettingsPacket(int cameraID)
		{
			this.cameraID = cameraID;
		}

		private	ClientResetCameraSettingsPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
				this.cachedLabel = "Resetting Camera settings on \"" + unityData.GetResourceName(typeof(Camera), this.cameraID) + '"' + (Conf.DebugMode != Conf.DebugState.None ? " (#" + this.cameraID + ")." : ".");

			GUILayout.Label(this.cachedLabel);
		}
	}
}