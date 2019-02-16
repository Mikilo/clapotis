using System.Runtime.CompilerServices;

#if FULL_NGTOOLS || NGTOOLS
using NGTools;
using System.Reflection;

[assembly: AssemblyTitle(Constants.PackageTitle)]
[assembly: AssemblyDescription("Plugin made for Unity.")]
[assembly: AssemblyProduct("Unity Editor")]
[assembly: AssemblyCompany("Michaël Nguyen")]
[assembly: AssemblyCopyright("Copyright © 2016 - Infinite")]
[assembly: AssemblyFileVersion(Constants.Version)]
[assembly: AssemblyVersion("0.0.0.0")]
#endif

#if NGTOOLS
[assembly: InternalsVisibleTo("NGCoreEditor")]
[assembly: InternalsVisibleTo("NGAssetFinderEditor")]
[assembly: InternalsVisibleTo("NGComponentReplacerEditor")]
[assembly: InternalsVisibleTo("NGComponentsInspectorEditor")]
[assembly: InternalsVisibleTo("NGConsole")]
[assembly: InternalsVisibleTo("NGConsoleEditor")]
[assembly: InternalsVisibleTo("NGDraggableObjectEditor")]
[assembly: InternalsVisibleTo("NGFav")]
[assembly: InternalsVisibleTo("NGFavEditor")]
[assembly: InternalsVisibleTo("NGFullscreenBindingsEditor")]
[assembly: InternalsVisibleTo("NGGameConsole")]
[assembly: InternalsVisibleTo("NGHierarchyEnhancerEditor")]
[assembly: InternalsVisibleTo("NGHubEditor")]
[assembly: InternalsVisibleTo("NGInspectorGadgetEditor")]
[assembly: InternalsVisibleTo("NGMissingScriptRecoveryEditor")]
[assembly: InternalsVisibleTo("NGNavSelectionEditor")]
[assembly: InternalsVisibleTo("NGPrefsEditor")]
[assembly: InternalsVisibleTo("NGRemoteScene")]
[assembly: InternalsVisibleTo("NGRemoteSceneEditor")]
[assembly: InternalsVisibleTo("NGRenamerEditor")]
[assembly: InternalsVisibleTo("NGScenesEditor")]
[assembly: InternalsVisibleTo("NGShaderFinderEditor")]
[assembly: InternalsVisibleTo("NGSpotlightEditor")]
[assembly: InternalsVisibleTo("NGSyncFoldersEditor")]
[assembly: InternalsVisibleTo("NGHubEditor")]
[assembly: InternalsVisibleTo("RemoteModule_For_NGConsole")]
#endif

[assembly: InternalsVisibleTo("NGToolsEditor")]
[assembly: InternalsVisibleTo("Assembly-CSharp")]
[assembly: InternalsVisibleTo("Assembly-CSharp-firstpass")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-firstpass")]

namespace NGTools
{
	partial class NGAssemblyInfo
	{
		public const string	Name = "NG Core";
		public const string	Version = "1.6";
		public const string	AssetStoreBuyLink = "http://u3d.as/f0e";
		public const string	WikiURL = "";
	}
}