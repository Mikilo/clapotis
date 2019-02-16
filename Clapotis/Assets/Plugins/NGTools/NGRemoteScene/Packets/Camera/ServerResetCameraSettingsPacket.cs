using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ServerResetCameraSettings)]
	internal sealed class ServerResetCameraSettingsPacket : ResponsePacket
	{
		public int		cameraID;
		public int		clearFlags = 1;
		public Color	background = Color.black;
		public int		cullingMask = -1;
		public int		projection = 0;
		public float	fieldOfView = 30F;
		public float	size = 5F;
		public float	clippingPlanesNear = 1F;
		public float	clippingPlanesFar = 1000F;
		public Rect		viewportRect = new Rect(0F, 0F, 1F, 1F);
		public float	cdepth = -1F;
		public int		renderingPath = -1;
		public bool		occlusionCulling = true;
		public bool		HDR = false;
		public int		targetDisplay = 0;

		public	ServerResetCameraSettingsPacket(int networkId) : base(networkId)
		{
		}

		private	ServerResetCameraSettingsPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}