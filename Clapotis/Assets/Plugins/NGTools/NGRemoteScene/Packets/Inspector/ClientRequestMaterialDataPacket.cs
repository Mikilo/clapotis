using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Material_ClientRequestMaterialData)]
	internal sealed class ClientRequestMaterialDataPacket : Packet, IGUIPacket
	{
		public int	materialInstanceID;

		private string	cachedLabel;

		public	ClientRequestMaterialDataPacket(int instanceID)
		{
			this.materialInstanceID = instanceID;
		}

		private	ClientRequestMaterialDataPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
				this.cachedLabel = "Requesting data from Material \"" + unityData.GetResourceName(typeof(Material), this.materialInstanceID) + (Conf.DebugMode != Conf.DebugState.None ? "\" (#" + this.materialInstanceID + ")." : "\".");

			GUILayout.Label(this.cachedLabel);
		}
	}
}