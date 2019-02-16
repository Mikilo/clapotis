using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.GameObject_ClientAddComponent)]
	internal sealed class ClientAddComponentPacket : Packet, IGUIPacket
	{
		public int		gameObjectInstanceID;
		public string	componentType;

		private string	cachedLabel;

		public	ClientAddComponentPacket(int gameObjectInstanceID, string componentType)
		{
			this.gameObjectInstanceID = gameObjectInstanceID;
			this.componentType = componentType;
		}

		private	ClientAddComponentPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
			{
				this.cachedLabel = string.Format("Adding Component \"{0}\" to {1}{2}.",
												 this.componentType,
												 unityData.GetResourceName(typeof(GameObject), this.gameObjectInstanceID),
												 (Conf.DebugMode != Conf.DebugState.None ? " (#" + this.gameObjectInstanceID + ")" : string.Empty));
			}

			GUILayout.Label(this.cachedLabel);
		}
	}
}