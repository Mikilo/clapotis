namespace NGToolsEditor.NGDraggableObject
{
	partial class NGAssemblyInfo
	{
		public static readonly string[]	ChangeLog = { 
			"0.2", "2016/6/23", @"Changed drop highlight.
Fixed null exception when dragging Object over a null entry in an array.
Fixed rank and context menu not showing on type Object.
Implemented command Copy, Cut and Paste.", 
			"0.5.17", "2016/7/9", @"Fixed dragging a prefab on a field. It takes only Component of the selected Game Object.", 
			"0.6.23", "2016/10/6", @"Changed toggling drag object. It does not need to recompile now.", 
			"0.6.24", "2016/10/16", @"Fixed Unity 4 compatibility.", 
			"0.8.28", "2017/1/13", @"Fixed dragging anywhere starting an Object drag.", 
			"0.8.30", "2017/2/12", @"Fixed rare bug where a Component has a null GameObject.", 
			"0.8.32", "2017/2/13", @"Implemented NG Draggable Object as a toggleable tool.", 
			"1.0.82", "2017/10/20", @"Implemented context menu when clicking on indexer.
Prevented null exception when toying with assets containing null members.
Replaced how NG Draggable Object injects itself into Unity Editor. Now uses reflection instead of a constant.", 
			"1.0.86", "2017/12/20", @"Fixed drawer in array misusing FieldType in Unity 2017.", 
			"1.1.89", "2018/5/13", @"Added WikiURL into NGAssemblyInfo.
Changed version to 1.2.
Implemented NG Change Log.
Prevented drag update to clear drag data.
Replaced StringComparison argument InvariantCultureIgnoreCase with OrdinalIgnoreCase."
		};
	}
}