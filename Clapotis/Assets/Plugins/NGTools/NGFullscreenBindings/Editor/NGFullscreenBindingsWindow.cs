using NGTools;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGFullscreenBindings
{
	public class NGFullscreenBindingsWindow
	{
		public const string	Title = "NG Fullscreen Bindings";
		public const string	MenuItemPath = Constants.MenuItemPath + NGFullscreenBindingsWindow.Title + "/";
		public const string	TargetScript = "NGFullscreenBindings/Editor/ExternalNGFullscreenBindings.cs";
		
		private static Regex	regex = new Regex("^[a-zA-Z0-9\\s_-]*$");
		private static bool		isGenerating = false;

		[MenuItem(NGFullscreenBindingsWindow.MenuItemPath + "Edit", priority = Constants.MenuItemPriority + 930)]
		public static void	Open()
		{
			Utility.OpenWindow<NGSettingsWindow>(NGSettingsWindow.Title, callback: w => w.Focus(NGFullscreenBindingsWindow.Title));
		}

		public static void	ToggleFullscreen(int f)
		{
			Metrics.UseTool(22); // NGFullscreenBindings

			NGChangeLogWindow.CheckLatestVersion(NGAssemblyInfo.Name);

			if (NGFullscreenBindingsWindow.DetectDiff() == true)
				return;

			if (HQ.Settings == null || f > HQ.Settings.Get<FullscreenBindingsSettings>().bindings.Length)
			{
				EditorUtility.DisplayDialog(Constants.PackageTitle, "The binding is invalid. Please regenerate it.", "OK");
				NGFullscreenBindingsWindow.Open();
				return;
			}

			Type	type = NGFullscreenBindingsWindow.GetType(HQ.Settings.Get<FullscreenBindingsSettings>().bindings[f - 1].type);

			if (type == null)
			{
				EditorUtility.DisplayDialog(Constants.PackageTitle, "The binding has been assigned an invalid Window.", "OK");
				NGFullscreenBindingsWindow.Open();
				return;
			}

			InternalNGDebug.VerboseLogFormat("Toggling fullscreen window \"{0}\".", type);

			EditorWindow[]	windows = Resources.FindObjectsOfTypeAll<EditorWindow>();

			for (int i = 0; i < windows.Length; i++)
			{
				if (windows[i].GetType() != type && windows[i].maximized == true)
				{
					windows[i].maximized = false;
					break;
				}
			}

			EditorWindow	window = EditorWindow.GetWindow(type);

			EditorApplication.delayCall += () => window.maximized = !window.maximized;
		}

		[NGSettings(NGFullscreenBindingsWindow.Title)]
		private static void	OnGUISettings()
		{
			if (HQ.Settings == null)
				return;

			EditorGUILayout.Space();

			FullscreenBindingsSettings	settings = HQ.Settings.Get<FullscreenBindingsSettings>();

			// Rebuild and restore as much as we can the previous bindings.
			if (settings.bindings == null ||
				settings.bindings.Length != 12)
			{
				FullscreenBindingsSettings.Binding[]	newBindings = new FullscreenBindingsSettings.Binding[12];
				Type									type = Utility.GetType("NGToolsEditor.NGFullscreenBindings", "ExternalNGFullscreenBindings");

				if (settings.bindings != null)
				{
					for (int i = 0; i < settings.bindings.Length && i < newBindings.Length; i++)
					{
						newBindings[i] = new FullscreenBindingsSettings.Binding(settings.bindings[i].label, settings.bindings[i].type) {
							active = settings.bindings[i].active,
							ctrl = settings.bindings[i].ctrl,
							shift = settings.bindings[i].shift,
							alt = settings.bindings[i].alt,
						};
					}

					for (int i = 0; i < settings.bindings.Length; i++)
					{
						FieldInfo	field = type.GetField("F" + (i + 1));

						if (field != null)
							newBindings[i] = new FullscreenBindingsSettings.Binding((string)field.GetRawConstantValue(), string.Empty) { active = type.GetMethod("ToggleFullscreenF" + (i + 1)) != null };
					}
				}

				for (int i = 0; i < newBindings.Length; i++)
				{
					if (newBindings[i] == null)
						newBindings[i] = new FullscreenBindingsSettings.Binding(string.Empty, string.Empty);
				}

				settings.bindings = newBindings;
			}

			using (LabelWidthRestorer.Get(30F))
			{
				for (int i = 0; i < settings.bindings.Length; i++)
				{
					FullscreenBindingsSettings.Binding	binding = settings.bindings[i];

					EditorGUILayout.BeginHorizontal();
					{
						binding.active = EditorGUILayout.Toggle(binding.active, GUILayoutOptionPool.Width(12F));

						EditorGUI.BeginDisabledGroup(binding.active == false);
						{
							GUILayout.Label("F" + (i + 1), GUILayoutOptionPool.Width(25F));

							binding.ctrl = GUILayout.Toggle(binding.ctrl, "Ctrl", "ToolbarButton", GUILayoutOptionPool.Width(35F));
							binding.shift = GUILayout.Toggle(binding.shift, "Shift", "ToolbarButton", GUILayoutOptionPool.Width(35F));
							binding.alt = GUILayout.Toggle(binding.alt, "Alt", "ToolbarButton", GUILayoutOptionPool.Width(35F));
							binding.label = EditorGUILayout.TextField(binding.label);

							GUILayout.FlexibleSpace();

							Type	t = NGFullscreenBindingsWindow.GetType(settings.bindings[i].type);
							if (t != null)
								GUILayout.Label(t.Name, GUILayoutOptionPool.ExpandWidthFalse);

							if (GUILayout.Button("Pick", GUILayoutOptionPool.Width(50F)) == true)
								NGFullscreenBindingsWindow.PickType(i);
						}
						EditorGUI.EndDisabledGroup();
					}
					EditorGUILayout.EndHorizontal();

					if (NGFullscreenBindingsWindow.regex.IsMatch(binding.label) == false)
						EditorGUILayout.HelpBox("Must contains only alpha numeric chars, space, tab, dash, underscore.", MessageType.Error, true);

					if (i == 0 && binding.ctrl == false && binding.shift == false && binding.alt == true)
						EditorGUILayout.HelpBox("This binding is already used. You must change it.", MessageType.Error, true);
				}
			}

			if (EditorApplication.isCompiling == true)
			{
				using (BgColorContentRestorer.Get(GeneralStyles.HighlightResultButton))
				{
					GUILayout.Button("Compiling...");
				}
			}
			else if (GUILayout.Button("Save") == true)
			{
				HQ.InvalidateSettings();
				NGFullscreenBindingsWindow.Generate();
			}
		}

		private static Type	GetType(string raw)
		{
			if (string.IsNullOrEmpty(raw) == true)
				return null;

			Type	type = Utility.GetType(raw);

			if (type == null)
			{
				int	n = raw.IndexOf(',');

				if (n != -1)
				{
					string	fullName = raw.Substring(0, n);

					type = Utility.GetType(fullName);
				}
			}

			return type;
		}

		private static bool	DetectDiff()
		{
			if (HQ.Settings == null || NGFullscreenBindingsWindow.isGenerating == true)
				return false;

			Type						type = Utility.GetType("NGToolsEditor.NGFullscreenBindings", "ExternalNGFullscreenBindings");
			FullscreenBindingsSettings	settings = HQ.Settings.Get<FullscreenBindingsSettings>();

			for (int i = 0; i < settings.bindings.Length; i++)
			{
				bool		changeDetected = false;
				FieldInfo	field = type.GetField("F" + (i + 1));

				if (field == null)
					changeDetected = true;
				else
				{
					TypeWitnessAttribute[]	attr = field.GetCustomAttributes(typeof(TypeWitnessAttribute), false) as TypeWitnessAttribute[];

					changeDetected = settings.bindings[i].Equals(new FullscreenBindingsSettings.Binding((string)field.GetRawConstantValue(), attr.Length > 0 ? attr[0].type : string.Empty) { active = type.GetMethod("ToggleFullscreenF" + (i + 1)) != null }) == false;
				}

				if (changeDetected == true)
				{
					InternalNGDebug.VerboseLog(NGFullscreenBindingsWindow.Title + " has detected a change between the code file and the configuration.");

					EditorApplication.delayCall += () =>
					{
						if (EditorUtility.DisplayDialog(NGFullscreenBindingsWindow.Title, "The current fullscreen bindings do not match your settings.\nThis might happen after an update of NG Tools.\n\nDo you want to restore your setup?", LC.G("Yes"), LC.G("No")) == true)
						{
							NGFullscreenBindingsWindow.isGenerating = true;
							NGFullscreenBindingsWindow.Generate();
						}
					};
					return true;
				}
			}

			return false;
		}

		private static void	PickType(int i)
		{
			FullscreenBindingsSettings	settings = HQ.Settings.Get<FullscreenBindingsSettings>();
			GenericTypesSelectorWizard	wizard = GenericTypesSelectorWizard.Start(NGFullscreenBindingsWindow.Title, typeof(EditorWindow), (t) =>
			{
				if (t != null)
					settings.bindings[i].type = t.GetShortAssemblyType();
				else
					settings.bindings[i].type = string.Empty;
				HQ.InvalidateSettings();
				Utility.RepaintEditorWindow(typeof(NGSettingsWindow));
			}, true, true);
			wizard.EnableNullValue = true;
			wizard.SelectedType = Type.GetType(settings.bindings[i].type);
		}

		private static void	Generate()
		{
			FullscreenBindingsSettings	settings = HQ.Settings.Get<FullscreenBindingsSettings>();
			StringBuilder				buffer = Utility.GetBuffer();

			buffer.Append(@"// File auto-generated by NGFullscreenBindingsWindow.
using UnityEditor;

namespace NGToolsEditor.NGFullscreenBindings
{
	public static class ExternalNGFullscreenBindings
	{");
			for (int i = 0; i < settings.bindings.Length; i++)
			{
				FullscreenBindingsSettings.Binding	binding = settings.bindings[i];

				buffer.AppendLine(@"
		[TypeWitness(""" + settings.bindings[i].type + "\")]");
				buffer.Append("		public const string	F" + (i + 1) + " = \"" + binding.label);

				buffer.Append(' ');
				if (binding.ctrl == true || binding.shift == true || binding.alt == true)
				{
					if (binding.ctrl == true)
						buffer.Append('%');
					if (binding.shift == true)
						buffer.Append('#');
					if (binding.alt == true)
						buffer.Append('&');
				}
				else
					buffer.Append('_');

				buffer.Append(@""";
");

				if (binding.CanBeInMenu() == true)
				{
					buffer.Append(@"
		[MenuItem(NGFullscreenBindingsWindow.MenuItemPath + ExternalNGFullscreenBindings.F" + (i + 1) + " + " + "\"F" + (i + 1) + '"' + ", priority = Constants.MenuItemPriority + " + (900 + i + 1) + @")]
		public static void	ToggleFullscreenF" + (i + 1) + @"()
		{
			NGFullscreenBindingsWindow.ToggleFullscreen(" + (i + 1) + @");
		}
");
				}
			}

			buffer.Append(@"	}
}");

			File.WriteAllText(HQ.RootPath + "/" + NGFullscreenBindingsWindow.TargetScript, Utility.ReturnBuffer(buffer));

			if (EditorApplication.isPlaying == true)
			{
				if (EditorUtility.DisplayDialog(Constants.PackageTitle, NGFullscreenBindingsWindow.Title + " must stop playing to force a refresh.", "Yes", "No") == true)
					EditorApplication.isPlaying = false;
				else
					return;
			}

			Utility.RecompileUnityEditor();
		}
	}
}