using NGTools.Network;
using System;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Asset_ClientRequestRawAsset)]
	internal sealed class ClientRequestRawAssetPacket : Packet, IGUIPacket, IPotentialMemoryDemandingPacket
	{
		public Type	type;
		public int	instanceID;

		private string	cachedLabel;

		public	ClientRequestRawAssetPacket(Type type, int instanceID)
		{
			this.type = type;
			this.instanceID = instanceID;
		}

		private	ClientRequestRawAssetPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
				this.cachedLabel = "Requesting " + this.type.Name + " \"" + unityData.GetResourceName(this.type, this.instanceID) + '"' + (Conf.DebugMode != Conf.DebugState.None ? " (#" + this.instanceID + ")." : ".");

			GUILayout.Label(this.cachedLabel);
		}
	}
}