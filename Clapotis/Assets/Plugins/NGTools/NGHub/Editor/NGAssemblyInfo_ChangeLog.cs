namespace NGToolsEditor.NGHub
{
	partial class NGAssemblyInfo
	{
		public static readonly string[]	ChangeLog = { 
			"0.2", "2016/6/23", @"Added button to easily toggle the mode.
First introduction of new tool NG Hub.
Fixed compatibility of component LoadScene in previous version.
Inversed defines compatibility in LoadScene.
Made NG Hub exportable.", 
			"0.3.3", "2016/6/29", @"Adjusted GUI in HubComponent AssetShortcut and LoadScene.
Fixed Component AssetShortcut and LoadScene now initializing correctly their GUIContent.
Fixed NG Hub displaying incomplete data at the first frame.
Fixed dropping feature failing when generating the Component.
Fixed wrong variable check, causing serializing to fault.
Improved display of HubComponent LoadScene and AssetShortcut.
Renamed field hubB into hubData in NGSettings.Hub.
Replaced stored data from EditorPrefs into NG Settings.", 
			"0.4.12", "2016/6/29", @"Fixed NG Hub not reloading correctly after 2 restart when docked.
Fixed null exception when editing a Component without NG Hub Extension opened.
Prevented HubComponent after deserialization.", 
			"0.4.13", "2016/6/29", @"Fixed extension window width in UNITY_4_7.
Removed corrupted component at initialization.", 
			"0.4.8", "2016/6/29", @"Greatly improved NG Hub. Added a dockable window and plenty of usefull stuff.
Moved data to NG Settings.", 
			"0.5.16", "2016/6/29", @"Added scrollbar in NG Hub Editor.
Changed shortcut %H by %#H.
Fixed NG Hub Extension not initializing settings.
Fixed Repaint not correctly called from Hub Editor.
Fixed null exception in the rare case where HubComponent window is opened but has no component.
Implemented drag & drop for Asset Shortcut and Load Scene.", 
			"0.6.21", "2016/8/7", @"Added a button to close the window when the dock mode is failing (Window without frame, no top menu or else).", 
			"0.6.23", "2016/10/6", @"Fixed hub component LoadScene requiring the scene to in the build.", 
			"0.6.24", "2016/10/16", @"Removed the unused method CustomGenericMenu in HubComponent.", 
			"0.8.27", "2017/1/13", @"Added message under OSX to warn about window switching on mouse's screen.
Changed an EditorPref key.
Implemented Undo.
Prevented hidden GUI to display tooltip.", 
			"0.9.37", "2017/5/1", @"Added workaround to compensate the Y-axis of the window.
Fixed dragging areas overflowing over default message.
Fixed scene having a wrong validation check.
Fixed scene validation in component LoadScene in Unity 5.2 or before.
Improved component Load Scene.
Improved component NG Scenes.", 
			"1.0.82", "2017/10/20", @"Fixed component MenuCaller not saving after picking a menu.
Fixed editing hub component not showing at the good position.
Fixed resize not correctly updating the windows.
Fixed right-width of NG Hub Extension with Unity 5.5 and newer.
Prevented configuration file warning to display rightafter pressing Play.
Separated per project if window is docked.", 
			"1.0.87", "2017/12/20", @"Added a color feedback when the scene is invalid in component Load Scene.
Adjusted width of the main window and the extension window.
Allowed background color for the docked window.
Fixed dropdown button showing up on wrong condition.
Fixed last component on the extension not receiving events.
Implemented NG Hub Dropdown to properly handle overflowing components.
Improved feedback and UI of components.
Prevented few fields in components Asset Shortcut and Menu Caller from serialization.", 
			"1.0.88", "2018/1/27", @"Adjusted windows in Unity 2017 and 2018.
Changed version to 1.3.", 
			"1.1.89", "2018/5/13", @"Added WikiURL into NGAssemblyInfo.
Cached all calls to GUILayoutOption.
Cached free ad string.
Changed version to 1.4.
Fixed colored UI when using personal skin.
Fixed docked window using DockBackgroundColor instead of its own color when available.
Fixed error popup messing the UI.
Fixed extension window throwing error due to source not initialized yet.
Fixed window parenting exception when playing with NG Hub fully opened.
Implemented NG Change Log.
Prevented drag & dropping from & to NG Hub.", 
			"1.1.90", "2018/5/18", @"Changed version to 1.5.
Fixed pro version not properly recognized."
		};
	}
}