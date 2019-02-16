using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Transform_ClientSetSibling)]
	internal sealed class ClientSetSiblingPacket : Packet, IGUIPacket
	{
		public int	instanceID;
		public int	instanceIDParent;
		public int	siblingIndex;

		private string	cachedLabel;

		public	ClientSetSiblingPacket(int instanceID, int instanceIDParent, int siblingIndex)
		{
			this.instanceID = instanceID;
			this.instanceIDParent = instanceIDParent;
			this.siblingIndex = siblingIndex;
		}

		private	ClientSetSiblingPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
				this.cachedLabel = "Moving GameObject \"" + unityData.GetGameObjectName(this.instanceID) + "\" " + (Conf.DebugMode != Conf.DebugState.None ? " (#" + this.instanceID + ")" : "") + " into \"" + unityData.GetGameObjectName(this.instanceIDParent) + "\" " + (Conf.DebugMode != Conf.DebugState.None ? " (#" + this.instanceIDParent + ")" : "") + " at position \"" + this.siblingIndex + "\".";

			GUILayout.Label(this.cachedLabel);
		}
	}
}