using NGTools;
using NGTools.NGRemoteScene;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[TabModuleForType(typeof(Texture2D))]
	public class UserTexture2DTab : TabModule
	{
		public const float	GUISpacing = 2F;
		public const float	BottomHeight = 32F;
		public const string	DefaultSuffix = " (User)";
		public static Color	FocusColor = new Color(30F / 255F, 180F / 255F, 140F / 255F);
		public static Color	InitialColor = new Color(55F / 255F, 85F / 255F, 205F / 255F);
		public static Color	HoverBackgroundColor = new Color(55F / 255F, 85F / 255F, 55F / 255F);

		private Vector2		scrollPosition;
		private string		textureName;
		private Texture2D	texture;

		private NGRemoteHierarchyWindow.UserAsset	hoverData;
		private string								cachedHoverData;

		public	UserTexture2DTab(ResourcesPickerWindow window) : base("User Texture2D", window)
		{
		}

		public override void	OnEnter()
		{
			base.OnEnter();

			this.window.wantsMouseMove = true;
			this.window.hierarchy.LoadUserAssets(typeof(Texture2D));
			this.window.minSize = new Vector2(350F, 300F);
		}

		public override void	OnLeave()
		{
			base.OnLeave();

			this.window.wantsMouseMove = false;
		}

		public override void	OnGUI(Rect r)
		{
			Rect	bodyRect = r;
			Rect	viewRect = r;
			float	height = r.height;

			List<NGRemoteHierarchyWindow.UserAsset>	assets = this.window.hierarchy.GetUserAssets(typeof(Texture2D));

			if (this.window.hierarchy.IsChannelBlocked(typeof(Texture2D).GetHashCode()) == true)
			{
				bodyRect.height = Constants.SingleLineHeight;
				GUI.Label(r, GeneralStyles.StatusWheel);
				bodyRect.xMin += bodyRect.height;
				GUI.Label(bodyRect, "Loading Texture2D");
				bodyRect.xMin -= bodyRect.height;
				bodyRect.y += bodyRect.height;
				bodyRect.height = height - bodyRect.height;

				this.window.Repaint();
			}

			if (assets != null && assets.Count > 0)
			{
				Utility.content.text = "User Texture2D:";
				Vector2	titleSize = GeneralStyles.Title1.CalcSize(Utility.content);
				float	maxWidth = titleSize.x;

				GUI.Label(bodyRect, Utility.content.text, GeneralStyles.Title1);

				for (int i = 0; i < assets.Count; i++)
				{
					Utility.content.text = assets[i].name;
					float	w = GeneralStyles.ToolbarButton.CalcSize(Utility.content).x;

					if (maxWidth < w)
						maxWidth = w;
				}

				viewRect.y = 0F;
				viewRect.width = maxWidth;
				viewRect.height = assets.Count * GeneralStyles.ToolbarButton.CalcSize(Utility.content).y;

				bodyRect.x += maxWidth;
				bodyRect.width = 1F;
				EditorGUI.DrawRect(bodyRect, Color.black);
				bodyRect.width = maxWidth;
				bodyRect.x -= maxWidth;

				bodyRect.yMin += titleSize.y;

				NGRemoteHierarchyWindow.UserAsset	hoverData = null;

				this.scrollPosition = GUI.BeginScrollView(bodyRect, this.scrollPosition, viewRect);
				{
					bodyRect.x = 0F;
					bodyRect.y = 0F;
					bodyRect.height = GeneralStyles.ToolbarButton.CalcSize(Utility.content).y;

					for (int i = 0; i < assets.Count; i++)
					{
						Color	restore = GeneralStyles.ToolbarButton.normal.textColor;

						if (this.window.selectedInstanceID == assets[i].instanceID)
							GeneralStyles.ToolbarButton.normal.textColor = UserTexture2DTab.FocusColor;
						else if (this.window.initialInstanceID == assets[i].instanceID)
							GeneralStyles.ToolbarButton.normal.textColor = UserTexture2DTab.InitialColor;

						Utility.content.text = assets[i].name;
						if (GUI.Button(bodyRect, Utility.content, GeneralStyles.ToolbarButton) == true)
						{
							if (this.window.selectedInstanceID == assets[i].instanceID)
								this.window.Close();
							else
							{
								this.window.selectedInstanceID = assets[i].instanceID;

								ByteBuffer	buffer = Utility.GetBBuffer();

								this.window.typeHandler.Serialize(buffer, typeof(Texture2D), new UnityObject(typeof(Texture2D), assets[i].instanceID));

								this.window.hierarchy.AddPacket(this.window.packetGenerator(this.window.valuePath, Utility.ReturnBBuffer(buffer)));
							}
						}
						GeneralStyles.ToolbarButton.normal.textColor = restore;

						if (bodyRect.Contains(Event.current.mousePosition) == true)
							hoverData = assets[i];

						bodyRect.y += bodyRect.height;
					}
				}
				GUI.EndScrollView();

				r.xMin += maxWidth + 2F;

				if (this.hoverData != hoverData)
				{
					this.hoverData = hoverData;

					if (this.hoverData != null)
						this.cachedHoverData = string.Join("\n", this.hoverData.data);
				}
			}

			using (LabelWidthRestorer.Get(80F))
			{
				bodyRect = r;
				bodyRect.height = Constants.SingleLineHeight;
				this.textureName = EditorGUI.TextField(bodyRect, "Name", this.textureName);
				bodyRect.y += bodyRect.height + UserTexture2DTab.GUISpacing;

				EditorGUI.BeginChangeCheck();
				bodyRect.height = 50F;
				this.texture = EditorGUI.ObjectField(bodyRect, "Texture", this.texture, typeof(Texture2D), false) as Texture2D;
				bodyRect.y += bodyRect.height + UserTexture2DTab.GUISpacing;
				if (EditorGUI.EndChangeCheck() == true)
				{
					if (this.texture != null && (string.IsNullOrEmpty(this.textureName) == true || this.textureName.EndsWith(UserTexture2DTab.DefaultSuffix) == true))
						this.textureName = this.texture.name + UserTexture2DTab.DefaultSuffix;
				}
			}

			if (this.texture != null)
			{
				bodyRect.height = height - (Constants.SingleLineHeight + UserTexture2DTab.GUISpacing + 50F + UserTexture2DTab.GUISpacing) - Constants.SingleLineHeight - UserTexture2DTab.BottomHeight;
				if (bodyRect.height < 100F)
					bodyRect.height = 100F;

				EditorGUI.DrawTextureTransparent(bodyRect, this.texture, ScaleMode.ScaleToFit);
				bodyRect.y += bodyRect.height;

				bodyRect.height = Constants.SingleLineHeight;
				GUI.Label(bodyRect, "W: " + this.texture.width + "  H: " + this.texture.height + "  Format: " + this.texture.format);
				bodyRect.y += bodyRect.height;

				try
				{
					bodyRect.height = UserTexture2DTab.BottomHeight;

					this.texture.GetPixel(0, 0);

					if (GUI.Button(bodyRect, "Send") == true)
						this.window.hierarchy.SendUserTexture2D(this.textureName, this.texture.EncodeToPNG(), asset => this.window.Repaint());
				}
				catch (UnityException)
				{
					EditorGUI.HelpBox(bodyRect, "Texture2D must be readable.", MessageType.Error);
				}
			}

			if (this.hoverData != null)
			{
				Texture	texture = EditorUtility.InstanceIDToObject(this.hoverData.instanceID) as Texture;

				if (texture != null)
				{
					if (texture.width >= texture.height)
					{
						r.width = 100F;
						r.height = 100F * texture.height / texture.width;
					}
					else
					{
						r.width = 100F * texture.width / texture.height;
						r.height = 100F;
					}

					EditorGUI.DrawTextureTransparent(r, texture, ScaleMode.ScaleToFit);
				}
				else
				{
					r.width = 100F;
					r.height = 32F;
					EditorGUI.DrawRect(r, UserTexture2DTab.HoverBackgroundColor);
					EditorGUI.HelpBox(r, "Preview not available.", MessageType.Warning);
				}

				r.y += r.height;

				Utility.content.text = this.cachedHoverData;
				Vector2	size = GUI.skin.label.CalcSize(Utility.content);

				r.width = size.x;
				r.height = size.y;

				EditorGUI.DrawRect(r, UserTexture2DTab.HoverBackgroundColor);
				GUI.Label(r, Utility.content.text);
			}

			if (Event.current.type == EventType.MouseMove)
				this.window.Repaint();
		}
	}
}