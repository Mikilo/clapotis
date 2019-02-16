namespace NGToolsEditor.NGInspectorGadget
{
	partial class NGAssemblyInfo
	{
		public static readonly string[]	ChangeLog = { 
			"0.2", "2016/6/23", @"Added CloneComponent in Component's context menu in Inspector.
Handled multi-components in ReorderComponentsWizard.
Replaced bottom delete button by per Component button in ReorderComponents.", 
			"0.8.27", "2017/1/13", @"Changed dialogs' title in NG Missing Script Recovery.
Complete revamped of Missing Script Recover. Now named NG Missing Script Recovery and part of NG Inspector Gadget.
Fixed compatibility issues.", 
			"0.8.29", "2017/2/3", @"Fixed crash when diagnosing non-prefab GameObject.", 
			"0.8.30", "2017/2/12", @"Fixed manually fixing a missing script during automatic recovery not stopping the recovery when completed.", 
			"1.0.82", "2017/10/20", @"Added a cancellable progress bar when scanning.
Fixed MonoScript displaying more than the TextArea limit.
Fixed cast issue when trying to fetch enable from a Transform in NG Reorder Components.
Implemented selection change awareness in NG Missing Script Recovery.
Introduction of MonoScriptEditor. A simple script visualizer in Inspector.
Splitted NG Missing Script Recovery into many files.", 
			"1.1.89", "2018/5/13", @"Added WikiURL into NGAssemblyInfo.
Cached all calls to GUILayoutOption.
Changed version to 1.1.
Replaced GUI.enabled by EditorGUI.Begin/EndDisabledGroup."
		};
	}
}