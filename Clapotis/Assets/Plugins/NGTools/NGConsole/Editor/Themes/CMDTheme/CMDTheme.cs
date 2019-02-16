using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	[CompleteTheme]
	internal sealed class CMDTheme : Theme
	{
		/// <summary></summary>
		/// <remarks>Test your Color in Unity 4, because rich text is buggy over there.</remarks>
		/// <param name="instance"></param>
		public override void	SetTheme(NGSettings instance)
		{
			GeneralSettings		general = instance.Get<GeneralSettings>();
			LogSettings			log = instance.Get<LogSettings>();
			StackTraceSettings	stackTrace = instance.Get<StackTraceSettings>();
			Color				defaultNormalColor = new Color(180F / 255F, 180F / 255F, 180F / 255F);

			general.consoleBackground = new Color(0F, 0F, 0F, 200F / 255F);

			general.MenuButtonStyle = new GUIStyle("ToolbarButton");
			general.MenuButtonStyle.clipping = TextClipping.Overflow;
			general.MenuButtonStyle.normal.background = AssetDatabase.LoadAssetAtPath(HQ.RootPath + "/NGConsole/Editor/Themes/CMDTheme/MenuButton.png", typeof(Texture2D)) as Texture2D;
			general.MenuButtonStyle.normal.textColor = new Color(51F / 255F, 228F / 255F, 1F, 1F);
			general.MenuButtonStyle.hover.background = null;
			general.MenuButtonStyle.active.background = AssetDatabase.LoadAssetAtPath(HQ.RootPath + "/NGConsole/Editor/Themes/CMDTheme/MenuButtonActive.png", typeof(Texture2D)) as Texture2D;
			general.MenuButtonStyle.active.textColor = new Color(180F / 255F, 180F / 255F, 180F / 255F, 1F);
			general.MenuButtonStyle.focused.background = null;
			general.MenuButtonStyle.onNormal.background = AssetDatabase.LoadAssetAtPath(HQ.RootPath + "/NGConsole/Editor/Themes/CMDTheme/MenuButtonFocused.png", typeof(Texture2D)) as Texture2D;
			general.MenuButtonStyle.onNormal.textColor = general.MenuButtonStyle.active.textColor;
			general.MenuButtonStyle.onHover.background = null;
			general.MenuButtonStyle.onActive.background = general.MenuButtonStyle.active.background;
			general.MenuButtonStyle.onActive.textColor = general.MenuButtonStyle.active.textColor;
			general.MenuButtonStyle.onFocused.background = null;
			general.MenuButtonStyle.padding = new RectOffset(6, 6, 0, 0);
			general.MenuButtonStyle.font = AssetDatabase.LoadAssetAtPath(HQ.RootPath + "/NGConsole/Editor/Themes/CMDTheme/Consolas.ttf", typeof(Font)) as Font;
			general.MenuButtonStyle.fontSize = 13;
			general.MenuButtonStyle.fixedHeight = 16;

			general.ToolbarStyle = new GUIStyle("Toolbar");
			general.ToolbarStyle.normal.background = null;

			log.selectedBackground = new Color(19F / 255F, 30F / 255F, 47F / 255F);
			log.evenBackground = new Color(0F, 0F, 0F);
			log.oddBackground = new Color(8F / 255F, 8F / 255F, 8F / 255F);

			log.Style = new GUIStyle(GUI.skin.label);
			log.Style.font = general.MenuButtonStyle.font;
			log.Style.fontSize = 13;
			log.Style.alignment = TextAnchor.UpperLeft;
			log.Style.wordWrap = false;
			log.Style.richText = true;
			log.Style.clipping = TextClipping.Clip;
			log.Style.normal.textColor = defaultNormalColor;
			log.Style.hover.textColor = defaultNormalColor;
			log.Style.active.textColor = defaultNormalColor;
			log.Style.focused.textColor = defaultNormalColor;
			log.Style.margin.left = 0;
			log.Style.margin.right = 0;

			log.TimeStyle = new GUIStyle(GUI.skin.label);
			log.TimeStyle.alignment = TextAnchor.MiddleLeft;
			log.TimeStyle.normal.textColor = new Color(58F / 255F, 206F / 255F, 255F / 255F);

			log.CollapseLabelStyle = new GUIStyle("CN CountBadge");
			log.CollapseLabelStyle.alignment = TextAnchor.LowerLeft;
			log.CollapseLabelStyle.fontSize = 10;
			log.CollapseLabelStyle.contentOffset = new Vector2(0F, 2F);
			log.CollapseLabelStyle.richText = false;
			log.CollapseLabelStyle.clipping = TextClipping.Overflow;
			log.CollapseLabelStyle.margin = new RectOffset(0, 0, 0, 0);
			log.CollapseLabelStyle.fixedHeight = 16F;

			log.ContentStyle = new GUIStyle(GUI.skin.label);
			log.ContentStyle.wordWrap = true;
			log.ContentStyle.richText = true;

			stackTrace.Style = new GUIStyle(log.Style);
			stackTrace.Style.font = general.MenuButtonStyle.font;
			stackTrace.Style.fontSize = 13;
			stackTrace.Style.hover.background = Utility.CreateDotTexture(.17F, .17F, .17F, 1F);
			stackTrace.Style.active.background = null;
			stackTrace.Style.margin.left = 0;
			stackTrace.Style.margin.right = 0;

			stackTrace.returnValueColor = new Color(92F / 255F, 193F / 255F, 114F / 255F);
			stackTrace.reflectedTypeColor = new Color(141F / 255F, 141F / 255F, 141F / 255F);
			stackTrace.methodNameColor = new Color(171F / 255F, 171F / 255F, 171F / 255F);
			stackTrace.argumentTypeColor = new Color(92F / 255F, 193F / 255, 114F / 255F);
			stackTrace.argumentNameColor = new Color(4F / 255F, 255F / 255F, 224F / 255F);
			stackTrace.filepathColor = new Color(167F / 255F, 172F / 255F, 172F / 255F);
			stackTrace.lineColor = new Color(141F / 255F, 141F / 255F, 255F / 255F);

			stackTrace.previewTextColor = new Color(167F / 255F, 172F / 255F, 172F / 255F);
			stackTrace.previewLineColor = new Color(141F / 255F, 141F / 255F, 255F / 255F);

			stackTrace.previewSourceCodeBackgroundColor = new Color(.11484375F, .11484375F, .11484375F);
			stackTrace.previewSourceCodeMainLineBackgroundColor = new Color(.01484375F, 0.01484375F, .01484375F);
			stackTrace.PreviewSourceCodeStyle = new GUIStyle(log.Style);
			stackTrace.PreviewSourceCodeStyle.margin = new RectOffset();

			if (stackTrace.keywords.Length >= 3)
			{
				if (stackTrace.keywords[0].keywords.Length > 0 &&
					stackTrace.keywords[0].keywords[0] == ";")
				{
					stackTrace.keywords[0].color = new Color(4F / 255F, 255F / 255F, 224F / 255F);
				}
				if (stackTrace.keywords[1].keywords.Length > 0 &&
					stackTrace.keywords[1].keywords[0] == "this")
				{
					stackTrace.keywords[1].color = new Color(92F / 255F, 193F / 255, 114F / 255F);
				}
				if (stackTrace.keywords[2].keywords.Length > 0 &&
					stackTrace.keywords[2].keywords[0] == "var")
				{
					stackTrace.keywords[2].color = new Color(52F / 255F, 193F / 255, 94F / 255F);
				}
			}

			ConsoleUtility.files.Reset();
		}
	}
}