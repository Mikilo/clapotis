using NGTools.Network;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	/// <summary>
	/// <para>Requests all primary data from a GameObject.</para>
	/// <para>That includes:</para>
	/// <list type="bullet">
	/// <item><description>Tag</description></item>
	/// <item><description>Layer</description></item>
	/// <item><description>IsStatic</description></item>
	/// <item><description>Behaviours</description></item>
	/// <item><description>Behaviours' fields</description></item>
	/// <item><description>Behaviours' methods</description></item>
	/// </list>
	/// </summary>
	/// <seealso cref="NGTools.ServerSendGameObjectDataPacket"/>
	[PacketLinkTo(RemoteScenePacketId.GameObject_ClientRequestGameObjectData)]
	internal sealed class ClientRequestGameObjectDataPacket : Packet, IGUIPacket
	{
		public List<int>	gameObjectInstanceIDs;

		private string	cachedLabel;

		public	ClientRequestGameObjectDataPacket(int gameObjectInstanceID)
		{
			this.gameObjectInstanceIDs = new List<int>() { gameObjectInstanceID };
		}

		private	ClientRequestGameObjectDataPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
			{
				StringBuilder	buffer = Utility.GetBuffer("Requesting data from GameObject ");

				for (int i = 0; i < this.gameObjectInstanceIDs.Count; i++)
				{
					if (i > 0)
						buffer.Append(", ");

					buffer.Append('"');
					buffer.Append(unityData.GetGameObjectName(this.gameObjectInstanceIDs[i]));
					buffer.Append('"');

					if (Conf.DebugMode != Conf.DebugState.None)
					{
						buffer.Append(" (#");
						buffer.Append(this.gameObjectInstanceIDs[i]);
						buffer.Append(')');
					}
				}

				buffer.Append('.');

				this.cachedLabel = Utility.ReturnBuffer(buffer);
			}

			GUILayout.Label(this.cachedLabel);
		}
	}
}