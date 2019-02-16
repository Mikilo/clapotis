using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Material_ClientChangeMaterialShader, true)]
	internal sealed class ClientChangeMaterialShaderPacket : Packet, IGUIPacket
	{
		public int	instanceID;
		public int	shaderInstanceID;

		private string	cachedGUI;

		public	ClientChangeMaterialShaderPacket(int instanceID, int shaderInstanceID)
		{
			this.instanceID = instanceID;
			this.shaderInstanceID = shaderInstanceID;
		}

		private	ClientChangeMaterialShaderPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedGUI == null)
			{
				this.cachedGUI = string.Format("Changing Material \"{0}\"{1} with Shader \"{2}\"{3}.",
											   unityData.GetResourceName(typeof(Material), this.instanceID),
											   (Conf.DebugMode != Conf.DebugState.None ? " (#" + this.instanceID + ")" : string.Empty),
											   unityData.GetResourceName(typeof(Shader), this.shaderInstanceID),
											   (Conf.DebugMode != Conf.DebugState.None ? " (#" + this.shaderInstanceID + ")" : string.Empty));
			}

			GUILayout.Label(this.cachedGUI);
		}
	}
}