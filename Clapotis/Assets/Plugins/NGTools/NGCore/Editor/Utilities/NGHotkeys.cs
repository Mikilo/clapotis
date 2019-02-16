using NGTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor
{
	public static class NGHotkeys
	{
		private struct MenuItemShortcut
		{
			public readonly string	name;
			public readonly Type	@class;
			public readonly string	staticMethod;

			public	MenuItemShortcut(string name, Type @class, string staticMethod)
			{
				this.name = name;
				this.@class = @class;
				this.staticMethod = staticMethod;
			}
		}

		public const string	Title = "NG Hotkeys";
		public const string	CustomHotkeysPath = "Editor/";
		public const string	CustomHotkeysFilename = "CustomHotkeys.cs";
		public const string	SubMenuItemPath = "Custom Hotkeys/";

		private static List<MenuItemShortcut>	shortcuts = new List<MenuItemShortcut>();
		private static bool						isGenerating;
		private static string					failedOnce;
		private static Vector2					scrollPosition;

		static	NGHotkeys()
		{
			foreach (Type type in Utility.EachNGTSubClassesOf(typeof(object)))
			{
				MethodInfo[]	methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

				for (int i = 0; i < methods.Length; i++)
				{
					if (methods[i].IsDefined(typeof(HotkeyAttribute), false) == true)
					{
						HotkeyAttribute[]	attributes = methods[i].GetCustomAttributes(typeof(HotkeyAttribute), false) as HotkeyAttribute[];

						NGHotkeys.AddHotKey(attributes[0].label, type, methods[i].Name);
					}
				}
			}
		}

		[MenuItem(Constants.MenuItemPath + NGHotkeys.SubMenuItemPath + "Edit", priority = 100000)]
		public static void	Open()
		{
			Utility.OpenWindow<NGSettingsWindow>(NGSettingsWindow.Title, callback: w => w.Focus("Hotkeys"));
		}

		public static void	AddHotKey(string name, Type type, string staticMethod)
		{
			for (int i = 0; i < NGHotkeys.shortcuts.Count; i++)
			{
				if (name.CompareTo(NGHotkeys.shortcuts[i].name) <= 0)
				{
					NGHotkeys.shortcuts.Insert(i, new MenuItemShortcut(name, type, staticMethod));
					return;
				}
			}

			NGHotkeys.shortcuts.Add(new MenuItemShortcut(name, type, staticMethod));
		}

		public static void	Invoke(string name, Type type, string staticMethod)
		{
			if (NGHotkeys.DetectDiff(false) == true)
				return;

			if (type != null)
			{
				MethodInfo	method = type.GetMethod(staticMethod, BindingFlags.Public | BindingFlags.Static);

				if (method != null)
				{
					method.Invoke(null, null);
					return;
				}
			}

			if (NGHotkeys.failedOnce == name)
				EditorWindow.GetWindow<NGSettingsWindow>(true, NGSettingsWindow.Title, true).Focus("Hotkeys");
			else
			{
				NGHotkeys.failedOnce = name;
				InternalNGDebug.LogWarning("Calling MenuItem \"" + name + "\" has failed. Please regenerate hotkeys file. (Go to Window/NG Tools/NG Settings/Hotkeys or recall this shortcut)");
			}
		}

		[NGSettings("Hotkeys", 5)]
		private static void	OnGUISettings()
		{
			if (HQ.Settings == null)
				return;

			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();
			{
				EditorGUILayout.BeginVertical(GUILayoutOptionPool.ExpandWidthTrue);
				GUILayout.Label("Bind a MenuItem with a custom hotkey.", GeneralStyles.SmallLabel);
				if (GUILayout.Button("Help", GUILayoutOptionPool.Width(50F)) == true)
					Application.OpenURL("https://docs.unity3d.com/ScriptReference/MenuItem.html");
				EditorGUILayout.EndVertical();

				EditorGUILayout.BeginVertical(GUILayoutOptionPool.Width(130F));
				if (EditorApplication.isCompiling == true)
				{
					using (BgColorContentRestorer.Get(GeneralStyles.HighlightResultButton))
					{
						GUILayout.Button("Compiling...", GeneralStyles.BigButton);
					}
				}
				else
				{
					EditorGUI.BeginDisabledGroup(NGHotkeys.DetectDiff(true) == false);
					if (GUILayout.Button("Save", GeneralStyles.BigButton) == true)
					{
						NGHotkeys.Generate();
						HQ.InvalidateSettings(HQ.Settings, true);
					}
					EditorGUI.EndDisabledGroup();
				}
				EditorGUILayout.EndVertical();
			}
			EditorGUILayout.EndHorizontal();

			Rect	r2 = GUILayoutUtility.GetLastRect();
			r2.x = 2F;
			r2.width += 13F;
			r2.yMin = r2.yMax - 1F;
			EditorGUI.DrawRect(r2, Color.gray);

			CustomHotkeysSettings	settings = HQ.Settings.Get<CustomHotkeysSettings>();

			NGHotkeys.scrollPosition = EditorGUILayout.BeginScrollView(NGHotkeys.scrollPosition);
			{
				for (int i = 0; i < NGHotkeys.shortcuts.Count; i++)
				{
					EditorGUILayout.BeginHorizontal();
					{
						MenuItemShortcut					shortcut = NGHotkeys.shortcuts[i];
						CustomHotkeysSettings.MethodHotkey	hotkey = null;

						for (int j = 0; j < settings.hotkeys.Count; j++)
						{
							if (settings.hotkeys[j].staticMethod == shortcut.@class.FullName + '.' + shortcut.staticMethod)
							{
								hotkey = settings.hotkeys[j];
								break;
							}
						}

						Utility.content.text = shortcut.name;
						Rect	r = GUILayoutUtility.GetRect(Utility.content, GUI.skin.label, GUILayoutOptionPool.Width(170F));
						EditorGUI.PrefixLabel(r, Utility.content);

						if (hotkey != null && string.IsNullOrEmpty(hotkey.bind) == false)
						{
							float	w = r.width;
							r.x -= 1F;
							r.width = 1F;
							EditorGUI.DrawRect(r, Color.cyan);
							r.x += 1F;
							r.width = w;
						}

						bool	hasChanged = false;

						EditorGUI.BeginChangeCheck();
						r.x += r.width;
						r.width = 50F;
						string	bind = EditorGUI.TextField(r, hotkey != null ? hotkey.bind : string.Empty);
						if (EditorGUI.EndChangeCheck() == true)
							hasChanged = true;
						r.x += r.width;

						r.width = 50F;
						EditorGUI.BeginChangeCheck();
						GUI.Toggle(r, bind.Contains("%"), "Ctrl", GeneralStyles.ToolbarToggle);
						if (EditorGUI.EndChangeCheck() == true)
						{
							GUI.FocusControl(null);

							hasChanged = true;

							int	n = bind.IndexOf('%');

							if (n != -1)
								bind = bind.Remove(n, 1);
							else
								bind = '%' + bind;
						}
						r.x += r.width;

						EditorGUI.BeginChangeCheck();
						GUI.Toggle(r, bind.Contains("#"), "Shift", GeneralStyles.ToolbarToggle);
						if (EditorGUI.EndChangeCheck() == true)
						{
							GUI.FocusControl(null);

							hasChanged = true;

							int	n = bind.IndexOf('#');

							if (n != -1)
								bind = bind.Remove(n, 1);
							else
								bind = '#' + bind;
						}
						r.x += r.width;

						EditorGUI.BeginChangeCheck();
						GUI.Toggle(r, bind.Contains("&"), "Alt", GeneralStyles.ToolbarToggle);
						if (EditorGUI.EndChangeCheck() == true)
						{
							GUI.FocusControl(null);

							hasChanged = true;

							int	n = bind.IndexOf('&');

							if (n != -1)
								bind = bind.Remove(n, 1);
							else
								bind = '&' + bind;
						}

						if (hasChanged == true)
						{
							if (string.IsNullOrEmpty(bind) == true)
							{
								if (hotkey != null)
									settings.hotkeys.Remove(hotkey);
							}
							else
							{
								if (hotkey == null)
									settings.hotkeys.Add(new CustomHotkeysSettings.MethodHotkey { staticMethod = shortcut.@class.FullName + '.' + shortcut.staticMethod, bind = bind });
								else
									hotkey.bind = bind;
							}
						}

						GUILayout.FlexibleSpace();
					}
					EditorGUILayout.EndHorizontal();

					GUILayout.Space(5F);
				}
			}
			EditorGUILayout.EndScrollView();
		}

		private static void	Generate()
		{
			if (EditorApplication.isPlaying == true)
			{
				if (EditorUtility.DisplayDialog(Constants.PackageTitle, NGHotkeys.Title + " must stop playing to force a refresh.", "Yes", "No") == true)
					EditorApplication.isPlaying = false;
				else
					return;
			}

			CustomHotkeysSettings	settings = HQ.Settings.Get<CustomHotkeysSettings>();
			StringBuilder			buffer = Utility.GetBuffer();

			buffer.Append(@"// File auto-generated by " + Constants.PackageTitle + @".
using UnityEditor;

namespace NGToolsEditor
{
	internal static class CustomHotkeys
	{");
			for (int i = 0, k = 1; i < settings.hotkeys.Count; i++)
			{
				CustomHotkeysSettings.MethodHotkey	binding = settings.hotkeys[i];

				int	j = 0;

				for (; j < NGHotkeys.shortcuts.Count; j++)
				{
					MenuItemShortcut	shortcut = NGHotkeys.shortcuts[j];

					if (shortcut.@class.FullName + '.' + shortcut.staticMethod == binding.staticMethod)
					{
						buffer.Append(@"
		[MenuItem(Constants.MenuItemPath + NGHotkeys.SubMenuItemPath + """ + shortcut.name + "	_" + binding.bind.Replace("\"", "\\\"") + @""", priority = 10000)]
		public static void	Hotkey" + k++ + @"()
		{
			NGHotkeys.Invoke(Constants.MenuItemPath + NGHotkeys.SubMenuItemPath + """ + shortcut.name + "\", Utility.GetType(\"" + shortcut.@class.Namespace + "\", \"" + shortcut.@class.Name + "\"), \"" + shortcut.staticMethod + @""");
		}
");
						break;
					}
				}

				if (j == NGHotkeys.shortcuts.Count)
					settings.hotkeys.RemoveAt(i--);
			}

			buffer.Append(@"	}
}");

			if (settings.hotkeys.Count > 0)
			{
				string	path = Path.Combine(HQ.RootPath, NGHotkeys.CustomHotkeysPath);
				Directory.CreateDirectory(path);
				File.WriteAllText(Path.Combine(path, NGHotkeys.CustomHotkeysFilename), Utility.ReturnBuffer(buffer));
			}
			else
			{
				string	path = Path.Combine(HQ.RootPath, NGHotkeys.CustomHotkeysPath);
				string	filepath = Path.Combine(path, NGHotkeys.CustomHotkeysFilename);

				AssetDatabase.DeleteAsset(filepath);
				if (Directory.GetFiles(path).Length == 0 && 
					Directory.GetDirectories(path).Length == 0)
				{
					Directory.Delete(path);
				}
			}

			Utility.RecompileUnityEditor();
		}

		private static bool	DetectDiff(bool silent)
		{
			if (HQ.Settings == null || NGHotkeys.isGenerating == true)
				return false;

			CustomHotkeysSettings	settings = HQ.Settings.Get<CustomHotkeysSettings>();
			bool					isDifferent = false;

			Type	type = Utility.GetType("NGToolsEditor", "CustomHotkeys");

			if (type != null)
			{
				MethodInfo[]	methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

				if (methods.Length != settings.hotkeys.Count)
					isDifferent = true;
				else
				{
					MenuItem[][]	attributes = new MenuItem[methods.Length][];

					for (int i = 0; i < methods.Length; i++)
						attributes[i] = methods[i].GetCustomAttributes(typeof(MenuItem), false) as MenuItem[];

					for (int i = 0; i < settings.hotkeys.Count; i++)
					{
						CustomHotkeysSettings.MethodHotkey	binding = settings.hotkeys[i];

						for (int j = 0; j < NGHotkeys.shortcuts.Count; j++)
						{
							MenuItemShortcut	shortcut = NGHotkeys.shortcuts[j];

							if (shortcut.@class.FullName + '.' + shortcut.staticMethod == binding.staticMethod)
							{
								for (int k = 0; k < attributes.Length; k++)
								{
									for (int l = 0; l < attributes[k].Length; l++)
									{
										string	s = Constants.MenuItemPath + NGHotkeys.SubMenuItemPath + shortcut.name + "	_";

										if (attributes[k][l].menuItem.StartsWith(s) == true)
										{
											if (attributes[k][l].menuItem.Substring(s.Length) != binding.bind)
											{
												isDifferent = true;
												goto quadrupleBreaks;
											}

											goto doubleBreaks;
										}
									}
								}

								doubleBreaks:

								break;
							}
						}
					}
				}
			}
			else if (settings.hotkeys.Count > 0)
				isDifferent = true;

			quadrupleBreaks:
			if (isDifferent == true && silent == false)
			{
				EditorApplication.delayCall += () =>
				{
					if (EditorUtility.DisplayDialog(NGHotkeys.Title, "The current hotkeys bindings do not match your settings.\nThis might happen after an update of NG Tools.\n\nDo you want to restore your setup?", LC.G("Yes"), LC.G("No")) == true)
					{
						NGHotkeys.isGenerating = true;
						NGHotkeys.Generate();
						HQ.InvalidateSettings(HQ.Settings, true);
					}
				};
			}

			return isDifferent;
		}
	}
}