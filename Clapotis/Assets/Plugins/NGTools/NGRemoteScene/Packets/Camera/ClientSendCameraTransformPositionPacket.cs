using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ClientSendCameraTransformPosition)]
	internal sealed class ClientSendCameraTransformPositionPacket : Packet, IGUIPacket
	{
		public Vector3	position;

		private string	cachedLabel;

		public	ClientSendCameraTransformPositionPacket(Vector3 position)
		{
			this.position = position;
		}

		private	ClientSendCameraTransformPositionPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override bool	AggregateInto(Packet pendingPacket)
		{
			ClientSendCameraTransformPositionPacket	packet = pendingPacket as ClientSendCameraTransformPositionPacket;

			if (packet != null && packet.position != this.position)
			{
				packet.position = this.position;
				return true;
			}

			return false;
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
				this.cachedLabel = "Moving ghost Camera (" + this.position + ").";

			GUILayout.Label(this.cachedLabel);
		}
	}
}