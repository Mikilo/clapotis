using NGLicenses;
using NGTools;
using NGTools.Network;
using NGTools.NGRemoteScene;
using NGToolsEditor.Network;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;

namespace NGToolsEditor.NGRemoteScene
{
	using UnityEngine;

	public class NGRemoteCameraWindow : NGRemoteWindow, IHasCustomMenu, IReplaySettings
	{
		private class CameraOverrideSettings
		{
			public List<bool>	componentsSelected = new List<bool>();

			public bool	overrideClearFlags = false;
			public bool	overrideBackground = false;
			public bool	overrideCullingMask = false;
			public bool	overrideProjection = false;
			public bool	overrideFieldOfView = false;
			public bool	overrideSize = false;
			public bool	overrideClippingPlanes = false;
			public bool	overrideViewportRect = false;
			public bool	overrideCdepth = false;
			public bool	overrideRenderingPath = false;
			public bool	overrideOcclusionCulling = false;
			public bool	overrideHDR = false;
			public bool	overrideTargetDisplay = false;

			public int		clearFlags = 1;
			public Color	background = Color.black;
			public int		cullingMask = -1;
			public int		projection = 0;
			public float	fieldOfView = 30F;
			public float	size = 5F;
			public float	clippingPlanesNear = 1F;
			public float	clippingPlanesFar = 1000F;
			public Rect		viewportRect = new Rect(0F, 0F, 1F, 1F);
			public float	cdepth = -1F;
			public int		renderingPath = -1;
			public bool		occlusionCulling = true;
			public bool		HDR = false;
			public int		targetDisplay = 0;

			[NonSerialized]
			private string	name;

			public	CameraOverrideSettings()
			{
				this.name = NGRemoteCameraWindow.Title + ".ghostCamera.";
				Utility.LoadEditorPref(this, typeof(CameraOverrideSettings), this.name);
			}

			public	CameraOverrideSettings(string name)
			{
				this.name = NGRemoteCameraWindow.Title + '.' + name + '.';
				Utility.LoadEditorPref(this, typeof(CameraOverrideSettings), this.name);
			}

			public void	Save()
			{
				Utility.SaveEditorPref(this, this.name);
			}
		}

		private class OptionsPopup : PopupWindowContent
		{
			public const float	Spacing = 2F;
			public const float	BigSpacing = 5F;

			private readonly NGRemoteCameraWindow	window;

			public	OptionsPopup(NGRemoteCameraWindow window)
			{
				this.window = window;
			}

			public override void	OnOpen()
			{
				base.OnOpen();

				this.editorWindow.wantsMouseMove = true;
			}

			public override Vector2	GetWindowSize()
			{
				float	height = OptionsPopup.Spacing + Constants.SingleLineHeight + OptionsPopup.BigSpacing + // Top margin + Target FPS + Spacing
					Constants.SingleLineHeight + OptionsPopup.BigSpacing + // Camera title
					Constants.SingleLineHeight + OptionsPopup.Spacing + Constants.SingleLineHeight + OptionsPopup.BigSpacing + // Resolution + Render Texture + Spacing
					Constants.SingleLineHeight + OptionsPopup.BigSpacing + // Modules title + Spacing
					Constants.SingleLineHeight + OptionsPopup.Spacing + // Capture title
					Constants.SingleLineHeight + OptionsPopup.Spacing + // Path
					Constants.SingleLineHeight + // Name Format
					Constants.SingleLineHeight + OptionsPopup.Spacing + // Tip
					Constants.SingleLineHeight + OptionsPopup.BigSpacing + // DateTime + Spacing
					Constants.SingleLineHeight + OptionsPopup.Spacing; // Reset tips + Bot margin

				for (int i = 0; i < this.window.modules.Count; i++)
					height += Constants.SingleLineHeight + OptionsPopup.Spacing + this.window.modules[i].GetGUIModuleHeight(this.window.Hierarchy) + OptionsPopup.Spacing;

				if (this.window.Hierarchy.IsClientConnected() == true && this.window.cameraConnected == true)
					height += 44F + OptionsPopup.BigSpacing; // Render Texture helper

				return new Vector2(350F, height);
			}

			public override void	OnGUI(Rect r)
			{
				// Setup margins.
				r.x = OptionsPopup.Spacing;
				r.width -= OptionsPopup.Spacing + OptionsPopup.Spacing;
				r.y = OptionsPopup.Spacing;

				r.height = Constants.SingleLineHeight;
				EditorGUI.BeginChangeCheck();
				this.window.targetRefresh = EditorGUI.IntSlider(r, LC.G("NGCamera_TargetRefresh"), this.window.targetRefresh, NGServerCamera.TargetRefreshMin, NGServerCamera.TargetRefreshMax);
				if (EditorGUI.EndChangeCheck() == true && this.window.cameraConnected == true)
				{
					this.window.targetRefresh = Mathf.Clamp(this.window.targetRefresh, NGServerCamera.TargetRefreshMin, NGServerCamera.TargetRefreshMax);
					this.window.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.TargetRefresh, SettingType.Integer, this.window.targetRefresh));
				}
				r.y += r.height + OptionsPopup.BigSpacing;

				bool	cameraIsRunning = this.window.Hierarchy.IsClientConnected() == true && this.window.cameraConnected == true;

				GUI.Box(r, string.Empty, GeneralStyles.Toolbar);
				this.window.overrideRenderTexture = GUI.Toggle(r, this.window.overrideRenderTexture, "Override Camera Render Texture" + (cameraIsRunning == true ? " (" + this.window.TextureWidth + "x" + this.window.TextureHeight + ")" : string.Empty));
				r.y += r.height + OptionsPopup.BigSpacing;

				EditorGUI.BeginDisabledGroup(cameraIsRunning == true);
				{
					if (this.window.overrideRenderTexture == false)
					{
						this.window.shrinkRatio = EditorGUI.Slider(r, "Shrink Ratio", this.window.shrinkRatio, .01F, 1F);
						r.y += r.height;

						GUI.Label(r, "Auto set resolution based on the ratio and the device's screen.", GeneralStyles.SmallLabel);
						r.y += r.height + OptionsPopup.Spacing + OptionsPopup.BigSpacing;
					}
					else
					{
						using (LabelWidthRestorer.Get(80F))
						{
							r.width = this.editorWindow.position.width * .4F;
							this.window.width = EditorGUI.IntField(r, LC.G("NGCamera_Resolution") + " W", this.window.width);
							if (this.window.width < 1)
								this.window.width = 1;
							r.x += r.width;
						}

						using (LabelWidthRestorer.Get(16F))
						{
							r.width = this.editorWindow.position.width * .2F;
							this.window.height = EditorGUI.IntField(r, "H", this.window.height);
							r.x += r.width;
							if (this.window.height < 1)
								this.window.height = 1;
						}

						r.width = this.editorWindow.position.width * .4F - OptionsPopup.Spacing;
						using (LabelWidthRestorer.Get(45F))
							this.window.depth = (RenderTextureDepth)NGEditorGUILayout.EnumPopup(r, LC.G("NGCamera_Depth"), this.window.depth);
						r.y += r.height + OptionsPopup.Spacing;

						r.x = OptionsPopup.Spacing;
						r.width = this.editorWindow.position.width - OptionsPopup.Spacing - OptionsPopup.Spacing;
						this.window.renderTextureFormat = (RenderTextureFormat)NGEditorGUILayout.EnumPopup(r, LC.G("NGCamera_RenderTextureFormat"), this.window.renderTextureFormat);
						r.y += r.height + OptionsPopup.BigSpacing;
					}
				}
				EditorGUI.EndDisabledGroup();

				if (cameraIsRunning == true)
				{
					r.height = 44F;
					EditorGUI.HelpBox(r, "You can not alter texture's settings if " + NGRemoteCameraWindow.NormalTitle + " is running.", MessageType.Info);
					r.y += r.height + OptionsPopup.BigSpacing;
				}

				r.height = Constants.SingleLineHeight;
				GUI.Box(r, string.Empty, GeneralStyles.Toolbar);
				GUI.Label(r, "Modules");
				r.y += r.height + OptionsPopup.BigSpacing;

				for (int i = 0; i < this.window.modules.Count; i++)
				{
					bool	available = false;

					if (this.window.cameraConnected == true)
					{
						if (this.window.modules[i].moduleID != ScreenshotModule.ModuleID)
						{
							for (int j = 0; j < this.window.modulesAvailable.Length; j++)
							{
								if (this.window.modulesAvailable[j] == this.window.modules[i].moduleID)
								{
									available = true;
									break;
								}
							}
						}
					}
					else // Compare name not Type, for performance.
						available = this.window.modules[i].name != "Screenshot";

					EditorGUI.BeginDisabledGroup(!available);
					{
						EditorGUI.BeginChangeCheck();
						r.height = Constants.SingleLineHeight;
						this.window.modules[i].active = NGEditorGUILayout.Switch(r, this.window.modules[i].name, this.window.modules[i].active);
						if (EditorGUI.EndChangeCheck() == true && this.window.cameraConnected == true)
							this.window.Hierarchy.Client.AddPacket(new ClientToggleModulePacket(this.window.modules[i].moduleID, this.window.modules[i].active));
						r.y += r.height + OptionsPopup.Spacing;
					}
					EditorGUI.EndDisabledGroup();

					r.height = this.window.modules[i].GetGUIModuleHeight(this.window.Hierarchy);
					this.window.modules[i].OnGUIModule(r, this.window.Hierarchy);
					r.y += r.height + OptionsPopup.Spacing;
				}

				r.height = Constants.SingleLineHeight;
				GUI.Box(r, string.Empty, GeneralStyles.Toolbar);
				GUI.Label(r, "Capture");
				r.y += r.height + OptionsPopup.Spacing;

				using (LabelWidthRestorer.Get(85F))
				{
					this.window.capturePath = NGEditorGUILayout.OpenFolderField(r, "Path", this.window.capturePath);
					r.y += r.height + OptionsPopup.Spacing;

					r.width -= 40F;
					this.window.captureNameFormat = EditorGUI.TextField(r, "Name Format", this.window.captureNameFormat);
					r.x += r.width;
					r.width = 40F;
					this.window.captureExportFormat = (CaptureFormat)NGEditorGUILayout.EnumPopup(r, this.window.captureExportFormat);
					Rect	r2 = r;
					r2.y -= 1F;
					GUI.Label(r2, ".");
					r.y += r.height;
				}

				r.x = OptionsPopup.Spacing;
				r.width = this.editorWindow.position.width - OptionsPopup.Spacing - OptionsPopup.Spacing;
				GUI.Label(r, "Name Format is based on .NET DateTime and without extension.", GeneralStyles.SmallLabel);
				r.y += r.height + OptionsPopup.Spacing;

				if (GUI.Button(r, "Help DateTime") == true)
					Help.BrowseURL("https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings");
				r.y += r.height + OptionsPopup.BigSpacing;

