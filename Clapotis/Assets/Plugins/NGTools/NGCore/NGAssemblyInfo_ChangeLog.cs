namespace NGTools
{
	partial class NGAssemblyInfo
	{
		public static readonly string[]	ChangeLog = { 
			"1.0.82", "2017/10/20", @"Added friendlyness to NGCoreDitor.
Added icons in Preferences.
Adjusted GUI open/save file methods in NGEditorGUILayout.
Changed version to 1.0.82.
Fixed NG Updater, it is now working in all versions of Unity.
Fixed Utility.IsComponentEnableable always returning true.
Fixed a crash due to the current scene.
Fixed delegate field being allowed by Utility.CanExposeTypeInInspector().
Fixed disabling send stats not correctly prompting the message.
Fixed import not importing all assets.
Fixed installation setup ovewritting the settings.
Fixed out of range exception when a settings object is null and last of the array of NG Settings.
Fixed restoring a backup not properly handling directory.
Implemented OpenFileField() in NGEditorGUILayout.
Implemented SafeLoadAllAssetsAtPath() in Utility.
Implemented a blocker to prevent fresh install setup.
Implemented fresh installation setup.
Implemented scene restoration after updating.
Implemented window About.
Improved GUI of Contact Form.
Improved GUI of licenses in Preferences.
Possibly fixed a small issue, preventing double notification from NG Fullscreen Bindings.
Prevented OnGUI() in NG Diagnostic being obfuscated.
Removed legacy check version method.
Renamed Common by NG Core.
Replaced hardcoded channels by remote channels in channel subscription GUI.", 
			"1.0.83", "2017/10/20", @"Fixed Show/HideIf not working on non-public fields.
Wrapped locking methods WaitOne/Set in try/finally.", 
			"1.0.85", "2017/10/31", @"Added images in window About.", 
			"1.0.86", "2017/12/20", @"Fixed sending stats not having invoices ready on time.", 
			"1.0.87", "2017/12/20", @"Added usage on attribute GHeader.
Changed access to member info of Field/PropertyModifier.
Fixed changing Company Name or Product Name losing internal configuration files.
Moved constant AllowSendStatsKeyPrefs from Constants to HQ.
Prevented obfuscation of UnityAssemblyVerifier's method OnGUI.", 
			"1.0.88", "2018/1/27", @"Changed main version to 1.0.88.
Changed version to 1.4.
Fixed Discord, Twitter and Unity icons being blurry.
Fixed Horizontal/VerticalScrollbar not having a good scrollbar color in personal skin.
Fixed NG icon having a mipmap in Unity editor.
Fixed RequestURL throwing a null exception when an inner exception is thrown.
Fixed request through HTTPS failing due to certificate validation.
Implemented complementary stats methods.
Improved send stats request handling on failure.
Removed update system.
Changed URL protocol from HTTP to HTTPS.", 
			"1.1.89", "2018/5/13", @"Added LogFormat() and VerboseLogFormat() in InternalNGDebug.
Added WikiURL into NGAssemblyInfo.
Added button on active licenses to show its active seats.
Added entry in window's context menu to open the change log.
Added loggers in InternalNGDebug.
Added method extension Utility.Append() for StringBuilder.
Added quotes around server's answer when prompting an issue.
Added the FileIdentifier of the asset in NG Check GUID.
Cached all calls to GUILayoutOption.
Changed Utility.NicifyVariableName() to handle ""m_"" prefix.
Changed main version to 1.1.89.
Changed version to 1.5.
Fixed InternalNGDebug.LogFile() throwing null exception on a null argument.
Fixed Unity 2018.2 compatibility issues.
Fixed Unity compatibility issue.
Fixed drawer of attributes ShowIf & HideIf.
Fixed help links URL.
Fixed network Client processing an empty buffer.
Fixed null exception in ShowIfDrawer when condition field is not found.
Fixed obfuscation renaming of Unity messages.
Fixed out of range exception when caching negative integer.
Fixed prefix ""NG"" being removed in an utility window.
Fixed sending stats not working correctly.
Fixed wizard GenericTypesSelector not handling interfaces.
Implemented AssertFormat() in InternalNGDebug.
Implemented ElasticLabel.
Implemented GUILayoutOptionPool.
Implemented NG Change Log.
Implemented Utility.GetLocalIdentifierFromObject() to get the real file identifier.
Implemented ViewTextWindow.
Implemented a popup message to ask the user to show the active seats when the requested license reached the maximum activation limit.
Implemented cache in Utility.GetType() and Utility.EachAllSubClassesOf().
Implemented overloads of DropZone() in Utility.
Implemented suffix path for NGEditorGUILayout.OpenFolderField() when opening it.
Improved GUI in window License's Seats.
Improved Packet auto-serializer to handle nested collection.
Improved reading file performance.
Increased the size of the message ""No invoice activated."" in the Licenses tab.
Moved GetSharedSettingsPath() from Preferences to NGSettings.
Optimized allocations and GUI repaint.
Removed Revoke seat button if the license is public.
Removed debug in ViewTextWindow.
Replaced GUI.enabled by EditorGUI.Begin/EndDisabledGroup.
Replaced context-menu by ""Seats"" button in tab Licenses.
Sealed attribute classes.", 
			"1.1.90", "2018/5/18", @"Changed version to 1.6.
Fixed pro version not properly recognized."
		};
	}
}