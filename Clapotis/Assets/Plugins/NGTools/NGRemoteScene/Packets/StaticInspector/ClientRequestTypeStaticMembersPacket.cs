using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.StaticClass_ClientRequestTypeStaticMembers)]
	internal sealed class ClientRequestTypeStaticMembersPacket : Packet, IGUIPacket
	{
		public int	typeIndex;

		private string	cachedLabel;

		public	ClientRequestTypeStaticMembersPacket(int type)
		{
			this.typeIndex = type;
		}

		private	ClientRequestTypeStaticMembersPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
				this.cachedLabel = "Requesting static members from class " + this.typeIndex + '.';

			GUILayout.Label(this.cachedLabel);
		}
	}
}