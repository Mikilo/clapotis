namespace NGTools.NGGameConsole
{
	partial class NGAssemblyInfo
	{
		public static readonly string[]	ChangeLog = { 
			"0.2", "2016/6/23", @"Fixed auto-scroll not working due to GUILayout, now replaced by GUI.
Fixed counters not handled when manually adding new log.
Fixed method OnGUI(int) in NGGameConsole causing method ambiguity exception.
Replaced GUILayout by GUI. Fixed auto-scroll.", 
			"0.6.23", "2016/10/6", @"Added a check when a Command contains a forbidden char in its name.
Added headers in activator Circle.
Centralized DataConsole's style into NGGameConsole.
Cleaned inspector of NGGameConsole.
Fixed a text mistake in ScreensData.
Fixed completion being case sensitive.
Implemented activators Circle and FourCorners to display the console.
Implemented copy of DataConsole.
Improved GUI code when displaying command completion.
Improved all DataConsole and added new ones.
Improved display and behaviour of the game console.
Prevented CLI to clamp the console in the screen.
Reduced resize's width space.
Replaced GUI Label by TextArea in all ConsoleData' FullGUI.", 
			"0.6.25", "2016/12/14", @"Cached text in Dataconsole Screens due to huge lag when requesting Screens.resolutions.
Implemented GUI to change settings at runtime in Game Console.
Set true by default to all time fields in DataConsole Time.", 
			"0.8.27", "2017/1/13", @"Changed order of some info in ScreensData.
Removed test GUI settings in NG CLI.
Removed useless space when rendering ShortGUI.", 
			"0.8.31", "2017/2/12", @"Fixed method Reset assigning Editor resources into prefab.", 
			"0.8.32", "2017/2/13", @"Factorized small code.", 
			"0.8.33", "2017/3/3", @"Added more options in some DataConsole.", 
			"0.8.35", "2017/4/16", @"Implemented a buffer size to cached logs before the first connection.", 
			"0.8.36", "2017/4/21", @"Fixed a wrong initial buffer size.", 
			"1.0.82", "2017/10/20", @"Added package's version in SystemInfoData.
Fixed close button not rendered in front.
Fixed displaying big text throwing length limit error.
Fixed module Remote using GUILayout instead of GUI.
Fixed using wrong namespace in local version.
Implemented option appendStackTrace.
Prevented rendering when there is nothing to show in ObjectCountData.
Replaced an invoke by a coroutine.", 
			"1.0.83", "2017/10/20", @"Fixed typo in local version of module Remote.", 
			"1.0.87", "2017/12/20", @"Implemented NGToolsData.
Implemented freeze and edit in DataConsole.
Splitted SystemInfoData into ApplicationData and SystemInfoData.", 
			"1.0.88", "2018/1/27", @"Changed version to 1.3.
Changed URL protocol from HTTP to HTTPS.", 
			"1.1.89", "2018/5/13", @"Added WikiURL into NGAssemblyInfo.
Added option autoDestroyInProduction.
Cached all calls to GUILayoutOption.
Changed Activator now toggling the Object through an UnityEvent instead of reference.
Changed version to 1.4.
Fixed calls to OnGUI when the game console is hidden.
Fixed obfuscation renaming of Unity messages.
Implemented DataConsole for Audio, Lightmap, Quality & Render settings.
Implemented fallback on member throwing exception.
Improved ClassInspector to handle Object and more structs.
Renamed ExportRowsEditorWindow to ExportRowsWindow.
Replaced GUI.enabled by EditorGUI.Begin/EndDisabledGroup.
Replaced StringComparison argument InvariantCultureIgnoreCase with OrdinalIgnoreCase.", 
			"1.1.90", "2018/5/18", @"Changed version to 1.5.
Fixed not catching message when the game console is hidden.
Fixed pro version not properly recognized."
		};
	}
}