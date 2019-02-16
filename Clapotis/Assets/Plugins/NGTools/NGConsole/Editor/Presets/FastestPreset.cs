namespace NGToolsEditor.NGConsole
{
	internal sealed class FastestPreset : Preset
	{
		public override void	SetSettings(NGSettings instance)
		{
			GeneralSettings		general = instance.Get<GeneralSettings>();
			LogSettings			log = instance.Get<LogSettings>();
			StackTraceSettings	stackTrace = instance.Get<StackTraceSettings>();

			general.openMode = GeneralSettings.ModeOpen.AssetDatabaseOpenAsset;
			//general.horizontalScrollbar = false;
			general.filterUselessStackFrame = false;
			//general.giveFocusToEditor = true;
			//general.forceFocusOnModifier = EventModifiers.Alt;

			log.displayTime = false;
			//log.timeFormat = "HH:mm:ss.fff";

			stackTrace.displayFilepath = StackTraceSettings.PathDisplay.Hidden;
			stackTrace.displayRelativeToAssets = false;

			stackTrace.displayReturnValue = false;
			stackTrace.displayReflectedType = StackTraceSettings.DisplayReflectedType.None;
			stackTrace.displayArgumentType = false;
			stackTrace.displayArgumentName = false;

			//stackTrace.previewLinesBeforeStackFrame = 3;
			//stackTrace.previewLinesAfterStackFrame = 3;
			stackTrace.displayTabAsSpaces = 0;
		}
	}
}