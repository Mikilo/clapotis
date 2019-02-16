using NGTools.Network;
using System.Text;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Material_ClientWatchMaterials)]
	internal sealed class ClientWatchMaterialsPacket : Packet, IGUIPacket
	{
		public int[]	materialInstanceIDs;

		private string	cachedLabel;

		public	ClientWatchMaterialsPacket(int[] instanceIDs)
		{
			this.materialInstanceIDs = instanceIDs;
		}

		private	ClientWatchMaterialsPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
			{
				if (this.materialInstanceIDs.Length > 0)
				{
					StringBuilder	buffer = Utility.GetBuffer("Watching Material ");

					for (int i = 0; i < this.materialInstanceIDs.Length; i++)
					{
						buffer.Append('"');
						buffer.Append(unityData.GetResourceName(typeof(Material), this.materialInstanceIDs[i]));
						buffer.Append('"');

						if (Conf.DebugMode != Conf.DebugState.None)
						{
							buffer.Append(" (#");
							buffer.Append(this.materialInstanceIDs[i]);
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
					this.cachedLabel = "Unwatching Material.";
			}

			GUILayout.Label(this.cachedLabel);
		}
	}
}