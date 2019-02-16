using NGTools;
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;

namespace NGToolsEditor
{
	using UnityEngine;

	public class NGSettings : ScriptableObject
	{
		public const int	Version = 2;

		private static readonly Type	fallbackEditorWindowType = UnityAssemblyVerifier.TryGetType(typeof(Editor).Assembly, "UnityEditor.FallbackEditorWindow");

		public static event Action<NGSettings>			Initialize;
		private static bool								lazySettingsCallbacksLoaded;
		/// <summary>Is triggered whenever a new setting is created. GUI context is not guaranteed, check it with Utility.CheckOnGUI().</summary>
		public static event Action<ScriptableObject>	SettingsGenerated;

		public static NGSettings	sharedSettings;

		[HideInInspector]
		public int	version =  NGSettings.Version;

		[HideInInspector, NonSerialized]
		private DynamicOrderedArray<Object>	subAssets;
		[HideInInspector, NonSerialized]
		private ScriptableObject			lastSubAsset;

		[HideInInspector, SerializeField]
		private bool	once;

		protected virtual void	OnEnable()
		{
			if (this.once == false && NGSettings.Initialize != null)
			{
				this.once = true;

				GUICallbackWindow.Open(() => NGSettings.Initialize(this));
			}
		}

		public T		Get<T>() where T : ScriptableObject
		{
			if (this.lastSubAsset != null && this.lastSubAsset is T)
				return this.lastSubAsset as T;

			return this.Load(typeof(T)) as T;
		}

		public object	Get(Type type)
		{
			if (this.lastSubAsset != null && this.lastSubAsset.GetType() == type)
				return this.lastSubAsset;

			return this.Load(type);
		}

		private ScriptableObject	Load(Type type)
		{
			if (this.subAssets == null || this.subAssets.array == null)
			{
				if (HQ.UsingEditorSettings == true)
				{
					NGSettings.CheckProjectCredentialsSettings();
					this.subAssets = new DynamicOrderedArray<Object>(InternalEditorUtility.LoadSerializedFileAndForget(NGSettings.GetSharedSettingsPath()));
				}
				else
					this.subAssets = new DynamicOrderedArray<Object>(AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(HQ.Settings)));

				this.CheckAssets();
			}

			for (int i = 0; i < this.subAssets.array.Length; i++)
			{
				if (this.subAssets.array[i].GetType() == type)
				{
					this.lastSubAsset = this.subAssets.array[i] as ScriptableObject;
					this.subAssets.BringToTop(i);
					return this.lastSubAsset;
				}
			}

			ScriptableObject	newInstance = ScriptableObject.CreateInstance(type);

			newInstance.name = type.FullName;
			newInstance.hideFlags = HideFlags.DontSave;

			Object[]	newSubAssets;

			if (this.subAssets.array.Length == 0)
				newSubAssets = new Object[] { this, newInstance };
			else
			{
				newSubAssets = new Object[this.subAssets.array.Length + 1];

				Array.Copy(this.subAssets.array, newSubAssets, this.subAssets.array.Length);
				newSubAssets[this.subAssets.array.Length] = newInstance;
			}

			this.subAssets.array = newSubAssets;
			this.lastSubAsset = newInstance;

			if (HQ.UsingEditorSettings == true)
				this.SaveSharedSettings();
			else
			{
				// Unity 2017 does not like DontSave flag.
				newInstance.hideFlags = HideFlags.None;
				AssetDatabase.AddObjectToAsset(newInstance, HQ.Settings);
				newInstance.hideFlags = HideFlags.DontSave;

				// This is to prevent "Assertion failed on expression: '!m_IsGettingEntries'". Because NG Console can request its settings in SyncLogs, which is getting entries.
				EditorApplication.delayCall -= AssetDatabase.SaveAssets;
				EditorApplication.delayCall += AssetDatabase.SaveAssets;
			}

			if (NGSettings.lazySettingsCallbacksLoaded == false)
			{
				NGSettings.lazySettingsCallbacksLoaded = true;

				foreach (Type t in Utility.EachNGTSubClassesOf(typeof(object)))
				{
					MethodInfo[]	methods = t.GetMethods(BindingFlags.NonPublic | BindingFlags.Static);

					for (int i = 0; i < methods.Length; i++)
					{
						if (methods[i].IsDefined(typeof(NGSettingsChangedAttribute), false) == true)
						{
							NGSettings.SettingsGenerated += Delegate.CreateDelegate(typeof(Action<ScriptableObject>), null, methods[i]) as Action<ScriptableObject>;
							break;
						}
					}
				}
			}

			if (NGSettings.SettingsGenerated != null)
				NGSettings.SettingsGenerated(newInstance);

			return newInstance;
		}

		public static string	GetSharedSettingsPath()
		{
			return Path.Combine(Application.persistentDataPath, Path.Combine(Constants.InternalPackageTitle, Constants.SettingsFilename));
		}

		public static NGSettings	CreateSharedSettings()
		{
			if (NGSettings.sharedSettings != null)
				Object.DestroyImmediate(NGSettings.sharedSettings, true);
			NGSettings.sharedSettings = ScriptableObject.CreateInstance<NGSettings>();
			NGSettings.sharedSettings.name = typeof(NGSettings).FullName;
			NGSettings.sharedSettings.subAssets = new DynamicOrderedArray<Object>(new Object[0]);
			return NGSettings.sharedSettings;
		}

