using NGLicenses;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace NGToolsEditor
{
	using UnityEngine;

	internal static class FreeLicenseOverlay
	{
		private const string	KeyPrefs = "NGTools_Free_windows";
		private const string	LastTimeKeyPref = "NGTools_Free_lastTime";
		private const long		ResetAdsIntervalHours = 8L;

		private static EditorWindow	lastWindow;
		private static Rect			r;
		private static string		ads;

		private static Dictionary<Type, string>	extraAds = new Dictionary<Type, string>();

		private static string	closedWindows = null;
		private static string	ClosedWindows
		{
			get
			{
				if (FreeLicenseOverlay.closedWindows == null)
				{
					string	path = Path.Combine(Application.persistentDataPath, Path.Combine(Constants.InternalPackageTitle, Constants.PackageTitle));
					string	timeS = EditorPrefs.GetString(FreeLicenseOverlay.LastTimeKeyPref);

					long	time = 0;
					long	now = DateTime.Now.Ticks;

					if (string.IsNullOrEmpty(timeS) == false)
						long.TryParse(timeS, out time);

					// Two ways to check last time. Hehehe, go away hacker! :)
					if (File.Exists(path) == true)
					{
						long	time2;

						if (long.TryParse(File.ReadAllText(path), out time2) == true)
						{
							if (time2 > time)
								time = time2;
						}
					}

					if (now - time > FreeLicenseOverlay.ResetAdsIntervalHours * 3600L * 10000000L) // 6 hours
					{
						try
						{
							EditorPrefs.SetString(FreeLicenseOverlay.KeyPrefs, string.Empty);
							EditorPrefs.SetString(FreeLicenseOverlay.LastTimeKeyPref, now.ToString());
							File.WriteAllText(path, now.ToString());
						}
						catch
						{
						}
					}

					FreeLicenseOverlay.closedWindows = EditorPrefs.GetString(FreeLicenseOverlay.KeyPrefs, string.Empty);
				}

				return FreeLicenseOverlay.closedWindows;
			}
			set
			{
				FreeLicenseOverlay.closedWindows = value;
			}
		}

		public static void	Append(Type window, string ads)
		{
			string	a;

			if (FreeLicenseOverlay.extraAds.TryGetValue(window, out a) == false)
				FreeLicenseOverlay.extraAds.Add(window, ads);
			else
				FreeLicenseOverlay.extraAds[window] = a + ads;
		}

		public static void	First(EditorWindow window, string assetName, string ads)
		{
			if (NGLicensesManager.IsPro(assetName) == true)
				return;

			FreeLicenseOverlay.lastWindow = window;

			if (FreeLicenseOverlay.ClosedWindows.IndexOf(window.GetType().Name) != -1)
				return;

			string	extra;

			if (FreeLicenseOverlay.extraAds.TryGetValue(window.GetType(), out extra) == true)
				ads += extra;

			FreeLicenseOverlay.ads = ads;

			FreeLicenseOverlay.r = window.position;
			FreeLicenseOverlay.r.x = 0F;
			FreeLicenseOverlay.r.y = 0F;

			if (FreeLicenseOverlay.r.height > 100F)
			{
				Rect	r2 = FreeLicenseOverlay.r;

				r2.yMin = r2.yMax - 35F;
				r2.height = 30F;
				r2.xMin = r2.xMax - 120F;
				r2.x -= 5F;

				if (GUI.Button(r2, "Buy NG Tools Pro") == true)
					Help.BrowseURL("https://www.assetstore.unity3d.com/en/#!/content/34109");

				r2.x -= r2.width;

				if (GUI.Button(r2, "Activate Invoice") == true)
				{
					Utility.ShowPreferencesWindowAt(Constants.PackageTitle);
					Preferences.tab = Preferences.Tab.Licenses;
				}
			}

			if (Event.current.type == EventType.MouseDown)
			{
				FreeLicenseOverlay.closedWindows += window.GetType().Name;
				EditorPrefs.SetString(FreeLicenseOverlay.KeyPrefs, FreeLicenseOverlay.closedWindows);
				Event.current.Use();
			}
		}

		public static void	Last(string assetName)
		{
			if (NGLicensesManager.IsPro(assetName) == true)
				return;

			if (FreeLicenseOverlay.lastWindow == null || FreeLicenseOverlay.closedWindows.IndexOf(FreeLicenseOverlay.lastWindow.GetType().Name) != -1)
				return;

			if (Event.current.type == EventType.Repaint)
			{
				Utility.DropZone(FreeLicenseOverlay.r, FreeLicenseOverlay.ads);

				if (r.height > 100F)
				{
					Rect	r2 = FreeLicenseOverlay.r;

					r2.yMin = r2.yMax - 35F;
					r2.height = 30F;
					r2.xMin = r2.xMax - 120F;
					r2.x -= 5F;
					GUI.Button(r2, "Buy NG Tools Pro");
					r2.x -= r2.width;
					GUI.Button(r2, "Activate Invoice");
				}
			}
		}
	}
}