using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Unity_ClientRequestLayers)]
	internal sealed class ClientRequestLayersPacket : Packet, IGUIPacket
	{
		public	ClientRequestLayersPacket()
		{
		}

		private	ClientRequestLayersPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			GUILayout.Label("Requesting Layers.");
		}
	}
}