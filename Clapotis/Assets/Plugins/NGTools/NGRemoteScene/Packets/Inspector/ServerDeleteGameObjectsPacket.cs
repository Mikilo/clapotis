using NGTools.Network;
using System.Collections.Generic;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.GameObject_ServerDeleteGameObjects)]
	internal sealed class ServerDeleteGameObjectsPacket : ResponsePacket
	{
		public List<int>	gameObjectInstanceIDs;

		public	ServerDeleteGameObjectsPacket(int networkId) : base(networkId)
		{
			this.gameObjectInstanceIDs = new List<int>();
		}

		private	ServerDeleteGameObjectsPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}