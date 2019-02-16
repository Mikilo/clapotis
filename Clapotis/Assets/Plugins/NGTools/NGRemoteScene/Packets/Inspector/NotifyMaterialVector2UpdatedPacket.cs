using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Material_NotifyMaterialVector2Updated)]
	internal sealed class NotifyMaterialVector2UpdatedPacket : Packet
	{
		public int					instanceID;
		public string				propertyName;
		public Vector2				value;
		public MaterialVector2Type	type;

		public	NotifyMaterialVector2UpdatedPacket(int instanceID, string propertyName, Vector2 value, MaterialVector2Type type)
		{
			this.instanceID = instanceID;
			this.propertyName = propertyName;
			this.value = value;
			this.type = type;
		}

		private	NotifyMaterialVector2UpdatedPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}