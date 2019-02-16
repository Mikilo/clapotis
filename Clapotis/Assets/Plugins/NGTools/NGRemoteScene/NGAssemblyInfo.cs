#if NGTOOLS
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("NG Remote Scene")]
[assembly: AssemblyDescription("Plugin made for Unity.")]
[assembly: AssemblyProduct("Unity Editor")]
[assembly: AssemblyCompany("Michaël Nguyen")]
[assembly: AssemblyCopyright("Copyright © 2016 - Infinite")]
[assembly: AssemblyFileVersion("1.4.0.0")]
[assembly: InternalsVisibleTo("NGRemoteSceneEditor")]
[assembly: InternalsVisibleTo("NGRemoteScene_For_NGHub")]
#endif

namespace NGTools.NGRemoteScene
{
	partial class NGAssemblyInfo
	{
		public const string	Name = "NG Remote Scene";
		public const string	Version = "1.5";
		public const string	AssetStoreBuyLink = "http://u3d.as/UKJ";
		public const string	WikiURL = Constants.WikiBaseURL + "#markdown-header-13-ng-remote-scene";
	}
}