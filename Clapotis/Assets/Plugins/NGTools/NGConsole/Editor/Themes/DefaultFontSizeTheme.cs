using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	internal sealed class DefaultFontSizeTheme : Theme
	{
		public override void	SetTheme(NGSettings instance)
		{
			LogSettings			log = instance.Get<LogSettings>();
			StackTraceSettings	stackTrace = instance.Get<StackTraceSettings>();

			log.styleOverride.ResetStyle();
			log.styleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.Alignment;
			log.styleOverride.alignment = TextAnchor.UpperLeft;
			log.styleOverride.overrideMask &= ~(int)GUIStyleOverride.Overrides.FontSize;
			log.height = 16F;

			log.timeStyleOverride.ResetStyle();
			log.timeStyleOverride.overrideMask &= ~(int)GUIStyleOverride.Overrides.FontSize;

			log.collapseLabelStyleOverride.fixedHeight = log.height;

			stackTrace.height = log.height;

			stackTrace.styleOverride.ResetStyle();
			stackTrace.styleOverride.overrideMask &= ~(int)GUIStyleOverride.Overrides.FontSize;

			stackTrace.previewHeight = 16F;

			stackTrace.previewSourceCodeStyleOverride.ResetStyle();
			stackTrace.previewSourceCodeStyleOverride.overrideMask |= (int)GUIStyleOverride.Overrides.Alignment;
			stackTrace.previewSourceCodeStyleOverride.alignment = TextAnchor.MiddleLeft;
			stackTrace.previewSourceCodeStyleOverride.overrideMask &= ~(int)GUIStyleOverride.Overrides.FontSize;
		}
	}
}