				if (GUI.Button(r, "Reset tips") == true)
					this.window.tipsHelper.EraseAll();
			}
		}

		private class GhostCameraOptionsPopup : PopupWindowContent
		{
			private readonly CameraOverrideSettings	target;
			private readonly NGRemoteCameraWindow	window;
			private readonly bool					updateNetwork;
			private readonly int					cameraGameObjectID;
			private readonly int					cameraID;

			public	GhostCameraOptionsPopup(CameraOverrideSettings target, NGRemoteCameraWindow window, bool updateNetwork, int cameraGameObjectID, int cameraID)
			{
				this.target = target;
				this.window = window;
				this.updateNetwork = updateNetwork;
				this.cameraGameObjectID = cameraGameObjectID;
				this.cameraID = cameraID;
			}

			public override Vector2	GetWindowSize()
			{
				if (this.window.Hierarchy != null && this.window.cameraConnected == true && this.window.Hierarchy.Layers != null)
					return new Vector2(300F, 16F * (Constants.SingleLineHeight + EditorGUIUtility.standardVerticalSpacing) + 5F); // Ghost camera warning + Culling Mask
				return new Vector2(300F, 15F * (Constants.SingleLineHeight + EditorGUIUtility.standardVerticalSpacing) + 5F); // Clipping Plane/Viewport Rect margins
			}

			public override void	OnGUI(Rect r)
			{
				if (this.window.toolbarStyle == null)
				{
					this.window.toolbarStyle = new GUIStyle("Toolbar");
					this.window.toolbarStyle.fixedHeight = 0F;
					this.window.toolbarStyle.stretchHeight = true;
				}

				Rect	r2 = GUILayoutUtility.GetRect(this.editorWindow.position.width, Constants.SingleLineHeight);

				GUI.Box(r2, GUIContent.none, GeneralStyles.Toolbar);
				GUI.Label(r2, LC.G("NGCamera_CameraSettings"));

				r2.xMin = r2.xMax - 120F;
				r2.width = 60F;
				if (this.cameraGameObjectID != 0 && GUI.Button(r2, "Select", GeneralStyles.ToolbarButton) == true)
				{
					this.window.Hierarchy.SelectGameObject(this.cameraGameObjectID);
					NGRemoteInspectorWindow.Open();
				}
				r2.x += r2.width;

				if (this.cameraID != 0)
				{
					if (this.window.resettingCameraID == this.cameraID)
					{
						GUI.DrawTexture(r2, GeneralStyles.StatusWheel.image, ScaleMode.ScaleToFit);
						this.editorWindow.Repaint();
					}
					else if (GUI.Button(r2, "Reset", GeneralStyles.ToolbarButton) == true)
					{
						this.window.resettingCameraID = this.cameraID;
						this.window.Hierarchy.Client.AddPacket(new ClientResetCameraSettingsPacket(this.cameraID), this.OnCameraSettingsReset);
						this.editorWindow.Repaint();
					}
				}

				bool	cameraIsRunning = this.window.Hierarchy.IsClientConnected() == true && this.window.cameraConnected == true;

				EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
				{
					this.target.overrideClearFlags = GUILayout.Toggle(this.target.overrideClearFlags, GUIContent.none, GUILayoutOptionPool.Width(14F));

					EditorGUI.BeginDisabledGroup(this.target.overrideClearFlags == false);
					{
						EditorGUI.BeginChangeCheck();
						this.target.clearFlags = EditorGUILayout.IntPopup(LC.G("NGCamera_ClearFlags"), this.target.clearFlags, NGRemoteCameraWindow.ClearFlagsLabels, NGRemoteCameraWindow.ClearFlagsValues);
						if (EditorGUI.EndChangeCheck() == true && this.updateNetwork == true)
							this.window.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraClearFlags, SettingType.Integer, this.target.clearFlags));
					}
					EditorGUI.EndDisabledGroup();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
				{
					this.target.overrideBackground = GUILayout.Toggle(this.target.overrideBackground, GUIContent.none, GUILayoutOptionPool.Width(14F));

					EditorGUI.BeginDisabledGroup(this.target.overrideBackground == false);
					{
						EditorGUI.BeginChangeCheck();
						this.target.background = EditorGUILayout.ColorField(LC.G("NGCamera_Background"), this.target.background);
						if (EditorGUI.EndChangeCheck() == true && this.updateNetwork == true)
							this.window.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraBackground, SettingType.Color, this.target.background));
					}
					EditorGUI.EndDisabledGroup();
				}
				EditorGUILayout.EndHorizontal();

				if (cameraIsRunning == true && this.window.Hierarchy.Layers != null)
				{
					EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
					{
						this.target.overrideCullingMask = GUILayout.Toggle(this.target.overrideCullingMask, GUIContent.none, GUILayoutOptionPool.Width(14F));

						EditorGUI.BeginDisabledGroup(this.target.overrideCullingMask == false);
						{
							EditorGUI.BeginChangeCheck();
							this.target.cullingMask = EditorGUILayout.MaskField(LC.G("NGCamera_CullingMask"), this.target.cullingMask, this.window.Hierarchy.Layers);
							if (EditorGUI.EndChangeCheck() == true && this.updateNetwork == true)
								this.window.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraCullingMask, SettingType.Integer, this.target.cullingMask));
						}
						EditorGUI.EndDisabledGroup();
					}
					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
				{
					this.target.overrideProjection = GUILayout.Toggle(this.target.overrideProjection, GUIContent.none, GUILayoutOptionPool.Width(14F));

					EditorGUI.BeginDisabledGroup(this.target.overrideProjection == false);
					{
						EditorGUI.BeginChangeCheck();
						this.target.projection = EditorGUILayout.Popup(LC.G("NGCamera_Projection"), this.target.projection, NGRemoteCameraWindow.ProjectionLabels);
						if (EditorGUI.EndChangeCheck() == true && this.updateNetwork == true)
							this.window.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraProjection, SettingType.Integer, this.target.projection));
					}
					EditorGUI.EndDisabledGroup();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
				{
					if (this.target.projection == 0)
					{
						this.target.overrideFieldOfView = GUILayout.Toggle(this.target.overrideFieldOfView, GUIContent.none, GUILayoutOptionPool.Width(14F));

						EditorGUI.BeginDisabledGroup(this.target.overrideFieldOfView == false);
						{
							EditorGUI.BeginChangeCheck();
							this.target.fieldOfView = EditorGUILayout.Slider(LC.G("NGCamera_FieldOfView"), this.target.fieldOfView, 1F, 179F);
							if (EditorGUI.EndChangeCheck() == true && this.updateNetwork == true)
								this.window.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraFieldOfView, SettingType.Single, this.target.fieldOfView));
						}
						EditorGUI.EndDisabledGroup();
					}
					else
					{
						this.target.overrideSize = GUILayout.Toggle(this.target.overrideSize, GUIContent.none, GUILayoutOptionPool.Width(14F));

						EditorGUI.BeginDisabledGroup(this.target.overrideSize == false);
						{
							EditorGUI.BeginChangeCheck();
							this.target.size = EditorGUILayout.FloatField(LC.G("NGCamera_Size"), this.target.size);
							if (EditorGUI.EndChangeCheck() == true && this.updateNetwork == true)
								this.window.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraSize, SettingType.Single, this.target.size));
						}
						EditorGUI.EndDisabledGroup();
					}
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal(this.window.toolbarStyle, GUILayoutOptionPool.Height(40F));
				{
					this.target.overrideClippingPlanes = GUILayout.Toggle(this.target.overrideClippingPlanes, GUIContent.none, GUILayoutOptionPool.Width(14F));

					EditorGUI.BeginDisabledGroup(this.target.overrideClippingPlanes == false);
					{
						EditorGUILayout.LabelField(LC.G("NGCamera_ClippingPlane"), GUILayoutOptionPool.Width(EditorGUIUtility.labelWidth));

						EditorGUILayout.BeginVertical();
						{
							using (LabelWidthRestorer.Get(50F))
							{
								EditorGUI.BeginChangeCheck();
								this.target.clippingPlanesNear = EditorGUILayout.FloatField(LC.G("NGCamera_Near"), this.target.clippingPlanesNear);
								if (EditorGUI.EndChangeCheck() == true)
								{
									if (this.target.clippingPlanesNear < .01F)
										this.target.clippingPlanesNear = .01F;

									if (this.updateNetwork == true)
										this.window.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraClippingPlanesNear, SettingType.Single, this.target.clippingPlanesNear));

									if (this.target.clippingPlanesNear >= this.target.clippingPlanesFar)
									{
										this.target.clippingPlanesFar = this.target.clippingPlanesNear + .01F;

										if (this.updateNetwork == true)
											this.window.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraClippingPlanesFar, SettingType.Single, this.target.clippingPlanesFar));
									}
								}

								EditorGUI.BeginChangeCheck();
								this.target.clippingPlanesFar = EditorGUILayout.FloatField(LC.G("NGCamera_Far"), this.target.clippingPlanesFar);
								if (EditorGUI.EndChangeCheck() == true)
								{
									if (this.target.clippingPlanesFar <= this.target.clippingPlanesNear)
										this.target.clippingPlanesFar = this.target.clippingPlanesNear + .01F;

									if (this.updateNetwork == true)
										this.window.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraClippingPlanesFar, SettingType.Single, this.target.clippingPlanesFar));
								}
							}
						}
						EditorGUILayout.EndVertical();
					}
					EditorGUI.EndDisabledGroup();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal(this.window.toolbarStyle, GUILayoutOptionPool.Height(55F));
				{
					this.target.overrideViewportRect = GUILayout.Toggle(this.target.overrideViewportRect, GUIContent.none, GUILayoutOptionPool.Width(14F));

					EditorGUI.BeginDisabledGroup(this.target.overrideViewportRect == false);
					{
						EditorGUI.BeginChangeCheck();
						this.target.viewportRect = EditorGUILayout.RectField(LC.G("NGCamera_ViewportRect"), this.target.viewportRect);
						if (EditorGUI.EndChangeCheck() == true && this.updateNetwork == true)
							this.window.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraViewportRect, SettingType.Rect, this.target.viewportRect));
					}
					EditorGUI.EndDisabledGroup();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
				{
					this.target.overrideCdepth = GUILayout.Toggle(this.target.overrideCdepth, GUIContent.none, GUILayoutOptionPool.Width(14F));

					EditorGUI.BeginDisabledGroup(this.target.overrideCdepth == false);
					{
						EditorGUI.BeginChangeCheck();
						this.target.cdepth = EditorGUILayout.FloatField(LC.G("NGCamera_Depth"), this.target.cdepth);
						if (EditorGUI.EndChangeCheck() == true && this.updateNetwork == true)
						{
							this.target.cdepth = Mathf.Clamp(this.target.cdepth, -100F, 100F);
							this.window.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraDepth, SettingType.Single, this.target.cdepth));
						}
					}
					EditorGUI.EndDisabledGroup();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
				{
					this.target.overrideRenderingPath = GUILayout.Toggle(this.target.overrideRenderingPath, GUIContent.none, GUILayoutOptionPool.Width(14F));

					EditorGUI.BeginDisabledGroup(this.target.overrideRenderingPath == false);
					{
						EditorGUI.BeginChangeCheck();
						this.target.renderingPath = EditorGUILayout.IntPopup(LC.G("NGCamera_RenderingPath"), this.target.renderingPath, NGRemoteCameraWindow.RenderingPathLabels, NGRemoteCameraWindow.RenderingPathValues);
						if (EditorGUI.EndChangeCheck() == true && this.updateNetwork == true)
							this.window.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraRenderingPath, SettingType.Integer, this.target.renderingPath));
					}
					EditorGUI.EndDisabledGroup();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
				{
					this.target.overrideOcclusionCulling = GUILayout.Toggle(this.target.overrideOcclusionCulling, GUIContent.none, GUILayoutOptionPool.Width(14F));

					EditorGUI.BeginDisabledGroup(this.target.overrideOcclusionCulling == false);
					{
						EditorGUI.BeginChangeCheck();
						this.target.occlusionCulling = EditorGUILayout.Toggle(LC.G("NGCamera_OcclusionCulling"), this.target.occlusionCulling);
						if (EditorGUI.EndChangeCheck() == true && this.updateNetwork == true)
							this.window.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraOcclusionCulling, SettingType.Boolean, this.target.occlusionCulling));
					}
					EditorGUI.EndDisabledGroup();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
				{
					this.target.overrideHDR = GUILayout.Toggle(this.target.overrideHDR, GUIContent.none, GUILayoutOptionPool.Width(14F));

					EditorGUI.BeginDisabledGroup(this.target.overrideHDR == false);
					{
						EditorGUI.BeginChangeCheck();
						this.target.HDR = EditorGUILayout.Toggle(LC.G("NGCamera_HDR"), this.target.HDR);
						if (EditorGUI.EndChangeCheck() == true && this.updateNetwork == true)
							this.window.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraHDR, SettingType.Boolean, this.target.HDR));
					}
					EditorGUI.EndDisabledGroup();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
				{
					this.target.overrideTargetDisplay = GUILayout.Toggle(this.target.overrideTargetDisplay, GUIContent.none, GUILayoutOptionPool.Width(14F));

					EditorGUI.BeginDisabledGroup(this.target.overrideTargetDisplay == false);
					{
						EditorGUI.BeginChangeCheck();
						this.target.targetDisplay = EditorGUILayout.Popup(LC.G("NGCamera_TargetDisplay"), this.target.targetDisplay, NGRemoteCameraWindow.DisplayLabels);
						if (EditorGUI.EndChangeCheck() == true && this.updateNetwork == true)
							this.window.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraTargetDisplay, SettingType.Integer, this.target.targetDisplay));
					}
					EditorGUI.EndDisabledGroup();
				}
				EditorGUILayout.EndHorizontal();
			}

			private void	OnCameraSettingsReset(ResponsePacket p)
			{
				if (p.CheckPacketStatus() == true)
				{
					ServerResetCameraSettingsPacket	packet = p as ServerResetCameraSettingsPacket;

					this.target.clearFlags = packet.clearFlags;
					this.target.background = packet.background;
					this.target.cullingMask = packet.cullingMask;
					this.target.projection = packet.projection;
					this.target.fieldOfView = packet.fieldOfView;
					this.target.size = packet.size;
					this.target.clippingPlanesNear = packet.clippingPlanesNear;
					this.target.clippingPlanesFar = packet.clippingPlanesFar;
					this.target.viewportRect = packet.viewportRect;
					this.target.cdepth = packet.cdepth;
					this.target.renderingPath = packet.renderingPath;
					this.target.occlusionCulling = packet.occlusionCulling;
					this.target.HDR = packet.HDR;
					this.target.targetDisplay = packet.targetDisplay;

					// Add some delay to show feedback to the user.
					Utility.RegisterIntervalCallback(() => this.window.resettingCameraID = 0, 10, 1);

					this.editorWindow.Repaint();
				}
			}
		}

		private class CameraSelectorPopup : PopupWindowContent
		{
			public static Color	SelectedCameraColor = Color.cyan;
			public static Color	GhostCameraColor = Color.green;

			private readonly NGRemoteCameraWindow	window;

			public	CameraSelectorPopup(NGRemoteCameraWindow window)
			{
				this.window = window;
			}

			public override void	OnOpen()
			{
				base.OnOpen();

				this.editorWindow.wantsMouseMove = true;
			}

			public override Vector2	GetWindowSize()
			{
				float	height = this.window.cameraIDs.Length * 18F;

				return new Vector2(350F, height);
			}

			public override void	OnGUI(Rect r)
			{
				for (int i = 0; i < this.window.cameraIDs.Length; i++)
				{
					EditorGUILayout.BeginHorizontal();
					{
						using (ColorContentRestorer.Get(this.window.ghostCameraID == this.window.cameraIDs[i], CameraSelectorPopup.GhostCameraColor))
						using (ColorContentRestorer.Get(this.window.streamingCameraSelected == i, CameraSelectorPopup.SelectedCameraColor))
						{
							if (GUILayout.Button(this.window.cameraNames[i], GeneralStyles.ToolbarButton, GUILayoutOptionPool.Width(300)) == true)
							{
								if (this.window.streamingCameraSelected == i)
									this.editorWindow.Close();
								else
								{
									this.window.streamingCameraSelected = i;
									this.window.Hierarchy.Client.AddPacket(new ClientPickCameraPacket(this.window.cameraIDs[i]));
								}
							}
						}

						if (this.window.cameraGameObjectIDs[i] != 0 && GUILayout.Button("Edit", GeneralStyles.ToolbarButton, GUILayoutOptionPool.Width(50)) == true)
						{
							this.window.Hierarchy.SelectGameObject(this.window.cameraGameObjectIDs[i]);
							NGRemoteInspectorWindow.Open();
						}
					}
					EditorGUILayout.EndHorizontal();
				}
			}
		}

		private enum Display
		{
			Camera,
			Selection
		}

		private enum RenderTextureDepth
		{
			NoDepth = 0,
			Depth16 = 16,
			Depth24 = 24,
		}

		private enum CaptureFormat
		{
			JPG,
			PNG,
			EXR
		}

		private enum RaycastState
		{
			None,
			RequestingRaycast,
			ResultReceived
		}

		public const string				NormalTitle = "NG Remote Camera";
		public const string				ShortTitle = "NG R Camera";
		public readonly static Color	HighlightSelectedCamera = Color.yellow;
		public readonly static Color	DefaultArrowColor = Color.white;
		public readonly static Color	HighlightArrowColor = Color.black;
		public readonly static Color	SpeedHighlightArrowColor = Color.yellow;
		public readonly static Color	PanelBackgroundColor = Color.gray * .8F;
		public readonly static int[]	ClearFlagsValues = new int[] { 1, 2, 3, 4 };
		public readonly static string[]	ClearFlagsLabels = new string[] { "Skybox", "Solid Color", "Depth only", "Don't Clear" };
		public readonly static string[]	ProjectionLabels = new string[] { "Perspective", "Orthographic" };
		public readonly static int[]	RenderingPathValues = new int[] { -1, 1, 3, 0, 2 };
		public readonly static string[]	RenderingPathLabels = new string[] { "Use Player Settings", "Forward", "Deferred", "Legacy Vertex Lit", "Legacy Deferred (light prepass)" };
		public readonly static string[]	DisplayLabels = new string[] { "Display 1", "Display 2", "Display 3", "Display 4", "Display 5", "Display 6", "Display 7", "Display 8" };

		private const float				MaxCameraRecordDuration = 10F;
		private static readonly string	FreeAdContent = NGRemoteCameraWindow.NormalTitle + " is restrained to " + NGRemoteCameraWindow.MaxCameraRecordDuration + " seconds recording duration.";

		[SerializeField]
		private int	targetRefresh = 24;

		[SerializeField]
		private float	xAxisSensitivity = .1F;
		[SerializeField]
		private float	yAxisSensitivity = .1F;
		[SerializeField]
		private float	moveSpeed = 1F;
		[SerializeField]
		private float	zoomSpeed = 1F;

		[SerializeField]
		private float	recordLastSeconds = 15F;

		[SerializeField]
		private bool		displayGhostPanel = false;
		[SerializeField]
		private bool		displayOverlay = false;

		[SerializeField]
		private float				shrinkRatio = 1F;
		[SerializeField]
		private int					width = 800;
		[SerializeField]
		private int					height = 600;
		[SerializeField]
		private RenderTextureDepth	depth = RenderTextureDepth.Depth24;
		[SerializeField]
		private RenderTextureFormat	renderTextureFormat = RenderTextureFormat.ARGB32;

		[SerializeField]
		private bool			overrideRenderTexture = false;
		[SerializeField]
		private CaptureFormat 	captureExportFormat = CaptureFormat.JPG;
		[SerializeField]
		private string			capturePath;
		[SerializeField]
		private string			captureNameFormat = "yyyy_MM_dd_HH_mm_ss";

		[NonSerialized]
		private CameraOverrideSettings	ghostCameraOverrides;
		[NonSerialized]
		private CameraOverrideSettings[]	camerasOverrides;
		private CameraOverrideSettings		TargetOverrides { get { return this.cameraSelected == -1 ? this.ghostCameraOverrides : this.camerasOverrides[this.cameraSelected]; } }

		[SerializeField]
		private Display	display = Display.Camera;

		[NonSerialized]
		private bool	cameraConnected;

		[NonSerialized]
		private byte[]		modulesAvailable;

		[NonSerialized]
		private int			cameraSelected = -1;
		[NonSerialized]
		private Vector2		camerasScrollPosition;
		[NonSerialized]
		private int			streamingCameraSelected;
		[NonSerialized]
		private int[]		cameraIDs;
		[NonSerialized]
		private int[]		cameraGameObjectIDs;
		[NonSerialized]
		private string[]	cameraNames;
		[NonSerialized]
		private int[][]		cameraComponentsIDs;
		[NonSerialized]
		private string[][]	cameraComponentsTypes;
		[NonSerialized]
		private int			ghostCameraID;
		[NonSerialized]
		private int			remoteFPS;
		[NonSerialized]
		private int			receivedTexturesCounter;
		[NonSerialized]
		private double		nextFPStime;

		[NonSerialized]
		private double	lastRaycastClick;
		[NonSerialized]
		private Vector2	lastRaycastMousePosition;
		[NonSerialized]
		private bool	hasDoubleRaycast;

		[NonSerialized]
		private RaycastState	raycastState;
		[NonSerialized]
		private Rect			raycastResultRect;
		[NonSerialized]
		private int[]			raycastResultIDs;
		[NonSerialized]
		private string[]		raycastResultNames;
		[NonSerialized]
		private string[]		raycastResultHierarchies;

		[NonSerialized]
		private bool	moveForward;
		[NonSerialized]
		private bool	moveBackward;
		[NonSerialized]
		private bool	moveLeft;
		[NonSerialized]
		private bool	moveRight;
		[NonSerialized]
		private Vector3	camPosition;
		[NonSerialized]
		private Vector2	camRotation;

		private bool	IsGhostCameraFocused { get { return this.streamingCameraSelected >= 0 && this.ghostCameraID == this.cameraIDs[this.streamingCameraSelected]; } }
		[NonSerialized]
		private bool	hasWindowFocus = false;
		[NonSerialized]
		private Vector2	dragPos;
		[NonSerialized]
		private Vector3	dragCamPosition;
		[NonSerialized]
		private Vector2	dragCamRotation;
		[NonSerialized]
		private bool	dragging = false;

		[NonSerialized]
		private Rect	textureRect;
		[NonSerialized]
		private Rect	panelRect;
		[NonSerialized]
		private Rect	togglePanelRect;
		[NonSerialized]
		private Rect	overlayInputRect;

		[NonSerialized]
		private GUIStyle	toolbarStyle;

		[NonSerialized]
		private TipsHelper	tipsHelper;

		public float	RecordLastSeconds { get { return this.recordLastSeconds; } }
		[NonSerialized]
		private int		textureWidth;
		public int		TextureWidth { get { return this.textureWidth; } }
		[NonSerialized]
		private int		textureHeight;
		public int		TextureHeight { get { return this.textureHeight; } }

		[NonSerialized]
		private List<CameraDataModuleEditor>	modules;
		public List<CameraDataModuleEditor>		Modules { get { return this.modules; } }

		[NonSerialized]
		private ScreenshotModuleEditor	textureModule;

		[NonSerialized]
		private int	stickyTransformID;

		[NonSerialized]
		private bool	keepCursorCenter = true;

		[NonSerialized]
		private int	resettingCameraID;

		[MenuItem(Constants.MenuItemPath + NGRemoteCameraWindow.NormalTitle, priority = Constants.MenuItemPriority + 220), Hotkey(NGRemoteCameraWindow.Title)]
		public static void	Open()
		{
			Utility.OpenWindow<NGRemoteCameraWindow>(NGRemoteCameraWindow.ShortTitle);
		}

		protected override void	OnEnable()
		{
			// Load before OnEnable() to prevent title to be restored and lose its icon.
			Utility.LoadEditorPref(this, NGEditorPrefs.GetPerProjectPrefix());

			base.OnEnable();

			Metrics.UseTool(21); // NGRemoteCamera

			this.ghostCameraOverrides = new CameraOverrideSettings();

			this.panelRect = new Rect(32F, 0F, 0F, 76F);
			this.togglePanelRect = new Rect(0F, 0F, 32F, 32F);
			this.overlayInputRect = new Rect(100F, 0F, 60F, 60F);

			this.tipsHelper = new TipsHelper(NGRemoteCameraWindow.NormalTitle + ".tips");

			// Initialize modules.
			this.modules = new List<CameraDataModuleEditor>();

			foreach (Type module in Utility.EachNGTSubClassesOf(typeof(CameraDataModuleEditor)))
			{
				CameraDataModuleEditor	instance = Activator.CreateInstance(module) as CameraDataModuleEditor;
				this.modules.Add(instance);

				if (instance.moduleID == ScreenshotModule.ModuleID)
					this.textureModule = instance as ScreenshotModuleEditor;
			}

			this.modules.Sort((a, b) => b.priority - a.priority);

			this.wantsMouseMove = true;
		}

		protected override void	OnDisable()
		{
			base.OnDisable();

			this.tipsHelper.Save();

			this.ghostCameraOverrides.Save();

			Utility.SaveEditorPref(this, NGEditorPrefs.GetPerProjectPrefix());
		}

		protected virtual void	OnFocus()
		{
			this.hasWindowFocus = true;
		}

		protected virtual void	OnLostFocus()
		{
			this.hasWindowFocus = false;

			if (this.Hierarchy == null || this.Hierarchy.Client == null || this.cameraConnected == false)
				return;

			this.moveForward = false;
			this.moveBackward = false;
			this.moveLeft = false;
			this.moveRight = false;

			if (this.IsGhostCameraFocused == true)
				this.Hierarchy.Client.AddPacket(new ClientSendCameraInputPacket(false, false, false, false, 0F));

			this.wantsMouseMove = false;

			if (Application.platform == RuntimePlatform.WindowsEditor && this.keepCursorCenter == true)
				Cursor.visible = true;

			this.Repaint();
		}

		protected virtual void	Update()
		{
			if (EditorApplication.timeSinceStartup >= this.nextFPStime)
			{
				this.remoteFPS = this.receivedTexturesCounter;
				this.receivedTexturesCounter = 0;
				this.nextFPStime = EditorApplication.timeSinceStartup + 1D;
			}
		}

		protected override void	OnHierarchyInit()
		{
			base.OnHierarchyInit();

			ConnectionsManager.Executer.HandlePacket(RemoteScenePacketId.Camera_NotifyAllCameras, this.OnNotifyAllCamerasReceived);
			ConnectionsManager.Executer.HandlePacket(RemoteScenePacketId.Camera_NotifyCameraTransform, this.OnNotifyCameraTransformReceived);
			ConnectionsManager.Executer.HandlePacket(RemoteScenePacketId.Camera_NotifyCameraData, this.OnNotifyCameraDataReceived);

			this.camPosition = Vector3.zero;
			this.camRotation = Vector2.zero;
		}

		protected override void	OnHierarchyUninit()
		{
			base.OnHierarchyUninit();

			ConnectionsManager.Executer.UnhandlePacket(RemoteScenePacketId.Camera_NotifyAllCameras, this.OnNotifyAllCamerasReceived);
			ConnectionsManager.Executer.UnhandlePacket(RemoteScenePacketId.Camera_NotifyCameraTransform, this.OnNotifyCameraTransformReceived);
			ConnectionsManager.Executer.UnhandlePacket(RemoteScenePacketId.Camera_NotifyCameraData, this.OnNotifyCameraDataReceived);
		}

		protected override void	OnHierarchyConnected()
		{
			base.OnHierarchyConnected();

			this.Clean();

			this.Hierarchy.GameObjectContextMenu += this.OnGameObjectContextMenu;
		}

		protected override void	OnHierarchyDisconnected()
		{
			base.OnHierarchyDisconnected();

			this.stickyTransformID = 0;
			this.cameraConnected = false;

			if (this.camerasOverrides != null)
			{
				for (int i = 0; i < this.camerasOverrides.Length; i++)
					this.camerasOverrides[i].Save();
			}

			this.Hierarchy.GameObjectContextMenu -= this.OnGameObjectContextMenu;
		}

		protected override void	OnGUIHeader()
		{
			FreeLicenseOverlay.First(this, NGTools.NGRemoteScene.NGAssemblyInfo.Name + " Pro", NGRemoteCameraWindow.FreeAdContent);

			bool	clientConnected = this.Hierarchy.IsClientConnected();

			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				if (GUILayout.Button("☰", "GV Gizmo DropDown") == true)
					PopupWindow.Show(new Rect(0F, 16F, 0F, 0F), new OptionsPopup(this));
				XGUIHighlightManager.DrawHighlightLayout(NGRemoteCameraWindow.NormalTitle + ".Options", this);

				if (NGEditorGUILayout.OutlineToggle("Stream Camera", this.display == Display.Camera) == true)
					this.display = Display.Camera;

				if (NGEditorGUILayout.OutlineToggle("Cameras", this.display == Display.Selection) == true)
					this.display = Display.Selection;

				if (this.display == Display.Camera)
				{
					GUILayout.FlexibleSpace();

					Utility.content.text = LC.G("NGCamera_RecordDuration");
					using (LabelWidthRestorer.Get(GUI.skin.label.CalcSize(Utility.content).x))
						this.recordLastSeconds = EditorGUILayout.DelayedFloatField(Utility.content, this.recordLastSeconds);

					if (this.CheckMaxCameraRecordDuration(this.recordLastSeconds) == false)
						this.recordLastSeconds = NGRemoteCameraWindow.MaxCameraRecordDuration;

					EditorGUI.BeginDisabledGroup(clientConnected == false || this.cameraConnected == false);
					{
						if (GUILayout.Button(LC.G("NGCamera_ExportReplay"), GeneralStyles.ToolbarButton) == true)
							Utility.OpenWindow<NGReplayWindow>(NGReplayWindow.Title, true, w => w.AddReplay(new Replay(this)));

						GUILayout.FlexibleSpace();

						if (GUILayout.Button("Capture", GeneralStyles.ToolbarButton) == true)
						{
							try
							{
								Texture2D	texture = this.textureModule.Texture;

								if (texture != null)
								{
									string	path = Path.Combine(this.capturePath, DateTime.Now.ToString(this.captureNameFormat, CultureInfo.InvariantCulture));
									byte[]	data;

									if (this.captureExportFormat == CaptureFormat.JPG)
									{
										data = texture.EncodeToJPG();
										path += ".jpg";
									}
									else if (this.captureExportFormat == CaptureFormat.PNG)
									{
										data = texture.EncodeToPNG();
										path += ".png";
									}
									else if (this.captureExportFormat == CaptureFormat.EXR)
									{
										data = texture.EncodeToEXR();
										path += ".exr";
									}
									else
										throw new Exception("Export format not handled.");

									Directory.CreateDirectory(this.capturePath);
									File.WriteAllBytes(path, data);
									InternalNGDebug.Log("Screenshot written into \"" + path + "\".");
								}
								else
									this.ShowNotification(new GUIContent("No Texture available."));
							}
							catch (Exception ex)
							{
								InternalNGDebug.LogException(ex);
								this.ShowNotification(new GUIContent(ex.ToString()));
							}
						}
					}
					EditorGUI.EndDisabledGroup();
				}

				GUILayout.FlexibleSpace();

				if (clientConnected == false || this.cameraConnected == false)
				{
					EditorGUI.BeginDisabledGroup(!clientConnected);
					{
						if (GUILayout.Button(LC.G("NGCamera_Connect"), GeneralStyles.ToolbarButton) == true)
						{
							if (this.Hierarchy.BlockRequestChannel(this.GetHashCode()) == true)
							{
								List<byte>	modulesAvailable = new List<byte>();
								for (int i = 0; i < this.modules.Count; i++)
									modulesAvailable.Add(this.modules[i].moduleID);

								if (this.overrideRenderTexture == false)
									this.Hierarchy.Client.AddPacket(new ClientConnectPacket(this.shrinkRatio, this.targetRefresh, modulesAvailable.ToArray()), this.OnCameraConnected);
								else
									this.Hierarchy.Client.AddPacket(new ClientConnectPacket(this.width, this.height, (int)this.depth, this.targetRefresh, this.renderTextureFormat, modulesAvailable.ToArray()), this.OnCameraConnected);
							}
						}
						XGUIHighlightManager.DrawHighlightLayout(NGRemoteCameraWindow.Title + ".Connect", this);
					}
					EditorGUI.EndDisabledGroup();
				}
				else
				{
					GUILayout.Label(LC.G("NGCamera_FPS") + " " + this.remoteFPS);

					if (GUILayout.Button(LC.G("NGCamera_Disconnect"), GeneralStyles.ToolbarButton) == true)
					{
						this.Clean();
						this.Hierarchy.Client.AddPacket(new ClientDisconnectPacket());
					}
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		private void	OnCameraConnected(ResponsePacket p)
		{
			this.Hierarchy.UnblockRequestChannel(this.GetHashCode());

			if (p.CheckPacketStatus() == true)
			{
				ServerIsInitializedPacket	packet = p as ServerIsInitializedPacket;

				this.Hierarchy.Client.AddPacket(new ClientRequestAllCamerasPacket(), this.OnAllCamerasReceived);

				for (int i = 0; i < this.modules.Count; i++)
					this.modules[i].OnServerInitialized(this, this.Hierarchy.Client);

				this.modulesAvailable = packet.modules;
				this.textureWidth = packet.width;
				this.textureHeight = packet.height;

				this.cameraConnected = true;

				this.Repaint();
			}
		}

		protected override void	OnGUIConnected()
		{
			if (this.display == Display.Camera)
				this.DisplayStreamingCamera();
			else if (this.display == Display.Selection)
				this.DisplayCameras();
		}

		protected override void	OnGUIDisconnected()
		{
			if (this.display == Display.Selection)
				this.DisplayCameras();
			else
				base.OnGUIDisconnected();
		}

		private void	DisplayStreamingCamera()
		{
			if (this.cameraConnected == false)
			{
				this.textureRect = GUILayoutUtility.GetLastRect();
				this.textureRect.x = 0F;
				this.textureRect.width = this.position.width;
				this.textureRect.y += this.textureRect.height;
				this.textureRect.height = this.position.height - this.textureRect.y - 1F;

				GUI.Label(this.textureRect, "Camera not started.", GeneralStyles.BigCenterText);
				EditorGUIUtility.AddCursorRect(this.textureRect, MouseCursor.Link);

				if (Event.current.type == EventType.MouseDown && this.textureRect.Contains(Event.current.mousePosition) == true)
					XGUIHighlightManager.Highlight(NGRemoteCameraWindow.Title + ".Connect");
				return;
			}

			EditorGUILayout.BeginHorizontal();
			{
				if (this.cameraIDs != null)
				{
					using (LabelWidthRestorer.Get(70F))
					{
						if (GUILayout.Button(LC.G("NGCamera_Cameras"), GeneralStyles.ToolbarDropDown) == true)
							PopupWindow.Show(new Rect(0F, 36F, 0F, 0F), new CameraSelectorPopup(this));
						XGUIHighlightManager.DrawHighlightLayout(NGRemoteCameraWindow.Title + ".CameraSelector", this);

						if (this.streamingCameraSelected >= 0)
							GUILayout.Label(this.cameraNames[this.streamingCameraSelected], GUILayoutOptionPool.MaxWidth(200F), GUILayoutOptionPool.ExpandWidthFalse);
					}

					if (this.IsGhostCameraFocused == false && this.ghostCameraID != 0)
					{
						EditorGUI.BeginDisabledGroup(this.streamingCameraSelected >= 0 && this.ghostCameraID == this.cameraIDs[this.streamingCameraSelected]);
						{
							if (GUILayout.Button(LC.G("NGCamera_PickGhostCameraAtCamera"), GeneralStyles.ToolbarButton) == true)
							{
								this.Hierarchy.Client.AddPacket(new ClientPickGhostCameraAtCameraPacket(this.cameraIDs[this.streamingCameraSelected]));

								for (int i = 0; i < this.cameraIDs.Length; i++)
								{
									if (this.cameraIDs[i] == this.ghostCameraID)
									{
										this.streamingCameraSelected = i;
										break;
									}
								}
							}

							if (GUILayout.Button(LC.G("NGCamera_PickGhostCamera"), GeneralStyles.ToolbarButton) == true)
							{
								for (int i = 0; i < this.cameraIDs.Length; i++)
								{
									if (this.cameraIDs[i] == this.ghostCameraID)
									{
										this.streamingCameraSelected = i;
										this.Hierarchy.Client.AddPacket(new ClientPickCameraPacket(this.ghostCameraID));
										break;
									}
								}
							}
						}
						EditorGUI.EndDisabledGroup();
					}

					if (this.streamingCameraSelected >= 0 && this.cameraGameObjectIDs[this.streamingCameraSelected] != 0 && GUILayout.Button("Edit Camera", GeneralStyles.ToolbarButton) == true)
					{
						this.Hierarchy.SelectGameObject(this.cameraGameObjectIDs[this.streamingCameraSelected]);
						NGRemoteInspectorWindow.Open();
					}

					GUILayout.FlexibleSpace();

					if (this.IsGhostCameraFocused == true)
					{
						if (this.stickyTransformID == 0)
							GUILayout.Label(LC.G("NGCamera_NoStickyTransform"));
						else
							GUILayout.Label(this.Hierarchy.GetResourceName(typeof(Transform), this.stickyTransformID));

						if (GUILayout.Button(LC.G("NGCamera_OpenPickerTransform"), GeneralStyles.ToolbarButton) == true)
							this.Hierarchy.PickupResource(typeof(Transform), string.Empty, this.CreatePacketStickGhostCamera, this.OnAnchorUpdated, this.stickyTransformID);
					}
				}
			}
			EditorGUILayout.EndHorizontal();

			this.textureRect = GUILayoutUtility.GetLastRect();
			this.textureRect.x = 0F;
			this.textureRect.width = this.position.width;
			this.textureRect.y += this.textureRect.height;
			this.textureRect.height = this.position.height - this.textureRect.y - 1F;

			if (this.streamingCameraSelected >= 0)
			{
				// This has to be executed before DrawCameraGUI, because raycastResultRect is used inside.
				if (this.raycastState == RaycastState.ResultReceived)
				{
					if (this.hasDoubleRaycast == true)
					{
						this.hasDoubleRaycast = false;
						this.raycastState = RaycastState.None;

						if (this.raycastResultIDs.Length > 0)
							this.Hierarchy.SelectGameObject(this.raycastResultIDs[0]);
					}
					else
					{
						this.raycastResultRect.x = this.textureRect.x;
						this.raycastResultRect.y = this.textureRect.y;
						this.raycastResultRect.width = 250F;
						this.raycastResultRect.height = 16F + (24F + 2F) * this.raycastResultNames.Length - (this.raycastResultNames.Length > 0 ? 2F : 0F);
					}
				}

				for (int i = 0; i < this.modules.Count; i++)
					this.modules[i].OnGUICamera(this, this.textureRect);

				this.DrawCameraGUI();

				if (this.raycastState == RaycastState.RequestingRaycast)
				{
					Rect	r = this.textureRect;

					r.width = 250F;
					r.height = 16F;

					EditorGUI.DrawRect(r, NGRemoteCameraWindow.PanelBackgroundColor);
					GUI.Label(r, "Raycast :");

					r.x += 60F;
					r.width = 16F;
					GUI.DrawTexture(r, GeneralStyles.StatusWheel.image, ScaleMode.ScaleToFit);

					this.Repaint();
				}
				else if (this.raycastState == RaycastState.ResultReceived)
				{
					EditorGUI.DrawRect(this.raycastResultRect, NGRemoteCameraWindow.PanelBackgroundColor);

					Rect	r = this.raycastResultRect;

					r.width -= 16F;
					r.height = 16F;

					if (this.raycastResultNames.Length <= 1)
						GUI.Label(r, "Raycast : " + this.raycastResultNames.Length + " hit");
					else
						GUI.Label(r, "Raycast : " + this.raycastResultNames.Length + " hits");

					r.x += r.width;
					r.width = 16F;
					if (GUI.Button(r, "X") == true)
						this.raycastState = RaycastState.None;

					r.y += r.height;
					r.x = this.raycastResultRect.x;
					r.width = this.raycastResultRect.width;

					ClientGameObject[]	selection = this.Hierarchy.GetSelectedGameObjects();
					StringBuilder		buffer = Utility.GetBuffer();

					for (int i = 0; i < this.raycastResultNames.Length; i++)
					{
						r.height = 24F;

						if (GUI.Button(r, GUIContent.none) == true)
							this.Hierarchy.SelectGameObject(this.raycastResultIDs[i]);

						r.height = 16F;
						r.x += 4F;

						bool	selected = false;

						for (int j = 0; j < selection.Length; j++)
						{
							if (selection[j].instanceID == this.raycastResultIDs[i])
							{
								selected = true;
								break;
							}
						}

						using (ColorContentRestorer.Get(selected, Color.green))
						{
							GUI.Label(r, this.raycastResultNames[i], GeneralStyles.ComponentName);
						}

						r.x -= 4F;
						r.y += r.height - 2F;

						using (ColorContentRestorer.Get(Color.grey))
						{
							r.height = 12F;
							NGEditorGUILayout.ElasticLabel(r, this.raycastResultHierarchies[i], '/', GeneralStyles.SmallLabel);
						}

						r.y += 10F + 2F;
					}

					Utility.RestoreBuffer(buffer);
				}
			}
			else
			{
				GUI.Label(this.textureRect, "No Camera selected.", GeneralStyles.BigCenterText);

				EditorGUIUtility.AddCursorRect(this.textureRect, MouseCursor.Link);

				if (Event.current.type == EventType.MouseDown && this.textureRect.Contains(Event.current.mousePosition) == true)
					XGUIHighlightManager.Highlight(NGRemoteCameraWindow.Title + ".CameraSelector");
			}
		}

		private void	DisplayCameras()
		{
			this.tipsHelper.HelpBox("CreateGhostCamera", "Create a ghost Camera deriving from the selected Camera below. Disable unnecessary Component for performance reason (Post process, rendering effect, shaders and similar).", MessageType.Info);
			if (this.tipsHelper.HelpBox("OptimizeGhostCamera", "A ghost Camera will potentially divide the framerate by 2. Reduce the target framerate and the render texture resolution in the options as needed.", MessageType.Warning) == true)
			{
				Rect	r3 = GUILayoutUtility.GetLastRect();

				EditorGUIUtility.AddCursorRect(r3, MouseCursor.Link);
				if (Event.current.type == EventType.MouseDown && r3.Contains(Event.current.mousePosition) == true)
					XGUIHighlightManager.Highlight(NGRemoteCameraWindow.NormalTitle + ".Options");
			}

			this.camerasScrollPosition = EditorGUILayout.BeginScrollView(this.camerasScrollPosition);
			{
				EditorGUILayout.BeginHorizontal();
				{
					EditorGUILayout.BeginVertical();
					{
						EditorGUILayout.BeginHorizontal(/*GeneralStyles.Toolbar*/);
						{
							Utility.content.text = "[New Camera]";
							Utility.content.image = UtilityResources.CameraIcon;
							GUILayout.Label(Utility.content, GeneralStyles.ToolbarButton);

							Rect	r = GUILayoutUtility.GetLastRect();

							if (GUILayout.Button("☰", "GV Gizmo DropDown", GUILayoutOptionPool.ExpandWidthFalse) == true)
								PopupWindow.Show(r, new GhostCameraOptionsPopup(this.ghostCameraOverrides, this, false, 0, 0));
						}
						EditorGUILayout.EndHorizontal();

						EditorGUI.BeginDisabledGroup(true);
						{
							EditorGUILayout.ToggleLeft("Transform", true);
							EditorGUILayout.ToggleLeft("Camera", true);
						}
						EditorGUI.EndDisabledGroup();

						GUILayout.FlexibleSpace();

						if (this.cameraConnected == true && this.cameraSelected == -1)
						{
							if (GUILayout.Button("Create Ghost Camera", GUILayoutOptionPool.Height(32F)) == true)
								this.CreateGhostCamera();

							GUILayout.Space(2F);
						}
					}
					EditorGUILayout.EndVertical();

					if (this.cameraConnected == true && this.cameraComponentsIDs != null)
					{
						if (Event.current.type == EventType.Repaint)
						{
							if (this.cameraSelected == -1)
							{
								Rect	r = GUILayoutUtility.GetLastRect();

								r.x += 1F;
								r.width -= 2F;
								Utility.DrawUnfillRect(r, HighlightSelectedCamera);
							}
						}
						else if (Event.current.type == EventType.MouseDown)
						{
							Rect	r = GUILayoutUtility.GetLastRect();

							if (r.Contains(Event.current.mousePosition) == true)
							{
								this.cameraSelected = -1;
								Event.current.Use();
							}
						}

						for (int i = 0; i < this.cameraComponentsIDs.Length; i++)
						{
							Rect	r = GUILayoutUtility.GetLastRect();

							r.height = this.position.height;
							r.xMin = r.xMax - 1F;
							EditorGUI.DrawRect(r, Color.grey);

							EditorGUILayout.BeginVertical();
							{
								EditorGUILayout.BeginHorizontal();
								{
									Utility.content.text = this.cameraNames[i];
									Utility.content.tooltip = this.cameraNames[i];
									Utility.content.image = UtilityResources.CameraIcon;
									GUILayout.Label(Utility.content, GeneralStyles.ToolbarButton, GUILayoutOptionPool.MaxWidth(200F));

									r = GUILayoutUtility.GetLastRect();

									if (GUILayout.Button("☰", "GV Gizmo DropDown", GUILayoutOptionPool.ExpandWidthFalse) == true)
										PopupWindow.Show(r, new GhostCameraOptionsPopup(this.camerasOverrides[i], this, false, this.cameraGameObjectIDs[i], this.cameraIDs[i]));
								}
								EditorGUILayout.EndHorizontal();

								while (this.camerasOverrides[i].componentsSelected.Count < this.cameraComponentsTypes[i].Length)
									this.camerasOverrides[i].componentsSelected.Add(true);

								for (int j = 0; j < this.cameraComponentsTypes[i].Length; j++)
								{
									Type	type = Type.GetType(this.cameraComponentsTypes[i][j]);

									EditorGUI.BeginDisabledGroup(j == 0 || type.Name == "Camera");
									{
										// Make sure the Transform is never toggled.
										if (j == 0 || type.Name == "Camera")
											this.camerasOverrides[i].componentsSelected[j] = true;

										if (type != null)
											this.camerasOverrides[i].componentsSelected[j] = EditorGUILayout.ToggleLeft(type.Name, this.camerasOverrides[i].componentsSelected[j]);
										else
											this.camerasOverrides[i].componentsSelected[j] = EditorGUILayout.ToggleLeft(this.cameraComponentsTypes[i][j], this.camerasOverrides[i].componentsSelected[j]);
									}
									EditorGUI.EndDisabledGroup();
								}

								GUILayout.FlexibleSpace();

								if (this.cameraSelected == i)
								{
									if (GUILayout.Button("Create ghost camera", GUILayoutOptionPool.Height(32F)) == true)
										this.CreateGhostCamera();

									GUILayout.Space(2F);
								}
							}
							EditorGUILayout.EndVertical();

							if (Event.current.type == EventType.Repaint)
							{
								if (this.cameraSelected == i)
								{
									r = GUILayoutUtility.GetLastRect();

									r.x += 1F;
									r.width -= 2F;
									Utility.DrawUnfillRect(r, HighlightSelectedCamera);
								}
							}
							else if (Event.current.type == EventType.MouseDown)
							{
								r = GUILayoutUtility.GetLastRect();

								if (r.Contains(Event.current.mousePosition) == true)
								{
									this.cameraSelected = i;
									Event.current.Use();
								}
							}
						}
					}

					Rect	r2 = GUILayoutUtility.GetLastRect();

					r2.height = this.position.height;
					r2.xMin = r2.xMax - 1F;
					EditorGUI.DrawRect(r2, Color.grey);

					if (this.Hierarchy.IsClientConnected() == true && this.cameraConnected == false)
					{
						GUILayout.Space(10F);

						if (GUILayout.Button("You need to start\nto load cameras.", GeneralStyles.BigCenterText, GUILayoutOptionPool.ExpandHeightTrue, GUILayoutOptionPool.ExpandWidthTrue) == true)
							XGUIHighlightManager.Highlight(NGRemoteCameraWindow.Title + ".Connect");
						EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
					}

					GUILayout.FlexibleSpace();
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndScrollView();

			Utility.content.image = null;
		}

		private void	DrawCameraGUI()
		{
			if (this.IsGhostCameraFocused == true)
			{
				if (Event.current.type == EventType.KeyDown)
				{
					bool	anyUpdate = true;

					if (Event.current.keyCode == KeyCode.UpArrow)
						this.moveForward = true;
					else if (Event.current.keyCode == KeyCode.DownArrow)
						this.moveBackward = true;
					else if (Event.current.keyCode == KeyCode.LeftArrow)
						this.moveLeft = true;
					else if (Event.current.keyCode == KeyCode.RightArrow)
						this.moveRight = true;
					else if (Event.current.keyCode == KeyCode.RightControl)
					{}
					else
						anyUpdate = false;

					if (anyUpdate == true)
					{
						float	moveSpeed = this.moveSpeed * (Event.current.control == true ? 2F : 1F);

						this.Hierarchy.Client.AddPacket(new ClientSendCameraInputPacket(this.moveForward, this.moveBackward, this.moveLeft, this.moveRight, moveSpeed));
					}
				}
				else if (Event.current.type == EventType.KeyUp)
				{
					bool	anyUpdate = true;

					if (Event.current.keyCode == KeyCode.UpArrow)
						this.moveForward = false;
					else if (Event.current.keyCode == KeyCode.DownArrow)
						this.moveBackward = false;
					else if (Event.current.keyCode == KeyCode.LeftArrow)
						this.moveLeft = false;
					else if (Event.current.keyCode == KeyCode.RightArrow)
						this.moveRight = false;
					else if (Event.current.keyCode == KeyCode.RightControl)
					{}
					else
						anyUpdate = false;

					if (anyUpdate == true)
					{
						float	moveSpeed = this.moveSpeed * (Event.current.control == true ? 2F : 1F);

						this.Hierarchy.Client.AddPacket(new ClientSendCameraInputPacket(this.moveForward, this.moveBackward, this.moveLeft, this.moveRight, moveSpeed));
					}
				}
				else if (Event.current.type == EventType.ScrollWheel && this.textureRect.Contains(Event.current.mousePosition) == true)
					this.Hierarchy.Client.AddPacket(new ClientSendCameraZoomPacket(-Event.current.delta.y * this.zoomSpeed));

				if (this.hasWindowFocus == true && this.dragging == true)
				{
					this.textureRect.x += 1F;
					this.textureRect.y += 1F;
					this.textureRect.width -= 2F;
					this.textureRect.height -= 2F;
					Utility.DrawUnfillRect(this.textureRect, Color.white);

					this.wantsMouseMove = true;

					this.Repaint();
				}

				this.panelRect.y = this.position.height - this.panelRect.height;
				this.panelRect.width = this.position.width - 32F;
				this.togglePanelRect.y = this.position.height - this.togglePanelRect.height;

				if (Event.current.type == EventType.MouseMove && this.dragging == true)
					this.dragging = false;
				else if (Event.current.type == EventType.MouseDrag && this.dragging == true)
				{
					if (Event.current.button != 2 && (Event.current.alt == false || Event.current.control == false))
					{
						Vector2	delta = new Vector2((Event.current.mousePosition.y - this.dragPos.y) * this.yAxisSensitivity,
													(Event.current.mousePosition.x - this.dragPos.x) * this.xAxisSensitivity);
						this.camRotation = this.dragCamRotation + delta;

						if (Application.platform == RuntimePlatform.WindowsEditor &&
							this.keepCursorCenter == true)
						{
							this.dragPos.x = (int)(this.textureRect.x + this.textureRect.width / 2);
							this.dragPos.y = (int)(this.textureRect.y + this.textureRect.height / 2);
							NativeMethods.SetCursorPos((int)(this.position.x + this.textureRect.x + this.textureRect.width / 2),
													   (int)(this.position.y + this.textureRect.y + this.textureRect.height / 2));
							this.dragCamRotation += delta;
						}

						this.Hierarchy.Client.AddPacket(new ClientSendCameraTransformRotationPacket(this.camRotation));
					}
					else
					{
						Vector3	up = Quaternion.Euler(this.camRotation.x, this.camRotation.y, 0F) * Vector3.up;
						// The 2 next lines are equal, but one does not allocate Vector3. Don't know why, but could'nt reproduce the same for up vector, maybe because of PitchRollYaw.
						//Vector3	right = Quaternion.Euler(this.camRotation.x, this.camRotation.y, 0F) * Vector3.right;
						Vector3	right = Quaternion.AngleAxis(this.camRotation.y, Vector3.up) * Vector3.right;
						Vector3	direction = (up * (Event.current.mousePosition.y - this.dragPos.y) * .01F + right * (Event.current.mousePosition.x - this.dragPos.x) * -.01F);
						this.camPosition = this.dragCamPosition + direction;

						if (this.keepCursorCenter == true)
						{
							this.dragPos.x = (int)(this.textureRect.x + this.textureRect.width / 2);
							this.dragPos.y = (int)(this.textureRect.y + this.textureRect.height / 2);
							NativeMethods.SetCursorPos((int)(this.position.x + this.textureRect.x + this.textureRect.width / 2),
													   (int)(this.position.y + this.textureRect.y + this.textureRect.height / 2));
							this.dragCamPosition += direction;
						}

						this.Hierarchy.Client.AddPacket(new ClientSendCameraTransformPositionPacket(this.camPosition));
					}
				}
			}

			if (Event.current.type == EventType.MouseDown &&
				this.textureRect.Contains(Event.current.mousePosition) == true &&
				(this.displayGhostPanel == false || this.panelRect.Contains(Event.current.mousePosition) == false) &&
				(this.raycastState == RaycastState.None || this.raycastResultRect.Contains(Event.current.mousePosition) == false) &&
				this.togglePanelRect.Contains(Event.current.mousePosition) == false)
			{
				if (Event.current.button == 1 || Event.current.alt == true)
				{
					if (this.IsGhostCameraFocused == true)
					{
						this.dragCamPosition = this.camPosition;
						this.dragCamRotation = this.camRotation;
						this.dragging = true;
						this.dragPos = Event.current.mousePosition;
						GUI.FocusControl(null);

						if (Application.platform == RuntimePlatform.WindowsEditor && this.keepCursorCenter == true)
							Cursor.visible = false;
					}
				}
				else if (Event.current.button == 0)
				{
					if (this.lastRaycastClick + Constants.DoubleClickTime > EditorApplication.timeSinceStartup &&
						Vector2.Distance(this.lastRaycastMousePosition, Event.current.mousePosition) < 5F)
					{
						this.hasDoubleRaycast = true;
					}
					else
					{
						float	viewportPositionX = 0F;
						float	viewportPositionY = 0F;

						if (this.textureModule.ScaleMode == ScaleMode.StretchToFill)
						{
							//viewportPositionX = (Event.current.mousePosition.x - this.textureRect.x) / this.textureRect.width;
							// If we suppose textureRect.x is always 0:
							viewportPositionX = Event.current.mousePosition.x / this.textureRect.width;
							viewportPositionY = 1F - (Event.current.mousePosition.y - this.textureRect.y) / this.textureRect.height;
						}
						else
						{
							float	textureRatio = (float)this.textureWidth / (float)this.textureHeight;
							float	guiRatio = this.textureRect.width / this.textureRect.height;

							if (this.textureModule.ScaleMode == ScaleMode.ScaleToFit)
							{
								if (textureRatio > guiRatio)
								{
									//viewportPositionX = (Event.current.mousePosition.x - this.textureRect.x) / this.textureRect.width;
									// If we suppose textureRect.x is always 0:
									viewportPositionX = Event.current.mousePosition.x / this.textureRect.width;

									//float	uncropHeight = ((float)this.textureHeight / (float)this.textureWidth) * this.textureRect.width;
									//float	yMin = (float)(this.textureRect.height - uncropHeight) / 2F;

									//viewportPositionY = 1F - (Event.current.mousePosition.y - yMin - this.textureRect.y) / uncropHeight;

									// Simplified version thanks to Wolfram Alpha. (With multiplication by 2 flatten)
									// https://www.wolframalpha.com/input/?i=1+-+((m+-+((H+-+(h+%2F+w+*+W))+%2F+2)+-+Y)+%2F+(h+%2F+w+*+W))
									viewportPositionY = .5F * (((this.textureWidth * (this.textureRect.height - (Event.current.mousePosition.y + Event.current.mousePosition.y - this.textureRect.y - this.textureRect.y))) / (this.textureHeight * this.textureRect.width)) + 1F);
								}
								else
								{
									//float	uncropWidth = (float)textureRatio * this.textureRect.height;
									//float	xMin = (float)(this.textureRect.width - uncropWidth) / 2F;

									//viewportPositionX = (Event.current.mousePosition.x - xMin) / uncropWidth;
									// Simplified version thanks to Wolfram Alpha. (With multiplication by 2 flatten)
									// https://www.wolframalpha.com/input/?i=(m+-+((W+-+(w+%2F+h+*+H))+%2F+2))+%2F+(w+%2F+h+*+H)
									viewportPositionX = ((this.textureHeight * (Event.current.mousePosition.x + Event.current.mousePosition.x - this.textureRect.width)) + this.textureRect.height * this.textureWidth) / (2F * this.textureRect.height * this.textureWidth);
									viewportPositionY = 1F - (Event.current.mousePosition.y - this.textureRect.y) / this.textureRect.height;
								}
							}
							else if (this.textureModule.ScaleMode == ScaleMode.ScaleAndCrop)
							{
								if (textureRatio < guiRatio)
								{
									//viewportPositionX = (Event.current.mousePosition.x - this.textureRect.x) / this.textureRect.width;
									// If we suppose textureRect.x is always 0:
									viewportPositionX = Event.current.mousePosition.x / this.textureRect.width;

									//float	uncropHeight = (float)this.textureHeight * this.textureRect.width / (float)this.textureWidth;
									//float	yMin = (float)(uncropHeight - this.textureRect.height) / 2F;

									//viewportPositionY = (yMin + ((1F - (Event.current.mousePosition.y - this.textureRect.y) / this.textureRect.height) * this.textureRect.height)) / uncropHeight;

									// Simplified version thanks to Wolfram Alpha. (With multiplication by 2 flatten)
									// https://www.wolframalpha.com/input/?i=(((((h+W)+%2F+w)+-+H)+%2F+2)+%2B+((1+-+((m+-+Y)+%2F+H)))+H)+%2F+((h+W)+%2F+w)
									viewportPositionY = .5F * ((((float)this.textureWidth * (this.textureRect.height - (Event.current.mousePosition.y + Event.current.mousePosition.y - this.textureRect.y - this.textureRect.y))) / ((float)this.textureHeight * this.textureRect.width)) + 1F);
								}
								else
								{
									//float	uncropWidth = (float)this.textureWidth * this.textureRect.height / (float)this.textureHeight;
									//float	xMin = (float)(uncropWidth - this.textureRect.width) / 2F;

									//viewportPositionX = (xMin + Event.current.mousePosition.x - this.textureRect.x) / uncropWidth;

									// Simplified version thanks to Wolfram Alpha.
									// https://www.wolframalpha.com/input/?i=((((((w+H)+%2F+h)+-+W)+%2F+2)+%2B+m+-+X))%2F(w+H+%2F+h)
									//viewportPositionX = ((this.textureHeight * (-this.textureRect.width + 2F * (Event.current.mousePosition.x - this.textureRect.x))) + this.textureRect.height * this.textureWidth) / (2F * this.textureRect.height * this.textureWidth);
									// If we suppose textureRect.x is always 0 and with multiplication by 2 flatten.
									viewportPositionX = ((this.textureHeight * (-this.textureRect.width + Event.current.mousePosition.x + Event.current.mousePosition.x)) + this.textureRect.height * this.textureWidth) / (2F * this.textureRect.height * this.textureWidth);
									viewportPositionY = 1F - (Event.current.mousePosition.y - this.textureRect.y) / this.textureRect.height;
								}
							}
						}

						if (0F < viewportPositionX && viewportPositionX < 1F &&
							0F < viewportPositionY && viewportPositionY < 1F)
						{
							this.Hierarchy.Client.AddPacket(new ClientRaycastScenePacket(this.cameraIDs[this.streamingCameraSelected], viewportPositionX, viewportPositionY), this.OnRaycastResultReceived);
							this.lastRaycastClick = EditorApplication.timeSinceStartup;
							this.lastRaycastMousePosition = Event.current.mousePosition;
							this.hasDoubleRaycast = Event.current.control;
							this.raycastState = RaycastState.RequestingRaycast;
						}
					}
				}
				else
				{
					if (this.IsGhostCameraFocused == true)
					{
						this.dragCamPosition = this.camPosition;
						this.dragging = true;
						this.dragPos = Event.current.mousePosition;
						GUI.FocusControl(null);
					}
				}
			}
			else if (Event.current.type == EventType.MouseUp)
			{
				this.dragging = false;

				if (Application.platform == RuntimePlatform.WindowsEditor && this.keepCursorCenter == true)
					Cursor.visible = true;
			}

			if (this.IsGhostCameraFocused == true)
			{
				if (this.displayOverlay == true)
				{
					this.overlayInputRect.x = 100F;
					this.overlayInputRect.y = this.textureRect.yMax - 160F;

					if (this.displayGhostPanel == true)
						this.overlayInputRect.y -= this.panelRect.height;

					Color	pressedColor = Event.current.control == true ? NGRemoteCameraWindow.SpeedHighlightArrowColor : NGRemoteCameraWindow.HighlightArrowColor;

					if (this.moveForward == true)
						Utility.DrawUnfillRect(this.overlayInputRect, pressedColor);
					else
						Utility.DrawUnfillRect(this.overlayInputRect, NGRemoteCameraWindow.DefaultArrowColor);

					this.overlayInputRect.y += this.overlayInputRect.height + 4F;
					if (this.moveBackward == true)
						Utility.DrawUnfillRect(this.overlayInputRect, pressedColor);
					else
						Utility.DrawUnfillRect(this.overlayInputRect, NGRemoteCameraWindow.DefaultArrowColor);

					this.overlayInputRect.x -= this.overlayInputRect.width + 4F;
					if (this.moveLeft == true)
						Utility.DrawUnfillRect(this.overlayInputRect, pressedColor);
					else
						Utility.DrawUnfillRect(this.overlayInputRect, NGRemoteCameraWindow.DefaultArrowColor);

					this.overlayInputRect.x += this.overlayInputRect.width + 4F + this.overlayInputRect.width + 4F;
					if (this.moveRight == true)
						Utility.DrawUnfillRect(this.overlayInputRect, pressedColor);
					else
						Utility.DrawUnfillRect(this.overlayInputRect, NGRemoteCameraWindow.DefaultArrowColor);
				}

				if (this.displayGhostPanel == true)
				{
					EditorGUI.DrawRect(this.panelRect, NGRemoteCameraWindow.PanelBackgroundColor);

					GUILayout.BeginArea(this.panelRect);
					{
						EditorGUILayout.BeginHorizontal();
						{
							GUILayout.Space(3F);

							EditorGUILayout.BeginVertical(GUILayoutOptionPool.Width(this.panelRect.width * .5F));
							{
								GUILayout.Space(3F);

								using (LabelWidthRestorer.Get(90F))
								{
									EditorGUILayout.BeginHorizontal();
									{
										this.xAxisSensitivity = EditorGUILayout.FloatField(LC.G("NGCamera_MouseXSensitivity"), this.xAxisSensitivity);
										this.yAxisSensitivity = EditorGUILayout.FloatField(LC.G("NGCamera_MouseYSensitivity"), this.yAxisSensitivity);
									}
									EditorGUILayout.EndHorizontal();

									EditorGUILayout.BeginHorizontal();
									{
										this.moveSpeed = EditorGUILayout.FloatField(LC.G("NGCamera_MoveSpeed"), this.moveSpeed);
										this.zoomSpeed = EditorGUILayout.FloatField(LC.G("NGCamera_ZoomSpeed"), this.zoomSpeed);
									}
									EditorGUILayout.EndHorizontal();

									EditorGUILayout.BeginHorizontal();
									{
										this.displayOverlay = EditorGUILayout.Toggle("Display Overlay", this.displayOverlay);

										if (Application.platform == RuntimePlatform.WindowsEditor)
											this.keepCursorCenter = EditorGUILayout.Toggle("Lock Cursor", this.keepCursorCenter);
									}
									EditorGUILayout.EndHorizontal();
								}
							}
							EditorGUILayout.EndVertical();

							EditorGUILayout.BeginVertical();
							{
								GUILayout.Space(3F);

								EditorGUI.BeginChangeCheck();
								this.camPosition = EditorGUILayout.Vector3Field(LC.G("NGCamera_Position"), this.camPosition);
								if (EditorGUI.EndChangeCheck() == true)
									this.Hierarchy.Client.AddPacket(new ClientSendCameraTransformPositionPacket(this.camPosition));

								EditorGUI.BeginChangeCheck();
								this.camRotation = EditorGUILayout.Vector2Field(LC.G("NGCamera_Rotation"), this.camRotation);
								if (EditorGUI.EndChangeCheck() == true)
									this.Hierarchy.Client.AddPacket(new ClientSendCameraTransformRotationPacket(this.camRotation));
							}
							EditorGUILayout.EndVertical();

							GUILayout.Space(3F);
						}
						EditorGUILayout.EndHorizontal();
					}
					GUILayout.EndArea();
				}

				if (GUI.Button(this.togglePanelRect, this.displayGhostPanel == true ? "<" : ">") == true)
					this.displayGhostPanel = !this.displayGhostPanel;
			}
		}

		private void	CreateGhostCamera()
		{
			CameraOverrideSettings	overrides = this.TargetOverrides;

			if (this.cameraSelected == -1)
				this.Hierarchy.Client.AddPacket(new ClientSetDefaultGhostCameraPacket(), this.OnGhostCameraSet);
			else
			{
				this.Hierarchy.Client.AddPacket(new ClientSetGhostCameraPacket(this.cameraIDs[this.cameraSelected],
																			   this.cameraComponentsIDs[this.cameraSelected],
																			   this.camerasOverrides[this.cameraSelected].componentsSelected.ToArray()),
												this.OnGhostCameraSet);
			}

			if (overrides.overrideClearFlags == true)
				this.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraClearFlags, SettingType.Integer, overrides.clearFlags));
			if (overrides.overrideBackground == true)
				this.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraBackground, SettingType.Color, this.TargetOverrides.background));
			if (overrides.overrideCullingMask == true)
				this.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraCullingMask, SettingType.Integer, this.TargetOverrides.cullingMask));
			if (overrides.overrideProjection == true)
				this.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraProjection, SettingType.Integer, this.TargetOverrides.projection));
			if (overrides.overrideFieldOfView == true)
				this.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraFieldOfView, SettingType.Single, this.TargetOverrides.fieldOfView));
			if (overrides.overrideSize == true)
				this.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraSize, SettingType.Single, this.TargetOverrides.size));
			if (overrides.overrideClippingPlanes == true)
			{
				this.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraClippingPlanesNear, SettingType.Single, this.TargetOverrides.clippingPlanesNear));
				this.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraClippingPlanesFar, SettingType.Single, this.TargetOverrides.clippingPlanesFar));
			}
			if (overrides.overrideViewportRect == true)
				this.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraViewportRect, SettingType.Rect, this.TargetOverrides.viewportRect));
			if (overrides.overrideCdepth == true)
				this.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraDepth, SettingType.Single, this.TargetOverrides.cdepth));
			if (overrides.overrideRenderingPath == true)
				this.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraRenderingPath, SettingType.Integer, this.TargetOverrides.renderingPath));
			if (overrides.overrideOcclusionCulling == true)
				this.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraOcclusionCulling, SettingType.Boolean, this.TargetOverrides.occlusionCulling));
			if (overrides.overrideHDR == true)
				this.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraHDR, SettingType.Boolean, this.TargetOverrides.HDR));
			if (overrides.overrideTargetDisplay == true)
				this.Hierarchy.Client.AddPacket(new ClientSetCameraSettingPacket(Setting.CameraTargetDisplay, SettingType.Integer, this.TargetOverrides.targetDisplay));

			this.display = Display.Camera;
		}

		private void	Clean()
		{
			this.cameraIDs = null;
			this.cameraGameObjectIDs = null;
			this.cameraNames = null;
			this.cameraComponentsIDs = null;
			this.cameraComponentsTypes = null;
			this.streamingCameraSelected = -1;

			this.raycastState = RaycastState.None;

			this.cameraConnected = false;
			this.cameraSelected = -1;
		}

		private Packet	CreatePacketStickGhostCamera(string valuePath, byte[] data)
		{
			TypeHandler	handler = TypeHandlersManager.GetTypeHandler<UnityObject>();
			UnityObject	unityObject = handler.Deserialize(Utility.GetBBuffer(data), typeof(Transform)) as UnityObject;

			return new ClientStickGhostCameraPacket(unityObject.instanceID, false);
		}

		private void	OnAnchorUpdated(ResponsePacket p)
		{
			if (p.CheckPacketStatus() == true)
				this.stickyTransformID = (p as ServerStickGhostCameraPacket).transformInstanceID;
		}

		private void	OnNotifyAllCamerasReceived(Client sender, Packet p)
		{
			ServerSendAllCamerasPacket	packet = p as ServerSendAllCamerasPacket;

			this.cameraIDs = packet.IDs;
			this.cameraGameObjectIDs = packet.gameObjectIDs;
			this.cameraNames = packet.names;
			this.cameraComponentsIDs = packet.componentsIDs;
			this.cameraComponentsTypes = packet.componentsTypes;
			this.ghostCameraID = packet.ghostCameraId;

			this.camerasOverrides = new CameraOverrideSettings[this.cameraIDs.Length];

			for (int i = 0; i < this.cameraIDs.Length; i++)
			{
				this.camerasOverrides[i] = new CameraOverrideSettings(this.cameraNames[i]);

				if (this.cameraIDs[i] == this.ghostCameraID)
					this.streamingCameraSelected = i;
			}

			this.Repaint();
		}

		private void	OnAllCamerasReceived(ResponsePacket p)
		{
			if (p.CheckPacketStatus() == true)
			{
				ServerSendAllCamerasPacket	packet = p as ServerSendAllCamerasPacket;

				this.cameraIDs = packet.IDs;
				this.cameraGameObjectIDs = packet.gameObjectIDs;
				this.cameraNames = packet.names;
				this.cameraComponentsIDs = packet.componentsIDs;
				this.cameraComponentsTypes = packet.componentsTypes;
				this.ghostCameraID = packet.ghostCameraId;

				this.camerasOverrides = new CameraOverrideSettings[this.cameraIDs.Length];

				for (int i = 0; i < this.cameraIDs.Length; i++)
				{
					this.camerasOverrides[i] = new CameraOverrideSettings(this.cameraNames[i]);

					if (this.cameraIDs[i] == this.ghostCameraID)
						this.streamingCameraSelected = i;
				}

				this.Repaint();
			}
		}

		private void	OnGhostCameraSet(ResponsePacket p)
		{
			if (p.CheckPacketStatus() == true)
			{
				ServerSetGhostCameraPacket	packet = p as ServerSetGhostCameraPacket;

				this.ghostCameraID = packet.ghostCameraID;

				this.Hierarchy.Client.AddPacket(new ClientRequestAllCamerasPacket(), this.OnAllCamerasReceived);
			}
		}

		private void	OnNotifyCameraTransformReceived(Client sender, Packet _packet)
		{
			NotifyCameraTransformPacket	packet = _packet as NotifyCameraTransformPacket;

			this.camPosition.x = packet.positionX;
			this.camPosition.y = packet.positionY;
			this.camPosition.z = packet.positionZ;
			this.camRotation.x = packet.rotationX;
			this.camRotation.y = packet.rotationY;
		}

		private void	OnRaycastResultReceived(ResponsePacket p)
		{
			this.raycastState = RaycastState.ResultReceived;

			if (p.CheckPacketStatus() == true)
			{
				ServerSendRaycastResultPacket	packet = p as ServerSendRaycastResultPacket;

				this.raycastResultIDs = packet.instanceIDs;
				this.raycastResultNames = packet.names;
				this.raycastResultHierarchies = new string[packet.names.Length];

				StringBuilder	buffer = Utility.GetBuffer();

				for (int i = 0; i < this.raycastResultHierarchies.Length; i++)
				{
					ClientGameObject	go = this.Hierarchy.GetGameObject(this.raycastResultIDs[i]);

					go = go.Parent;
					while (go != null)
					{
						if (buffer.Length > 1)
							buffer.Insert(0, '/');
						buffer.Insert(0, go.name);
						go = go.Parent;
					}

					this.raycastResultHierarchies[i] = buffer.ToString();
					buffer.Length = 0;
				}

				Utility.RestoreBuffer(buffer);
			}
		}

		private void	OnNotifyCameraDataReceived(Client sender, Packet _packet)
		{
			NotifyCameraDataPacket	packet = _packet as NotifyCameraDataPacket;

			if (ScreenshotModule.ModuleID == packet.moduleID)
				++this.receivedTexturesCounter;

			for (int i = 0; i < this.modules.Count; i++)
			{
				if (this.modules[i].moduleID == packet.moduleID &&
					this.modules[i].active == true)
				{
					this.modules[i].HandlePacket(this, packet.time, packet.data);
					this.Repaint();
					break;
				}
			}
		}

		private void	OnGameObjectContextMenu(ClientGameObject gameObject, GenericMenu menu)
		{
			if (this.cameraConnected == true)
				menu.AddItem(new GUIContent("Use as anchor for " + NGRemoteCameraWindow.NormalTitle), false, this.RequestGameObjectAsAnchor, gameObject);
		}

		private void	RequestGameObjectAsAnchor(object data)
		{
			ClientGameObject	gameObject = data as ClientGameObject;

			if (gameObject.components == null)
				gameObject.RequestComponents(this.Hierarchy.Client);

			this.Hierarchy.Client.AddPacket(new ClientStickGhostCameraPacket(gameObject.instanceID, true));
		}

		private bool	CheckMaxCameraRecordDuration(float duration)
		{
			return NGLicensesManager.Check(duration <= NGRemoteCameraWindow.MaxCameraRecordDuration, NGTools.NGRemoteScene.NGAssemblyInfo.Name + " Pro", "Free version does not allow more than " + NGRemoteCameraWindow.MaxCameraRecordDuration + " seconds.\n\n");
		}

		void	IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			if (this.Hierarchy != null)
				this.Hierarchy.AddTabMenus(menu, this);
			Utility.AddNGMenuItems(menu, this, NGRemoteCameraWindow.NormalTitle, Constants.WikiBaseURL + "#markdown-header-134-ng-remote-camera");
			menu.AddItem(new GUIContent(NGRemoteCameraWindow.NormalTitle + "/Reset Tips"), false, () => this.tipsHelper.EraseAll());
		}
	}
}