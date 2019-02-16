using NGTools.Network;

namespace NGTools.NGRemoteScene
{
	/// <summary>Sends all fields and properties from a Component + enable.</summary>
	[PacketLinkTo(RemoteScenePacketId.GameObject_ServerAddedComponent)]
	internal sealed class ServerAddedComponentPacket : ResponsePacket
	{
		public ServerComponent	serverComponent;
		public int				gameObjectInstanceID;
		public NetComponent		component;

		public	ServerAddedComponentPacket(int networkId) : base(networkId)
		{
		}

		private	ServerAddedComponentPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override void	Out(ByteBuffer buffer)
		{
			if (this.OutResponseStatus(buffer) == true)
			{
				buffer.Append(this.gameObjectInstanceID);
				NetComponent.Serialize(buffer, this.serverComponent);
			}
		}

		public override void	In(ByteBuffer buffer)
		{
			if (this.InResponseStatus(buffer) == true)
			{
				this.gameObjectInstanceID = buffer.ReadInt32();
				this.component = NetComponent.Deserialize(buffer);
			}
		}
	}
}