using NGTools.Network;
using System.Text;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.GameObject_ClientWatchGameObjects)]
	internal sealed class ClientWatchGameObjectsPacket : Packet, IGUIPacket
	{
		public int[]	gameObjectInstanceIDs;

		private string	cachedLabel;

		public	ClientWatchGameObjectsPacket(int[] instanceIDs)
		{
			this.gameObjectInstanceIDs = instanceIDs;
		}

		private	ClientWatchGameObjectsPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
			{
				if (this.gameObjectInstanceIDs.Length > 0)
				{
					StringBuilder	buffer = Utility.GetBuffer("Watching GameObject ");

					for (int i = 0; i < this.gameObjectInstanceIDs.Length; i++)
					{
						buffer.Append('"');
						buffer.Append(unityData.GetGameObjectName(this.gameObjectInstanceIDs[i]));
						buffer.Append('"');

						if (Conf.DebugMode != Conf.DebugState.None)
						{
							buffer.Append(" (#");
							buffer.Append(this.gameObjectInstanceIDs[i]);
							buffer.Append(')');
						}

						buffer.Append(", ");
					}

					if (buffer.Length > 0)
						buffer.Length -= 2;

					buffer.Append('.');
					this.cachedLabel = Utility.ReturnBuffer(buffer);
				}
				else
					this.cachedLabel = "Unwatching GameObject.";
			}

			GUILayout.Label(this.cachedLabel);
		}
	}
}