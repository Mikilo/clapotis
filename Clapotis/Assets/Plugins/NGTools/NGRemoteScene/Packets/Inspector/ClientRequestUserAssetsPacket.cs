using NGTools.Network;
using System;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Asset_ClientRequestUserAssets)]
	internal sealed class ClientRequestUserAssetsPacket : Packet, IGUIPacket
	{
		public Type	type;

		private string	cachedLabel;

		public	ClientRequestUserAssetsPacket(Type type)
		{
			this.type = type;
		}

		private	ClientRequestUserAssetsPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
				this.cachedLabel = "Requesting user assets of type \"" + this.type + "\".";

			GUILayout.Label(this.cachedLabel);
		}
	}
}