namespace NGTools
{
	public class Errors
	{
		public const int	None = 0;

		// Common errors 0XXX
		public const int	InternalServerError = -1;
		public const int	ServerException = -2;
		public const int	DataCorrupted = -3;
		public const int	UnhandledPacket = -100;
		public const int	Timeout = -101;

		// Scene server errors 3XXX
		public const int	Server_TypeNotFound = -3000;
		public const int	Server_GameObjectNotFound = -3001;
		public const int	Server_ComponentNotFound = -3002;
		public const int	Server_PathNotResolved = -3003;
		public const int	Server_MethodNotFound = -3004;
		public const int	Server_InvalidArgument = -3005;
		public const int	Server_InvocationFailed = -3006;
		public const int	Server_MaterialNotFound = -3007;
		public const int	Server_NGShaderNotFound = 3008;
		public const int	Server_ShaderNotFound = 3009;
		public const int	Server_ShaderPropertyNotFound = -3010;
		public const int	Server_DisableServerForbidden = -3011;
		public const int	Server_AssetNotFound = -3012;
		public const int	Server_TypeIndexOutOfRange = -3013;
		public const int	Server_AddingComponentFailed = -3014;
		public const int	Server_PartialGameObjectDeletion = -3015;

		// Scene errors. 35XX
		public const int	Scene_Exception = -3500;
		public const int	Scene_GameObjectNotFound = -3502;
		public const int	Scene_ComponentNotFound = -3503;
		public const int	Scene_PathNotResolved = -3504;
		public const int	Scene_SceneOutOfRange = -3520;
		public const int	Scene_SceneNotLoaded = -3521;
		public const int	Scene_SceneAlreadyActive = -3522;
		public const int	Scene_SceneInvalid = -3523;

		// Game Console errors. 4XXX
		public const int	GameConsole_NullDataConsole = 4000;

		// CLI errors. 45XX
		public const int	CLI_NotAvailable = -4500;
		public const int	CLI_RootCommandEmptyAlias = 4501;
		public const int	CLI_RootCommandNullBehaviour = 4502;
		public const int	CLI_EmptyRootCommand = 4503;
		public const int	CLI_MethodDoesNotReturnString = 4504;
		public const int	CLI_ForbiddenCommandOnField = 4505;
		public const int	CLI_UnsupportedPropertyType = 4506;
		public const int	CLI_ForbiddenCharInName = 4507;

		// Camera errors. 5XXX
		public const int	Camera_NotInitialized = -5000;
		public const int	Camera_ModuleNotFound = -5001;
		public const int	Camera_AnchorNotFound = -5002;
		public const int	Camera_InvalidAnchor = -5003;
		public const int	Camera_CameraNotFound = -5004;

		// Fav errors. 7XXX
		public const int	Fav_ResolverNotStatic = 7000;
		public const int	Fav_ResolverIsAmbiguous = 7001;
		public const int	Fav_ResolverThrownException = 7002;
	}
}