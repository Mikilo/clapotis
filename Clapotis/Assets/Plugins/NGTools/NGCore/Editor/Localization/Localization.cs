using NGTools;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NGToolsEditor
{
	public static class Localization
	{
		public static Dictionary<string, string>	defaultLocale = new Dictionary<string, string>();
		public static Dictionary<string, string>	locale = new Dictionary<string, string>();
		public static EmbedLocale[]					embedLocales;

		static	Localization()
		{
			// TODO Enable this line the day there will be more than 1 language.
			//Localization.embedLocales = Utility.CreateAllInstancesOf<EmbedLocale>();
			Localization.embedLocales = new EmbedLocale[] { new Language_english() };
			Localization.LoadLanguage(Constants.DefaultLanguage, Localization.defaultLocale);

			string	currentLanguage = Localization.CurrentLanguage();

			if (string.IsNullOrEmpty(currentLanguage) == false)
			{
				if (Localization.LoadLanguage(currentLanguage) == false)
					Localization.SaveLanguage(Constants.DefaultLanguage);
			}
			else
				Localization.SaveLanguage(Constants.DefaultLanguage);
		}

		/// <summary>
		/// <para>Fetches the content associated to the given <paramref name="key"/>.</para>
		/// <para>Returns the content from the default language when not found.</para>
		/// <para>If still missing from the default language, returns a debug string.</para>
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static string	Get(string key)
		{
			string	content;

			if (Localization.locale.TryGetValue(key, out content) == true ||
				Localization.defaultLocale.TryGetValue(key, out content) == true)
			{
				return content;
			}

			return "LC[" + Localization.CurrentLanguage() + "][" + key + "]";
		}

		public static string	CurrentLanguage()
		{
			return NGEditorPrefs.GetString(Constants.LanguageEditorPref);
		}

		public static void		SaveLanguage(string language)
		{
			NGEditorPrefs.SetString(Constants.LanguageEditorPref, language);
		}

		/// <summary>
		/// </summary>
		/// <param name="culture"></param>
		/// <param name="refLocale"></param>
		/// <param name="replaceValueByFile">Only used for debug purpose.</param>
		public static bool	LoadLanguage(string culture, Dictionary<string, string> refLocale = null, bool replaceValueByFile = false)
		{
			if (refLocale == null)
				refLocale = Localization.locale;

			if (HQ.RootPath == string.Empty)
			{
				Debug.LogWarning(Constants.RootFolderName + " folder was not found.");
				return false;
			}

			try
			{
				refLocale.Clear();

				string	localFolder = Path.Combine(HQ.RootPath, Constants.RelativeLocaleFolder + culture);
				if (Directory.Exists(localFolder) == true)
				{
					string[]	files = Directory.GetFiles(localFolder, "*.txt", SearchOption.AllDirectories);

					for (int i = 0; i < files.Length; i++)
					{
						using (FileStream fs = File.Open(files[i], FileMode.Open, FileAccess.Read, FileShare.Read))
						using (BufferedStream bs = new BufferedStream(fs))
						using (StreamReader sr = new StreamReader(bs))
						{
							string	line;
							int		j = 0;

							while ((line = sr.ReadLine()) != null)
							{
								// Check comment line.
								if (line.StartsWith("//") ||
									line.Length <= 2)
								{
									continue;
								}

								int	separatorPosition = line.IndexOf('=');

								if (separatorPosition == -1)
								{
									Debug.LogWarning("No separator \"=\" found at line " + (++j).ToCachedString() + " in file \"" + files[i] + "\".");
									continue;
								}

								string	key = line.Substring(0, separatorPosition);
								string	value = line.Substring(separatorPosition + 1);

								if (refLocale.ContainsKey(key) == false)
								{
									if (replaceValueByFile == true)
										refLocale.Add(key, files[i]);
									else
										refLocale.Add(key, value.Replace(@"\n", "\n"));
								}
							}
						}
					}
				}

				for (int i = 0; i < Localization.embedLocales.Length; i++)
				{
					if (Localization.embedLocales[i].language == Utility.NicifyVariableName(culture))
					{
						foreach (var item in Localization.embedLocales[i].pairs)
						{
							if (refLocale.ContainsKey(item.Key) == false)
								refLocale.Add(item.Key, item.Value);
						}
					}
				}

				return refLocale.Count > 0;
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException(Constants.PackageTitle + " failed to load language \"" + culture + "\".", ex);
			}

			return false;
		}
	}
}