using System;
using System.IO;
using UnityEditor;

namespace NGToolsEditor.Internal
{
	static class ExportUtility
	{
		private static void	CLIExportPackage()
		{
			string[]	args = Environment.GetCommandLineArgs();

			ExportUtility.ExportPackage(args, 8);
		}

		public static void	ExportPackage(string[] args, int offset)
		{
			for (int i = offset; i < args.Length;)
			{
				string		destination = args[i];
				string[]	files = new string[int.Parse(args[i + 1])];

				for (int j = 0; j < files.Length; j++)
					files[j] = args[i + j + 2];

				AssetDatabase.ExportPackage(files, destination, ExportPackageOptions.Default);
				AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

				for (int j = 0; j < files.Length; j++)
				{
					AssetDatabase.DeleteAsset(files[j]);

					if (Directory.GetFiles(Path.GetDirectoryName(files[j])).Length == 0)
						AssetDatabase.DeleteAsset(Path.GetDirectoryName(files[j]));
				}

				i += 2 + files.Length;

				string	target = Path.GetFileNameWithoutExtension(destination);
				target = target.Substring(target.LastIndexOf('_') + 1);
				File.WriteAllText(Path.Combine(Path.GetDirectoryName(destination), "README.txt"), "The package \"" + Path.GetFileName(destination)  + "\" is an addon for \"" + target + @""".
It can only be used if """ + target + @""" is installed.

WARNING: It can contain source code, importing it without the dependencies will generate compilation errors!");
			}
		}
	}
}