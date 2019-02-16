namespace NGToolsEditor.NGNavSelection
{
	partial class NGAssemblyInfo
	{
		public static readonly string[]	ChangeLog = { 
			"0.2", "2016/6/23", @"Fixed historic not being restored correctly.
Fixed historic not saved when compiling.
Fixed null exception when the data is corrupted and an entry in historic is empty.
Implemented Last Selection window.
Removed debug.
Replaced plain string title by the title constant.", 
			"0.3.3", "2016/6/29", @"Changed default shortcut to invoke Last Selection window.", 
			"0.4.12", "2016/6/29", @"Fixed mac compatibility.
Fixed null exception when changing the selection without having previously selected one in the historic.
Prevented adding selection just selected through NG Nav Selection.", 
			"0.4.8", "2016/6/29", @"Fixed invoking methods from User32 on Mac.
Moved data to NG Settings.
Revamped the whole system, now NG Nav Selection is a usable window.", 
			"0.5.16", "2016/6/29", @"Added save historic on editor exit.
Fixed folders' IDs not correctly fetched when initialized during the first frame.
Fixed historic now project dependent.
Fixed multi Unity versions compatibility.", 
			"0.6.22", "2016/9/21", @"Prevented displaying the window when no preferences are set.
Implemented display of hierarchy.", 
			"0.6.23", "2016/10/6", @"Fixed null exception when deleting a selected asset in versions 5.1 and older.", 
			"0.6.25", "2016/12/14", @"Fixed calling EditorApplication.isPlaying in a static constructor.
Fixed last hash set to 0 when nothing is selected.", 
			"1.0.88", "2018/1/27", @"Changed version to 1.1.", 
			"1.1.89", "2018/5/13", @"Added WikiURL into NGAssemblyInfo.
Cached all calls to GUILayoutOption.
Changed version to 1.2.
Fixed selection of broken assets not stored properly.
Implemented NG Change Log.
Implemented component Nav Picker for NG Hub.
Replaced GUI.enabled by EditorGUI.Begin/EndDisabledGroup."
		};
	}
}