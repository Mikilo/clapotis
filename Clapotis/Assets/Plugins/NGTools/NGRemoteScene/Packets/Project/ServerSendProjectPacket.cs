using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Asset_ServerSendProject)]
	internal sealed class ServerSendProjectPacket : ResponsePacket
	{
		public ListingAssets.AssetReferences[]	assets;

		public	ServerSendProjectPacket(int networkId) : base(networkId)
		{
		}

		private	ServerSendProjectPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override void	Out(ByteBuffer buffer)
		{
			if (this.OutResponseStatus(buffer) == true)
			{
				bool	error = false;

				buffer.Append(this.assets.Length);

				for (int i = 0; i < this.assets.Length; i++)
				{
					buffer.AppendUnicodeString(this.assets[i].asset);
					buffer.Append(this.assets[i].mainAssetIndex);
					buffer.Append(this.assets[i].references.Length);

					for (int j = 0; j < this.assets[i].references.Length; j++)
					{
						Object	obj = this.assets[i].references[j];

						if (obj == null)
						{
							error = true;
							InternalNGDebug.LogWarning("Asset \"" + this.assets[i].asset + "\" has a null reference.");
							buffer.Append(0);
							buffer.AppendUnicodeString(null);
							buffer.AppendUnicodeString(null);
						}
						else
						{
							buffer.Append(obj.GetInstanceID());

							if (typeof(Component).IsAssignableFrom(obj.GetType()) == true)
								buffer.AppendUnicodeString(obj.GetType().Name);
							else
								buffer.AppendUnicodeString(obj.name);

							buffer.AppendUnicodeString(obj.GetType().GetShortAssemblyType());
						}
					}
				}

				if (error == true)
					InternalNGDebug.LogWarning("A null asset has been detected. You should refresh \"Embedded Resources\" in NG Server Scene.");
			}
		}

		public override void	In(ByteBuffer buffer)
		{
			if (this.InResponseStatus(buffer) == true)
			{
				int	total = buffer.ReadInt32();

				this.assets = new ListingAssets.AssetReferences[total];

				for (int i = 0; i < total; i++)
				{
					this.assets[i] = new ListingAssets.AssetReferences();
					this.assets[i].asset = buffer.ReadUnicodeString();
					this.assets[i].mainAssetIndex = buffer.ReadInt32();
					this.assets[i].IDs = new int[buffer.ReadInt32()];
					this.assets[i].subNames = new string[this.assets[i].IDs.Length];
					this.assets[i].types = new string[this.assets[i].IDs.Length];

					for (int j = 0; j < this.assets[i].IDs.Length; j++)
					{
						this.assets[i].IDs[j] = buffer.ReadInt32();
						this.assets[i].subNames[j] = buffer.ReadUnicodeString();
						this.assets[i].types[j] = buffer.ReadUnicodeString();
					}
				}
			}
		}
	}
}