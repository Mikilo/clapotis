using NGLicenses;
using NGTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditorInternal;
#if UNITY_2018_3_OR_NEWER
using UnityEngine.Experimental.UIElements;
#endif

namespace NGToolsEditor
{
	using UnityEditor;
	using UnityEngine;

	public static class Preferences
	{
#if UNITY_2018_3_OR_NEWER
		internal class NGPreferencesProvider : SettingsProvider
		{
			[SettingsProvider]
			private static SettingsProvider	CreateProvider()
			{
				return new NGPreferencesProvider();
			}

			private	NGPreferencesProvider() : base("Preferences/" + Constants.PackageTitle, SettingsScope.User)
			{
				this.label = Constants.PackageTitle;
			}

			public override void	OnTitleBarGUI()
			{
				GUILayout.Label(Constants.Version, GeneralStyles.Version);
			}

			public override void	OnGUI(string searchContext)
			{
				//base.OnGUI(searchContext);

				Preferences.PreferencesGUI();
			}
		}

		internal class NGPreferencesToolsProvider : SettingsProvider
		{
			[SettingsProvider]
			private static SettingsProvider	CreateProvider()
			{
				return new NGPreferencesToolsProvider();
			}

			private	NGPreferencesToolsProvider() : base("Preferences/" + Constants.PackageTitle + "/Tools", SettingsScope.User)
			{
				this.label = "Tools";
			}

			public override void	OnTitleBarGUI()
			{
				GUILayout.Label(Constants.Version, GeneralStyles.Version);
			}

			public override void	OnGUI(string searchContext)
			{
				//base.OnGUI(searchContext);

				Preferences.OnGUIToolsVersions();
			}
		}

		internal class NGPreferencesLicensesProvider : SettingsProvider
		{
			[SettingsProvider]
			private static SettingsProvider	CreateProvider()
			{
				return new NGPreferencesLicensesProvider();
			}

			private	NGPreferencesLicensesProvider() : base("Preferences/" + Constants.PackageTitle + "/Licenses", SettingsScope.User)
			{
				this.label = "Licenses";
			}

			public override void	OnTitleBarGUI()
			{
				GUILayout.Label(Constants.Version, GeneralStyles.Version);
			}

			public override void	OnGUI(string searchContext)
			{
				//base.OnGUI(searchContext);

				Preferences.OnGUILicenses();
			}
		}
#endif

		private sealed class KudosCommentPopup : PopupWindowContent
		{
			private readonly EditorWindow	window;

			private readonly int	kudos;

			private string	comment = string.Empty;

			public	KudosCommentPopup(EditorWindow window, int kudos)
			{
				this.window = window;
				this.kudos = kudos;
			}

			public override Vector2	GetWindowSize()
			{
				return new Vector2(Mathf.Max(this.window.position.width, 175F), 60F);
			}

			public override void	OnGUI(Rect r)
			{
				using (LabelWidthRestorer.Get(200F))
					EditorGUILayout.PrefixLabel("Comment (" + this.comment.Length + " / 255 chars max)");

				this.comment = EditorGUILayout.TextField(this.comment);
				if (this.comment.Length > 255)
					this.comment = this.comment.Remove(255);

				if (GUILayout.Button("Send " + (this.kudos > 0 ? ":)" : ":(")) == true)
				{
					Preferences.SendKudos(this.kudos, this.comment);
					this.window.Focus();
				}
			}
		}

		private sealed class PreviewVersionsBackupPopup : PopupWindowContent
		{
			private string	content;

			public	PreviewVersionsBackupPopup(string content)
			{
				this.content = content;
			}

			public override Vector2	GetWindowSize()
			{
				Utility.content.text = content;
				return EditorStyles.label.CalcSize(Utility.content) + new Vector2(10F, 0F);
			}

			public override void	OnGUI(Rect r)
			{
				EditorGUI.TextArea(r, this.content);
			}
		}

		public enum Tab
		{
			Preferences,
			Tools,
			Licenses
		}

		public enum ToolsTab
		{
			Versions,
			Backups,
			Channel
		}

		public const string		Title = "NG Preferences";
		public const float		KarmaAnimYOffset = 25F;
		public static string[]	PlusKarmaSentences = new string[] { "Karma is a good way to know how people like it or not", "Thank you", "You are a good person", "This is kind of you", "I guess you like it", "Damn! This is nice!", "More more more!", "Encouragement is welcome.", "I can see you are a man of culture..." };
		public static string[]	LessKarmaSentences = new string[] { "Karma is a good way to know how people like it or not", "Well, well, well...", "At least I tried my best", "You can contact me if so", "I am sorry it doesn't fit your needs" };

		public static Tab		tab = Tab.Preferences;
		public static ToolsTab	toolsTab = ToolsTab.Versions;

		private static Vector2		scrollPositionPreferences = new Vector2();
		private static string[]		languages;
		private static int			currentLanguage;
		private static NGSettings[]	assets;
		private static string[]		names;
		private static Texture[]	languageIcons;

		private static string					karma = "Karma";
		private static BgColorContentAnimator	karmaFeedback;
		private static string					karmaSentence = string.Empty;

		private static string	currentInvoiceText;
		private static Vector2	scrollPositionTools = new Vector2();
		private static Vector2	scrollPositionLicenses = new Vector2();

		internal static GUIContent	DiscordContent = new GUIContent("Discord");
		internal static GUIContent	TwitterContent = new GUIContent("Twitter");
		internal static GUIContent	UnityForumContent = new GUIContent("Forum");

