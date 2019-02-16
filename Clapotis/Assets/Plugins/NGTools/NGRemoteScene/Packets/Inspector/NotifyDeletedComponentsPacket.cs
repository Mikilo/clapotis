using NGTools.Network;
using System.Collections.Generic;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Component_NotifyDeletedComponents)]
	internal sealed class NotifyDeletedComponentsPacket : Packet
	{
		public List<int>	gameObjectInstanceIDs;
		public List<int>	instanceIDs;

		public	NotifyDeletedComponentsPacket()
		{
			this.gameObjectInstanceIDs = new List<int>();
			this.instanceIDs = new List<int>();
		}

		public	NotifyDeletedComponentsPacket(int gameObjectInstanceID, int instanceID)
		{
			this.gameObjectInstanceIDs = new List<int>();
			this.instanceIDs = new List<int>();

			this.gameObjectInstanceIDs.Add(gameObjectInstanceID);
			this.instanceIDs.Add(instanceID);
		}

		private	NotifyDeletedComponentsPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public void	Add(int gameObjectInstanceID, int instanceID)
		{
			this.gameObjectInstanceIDs.Add(gameObjectInstanceID);
			this.instanceIDs.Add(instanceID);
		}
	}
}