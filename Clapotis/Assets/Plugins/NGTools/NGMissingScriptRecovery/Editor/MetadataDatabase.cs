using NGTools;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NGToolsEditor.NGMissingScriptRecovery
{
	public static class MetadataDatabase
	{
		private static Dictionary<Type, TypeIdentifiers>	database;

		public static bool	Initialize()
		{
			if (MetadataDatabase.database == null)
			{
				try
				{
					MetadataDatabase.database = new Dictionary<Type, TypeIdentifiers>(1024);

					string[]	files = Directory.GetFiles(Application.dataPath + "/../Library/metadata", "*.info", SearchOption.AllDirectories);

					for (int i = 0; i < files.Length; i++)
						MetadataDatabase.Extract(files[i]);

					return true;
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException(ex);
				}

				return false;
			}

			return true;
		}

		public static TypeIdentifiers	GetIdentifiers(Type type)
		{
			MetadataDatabase.Initialize();

			TypeIdentifiers	identifiers;

			MetadataDatabase.database.TryGetValue(type, out identifiers);

			return identifiers;
		}

		private static void	Extract(string filepath)
		{
			using (FileStream fs = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (BufferedStream bs = new BufferedStream(fs))
			using (StreamReader sr = new StreamReader(bs))
			{
				string	line;

				while ((line = sr.ReadLine()) != null)
				{
					if (line.StartsWith("    guid:") == true)
					{
						try
						{
							// We are supposing all lines following guid is as follow:
							string	guid = line.Substring("    guid: ".Length);
							string	path = line = sr.ReadLine().Substring("    path: ".Length);

							line = sr.ReadLine();
							// Path can be on 2 lines.
							if (line.StartsWith("      ") == true)
							{
								path += line.Substring("     ".Length);
								line = sr.ReadLine();
							}

							string	localIdentifier = line.Substring("    localIdentifier: ".Length);
							string	thumbnailClassID = line = sr.ReadLine().Substring("    thumbnailClassID: ".Length);
							sr.ReadLine(); // Skip flags.
							string	scriptClassName = line = sr.ReadLine().Substring("    scriptClassName: ".Length);

							// Check if it is a script.
							if (thumbnailClassID == "115")
							{
								Type	type = Utility.GetType(scriptClassName);

								if (type != null && MetadataDatabase.database.ContainsKey(type) == false)
								{
									MetadataDatabase.database.Add(type, new TypeIdentifiers()
									{
										guid = guid,
										localIdentifier = localIdentifier,
										path = path,
									});
								}
							}
						}
						catch (Exception ex)
						{
							InternalNGDebug.LogException("Extraction failed on \"" + filepath + "\" at line \"" + line + "\".", ex);
						}
					}
				}
			}
		}
	}
}