		static	Preferences()
		{
			// TODO Unity <5.6 backward compatibility?
			MethodInfo	ResetAssetsMethod = typeof(Preferences).GetMethod("ResetAssets", BindingFlags.Static | BindingFlags.NonPublic);

			try
			{
				EventInfo	projectChangedEvent = typeof(EditorApplication).GetEvent("projectChanged");
				projectChangedEvent.AddEventHandler(null, Delegate.CreateDelegate(projectChangedEvent.EventHandlerType, null, ResetAssetsMethod));
				//EditorApplication.projectChanged += Preferences.ResetAssets;
			}
			catch
			{
				FieldInfo	projectWindowChangedField = UnityAssemblyVerifier.TryGetField(typeof(EditorApplication), "projectWindowChanged", BindingFlags.Static | BindingFlags.Public);
				if (projectWindowChangedField != null)
					projectWindowChangedField.SetValue(null, Delegate.Combine((Delegate)projectWindowChangedField.GetValue(null), Delegate.CreateDelegate(projectWindowChangedField.FieldType, null, ResetAssetsMethod)));
				//EditorApplication.projectWindowChanged += Preferences.ResetAssets;
			}

			HQ.SettingsChanged += Preferences.RefreshNGSettings;

			// Delay load, because Unity has not loaded its resources yet.
			Utility.SafeDelayCall(() =>
			{
				NGLicensesManager.ActivationFailed += Preferences.OnActivationLicenseFailed;
				NGLicensesManager.RevokeFailed += Preferences.OnRevokeLicenseFailed;

				Preferences.tab = (Tab)NGEditorPrefs.GetInt("Preferences_tab", (int)Preferences.tab);

				Texture2D	icon = new Texture2D(0, 0, TextureFormat.RGBA32, false);
				icon.hideFlags = HideFlags.DontSave;
				icon.LoadImage(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAAsTAAALEwEAmpwYAAAAB3RJTUUH4QoMAzMeHYgjlAAAAm9JREFUWMPt18+LVlUYB/DP6FuC1EEwU8PrQl0ZLZJZCCUiaoi2UGihphwrqfBfGH9s2rQIRETd5QVduLIJRSGGcKGUBIqKUBSWB5TEX91MURTbnInhnXfed+7rzEjgs7lc+N7n+z3Pr/NcXthztp6xclSU1cvYivV4C1NwBcewO8VwvQnfi3M9+aWBrSmGA12SL8A3eHMEyD18iu+xCh/iVophfSMD1mB/UVaNFMPemuQzMIC5bWCv4FCOeA9u4G2YlAHL8nNPUVY7awbgyw7kgzYpk/+KzXivKKttgyk4jI1DwEdzSm53OP20fJqXagh+mOvjIpYMRuBmE2gdLhVltaUoq3aFurQmuUx+HitTDH8NCjjdAjgbX+NCUVafFGU1tQXm9S5q9gLeTTH8+V8bFmU1JedmTpsP7+M4TuHH3GLvoL+mgIEUw4phc6Aoq9X4FpNrOHuMRk0B/SmGtUMrU1FWfZiOvux0tNboIgXXWzmYiS8maPr+0tybcHICx//ZVgJO4NwEkP+Nn4YJSDE8xSbcGWcBx1IMD1tFQIrhch4sl8dRwMFW83moXcMH2IFbY0x+Cd91aqNX86RqjMPpt+dUjxyBFMPv+GocyE+mGPpHuiKbrQ9HxpD8Lj5rd0drisITbMDn+OMZyZ9gc4rhalc7Yb6Ke3EAi2qSP8XHKYaDnbaUduSr85Vcl/wRYifyYRHIy2kvlucNaWEXYf8Nm1IMP4xqLS/KagW2Yz5mPUMLPsA+7Eox/FPrvyDv9BvxEZbU/F+4kdO0r12xjboIi7Kah/exOO/5b+C1XNFVJvw573UDOJNieOyF/V/tX8Dkq2qqkK/GAAAAAElFTkSuQmCC"));
				Preferences.TwitterContent.image = icon;

				icon = new Texture2D(0, 0, TextureFormat.RGBA32, false);
				icon.hideFlags = HideFlags.DontSave;
				icon.LoadImage(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAAsTAAALEwEAmpwYAAAAB3RJTUUH4QoMAzM0xjPqQgAAA1hJREFUWMPtlk1oI2UYx3+TSTKT7ybdSbvjNl02rHW20uLWBQUrKh5EVhY9iIjsZQ+CyK5YAop4EPRUorinvQmyF/XiQS9i8ayswiJt3NroJjFt96Nbk6Yz+ZrEQ7vpaCY20bRF8Dm+z8P//b3v8/G+Ats2M5sSgbeAC0CEvbE7wIfAe8mEZgI4Lc43gXfYW4ts79EA3gVwWJyvsX/W2ssKMLiPAIN2AAdiBw7g7CZIVSSmxkMMBFzkb5a5+lMRjywiubf4y9UG1VqD6ZNhVEUms2Lw/XyB5VuVfwcQ8Dp59skhhgbdrN6uIrkdPH4qwtPTim38xmadX/MGhxWJl5+PsZTV+XzuBht6/Z8BnHkiynjcD8DQoLTraQI+JxP3BgCo1RpMjgVoNptc/mK59xoIBZxMjAURRQFRFHrOrcu1JT0xFiQUcPYO8PBkGIfQhyoX4KGJgd4BTmrBvlX61IlQbwCqIhEJudrWK9UG6ZxOpdpo85V0k19+06mbzfb5G3KhKlL3RRiP+drWjLLJ+x9fZ71YIxx08frZo3hkEYBb61UuXs5gVExGhmVefXEU8S/5i8d8tm1pewOjh+W2tcWMznqxBsB6scZiRm/5flzcwKiYAORWy6zYbGSn2REgatNyalRqnUp0CKjRnZgRi7jXI3JowN2VZscUhG3aRgm7eeWFGAvpEififpTwzibHYz7OPXeEzLLBA1oQWXJ0pdkRQN7OLUATMAwTr0dkVPUwqnpshbRjfrRj/p2aqZi4XY7WrVk1d03BzbWdHArA76U6c9+usZTVaTT/vuWyKwbffLfGjbXqnwrRqrnrDVz6NMdLp1XiI95WWw4fkvhhocDbFxc5MiwTjbjxyiIOh8CmYXKnUCW3WuapRxSmpyI4LdMzndM7jmNnp0fl0idZtLifB8dDHFU9eD0iV+YLVGpbsyCd020Fr14rcur+EMXNOtfzBlfmC6TSJZq9PkZNYCFdYiFd6mnqLWV13vjg2n/nQ/I/gBXA6LP2ajeaVoDP+gzgA54Bit0CnAe+7iNAADCBx4DbnYLa/jwzs6n7gOE+QfycTGj5mdnUcWAOGLnrSCY0wRZgr2xmNnUP8CUwaQXYty5IJrQ88Cjw1YG1YTKhFYHTwEd31/4AmLIGtnNGDhMAAAAASUVORK5CYII="));
				Preferences.DiscordContent.image = icon;

				icon = new Texture2D(0, 0, TextureFormat.RGBA32, false);
				icon.hideFlags = HideFlags.DontSave;
				icon.LoadImage(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAAsTAAALEwEAmpwYAAAAB3RJTUUH4QoMBAYVLSQuLwAAB6JJREFUWMPFl39wVNUVxz/3vrcb2E2IIEp+ktTwMxHGmFg6WsGBYC2DtmVIGgpiJC6MNcKUCtqOLTLT6RQ7GSCFOLSxwAz+WtoOYIwGqlgsTiEKDnQsRSJufm1+bgKhSXb3vXf6R8IKBsSKHc/M/efdc9/5nnPP+Z57FP9nmTVrlvT395ORkUFeQQFPrVihLt03v2qDhYWFEgwG6evrQ0To6elBKUUgECD51Olh+tcNYNu2bVJVVYXWmmg0yunTp3EcB9M0EZHYikQitLQEh53XX9bws8/65dszZ0plZSUDAwNEIhEcx0FEAIhGoziOE9N3HIfz3SECFxWuF8AHJ2sYGAqzUopIJEI4HMayLOLj40lKShp25ty5HnZt3Hj9V1BeUSH+F17kwoVetDYYOXIk6enpTJkyhdtvv53s7Gy2bK0kGAxiGEYMpGVFafqo/n8HsO5XG+TMvz/EcixMTGpfe53ExATy8/PIy8tj2rRpZGZm4vV6cbvd7N1XTd3RI2it8HrjSR8/ng//eZIo0NrWht8vUlSk1BcCUF6+WWper6anO4Rt22it8fl8FBcXk5CQgNYay7KwLAulFKHuHnbu3IFlWYgIc++9j9zbpvP00ycQEbq7u2DMm18sAuWbN0v1vlfp6mjHcRy01qxatYqSkhJEBNu2sW0bpRQulwvTNHn+j9s589FptFaMHTuO0tJlGBpGjUqkt7eXgYEBTrz9zrWTsHzzZql5tZqujnZEBK01a9asoaSkBAClFKZpXrYCDY3s3r176N5h8ZIljE9PJTU1lczMTBzHIRIOE2hu/vwIVDznl31/qaKjvQ0RwXEcVq5cSVFREV1dXUSjUSzHQmyJlZrbHceGDRuGvNdMzc5hbkEBoVCIhIQEsrOzef/997Esi57O9qsD+E15hbzyQgWtwSCWZWHbNj6fj4KCAs6cORPTU0qhlEJrjdvt5u/vvstr1dVYloVhGBQXF2NZUVpbWwmFQqSkpHDhwgX6+vpAKbZs2SJlZWXK/Kzxl3ftoK01iOM4GIbB44+XsXzFciLhKN4EL4bSgMJQBqIEwzCIRi12bN9Bf/8gL8yeU8AD9z9A1A7j8XhQSpGfn09iYiKdnZ1DpGVcHoFf/3aTvPTiTtqCLTHvvvf9HzCn4DucOlWPiGCaGsMwMQwDNdRS4txu/vrWWxw/9h5aa0YlJrJ69RPEj/ISibhhiB0nTJjApEmTCIVCTL8tj5UrH/20DFevXSsv79pBe3sbWutYiA8c2E/tG28ggEIQQCuNGrIuCApFJBLmYotburSE/LzbsG0bI27wXyKCy+Xizjvv5GyggeLCBfhf2jUYgXXrRE6dWkyoqxP1mXw4f+7cIIsBcoVkvfhda41pmhhKkZU1AQDDMIbp5+Tk4PV4WLBgQcyUuX69Un6/XzpCnZz+14eDWa0GPZ04aTJutxvbtodKzYXWCgRM12DpGYZJc1MjDQ0BxHF47rmtzJv3XRJHxQ8DcPz4cRobApT6Vsjzf9j2qb/bZZ1UVfllztx7JS0tTVJSUiQ5OVkWL1ki/+kbEEdEolFbLMcRxxFx5HI5/sFJmTR5sqSmpkpSUpJseLZcriTz58+Xm2++Se7I/6YcOnRoeFCr/H6ZM/c+SU8fL2lpaTJu3DhZunSpWJYl15Knf/mMJCUlSVpqqkybPl3O1J+9bL+5uVmys7MlJSVFJk+ZKk/+/BcyjAkfKSpSPypexOScHAzDwO12U1tby7Jly7Ci1uf2jFWPl/GNW25BFHR2dFBRsfmy/aNHj9LT04PWmhFeD7dOzbkyFZeWPqQWFRYzZUo2hmEQFxfH/gP7ecT3CJZ1dRBjx97Ioz9+DK00LpeLmprXOPzuP2L7hw8fjlH6TWPG8uCDxeqqvaC09CG1cGEhE6cOgXDHceDAAZYvX45t21cFsWjRD7ljxrdwHIeB/j42bdqIyGAZHjt+DMMwcLlcpKakXrsZ+XzLVFHhQiYORcLlclFbW4vP57tqJExtsPonP8Xj8WIYJnVHj/DnPXtobGykuakZwzAY6fGQlTUpdsb4vHt9de/e9U89+bNnWjs7sCJhRno8fBJooK6ujnB4AI/Hw+jRo2PEBDA+PY1AQwMnT55AK0VjYxO27fDOO39DKc2YG8eyePFj7Nr1+/UXueSaUllZKfUff0IkGmVkXBxnz9ZTV1eHx+MhIyOD3Nxc7rrrLnJzc0lOTqYr1MP9988n2NKMaZp4vfH09p4H4Nbpeezbs1tdSmZfSMTvF1VUpGTPHtkdifC7LVtpbPgEEWJEdcMNN5CVlcXdd8+kvv5jqqv3obUeYlOFOy6Oe2bPobK9FbV7EIT6sq/i1U+slbcPvklvby+2ZQ12hiEwIsKIESNiM8HFfpAwejTLHn6YVWVl6roHk9n3zERrTSAQoLurg1B39+B8EI7gOHasWi7Nj9GjEpk9a9awfnJdUlNTI01NTQRaWgi1hWjtDNLZ1s75c+eIRCNEwuHYszw3P58/vfLKVzsbzps3L/bDI0eOSCAQoLu7m0CgkebOdjpbWgiFuhHHITMj66ufDS+VGTNmXObdwYMHpaEhSHt7M/39FunpSXxt8p68d6UnBf8FgSnmy8pEHxIAAAAASUVORK5CYII="));
				Preferences.UnityForumContent.image = icon;

				try
				{
					string			rootPath = Path.Combine(HQ.RootPath, Constants.RelativeLocaleFolder);
					List<string>	languages = new List<string>();

					if (Directory.Exists(rootPath) == true)
						languages.AddRange(Directory.GetDirectories(rootPath));

					for (int i = 0; i < languages.Count; i++)
						languages[i] = Utility.NicifyVariableName(languages[i].Substring(rootPath.Length));

					for (int i = 0; i < Localization.embedLocales.Length; i++)
					{
						if (languages.Contains(Localization.embedLocales[i].language) == false)
							languages.Add(Localization.embedLocales[i].language);
					}

					string	prefLanguage = Localization.CurrentLanguage();

					for (Preferences.currentLanguage = 0; Preferences.currentLanguage < languages.Count; Preferences.currentLanguage++)
					{
						if (prefLanguage == languages[Preferences.currentLanguage])
							break;
					}

					if (Preferences.currentLanguage >= languages.Count)
						Preferences.currentLanguage = 0;

					Preferences.languages = languages.ToArray();
					NGDiagnostic.Log(Preferences.Title, "Languages", string.Join(", ", Preferences.languages));
				}
				catch (Exception ex)
				{
					Preferences.languages = new string[0];
					Preferences.languageIcons = new Texture[0];
					InternalNGDebug.LogException(ex);
				}

				Preferences.languageIcons = new Texture[Preferences.languages.Length];

				for (int i = 0; i < Preferences.languages.Length; i++)
				{
					Preferences.languageIcons[i] = AssetDatabase.LoadAssetAtPath(Path.Combine(HQ.RootPath, Constants.RelativeLocaleFolder + Preferences.languages[i] + "/" + Preferences.languages[i]) + ".png", typeof(Texture)) as Texture;
					if (Preferences.languageIcons[i] == null)
					{
						for (int j = 0; j < Localization.embedLocales.Length; j++)
						{
							if (Preferences.languages[i] == Localization.embedLocales[j].language)
							{
								Preferences.languageIcons[i] = Localization.embedLocales[j].icon;
								break;
							}
						}
					}
				}
			});
		}

		[MenuItem(Constants.MenuItemPath + "Get NG Log", priority = Constants.MenuItemPriority + 1001), Hotkey("Get NG Log")]
		public static void	GetNGLog()
		{
			EditorUtility.RevealInFinder(InternalNGDebug.LogPath);
		}

#if !UNITY_2018_3_OR_NEWER
		[PreferenceItem(Constants.PreferenceTitle)]
#endif
		public static void	PreferencesGUI()
		{
			EditorGUI.BeginDisabledGroup(EditorApplication.isCompiling);
			{
#if FORCE_INVOICE
				EditorGUILayout.HelpBox("This special version has all its features enabled for the time of the Unity Awards event.", MessageType.Warning);
#endif
#if !UNITY_2018_3_OR_NEWER
				EditorGUILayout.BeginHorizontal();
				{
					EditorGUI.BeginChangeCheck();
					GUILayout.Toggle(Preferences.tab == Tab.Preferences, "Preferences", "ToolbarButton");
					if (EditorGUI.EndChangeCheck() == true)
					{
						NGEditorPrefs.SetInt("Preferences_tab", (int)Tab.Preferences);
						Preferences.tab = Tab.Preferences;
					}

					EditorGUI.BeginChangeCheck();
					GUILayout.Toggle(Preferences.tab == Tab.Tools, "Tools", "ToolbarButton");
					if (EditorGUI.EndChangeCheck() == true)
					{
						NGEditorPrefs.SetInt("Preferences_tab", (int)Tab.Tools);
						Preferences.tab = Tab.Tools;
					}

					EditorGUI.BeginChangeCheck();
					GUILayout.Toggle(Preferences.tab == Tab.Licenses, "Licenses", "ToolbarButton");
					if (EditorGUI.EndChangeCheck() == true)
					{
						NGEditorPrefs.SetInt("Preferences_tab", (int)Tab.Licenses);
						Preferences.tab = Tab.Licenses;
					}
				}
				EditorGUILayout.EndHorizontal();

				if (Preferences.tab == Tab.Preferences)
					Preferences.OnGUIPreferences();
				else if (Preferences.tab == Tab.Tools)
					Preferences.OnGUIToolsVersions();
				else if (Preferences.tab == Tab.Licenses)
					Preferences.OnGUILicenses();
#else
				Preferences.OnGUIPreferences();
#endif
			}
			EditorGUI.EndDisabledGroup();
		}

		private static void	OnGUIToolsVersions()
		{
			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				GUILayout.Label("Name", GUILayoutOptionPool.Width(180F));
				GUILayout.Label("Version", GUILayoutOptionPool.ExpandWidthTrue);
				GUILayout.FlexibleSpace();
				GUILayout.Label("Licensing");
				GUILayout.Space(16F);
			}
			EditorGUILayout.EndHorizontal();

			Preferences.scrollPositionTools = EditorGUILayout.BeginScrollView(Preferences.scrollPositionTools);
			{
				foreach (HQ.ToolAssemblyInfo tool in HQ.EachTool)
				{
					EditorGUILayout.BeginHorizontal();
					{
						if (GUILayout.Button("☰", GUILayoutOptionPool.Width(18F)) == true)
						{
							GenericMenu	menu = new GenericMenu();

							if (string.IsNullOrEmpty(tool.wikiURL) == false)
								menu.AddItem(new GUIContent("Help"), false, url => Application.OpenURL((string)url), tool.wikiURL);

							menu.AddItem(new GUIContent("Send feedback"), false, name => ContactFormWizard.Open(ContactFormWizard.Subject.Feedback, (string)name, null), tool.name);

							if (NGChangeLogWindow.HasChangeLog(tool.name) == true)
								menu.AddItem(new GUIContent("Change log"), false, name => NGChangeLogWindow.Open((string)name), tool.name);

							menu.ShowAsContext();
						}

						EditorGUILayout.LabelField(tool.name, GUILayoutOptionPool.Width(170F));
						GUILayout.Label(tool.version);

						GUILayout.FlexibleSpace();

						if (string.IsNullOrEmpty(tool.assetStoreBuyLink) == false)
						{
							if (NGLicensesManager.IsPro(tool.name + " Pro") == true)
							{
								using (ColorContentRestorer.Get(GeneralStyles.HighlightActionButton))
								{
									GUILayout.Label("PRO");
									Rect	r = GUILayoutUtility.GetLastRect();
									Utility.DrawUnfillRect(r, Color.cyan);
								}
							}
							else
							{
								using (ColorContentRestorer.Get(GeneralStyles.HighlightActionButton))
								{
									if (GUILayout.Button("Buy", GUILayoutOptionPool.Width(50F)) == true)
										Application.OpenURL(tool.assetStoreBuyLink);
								}

								using (ColorContentRestorer.Get(GeneralStyles.HighlightResultButton))
								{
									GUILayout.Label("FREE");
								}
							}
						}
						else
							GUILayout.Label("FULL");
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			EditorGUILayout.EndScrollView();

			GUILayout.FlexibleSpace();

			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				GUILayout.Label("Free(Limited) Pro(Full featured) Full(No Free/Pro, all unlocked)", GeneralStyles.SmallLabel);
			}
			EditorGUILayout.EndHorizontal();
		}

		private static void	OnGUILicenses()
		{
#if !FORCE_INVOICE
			EditorGUILayout.HelpBox(Constants.PackageTitle + " allows up to 2 seats activation per invoice.", MessageType.Info);
			EditorGUILayout.BeginHorizontal();
			{
				EditorGUILayout.HelpBox("If you are having issues with the license:", MessageType.Info);
				if (GUILayout.Button("Help", GUILayoutOptionPool.ExpandHeightTrue) == true)
					LicenseHelpWindow.Toggle();
			}
			EditorGUILayout.EndHorizontal();
#endif
			if (NGLicensesManager.IsServerOperationnal == false)
				EditorGUILayout.HelpBox("Server seems not responding correctly, retry later or contact the author.", MessageType.Error);

#if FORCE_INVOICE
			EditorGUI.BeginDisabledGroup(true);
#endif
			Preferences.scrollPositionLicenses = EditorGUILayout.BeginScrollView(Preferences.scrollPositionLicenses);
			{
				EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
				{
					GUILayout.Label("Active Invoice", GeneralStyles.CenterText, GUILayoutOptionPool.Width(110F));
					GUILayout.Label("Asset", GeneralStyles.CenterText);
					GUILayout.Label(string.Empty);
					GUILayout.Label(string.Empty, GUILayoutOptionPool.Width(20F));
				}
				EditorGUILayout.EndHorizontal();

				using (LabelWidthRestorer.Get(100F))
				{
					int	totalActiveInvoices = 0;

					foreach (License invoice in NGLicensesManager.EachInvoices())
					{
						if (invoice.active == false)
							continue;

						++totalActiveInvoices;
					}

					Rect	r = GUILayoutUtility.GetLastRect();
					r.y += r.height;
					r.height = Mathf.Max(1, totalActiveInvoices) * (Constants.SingleLineHeight + 8F);
					EditorGUI.HelpBox(r, string.Empty, MessageType.None);

					if (totalActiveInvoices == 0)
						GUILayout.Label("No invoice activated.", GeneralStyles.AllCenterTitle);
					else
					{
						foreach (License invoice in NGLicensesManager.EachInvoices())
						{
							if (invoice.active == false)
								continue;

							++totalActiveInvoices;

							EditorGUILayout.BeginHorizontal();
							{
								if (NGLicensesManager.IsCheckingInvoice(invoice.invoice) == true)
								{
									EditorGUILayout.TextField(invoice.invoice, GUILayoutOptionPool.Width(110F));
									GUILayout.Label(GeneralStyles.StatusWheel, GUILayoutOptionPool.Width(20F));
									GUILayout.Label("Requesting...");

									Utility.RepaintEditorWindow(Utility.settingsWindowType);
								}
								else
								{
									EditorGUILayout.TextField(invoice.invoice, GUILayoutOptionPool.Width(110F));
									GUILayout.Label(invoice.assetName);

									if (GUILayout.Button("Revoke", "ButtonLeft", GUILayoutOptionPool.Width(85F)) == true)
										NGLicensesManager.RevokeLicense(invoice.invoice);
									if (GUILayout.Button("Seats", "ButtonRight", GUILayoutOptionPool.Width(45F)) == true)
										Preferences.RequestActiveSeats(invoice.invoice);
									XGUIHighlightManager.DrawHighlightLayout(Preferences.Title + ".CheckSeats", EditorWindow.focusedWindow, XGUIHighlightManager.Highlights.Glow);
								}
							}
							EditorGUILayout.EndHorizontal();
						}
					}

					GUILayout.Space(25F);

					EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
					{
						GUILayout.Label("Available Invoice", GeneralStyles.CenterText, GUILayoutOptionPool.Width(110F));
						GUILayout.Label("Asset", GeneralStyles.CenterText);
						GUILayout.Label(string.Empty);
						GUILayout.Label(string.Empty, GUILayoutOptionPool.Width(20F));
					}
					EditorGUILayout.EndHorizontal();

					int	totalInactiveInvoices = 0;

					foreach (License invoice in NGLicensesManager.EachInvoices())
					{
						if (invoice.active == true)
							continue;

						++totalInactiveInvoices;
					}

					r = GUILayoutUtility.GetLastRect();
					r.y += r.height;
					r.height = Mathf.Max(1, totalInactiveInvoices) * (Constants.SingleLineHeight + 8F);
					EditorGUI.HelpBox(r, string.Empty, MessageType.None);

					if (totalInactiveInvoices == 0)
						GUILayout.Label("No invoice available. Add invoice by using the input below.");
					else
					{
						foreach (License invoice in NGLicensesManager.EachInvoices())
						{
							if (invoice.active == true)
								continue;

							++totalInactiveInvoices;

							EditorGUILayout.BeginHorizontal();
							{
								if (NGLicensesManager.IsCheckingInvoice(invoice.invoice) == true)
								{
									EditorGUILayout.TextField(invoice.invoice, GUILayoutOptionPool.Width(110F));
									GUILayout.Label(GeneralStyles.StatusWheel, GUILayoutOptionPool.Width(20F));
									GUILayout.Label("Requesting...");

									Utility.RepaintEditorWindow(Utility.settingsWindowType);
								}
								else if (invoice.status == Status.Unknown) // Not verified yet.
								{
									EditorGUILayout.TextField(invoice.invoice, GUILayoutOptionPool.Width(110F));
									GUILayout.Label("Require check ---->");

									if (GUILayout.Button("Check", GUILayoutOptionPool.Width(80F)) == true)
										NGLicensesManager.VerifyLicenses(invoice.invoice);
									XGUIHighlightManager.DrawHighlightLayout(Preferences.Title + ".CheckLicense", EditorWindow.focusedWindow, XGUIHighlightManager.Highlights.Glow);

									if (GUILayout.Button("X", GUILayoutOptionPool.Width(20F)) == true)
										NGLicensesManager.RemoveInvoice(invoice.invoice);
								}
								else if (invoice.status == Status.Banned)
								{
									EditorGUILayout.TextField(invoice.invoice, GUILayoutOptionPool.Width(110F));

									EditorGUI.BeginDisabledGroup(true);
									{
										GUILayout.Label("Unauthorized");
									}
									EditorGUI.EndDisabledGroup();

									if (GUILayout.Button("X", GUILayoutOptionPool.Width(20F)) == true)
										NGLicensesManager.RemoveInvoice(invoice.invoice);
								}
								else if (invoice.status == Status.Invalid)
								{
									EditorGUILayout.TextField(invoice.invoice, GUILayoutOptionPool.Width(110F));

									EditorGUI.BeginDisabledGroup(true);
									{
										GUILayout.Label("Invalid");
									}
									EditorGUI.EndDisabledGroup();

									if (GUILayout.Button("X", GUILayoutOptionPool.Width(20F)) == true)
										NGLicensesManager.RemoveInvoice(invoice.invoice);
								}
								else
								{
									EditorGUILayout.TextField(invoice.invoice, GUILayoutOptionPool.Width(110F));

									EditorGUI.BeginDisabledGroup(true);
									{
										GUILayout.Label(invoice.assetName);
									}
									EditorGUI.EndDisabledGroup();

									using (ColorContentRestorer.Get(GeneralStyles.HighlightActionButton))
									{
										if (GUILayout.Button("Activate", "ButtonLeft", GUILayoutOptionPool.Width(60F)) == true)
											NGLicensesManager.ActivateLicense(invoice.invoice);
										XGUIHighlightManager.DrawHighlightLayout(Preferences.Title + ".ActivateLicense", EditorWindow.focusedWindow, XGUIHighlightManager.Highlights.Glow);
									}

									if (GUILayout.Button("Seats", "ButtonRight", GUILayoutOptionPool.Width(45F)) == true)
										Preferences.RequestActiveSeats(invoice.invoice);
									XGUIHighlightManager.DrawHighlightLayout(Preferences.Title + ".CheckSeats", EditorWindow.focusedWindow, XGUIHighlightManager.Highlights.Glow);

									if (GUILayout.Button("X", GUILayoutOptionPool.Width(20F)) == true)
										NGLicensesManager.RemoveInvoice(invoice.invoice);
								}
							}
							EditorGUILayout.EndHorizontal();
						}
					}
				}
			}
			EditorGUILayout.EndScrollView();

			GUILayout.FlexibleSpace();

			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				using (LabelWidthRestorer.Get(70F))
				{
					Preferences.currentInvoiceText = EditorGUILayout.TextField("Invoice", Preferences.currentInvoiceText);
					XGUIHighlightManager.DrawHighlightLayout(Preferences.Title + ".AddInvoice", EditorWindow.focusedWindow);
					EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(Preferences.currentInvoiceText));
					if (GUILayout.Button("Add", GeneralStyles.ToolbarButton) == true)
						NGLicensesManager.AddInvoice(Preferences.currentInvoiceText);
					EditorGUI.EndDisabledGroup();
				}
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(4F);

#if FORCE_INVOICE
			EditorGUI.EndDisabledGroup();
#endif
		}

		private static void	RequestActivateSeat(object raw)
		{
			NGLicensesManager.ActivateLicense((string)raw);
		}

		private static void	RequestActiveSeats(object raw)
		{
			NGLicensesManager.ShowActiveSeatsFromLicense((string)raw, Preferences.OnSetInvoiceSeats);
		}

		private static void	OnSetInvoiceSeats(string invoice, string[] seats)
		{
			Utility.OpenWindow<LicenseSeatsWindow>(true, LicenseSeatsWindow.Title, true, null, window => {
				window.Set(invoice, seats);
			});
		}

		private static void	OnGUIPreferences()
		{
			if (NGLicensesManager.IsPro("NG Tools Pro") == false)
			{
				EditorGUILayout.BeginHorizontal();
				{
					EditorGUILayout.HelpBox("Update to NG Tools Pro in the Asset Store", MessageType.Info);
					if (GUILayout.Button("Buy NG Tools Pro", GUILayoutOptionPool.Height(36F)) == true && EditorUtility.DisplayDialog("", "You are opening the Asset Store page on your browser. Do you confirm?", "Yes", "No") == true)
					{
						Help.BrowseURL("https://www.assetstore.unity3d.com/en/#!/content/34109");
					}
				}
				EditorGUILayout.EndHorizontal();
			}

			if (Preferences.languages != null && Preferences.languages.Length > 1)
			{
				GUILayout.Space(5F);

				EditorGUILayout.BeginHorizontal();
				{
					GUILayout.Label(LC.G("Language"));

					if (Preferences.languageIcons != null)
					{
						Rect	r = GUILayoutUtility.GetLastRect();

						r.x += r.width;
						r.width = 16F;
						r.height = 11F;
						r.y += 4F;

						for (int i = 0; i < Preferences.languages.Length; i++)
						{
							r.x -= r.width + 5F;

							if (Event.current.type == EventType.MouseDown &&
								r.Contains(Event.current.mousePosition) == true)
							{
								Preferences.currentLanguage = i;
								Localization.SaveLanguage(Preferences.languages[Preferences.currentLanguage]);
								if (Localization.LoadLanguage(Preferences.languages[Preferences.currentLanguage]) == true)
								{
									InternalNGDebug.Log(string.Format(LC.G("Preferences_LoadedLocale"), Preferences.languages[Preferences.currentLanguage]));
									Utility.RepaintEditorWindow(typeof(EditorWindow));
								}
							}

							EditorGUI.DrawTextureTransparent(r, Preferences.languageIcons[i]);

							if (i == Preferences.currentLanguage && Event.current.type == EventType.Repaint)
								Utility.DrawUnfillRect(r, Color.white);
						}
					}
				}
				EditorGUILayout.EndHorizontal();
			}

			GUILayout.Space(5F);

			EditorGUILayout.BeginHorizontal();
			{
				if (GUILayout.Button(LC.G("Preferences_GenerateConfigFile")) == true)
				{
					string	path = EditorUtility.SaveFilePanelInProject(LC.G("Preferences_SaveConfigFile"), "NGSettings", "asset", LC.G("Preferences_ChoosePathConfigFile"));

					if (string.IsNullOrEmpty(path) == false)
						HQ.CreateNGSettings(path);
				}

				GUILayout.Space(2F);

				if (GUILayout.Button(LC.G("Refresh")) == true)
					TaskLoadAssets();
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5F);

			EditorGUILayout.LabelField(LC.G("Preferences_ConfigurationFilesAvailable"));

			Preferences.scrollPositionPreferences = EditorGUILayout.BeginScrollView(Preferences.scrollPositionPreferences);
			{
				EditorGUILayout.BeginHorizontal();
				{
					if (GUILayout.Button(LC.G("Open"), GUILayoutOptionPool.ExpandWidthFalse) == true)
						EditorUtility.RevealInFinder(NGSettings.GetSharedSettingsPath());

					bool	on = HQ.Settings != null && (HQ.Settings.hideFlags & HideFlags.DontSave) == HideFlags.DontSave;

					EditorGUI.BeginChangeCheck();
					GUILayout.Toggle(on, LC.G("Preferences_SharedSettings"), GUI.skin.button);
					if (EditorGUI.EndChangeCheck() == true)
					{
						if (on == false)
						{
							HQ.LoadSharedNGSetting();
							NGEditorPrefs.SetString(Constants.ConfigPathKeyPref, null, true);
							InternalEditorUtility.RepaintAllViews();
						}
						else
							Selection.activeObject = NGSettings.sharedSettings;
					}

					if (GUILayout.Button(LC.G("Preferences_Reset"), GUILayoutOptionPool.ExpandWidthFalse) == true && ((Event.current.modifiers & Constants.ByPassPromptModifier) != 0 || EditorUtility.DisplayDialog(LC.G("Preferences_ConfirmResetSharedSettingsTitle"), LC.G("Preferences_ConfirmResetSharedSettings"), LC.G("Yes"), LC.G("No")) == true))
					{
						NGSettings.sharedSettings = null;
						HQ.LoadSharedNGSetting(true);
						NGEditorPrefs.SetString(Constants.ConfigPathKeyPref, null, true);
						InternalEditorUtility.RepaintAllViews();
					}
				}
				EditorGUILayout.EndHorizontal();

				if (Preferences.assets != null)
				{
					for (int i = 0; i < Preferences.assets.Length; i++)
					{
						if (Preferences.assets[i] == null || (Preferences.assets[i].hideFlags & HideFlags.DontSave) == HideFlags.DontSave)
							continue;

						EditorGUILayout.BeginHorizontal();
						{
							EditorGUI.BeginChangeCheck();
							GUILayout.Toggle(Selection.activeObject == Preferences.assets[i], LC.G("Preferences_Focus"), GUI.skin.button, GUILayoutOptionPool.ExpandWidthFalse);
							if (EditorGUI.EndChangeCheck() == true)
								Selection.activeObject = Preferences.assets[i];

							EditorGUI.BeginChangeCheck();
							GUILayout.Toggle(Preferences.assets[i] == HQ.Settings, Preferences.names[i], GeneralStyles.LeftButton);
							if (EditorGUI.EndChangeCheck() == true && Preferences.assets[i] != HQ.Settings)
							{
								NGEditorPrefs.SetString(Constants.ConfigPathKeyPref, Preferences.names[i], true);
								HQ.SetSettings(Preferences.assets[i]);
								InternalEditorUtility.RepaintAllViews();
							}
						}
						EditorGUILayout.EndHorizontal();
					}
				}
				EditorGUILayout.EndScrollView();
			}

			EditorGUILayout.Space();

			using (LabelWidthRestorer.Get(60F))
			{
				EditorGUI.BeginChangeCheck();
				string	logPath = NGEditorGUILayout.SaveFileField("Log Path", InternalNGDebug.LogPath);
				if (EditorGUI.EndChangeCheck() == true)
					InternalNGDebug.LogPath = logPath;
			}

			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				if (GUILayout.Button(Preferences.DiscordContent, GeneralStyles.ToolbarButton) == true)
					Application.OpenURL(Constants.DiscordURL);

				if (GUILayout.Button(Preferences.TwitterContent, GeneralStyles.ToolbarButton) == true)
					Application.OpenURL(Constants.TwitterURL);

				if (GUILayout.Button(Preferences.UnityForumContent, GeneralStyles.ToolbarButton) == true)
					Application.OpenURL(Constants.SupportForumUnityThread);
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				if (GUILayout.Button(LC.G("Preferences_Contact"), GeneralStyles.ToolbarButton) == true)
				ContactFormWizard.Open(ContactFormWizard.Subject.Contact);

				//if (GUILayout.Button(LC.G("Preferences_Tips"), GeneralStyles.ToolbarButton) == true)
				//	Utility.OpenWindow<TipsWindow>(true, string.Empty, true);

				if (GUILayout.Button(LC.G("Preferences_Diagnose"), GeneralStyles.ToolbarButton) == true && EditorUtility.DisplayDialog(Constants.PackageTitle, "Diagnostic will stop the scene, save the project and force a refresh.\nDo you confirm?", LC.G("Yes"), LC.G("No")) == true)
					NGDiagnostic.Diagnose();
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				EditorGUI.BeginChangeCheck();
				Rect	r = GUILayoutUtility.GetRect(0F, 16F, "GV Gizmo DropDown");
				bool	value = string.IsNullOrEmpty(HQ.RootedMenuFilePath) == false && File.Exists(HQ.RootedMenuFilePath);

				if (GUI.Button(r, "Rooted Menu:" + (value == true ? LC.G("Yes") : LC.G("No")), "GV Gizmo DropDown") == true)
				{
					GenericMenu	menu = new GenericMenu();
					menu.AddItem(new GUIContent(LC.G("Yes")), value == true, HQ.SetNestedMode, true);
					menu.AddItem(new GUIContent(LC.G("No")), value == false, HQ.SetNestedMode, false);
					menu.DropDown(r);
				}
				XGUIHighlightManager.DrawHighlight(Preferences.Title + ".RootedMenu", EditorWindow.focusedWindow, r);

				r = GUILayoutUtility.GetRect(0F, 16F, "GV Gizmo DropDown");
				bool	sendStats = NGEditorPrefs.GetBool(HQ.AllowSendStatsKeyPref, true);
				if (GUI.Button(r, "Send Stats:" + (sendStats ? LC.G("Yes") : LC.G("No")), "GV Gizmo DropDown") == true)
				{
					GenericMenu	menu = new GenericMenu();
					menu.AddItem(new GUIContent("Yes"), sendStats == true, HQ.SetSendStats, true);
					menu.AddItem(new GUIContent("No"), sendStats == false, HQ.SetSendStats, false);
					menu.DropDown(r);
				}

				r = GUILayoutUtility.GetRect(0F, 16F, "GV Gizmo DropDown");
				if (GUI.Button(r, "Debug:" + Enum.GetName(typeof(Conf.DebugState), Conf.DebugMode), "GV Gizmo DropDown") == true)
				{
					GenericMenu	menu = new GenericMenu();
					menu.AddItem(new GUIContent("None"), Conf.DebugMode == Conf.DebugState.None, SetDebugMode, Conf.DebugState.None);
					menu.AddItem(new GUIContent("Active"), Conf.DebugMode == Conf.DebugState.Active, SetDebugMode, Conf.DebugState.Active);
					menu.AddItem(new GUIContent("Verbose"), Conf.DebugMode == Conf.DebugState.Verbose, SetDebugMode, Conf.DebugState.Verbose);
					menu.DropDown(r);
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				using (BgColorContentRestorer.Get(Color.green * .9F))
				{
					if (GUILayout.Button(":)", GeneralStyles.ToolbarButton) == true)
						Preferences.SendKudos(1, string.Empty);

					Rect	r = GUILayoutUtility.GetLastRect();

					if (GUILayout.Button("@", "GV Gizmo DropDown", GUILayoutOptionPool.Width(35F)) == true)
						PopupWindow.Show(r, new KudosCommentPopup(EditorWindow.focusedWindow, 1));
				}

				if (Preferences.karmaFeedback != null)
				{
					using (ColorContentRestorer.Get(Preferences.karmaFeedback.af.isAnimating, Preferences.karmaFeedback.Value * Color.yellow))
					{
						GUILayout.Label(Preferences.karma, GeneralStyles.CenterText);
						Rect	r = GUILayoutUtility.GetLastRect();
						r.y -= r.height + Preferences.karmaFeedback.Value * Preferences.KarmaAnimYOffset;
						r.width = EditorWindow.focusedWindow.position.width;
						r.x = 0;
						EditorGUI.DrawRect(r, (EditorGUIUtility.isProSkin == false ? Color.white * .9F : Color.black * .8F));
						GUI.Label(r, Preferences.karmaSentence, GeneralStyles.CenterText);
					}

					if (Preferences.karmaFeedback.af.isAnimating == false)
						Preferences.karmaFeedback = null;
				}
				else
					GUILayout.Label(Preferences.karma, GeneralStyles.CenterText);

				using (BgColorContentRestorer.Get(Color.red * .6F))
				{
					if (GUILayout.Button(":(", GeneralStyles.ToolbarButton) == true)
					{
						if (NGLicensesManager.HasValidLicense() == false)
							EditorUtility.DisplayDialog(Constants.PackageTitle, "Stop! Hate is only available for Pro version.\n\nIf you want to criticize, buy it first.\n\nHehehe! X)", "OK");
						else
							Preferences.SendKudos(-1, string.Empty);
					}

					Rect	r = GUILayoutUtility.GetLastRect();

					if (GUILayout.Button("@", "GV Gizmo DropDown", GUILayoutOptionPool.Width(35F)) == true)
					{
						if (NGLicensesManager.HasValidLicense() == false)
							EditorUtility.DisplayDialog(Constants.PackageTitle, "Stop! Hate is only available for Pro version.\n\nIf you want to criticize, buy it first.\n\nHehehe! X)", "OK");
						else
							PopupWindow.Show(r, new KudosCommentPopup(EditorWindow.focusedWindow, -1));
					}
				}

#if !UNITY_2018_3_OR_NEWER
				GUILayout.Label(Constants.Version, GeneralStyles.Version, GUILayoutOptionPool.ExpandWidthFalse);
#endif
			}
			EditorGUILayout.EndHorizontal();
		}

		private static void	SetDebugMode(object mode)
		{
			Conf.DebugMode = (Conf.DebugState)mode;
			Conf.Save();
		}

		private static void	SendKudos(int n, string comment)
		{
			StringBuilder	buffer = Utility.GetBuffer(HQ.ServerEndPoint + "kudos.php?n=");

			buffer.Append(SystemInfo.deviceName);
			buffer.Append("&un=");
			buffer.Append(Environment.UserName);

			if (n < 0)
				buffer.Append("&bk");

			if (string.IsNullOrEmpty(comment) == false)
			{
				buffer.Append("&c=");
				buffer.Append(comment);
			}

			Utility.RequestURL(Utility.ReturnBuffer(buffer), (status, arg) =>
			{
				if (status == Utility.RequestStatus.Completed)
				{
					int	karma;

					EditorApplication.delayCall += () =>
					{
						if (string.IsNullOrEmpty(comment) == false)
							EditorUtility.DisplayDialog(Constants.PreferenceTitle, "I read every comments you send. Good or not!", "OK");

						Preferences.karmaFeedback = new BgColorContentAnimator(EditorWindow.focusedWindow.Repaint, 0F, 1F);
						Preferences.karmaFeedback.af.speed = .5F;
						if (n == 1)
							Preferences.karmaSentence = Preferences.PlusKarmaSentences[Random.Range(0, Preferences.PlusKarmaSentences.Length)];
						else
							Preferences.karmaSentence = Preferences.LessKarmaSentences[Random.Range(0, Preferences.LessKarmaSentences.Length)];
					};

					if (int.TryParse(arg as string, out karma) == true)
						Preferences.karma = "Karma (" + karma + ")";
					else
						Preferences.karma = "Karma (E)";
				}
			});
		}

		private static void	TaskLoadAssets()
		{
			string[]	assets = Directory.GetFiles("Assets", "*.asset", SearchOption.AllDirectories);

			int	maxAssetsToLoad = assets.Length;
			int	assetsLoaded = 0;

			for (int j = 0; j < assets.Length; j++)
			{
				AssetDatabase.LoadAssetAtPath(assets[j], typeof(NGSettings));
				++assetsLoaded;

				EditorUtility.DisplayProgressBar(Constants.PackageTitle, "Looking for NG Settings in all assets. (" + assetsLoaded + " / " + maxAssetsToLoad + ")", (float)assetsLoaded / (float)maxAssetsToLoad);
			}

			EditorUtility.ClearProgressBar();

			Preferences.RefreshNGSettings();
		}

		private static void	ResetAssets()
		{
			if (Preferences.assets != null)
			{
				List<NGSettings>	assets = new List<NGSettings>(Preferences.assets);
				List<string>		names = new List<string>(Preferences.names);

				for (int i = 0; i < assets.Count; i++)
				{
					if (assets[i] == null)
					{
						assets.RemoveAt(i);
						names.RemoveAt(i);
					}
				}

				Preferences.assets = assets.ToArray();
				Preferences.names = names.ToArray();
			}
		}

		private static void	RefreshNGSettings()
		{
			Preferences.assets = Resources.FindObjectsOfTypeAll<NGSettings>();
			if (Preferences.assets.Length == 0)
				return;

			Preferences.names = new string[Preferences.assets.Length];

			for (int i = 0; i < Preferences.assets.Length; i++)
			{
				if ((Preferences.assets[i].hideFlags & HideFlags.DontSave) != HideFlags.DontSave)
					Preferences.names[i] = AssetDatabase.GetAssetPath(Preferences.assets[i].GetInstanceID());
			}
		}

		private static void	OnRevokeLicenseFailed(string invoice, string message, string result)
		{
			if (EditorUtility.DisplayDialog(Constants.PackageTitle, message, "OK", "Contact") == false)
				ContactFormWizard.Open(ContactFormWizard.Subject.BugReport, "Revoke license using invoice \"" + invoice + "\" failed. Server responded with \"" + result + "\".");
		}

		private static void	OnActivationLicenseFailed(string invoice, string message, string result)
		{
			if (result == "-4")
			{
				if (EditorUtility.DisplayDialog(Constants.PackageTitle, message, "Show active seats", "Contact") == true)
					NGLicensesManager.ShowActiveSeatsFromLicense(invoice, Preferences.OnSetInvoiceSeats);
				else
					ContactFormWizard.Open(ContactFormWizard.Subject.BugReport, "Activate license using invoice \"" + invoice + "\" failed. Server responded with \"" + result + "\".");
			}
			else if (EditorUtility.DisplayDialog(Constants.PackageTitle, message, "OK", "Contact") == false)
				ContactFormWizard.Open(ContactFormWizard.Subject.BugReport, "Activate license using invoice \"" + invoice + "\" failed. Server responded with \"" + result + "\".");
		}
	}
}