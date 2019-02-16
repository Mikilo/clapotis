using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Material_ClientUpdateMaterialProperty, true)]
	internal sealed class ClientUpdateMaterialPropertyPacket : Packet, IGUIPacket
	{
		public int		instanceID;
		public string	propertyName;
		public byte[]	rawValue;

		private string	cachedLabel;

		public	ClientUpdateMaterialPropertyPacket(int instanceID, string propertyName, byte[] rawValue)
		{
			this.instanceID = instanceID;
			this.propertyName = propertyName;
			this.rawValue = rawValue;
		}

		private	ClientUpdateMaterialPropertyPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override bool	AggregateInto(Packet pendingPacket)
		{
			ClientUpdateMaterialPropertyPacket	packet = pendingPacket as ClientUpdateMaterialPropertyPacket;

			if (packet != null &&
				packet.instanceID == this.instanceID &&
				packet.propertyName.Equals(this.propertyName) == true)
			{
				packet.rawValue = this.rawValue;
				return true;
			}

			return false;
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
			{
				this.cachedLabel = string.Format("Updating Material {0}.{1}{2}.",
												 unityData.GetResourceName(typeof(Material), this.instanceID),
												 this.propertyName,
												 (Conf.DebugMode != Conf.DebugState.None ? " (#" + this.instanceID + ")" : string.Empty));
			}

			GUILayout.Label(this.cachedLabel);
		}
	}
}