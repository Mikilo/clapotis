using NGTools;
using NGTools.Network;
using NGTools.NGRemoteScene;
using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	internal sealed class ScreenshotModuleEditor : CameraDataModuleEditor
	{
		internal sealed class Screenshot : CameraData
		{
			public bool		compressed;
			public byte[]	data;
		}

		public const string	UseJPGKeyPref = "ScreenshotModulEditor.useJPG";
		public const string	UseCompressionKeyPref = "ScreenshotModulEditor.useCompression";
		public const string	ScaleModeKeyPref = "ScreenshotModulEditor.scaleMode";

		public Texture2D	Texture { get { return this.texture; } }
		public ScaleMode	ScaleMode { get { return this.scaleMode; } }

		private bool		useJPG;
		private bool		useCompression;
		private Texture2D	texture;
		private ScaleMode	scaleMode = ScaleMode.ScaleToFit;

		private byte[]	compressionBuffer;

		public	ScreenshotModuleEditor() : base(ScreenshotModule.ModuleID, ScreenshotModule.Priority, ScreenshotModule.Name)
		{
			this.useJPG = NGEditorPrefs.GetBool(ScreenshotModuleEditor.UseJPGKeyPref, this.useJPG, true);
			this.useCompression = NGEditorPrefs.GetBool(ScreenshotModuleEditor.UseCompressionKeyPref, this.useCompression, true);
			this.scaleMode = (ScaleMode)NGEditorPrefs.GetInt(ScreenshotModuleEditor.ScaleModeKeyPref, (int)this.scaleMode, true);
		}

		public override void	OnGUICamera(IReplaySettings settings, Rect r)
		{
			if (this.texture == null ||
				this.texture.width != settings.TextureWidth ||
				this.texture.height != settings.TextureHeight)
			{
				this.texture = new Texture2D(settings.TextureWidth, settings.TextureHeight, TextureFormat.ARGB32, false);
			}

			EditorGUI.DrawPreviewTexture(r, this.texture, null, this.scaleMode);
		}

		public override float	GetGUIModuleHeight(NGRemoteHierarchyWindow hierarchy)
		{
			return 18F * 3F;
		}

		public override void	OnGUIModule(Rect r, NGRemoteHierarchyWindow hierarchy)
		{
			using (LabelWidthRestorer.Get(110F))
			{
				r.height = Constants.SingleLineHeight;
				EditorGUI.BeginChangeCheck();
				this.useJPG = EditorGUI.Toggle(r, "Use JPG", this.useJPG);
				if (EditorGUI.EndChangeCheck() == true)
				{
					NGEditorPrefs.SetBool(ScreenshotModuleEditor.UseJPGKeyPref, this.useJPG, true);

					if (hierarchy.IsClientConnected() == true)
						hierarchy.Client.AddPacket(new ClientModuleSetUseJPGPacket(this.useJPG));
				}
				r.y += r.height + 2F;

				EditorGUI.BeginChangeCheck();
				this.useCompression = EditorGUI.Toggle(r, "Use Compression", this.useCompression);
				if (EditorGUI.EndChangeCheck() == true)
				{
					NGEditorPrefs.SetBool(ScreenshotModuleEditor.UseCompressionKeyPref, this.useCompression, true);

					if (hierarchy.IsClientConnected() == true)
						hierarchy.Client.AddPacket(new ClientModuleSetUseCompressionPacket(this.useCompression));
				}
				r.y += r.height + 2F;

				EditorGUI.BeginChangeCheck();
				r.width = 210F;
				this.scaleMode = (ScaleMode)NGEditorGUILayout.EnumPopup(r, "Scale Mode", this.scaleMode);
				if (EditorGUI.EndChangeCheck() == true)
					NGEditorPrefs.SetInt(ScreenshotModuleEditor.ScaleModeKeyPref, (int)this.scaleMode, true);
			}
		}

		public override void	OnServerInitialized(IReplaySettings settings, Client server)
		{
			server.AddPacket(new ClientModuleSetUseJPGPacket(this.useJPG));
			server.AddPacket(new ClientModuleSetUseCompressionPacket(this.useCompression));
		}

		public override void	HandlePacket(IReplaySettings settings, float time, byte[] data)
		{
			if (this.texture == null ||
				this.texture.width != settings.TextureWidth ||
				this.texture.height != settings.TextureHeight)
			{
				this.texture = new Texture2D(settings.TextureWidth, settings.TextureHeight, TextureFormat.ARGB32, false);
			}

			this.RemoveOldData(time - settings.RecordLastSeconds);

			bool	usedCompression = false;

			if (this.useCompression == true)
			{
				try
				{
					using (MemoryStream ms = new MemoryStream(data))
					using (GZipStream gz = new GZipStream(ms, CompressionMode.Decompress, true))
					{
						using (MemoryStream output = new MemoryStream())
						{
							if (this.compressionBuffer == null)
								this.compressionBuffer = new byte[4048];

							int	count = 0;

							do
							{
								count = gz.Read(this.compressionBuffer, 0, 4048);
								if (count > 0)
									output.Write(this.compressionBuffer, 0, count);
							}
							while (count > 0);

							data = output.ToArray();
						}
					}

					usedCompression = true;
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogFileException("Decompressing texture.", ex);
				}
			}

			this.texture.LoadImage(data);

			this.data.Add(new Screenshot() { time = time, compressed = usedCompression, data = data });
		}

		public override ReplayDataModule	ConvertToReplay(IReplaySettings settings)
		{
			return new ScreenshotReplayModule(this, settings.TextureWidth, settings.TextureHeight);
		}
	}
}