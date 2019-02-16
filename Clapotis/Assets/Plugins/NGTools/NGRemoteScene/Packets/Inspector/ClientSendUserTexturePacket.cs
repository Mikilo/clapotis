using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Asset_ClientSendUserTexture2D)]
	internal sealed class ClientSendUserTexture2DPacket : Packet, IGUIPacket
	{
		public string	name;
		public byte[]	raw;

		private string	cachedLabel;

		public	ClientSendUserTexture2DPacket(string name, byte[] raw)
		{
			this.name = name;
			this.raw = raw;
		}

		private	ClientSendUserTexture2DPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
				this.cachedLabel = "Sending Texture2D \"" + this.name + "\" (" + raw.Length + " bytes).";

			GUILayout.Label(this.cachedLabel);
		}
	}
}