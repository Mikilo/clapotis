namespace NGTools.NGConsole
{
	partial class NGAssemblyInfo
	{
		public static readonly string[]	ChangeLog = { 
			"0.2", "2016/6/23", @"Added Reset button to reset at any time any sample.
Added a missing translation.
Added a more accurate label in TimeStart/EndMode.
Added a specific color for type.
Added attribute Conditional to all loggers.
Added auto-update in ModuleEditorWindow.
Added button to print Snapshot in module Debug.
Added cache in LogEntry.
Added feedback when sample's conditions are filled.
Added logger LogHierarchy to output the whole hierarchy from an object.
Added logger handling array of RaycastHit.
Added multi-Object exporter/importer.
Added setting smoothScrolling in ConsoleSettings.
Added smooth effect on new log.
Added spacing between contexts in MultiContextsRow.
Added translation ""StackTrace_PingFolderOnModifier"".
Added variables to Constants.
Applied ExportableAttribute on property.
Centered text when there is no sample in RecorderModule.
Changed LightTheme.
Changed LogFile's argument type to object.
Changed NGDebug.Log() behaviour. Adds null reference.
Changed attribute HideFromExport, which now really hides fields and properties in the wizard.
Changed default value of caseSensitive to IgnoreCase.
Changed field's value displaying from Label to TextField in DataRow.
Changed label RequirePlayMode only displaying when there is at least one sample in RecorderModule.
Changed some folders' path.
Changed variables' name in loggers.
Cleaned, refactored, optimized and improved many Row, StreamLog and Module classes.
First implementation of PerWindowVars to handle multi windows.
Fixed CompileRow drawing background color of each stack frame sharing the same position.
Fixed ConsoleSetting not saved when applying Theme or Preset.
Fixed ConsoleSettingsEditor failing when the instance is destroyed.
Fixed ContentFilter mis-displaying the case sensitive option.
Fixed MaxLogEndMode recording one more log. Might be confusing for users.
Fixed Module's ID not zero-indexed anymore.
Fixed ModuleEditorWindow not able to fetch the right module Unity starts.
Fixed ModuleEditorWindow not updating correctly when it should be.
Fixed MultiContextsRow not displaying well when there is no element at all.
Fixed NG Console not aborting initialization when there is no ConsoleSettings available.
Fixed NG Console not enabling correctly when transiting from null to a good instance of ConsoleSettings.
Fixed NG Console not setting correctly its static field settings.
Fixed NGConsole updating while not even initialized after changing the settings file.
Fixed NGDebug.Log sending a bad buffer when the given array is empty.
Fixed OnEnable being called while Preferences has not load the settings yet.
Fixed RecorderModule using EditorStyles. Sometime it throws exception.
Fixed RemoteModule using its own commands instead of those from Constants.
Fixed RowsDrawer not resetting scroll position when clearing logs.
Fixed RowsDrawer not saving the scroll position correctly.
Fixed RowsDrawer only using NGConsole as EditorWindow.
Fixed SampleStream reconnecting old logs after a fresh Unity start.
Fixed and improved SampleStream's behaviour.
Fixed boolean ""initialized"" being restored, causing Unity Editor to throw exception when reimporting the package.
Fixed buffer leak in DefaultRow.
Fixed command HandleKeyboardCommand sent to the first selected log only.
Fixed freeze when no settings is available.
Fixed generic type not correctly parsed by LogConditionParser when generic type is also generic.
Fixed horizontal scrollbar not taking into account an initial offset.
Fixed input command not handling Ctrl + Tab on KeyDown.
Fixed logger Snapshot's caching.
Fixed module window not displaying the title.
Fixed null exception from ModuleEditorWindow when they are enabled before NGConsole.
Fixed opening file issue since 5.3.
Fixed out of range exception when toggling log type with a selection containing indexes higher than the next rows count.
Fixed preview displaying out of current drawing window.
Fixed row content's height not updated well when value is between Constants.MinRowContentHeight and Constants.CriticalMinimumContentHeight.
Fixed shortcut for NG Settings not working in Unity 4.5.
Fixed switching current stream when deleting a stream.
Fixed thrown exception when there is no NGConsole in the editor and the clear utility is called.
Fixed.copy not copying good values in the good order in filtered stream.
Greatly improve performance in RowsDrawer. NG Console can theoretically easily handle more than 1 000 000 logs!
Greatly improved loggers DataRow, MultiContextsRow, MultiTagsRow, DefaultRow.
Handled static method in stack trace.
Implemented PerWindowVars in RowsDrawer, ArchiveModule, RecorderModule and RemoteModule.
Implemented Property in the export/import settings.
Implemented attribute ExcludeFromExport.
Implemented keyboard and mouse inputs in MultiContextsRow.
Improved ContentFilter working with a space at start.
Improved DebugModule.
Improved DebugModule.
Improved NGDebug's loggers.
Improved StreamLog and MainStream header displaying counters with the right width.
Improved and cleaned NGDebug.
Improved behaviour in MultiContextRow.
Improved in RowsDrawer when dealing with massive logs.
Improved stack trace parser now handling generic classes.
Moved class NGDebug from Common to NGConsole.
Optimized method FitFocusedLogInScreen from RowsDrawer.
Refactored variable.
Removed Conditional to all normal loggers in NGDebug.
Removed attribute Exportable from all fields in LogEntry.
Removed debug.
Removed debug.
Removed last line when copying logs.
Renamed Constants.ConsoleTitle to Constants.PackageTitle.
Renamed ExportRowsScriptableWizard into ExportRowsEditorWindow.
Renamed variable editor into console.
Replaced GUIStyle from NGSettings by a local GUIStyle.
Replaced GetFieldsHierarchyOrdered by EachFieldsHierarchyOrdered.
Replaced new line \n by Environment.NewLine.
Replaced the aggresive notification RequirePlayMode into a little label.
Simplified TimeStart/EndMode.", 
			"0.3.3", "2016/6/29", @"Added attribute NonSerialized on many fields.
Added coloring background per log in module ColorMarkers.
Added method InternalLogWarning in NGDebug.
Changed GUI on the right of the menu, now is fixed.
Changed default value of openMode in NGSettings.Console.
Disabled module Recorder due to critical crash.
Fixed AssertFile in NGDebug being too verbose..
Fixed GroupFilter not refreshing after deleting a filter.
Fixed NG Settings not updated in time.
Fixed RowsDrawer decrementing selections in RemoveSelection.
Fixed compile warning.
Fixed module Archive not uninitializing correctly.
Fixed module Main not correctly switching backward.
Implemented altering buttons Clear, Clear on Play, Error Pause.
Integrated method VerboseAsset in NGDebug.
Moved ColorMarkersModule's markers into NGSettings.
Moved static fields initialization into static constructor.
Prevented DefaultRow and DataRow from clearing the selection when opening the context menu on a selected one.
Removed unused asset RemoteSettings.
Replaced Unity Console by NG Console when opening Unity Console while no NG Console is present.
Replaced serialization code.
Replaced symbol NGC_DEBUG by NGT_DEBUG.
Updated support thread URL.
Wrapping most code with try/catch.", 
			"0.4.12", "2016/6/29", @"Added default Color Background markers.
Added many C# types in default keywords.
Added method Reset in FastFileCache.
Fixed warning compile.
Implemented Sort by error in stream Compiler.
Implemented drawer for ColorBackground.
Improved Light and Dark themes.
Marked static variables readonly.", 
			"0.4.13", "2016/6/29", @"Added more changes in Dark and Light themes. No more margins.
Implemented preview in settings General/Log and General/Stack Trace.
Moved class FrameBuilder outside LogConditionParser.
Replaced raw float constant by a constant variable.", 
			"0.4.8", "2016/6/29", @"Added native console's context menu items to NG Console.
Fixed LogConditionParser not handling truncated stack frame.
Fixed ModuleWindow throwing an exception because it is focusing a destroyed stream.
Fixed fast clicking between logs opening executing an action.
Implemented shift selection.
Improved FitFocusedLogInScreen by targetting an index instead of just trying to fit the last log or the first.
Moved serialization from ColorMarker to GroupFilter, it makes more sense.
Reduced drag distance from 60 to 40.
Removed unused variable stepPageUpDown since FitFocusedLogInScreen handles it correctly now.
Renamed variable LastClick by LastClickTime.", 
			"0.5.16", "2016/6/29", @"Changed fallback localization now shorter.
Factorized all foldout styles into one.
Fixed LogConditionParser working on null Frame.
Fixed MainStream receiving compile logs.
Fixed ModuleWindow not repaint when new logs arrived.
Fixed NGSettings.stackTrace.filters being null.
Fixed auto-scroll not working at all when scrolling is smooth.
Fixed fold styles being generated by Unity instead of NG Console.
Fixed foldout styles' textures being unload when unplaying.
Fixed foldoutStyle not working because the property fixedWidth was not assigned.
Fixed method Clear only called on the first instance of NGConsoleWindow.
Implemented setting previewHeight.
Improved all themes.
Improved skip filters context menu.
Moved initlialization of SectionDrawer from procedural code to static constructor.
Removed generated textures from themes.
Added GUI to Packet sent by Client.", 
			"0.5.19", "2016/7/12", @"Removed shortcuts to Preferences and NG Settings from the header bar.", 
			"0.6.21", "2016/8/7", @"Fixed ConsoleSettingsEditor not invalidating NG Settings on change.
Fixed null exception when clearing logs through NG Console's shortcut with no NG Console instance.
Removed unused using and correct assertion message.", 
			"0.6.22", "2016/9/21", @"Moved InternalNGDebug to its own file.
Implemented left/right input and copy commands in CompileRow.
Added internal loggers.
Fixed CompileRow throwing null exception in Archive module after recompiling.
Fixed InternalNGDebug not being skipped through the stack frame filter.
Fixed types deriving from ILogFilter not calling their event ToggleEnabled.
Improved exception loggers.
Set default reflectedTypeColor in Light and Dark themes.", 
			"0.6.23", "2016/10/6", @"Fixed Unity 5.4 compatibility.
Fixed archived log not draggable.
Fixed console catching ExitGUIException.
Fixed leak when enabling/disabling module Remote.
Fixed module Remote duplicating rows after compilation
Fixed rows being fetch from console in RowsDrawer instead of the defined fetcher.
Fixed streams in module Remote being cleared after compilation.
Forced repaint of NG Console on command nodes received.
Forced repaint when a command's answer is received.
Forced update console when altering color markers.
Implemented drag of log's context in DefaultRow and MultiContextRow.
Implemented output of properties in NGDebug.Snapshot.
Improved copy of CompileRow.
Improved module Remote's GUI.", 
			"0.6.24", "2016/10/16", @"Merged all fields of NGDebug.Snapshot into a single TextArea instead of many TextField.", 
			"0.6.25", "2016/12/14", @"Added a button to set the file path in the export window.
Added suffix in method Snapshot.
Fixed copying a log from the keyboard using the same timer as the mouse.
Fixed export button being disabled when the export path is empty.
Fixed preview not hidden when toggling off a row.
Fixed scrolling offsets not restored after serialization pass.
Fixed settings of console's modules not being prioritized.
Fixed some GUI in filters.
Forced class InternalNGDebug to be skipped in UselessFrameFilter.
Forced filter to be enabled by default at the creation.
Implemented go to file in DataRow.
Improved Snapshot and added method Snapshots in NGDebug.
Improved and fixed how Snapshots outputs the data.
Optimized RemoteRow by caching redundant variables.
Renamed OnFocus and OnBlur by OnEnable and OnDestroy in ILogExporter.", 
			"0.8.27", "2017/1/13", @"Added few debugs in module Debug.
Changed pacman texture's meta.
Embedded texture pacman into module Main.
Fixed LogConditionParser not handling nested class.
Fixed LogConditionParser to parse more cases.
Fixed assertion not correctly handled.
Fixed field not serialized by SaveToSerializedFileAndForget.
Implemented Undo in ColorMarkersWizard's.
Implemented save settings after altering any stream or filter.
Revamped ColorMarkersWizard's GUI.", 
			"0.8.30", "2017/2/12", @"Added scrollbar to the export logs wizard for clarity.", 
			"0.8.32", "2017/2/13", @"Fixed archived logs not persisted.", 
			"0.8.33", "2017/3/3", @"Fixed NGDebug.Snapshot not correctly handling string.
Fixed freeze when altering background color of a Color Marker with a lot of logs.", 
			"0.8.34", "2017/3/25", @"Added boolean to clear the space when connecting module Remote.
Fixed compiler stream displaying filtered out files.", 
			"0.8.35", "2017/4/16", @"Added a button to clear stream's logs.", 
			"0.9.37", "2017/5/1", @"Added a button to clear LogConditionParser's cache in module Debug.
Added theme CMD.", 
			"1.0.82", "2017/10/20", @"Added a Sort after an Add in the selection.
Added a new test button in module Debug.
Added a verification after OnEnable to verify if styles are abnormally null.
Added tooltip on log types.
Adjusted scrollbar shape in RowsDrawer.
Adjusted small GUI artefact in module Archive.
Adjusted width of buttons in filter MaskType when option differentiateException is changed.
Changed default working stream to 1 in module Main.
Changed meta data of MenuButton textures.
Disabled menu item of already present filters in skip stack frame's context menu.
Fixed ColorMarker not correctly serialized.
Fixed GUI of filter MaskType.
Fixed ModuleWindow not having its title updated when needed.
Fixed RowsDrawer sending wrong arguments to command HandleKeyboard.
Fixed Snapshot drawing under the scrollbar.
Fixed Snapshot throwing exception of some particular properties.
Fixed altering height of row content not being saved.
Fixed compatibility for Unity 2017.1.0b6.
Fixed compiler stream not correctly displaying log type filters.
Fixed deleting logs in module Archive when drag & dropping.
Fixed deleting logs not correctly updating the scrollbar.
Fixed drag in DefaultRow not clean up after a MouseUp.
Fixed error GUI not displaying correctly.
Fixed exporting logs having the path not interactable.
Fixed field stacktrace being null in some particular logs.
Fixed filter Content not updating stream when toggling case sensitivity.
Fixed focused stream not saved when changed.
Fixed log type markers not added when a color marker was added.
Fixed log's content not correctly fitting the area when the scrollbar is displayed.
Fixed recovering GUIStyle calling GUI API not in a GUI context.
Fixed selecting a log not updating the content.
Fixed shortcut to console's Clear not working if no NG Console opened.
Fixed stream not having its name updated whenever the name or the category state is altered.
Fixed weird out of range exception in ContentFilter. A char cast was being an int cast causing the out of range.
Implemented Boyer-Moore search algorithm in ContentFilter.
Implemented CompleteThemeAttribute to differentiate partial and complete themes.
Implemented DynamicFunc to pass argument between invoked Func.
Implemented a scrollbar in the wizard Color Markers.
Implemented an option to draw log types in header bar or near the stream.
Implemented colored dots in scrollbar. They appear from warnings, errors, exceptions and color markers.
Implemented dots in scrollbar for temporary background colored logs.
Implemented method StaticSnapshot in NGDebug.
Implemented ping when selecting a compiler's log.
Improved ContentFilter performance.
Improved color feedback of log types.
Lowered alpha component to 0.39 in default background colors of module Color Markers.
Moved button Collapse into the header.
Moved free license's ads into a variable, to avoid allocation.
Moved stream options into the header.
Optimized UnityLogEntries.
Overloaded NGDebug.Snapshot with a prefix argument.
Prevented SyncLogs to save the modules rightafter the initialization.
Prevented a buffer leak when jumping to a folder through a stack frame's path.
Prevented potentially huge fields to be serialized.
Prevented the console to save modules when getting disabled.
Refactored code in module Archive.
Removed background color of options warning and error. Was not easy to read them when colored.
Renamed Utility by ConsoleUtility.
Replaced GUILayout by GUI.
Replaced Unity assembly compatibility with a more reliable fallback.
Replaced all Editor/GUILayout with Editor/GUI.
Replaced colored foldout by a small colored rectangle.
Replaced foldout style with the native foldout.
Reset GUI focus when switching between filters.
Revamped UnityLogEntries and UnityLogEntry.
Set default option Smooth Scrolling to true.", 
			"1.0.83", "2017/10/20", @"Added a tooltip showing the original label when it is changed for buttons Clear, Collapse, Clear on Play, Error Pause.
Overloaded NGDebug.StaticSnapshot() with a non-generic method.", 
			"1.0.87", "2017/12/20", @"Implemented random color for a new Color Marker.
Removed code conflicting with Unity 2017.3.0bX and later.", 
			"1.0.88", "2018/1/27", @"Changed version to 1.3.
Fixed GoToLine trying to open an asset when it should not.
Fixed Pac-man image being mipmapped.
Fixed a rare scenario when a log gets truncated and has no stack frame at all.
Fixed filters and rows not correctly positionned in some cases.
Fixed out of range exception when jumping to an non existing line with a rare compile error.
Fixed selection being cleaned when a filter is altered.
Prevented wizard Color Markers to shrink too small.", 
			"1.1.89", "2018/5/13", @"Added WikiURL into NGAssemblyInfo.
Added few tests in module Debug.
Cached all calls to GUILayoutOption.
Cached free ad string.
Changed version to 1.4.
Fixed a minor condition operator in RowsDrawer.
Fixed colored UI when using personal skin.
Fixed drawing the log's content not properly handling the scrollbar width.
Fixed particular valid stack frame being discarded.
Fixed selecting all logs not repainting the window.
Fixed tooltip on Unity buttons not showing the good tooltip.
Implemented JSONRow.
Implemented NG Change Log.
Implemented an advanced copy popup.
Implemented default export sources when exporting logs.
Implemented export of selection in the context menu of DefaultRow.
Implemented one more condition when parsing truncated message.
Improved selection of logs in rows Default, Data and JSON.
Moved filtering methods.
Optimized repaint.
Removed Editor limitation in logger StaticSnapshot, Snapshot, LogTags, MTLog and LogSON.
Removed memory allocation from Filter's name when drawing GroupFilters.
Renamed ExportRowsEditorWindow to ExportRowsWindow.
Replaced GUI.enabled by EditorGUI.Begin/EndDisabledGroup.
Restored copy shortcut to just copy the line/log.
Revamped export logs system.
Sealed attribute classes.
Set a minimum size for the export logs window.
Set advance copy on Shift+C.", 
			"1.1.90", "2018/5/18", @"Changed version to 1.5.
Fixed pro version not properly recognized."
		};
	}
}