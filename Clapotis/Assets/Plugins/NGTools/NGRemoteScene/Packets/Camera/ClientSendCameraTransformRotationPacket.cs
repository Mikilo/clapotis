using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ClientSendCameraTransformRotation)]
	internal sealed class ClientSendCameraTransformRotationPacket : Packet, IGUIPacket
	{
		public Vector2	rotation;

		private string	cachedLabel;

		public	ClientSendCameraTransformRotationPacket(Vector2 rotation)
		{
			this.rotation = rotation;
		}

		private	ClientSendCameraTransformRotationPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override bool	AggregateInto(Packet pendingPacket)
		{
			ClientSendCameraTransformRotationPacket	packet = pendingPacket as ClientSendCameraTransformRotationPacket;

			if (packet != null && packet.rotation != this.rotation)
			{
				packet.rotation = this.rotation;
				return true;
			}

			return false;
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
				this.cachedLabel = "Rotating ghost Camera (" + this.rotation + ").";

			GUILayout.Label(this.cachedLabel);
		}
	}
}