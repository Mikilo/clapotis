using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_ClientConnect)]
	internal sealed class ClientConnectPacket : Packet, IGUIPacket
	{
		public bool					overrideResolution;
		public float				shrinkRatio;
		public int					width;
		public int					height;
		public int					depth;
		public int					targetRefresh;
		public RenderTextureFormat	format;
		public byte[]				modulesAvailable;

		public	ClientConnectPacket(float shrinkRatio, int targetRefresh, byte[] modulesAvailable)
		{
			this.overrideResolution = false;
			this.shrinkRatio = shrinkRatio;
			this.targetRefresh = targetRefresh;
			this.modulesAvailable = modulesAvailable;
		}

		public	ClientConnectPacket(int width, int height, int depth, int targetRefresh, RenderTextureFormat format, byte[] modulesAvailable)
		{
			this.overrideResolution = true;
			this.width = width;
			this.height = height;
			this.depth = depth;
			this.targetRefresh = targetRefresh;
			this.format = format;
			this.modulesAvailable = modulesAvailable;
		}

		private	ClientConnectPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override void	Out(ByteBuffer buffer)
		{
			buffer.Append(this.networkId);
			buffer.Append(this.overrideResolution);

			if (this.overrideResolution == false)
				buffer.Append(this.shrinkRatio);
			else
			{
				buffer.Append(this.width);
				buffer.Append(this.height);
				buffer.Append(this.depth);
				buffer.Append((int)this.format);
			}

			buffer.Append(this.targetRefresh);
			buffer.Append(this.modulesAvailable.Length);
			buffer.Append(this.modulesAvailable, 0, this.modulesAvailable.Length);
		}

		public override void	In(ByteBuffer buffer)
		{
			this.networkId = buffer.ReadInt32();
			this.overrideResolution = buffer.ReadBoolean();

			if (this.overrideResolution == false)
				this.shrinkRatio = buffer.ReadSingle();
			else
			{
				this.width = buffer.ReadInt32();
				this.height = buffer.ReadInt32();
				this.depth = buffer.ReadInt32();
				this.format = (RenderTextureFormat)buffer.ReadInt32();
			}

			this.targetRefresh = buffer.ReadInt32();
			this.modulesAvailable = buffer.ReadBytes(buffer.ReadInt32());
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			GUILayout.Label("Connecting NG Camera.");
		}
	}
}