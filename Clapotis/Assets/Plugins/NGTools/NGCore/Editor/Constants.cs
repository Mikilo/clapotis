using UnityEngine;
using NGConstants = NGTools.Constants;

namespace NGToolsEditor
{
	public static class Constants
	{
		#region NG Tools
		public const string	Version = NGConstants.Version;
		public const string	InternalPackageTitle = NGConstants.InternalPackageTitle;
		public const string	PackageTitle = NGConstants.PackageTitle;
		public const string	PreferenceTitle = NGConstants.PackageTitle;
		public const string	RootFolderName = "NGTools";
#if NGTOOLS
		public const string	SettingsFilename = "NGSettings.lib.asset";
#else
		public const string	SettingsFilename = "NGSettings.asset";
#endif
		public const string	DefaultDebugLogFilepath = NGConstants.DefaultDebugLogFilepath;
		public const string	ConfigPathKeyPref = "NGTools_ConfigPath";
		public const string	WikiBaseURL = NGConstants.WikiBaseURL;
		public const string	DiscordURL = "https://discord.gg/sf8RHE5";
		public const string	TwitterURL = "https://twitter.com/_Mikilo_";
		public const string	TicketURL = "https://bitbucket.org/Mikilo/neguen-tools/issues?status=new&status=open";
		public const string	ChangeLogURL = "https://bitbucket.org/Mikilo/neguen-tools/wiki/Changelog";
		public const string	AssetStoreNGToolsFreeURL = "https://www.assetstore.unity3d.com/en/#!/content/80093";
		public const string	AssetStoreNGToolsProURL = "https://www.assetstore.unity3d.com/en/#!/content/34109";
		#endregion

		#region Localization
		public const string	DefaultLanguage = "english";
		public const string	RelativeLocaleFolder = "Locales/";
		public const string	LanguageEditorPref = "NGTLocalizationLanguage";
		#endregion

		#region Inputs Manager
		public const float	CheckFadeoutCooldown = 6F;
		#endregion

		#region Export
		public const string	LastExportPathPref = "NGTLastExportPath";
		#endregion

		#region Contact
		public const string	SupportForumUnityThread = "http://forum.unity3d.com/threads/released-ng-tools-skyrocket-your-unity-workflow-efficiency.378040/";
		public const string	SupportEmail = "support@ngtools.tech";
		#endregion

		#region Common
		public const EventModifiers	ByPassPromptModifier = EventModifiers.Shift;
		public const float	MinStartDragDistance = 40F;
		public const double	DoubleClickTime = .3D;
		public const int	MenuItemPriority = 1000;
		public const float	SingleLineHeight = 16F;
		public const string	MenuItemPath = "Window/" + Constants.PackageTitle + "/";
		#endregion
	}
}