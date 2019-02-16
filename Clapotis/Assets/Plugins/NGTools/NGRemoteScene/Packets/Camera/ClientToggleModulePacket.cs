using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ClientToggleModule)]
	internal sealed class ClientToggleModulePacket : Packet, IGUIPacket
	{
		public byte	moduleID;
		public bool	active;

		private string	cachedLabel;

		public	ClientToggleModulePacket(byte moduleID, bool active)
		{
			this.moduleID = moduleID;
			this.active = active;
		}

		private	ClientToggleModulePacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override bool	AggregateInto(Packet pendingPacket)
		{
			ClientToggleModulePacket	packet = pendingPacket as ClientToggleModulePacket;

			if (packet != null && packet.moduleID == this.moduleID)
			{
				packet.active = this.active;
				return true;
			}

			return false;
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
			{
				if (this.active == true)
					this.cachedLabel = "Activating camera module " + this.moduleID + '.';
				else
					this.cachedLabel = "Deactivating camera module " + this.moduleID + '.';
			}

			GUILayout.Label(this.cachedLabel);
		}
	}
}