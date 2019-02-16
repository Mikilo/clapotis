using NGTools.Network;
using System;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Material_ClientUpdateMaterialVector2, true)]
	internal sealed class ClientUpdateMaterialVector2Packet : Packet, IGUIPacket
	{
		public int					instanceID;
		public string				propertyName;
		public Vector2				value;
		public MaterialVector2Type	type;

		private string	cachedLabel;

		public	ClientUpdateMaterialVector2Packet(int instanceID, string propertyName, Vector2 value, MaterialVector2Type type)
		{
			this.instanceID = instanceID;
			this.propertyName = propertyName;
			this.value = value;
			this.type = type;
		}

		private	ClientUpdateMaterialVector2Packet(ByteBuffer buffer) : base(buffer)
		{
		}

		public override bool	AggregateInto(Packet pendingPacket)
		{
			ClientUpdateMaterialVector2Packet	packet = pendingPacket as ClientUpdateMaterialVector2Packet;

			if (packet != null &&
				packet.instanceID == this.instanceID &&
				packet.type == this.type &&
				packet.propertyName.Equals(this.propertyName) == true)
			{
				packet.value = this.value;
				return true;
			}

			return false;
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
			{
				this.cachedLabel = string.Format("Updating Material {0}.{1}.{2} ({3}){4}.",
												 unityData.GetResourceName(typeof(Material), this.instanceID),
												 this.propertyName,
												 Enum.GetName(typeof(MaterialVector2Type), this.type),
												 this.value,
												 (Conf.DebugMode != Conf.DebugState.None ? " (#" + this.instanceID + ")" : string.Empty));
			}

			GUILayout.Label(this.cachedLabel);
		}
	}
}