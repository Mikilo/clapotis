using NGTools.Network;

namespace NGTools.NGGameConsole
{
	[PacketLinkTo(GameConsolePacketId.CLI_ServerSendCommandNodes)]
	internal sealed class ServerSendCommandNodesPacket : ResponsePacket
	{
		public RemoteCommand	root;

		public	ServerSendCommandNodesPacket(int networkId) : base(networkId)
		{
		}

		private	ServerSendCommandNodesPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override void	Out(ByteBuffer buffer)
		{
			if (this.OutResponseStatus(buffer) == true)
				this.BrowseOut(buffer, this.root);
		}

		public override void	In(ByteBuffer buffer)
		{
			if (this.InResponseStatus(buffer) == true)
				this.root = this.BrowseIn(buffer);
		}

		private void	BrowseOut(ByteBuffer buffer, CommandNode node)
		{
			buffer.AppendUnicodeString(node.name);
			buffer.AppendUnicodeString(node.description);
			buffer.Append(node.IsLeaf);
			buffer.Append(node.children.Count);

			for (int i = 0; i < node.children.Count; i++)
				this.BrowseOut(buffer, node.children[i]);
		}

		private RemoteCommand	BrowseIn(ByteBuffer buffer)
		{
			return new RemoteCommand(buffer);
		}
	}
}