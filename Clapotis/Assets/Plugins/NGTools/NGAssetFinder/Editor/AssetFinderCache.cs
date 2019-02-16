using NGTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NGToolsEditor.NGAssetFinder
{
	internal class AssetFinderCache : UnityEditor.AssetModificationProcessor
	{
		public const string	CachePath = "NGAssetFinderCache.txt";

		private static string[]	extensions = new string[] {
			".meta",
			".prefab",
			".mat",
			".anim",
			".shader",
			".controller",
			".asset",
			".unity",
			".rendertexture",
			".mixer",
			".prefs",
			".colors",
			".gradients",
			".curves",
			".curvesnormalized",
			".particlecurves",
			".particlecurvessigned",
			".particledoublecurves",
			".particledoublecurvessigned",
			".shadervariants",
			".flare",
			".overrideController",
			".mask",
			".guiskin",
			".fontsettings",
			".cubemap",
			".physicmaterial",
			".physicsmaterial2d",
			".giparams",
		};

		public static Dictionary<string, List<string>>	usages;
		private static List<string>						pendingFiles = new List<string>();
		private static Dictionary<string, string[]>		usagesFiles;
		private static List<string>						fileIDs = new List<string>(16);

		public static string	GetCachePath()
		{
			return Path.Combine(Application.persistentDataPath, Path.Combine(Constants.InternalPackageTitle, AssetFinderCache.CachePath));
		}

		public static void	SaveCache(string path)
		{
			if (AssetFinderCache.usagesFiles == null)
				return;

			try
			{
				double			time = EditorApplication.timeSinceStartup;
				StringBuilder	buffer = Utility.GetBuffer();
				int				n = 0;

				buffer.Capacity = AssetFinderCache.usagesFiles.Count * 156; // Approximation of what might be a close capacity.
				buffer.Append(AssetFinderCache.usagesFiles.Count);
				buffer.AppendLine();

				foreach (var pair in AssetFinderCache.usagesFiles)
				{
					if (File.Exists(pair.Key) == false)
						continue;

					buffer.AppendLine(pair.Key);
					buffer.Append(pair.Value.Length);
					buffer.AppendLine();

					for (int i = 0; i < pair.Value.Length; i++)
						buffer.AppendLine(pair.Value[i]);

					++n;
				}

				int	length = buffer.Length;
				int	capacity = buffer.Capacity;
				File.WriteAllText(path, Utility.ReturnBuffer(buffer));
				InternalNGDebug.VerboseLogFormat("[AssetFinderCache] Saved {0} entries in {1} seconds. (Buffer {2}/{3}/{4})", n, EditorApplication.timeSinceStartup - time, AssetFinderCache.usagesFiles.Count * 156, length, capacity);
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException("[AssetFinderCache] Saving cache failed.", ex);
			}
		}

		public static bool	LoadCache(string path)
		{
			if (File.Exists(path) == false)
				return false;

			try
			{
				double	time = EditorApplication.timeSinceStartup;

				using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
				using (BufferedStream bs = new BufferedStream(fs))
				using (StreamReader sr = new StreamReader(bs))
				{
					string	line;

					AssetFinderCache.usagesFiles = new Dictionary<string, string[]>(int.Parse(sr.ReadLine()));

					while ((line = sr.ReadLine()) != null)
					{
						string[]	IDs = new string[int.Parse(sr.ReadLine())];

						for (int i = 0; i < IDs.Length; i++)
							IDs[i] = sr.ReadLine();

						AssetFinderCache.usagesFiles.Add(line, IDs);
					}
				}

				InternalNGDebug.VerboseLogFormat("[AssetFinderCache] Loaded {0} entries in {1} seconds.", AssetFinderCache.usagesFiles.Count, EditorApplication.timeSinceStartup - time);
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException("[AssetFinderCache] Loading cache failed.", ex);
				return false;
			}

			return true;
		}

		public static void	ClearCache()
		{
			AssetFinderCache.usages = null;
			AssetFinderCache.usagesFiles = null;
			File.Delete(AssetFinderCache.GetCachePath());
		}

		public static void	CacheProjectReferences(bool showProgress = true)
		{
			AssetDatabase.SaveAssets();
			AssetFinderCache.RestorePendingFiles();

			if (AssetFinderCache.usages != null)
				return;

			try
			{
				if (showProgress == true)
					EditorUtility.DisplayProgressBar(NGAssetFinderWindow.Title + " - Project", "Loading cache from disk...", 0F);

				if (AssetFinderCache.LoadCache(AssetFinderCache.GetCachePath()) == false)
					AssetFinderCache.usagesFiles = new Dictionary<string, string[]>();

				AssetFinderCache.usages = new Dictionary<string, List<string>>((int)(AssetFinderCache.usagesFiles.Count * .375F)); // Approximation of an average used assets.

				EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

				if (showProgress == true)
					EditorUtility.DisplayProgressBar(NGAssetFinderWindow.Title + " - Project", "Fetching all asset paths...", 0F);

				double		time = EditorApplication.timeSinceStartup;
				string[]	assets = AssetDatabase.GetAllAssetPaths();
				int			cacheHits = 0;

				for (int i = 0; i < assets.Length; i++)
				{
					if ((assets[i].StartsWith("Assets/") == false &&
						 assets[i].StartsWith("ProjectSettings/") == false &&
						 assets[i].StartsWith("Library/") == false) ||
						Directory.Exists(assets[i]) == true)
					{
						continue;
					}

					if (showProgress == true && (i % 18) == 0)
					{
						string	progressBarTitle = NGAssetFinderWindow.Title + " - Project (" + (i + 1) + " / " + assets.Length + ")";
						string	progressBarContent = "Caching...";
						float	progressBarRate = (float)(i + 1) / (float)assets.Length;

						if (EditorUtility.DisplayCancelableProgressBar(progressBarTitle, progressBarContent, progressBarRate) == true)
							throw new BreakException();
					}

					string[]	IDs;
					long		ticks = -1;

					if (AssetFinderCache.usagesFiles.TryGetValue(assets[i], out IDs) == true)
					{
						ticks = File.GetLastWriteTime(assets[i]).Ticks;
						if (ticks == long.Parse(IDs[0]))
						{
							++cacheHits;

							for (int j = 1; j < IDs.Length; j++)
							{
								List<string>	cache;

								if (AssetFinderCache.usages.TryGetValue(IDs[j], out cache) == false)
								{
									cache = new List<string>();
									AssetFinderCache.usages.Add(IDs[j], cache);
								}

								if (cache.Contains(assets[i]) == false)
									cache.Add(assets[i]);
							}

							continue;
						}
						else
							AssetFinderCache.usagesFiles.Remove(assets[i]);
					}

					AssetFinderCache.ExtractReferences(assets[i], true, ticks);
				}

				InternalNGDebug.VerboseLogFormat("[AssetFinderCache] Constructed cache in {0} seconds. ({1} cache hits, {2} elements)", EditorApplication.timeSinceStartup - time, cacheHits, AssetFinderCache.usages.Count);
			}
			finally
			{
				if (showProgress == true)
					EditorUtility.ClearProgressBar();
			}
		}

		private static void	ExtractReferences(string file, bool add, long ticks = -1)
		{
			string	openedFile = file;
			int		i = 0;

			// Check if file has IDs in its meta or itself based on a hardcoded database.
			for (int l = file.Length, max = AssetFinderCache.extensions.Length; i < max; i++)
			{
				string	ext = AssetFinderCache.extensions[i];
				int		extLength = ext.Length;

				if (l > extLength)
				{
					int	j = 0;

					for (; j < extLength; j++)
					{
						if (ext[j] != file[l - extLength + j] && ext[j] != file[l - extLength + j] + ('a' - 'A'))
							break;
					}

					if (j == extLength)
						break;
				}
			}

			if (i == AssetFinderCache.extensions.Length)
			{
				using (FileStream stream = File.OpenRead(file))
				{
					byte[]	array = new byte[5];
					int		n = stream.Read(array, 0, 5);

					if (n == 5 &&
						array[0] == '%' &&
						array[1] == 'Y' &&
						array[2] == 'A' &&
						array[3] == 'M' &&
						array[4] == 'L')
					{
						InternalNGDebug.VerboseLogFormat("Extracting references from \"{0}\" on fallback.", file);
					}
					else
						openedFile += ".meta";
				}
			}

			if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) == true)
				file = file.Substring(0, file.Length - ".meta".Length);

			AssetFinderCache.fileIDs.Clear();

			if (add == true)
			{
				if (ticks == -1)
					ticks = File.GetLastWriteTime(file).Ticks;
				AssetFinderCache.fileIDs.Add(ticks.ToString());
			}
			else if (AssetFinderCache.usagesFiles.ContainsKey(file) == true)
				AssetFinderCache.usagesFiles.Remove(file);

			using (FileStream fs = File.Open(openedFile, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (BufferedStream bs = new BufferedStream(fs))
			using (StreamReader sr = new StreamReader(bs))
			{
				string	lastId = "                                ";
				string	line;

				while ((line = sr.ReadLine()) != null)
				{
					if (line.Length < 11 + 8 + 32 + 1) // {fileID: , guid: }
						continue;

					int	n = line.IndexOf("{fileID: ");
					if (n != -1)
					{
						int	m = line.IndexOf(", guid: ", n + "{fileID: ".Length + 2);
						if (m != -1)
						{
							int	j = 0;
							for (i = m + ", guid: ".Length; j < 32 && i < line.Length; ++i, ++j)
							{
								if (line[i] != lastId[j])
									break;
							}

							if (j == 32)
								continue;

							string			id = line.Substring(m + ", guid: ".Length, 32);
							List<string>	cache;

							if (add == true)
							{
								if (AssetFinderCache.fileIDs.Contains(id) == false)
									AssetFinderCache.fileIDs.Add(id);
								else
									continue;
							}

							if (AssetFinderCache.usages.TryGetValue(id, out cache) == false)
							{
								cache = new List<string>();
								AssetFinderCache.usages.Add(id, cache);
							}

							if (add == true)
							{
								if (cache.Contains(file) == false)
									cache.Add(file);
							}
							else
							{
								if (cache.Contains(file) == true)
									cache.Remove(file);
							}

							lastId = id;
						}
					}
				}
			}

			if (add == true)
				AssetFinderCache.usagesFiles.Add(file, AssetFinderCache.fileIDs.ToArray());
		}

		public static void		UpdateFile(string file)
		{
			if (AssetFinderCache.pendingFiles.Contains(file) == false)
			{
				AssetFinderCache.ExtractReferences(file, false);
				AssetFinderCache.pendingFiles.Add(file);
			}

			EditorApplication.delayCall += AssetFinderCache.RestorePendingFiles;
		}

		private static string[]		OnWillSaveAssets(string[] paths)
		{
			if (AssetFinderCache.usages == null)
				return paths;

			foreach (string file in paths)
			{
				AssetFinderCache.ExtractReferences(file, false);

				if (AssetFinderCache.pendingFiles.Contains(file) == false)
					AssetFinderCache.pendingFiles.Add(file);
			}

			EditorApplication.delayCall += AssetFinderCache.RestorePendingFiles;

			return paths;
		}

		private static void	RestorePendingFiles()
		{
			foreach (string file in AssetFinderCache.pendingFiles)
				AssetFinderCache.ExtractReferences(file, true);
			AssetFinderCache.pendingFiles.Clear();
		}
	}
}