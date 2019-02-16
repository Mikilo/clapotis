namespace NGToolsEditor.NGAssetFinder
{
	partial class NGAssemblyInfo
	{
		public static readonly string[]	ChangeLog = { 
			"1.0.82", "2017/10/20", @"Added cancellable progress bar.
Added more feedback to prevent the feeling of stalling.
Fixed GameObject filters not persisted between scenes and compilation.
Fixed TypeFinder not implemented in IList.
Fixed a rare null exception when deserializing.
Fixed search using GameObject filters.
Forced drawing of the image instead of GUIContent.
Implemented activable GameObject filter.
Improved Type analyzer algorithm.
Improved the way scene filters are saved/restored.
Renamed NG Asset Finder with NG Asset Finder.", 
			"1.0.83", "2017/10/20", @"Fixed exception not cached when cancelling a scan.
Fixed null exception coming from unused code in OnDisable().", 
			"1.0.84", "2017/10/31", @"Changed version to 1.1.
Fixed matches found by a TypeFinder not correctly handled.", 
			"1.0.85", "2017/10/31", @"Fixed missing argument when checking free feature.", 
			"1.0.87", "2017/12/20", @"Fixed scan failure due to TypeFinder not incrementing counters.", 
			"1.0.88", "2018/1/27", @"Changed version to 1.4.", 
			"1.1.89", "2018/5/13", @"Added WikiURL into NGAssemblyInfo.
Added highlights and better result feedbacks.
Cached all calls to GUILayoutOption.
Cached free ad string.
Changed version to 2.0.
Fixed C# & Material finders not nicifying their label.
Fixed Unity compatibility issue.
Fixed looking for GameObject not detected correctly.
Fixed searching in scene not working in nested GameObject.
Implemented NG Change Log.
Implemented a selector when searching for assets relying on files' extension.
Implemented caching.
Optimized CSharpFinder and MaterialFinder.
Replaced ""Search something"" by ""Find all references"".
Revamped search algorithm and filtering system.
Revamped the UI.
Separated references and prefab modifications when parsing a scene.
Set option ""Use Cache"" to true by default.", 
			"1.1.90", "2018/5/18", @"Changed version to 2.1.
Fixed a very rare exception when the target asset is destroyed during initialization.
Fixed aborting search not deleting current working Type.
Fixed pro version not properly recognized.
Fixed scene counters not properly appending the comma.
Fixed x offset when drawing array indexes.
Implemented GameObject sub-assets from a scene Object.
Improved search in scene algorithm."
		};
	}
}