using NGTools;
using NGTools.NGRemoteScene;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[TabModuleForType(typeof(Sprite))]
	public class UserSpriteTab : TabModule
	{
		public const float	GUISpacing = 2F;
		public const float	BottomHeight = 32F;
		public const string	DefaultSuffix = " (User)";
		public static Color	FocusColor = new Color(30F / 255F, 180F / 255F, 140F / 255F);
		public static Color	InitialColor = new Color(55F / 255F, 85F / 255F, 205F / 255F);
		public static Color	HoverBackgroundColor = new Color(55F / 255F, 85F / 255F, 55F / 255F);

		private Vector2	scrollPosition;
		private string	textureName;
		private Sprite	sprite;

		private NGRemoteHierarchyWindow.UserAsset	hoverData;
		private string								cachedHoverData;

		public	UserSpriteTab(ResourcesPickerWindow window) : base("User Sprite", window)
		{
		}

		public override void	OnEnter()
		{
			base.OnEnter();

			this.window.wantsMouseMove = true;
			this.window.hierarchy.LoadUserAssets(typeof(Sprite));
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

			List<NGRemoteHierarchyWindow.UserAsset>	assets = this.window.hierarchy.GetUserAssets(typeof(Sprite));

			if (this.window.hierarchy.IsChannelBlocked(typeof(Sprite).GetHashCode()) == true)
			{
				bodyRect.height = Constants.SingleLineHeight;
				GUI.Label(r, GeneralStyles.StatusWheel);
				bodyRect.xMin += bodyRect.height;
				GUI.Label(bodyRect, "Loading Sprite");
				bodyRect.xMin -= bodyRect.height;
				bodyRect.y += bodyRect.height;
				bodyRect.height = height - bodyRect.height;

				this.window.Repaint();
			}

			if (assets != null && assets.Count > 0)
			{
				Utility.content.text = "User Sprite:";
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
							GeneralStyles.ToolbarButton.normal.textColor = UserSpriteTab.FocusColor;
						else if (this.window.initialInstanceID == assets[i].instanceID)
							GeneralStyles.ToolbarButton.normal.textColor = UserSpriteTab.InitialColor;

						Utility.content.text = assets[i].name;
						if (GUI.Button(bodyRect, Utility.content, GeneralStyles.ToolbarButton) == true)
						{
							if (this.window.selectedInstanceID == assets[i].instanceID)
								this.window.Close();
							else
							{
								this.window.selectedInstanceID = assets[i].instanceID;

								ByteBuffer	buffer = Utility.GetBBuffer();

								this.window.typeHandler.Serialize(buffer, typeof(Sprite), new UnityObject(typeof(Sprite), assets[i].instanceID));

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
				bodyRect.y += bodyRect.height + UserSpriteTab.GUISpacing;

				EditorGUI.BeginChangeCheck();
				bodyRect.height = 50F;
				this.sprite = EditorGUI.ObjectField(bodyRect, "Sprite", this.sprite, typeof(Sprite), false) as Sprite;
				bodyRect.y += bodyRect.height + UserSpriteTab.GUISpacing;
				if (EditorGUI.EndChangeCheck() == true)
				{
					if (this.sprite != null && (string.IsNullOrEmpty(this.textureName) == true || this.textureName.EndsWith(UserSpriteTab.DefaultSuffix) == true))
						this.textureName = this.sprite.name + UserSpriteTab.DefaultSuffix;
				}
			}

			if (this.sprite != null)
			{
				bodyRect.height = height - (Constants.SingleLineHeight + UserSpriteTab.GUISpacing + 50F + UserSpriteTab.GUISpacing) - Constants.SingleLineHeight - UserSpriteTab.BottomHeight;
				if (bodyRect.height < 100F)
					bodyRect.height = 100F;

				EditorGUI.DrawTextureTransparent(bodyRect, this.sprite.texture, ScaleMode.ScaleToFit);
				bodyRect.y += bodyRect.height;

				bodyRect.height = Constants.SingleLineHeight;
				GUI.Label(bodyRect, "W: " + this.sprite.texture.width + "  H: " + this.sprite.texture.height + "  Format: " + this.sprite.texture.format + "  PixelsPerUnit: " + this.sprite.pixelsPerUnit);
				bodyRect.y += bodyRect.height;

				try
				{
					bodyRect.height = UserSpriteTab.BottomHeight;

					this.sprite.texture.GetPixel(0, 0);

					if (GUI.Button(bodyRect, "Send") == true)
						this.window.hierarchy.SendUserSprite(this.textureName, this.sprite.texture.EncodeToPNG(), this.sprite.rect, this.sprite.pivot, this.sprite.pixelsPerUnit, asset => this.window.Repaint());
				}
				catch (UnityException)
				{
					EditorGUI.HelpBox(bodyRect, "Sprite must be readable.", MessageType.Error);
				}
			}

			if (this.hoverData != null)
			{
				Sprite	sprite = EditorUtility.InstanceIDToObject(this.hoverData.instanceID) as Sprite;

				if (sprite != null)
				{
					if (sprite.texture.width >= sprite.texture.height)
					{
						r.width = 100F;
						r.height = 100F * sprite.texture.height / sprite.texture.width;
					}
					else
					{
						r.width = 100F * sprite.texture.width / sprite.texture.height;
						r.height = 100F;
					}

					EditorGUI.DrawTextureTransparent(r, sprite.texture, ScaleMode.ScaleToFit);
				}
				else
				{
					r.width = 100F;
					r.height = 32F;
					EditorGUI.DrawRect(r, UserSpriteTab.HoverBackgroundColor);
					EditorGUI.HelpBox(r, "Preview not available.", MessageType.Warning);
				}

				r.y += r.height;

				Utility.content.text = this.cachedHoverData;
				Vector2	size = GUI.skin.label.CalcSize(Utility.content);

				r.width = size.x;
				r.height = size.y;

				EditorGUI.DrawRect(r, UserSpriteTab.HoverBackgroundColor);
				GUI.Label(r, Utility.content.text);
			}

			if (Event.current.type == EventType.MouseMove)
				this.window.Repaint();
		}
	}
}
