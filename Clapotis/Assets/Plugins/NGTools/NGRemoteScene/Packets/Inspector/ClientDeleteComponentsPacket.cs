using NGTools.Network;
using System.Collections.Generic;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Component_ClientDeleteComponents, true)]
	internal sealed class ClientDeleteComponentsPacket : Packet, IGUIPacket
	{
		public List<int>	gameObjectInstanceIDs;
		public List<int>	componentInstanceIDs;

		private string	cachedLabel;
		private bool	foldout;

		public	ClientDeleteComponentsPacket()
		{
			this.gameObjectInstanceIDs = new List<int>();
			this.componentInstanceIDs = new List<int>();
		}

		public	ClientDeleteComponentsPacket(int gameObjectInstanceID, int componentInstanceID)
		{
			this.gameObjectInstanceIDs = new List<int>();
			this.componentInstanceIDs = new List<int>();

			this.gameObjectInstanceIDs.Add(gameObjectInstanceID);
			this.componentInstanceIDs.Add(componentInstanceID);
		}

		private	ClientDeleteComponentsPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public void	Add(int gameObjectInstanceID, int componentInstanceID)
		{
			this.gameObjectInstanceIDs.Add(gameObjectInstanceID);
			this.componentInstanceIDs.Add(componentInstanceID);
			this.cachedLabel = null;
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
				this.cachedLabel = "Deleting " + this.componentInstanceIDs.Count + " Component";

			this.foldout = GUILayout.Toggle(this.foldout, this.cachedLabel);
			if (this.foldout == true)
			{
				for (int i = 0; i < this.gameObjectInstanceIDs.Count; i++)
				{
					GUILayout.Label(string.Format("{0}{1}.{2}{3}",
								    unityData.GetGameObjectName(this.gameObjectInstanceIDs[i]),
								    (Conf.DebugMode != Conf.DebugState.None ? " (#" + this.gameObjectInstanceIDs[i] + ')' : string.Empty),
								    unityData.GetBehaviourName(this.gameObjectInstanceIDs[i], this.componentInstanceIDs[i]),
								    (Conf.DebugMode != Conf.DebugState.None ? " (#" + this.componentInstanceIDs[i] + ')' : string.Empty)));
				}
			}
		}
	}
}