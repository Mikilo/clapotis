using NGPackageHelpers;
using System;
using System.IO;
using UnityEditor;

namespace NGToolsEditor.Internal
{
	using NGTools;

	[InitializeOnLoad]
	public class PackageExporter : UnityEditor.AssetModificationProcessor
	{
		public const string	ConstantsPath = "Assets/NGTools/Common/Constants.cs";

		public static bool		exporting;
		public static string	exportVersion;

		static	PackageExporter()
		{
			NGPackageExporterWindow.BeforeExport += PackageExporter.PrepareUpdateVersion;
			NGPackageExporterWindow.GetVersion = PackageExporter.GetVersion;
		}

		private static void	PrepareUpdateVersion(NGPackageExporterWindow exporter)
		{
			PackageExporter.exporting = true;
			PackageExporter.exportVersion = exporter.version;
		}

		private static string	GetVersion()
		{
			return Constants.Version;
		}

		private static string[]	OnWillSaveAssets(string[] paths)
		{
			if (PackageExporter.exporting == true)
			{
				PackageExporter.exporting = false;

				if (string.IsNullOrEmpty(PackageExporter.exportVersion) == false &&
					PackageExporter.exportVersion != Constants.Version)
				{
					if (File.Exists(PackageExporter.ConstantsPath) == true)
					{
						try
						{
							string[]	constantsLines = File.ReadAllLines(PackageExporter.ConstantsPath);

							for (int i = 0; i < constantsLines.Length; i++)
							{
								if (constantsLines[i].Contains("Version") == true)
								{
									// Generate a version number like <Version = "1.23.456";>
									try
									{
										constantsLines[i] = constantsLines[i].Replace(Constants.Version, PackageExporter.exportVersion);
									}
									catch (Exception ex)
									{
										InternalNGDebug.LogException(constantsLines[i], ex);
									}
									break;
								}
							}

							File.WriteAllLines(PackageExporter.ConstantsPath, constantsLines);
						}
						catch (Exception ex)
						{
							InternalNGDebug.LogException(ex);
						}
					}
					else
						InternalNGDebug.LogWarning("Export version has changed, but could not update the file.");
				}
			}

			return paths;
		}
	}
}