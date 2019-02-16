using NGTools.Network;
using System;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Asset_ClientRequestResources)]
	internal sealed class ClientRequestResourcesPacket : Packet, IGUIPacket
	{
		public Type	type;
		public bool	forceRefresh;

		private string	cachedLabel;

		public	ClientRequestResourcesPacket(Type type, bool forceRefresh)
		{
			this.type = type;
			this.forceRefresh = forceRefresh;
		}

		private	ClientRequestResourcesPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
			{
				if (this.forceRefresh == true)
					this.cachedLabel = "Force requesting resources of type \"" + this.type.Name + "\".";
				else
					this.cachedLabel = "Requesting resources of type \"" + this.type.Name + "\".";
			}

			GUILayout.Label(this.cachedLabel);
		}
	}
}