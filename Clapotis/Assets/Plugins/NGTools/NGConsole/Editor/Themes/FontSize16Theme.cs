using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	internal sealed class FontSize16Theme : Theme
	{
		public override void	SetTheme(NGSettings instance)
		{
			LogSettings			log = instance.Get<LogSettings>();
			StackTraceSettings	stackTrace = instance.Get<StackTraceSettings>();

			log.styleOverride.ResetStyle();
			log.styleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.Alignment;
			log.styleOverride.alignment = TextAnchor.UpperLeft;
			log.styleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.FontSize;
			log.styleOverride.fontSize = 16;
			log.height = 24F;

			log.timeStyleOverride.ResetStyle();
			log.timeStyleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.FontSize;
			log.timeStyleOverride.fontSize = 12;

			log.collapseLabelStyleOverride.fixedHeight = 16F;

			stackTrace.height = 20F;

			stackTrace.styleOverride.ResetStyle();
			stackTrace.styleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.FontSize;
			stackTrace.styleOverride.fontSize = 14;

			stackTrace.previewHeight = 24F;

			stackTrace.previewSourceCodeStyleOverride.ResetStyle();
			stackTrace.previewSourceCodeStyleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.Alignment;
			stackTrace.previewSourceCodeStyleOverride.alignment = TextAnchor.MiddleLeft;
			stackTrace.previewSourceCodeStyleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.FontSize;
			stackTrace.previewSourceCodeStyleOverride.fontSize = 16;
		}
	}
}