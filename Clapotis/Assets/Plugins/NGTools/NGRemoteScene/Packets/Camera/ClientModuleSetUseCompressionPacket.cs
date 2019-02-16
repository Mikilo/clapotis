using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ClientModuleSetUseCompression)]
	internal sealed class ClientModuleSetUseCompressionPacket : Packet, IGUIPacket
	{
		public bool	useCompression;

		public	ClientModuleSetUseCompressionPacket(bool useCompression)
		{
			this.useCompression = useCompression;
		}

		private	ClientModuleSetUseCompressionPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override bool	AggregateInto(Packet pendingPacket)
		{
			ClientModuleSetUseCompressionPacket	packet = pendingPacket as ClientModuleSetUseCompressionPacket;

			if (packet != null)
			{
				packet.useCompression = this.useCompression;
				return true;
			}

			return false;
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.useCompression == true)
				GUILayout.Label("Enabling Camera compression.");
			else
				GUILayout.Label("Disabling Camera compression.");
		}
	}
}