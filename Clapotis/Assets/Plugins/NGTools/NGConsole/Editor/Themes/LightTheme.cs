using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	[CompleteTheme]
	internal sealed class LightTheme : Theme
	{
		[NGSettingsChanged]
		private static void	OnSettingsGenerated(ScriptableObject settings)
		{
			if (EditorGUIUtility.isProSkin == true)
				return;

			Action	OverrideSettings = () =>
			{
				if (settings is GeneralSettings)
					LightTheme.SetGeneralSettings(settings as GeneralSettings);
				else if (settings is LogSettings)
					LightTheme.SetLogSettings(settings as LogSettings);
				else if (settings is StackTraceSettings)
					LightTheme.SetStackTraceSettings(settings as StackTraceSettings);
			};

			if (Utility.CheckOnGUI() == false)
				GUICallbackWindow.Open(OverrideSettings);
			else
				OverrideSettings();
		}

		private static void	SetGeneralSettings(GeneralSettings general)
		{
			general.consoleBackground.a = 0F;

			general.menuButtonStyleOverride.ResetStyle();
			general.menuButtonStyleOverride.baseStyleName = "ToolbarButton";
			general.menuButtonStyleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.Clipping;
			general.menuButtonStyleOverride.clipping = TextClipping.Overflow;

			general.toolbarStyleOverride.ResetStyle();
			general.toolbarStyleOverride.baseStyleName = "Toolbar";
		}

		private static void	SetLogSettings(LogSettings log)
		{
			Color	defaultNormalColor = new Color(0F / 255F, 0F / 255F, 0F / 255F);

			log.selectedBackground = new Color(62F / 255F, 125F /231F, 231F / 255F);
			log.evenBackground = new Color(216F / 255F, 216F / 255F, 216F / 255F);
			log.oddBackground = new Color(222F / 255F, 222F / 255F, 222F / 255F);

			log.styleOverride.ResetStyle();
			log.styleOverride.baseStyleName = "label";
			log.styleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.Alignment;
			log.styleOverride.alignment = TextAnchor.UpperLeft;
			log.styleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.WordWrap;
			log.styleOverride.wordWrap = false;
			log.styleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.RichText;
			log.styleOverride.richText = true;
			log.styleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.Clipping;
			log.styleOverride.clipping = TextClipping.Clip;
			log.styleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.Normal;
			log.styleOverride.normal.textColor = defaultNormalColor;
			log.styleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.Hover;
			log.styleOverride.hover.textColor = defaultNormalColor;
			log.styleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.Active;
			log.styleOverride.active.textColor = defaultNormalColor;
			log.styleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.Focused;
			log.styleOverride.focused.textColor = defaultNormalColor;
			log.styleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.Margin;
			log.styleOverride.margin = new RectOffset(0, 0, GUI.skin.label.margin.top, GUI.skin.label.margin.bottom);

			log.timeStyleOverride.ResetStyle();
			log.timeStyleOverride.baseStyleName = "label";
			log.timeStyleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.Alignment;
			log.timeStyleOverride.alignment = TextAnchor.MiddleLeft;
			log.timeStyleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.Normal;
			log.timeStyleOverride.normal.textColor = new Color(111F / 255F, 85F / 255F, 0F / 255F);

			log.collapseLabelStyleOverride.ResetStyle();
			log.collapseLabelStyleOverride.baseStyleName = "CN CountBadge";
			log.collapseLabelStyleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.Alignment;
			log.collapseLabelStyleOverride.alignment = TextAnchor.LowerLeft;
			log.collapseLabelStyleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.FontSize;
			log.collapseLabelStyleOverride.fontSize = 10;
			log.collapseLabelStyleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.ContentOffset;
			log.collapseLabelStyleOverride.contentOffset = new Vector2(0F, 2F);
			log.collapseLabelStyleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.RichText;
			log.collapseLabelStyleOverride.richText = false;
			log.collapseLabelStyleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.Clipping;
			log.collapseLabelStyleOverride.clipping = TextClipping.Overflow;
			log.collapseLabelStyleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.Margin;
			log.collapseLabelStyleOverride.margin = new RectOffset();
			log.collapseLabelStyleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.FixedHeight;
			log.collapseLabelStyleOverride.fixedHeight = 16F;

			log.contentStyleOverride.ResetStyle();
			log.contentStyleOverride.baseStyleName = "label";
			log.contentStyleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.WordWrap;
			log.contentStyleOverride.wordWrap = true;
			log.contentStyleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.RichText;
			log.contentStyleOverride.richText = true;
		}

		private static void	SetStackTraceSettings(StackTraceSettings stackTrace)
		{
			stackTrace.styleOverride.ResetStyle();
			stackTrace.styleOverride.baseStyleName = "label";
			stackTrace.styleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.WordWrap;
			stackTrace.styleOverride.wordWrap = false;
			stackTrace.styleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.RichText;
			stackTrace.styleOverride.richText = true;
			stackTrace.styleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.Margin;
			stackTrace.styleOverride.margin = new RectOffset(0, 0, GUI.skin.label.margin.top, GUI.skin.label.margin.bottom);

			stackTrace.returnValueColor = new Color(78F / 255F, 50F / 255F, 255F / 255F);
			stackTrace.reflectedTypeColor = new Color(103F / 255F, 103F / 255F, 103F / 255F);
			stackTrace.methodNameColor = new Color(49F / 255F, 68F / 255F, 84F / 255F);
			stackTrace.argumentTypeColor = new Color(84F / 255F, 40F / 255, 250F / 255F);
			stackTrace.argumentNameColor = new Color(170F / 255F, 115F / 255F, 114F / 255F);
			stackTrace.filepathColor = new Color(69F / 255F, 69F / 255F, 69F / 255F);
			stackTrace.lineColor = new Color(44F / 255F, 6F / 255F, 199F / 255F);

			stackTrace.previewTextColor = stackTrace.filepathColor;
			stackTrace.previewLineColor = stackTrace.lineColor;

			stackTrace.previewSourceCodeBackgroundColor = new Color(174F / 255F, 177F / 255F, 177F / 255F);
			stackTrace.previewSourceCodeMainLineBackgroundColor = new Color(161F / 255F, 161F / 255F, 161F / 255F);

			stackTrace.previewSourceCodeStyleOverride.ResetStyle();
			stackTrace.previewSourceCodeStyleOverride.baseStyleName = "label";
			stackTrace.previewSourceCodeStyleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.RichText;
			stackTrace.previewSourceCodeStyleOverride.richText = true;
			stackTrace.previewSourceCodeStyleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.Margin;
			stackTrace.previewSourceCodeStyleOverride.margin = new RectOffset(0, 0, GUI.skin.label.margin.top, GUI.skin.label.margin.bottom);

			stackTrace.styleOverride.CopyTo(stackTrace.previewSourceCodeStyleOverride);
			stackTrace.previewSourceCodeStyleOverride.margin = new RectOffset();

			if (stackTrace.keywords.Length >= 3)
			{
				if (stackTrace.keywords[0].keywords.Length > 0 &&
					stackTrace.keywords[0].keywords[0] == ";")
				{
					stackTrace.keywords[0].color = new Color(96F / 255F, 69F / 255F, 0F / 255F);
				}
				if (stackTrace.keywords[1].keywords.Length > 0 &&
					stackTrace.keywords[1].keywords[0] == "this")
				{
					stackTrace.keywords[1].color = new Color(141F / 255F, 47F / 255F, 212F / 255F);
				}
				if (stackTrace.keywords[2].keywords.Length > 0 &&
					stackTrace.keywords[2].keywords[0] == "var")
				{
					stackTrace.keywords[2].color = new Color(186F / 255F, 99F / 255F, 218F / 255F);
				}
			}
		}

		/// <summary></summary>
		/// <remarks>Test your Color in Unity 4, because rich text is buggy over there.</remarks>
		/// <param name="instance"></param>
		public override void	SetTheme(NGSettings instance)
		{
			LightTheme.SetGeneralSettings(instance.Get<GeneralSettings>());
			LightTheme.SetLogSettings(instance.Get<LogSettings>());
			LightTheme.SetStackTraceSettings(instance.Get<StackTraceSettings>());
			ConsoleUtility.files.Reset();
		}
	}
}