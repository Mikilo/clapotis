using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Scene_ClientRequestHierarchy)]
	internal sealed class ClientRequestHierarchyPacket : Packet, IGUIPacket
	{
		public	ClientRequestHierarchyPacket()
		{
		}

		private	ClientRequestHierarchyPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			GUILayout.Label("Requesting Hierarchy.");
		}
	}
}