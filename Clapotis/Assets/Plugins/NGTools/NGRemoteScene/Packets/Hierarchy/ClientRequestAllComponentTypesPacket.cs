using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Unity_ClientRequestAllComponentTypes)]
	internal sealed class ClientRequestAllComponentTypesPacket : Packet, IGUIPacket
	{
		public	ClientRequestAllComponentTypesPacket()
		{
		}

		private	ClientRequestAllComponentTypesPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			GUILayout.Label("Requesting all Behaviour C# Types.");
		}
	}
}