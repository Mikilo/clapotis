using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ClientStickGhostCamera)]
	internal sealed class ClientStickGhostCameraPacket : Packet, IGUIPacket
	{
		public int	instanceID;
		public bool	isGameObject;

		private string	cachedLabel;

		public	ClientStickGhostCameraPacket(int ID, bool isGameObject)
		{
			this.instanceID = ID;
			this.isGameObject = isGameObject;
		}

		private	ClientStickGhostCameraPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
				this.cachedLabel = "Sticking ghost Camera to \"" + (this.isGameObject == false ? unityData.GetResourceName(typeof(Transform), this.instanceID) : unityData.GetResourceName(typeof(GameObject), this.instanceID)) + (Conf.DebugMode != Conf.DebugState.None ? "\" (#" + this.instanceID + ")." : "\".");

			GUILayout.Label(this.cachedLabel);
		}
	}
}