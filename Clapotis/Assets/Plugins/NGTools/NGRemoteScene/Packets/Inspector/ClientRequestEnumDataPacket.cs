using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Unity_ClientRequestEnumData)]
	internal sealed class ClientRequestEnumDataPacket : Packet, IGUIPacket
	{
		public string	type;

		private string	cachedLabel;

		public	ClientRequestEnumDataPacket(string type)
		{
			this.type = type;
		}

		private	ClientRequestEnumDataPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
				this.cachedLabel = "Requesting Enum of type \"" + this.type + "\".";

			GUILayout.Label(this.cachedLabel);
		}
	}
}