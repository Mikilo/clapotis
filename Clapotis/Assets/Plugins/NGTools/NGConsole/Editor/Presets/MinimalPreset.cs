namespace NGToolsEditor.NGConsole
{
	internal sealed class MinimalPreset : Preset
	{
		public override void	SetSettings(NGSettings ngsettings)
		{
			GeneralSettings		general = ngsettings.Get<GeneralSettings>();
			LogSettings			log = ngsettings.Get<LogSettings>();
			StackTraceSettings	stackTrace = ngsettings.Get<StackTraceSettings>();

			//general.openMode = GeneralSettings.ModeOpen.AssetDatabaseOpenAsset;
			//general.horizontalScrollbar = false;
			general.filterUselessStackFrame = true;
			//general.giveFocusToEditor = true;
			//general.forceFocusOnModifier = EventModifiers.Alt;

			log.displayTime = false;
			//log.timeFormat = "HH:mm:ss.fff";

			stackTrace.displayFilepath = StackTraceSettings.PathDisplay.Hidden;
			//stackTrace.displayRelativeToAssets = true;

			stackTrace.displayReturnValue = false;
			stackTrace.displayReflectedType = StackTraceSettings.DisplayReflectedType.None;
			stackTrace.displayArgumentType = false;
			stackTrace.displayArgumentName = false;

			//stackTrace.previewLinesBeforeStackFrame = 3;
			//stackTrace.previewLinesAfterStackFrame = 3;
			//stackTrace.displayTabAsSpaces = 4;
		}
	}
}