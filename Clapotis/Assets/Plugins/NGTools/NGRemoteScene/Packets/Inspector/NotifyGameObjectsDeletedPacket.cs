using NGTools.Network;
using System.Collections.Generic;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.GameObject_NotifyGameObjectsDeleted)]
	internal sealed class NotifyGameObjectsDeletedPacket : Packet
	{
		public List<int>	instanceIDs;

		public	NotifyGameObjectsDeletedPacket()
		{
			this.instanceIDs = new List<int>();
		}

		public	NotifyGameObjectsDeletedPacket(int instanceID)
		{
			this.instanceIDs = new List<int>();
			this.instanceIDs.Add(instanceID);
		}

		private	NotifyGameObjectsDeletedPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override bool	AggregateInto(Packet pendingPacket)
		{
			NotifyGameObjectsDeletedPacket	packet = pendingPacket as NotifyGameObjectsDeletedPacket;

			if (packet != null)
			{
				for (int i = 0; i < this.instanceIDs.Count; i++)
				{
					if (packet.instanceIDs.Contains(this.instanceIDs[i]) == false)
						packet.instanceIDs.Add(this.instanceIDs[i]);
				}

				return true;
			}

			return false;
		}
	}
}