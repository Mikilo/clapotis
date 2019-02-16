using NGTools.Network;

namespace NGTools.NGRemoteScene
{
	[RegisterPacketIds]
	internal static class RemoteScenePacketId
	{
		public const int	Unity_ClientRequestLayers = 3000;
		public const int	Unity_ServerSendLayers = 3001;

		public const int	Unity_ClientRequestAllComponentTypes = 3002;
		public const int	Unity_ServerSendAllComponentTypes = 3003;

		public const int	Unity_ClientRequestEnumData = 3004;
		public const int	Unity_ServerSendEnumData = 3005;

		public const int	Scene_ClientRequestHierarchy = 3100;
		public const int	Scene_ServerSendHierarchy = 3101;
		public const int	Scene_ClientSetActiveScene = 3102;
		public const int	Scene_ServerSetActiveScene = 3103;
		public const int	Scene_NotifySceneChanged = 3104;

		public const int	Asset_ClientRequestResources = 3200;
		public const int	Asset_ServerSendResources = 3201;
		public const int	Asset_ClientRequestProject = 3210;
		public const int	Asset_ServerSendProject = 3211;
		public const int	Asset_ClientRequestUserAssets = 3220;
		public const int	Asset_ServerSendUserAssets = 3221;
		public const int	Asset_ClientSendUserTexture2D = 3222;
		public const int	Asset_ClientSendUserSprite = 3223;
		public const int	Asset_ServerAcknowledgeUserTexture = 3224;

		public const int	Asset_ClientRequestRawAsset = 3230;
		public const int	Asset_ServerSendRawAsset = 3231;

		public const int	Class_ClientUpdateFieldValue = 3300;
		public const int	Class_ServerUpdateFieldValue = 3301;
		public const int	Class_NotifyFieldValueUpdated = 3302;
		public const int	Class_ClientLoadBigArray = 3310;

		public const int	Transform_ClientSetSibling = 3400;
		public const int	Transform_ServerSetSibling = 3401;

		public const int	GameObject_ClientRequestGameObjectData = 3500;
		public const int	GameObject_ServerSendGameObjectData = 3501;
		public const int	GameObject_ClientWatchGameObjects = 3502;

		public const int	GameObject_ClientDeleteGameObjects = 3510;
		public const int	GameObject_ServerDeleteGameObjects = 3511;
		public const int	GameObject_NotifyGameObjectsDeleted = 3512;

		public const int	GameObject_ClientAddComponent = 3520;
		public const int	GameObject_ServerAddedComponent = 3521;
		public const int	GameObject_NotifyComponentAdded = 3522;

		public const int	Component_ClientInvokeBehaviourMethod = 3600;
		public const int	Component_ServerReturnInvokeResult = 3601;
		public const int	Component_ClientDeleteComponents = 3602;
		public const int	Component_ServerDeleteComponents = 3603;
		public const int	Component_NotifyDeletedComponents = 3604;

		public const int	Material_ClientRequestMaterialData = 3700;
		public const int	Material_ServerSendMaterialData = 3701;
		public const int	Material_ClientWatchMaterials = 3702;
		public const int	Material_NotifyMaterialDataChanged = 3703;

		public const int	Material_ClientUpdateMaterialProperty = 3710;
		public const int	Material_ServerUpdateMaterialProperty = 3711;
		public const int	Material_NotifyMaterialPropertyUpdated = 3712;
		public const int	Material_ClientUpdateMaterialVector2 = 3713;
		public const int	Material_ServerUpdateMaterialVector2 = 3714;
		public const int	Material_NotifyMaterialVector2Updated = 3715;
		public const int	Material_ClientChangeMaterialShader = 3716;

		public const int	StaticClass_ClientRequestInspectableTypes = 3800;
		public const int	StaticClass_ServerSendInspectableTypes = 3801;
		public const int	StaticClass_ClientRequestTypeStaticMembers = 3802;
		public const int	StaticClass_ServerSendTypeStaticMembers = 3803;
		public const int	StaticClass_ClientWatchTypes = 3804;

		public const int	Camera_ClientConnect = 3900;
		public const int	Camera_ClientDisconnect = 3901;
		public const int	Camera_ServerIsInitialized = 3902;

		public const int	Camera_ClientRequestAllCameras = 3910;
		public const int	Camera_ServerSendAllCameras = 3911;
		public const int	Camera_NotifyAllCameras = 3913;

		public const int	Camera_ClientSetSetting = 3920;
		public const int	Camera_ClientResetCameraSettings = 3921;
		public const int	Camera_ServerResetCameraSettings = 3922;
		public const int	Camera_ClientToggleModule = 3923;

		public const int	Camera_ClientSetDefaultGhostCamera = 3924;
		public const int	Camera_ClientSetGhostCamera = 3925;
		public const int	Camera_ServerSetGhostCamera = 3926;
		public const int	Camera_ClientPickCamera = 3927;
		public const int	Camera_ClientPickGhostCameraAtCamera = 3928;
		public const int	Camera_ClientStickGhostCamera = 3929;
		public const int	Camera_ServerStickGhostCamera = 3930;

		public const int	Camera_NotifyCameraData = 3931;
		public const int	Camera_NotifyCameraTransform = 3932;
		public const int	Camera_ClientSendCameraInput = 3933;
		public const int	Camera_ClientSendCameraTransformPosition = 3934;
		public const int	Camera_ClientSendCameraTransformRotation = 3935;
		public const int	Camera_ClientSendCameraZoom = 3936;

		public const int	Camera_ClientRaycastScene = 3940;
		public const int	Camera_ServerSendRaycastResult = 3941;

		public const int	Camera_ClientModuleSetUseJPG = 10000;
		public const int	Camera_ClientModuleSetUseCompression = 10001;
	}
}