namespace NGToolsEditor.NGScenes
{
	partial class NGAssemblyInfo
	{
		public static readonly string[]	ChangeLog = { 
			"0.5.19", "2016/7/12", @"Moved NG Scenes into its own folder.", 
			"0.6.23", "2016/10/6", @"Fixed drag from the lists of recent and all scenes.", 
			"0.6.25", "2016/12/14", @"Fixed dragging from nothing.", 
			"0.8.33", "2017/3/3", @"Fixed text field not responding to Enter/Return/Escape in PopupWindowContent.
Implemented build scenes profiles.
Removed TextField hack, not a bug, just a mistake...", 
			"0.8.36", "2017/4/21", @"Added feedback when there is no profile yet.", 
			"1.0.82", "2017/10/20", @"Added MenuItem in Windows/NG Tools.
Fixed MenuItem's method not public.
Fixed order of MenuItem used when generating the rooted menu.
Fixed save profile feedback not rendering correctly on personal skin.
Reduced free profiles from 3 to 2.", 
			"1.0.87", "2017/12/20", @"Fixed many NG Hub components Scenes not sharing a unique NG Scenes window.", 
			"1.1.89", "2018/5/13", @"Added WikiURL into NGAssemblyInfo.
Added save changes prompt before loading a scene on Single mode.
Cached all calls to GUILayoutOption.
Cached free ad string.
Changed version to 1.3.
Fixed Unity compatibility issue.
Fixed changing scene while playing.
Fixed no recent used scenes having one entry.
Implemented ElasticLabel.
Implemented NG Change Log.
Implemented context menu on windows.
Optimized allocations and GUI repaint.
Replaced GUI.enabled by EditorGUI.Begin/EndDisabledGroup.
Replaced StringComparison argument InvariantCultureIgnoreCase with OrdinalIgnoreCase.", 
			"1.1.90", "2018/5/18", @"Changed version to 1.4.
Fixed pro version not properly recognized."
		};
	}
}