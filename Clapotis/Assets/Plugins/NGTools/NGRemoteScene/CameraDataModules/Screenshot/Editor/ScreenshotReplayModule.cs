using NGTools;
using NGTools.NGRemoteScene;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	internal sealed class ScreenshotReplayModule : ReplayDataModule
	{
		private bool		keepAspectRatio = false;
		private Texture2D	texture;

		public	ScreenshotReplayModule() : base(ScreenshotModule.ModuleID, ScreenshotModule.Priority, ScreenshotModule.Name)
		{
		}

		public	ScreenshotReplayModule(ScreenshotModuleEditor module, int textureWidth, int textureHeight) : base(ScreenshotModule.ModuleID, ScreenshotModule.Priority, ScreenshotModule.Name)
		{
			this.data.AddRange(module.data);
			this.texture = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false);
			this.texture.hideFlags = HideFlags.HideAndDontSave;
			this.texture.LoadImage((this.data[0] as ScreenshotModuleEditor.Screenshot).data);
		}

		public override void	OnGUIReplay(Rect r)
		{
			EditorGUI.DrawPreviewTexture(r, this.texture, null, this.keepAspectRatio == true ? ScaleMode.ScaleToFit : ScaleMode.StretchToFill);
		}

		public override void	SetTime(float time)
		{
			int	lastIndex = this.index;

			base.SetTime(time);

			if (this.index < 0)
				this.index = 0;

			if (lastIndex != this.index)
				this.texture.LoadImage((this.data[this.index] as ScreenshotModuleEditor.Screenshot).data);
		}

		public override void	Export(ByteBuffer buffer)
		{
			buffer.Append(this.data.Count);

			foreach (ScreenshotModuleEditor.Screenshot screenshot in this.data)
			{
				buffer.Append(screenshot.time);
				buffer.Append(screenshot.data.Length);
				buffer.Append(screenshot.data);
			}
		}

		public override void	Import(Replay replay, ByteBuffer buffer)
		{
			Int32	count = buffer.ReadInt32();

			this.data.Clear();
			this.data.Capacity = count;

			for (int i = 0; i < count; i++)
			{
				ScreenshotModuleEditor.Screenshot	screenshot = new ScreenshotModuleEditor.Screenshot();
				screenshot.time = buffer.ReadSingle();
				screenshot.data = buffer.ReadBytes(buffer.ReadInt32());
				this.data.Add(screenshot);
			}

			this.texture = new Texture2D(replay.width, replay.height, TextureFormat.ARGB32, false);
			this.texture.hideFlags = HideFlags.HideAndDontSave;
		}

		public override void	OnGUIOptions(NGReplayWindow window)
		{
			EditorGUI.BeginChangeCheck();
			Utility.content.text = "       ";
			Utility.content.tooltip = "Keep Aspect Ratio";
			GUILayout.Toggle(this.keepAspectRatio, Utility.content, GeneralStyles.ToolbarButton);
			Utility.content.tooltip = null;
			if (EditorGUI.EndChangeCheck() == true)
				this.keepAspectRatio = !this.keepAspectRatio;

			Rect	r2 = GUILayoutUtility.GetLastRect();

			r2.xMin += 6F;
			r2.xMax -= 5F;
			r2.yMin += 2F;
			r2.yMax -= 2F;
			Utility.DrawUnfillRect(r2, EditorStyles.label.normal.textColor);

			r2.xMin += 5F;
			r2.xMax -= 5F;
			r2.yMin += 1F;
			r2.yMax -= 2F;
			Utility.DrawRectDotted(r2, window.position, Color.blue, 0.001F);
		}
	}
}