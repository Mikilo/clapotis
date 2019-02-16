using NGTools.Network;
using System.Collections.Generic;

namespace NGTools.NGRemoteScene
{
	/// <summary>
	/// <para>Sends all primary data of a GameObject.</para>
	/// </summary>
	/// <seealso cref="NGTools.ClientRequestGameObjectDataPacket"/>
	[PacketLinkTo(RemoteScenePacketId.GameObject_ServerSendGameObjectData)]
	internal sealed class ServerSendGameObjectDataPacket : ResponsePacket
	{
		public List<ServerGameObject>	serverGameObjects;
		public NetGameObjectData[]		gameObjectData;

		public	ServerSendGameObjectDataPacket(int networkId) : base(networkId)
		{
			this.serverGameObjects = new List<ServerGameObject>();
		}

		private	ServerSendGameObjectDataPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override void	Out(ByteBuffer buffer)
		{
			if (this.OutResponseStatus(buffer) == true)
			{
				buffer.Append(this.serverGameObjects.Count);

				for (int i = 0; i < this.serverGameObjects.Count; i++)
					NetGameObjectData.Serialize(buffer, this.serverGameObjects[i]);
			}
		}

		public override void	In(ByteBuffer buffer)
		{
			if (this.InResponseStatus(buffer) == true)
			{
				this.gameObjectData = new NetGameObjectData[buffer.ReadInt32()];

				for (int i = 0; i < this.gameObjectData.Length; i++)
					this.gameObjectData[i] = NetGameObjectData.Deserialize(buffer);
			}
		}
	}
}