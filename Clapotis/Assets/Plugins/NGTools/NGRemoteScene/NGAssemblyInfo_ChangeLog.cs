namespace NGTools.NGRemoteScene
{
	partial class NGAssemblyInfo
	{
		public static readonly string[]	ChangeLog = { 
			"0.2", "2016/6/23", @"Added exception MissingTypeHandlerException.
Added highlight on the original reference.
Added item NG Project in NG Hierarvhy's context menu.
Added little spacing in NG Project's header.
Added prompt to send packets when disabling Batch mode.
Changed namespace.
Displayed packet's value in historic when updating a field.
Fixed historic not correctly displayed.
Fixed network issue when a reference is null in embedded assets in NG Server Scene.
Handled instant update in Resources Picker.
Implemented delete key in Object field.
Implemented method IsDefined and GetCustomAttributes in IValueGetter.
Implemented texture's Offset and Scale in Material editor.
Improved Batch mode by replacing GUILayout by GUI.
Improved code.
Memorized values when using Method Invoker wizard.
Removed debug.", 
			"0.4.12", "2016/6/29", @"Implemented method Serialize with less parameters.
Removed the Debug mode in NG Inspector.
Renamed method FastSerialize by Serialize.", 
			"0.5.16", "2016/6/29", @"Added method in IUnityData to fetch a resource's name. Used in OnGUI of Packet.
Implemented multi-ComponentExposer.
Added missing properties in ColliderExposer.
Fixed ArrayDrawer deleting foldout when inspector was too small.
Fixed ArrayHandler not working on array of UnityEngine.Object.
Forgotten files for multi exposers.
Improved debug for Packet.
Modified GUI when NetMaterial is not loaded yet in NG Inspector.
Modified height of message when NG Remote has no NG Hierarchy available.
Removed Packet's method handler for Scene_ClientSetSibling in ClientSceneExecuter.
Removed call to Show when calling GetWindow before.
Removed unused field pathEndPosition in DataDrawer.
Renamed hierarchy by Hierarchy in NGRemoteWindow.", 
			"0.5.17", "2016/7/9", @"Added a received bytes counter in NG Camera.
Appended hours, minutes and seconds after replay's default save name.
Auto select the new replay if there is none.
Fixed GUI of modules Keyboard and Mouse.
Fixed Mouse and Touch module not sending Y position from top.
Fixed NG Camera not allowed to reconnect right after disconnecting, due to request channel being blocked.
Fixed NG Camera not drawing rectangle correctly when dragging.
Fixed UDP client not working if it does not send a packet right after connecting.
Fixed disabling module Screenshot in NG Camera.
Fixed fetching value from dictionary in an assert call.
Fixed incrementing UDP port in each initialization.
Forced repaint of NG Hierarchy when server instances list is modified.
Improved feedback when raycasting in NG Camera.
Paused current replay when switching between replays.
Renamed ReplayDataModuleEditor by ReplayDataModule.", 
			"0.5.18", "2016/7/10", @"Added context menu when right clicking on replay tab to delete it.
Fixed NG Hierarchy not disconnecting correctly when a socket is shutdown unexpectedly.
Forced focus of ghost camera when connecting NG Camera.
Implemented button in NG Camera to pick ghost camera and move it to current camera.
Implemented drag in NG Camera's camera GUI to rotate and move camera like Scene window.", 
			"0.5.19", "2016/7/12", @"Added anchoring of ghost camera.
Fixed Unity compatibility issue.
Fixed error when disconnecting NG Hierarchy by recompiling or else.
Renamed and moved enum Buttons by MouseButtons from NGServerCamera into MouseModule.
Replaced UDP by TCP in NG Camera.", 
			"0.6.21", "2016/8/7", @"Added error notification when requesting a not found module.
Added feedback when saving a replay.
Added monitoring packets of NG R Hierarchy's Client as a debug only.
Added packets monitoring and scene status debug in NG Server Scene.
Fixed NGRemoteWindow never correctly added to NG Remote Hierarchy.
Fixed UDP broadcast server initialization failure.
Fixed Utility.content.tooltip was not cleared after use.
Fixed array of classes sending real Type instead of GenericClass.
Fixed camera updating position instead of local position. Was wrong since ghost camera cans stick to any Transform.
Fixed connecting to a server was adding a wrong address to the auto-detect servers list.
Fixed disconnecting NG Remote Hierarchy. It does not block anymore and try to close the Client.
Fixed handling disconnected client.
Fixed null exception in Awake of CameraModulesRunner. Modules were not assigned yet.
Fixed null exception in NG Remote Camera when opening the window and choosing tab Modules and connecting NG Remote Hierarchy.
Handled IPV6 when overriding address and port.
Implemented async connection of NG Remote Hierarchy.
Implemented range of ports for UDP broadcasting.
Improved handling modules of NG Camera, now modules are unique and send only to those who has activate them.
Removed unused field in NGServerScene.
Slightly revamped how NG Hierarchy handles auto-detection.", 
			"0.6.22", "2016/9/21", @"Added a type for assets embarked in Project, allowing a more accurate drag & drop from.
Added color for both skin for Material header in remote Inspector.
Added exposer Rigidbody.
Added more detail in exception message.
Fixed Inspector asking for type different than Texture when picking a Texture.
Fixed array of UnityObject not handled through network.
Fixed button not displaying correctly.
Fixed exposer of AudioSource.
Fixed scanning shaders gathering all shaders with built-ins.
Fixed spacing added only in Repaint, creating shift in the drawing.
Implemented placeholder in R Hierarchy to reduce used space.
Improved debug logs.
Improved key inputs of ResourcesPicker.
Prevented Inspector to stop all GUI due to an issue in a Component.
Removed a warning.
Simplified NG Server Scene's inspector.", 
			"0.6.23", "2016/10/6", @"Implemented array of UnityObject.
Implemented selection of an asset in R Project.
Prevented NG Camera to serialize the textureModule field.", 
			"0.6.24", "2016/10/16", @"Added a scrollbar in ghost camera options in NG Remote Camera.
Added comments and renamed few methods.
Added headers in NG Server Scene.
Added info ""Not Connected"" in NG R Hierarchy.
Fixed ghost camera's settings scrollbar expanding on height while not even shown.
Fixed sorting modules not working at all.
Implemented Batch mode only for some packets.
Moved packet ClientModuleSetUseJPG into its own file.
Moved packet ServerStickGhostCamera into its own file.
Saved fold state of folders in ListingAssetDrawer.", 
			"0.6.25", "2016/12/14", @"Changed NG Remote Scene to send disabled Game Object in hierarchy.
Fixed a warning compile in non-debug mode.", 
			"0.8.27", "2017/1/13", @"Added sending a disconnect packet when disconnecting from NG R Camera.
Appended [BETA] to NG Remote Camera and NG Replay titles.
Changed static method name in CustomMonitorData.
Discarded generic method from remote invoking.
Fixed EnumHandler not deserializing correctly.
Fixed NG Server Scene not correctly fetching all GameObject.
Fixed cast errors when copying Component.
Fixed disabled GameObject not displayed as disabled in NG R Hierarchy.
Fixed invoking method sharing its name.
Fixed list not cleared when loading Component in Unity 4.
Forced DeleteGameObjects in NGServerScene to update the monitoring avoiding a deletion duplication.
Greatly improved NG R Inspector rendering.
Implemented EnumArgumentDrawer.
Implemented Exception MissingComponent.
Implemented method's return value feedback after invoke.
Implemented opening replay when clicking on ""No replay yet"" message.
Inserted Remote after NG in classes Hierarchy, Inspector, Project, Camera.
Integrated free version limitation in NGRemoteWindow.
Optimized ListAssetsDrawer.
Prevented Packet monitoring to display latest Packet.
Prevented an exception when playing with no listener in NG Server Scene.
Prevented embedding null assets in NG Server Scene.
Renamed methods Serialize/Deserialize with Save/Load in ArgumentDrawer.
Revamped how options were displayed. Now appearing in a nice popup.
Updated EnumHandler to handle Type Enum.
Updated all ComponentExposer.", 
			"0.8.32", "2017/2/13", @"Revamped Ping not working correctly on some devices.", 
			"0.8.33", "2017/3/3", @"Fixed MethodArgumentsWindow throwing error when unplaying/playing.
Implemented auto-refresh hierarchy.", 
			"1.0.82", "2017/10/20", @"Added a lot of debug logs.
Added debugs in network object serializers.
Added link.xml to prevent IL2CPP stripping types used in exposers.
Added prefix in paths's foldout of ListingAssetsDrawer.
Changed GUI of replay modules Keyboard and Mouse.
Changed message when auto-disconnecting on an expired ping.
Changed order when fetching Type from string in TypeHandlersManager.
Changed selection now changing on MouseDown in NG R Hierarchy.
Embed TypeHandler into NetField and ArrayHandler to guarantee a safe deserialization.
Fixed ClassDrawer embedding its object. It now uses the DataDrawer.
Fixed NGServerScene processing only one root GameObject.
Fixed array not resizing.
Fixed change scene notifier not reset when disconnecting.
Fixed custom TypeHandler not being correctly handled.
Fixed enum not updating the server, due to Type mismatch.
Fixed event not unregistered in NGServerCamera.
Fixed fetching TypeHandler from string type not looking in all assemblies.
Fixed gathering root GameObject.
Fixed hierarchy not resetting ""auto request hierarchy"" timer.
Fixed icon not restoring.
Fixed null exception when drag & dropping Game Object in NG R Hierarchy.
Fixed opening NG R Camera from context menu.
Fixed scanned shaders in  NGServerScene not persisted.
Fixed server not closing the client socket on disconnection.
Fixed type Renderer not accepted as a Material holder.
Fixed typo in title when exporting replay from NG R Camera.
Greatly improved GUI of NG R Project.
Implemented Scene.
Implemented buffer fallbacks to greatly enhance safety.
Implemented fallback whenever a field is failing.
Implemented notification when the hierarchy is automatically refreshed and massive.
Improved ClientComponent's GUI.
Improved GUI of ListingAssetsDrawer.
Improved GUI of Material in NG R Inspector.
Improved UI in NG R Inspector.
Improved algorithm of ListingAssetsDrawer.
Improved client types management.
Improved debug info.
Improved interaction with null array. We can modify them now.
Moved GUI style SmallLabel into ValueMemorizer<T>.
Optimized fetching of FieldInfo and PropertyInfo.
Prevented assigning a prefab anchor to the ghost camera.
Prevented pinging the server when using Verbose mode.
Refactored a lot of code.
Removed legacy code.
Removed preprocessor NGTOOLS_FREE.
Replaced returning null by throwing an exception when no TypeHandler is found.
Replaced the way to open remote windows.
Turned logs into error logs.
Wrapped array size between brackets in ArrayDrawer.", 
			"1.0.87", "2017/12/20", @"Fixed NG Server Camera stops working when timeScale is 0.
Fixed component exposers not working with Unity 2017.3 and higher.
Implemented ""Use as anchor"" from NG R Hierarchy.
Implemented dynamic context-menu on Game Object in NG R Hierarchy.
Implemented ping on key F in NG R Hierarchy.", 
			"1.0.88", "2018/1/27", @"Changed version to 1.3.", 
			"1.1.89", "2018/5/13", @"Added WikiURL into NGAssemblyInfo.
Added option autoDestroyInProduction.
Cached all calls to GUILayoutOption.
Cached free ad string.
Changed default network refresh interval from 0,01 to 0,025.
Changed version to 1.4.
Displayed GameObject's children as disabled when parent is disabled.
Fixed NG R Hierarchy not updating when not focused.
Fixed changing a Shader now updating the Material accordingly.
Fixed colored UI when using personal skin.
Fixed conflict between members sharing a name.
Fixed deleting a Component not synchronized on the client.
Fixed deleting a GameObject putting all its children in the root.
Fixed disconnected client still processing data.
Fixed displaying GameObject's name not using the whole width.
Fixed null exception when changing with a non-existent Shader.
Fixed obfuscation renaming of Unity messages.
Fixed remote windows not repainting when created after NG R Hierarchy connects.
Fixed scaling time to 0 freezing module Screenshot.
Forced opening remote windows near their Unity equivalent.
Implemented NG Change Log.
Implemented NG Remote Static Inspector.
Implemented button in NG R Hierarchy to focus remote/Unity windows in a single click.
Implemented cascade folding in NG R Hierarchy.
Implemented sending Texture2D/Sprite to the device.
Improved cleaning when disconnecting from a server.
Improved multi NG Remote Hierarchy.
Improved waiting feedbacks when requesting the server.
Optimized allocations and GUI repaint.
Prevented overriding properties to show up twice in NG R Inspector.
Prevented the user to delete or disable the server.
Replaced GUI.enabled by EditorGUI.Begin/EndDisabledGroup.
Revamped ListingAssetsDrawer, now deported to a window instead of inline Inspector.
Updated Component exposers for Unity 2018.", 
			"1.1.90", "2018/5/18", @"Changed version to 1.5.
Fixed pro version not properly recognized.
Fixed referencing assets for NG R Project not dirtying the Component."
		};
	}
}