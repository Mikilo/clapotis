namespace NGToolsEditor.NGSyncFolders
{
	partial class NGAssemblyInfo
	{
		public static readonly string[]	ChangeLog = { 
			"0.8.27", "2017/1/13", @"Introduction of tool NG Sync Folders.
Implemented Undo.
Implemented error feedback when a file failed its action.
Implemented multi profiles to change folders in few clicks.
Limited number of profiles in free version.", 
			"0.8.28", "2017/1/13", @"Added buttons to open and clear the cache.", 
			"0.8.29", "2017/2/3", @"Fixed invalidation not applied after sync'ing.
Fixed null exception when sync'ing a disabled slave.", 
			"0.8.30", "2017/2/12", @"Added feedback when scanning.
Moved settings class to its own file.", 
			"0.8.33", "2017/3/3", @"Changed progress bar displaying for disabled or non-scanned slaves.
Fixed progress bar displaying for disabled slaves.
Fixed synchronizing all leaving folds open and empty.
Improved exception message.
Moved class Profile into its own file.", 
			"1.0.82", "2017/10/20", @"Added a cancellable progress bar when scanning and synchronizing.
Changed GUI to always display the path.
Disabled Watch feature.
Fixed IO exception when both paths are empty.
Implemented a scrollbar for slaves.
Improved path feedback when altering it.
Improved path feedback when it is invalid.
Removed beta state.", 
			"1.0.88", "2018/1/27", @"Changed version to 1.2.", 
			"1.1.89", "2018/5/13", @"Added WikiURL into NGAssemblyInfo.
Cached all calls to GUILayoutOption.
Cached free ad string.
Changed version to 1.3.
Fixed cached strings not properly updated.
Fixed colored UI when using personal skin.
Fixed scanning an empty path throwing exception.
Greatly improved UI.
Implemented ElasticLabel.
Implemented NG Change Log.
Improved reading file performance.
Revamped filtering system.", 
			"1.1.90", "2018/5/18", @"Changed version to 1.4.
Fixed network path not working.
Fixed pro version not properly recognized."
		};
	}
}