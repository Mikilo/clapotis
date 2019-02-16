using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ClientSendCameraInput)]
	internal sealed class ClientSendCameraInputPacket : Packet, IGUIPacket
	{
		/// <summary>Mask defining directions (forward 1, backward 2, left 4, right 8).</summary>
		public byte		directions;
		public float	speed;

		private string	cachedLabel;

		public	ClientSendCameraInputPacket(bool forward, bool backward, bool left, bool right, float speed)
		{
			this.directions = (byte)((forward == true ? (1 << 0) : 0) |
									 (backward == true ? (1 << 1) : 0) |
									 (left == true ? (1 << 2) : 0) |
									 (right == true ? (1 << 3) : 0));
			this.speed = speed;
		}

		private	ClientSendCameraInputPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override bool	AggregateInto(Packet pendingPacket)
		{
			ClientSendCameraInputPacket	packet = pendingPacket as ClientSendCameraInputPacket;

			if (packet != null &&
				(packet.directions != this.directions ||
				 Mathf.Approximately(packet.speed, this.speed) == false))
			{
				packet.directions = this.directions;
				packet.speed = this.speed;
				return true;
			}

			return false;
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
				this.cachedLabel = "Moving ghost Camera (" + ((this.directions & (1 << 1)) != 0 ? "Forward " : "") + ((this.directions & (1 << 1)) != 0 ? "Backward " : "") + ((this.directions & (1 << 2)) != 0 ? "Left " : "") + ((this.directions & (1 << 3)) != 0 ? "Right " : "") + " x" + this.speed + ").";

			GUILayout.Label(this.cachedLabel);
		}
	}
}