using NGTools.Network;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Material_ServerUpdateMaterialProperty)]
	internal sealed class ServerUpdateMaterialPropertyPacket : ResponsePacket
	{
		public int		materialInstanceID;
		public string	propertyName;
		public byte[]	rawValue;

		public	ServerUpdateMaterialPropertyPacket(int networkId) : base(networkId)
		{
		}

		private	ServerUpdateMaterialPropertyPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}