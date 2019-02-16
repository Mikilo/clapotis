using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.StaticClass_ClientRequestInspectableTypes)]
	internal sealed class ClientRequestInspectableTypesPacket : Packet, IGUIPacket
	{
		public	ClientRequestInspectableTypesPacket()
		{
		}

		private	ClientRequestInspectableTypesPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			GUILayout.Label("Requesting inspectable classes.");
		}
	}
}