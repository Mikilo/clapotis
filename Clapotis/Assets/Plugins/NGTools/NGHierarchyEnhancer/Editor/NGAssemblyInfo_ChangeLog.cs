namespace NGToolsEditor.NGHierarchyEnhancer
{
	partial class NGAssemblyInfo
	{
		public static readonly string[]	ChangeLog = { 
			"0.2", "2016/6/23", @"Fixed null exception when browsing components.
Fixed rect not starting at 0.
Handled AudioSource, Renderer and ParticleSystem in GameObjectMenu.
Replaced Button by Toggle when toggling Game Object's active state.
Replaced interface IHierarchyMenuItem by a call to a method using reflection.
Replaced label NG by a button.", 
			"0.3.3", "2016/6/29", @"Integrated Undo record when toggling GameObject's active.
Prevented force focus when the mouse is not over the Hierarchy window.", 
			"0.4.8", "2016/6/29", @"Moved data to NG Settings.", 
			"0.6.23", "2016/10/6", @"Fixed Inspector not refreshed when toggling a Component.", 
			"0.6.24", "2016/10/16", @"Implemented background color associated with a layer.
Implemented drawing icon per layer.", 
			"0.6.25", "2016/12/14", @"Implemented coloring per Component.
Improved GUI in NG Settings.
Implemented multi GameObject toggling.", 
			"0.8.27", "2017/1/13", @"Fixed mask value not correctly handled, preventing display of toggle and others.
Replaced a cast.", 
			"0.8.35", "2017/4/16", @"Added an option to automatically draw Unity's native Component.
Added witness texture per Component.
Fixed removing element from Component Colors not removing anything.", 
			"0.8.36", "2017/4/21", @"Fixed offset when displaying Component textures in Unity 5.3 or under.", 
			"0.9.37", "2017/5/1", @"Changed option Draw Unity Components's default value to false.
Fixed wrong texture's offset in Unity 5.3.8.", 
			"1.0.82", "2017/10/20", @"Changed Metrics now called when really using the GUI.
Replaced calling OnHierarchyGUI via Reflection with an interface.", 
			"1.1.89", "2018/5/13", @"Added WikiURL into NGAssemblyInfo.
Changed version to 1.1.
Implemented NG Change Log.
Optimized allocations and GUI repaint.
Replaced GUI.enabled by EditorGUI.Begin/EndDisabledGroup."
		};
	}
}