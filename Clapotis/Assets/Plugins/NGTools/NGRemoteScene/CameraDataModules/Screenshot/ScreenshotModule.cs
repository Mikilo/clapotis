using NGTools.Network;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	internal sealed class ScreenshotModule : CameraServerDataModule
	{
		public const byte	ModuleID = 1;
		public const int	Priority = 1000;
		public const string	Name = "Screenshot";

		private float		nextTime;
		private Texture2D	texture;
		private bool		useJPG = true;
		private bool		useCompression = false;

		public	ScreenshotModule() : base(ScreenshotModule.ModuleID, ScreenshotModule.Priority, ScreenshotModule.Name)
		{
		}

		public override void	Awake(NGServerScene scene)
		{
			scene.server.executer.HandlePacket(RemoteScenePacketId.Camera_ClientModuleSetUseJPG, this.SetUseJPG);
			scene.server.executer.HandlePacket(RemoteScenePacketId.Camera_ClientModuleSetUseCompression, this.SetUseCompression);
		}

		public override void	OnDestroy(NGServerScene scene)
		{
			scene.server.executer.UnhandlePacket(RemoteScenePacketId.Camera_ClientModuleSetUseJPG, this.SetUseJPG);
			scene.server.executer.UnhandlePacket(RemoteScenePacketId.Camera_ClientModuleSetUseCompression, this.SetUseCompression);
		}

		public void	Update(ICameraScreenshotData data)
		{
			float	t = Time.unscaledTime;

			if (t <= this.nextTime)
				return;

			Camera	camera = data.TargetCamera;

			if (camera == null)
				return;

			float	timeOverflow = 0F;
			if (t - this.nextTime <= (1F / data.TargetRefresh) * 2F)
				timeOverflow = t - this.nextTime;

			this.nextTime = t + (1F / data.TargetRefresh) - timeOverflow;

			RenderTexture	restore = camera.targetTexture;
			camera.targetTexture = data.RenderTexture;

			bool	restoreWire = GL.wireframe;
			GL.wireframe = data.Wireframe;

			camera.Render();

			GL.wireframe = restoreWire;
			camera.targetTexture = restore;

			RenderTexture.active = data.RenderTexture;

			if (this.texture == null ||
				this.texture.width != data.Width ||
				this.texture.height != data.Height)
			{
				this.texture = new Texture2D(data.Width, data.Height, TextureFormat.ARGB32, false)
				{
					hideFlags = HideFlags.HideAndDontSave
				};
			}

			this.texture.ReadPixels(new Rect(0F, 0F, data.Width, data.Height), 0, 0);
			RenderTexture.active = null;

			byte[]	raw = this.useJPG == true ? this.texture.EncodeToJPG() : this.texture.EncodeToPNG();

			if (this.useCompression == true)
			{
				using (MemoryStream ms = new MemoryStream())
				using (GZipStream gz = new GZipStream(ms, CompressionMode.Compress, true))
				{
					gz.Write(raw, 0, raw.Length);
					gz.Close();
					raw = ms.ToArray();
				}
			}

			data.Sender.AddPacket(new NotifyCameraDataPacket(this.moduleID, Time.unscaledTime, raw));
		}

		private void	SetUseJPG(Client client, Packet _packet)
		{
			ClientModuleSetUseJPGPacket	packet = _packet as ClientModuleSetUseJPGPacket;

			this.useJPG = packet.useJPG;
		}

		private void	SetUseCompression(Client client, Packet _packet)
		{
			ClientModuleSetUseCompressionPacket	packet = _packet as ClientModuleSetUseCompressionPacket;

			this.useCompression = packet.useCompression;
		}
	}
}