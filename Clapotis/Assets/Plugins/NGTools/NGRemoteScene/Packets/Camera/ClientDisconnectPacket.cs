using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ClientDisconnect)]
	internal sealed class ClientDisconnectPacket : Packet, IGUIPacket
	{
		public	ClientDisconnectPacket()
		{
		}

		private	ClientDisconnectPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			GUILayout.Label("Disconnecting NG Camera.");
		}
	}
}