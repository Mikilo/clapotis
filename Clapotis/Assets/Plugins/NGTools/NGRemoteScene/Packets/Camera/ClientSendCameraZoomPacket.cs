using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ClientSendCameraZoom)]
	internal sealed class ClientSendCameraZoomPacket : Packet, IGUIPacket
	{
		public float	factor;

		private string	cachedLabel;

		public	ClientSendCameraZoomPacket(float factor)
		{
			this.factor = factor;
		}

		private	ClientSendCameraZoomPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
				this.cachedLabel = "Zooming ghost Camera (" + this.factor + ").";

			GUILayout.Label(this.cachedLabel);
		}
	}
}