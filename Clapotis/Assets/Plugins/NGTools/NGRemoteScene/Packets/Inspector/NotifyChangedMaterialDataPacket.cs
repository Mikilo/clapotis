using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Material_NotifyMaterialDataChanged)]
	internal sealed class NotifyMaterialDataChangedPacket : Packet
	{
		public readonly Material	serverMaterial;
		public readonly NGShader	ngShader;
		public NetMaterial			netMaterial;

		public	NotifyMaterialDataChangedPacket(Material serverMaterial, NGShader ngShader)
		{
			this.serverMaterial = serverMaterial;
			this.ngShader = ngShader;
		}

		private	NotifyMaterialDataChangedPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override void	Out(ByteBuffer buffer)
		{
			buffer.Append(this.networkId);
			NetMaterial.Serialize(buffer, this.serverMaterial, this.ngShader);
		}

		public override void	In(ByteBuffer buffer)
		{
			this.networkId = buffer.ReadInt32();
			this.netMaterial = NetMaterial.Deserialize(buffer);
		}
	}
}