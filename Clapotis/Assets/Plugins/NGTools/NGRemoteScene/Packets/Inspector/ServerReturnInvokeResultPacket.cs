using NGTools.Network;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Component_ServerReturnInvokeResult)]
	internal sealed class ServerReturnInvokeResultPacket : ResponsePacket
	{
		public string	result;

		public	ServerReturnInvokeResultPacket(int networkId) : base(networkId)
		{
		}

		private	ServerReturnInvokeResultPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}