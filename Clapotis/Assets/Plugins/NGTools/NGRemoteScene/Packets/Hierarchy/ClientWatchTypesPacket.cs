using NGTools.Network;
using System.Text;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.StaticClass_ClientWatchTypes)]
	internal sealed class ClientWatchTypesPacket : Packet, IGUIPacket
	{
		public int[]	typeIndexes;

		private string	cachedLabel;

		public	ClientWatchTypesPacket(int[] typeIndexes)
		{
			this.typeIndexes = typeIndexes;
		}

		private	ClientWatchTypesPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
			{
				if (this.typeIndexes.Length > 0)
				{
					StringBuilder	buffer = Utility.GetBuffer("Watching Type ");

					for (int i = 0; i < this.typeIndexes.Length; i++)
					{
						buffer.Append('"');
						buffer.Append(unityData.GetTypeName(this.typeIndexes[i]));
						buffer.Append("\", ");
					}

					if (this.typeIndexes.Length > 0)
						buffer.Length -= 2;

					buffer.Append('.');

					this.cachedLabel = Utility.ReturnBuffer(buffer);
				}
				else
					this.cachedLabel = "Unwatching Type.";
			}

			GUILayout.Label(this.cachedLabel);
		}
	}
}