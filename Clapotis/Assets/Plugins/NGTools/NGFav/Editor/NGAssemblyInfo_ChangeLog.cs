namespace NGToolsEditor.NGFav
{
	partial class NGAssemblyInfo
	{
		public static readonly string[]	ChangeLog = { 
			"0.2", "2016/6/23", @"Added auto-refresh.
Added image in the drag area.
Added tooltip on rank to advice the user to press the shortcut.
Changed text color when the favorite's reference is missing.
Fixed click on image not handled correctly.
Fixed image not displaying correctly.
Fixed null exception when selecting a favorite without any active Scene window.
Fixed selecting favorite on click down.
Fixed shortcut tooltip not cleaned.
Fixed thrown exception when the hierarchy is an empty list.
Fixed window displaying nothing when there is no save at all.
Implemented ping on each element of the favorite.
Improved selecting a favorite now always pinging the main object.
Selecting a fav through shortcut will now focus the Game Object in the scene.
improved NG Fav now project-independent.", 
			"0.3.3", "2016/6/29", @"Fixed select favorite not working at all.
Implemented nested favorite inside dynamic favorite.
Moved stored data to NG Settings.
Wrapped MenuItem Fav Clear in NGT_DEBUG.", 
			"0.4.12", "2016/6/29", @"Fixed label displayed in red as default.
Modified NG Fav menu item's priority.
Replaced button's label ""-"" by ""Erase"".", 
			"0.4.8", "2016/6/29", @"Added constructors and method GetSelectionHash to Selection.
Removed unused serialization methods.
Renamed class Selections by Selection.
Replaced try/catch exception output by a red notification.", 
			"0.5.16", "2016/6/29", @"Fixed null exception in menu item NG Fav Clear when there is no NG Fav opened.", 
			"0.6.25", "2016/12/14", @"Fixed favorite resolving not handling disabled Game Object.", 
			"0.8.27", "2017/1/13", @"Added error message on top when one occurs.
Fixed having an empty name when switching favorite.", 
			"0.8.36", "2017/4/21", @"Fixed Unity 2017 deprecated methods.", 
			"1.0.82", "2017/10/20", @"Fixed adding a default favorite after each compilation.
Fixed root GameObjects not being refreshed.
Fixed scene selection being binded to a project prefab.", 
			"1.0.83", "2017/10/20", @"Renamed shortcuts' menu item.", 
			"1.0.88", "2018/1/27", @"Changed version to 1.2.", 
			"1.1.89", "2018/5/13", @"Added WikiURL into NGAssemblyInfo.
Cached free ad string.
Changed version to 1.3.
Fixed colored UI when using personal skin.
Implemented NG Change Log.
Implemented select favorite from icon area.
Optimized allocations and GUI repaint.
Reduced memory allocation in window NG Fav.
Replaced GUI.enabled by EditorGUI.Begin/EndDisabledGroup.", 
			"1.1.90", "2018/5/18", @"Changed version to 1.4.
Fixed pro version not properly recognized."
		};
	}
}