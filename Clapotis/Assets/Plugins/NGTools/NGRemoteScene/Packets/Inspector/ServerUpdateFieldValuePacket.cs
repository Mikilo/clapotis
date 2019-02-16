using NGTools.Network;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Class_ServerUpdateFieldValue)]
	internal sealed class ServerUpdateFieldValuePacket : ResponsePacket
	{
		public string	fieldPath;
		public byte[]	rawValue;

		public	ServerUpdateFieldValuePacket(int networkId) : base(networkId)
		{
		}

		private	ServerUpdateFieldValuePacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}