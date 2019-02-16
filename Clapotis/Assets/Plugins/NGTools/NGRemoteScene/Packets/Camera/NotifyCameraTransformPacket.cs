using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_NotifyCameraTransform)]
	internal sealed class NotifyCameraTransformPacket : Packet
	{
		public float	positionX;
		public float	positionY;
		public float	positionZ;
		public float	rotationX;
		public float	rotationY;

		public	NotifyCameraTransformPacket(Vector3 position, float rotationX, float rotationY)
		{
			this.positionX = position.x;
			this.positionY = position.y;
			this.positionZ = position.z;
			this.rotationX = rotationX;
			this.rotationY = rotationY;
		}

		private	NotifyCameraTransformPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}