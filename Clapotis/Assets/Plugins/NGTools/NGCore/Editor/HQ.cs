using NGLicenses;
using NGTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor
{
	[InitializeOnLoad]
	public static class HQ
	{
		internal class ToolAssemblyInfo
		{
			public string	name;
			public string	version;
			public string	assetStoreBuyLink;
			public string	wikiURL;
			public bool		hidden;
		}

		public const string	NestedNGMenuItems = "ExternalNGMenuItems.cs";
		public const string	ServerEndPoint = "https://unityapi.ngtools.tech/";
		public const string	DeployingKeyPref = "NGTools_IsDeploying";
		public const string	AllowSendStatsKeyPref = "NGTools_AllowSendStats";

		private static string	rootPath;
		public static string	RootPath { get { return HQ.rootPath; } }

		private static string	rootedMenuFilePath;
		public static string	RootedMenuFilePath { get { return HQ.rootedMenuFilePath; } }

		public static event Action	SettingsChanged;

		public static bool	UsingEditorSettings { get { return NGSettings.sharedSettings == HQ.settings; } }

		private static bool			lazySettingsLoaded;
		private static NGSettings	settings;
		/// <summary>
		/// <para>Defines variables that you should use to have a coherent whole.</para>
		/// <para>WARNING! Settings can be null at almost any time, you must prevent doing anything when this case happens!</para>
		/// </summary>
		public static NGSettings	LastSettings { get; private set; }
		public static NGSettings	Settings
		{
			get
			{
				if (HQ.lazySettingsLoaded == false)
				{
					HQ.lazySettingsLoaded = true;

					string	path = NGEditorPrefs.GetString(Constants.ConfigPathKeyPref, null, true);

					NGDiagnostic.Log(Preferences.Title, "ConfigPath", path);
					if (string.IsNullOrEmpty(path) == false)
					{
						HQ.settings = AssetDatabase.LoadAssetAtPath(path, typeof(NGSettings)) as NGSettings;
						NGDiagnostic.Log(Preferences.Title, "IsConfigPathValid", HQ.settings != null);
						InternalNGDebug.Assert(HQ.settings != null, "NG Settings is null at \"" + path + "\".");

						if (HQ.SettingsChanged != null)
							HQ.SettingsChanged();
					}
					else
						HQ.LoadSharedNGSetting();
				}

				return HQ.settings;
			}
		}

		private static List<ToolAssemblyInfo>			toolsAssemblyInfo;
		internal static IEnumerable<ToolAssemblyInfo>	EachTool
		{
			get
			{
				if (HQ.toolsAssemblyInfo == null)
				{
					HQ.toolsAssemblyInfo = new List<ToolAssemblyInfo>(22);

					foreach (Type type in Utility.EachNGTSubClassesOf(typeof(object), t => t.Name == "NGAssemblyInfo"))
					{
						FieldInfo	name = type.GetField("Name");
						FieldInfo	version = type.GetField("Version");
						FieldInfo	assetStoreBuyLink = type.GetField("AssetStoreBuyLink");
						FieldInfo	wikiURL = type.GetField("WikiURL");
						FieldInfo	changeLog = type.GetField("ChangeLog");

						if (name != null && version != null && assetStoreBuyLink != null && wikiURL != null &&
							name.FieldType == typeof(string) &&
							version.FieldType == typeof(string) &&
							assetStoreBuyLink.FieldType == typeof(string) &&
							wikiURL.FieldType == typeof(string))
						{
							ToolAssemblyInfo	tai = new ToolAssemblyInfo()
							{
								name = (string)name.GetRawConstantValue(),
								version = (string)version.GetRawConstantValue(),
								assetStoreBuyLink = (string)assetStoreBuyLink.GetRawConstantValue(),
								wikiURL = (string)wikiURL.GetRawConstantValue(),
								hidden = type.GetField("Hidden") != null,
							};

							HQ.toolsAssemblyInfo.Add(tai);

							if (changeLog != null && changeLog.IsStatic == true && changeLog.FieldType == typeof(string[]))
							{
								string[]	changeLogContent = changeLog.GetValue(null) as string[];

								for (int i = 0; i + 2 < changeLogContent.Length; i += 3)
									NGChangeLogWindow.AddChangeLog(tai.name, changeLogContent[i], changeLogContent[i + 1], changeLogContent[i + 2]);
							}
						}
						else
							InternalNGDebug.LogError(type.FullName + " from \"" + type.Module + "\" does not respect the format.");
					}

					HQ.toolsAssemblyInfo.Sort((a, b) => a.name.CompareTo(b.name));
				}

				for (int i = 0; i < HQ.toolsAssemblyInfo.Count; i++)
				{
					if (Conf.DebugMode == Conf.DebugState.None && HQ.toolsAssemblyInfo[i].name == "NG Licenses")
						continue;

					yield return HQ.toolsAssemblyInfo[i];
				}
			}
		}

		private static int		cachedMacAddressHash = -1;
		private static string	nestedMenuFailedOnce;

		static	HQ()
		{
			HQ.rootPath = Utility.GetPackagePath();
			NGDiagnostic.Log(Preferences.Title, "RootPath", HQ.RootPath);

			if (HQ.RootPath == string.Empty)
			{
				InternalNGDebug.LogWarning(Constants.RootFolderName + " folder was not found.");
				return;
			}

			HQ.SettingsChanged += HQ.CheckSettingsVersion;

			string[]	files = Directory.GetFiles(HQ.RootPath, HQ.NestedNGMenuItems, SearchOption.AllDirectories);
			if (files.Length == 1)
				HQ.rootedMenuFilePath = files[0];

			NGLicensesManager.LicensesLoaded += () =>
			{
				NGDiagnostic.Log(Preferences.Title, "AllowSendStats", NGEditorPrefs.GetBool(HQ.AllowSendStatsKeyPref, true));
				if (NGEditorPrefs.GetBool(HQ.AllowSendStatsKeyPref, true) == true)
					HQ.SendStats();
			};
			NGLicensesManager.ActivationSucceeded += (invoice) =>
			{
				string	path = Path.Combine(Application.persistentDataPath, Path.Combine(Constants.InternalPackageTitle, "sendStats." + Utility.UnityVersion + "." + Constants.Version + ".txt"));

				if (File.Exists(path) == true)
					File.Delete(path);
			};

			//Conf.DebugMode = (Conf.DebugState)EditorPrefs.GetInt(Conf.DebugModeKeyPref, (int)Conf.DebugMode);
			Utility.SafeDelayCall(() =>
			{
				NGLicensesManager.Title = Constants.PackageTitle;
				NGLicensesManager.IntermediatePath = Constants.InternalPackageTitle;

				NGDiagnostic.Log(Preferences.Title, "LogPath", InternalNGDebug.LogPath);
			});

			NGDiagnostic.Log(Preferences.Title, "DebugMode", Conf.DebugMode);

			// TODO Unity <5.6 backward compatibility?
			MethodInfo	ResetAssetsMethod = typeof(HQ).GetMethod("ResetAssets", BindingFlags.Static | BindingFlags.NonPublic);

			try
			{
				EventInfo	projectChangedEvent = typeof(EditorApplication).GetEvent("projectChanged");
				projectChangedEvent.AddEventHandler(null, Delegate.CreateDelegate(projectChangedEvent.EventHandlerType, null, ResetAssetsMethod));
				//EditorApplication.projectChanged += HQ.ResetAssets;
			}
			catch
			{
				FieldInfo	projectWindowChangedField = UnityAssemblyVerifier.TryGetField(typeof(EditorApplication), "projectWindowChanged", BindingFlags.Static | BindingFlags.Public);
				if (projectWindowChangedField != null)
					projectWindowChangedField.SetValue(null, Delegate.Combine((Delegate)projectWindowChangedField.GetValue(null), Delegate.CreateDelegate(projectWindowChangedField.FieldType, null, ResetAssetsMethod)));
				//EditorApplication.projectWindowChanged += HQ.ResetAssets;
			}

			EditorApplication.projectWindowItemOnGUI += ProjectCopyAssets.OnProjectElementGUI;
		}

		public static void	InitSetup()
		{
			EditorUtility.DisplayDialog(Constants.PackageTitle, "This feature has been deprecated. Please update the menu.", "OK");
		}

		public static void	SetSettings(NGSettings settings)
		{
			HQ.LastSettings = HQ.settings;
			HQ.settings = settings;

			if (HQ.SettingsChanged != null)
				HQ.SettingsChanged();
		}

		public static void	CreateNGSettings(string path)
		{
			try
			{
				NGSettings	asset = ScriptableObject.CreateInstance<NGSettings>();

				AssetDatabase.CreateAsset(asset, path);
				AssetDatabase.SaveAssets();

				NGEditorPrefs.SetString(Constants.ConfigPathKeyPref, path, true);

				HQ.SetSettings(asset);

				// Need to skip many frames before really writing the data. Don't know why it requires 2 frames.
				EditorApplication.delayCall += () =>
				{
					EditorApplication.delayCall += () =>
					{
						HQ.InvalidateSettings();
						AssetDatabase.SaveAssets();
					};
				};
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException(ex);
				HQ.SetSettings(null);
			}
		}

		public static void		InvalidateSettings(NGSettings settings = null, bool directSave = false)
		{
			if (settings == null)
				settings = HQ.settings;

			if ((settings.hideFlags & HideFlags.DontSave) == HideFlags.DontSave)
				HQ.SaveSharedNGSettings(settings, directSave);
			else
				EditorUtility.SetDirty(settings);
		}

		internal static void	LoadSharedNGSetting(bool skipLoad = false)
		{
			if (skipLoad == false && NGSettings.sharedSettings != null)
			{
				HQ.SetSettings(NGSettings.sharedSettings);
				return;
			}

			NGSettings	asset = null;

			if (skipLoad == false)
			{
				asset = NGSettings.LoadSharedSettings();
				if (asset != null)
					asset.hideFlags = HideFlags.DontSave;
			}

			if (skipLoad == true || asset == null)
			{
				asset = NGSettings.CreateSharedSettings();
				asset.hideFlags = HideFlags.DontSave;

				Directory.CreateDirectory(Path.GetDirectoryName(NGSettings.GetSharedSettingsPath()));
				HQ.SaveSharedNGSettings(asset);
			}
			else
				NGSettings.sharedSettings = asset;

			HQ.SetSettings(asset);
		}

		private static void	SaveSharedNGSettings(NGSettings settings = null, bool directSave = false)
		{
			if (settings == null)
				settings = HQ.settings;

			if (directSave == false)
				Utility.RegisterIntervalCallback(HQ.WriteToDisk, 200, 1);
			else
				HQ.WriteToDisk();
		}

		private static void	WriteToDisk()
		{
			try
			{
				InternalNGDebug.InternalLog("Writing shared settings to disk.");
				NGSettings.sharedSettings.SaveSharedSettings();
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException("An error occurred when " + Constants.PackageTitle + " tried to write data into \"" + NGSettings.GetSharedSettingsPath() + "\".", ex);
			}
		}

		private static void	CheckSettingsVersion()
		{
			if (HQ.settings != null && HQ.settings.version != NGSettings.Version)
			{
				EditorApplication.delayCall += () =>
				{
					if (EditorUtility.DisplayDialog(Constants.PackageTitle, string.Format(LC.G("Preferences_AskResetSettings"), HQ.settings.version, NGSettings.Version), LC.G("Yes"), LC.G("No")) == true)
					{
						GUICallbackWindow.Open(() =>
						{
							SerializedObject	obj = new SerializedObject(HQ.settings);
							NGSettings			newSettings = ScriptableObject.CreateInstance<NGSettings>();

							newSettings.hideFlags = HQ.settings.hideFlags;

							if (NGSettings.sharedSettings == HQ.settings)
							{
								File.Delete(NGSettings.GetSharedSettingsPath());
								NGSettings.sharedSettings = newSettings;
							}

							SerializedObject	newObject = new SerializedObject(newSettings);
							SerializedProperty	it = obj.GetIterator();

							it.Next(true);

							SerializedProperty	end = it.GetEndProperty();

							while (SerializedProperty.EqualContents(it, end) == false && it.Next(true) == true)
								newObject.CopyFromSerializedProperty(it);

							newObject.ApplyModifiedProperties();

							string	path = AssetDatabase.GetAssetPath(HQ.settings.GetInstanceID());

							if (string.IsNullOrEmpty(path) == false)
								AssetDatabase.CreateAsset(newSettings, path);

							HQ.settings = newSettings;

							if (HQ.SettingsChanged != null)
								HQ.SettingsChanged();
						});
					}
				};
			}
		}

		private static void	ResetAssets()
		{
			if (HQ.settings == null)
			{
				NGEditorPrefs.DeleteKey(Constants.ConfigPathKeyPref, true);
				GUICallbackWindow.Open(() => HQ.LoadSharedNGSetting());
			}
		}

		public static void	Invoke(string name, Type type, string staticMethod)
		{
			if (type != null)
			{
				MethodInfo	method = type.GetMethod(staticMethod, BindingFlags.Public | BindingFlags.Static);

				if (method != null)
				{
					method.Invoke(null, null);
					return;
				}
			}

			if (HQ.nestedMenuFailedOnce == name)
			{
				Utility.ShowPreferencesWindowAt(Constants.PreferenceTitle);
				Preferences.tab = Preferences.Tab.Preferences;
				XGUIHighlightManager.Highlight(Preferences.Title + ".RootedMenu");
			}
			else
			{
				HQ.nestedMenuFailedOnce = name;
				InternalNGDebug.LogWarning("Calling MenuItem \"" + name + "\" has failed. Please regenerate rooted menu. (Go to Edit/Preferences/NG Tools or recall this menu)");
			}
		}

		internal static void	SetNestedMode(object value)
		{
			if (EditorApplication.isPlaying == true)
			{
				EditorUtility.DisplayDialog(Constants.PackageTitle, LC.G("Preferences_WarningPlayMode"), LC.G("Ok"));
				return;
			}

			if ((bool)value == true)
			{
				StringBuilder	buffer = Utility.GetBuffer();

				buffer.Append(@"// File auto-generated by " + Constants.PackageTitle + @".
using UnityEditor;

namespace NGToolsEditor
{
	internal static class ExternalNGMenuItems
	{
		public const string	MenuItemPath = Constants.PackageTitle + ""/"";");
				int	p = buffer.Length;

				buffer.Append(@"
		");

				Type	externalNGMenuItems = Type.GetType("NGToolsEditor.ExternalNGMenuItems");

				foreach (Type t in Utility.EachAllSubClassesOf(typeof(object),
															   (Assembly a) => a.FullName.StartsWith("NG") || a.FullName.StartsWith("Assembly-"),
															   (Type t) => t != null && t != externalNGMenuItems && t.Namespace != null && t.Namespace.StartsWith("NG")))
				{
					foreach (MethodInfo m in t.GetMethods(BindingFlags.Static | BindingFlags.Public))
					{
						if (m.IsDefined(typeof(MenuItem), false) == true)
						{
							MenuItem[]	attributes = m.GetCustomAttributes(typeof(MenuItem), false) as MenuItem[];

							for (int i = 0; i < attributes.Length; i++)
							{
								if (attributes[i].menuItem.StartsWith(Constants.MenuItemPath) == false ||
									attributes[i].menuItem.Contains(NGHotkeys.SubMenuItemPath) == true)
								{
									continue;
								}

								string	label = '"' + attributes[i].menuItem.Substring(Constants.MenuItemPath.Length) + '"';

								buffer.Append(@"

		[MenuItem(ExternalNGMenuItems.MenuItemPath + " + label + ", priority = " + attributes[i].priority + @")]
		public static void	" + t.Name + m.Name + @"()
		{
			HQ.Invoke(ExternalNGMenuItems.MenuItemPath + " + label + ", Utility.GetType(\"" + t.Namespace + "\", \"" + t.Name + "\"), \"" + m.Name + @""");
		}");
							}
						}
					}
				}

				buffer.Append(@"
	}
}");
				if (Directory.CreateDirectory(HQ.RootPath + "/Editor") != null)
				{
					File.WriteAllText(HQ.RootPath + "/Editor/" + HQ.NestedNGMenuItems, Utility.ReturnBuffer(buffer));
					Utility.RecompileUnityEditor();
				}
			}
			else
			{
				string[]	files = Directory.GetFiles(HQ.RootPath, HQ.NestedNGMenuItems, SearchOption.AllDirectories);

				if (files.Length == 1)
				{
					AssetDatabase.MoveAssetToTrash(files[0]);

					string	parentFolder = Path.GetDirectoryName(files[0]);

					if (Directory.GetFiles(parentFolder).Length == 0 &&
						Directory.GetDirectories(parentFolder).Length == 0)
					{
						Directory.Delete(parentFolder);
					}
				}
			}
		}

		internal static void	SetSendStats(object sendStats)
		{
			if ((bool)sendStats == false)
			{
				if (EditorUtility.DisplayDialog(Constants.PackageTitle, "For those who might want to know why I send stats.\nI need some info about Unity Editor usage, especially because Unity does not provide them.\nIn order to keep supporting legacy versions, unused tools or platforms such as Unity 4, Mac or else.\n\nDo you confirm not sending stats?", "Yes", "No") == false)
					return;

				HQ.SendStats(false);
			}

			NGEditorPrefs.SetBool(HQ.AllowSendStatsKeyPref, (bool)sendStats);
		}

		/// <summary>
		/// For the curious who might want to know why I send these stats.
		/// I need some info about Unity Editor usage, especially because Unity does not provide them.
		/// In order to keep supporting old versions or platforms.
		/// </summary>
		/// <param name="sendStats"></param>
		internal static void	SendStats(bool sendStats = true)
		{
			string	path = Path.Combine(Application.persistentDataPath, Path.Combine(Constants.InternalPackageTitle, "sendStats." + Utility.UnityVersion + "." + Constants.Version + ".txt"));
			bool	sentOnce = false;
			string	today = DateTime.Now.ToString("yyyyMMdd");

			if (File.Exists(path) == true)
			{
				string	lastTime = File.ReadAllText(path);

				if (lastTime == today)
					sentOnce = true;
			}

			if (sentOnce == false)
			{
				try
				{
					Directory.CreateDirectory(Path.GetDirectoryName(path));
					File.WriteAllText(path, today);
				}
				catch
				{
					string	rawSentOnceCount = HQ.GetStatsComplementary("SSSOC");
					int		errorCount;

					if (string.IsNullOrEmpty(rawSentOnceCount) == false && int.TryParse(rawSentOnceCount, out errorCount) == true)
						HQ.SetStatsComplementary("SSSOC", (errorCount + 1).ToString());
					else
						HQ.SetStatsComplementary("SSSOC", "1");
				}
			}

			if (sentOnce == false || sendStats == false)
			{
				StringBuilder	buffer = Utility.GetBuffer(HQ.ServerEndPoint + "unityeditor.php?u=");

				buffer.Append(Utility.UnityVersion);
				buffer.Append("&o=");
				buffer.Append(SystemInfo.operatingSystem);
				buffer.Append("&p=");
				buffer.Append(Constants.Version);
				buffer.Append("&n=");
				buffer.Append(SystemInfo.deviceName);
				buffer.Append("&un=");
				buffer.Append(Environment.UserName);
				buffer.Append("&ut=");
				buffer.Append(Metrics.GetUsedTools());
				Metrics.ResetUsedTools();

				buffer.Append("&m=");
				buffer.Append(HQ.GetMACAddressHash());

				foreach (License license in NGLicensesManager.EachInvoices())
				{
					if (license.active == true && license.status != Status.Banned)
					{
						buffer.Append("&in[]=");
						buffer.Append(license.invoice);
					}
				}

				foreach (HQ.ToolAssemblyInfo tool in HQ.EachTool)
				{
					buffer.Append("&to[]=");
					buffer.Append(tool.name);
					buffer.Append(":");
					buffer.Append(tool.version);
				}

				string	complementary = NGEditorPrefs.GetString(HQ.ComplementaryKeyPref);
				if (string.IsNullOrEmpty(complementary) == false)
					buffer.Append("&com=" + complementary);

				if (sendStats == false)
					buffer.Append("&s");

				Utility.RequestURL(Utility.ReturnBuffer(buffer), (s, r) =>
				{
					if (s == Utility.RequestStatus.Completed)
						NGEditorPrefs.DeleteKey(HQ.ComplementaryKeyPref);
					else
					{
						string	rawErrorCount = HQ.GetStatsComplementary("SSEC");
						int		errorCount;

						if (string.IsNullOrEmpty(rawErrorCount) == false && int.TryParse(rawErrorCount, out errorCount) == true)
							HQ.SetStatsComplementary("SSEC", (errorCount + 1).ToString());
						else
							HQ.SetStatsComplementary("SSEC", "1");
					}
				});
			}
		}

		private const string	ComplementaryKeyPref = "NGTools_Complementary";
		private const char		ComplementarySeparator = ';';

		internal static void	SetStatsComplementary(string key, string value)
		{
			string			complementary = NGEditorPrefs.GetString(HQ.ComplementaryKeyPref);
			List<string>	data = new List<string>();
			bool			found = false;

			if (string.IsNullOrEmpty(complementary) == false)
				data.AddRange(complementary.Split(HQ.ComplementarySeparator));

			for (int i = 0; i < data.Count; i++)
			{
				if (data[i].StartsWith(key + '=') == true)
				{
					found = true;
					if (string.IsNullOrEmpty(value) == true)
						data.RemoveAt(i--);
					else
						data[i] = key + '=' + value;
				}
			}

			if (found == false)
				data.Add(key + '=' + value);

			NGEditorPrefs.SetString(HQ.ComplementaryKeyPref, string.Join(HQ.ComplementarySeparator.ToString(), data.ToArray()));
		}

		internal static string	GetStatsComplementary(string key)
		{
			string		complementary = NGEditorPrefs.GetString(HQ.ComplementaryKeyPref);
			string[]	data = complementary.Split(HQ.ComplementarySeparator);

			for (int i = 0; i < data.Length; i++)
			{
				if (data[i].StartsWith(key + '=') == true)
					return data[i].Substring(key.Length + 1);
			}

			return string.Empty;
		}

		internal static int	GetMACAddressHash()
		{
			if (HQ.cachedMacAddressHash != -1)
				return HQ.cachedMacAddressHash;

			bool	skipTunnelAndLoopback = false;

			HQ.cachedMacAddressHash = 0;

			while (true)
			{
				List<NetworkInterface>	networks = new List<NetworkInterface>(NetworkInterface.GetAllNetworkInterfaces());

				for (int i = 0; i < networks.Count; i++)
				{
					if (networks[i].OperationalStatus != OperationalStatus.Up ||
						(skipTunnelAndLoopback == false &&
						 (networks[i].NetworkInterfaceType == NetworkInterfaceType.Loopback ||
						  networks[i].NetworkInterfaceType == NetworkInterfaceType.Tunnel)))
					{
						networks.RemoveAt(i);
						--i;
						continue;
					}

					byte[]	macAddress = networks[i].GetPhysicalAddress().GetAddressBytes();
					int		j = 0;

					for (; j < macAddress.Length; j++)
					{
						if (macAddress[j] == 0)
							break;
					}

					if (macAddress.Length == 0 || j < macAddress.Length)
					{
						networks.RemoveAt(i);
						--i;
						continue;
					}
				}

				if (networks.Count >= 1)
				{
					HQ.cachedMacAddressHash = networks[0].GetPhysicalAddress().ToString().GetHashCode();
					break;
				}

				if (skipTunnelAndLoopback == false)
					skipTunnelAndLoopback = true;
				else
					break;
			}

			return HQ.cachedMacAddressHash;
		}
	}
}