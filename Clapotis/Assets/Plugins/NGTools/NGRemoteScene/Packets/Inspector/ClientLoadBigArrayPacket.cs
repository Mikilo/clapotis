using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Class_ClientLoadBigArray)]
	internal sealed class ClientLoadBigArrayPacket : Packet, IGUIPacket
	{
		public string	arrayPath;

		private string	cachedLabel;

		public	ClientLoadBigArrayPacket(string arrayPath)
		{
			this.arrayPath = arrayPath;
		}

		private	ClientLoadBigArrayPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
				this.cachedLabel = "Loading big array at \"" + this.arrayPath + "\".";

			GUILayout.Label(this.cachedLabel);
		}
	}
}