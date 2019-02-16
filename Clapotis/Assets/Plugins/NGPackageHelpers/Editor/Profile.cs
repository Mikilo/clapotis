using System;
using System.Collections.Generic;

namespace NGPackageHelpers
{
	[Serializable]
	public class Reference
	{
		public bool		localProfile;
		public string	nameProfile;
		public string	path;
		public bool		localCopy;
		public bool		editor;
	}

	[Serializable]
	public class Profile
	{
		public string	name;

		// Exclusion/inclusion keywords.
		public List<string>	excludeKeywords = new List<string>();
		public List<string>	includeKeywords = new List<string>();

		// Common
		public string	packagePath = "Path/To/Package";

		// Export data
		public string	exportPath = @"C:\SOME\WHERE";
		public string	nameFormat = "PACKAGENAME_{0}_{1}";
		public string	devPrefix = "DEV_";

		// Deploy data
		public List<string>	projects = new List<string>();
		public bool			deployMeta = false;

		// DLL
		public List<string>	unityProjectPaths = new List<string>();

		public string			DLLName = "PackageName";
		public bool				appendVersion = true;
		public bool				appendEditor = true;
		public string			outputPath = "PathToDestination";
		public string			outputEditorPath = "PathToDestination";
		public string			defines = "";
		public string			editorDefines = "UNITY_EDITOR;UNITY_EDITOR_WIN;";
		public List<Reference>	references = new List<Reference>();
		public bool				unityEngineDLLRequiredForEditor = false;
		public bool				generateDocumentation = false;
		public bool				generateProgramDatabase = false;

		public string	confuserExPath = @"C:\ConfuserEx";
		public string	obfuscateFilters = string.Empty;

		public bool	copyMeta = false;

		public bool	showReferences = true;
		public bool	showForceResourcesKeywords = true;
		public bool	showObfuscationFilters = true;

		public List<string>	forceResourcesKeywords = new List<string>();
	}
}