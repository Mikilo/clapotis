using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Material_ServerUpdateMaterialVector2)]
	internal sealed class ServerUpdateMaterialVector2Packet : ResponsePacket
	{
		public int					instanceID;
		public string				propertyName;
		public MaterialVector2Type	type;
		public Vector2				value;

		public	ServerUpdateMaterialVector2Packet(int networkId) : base(networkId)
		{
		}

		private	ServerUpdateMaterialVector2Packet(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}