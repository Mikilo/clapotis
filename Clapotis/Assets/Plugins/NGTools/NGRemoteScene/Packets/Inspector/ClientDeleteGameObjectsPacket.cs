using NGTools.Network;
using System.Collections.Generic;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.GameObject_ClientDeleteGameObjects, true)]
	internal sealed class ClientDeleteGameObjectsPacket : Packet, IGUIPacket
	{
		public List<int>	gameObjectInstanceIDs;

		private string	cachedLabel;
		private bool	foldout;

		public	ClientDeleteGameObjectsPacket()
		{
			this.gameObjectInstanceIDs = new List<int>();
		}

		public	ClientDeleteGameObjectsPacket(int instanceID)
		{
			this.gameObjectInstanceIDs = new List<int>();
			this.gameObjectInstanceIDs.Add(instanceID);
		}

		private	ClientDeleteGameObjectsPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public void	Add(int instanceID)
		{
			this.gameObjectInstanceIDs.Add(instanceID);
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
				this.cachedLabel = "Deleting " + this.gameObjectInstanceIDs.Count + " GameObject";

			this.foldout = GUILayout.Toggle(this.foldout, this.cachedLabel);
			if (this.foldout == true)
			{
				for (int i = 0; i < this.gameObjectInstanceIDs.Count; i++)
					GUILayout.Label(unityData.GetGameObjectName(this.gameObjectInstanceIDs[i]) + (Conf.DebugMode != Conf.DebugState.None ? " (#" + this.gameObjectInstanceIDs[i] + ')' : string.Empty));
			}
		}
	}
}