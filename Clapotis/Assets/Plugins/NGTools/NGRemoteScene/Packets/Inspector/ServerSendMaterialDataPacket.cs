using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Material_ServerSendMaterialData)]
	internal sealed class ServerSendMaterialDataPacket : ResponsePacket
	{
		public Material		serverMaterial;
		public NGShader		ngShader;
		public NetMaterial	netMaterial;

		public	ServerSendMaterialDataPacket(int networkId) : base(networkId)
		{
		}

		private	ServerSendMaterialDataPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override void	Out(ByteBuffer buffer)
		{
			if (this.OutResponseStatus(buffer) == true)
				NetMaterial.Serialize(buffer, this.serverMaterial, this.ngShader);
		}

		public override void	In(ByteBuffer buffer)
		{
			if (this.InResponseStatus(buffer) == true)
				this.netMaterial = NetMaterial.Deserialize(buffer);
		}
	}
}