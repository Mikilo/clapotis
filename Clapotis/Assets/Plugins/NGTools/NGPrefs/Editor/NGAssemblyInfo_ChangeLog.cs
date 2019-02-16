namespace NGToolsEditor.NGPrefs
{
	partial class NGAssemblyInfo
	{
		public static readonly string[]	ChangeLog = { 
			"0.2", "2016/6/23", @"Changed background color when an entry is altered.
Converted private fields to public to make them serializable.", 
			"0.4.12", "2016/6/29", @"Fixed PrefsManager throwing exception in mac when the plist file was not found.", 
			"0.4.8", "2016/6/29", @"Implemented Mac editing.", 
			"0.6.21", "2016/8/7", @"Fixed clearing current preferences not clearing correctly.
Implemented long string editing (When higher than 16382).
Improved load algoritm when fetching massive data from registrar.", 
			"0.6.23", "2016/10/6", @"Changed pref background color when altered.
Clamped label width.
Improved initialization by loading only displayed values.", 
			"0.6.24", "2016/10/16", @"Fixed Mac errors.", 
			"0.6.25", "2016/12/14", @"Fixed clearing filtered preferences.
Fixed deleting filtered preferences messing indexes.", 
			"0.8.27", "2017/1/13", @"Fixed losing keyboard focus after resetting a preference.
Moved try/catch out from LoadPreferences.", 
			"1.0.82", "2017/10/20", @"Added a warning message for user under OSX using NG Prefs in a new project.
Changed public class accessor of Plist to internal.
Fixed PlayerPrefs location on Windows for Unity 5.5 and newer.
Fixed null exception when an entry is deleted outside of NG Prefs.
Set the GUI focus to null when resetting a preference.", 
			"1.0.88", "2018/1/27", @"Changed version to 1.1.", 
			"1.1.89", "2018/5/13", @"Added WikiURL into NGAssemblyInfo.
Cached all calls to GUILayoutOption.
Changed version to 1.2.
Fixed filter throwing null exception during first startup.
Fixed help links URL.
Fixed null exception from keywords on the very first initialization.
Implemented NG Change Log.
Improved filtering.
Replaced GUI.enabled by EditorGUI.Begin/EndDisabledGroup."
		};
	}
}