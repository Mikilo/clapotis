using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Unity_ServerSendLayers)]
	internal sealed class ServerSendLayersPacket : ResponsePacket
	{
		public const int	MaxLayers = 32;

		public string[]	layers;

		public	ServerSendLayersPacket(int networkId) : base(networkId)
		{
		}

		private	ServerSendLayersPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override void	Out(ByteBuffer buffer)
		{
			if (this.OutResponseStatus(buffer) == true)
			{
				for (int i = 0; i < ServerSendLayersPacket.MaxLayers; i++)
					buffer.AppendUnicodeString(LayerMask.LayerToName(i));
			}
		}

		public override void	In(ByteBuffer buffer)
		{
			if (this.InResponseStatus(buffer) == true)
			{
				this.layers = new string[ServerSendLayersPacket.MaxLayers];

				for (int i = 0; i < ServerSendLayersPacket.MaxLayers; i++)
					this.layers[i] = buffer.ReadUnicodeString();
			}
		}
	}
}