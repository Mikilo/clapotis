using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGSpotlight
{
	internal sealed class TextureFormatFilter : AssetFilter
	{
		private class TextureFormatInstance : IFilterInstance
		{
			int	IFilterInstance.FilterLevel { get { return 1; } }
			int	IFilterInstance.FamilyMask { get { return 1 << 7; } }

			private TextureFormatFilter		parent;
			private TextureImporterFormat	format;
			private string					label;
			private float					width;

			public	TextureFormatInstance(TextureFormatFilter parent, TextureImporterFormat format)
			{
				this.parent = parent;
				this.format = format;
				this.label = "Format:" + format;
				Utility.content.text = this.label;
				this.width = GeneralStyles.ToolbarButton.CalcSize(Utility.content).x;
			}

			bool	IFilterInstance.CheckFilterIn(NGSpotlightWindow window, IDrawableElement element)
			{
				DefaultAssetDrawer	hasGameObject = element as DefaultAssetDrawer;

				if (hasGameObject != null)
				{
					TextureImporter	textureImporter = AssetImporter.GetAtPath(hasGameObject.path) as TextureImporter;

					if (textureImporter != null)
					{
						string	platform;

						switch (EditorUserBuildSettings.activeBuildTarget)
						{
							case BuildTarget.StandaloneLinux:
							case BuildTarget.StandaloneLinux64:
							case BuildTarget.StandaloneLinuxUniversal:
							case (BuildTarget)4: // BuildTarget.StandaloneOSXIntel
							case (BuildTarget)27: // BuildTarget.StandaloneOSXIntel64
							case (BuildTarget)2: // BuildTarget.StandaloneOSXUniversal
							case BuildTarget.StandaloneWindows:
							case BuildTarget.StandaloneWindows64:
								platform = "Standalone";
								break;
							case BuildTarget.Android:
								platform = "Android";
								break;
							case BuildTarget.iOS:
								platform = "iPhone";
								break;
							case BuildTarget.N3DS:
								platform = "Nintendo 3DS";
								break;
							case BuildTarget.PS4:
								platform = "PS4";
								break;
							case BuildTarget.PSP2:
								platform = "PSP2";
								break;
							case BuildTarget.Switch: // Documentation of Unity does not state on this one.
								platform = "Switch";
								break;
							case BuildTarget.tvOS:
								platform = "tvOS";
								break;
							case BuildTarget.WebGL:
								platform = "WebGL";
								break;
							case BuildTarget.WSAPlayer:
								platform = "Windows Store Apps";
								break;
							case BuildTarget.XboxOne:
								platform = "XboxOne";
								break;
							default:
								return false;
						}

						int						mts;
						TextureImporterFormat	textureFormat;

						if (textureImporter.GetPlatformTextureSettings(platform, out mts, out textureFormat) == true)
							return (int)textureFormat == (int)this.format;
					}
				}

				return false;
			}

			float	IFilterInstance.GetWidth()
			{
				return this.width;
			}

			void	IFilterInstance.OnGUI(Rect r, NGSpotlightWindow window)
			{
				if (GUI.Button(r, this.label, GeneralStyles.ToolbarButton) == true)
					window.RemoveFilterInstance(this);
			}

			bool	IFilterInstance.CheckFilterRequirements(NGSpotlightWindow window)
			{
				return this.parent.CheckFilterRequirements(window);
			}
		}

		public	TextureFormatFilter() : base(":format=\"\"", "Format of a texture.")
		{
		}

		public override IFilterInstance	Identify(NGSpotlightWindow window, string keywords, string lowerKeywords)
		{
			if (lowerKeywords.Length >= 8 && lowerKeywords[0] == ':' && lowerKeywords[1] == 'f' && lowerKeywords[2] == 'o' && lowerKeywords[3] == 'r' && lowerKeywords[4] == 'm' && lowerKeywords[5] == 'a' && lowerKeywords[6] == 't' && lowerKeywords[7] == '=')
			{
				if (lowerKeywords.Length == 8)
					window.error.Add("A texture format is required.");
				else
				{
					keywords = keywords.Substring(8);

					try
					{
						TextureImporterFormat value = (TextureImporterFormat)Enum.Parse(typeof(TextureImporterFormat), keywords, true);

						return new TextureFormatInstance(this, value);
					}
					catch
					{
						window.error.Add("Texture importer format \"" + keywords + "\" does not exist. (" + string.Join(", ", Enum.GetNames(typeof(TextureImporterFormat))) + ")");
					}
				}
			}

			return null;
		}

		public override bool	CheckFilterRequirements(NGSpotlightWindow window)
		{
			for (int i = 0; i < window.filterInstances.Count; i++)
			{
				if (window.filterInstances[i].FilterLevel == 0 &&
					window.filterInstances[i].FamilyMask == 1 << 7)
				{
					return true;
				}
			}

			return false;
		}
	}
}