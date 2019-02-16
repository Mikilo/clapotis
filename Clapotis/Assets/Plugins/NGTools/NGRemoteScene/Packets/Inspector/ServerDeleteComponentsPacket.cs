using NGTools.Network;
using System.Collections.Generic;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Component_ServerDeleteComponents)]
	internal sealed class ServerDeleteComponentsPacket : ResponsePacket
	{
		public List<int>	gameObjectInstanceIDs;
		public List<int>	instanceIDs;

		public	ServerDeleteComponentsPacket(int networkId) : base(networkId)
		{
			this.gameObjectInstanceIDs = new List<int>();
			this.instanceIDs = new List<int>();
		}

		public	ServerDeleteComponentsPacket(int networkId, int gameObjectInstanceID, int instanceID) : base(networkId)
		{
			this.gameObjectInstanceIDs = new List<int>();
			this.instanceIDs = new List<int>();

			this.gameObjectInstanceIDs.Add(gameObjectInstanceID);
			this.instanceIDs.Add(instanceID);
		}

		private	ServerDeleteComponentsPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public void	Add(int gameObjectInstanceID, int instanceID)
		{
			this.gameObjectInstanceIDs.Add(gameObjectInstanceID);
			this.instanceIDs.Add(instanceID);
		}
	}
}