		public static NGSettings	LoadSharedSettings()
		{
			// Delete all previous instances of shared NGSEttings.
			foreach (NGSettings item in Resources.FindObjectsOfTypeAll<NGSettings>())
			{
				if (item.name == typeof(NGSettings).FullName)
					Object.DestroyImmediate(item, true);
			}

			NGSettings.CheckProjectCredentialsSettings();

			Object[]	assets = InternalEditorUtility.LoadSerializedFileAndForget(NGSettings.GetSharedSettingsPath());

			for (int i = 0; i < assets.Length; i++)
			{
				if (assets[i] is NGSettings)
				{
					NGSettings	settings = assets[i] as NGSettings;

					settings.subAssets = new DynamicOrderedArray<Object>(assets);
					settings.CheckAssets();
					settings.name = typeof(NGSettings).FullName;
					return settings;
				}
			}

			return null;
		}

		public void		SaveSharedSettings()
		{
			string	folder = Path.GetDirectoryName(NGSettings.GetSharedSettingsPath());

			if (Directory.Exists(folder) == false)
				Directory.CreateDirectory(folder);

			InternalEditorUtility.SaveToSerializedFileAndForget(this.subAssets.array, NGSettings.GetSharedSettingsPath(), true);
		}

		public void		Clear(Type type)
		{
			this.lastSubAsset = null;

			for (int i = 0; i < this.subAssets.array.Length; i++)
			{
				if (this.subAssets.array[i].GetType() == type)
				{
					Object.DestroyImmediate(this.subAssets.array[i], true);

					Object[]	newAssets = new Object[this.subAssets.array.Length - 1];

					for (int j = 0; j < i; j++)
						newAssets[j] = this.subAssets.array[j];

					for (int j = i + 1; j < this.subAssets.array.Length; j++)
						newAssets[j - 1] = this.subAssets.array[j];

					this.subAssets.array = newAssets;
					break;
				}
			}
		}

		private void	CheckAssets()
		{
			int	countNullAssets = 0;

			for (int i = 0; i < this.subAssets.array.Length; i++)
			{
				if (this.subAssets.array[i] == null ||
					this.subAssets.array[i].GetType() == NGSettings.fallbackEditorWindowType)
				{
					this.subAssets.array[i] = null;
					++countNullAssets;
				}
			}

			if (countNullAssets > 0)
			{
				Object[]	newAssets = new Object[this.subAssets.array.Length - countNullAssets];

				for (int i = 0, j = 0; j < this.subAssets.array.Length; ++i, ++j)
				{
					while (j < this.subAssets.array.Length && this.subAssets.array[j] == null)
						++j;

					if (j < this.subAssets.array.Length)
						newAssets[i] = this.subAssets.array[j];
				}

				this.subAssets.array = newAssets;

				InternalNGDebug.InternalLog("NGSettings has detected corrupted data and has been cleaned.");
			}
		}

		/// <summary>
		/// Verifies if the current project credentials (Company Name & Product Name) are using the same previous NGSettings.
		/// If not, restore the previous NGSettings to the new location.
		/// </summary>
		private static void	CheckProjectCredentialsSettings()
		{
			string	path = NGSettings.GetSharedSettingsPath();
			string	lastPath = NGSettings.GetProjectCredentials();
			string	start = PlayerSettings.companyName.Replace('?', '_') + '?' + PlayerSettings.productName.Replace('?', '_') + '?';

			if (string.IsNullOrEmpty(lastPath) == false)
			{
				if (lastPath.StartsWith(start) == false)
				{
					NGSettings.CreateProjectCredentialsFile();

					string[]	data = lastPath.Split('?');

					if (data.Length == 3 && File.Exists(data[2]) == true)
					{
						if (File.Exists(path) == true) // Settings file does exist in the new folder, we need to ask to overwrite or restore.
						{
							if (EditorUtility.DisplayDialog(Constants.PackageTitle, "A change has been detected on Company Name or Product Name.\nFrom:\n  " + data[0] + Environment.NewLine + "  " + data[1] + "\nTo:\n  " + PlayerSettings.companyName + Environment.NewLine + "  " + PlayerSettings.productName + "\n\nWould you like to keep your " + Constants.PackageTitle + " settings and licenses?", "Keep", "No") == true)
							{
								NGSettings.CopyDirectory(Path.GetDirectoryName(data[2]), Path.GetDirectoryName(path));
								InternalNGDebug.Log(Constants.PackageTitle + " files have been restored.");
							}
						}
						else
						{
							NGSettings.CopyDirectory(Path.GetDirectoryName(data[2]), Path.GetDirectoryName(path));
							InternalNGDebug.Log("A change has been detected on Company Name or Product Name. " + Constants.PackageTitle + " files automatically restored.");
						}
					}
				}
			}
			else
				NGSettings.CreateProjectCredentialsFile();
		}

		private static void	CopyDirectory(string source, string destination)
		{
			string[]	files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);

			foreach (string file in files)
			{
				string	fileName = file.Substring(source.Length + 1);
				string	destFile = Path.Combine(destination, fileName);

				Directory.CreateDirectory(Path.GetDirectoryName(destFile));
				File.Copy(file, destFile, true);
			}
		}

		private static string	GetProjectLocalFile()
		{
			string	local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

			local = Path.Combine(local, Constants.InternalPackageTitle);
			return Path.Combine(local, Application.dataPath.Replace('/', '_').Replace('\\', '_').Replace(':', '_') + ".txt");
		}

		private static string	GetProjectCredentials()
		{
			string	file = NGSettings.GetProjectLocalFile();

			if (File.Exists(file) == true)
				return File.ReadAllText(file);
			return string.Empty;
		}

		private static void		CreateProjectCredentialsFile()
		{
			string	file = NGSettings.GetProjectLocalFile();

			Directory.CreateDirectory(Path.GetDirectoryName(file));
			File.WriteAllText(file, PlayerSettings.companyName.Replace('?', '_') + '?' + PlayerSettings.productName.Replace('?', '_') + '?' + NGSettings.GetSharedSettingsPath());
		}
	}
}