using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Scene_ClientSetActiveScene)]
	internal sealed class ClientSetActiveScenePacket : Packet, IGUIPacket
	{
		public int	index;

		private string	cachedLabel;

		public	ClientSetActiveScenePacket(int index)
		{
			this.index = index;
		}

		private	ClientSetActiveScenePacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
				this.cachedLabel = "Setting active scene " + this.index + '.';

			GUILayout.Label(this.cachedLabel);
		}
	}
}