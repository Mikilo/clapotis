using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ClientRaycastScene)]
	internal sealed class ClientRaycastScenePacket : Packet, IGUIPacket
	{
		public int		cameraID;
		public float	viewportX;
		public float	viewportY;

		private string	cachedLabel;

		public	ClientRaycastScenePacket(int cameraID, float viewportX, float viewportY)
		{
			this.cameraID = cameraID;
			this.viewportX = viewportX;
			this.viewportY = viewportY;
		}

		private	ClientRaycastScenePacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override bool	AggregateInto(Packet pendingPacket)
		{
			ClientRaycastScenePacket	packet = pendingPacket as ClientRaycastScenePacket;

			if (packet != null && (Mathf.Approximately(packet.viewportX, this.viewportX) == false || Mathf.Approximately(packet.viewportY, this.viewportY) == false))
			{
				packet.viewportX = this.viewportX;
				packet.viewportY = this.viewportY;
				return true;
			}

			return false;
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
				this.cachedLabel = "Raycasting Camera \"" + unityData.GetResourceName(typeof(Camera), this.cameraID) + '"' + (Conf.DebugMode != Conf.DebugState.None ? " (#" + this.cameraID + ')' : string.Empty) + " at viewport (" + this.viewportX + " ; " + this.viewportY + ").";

			GUILayout.Label(this.cachedLabel);
		}
	}
}