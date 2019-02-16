using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Class_ClientUpdateFieldValue, true)]
	internal sealed class ClientUpdateFieldValuePacket : Packet, IGUIPacket
	{
		public string	fieldPath;
		public byte[]	rawValue;

		private TypeHandler	deserializer;
		private string		cachedLabel;

		public	ClientUpdateFieldValuePacket(string fieldPath, byte[] rawValue, TypeHandler deserializer)
		{
			this.fieldPath = fieldPath;
			this.rawValue = rawValue;
			this.deserializer = deserializer;
		}

		private	ClientUpdateFieldValuePacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override bool	AggregateInto(Packet pendingPacket)
		{
			ClientUpdateFieldValuePacket	packet = pendingPacket as ClientUpdateFieldValuePacket;

			if (packet != null && packet.fieldPath.Equals(this.fieldPath) == true)
			{
				packet.rawValue = this.rawValue;
				packet.cachedLabel = null;
				return true;
			}

			return false;
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
			{
				string[]	paths = this.fieldPath.Split(NGServerScene.ValuePathSeparator);

				try
				{
					unityData.FetchReadablePaths(paths, true);

					if (this.deserializer == null)
						this.cachedLabel = "Updating " + string.Join(" > ", paths) + '.';
					else
						this.cachedLabel = "Updating " + string.Join(" > ", paths) + " (" + this.deserializer.Deserialize(new ByteBuffer(this.rawValue), null) + ").";
				}
				catch
				{
					if (this.deserializer == null)
						this.cachedLabel = "Updating " + this.fieldPath + '.';
					else
						this.cachedLabel = "Updating " + this.fieldPath + " (" + this.deserializer.Deserialize(new ByteBuffer(this.rawValue), null) + ").";
				}
			}

			GUILayout.Label(this.cachedLabel);
		}
	}
}