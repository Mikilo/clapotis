using NGTools;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	public static class ConsoleSettingsEditor
	{
		public enum MainTab
		{
			General,
			Inputs,
			Themes,
			Presets,
		}

		public enum GeneralTab
		{
			General,
			Log,
			StackTrace
		}

		private static readonly Color	HighlightInput = new Color(.4F, 9F, .25F);

		public static MainTab		currentTab = MainTab.General;
		public static GeneralTab	currentGeneralTab = GeneralTab.General;
		public static Vector2		generalGeneralScrollPosition;
		public static Vector2		generalLogScrollPosition;
		public static Vector2		generalStackTraceScrollPosition;

		private static int		selectedInputsGroup = 0;

		private static Type[]	completeThemeTypes;
		private static string[]	completeThemeNames;
		private static Type[]	themeTypes;
		private static string[]	themeNames;
		private static Type[]	presetTypes;
		private static string[]	presetNames;

		private static Vector2			inputScrollPosition;
		private static List<GUITimer>	testInputAnimationFeedback;
		private static InputCommand		registeringCommand;
		private static bool				shiftPressed;

		private static SectionDrawer	sectionGeneral;
		private static SectionDrawer	sectionLog;
		private static SectionDrawer	sectionStackTrace;

		private static GUIStyle	menuButtonStyle;

		static	ConsoleSettingsEditor()
		{
			ConsoleSettingsEditor.inputScrollPosition = new Vector2();
			ConsoleSettingsEditor.testInputAnimationFeedback = new List<GUITimer>();

			ConsoleSettingsEditor.sectionGeneral = new SectionDrawer(typeof(GeneralSettings));
			ConsoleSettingsEditor.sectionLog = new SectionDrawer(typeof(LogSettings));
			ConsoleSettingsEditor.sectionStackTrace = new SectionDrawer(typeof(StackTraceSettings));
		}

		//public void	Uninit()
		//{
		//	NGSettingsWindow.RemoveSection(NGConsoleWindow.Title);

		//	ConsoleSettingsEditor.sectionGeneral.Uninit();
		//	ConsoleSettingsEditor.sectionLog.Uninit();
		//	ConsoleSettingsEditor.sectionStackTrace.Uninit();
		//}

		[NGSettings(NGConsoleWindow.Title, 10)]
		private static void	OnGUI()
		{
			if (HQ.Settings == null)
			{
				GUILayout.Label(LC.G("ConsoleSettings_NullTarget"));
				return;
			}

			if (ConsoleSettingsEditor.menuButtonStyle == null)
				ConsoleSettingsEditor.menuButtonStyle = new GUIStyle("ToolbarButton");

			GUILayout.BeginHorizontal();
			{
				if (GUILayout.Toggle(ConsoleSettingsEditor.currentTab == MainTab.General, LC.G("ConsoleSettings_General"), ConsoleSettingsEditor.menuButtonStyle) == true)
					ConsoleSettingsEditor.currentTab = MainTab.General;
				if (GUILayout.Toggle(ConsoleSettingsEditor.currentTab == MainTab.Inputs, LC.G("ConsoleSettings_Inputs"), ConsoleSettingsEditor.menuButtonStyle) == true)
					ConsoleSettingsEditor.currentTab = MainTab.Inputs;
				if (GUILayout.Toggle(ConsoleSettingsEditor.currentTab == MainTab.Themes, LC.G("ConsoleSettings_Themes"), ConsoleSettingsEditor.menuButtonStyle) == true)
					ConsoleSettingsEditor.currentTab = MainTab.Themes;
				if (GUILayout.Toggle(ConsoleSettingsEditor.currentTab == MainTab.Presets, LC.G("ConsoleSettings_Presets"), ConsoleSettingsEditor.menuButtonStyle) == true)
					ConsoleSettingsEditor.currentTab = MainTab.Presets;
			}
			GUILayout.EndHorizontal();

			EditorGUI.BeginChangeCheck();
			{
				if (ConsoleSettingsEditor.currentTab == MainTab.General)
					ConsoleSettingsEditor.OnGUIGeneral();
				else if (ConsoleSettingsEditor.currentTab == MainTab.Inputs)
					ConsoleSettingsEditor.OnGUIInputs();
				else if (ConsoleSettingsEditor.currentTab == MainTab.Themes)
					ConsoleSettingsEditor.OnGUIThemes();
				else if (ConsoleSettingsEditor.currentTab == MainTab.Presets)
					ConsoleSettingsEditor.OnGUIPresets();
			}
			if (EditorGUI.EndChangeCheck() == true)
				HQ.InvalidateSettings();
		}

		private static void	OnGUIGeneral()
		{
			GUILayout.BeginHorizontal();
			{
				if (GUILayout.Toggle(ConsoleSettingsEditor.currentGeneralTab == GeneralTab.General, LC.G("ConsoleSettings_General_General"), ConsoleSettingsEditor.menuButtonStyle) == true)
					ConsoleSettingsEditor.currentGeneralTab = GeneralTab.General;
				if (GUILayout.Toggle(ConsoleSettingsEditor.currentGeneralTab == GeneralTab.Log, LC.G("ConsoleSettings_General_Log"), ConsoleSettingsEditor.menuButtonStyle) == true)
					ConsoleSettingsEditor.currentGeneralTab = GeneralTab.Log;
				if (GUILayout.Toggle(ConsoleSettingsEditor.currentGeneralTab == GeneralTab.StackTrace, LC.G("ConsoleSettings_General_StackTrace"), ConsoleSettingsEditor.menuButtonStyle) == true)
					ConsoleSettingsEditor.currentGeneralTab = GeneralTab.StackTrace;
			}
			GUILayout.EndHorizontal();

			if (ConsoleSettingsEditor.currentGeneralTab == GeneralTab.General)
				ConsoleSettingsEditor.OnGUIGeneralGeneral();
			else if (ConsoleSettingsEditor.currentGeneralTab == GeneralTab.Log)
				ConsoleSettingsEditor.OnGUIGeneralLog();
			else if (ConsoleSettingsEditor.currentGeneralTab == GeneralTab.StackTrace)
				ConsoleSettingsEditor.OnGUIGeneralStackTrace();
		}

		private static void	OnGUIGeneralGeneral()
		{
			ConsoleSettingsEditor.generalGeneralScrollPosition = EditorGUILayout.BeginScrollView(ConsoleSettingsEditor.generalGeneralScrollPosition);
			{
				ConsoleSettingsEditor.sectionGeneral.OnGUI();
				GUILayout.Space(10F);
			}
			EditorGUILayout.EndScrollView();
		}

		private static void	OnGUIGeneralLog()
		{
			LogSettings	settings = HQ.Settings.Get<LogSettings>();

			ConsoleSettingsEditor.generalLogScrollPosition = EditorGUILayout.BeginScrollView(ConsoleSettingsEditor.generalLogScrollPosition);
			{
				ConsoleSettingsEditor.sectionLog.OnGUI();

				GUILayout.Space(10F);
			}
			EditorGUILayout.EndScrollView();

			EditorGUILayout.BeginVertical(GUILayoutOptionPool.Height(16F + 5F * settings.height));
			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			EditorGUILayout.LabelField("Preview :");
			EditorGUILayout.EndHorizontal();

			ConsoleSettingsEditor.DrawRow(0, 0, false, "A selected row.", "");
			ConsoleSettingsEditor.DrawRow(1, 0, false, "A normal even row.", "1");
			ConsoleSettingsEditor.DrawRow(2, 1, false, "A warning odd row.", "23");
			ConsoleSettingsEditor.DrawRow(3, 2, false, "An error even row.", "456");
			ConsoleSettingsEditor.DrawRow(4, 3, false, "An exception odd row.", "7890");

			EditorGUILayout.EndVertical();
		}

		private static void	DrawRow(int i, int foldType, bool fold, string content, string collapseCount)
		{
			LogSettings	settings = HQ.Settings.Get<LogSettings>();
			Rect		r = GUILayoutUtility.GetRect(0F, settings.height, settings.Style);
			float		originX = r.x;
			float		originWidth = r.width;

			if (Event.current.type == EventType.Repaint)
			{
				if (i == 0)
					EditorGUI.DrawRect(r, settings.selectedBackground);
				else if ((i & 1) == 0)
					EditorGUI.DrawRect(r, settings.evenBackground);
				else
					EditorGUI.DrawRect(r, settings.oddBackground);
			}

			r.x += 2F;
			r.width = 16F;

			Color	foldoutColor = Color.white;
			bool	isDefaultLog = false;

			if (foldType == 1)
				foldoutColor = ConsoleConstants.WarningFoldoutColor;
			else if (foldType == 2)
				foldoutColor = ConsoleConstants.ErrorFoldoutColor;
			else if (foldType == 3)
				foldoutColor = ConsoleConstants.ExceptionFoldoutColor;
			else
				isDefaultLog = true;

			using (BgColorContentRestorer.Get(!isDefaultLog, foldoutColor))
			{
				EditorGUI.Foldout(r, fold, string.Empty);
			}

			r.x -= 2F;
			r.width = 3F;

			if (isDefaultLog == false)
				EditorGUI.DrawRect(r, foldoutColor);

			r.width = 16F;

			r.xMin += r.width;

			// Draw time.
			if (settings.displayTime == true)
			{
				Utility.content.text = DateTime.Now.ToString(settings.timeFormat);
				r.width = settings.TimeStyle.CalcSize(Utility.content).x;
				GUI.Label(r, Utility.content, settings.TimeStyle);
				r.x += r.width;
			}

			// Draw frame count.
			if (settings.displayFrameCount == true)
			{
				Utility.content.text = Time.frameCount.ToString();
				r.width = settings.TimeStyle.CalcSize(Utility.content).x;
				GUI.Label(r, Utility.content, settings.TimeStyle);
				r.x += r.width;
			}

			// Draw rendered frame count.
			if (settings.displayRenderedFrameCount == true)
			{
				Utility.content.text = Time.renderedFrameCount.ToString();
				r.width = settings.TimeStyle.CalcSize(Utility.content).x;
				GUI.Label(r, Utility.content, settings.TimeStyle);
				r.x += r.width;
			}
			r.width = originWidth - (r.x - originX);

			GUI.Button(r, content, settings.Style);

			if (collapseCount != string.Empty)
			{
				// Draw collapse count.
				Utility.content.text = collapseCount;
				r.xMin += r.width - settings.CollapseLabelStyle.CalcSize(Utility.content).x;
				GUI.Label(r, Utility.content, settings.CollapseLabelStyle);
			}
		}

		private static void	OnGUIGeneralStackTrace()
		{
			StackTraceSettings	stackTraceSettings = HQ.Settings.Get<StackTraceSettings>();
			LogSettings			logSettings = HQ.Settings.Get<LogSettings>();

			ConsoleSettingsEditor.generalStackTraceScrollPosition = EditorGUILayout.BeginScrollView(ConsoleSettingsEditor.generalStackTraceScrollPosition);
			{
				EditorGUI.BeginChangeCheck();
				ConsoleSettingsEditor.sectionStackTrace.OnGUI();
				if (EditorGUI.EndChangeCheck() == true)
				{
					LogConditionParser.cachedFrames.Clear();
					LogConditionParser.cachedFramesArrays.Clear();
					MainModule.methodsCategories.Clear();
				}

				GUILayout.Space(10F);
			}
			EditorGUILayout.EndScrollView();

			EditorGUILayout.BeginVertical(GUILayoutOptionPool.Height(16F + 2 * logSettings.height + 8 * Constants.SingleLineHeight));
			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			EditorGUILayout.LabelField("Preview :");
			EditorGUILayout.EndHorizontal();

			if (stackTraceSettings.skipUnreachableFrame == true)
				ConsoleSettingsEditor.DrawStackFrame(0, "Sub Frame.", "Assets/An/Existing/File.cs", true);
			else
			{
				ConsoleSettingsEditor.DrawStackFrame(0, "Top Frame.", "A/File/Somewhere/That/Does/Not/Exist.cs", false);
				ConsoleSettingsEditor.DrawStackFrame(1, "Sub Frame.", "Assets/An/Existing/File.cs", true);
			}

			ConsoleSettingsEditor.DrawStackFrameCode(1, false, "using UnityEngine;");
			ConsoleSettingsEditor.DrawStackFrameCode(2, false, "private static class Foo : Object");
			ConsoleSettingsEditor.DrawStackFrameCode(3, false, "{");
			ConsoleSettingsEditor.DrawStackFrameCode(4, false, "	public internal void	Func(Vector2 v)");
			ConsoleSettingsEditor.DrawStackFrameCode(5, false, "	{");
			ConsoleSettingsEditor.DrawStackFrameCode(6, true, "		Debug.Log(\"Someting\");");
			ConsoleSettingsEditor.DrawStackFrameCode(7, false, "	}");
			ConsoleSettingsEditor.DrawStackFrameCode(8, false, "}");

			EditorGUILayout.EndVertical();
		}

		private static void	DrawStackFrameCode(int i, bool mainLine, string content)
		{
			StackTraceSettings	settings = HQ.Settings.Get<StackTraceSettings>();
			Rect				r = GUILayoutUtility.GetRect(0F, settings.previewHeight, settings.PreviewSourceCodeStyle);

			if (Event.current.type == EventType.Repaint)
			{
				if (mainLine == false)
					EditorGUI.DrawRect(r, settings.previewSourceCodeBackgroundColor);
				else
					EditorGUI.DrawRect(r, settings.previewSourceCodeMainLineBackgroundColor);
			}

			GUI.Label(r, Utility.Color(i.ToString(), settings.previewLineColor) + ConsoleUtility.ColorLine(content), settings.PreviewSourceCodeStyle);
			r.y += r.height;
		}

		private static void	DrawStackFrame(int i, string a, string filepath, bool fileExist)
		{
			StackTraceSettings	settings = HQ.Settings.Get<StackTraceSettings>();
			Rect				r = GUILayoutUtility.GetRect(0F, settings.height, settings.Style);
			float				width = r.width;

			// Substract viewRect to avoid scrollbar.
			r.height = settings.height;

			// Display the stack trace.
			r.width = width - 16F;
			FrameBuilder.Clear();
			FrameBuilder.returnType = "Void";
			FrameBuilder.namespaceName = "namespace";
			FrameBuilder.classType = "class";
			FrameBuilder.methodName = "func";
			FrameBuilder.fileName = filepath;
			FrameBuilder.fileExist = fileExist;
			FrameBuilder.line = 1234;
			FrameBuilder.parameterTypes.Add("Rect");
			FrameBuilder.parameterNames.Add("r");
			FrameBuilder.parameterTypes.Add("int");
			FrameBuilder.parameterNames.Add("a");

			if (i == 0)
				GUI.Button(r, FrameBuilder.ToString("→ ", settings), settings.Style);
			else
				GUI.Button(r, FrameBuilder.ToString("↑ ", settings), settings.Style);

			r.x = r.width;
			r.width = 16F;
			GUI.Button(r, "+", settings.Style);

			r.y += r.height;
		}

		private static void	OnGUIThemes()
		{
			Type[]		types;
			string[]	names;

			if (ConsoleSettingsEditor.themeTypes == null)
			{
				List<Type>		completeThemeTypes = new List<Type>(2);
				List<string>	completeThemeNames = new List<string>(2);
				List<Type>		themeTypes = new List<Type>(2);
				List<string>	themeNames = new List<string>(2);

				foreach (Type c in Utility.EachNGTSubClassesOf(typeof(Theme)))
				{
					if (c.IsDefined(typeof(CompleteThemeAttribute), false) == true)
					{
						completeThemeTypes.Add(c);
						completeThemeNames.Add(Utility.NicifyVariableName(c.Name));
					}
					else
					{
						themeTypes.Add(c);
						themeNames.Add(Utility.NicifyVariableName(c.Name));
					}
				}

				ConsoleSettingsEditor.completeThemeTypes = completeThemeTypes.ToArray();
				ConsoleSettingsEditor.completeThemeNames = completeThemeNames.ToArray();

				ConsoleSettingsEditor.themeTypes = themeTypes.ToArray();
				ConsoleSettingsEditor.themeNames = themeNames.ToArray();
			}

			for (int j = 0; j < 2; j++)
			{
				if (j == 0)
				{
					GUILayout.Label("Main Themes");
					types = ConsoleSettingsEditor.completeThemeTypes;
					names = ConsoleSettingsEditor.completeThemeNames;
				}
				else
				{
					GUILayout.Label("Partial Themes");
					types = ConsoleSettingsEditor.themeTypes;
					names = ConsoleSettingsEditor.themeNames;
				}

				for (int i = 0; i < types.Length; i++)
				{
					if (GUILayout.Button(names[i]) == true &&
						((Event.current.modifiers & Constants.ByPassPromptModifier) != 0 || EditorUtility.DisplayDialog(names[i], LC.G("NGSettings_ConfirmApply"), LC.G("Yes"), LC.G("No")) == true))
					{
						Theme	theme = Activator.CreateInstance(types[i]) as Theme;

						theme.SetTheme(HQ.Settings);

						EditorUtility.UnloadUnusedAssetsImmediate();
						HQ.InvalidateSettings();
						InternalNGDebug.Log("Theme \"" + names[i] + "\" applied on " + HQ.Settings + ".");
					}
				}
			}
		}

		private static void	OnGUIPresets()
		{
			if (ConsoleSettingsEditor.presetTypes == null)
			{
				List<Type>		types = new List<Type>(4);
				List<string>	names = new List<string>(4);

				foreach (Type c in Utility.EachNGTSubClassesOf(typeof(Preset)))
				{
					types.Add(c);
					names.Add(Utility.NicifyVariableName(c.Name));
				}

				ConsoleSettingsEditor.presetTypes = types.ToArray();
				ConsoleSettingsEditor.presetNames = names.ToArray();
			}

			for (int i = 0; i < ConsoleSettingsEditor.presetTypes.Length; i++)
			{
				if (GUILayout.Button(ConsoleSettingsEditor.presetNames[i]) == true &&
					((Event.current.modifiers & Constants.ByPassPromptModifier) != 0 || EditorUtility.DisplayDialog(ConsoleSettingsEditor.presetNames[i], LC.G("NGSettings_ConfirmApply"), LC.G("Yes"), LC.G("No")) == true))
				{
					Preset	preset = Activator.CreateInstance(ConsoleSettingsEditor.presetTypes[i]) as Preset;

					preset.SetSettings(HQ.Settings);

					EditorUtility.UnloadUnusedAssetsImmediate();
					HQ.InvalidateSettings();
					InternalNGDebug.Log("Preset \"" + ConsoleSettingsEditor.presetNames[i] + "\" applied on " + HQ.Settings + ".");
				}
			}
		}

		private static void	OnGUIInputs()
		{
			ConsoleSettings	settings = HQ.Settings.Get<ConsoleSettings>();

			for (int n = 0, i = 0; i < settings.inputsManager.groups.Count; ++i, ++n)
			{
				if (GUILayout.Toggle(i == ConsoleSettingsEditor.selectedInputsGroup, LC.G("InputGroup_" + settings.inputsManager.groups[i].name), ConsoleSettingsEditor.menuButtonStyle) == true)
					ConsoleSettingsEditor.selectedInputsGroup = i;
			}

			if (ConsoleSettingsEditor.selectedInputsGroup < settings.inputsManager.groups.Count)
			{
				ConsoleSettingsEditor.inputScrollPosition = EditorGUILayout.BeginScrollView(ConsoleSettingsEditor.inputScrollPosition);
				{
					for (int n = 0, j = 0; j < settings.inputsManager.groups[ConsoleSettingsEditor.selectedInputsGroup].commands.Count; ++j, ++n)
					{
						while (n >= ConsoleSettingsEditor.testInputAnimationFeedback.Count)
						{
							var	af = new AnimFloat(0F, EditorWindow.focusedWindow.Repaint);
							af.speed = 1F;
							af.target = 0F;
							ConsoleSettingsEditor.testInputAnimationFeedback.Add(new GUITimer(EditorWindow.focusedWindow.Repaint, Constants.CheckFadeoutCooldown, 0F));
						}

						if (settings.inputsManager.groups[ConsoleSettingsEditor.selectedInputsGroup].commands[j].Check() == true)
						{
							ConsoleSettingsEditor.testInputAnimationFeedback[n].Start();
							ConsoleSettingsEditor.testInputAnimationFeedback[n].af.valueChanged.RemoveAllListeners();
							ConsoleSettingsEditor.testInputAnimationFeedback[n].af.valueChanged.AddListener(EditorWindow.focusedWindow.Repaint);
						}

						if (ConsoleSettingsEditor.testInputAnimationFeedback[n].Value > 0F)
						{
							using (ColorContentRestorer.Get(Color.Lerp(GUI.contentColor, ConsoleSettingsEditor.HighlightInput, ConsoleSettingsEditor.testInputAnimationFeedback[n].Value)))
								ConsoleSettingsEditor.DrawInputCommand(settings.inputsManager.groups[ConsoleSettingsEditor.selectedInputsGroup].commands[j]);
						}
						else
							ConsoleSettingsEditor.DrawInputCommand(settings.inputsManager.groups[ConsoleSettingsEditor.selectedInputsGroup].commands[j]);
					}

					GUILayout.Space(10F);
				}
				EditorGUILayout.EndScrollView();
			}
		}

		private static void	DrawInputCommand(InputCommand command)
		{
			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Label(LC.G("Input_" + command.name), GeneralStyles.Title1, GUILayoutOptionPool.ExpandWidthFalse);

				if (ConsoleSettingsEditor.registeringCommand == command)
				{
					if (Event.current.type == EventType.KeyUp)
					{
						if (Event.current.keyCode == KeyCode.LeftControl || Event.current.keyCode == KeyCode.RightControl)
							command.modifiers ^= InputCommand.Control;
						else if (Event.current.keyCode == KeyCode.LeftShift || Event.current.keyCode == KeyCode.RightShift)
							command.modifiers ^= InputCommand.Shift;
						else if (Event.current.keyCode == KeyCode.LeftAlt || Event.current.keyCode == KeyCode.RightAlt)
							command.modifiers ^= InputCommand.Alt;
						else
							command.keyCode = Event.current.keyCode;

						Event.current.Use();
					}
					else if (Event.current.shift == true)
						ConsoleSettingsEditor.shiftPressed = true;
					else if (ConsoleSettingsEditor.shiftPressed == true)
					{
						command.modifiers ^= InputCommand.Shift;
						ConsoleSettingsEditor.shiftPressed = false;
					}

					if (GUILayout.Button(LC.G("Stop"), GUILayoutOptionPool.MaxWidth(80F)) == true)
						registeringCommand = null;

					GUILayout.Label(LC.G("ConsoleSettings_PressAny"));

					// Force repaint to handle shift input.
					//ConsoleSettingsEditor.Repaint();
				}
				else if (GUILayout.Button(LC.G("Edit"), GUILayoutOptionPool.MaxWidth(80F)) == true)
				{
					Utility.content.text = LC.G("InputsWizard_PressAnythingToEditCommand");
					registeringCommand = command;
					ConsoleSettingsEditor.shiftPressed = false;
				}
			}
			EditorGUILayout.EndHorizontal();

			string	description = LC.G("Input_" + command.name + InputCommand.DescriptionLocalizationSuffix);

			if (string.IsNullOrEmpty(description) == false)
				EditorGUILayout.LabelField(description, GeneralStyles.WrapLabel);

			EditorGUILayout.BeginHorizontal();
			{
				command.keyCode = (KeyCode)EditorGUILayout.EnumPopup(command.keyCode, GUILayoutOptionPool.Width(130F));

				EditorGUI.BeginChangeCheck();
				GUILayout.Toggle((command.modifiers & InputCommand.Control) != 0, "Ctrl", ConsoleSettingsEditor.menuButtonStyle, GUILayoutOptionPool.Width(50F));
				if (EditorGUI.EndChangeCheck() == true)
					command.modifiers ^= InputCommand.Control;

				EditorGUI.BeginChangeCheck();
				GUILayout.Toggle((command.modifiers & InputCommand.Shift) != 0, "Shift", ConsoleSettingsEditor.menuButtonStyle, GUILayoutOptionPool.Width(50F));
				if (EditorGUI.EndChangeCheck() == true)
					command.modifiers ^= InputCommand.Shift;

				EditorGUI.BeginChangeCheck();
				GUILayout.Toggle((command.modifiers & InputCommand.Alt) != 0, "Alt", ConsoleSettingsEditor.menuButtonStyle, GUILayoutOptionPool.Width(50F));
				if (EditorGUI.EndChangeCheck() == true)
					command.modifiers ^= InputCommand.Alt;
			}
			EditorGUILayout.EndHorizontal();
		}
	}
}