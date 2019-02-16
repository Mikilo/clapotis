using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	internal sealed class VerbosePreset : Preset
	{
		public override void	SetSettings(NGSettings ngsettings)
		{
			GeneralSettings		general = ngsettings.Get<GeneralSettings>();
			LogSettings			log = ngsettings.Get<LogSettings>();
			StackTraceSettings	stackTrace = ngsettings.Get<StackTraceSettings>();

			//general.openMode = ConsoleSettings.ModeOpen.AssetDatabaseOpenAsset;
			//general.horizontalScrollbar = true;
			general.filterUselessStackFrame = false;

			//log.giveFocusToEditor = true;
			log.forceFocusOnModifier = EventModifiers.Alt;
			log.displayTime = true;
			//log.timeFormat = "HH:mm:ss.fff";

			stackTrace.displayFilepath = StackTraceSettings.PathDisplay.Visible;
			stackTrace.displayRelativeToAssets = false;

			stackTrace.displayReturnValue = true;
			stackTrace.displayReflectedType = StackTraceSettings.DisplayReflectedType.NamespaceAndClass;
			stackTrace.displayArgumentType = true;
			stackTrace.displayArgumentName = true;

			stackTrace.previewLinesBeforeStackFrame = 4;
			stackTrace.previewLinesAfterStackFrame = 4;
			stackTrace.displayTabAsSpaces = 4;
		}
	}
}