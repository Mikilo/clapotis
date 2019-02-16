using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Component_ClientInvokeBehaviourMethod, true)]
	internal sealed class ClientInvokeBehaviourMethodPacket : Packet, IGUIPacket
	{
		public int		gameObjectInstanceID;
		public int		componentInstanceID;
		public string	methodSignature;
		public byte[]	arguments;

		private string	cachedLabel;

		public	ClientInvokeBehaviourMethodPacket(int gameObjectInstanceID, int instanceID, string methodSignature, byte[] arguments)
		{
			this.gameObjectInstanceID = gameObjectInstanceID;
			this.componentInstanceID = instanceID;
			this.methodSignature = methodSignature;
			this.arguments = arguments;
		}

		private	ClientInvokeBehaviourMethodPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
			{
				this.cachedLabel = string.Format("Invoking {0}{1}.{2}{3}.{4}",
												 unityData.GetGameObjectName(this.gameObjectInstanceID),
												 (Conf.DebugMode != Conf.DebugState.None ? " (#" + this.gameObjectInstanceID + ')' : string.Empty),
												 unityData.GetBehaviourName(this.gameObjectInstanceID, this.componentInstanceID),
												 (Conf.DebugMode != Conf.DebugState.None ? " (#" + this.componentInstanceID + ')' : string.Empty),
												 this.methodSignature);
			}

			GUILayout.Label(this.cachedLabel);
		}
	}
}