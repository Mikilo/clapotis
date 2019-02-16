namespace NGToolsEditor.NGMissingScriptRecovery
{
	partial class NGAssemblyInfo
	{
		public static readonly string[]	ChangeLog = { 
			"1.0.82", "2017/10/20", @"Implemented AssemblyInfo for NG Missing Script Recovery.
Implemented a default editor to offer to fix broken Component.
Moved NG Missing Script Recovery to its own folder.
Subtly improved matching Type algorithm.", 
			"1.0.83", "2017/10/20", @"Added console logs over file logs during automatic recovery.", 
			"1.0.87", "2017/12/20", @"Fixed hidden but serialized fields not taking into account.
Fixed null exception when scanning in Unity 2017.3.0b11.", 
			"1.1.89", "2018/5/13", @"Added WikiURL into NGAssemblyInfo.
Cached all calls to GUILayoutOption.
Changed version to 1.2.
Fixed Editor MissingGUI throwing null exception on destroyed GameObject.
Fixed colored UI when using personal skin.
Fixed obfuscation renaming of Unity messages.
Fixed recovering from a RectTransform not identified properly.
Implemented HighlightMatchedPopup when hovering a potential Type.
Implemented NG Change Log.
Improved reading file performance.
Reduced window's title length.
Replaced GUI.enabled by EditorGUI.Begin/EndDisabledGroup."
		};
	}
}