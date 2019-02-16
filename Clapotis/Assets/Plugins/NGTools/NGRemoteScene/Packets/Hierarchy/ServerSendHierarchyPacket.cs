using NGTools.Network;
using System.Collections.Generic;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Scene_ServerSendHierarchy)]
	internal sealed class ServerSendHierarchyPacket : ResponsePacket
	{
		public List<ServerScene>	serverScenes;
		public int					activeScene;
		public NetScene[]			clientScenes;

		public	ServerSendHierarchyPacket(int networkId) : base(networkId)
		{
		}

		private	ServerSendHierarchyPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override void	Out(ByteBuffer buffer)
		{
			if (this.OutResponseStatus(buffer) == true)
			{
				buffer.Append(this.activeScene);
				buffer.Append(this.serverScenes.Count);

				for (int i = 0; i < this.serverScenes.Count; i++)
					NetScene.Serialize(this.serverScenes[i], buffer);
			}
		}

		public override void	In(ByteBuffer buffer)
		{
			if (this.InResponseStatus(buffer) == true)
			{
				this.activeScene = buffer.ReadInt32();

				int	length = buffer.ReadInt32();

				this.clientScenes = new NetScene[length];

				for (int i = 0; i < length; i++)
					this.clientScenes[i] = NetScene.Deserialize(buffer);
			}
		}
	}
}