using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ClientModuleSetUseJPG)]
	internal sealed class ClientModuleSetUseJPGPacket : Packet, IGUIPacket
	{
		public bool	useJPG;

		public	ClientModuleSetUseJPGPacket(bool useJPG)
		{
			this.useJPG = useJPG;
		}

		private	ClientModuleSetUseJPGPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override bool	AggregateInto(Packet pendingPacket)
		{
			ClientModuleSetUseJPGPacket	packet = pendingPacket as ClientModuleSetUseJPGPacket;

			if (packet != null)
			{
				packet.useJPG = this.useJPG;
				return true;
			}

			return false;
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.useJPG == true)
				GUILayout.Label("Selecting Camera JPG format.");
			else
				GUILayout.Label("Selecting Camera PNG format.");
		}
	}
}