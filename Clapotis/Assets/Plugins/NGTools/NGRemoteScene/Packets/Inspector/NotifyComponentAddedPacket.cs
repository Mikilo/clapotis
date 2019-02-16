using NGTools.Network;

namespace NGTools.NGRemoteScene
{
	/// <summary>Sends all fields and properties from a Component + enable.</summary>
	[PacketLinkTo(RemoteScenePacketId.GameObject_NotifyComponentAdded)]
	internal sealed class NotifyComponentAddedPacket : Packet
	{
		public readonly ServerComponent	serverComponent;
		public int						gameObjectInstanceID;
		public NetComponent				component;

		public	NotifyComponentAddedPacket(int gameObjectInstanceID, ServerComponent serverComponent)
		{
			this.gameObjectInstanceID = gameObjectInstanceID;
			this.serverComponent = serverComponent;
		}

		private	NotifyComponentAddedPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override void	Out(ByteBuffer buffer)
		{
			buffer.Append(this.networkId);
			buffer.Append(this.gameObjectInstanceID);
			NetComponent.Serialize(buffer, this.serverComponent);
		}

		public override void	In(ByteBuffer buffer)
		{
			this.networkId = buffer.ReadInt32();
			this.gameObjectInstanceID = buffer.ReadInt32();
			this.component = NetComponent.Deserialize(buffer);
		}
	}
}