using System;
using UnityEditor;
using UnityEditorInternal;

namespace NGToolsEditor.NGInspectorGadget
{
	using UnityEngine;

	[InitializeOnLoad]
	[CustomEditor(typeof(MonoScript))]
	public class MonoScriptEditor : Editor
	{
		public const int	MaxChars = (UInt16.MaxValue >> 2) - 1;

		private Vector2	scrollPos;
		private string	content;

		static	MonoScriptEditor()
		{
			NGInspectorGadget.GUISettings += MonoScriptEditor.OnGUISettings;

			HQ.SettingsChanged += () =>
			{
				if (HQ.Settings == null)
					return;

				if (HQ.Settings.Get<InspectorGadgetSettings>().activeScriptVisualizer == true)
				{
					if (Utility.AddCustomEditor(typeof(MonoScript), typeof(MonoScriptEditor)) == true)
						MonoScriptEditor.UpdateInspector();
				}
				else if (Utility.RemoveCustomEditor(typeof(MonoScriptEditor)) == true)
					MonoScriptEditor.UpdateInspector();
			};
		}

		private static void	OnGUISettings()
		{
			if (Utility.IsCustomEditorCompatible == false)
			{
				EditorGUILayout.HelpBox("NG Tools has detected a change in Unity code. Please contact the author.", MessageType.Error);

				if (GUILayout.Button("Contact the author") == true)
					ContactFormWizard.Open(ContactFormWizard.Subject.BugReport, "MonoScriptEditor is incompatible with " + Utility.UnityVersion + ".");
				return;
			}

			InspectorGadgetSettings	settings = HQ.Settings.Get<InspectorGadgetSettings>();

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Script visualizer lets you select the text when you inspect a script.", GeneralStyles.WrapLabel);
			bool	active = NGEditorGUILayout.Switch("Script Visualizer", settings.activeScriptVisualizer);
			if (EditorGUI.EndChangeCheck() == true)
			{
				bool	mustUpdate = active == true ? Utility.AddCustomEditor(typeof(MonoScript), typeof(MonoScriptEditor)) : Utility.RemoveCustomEditor(typeof(MonoScriptEditor));

				if (mustUpdate == true)
					MonoScriptEditor.UpdateInspector();

				settings.activeScriptVisualizer = active;
				HQ.InvalidateSettings();
			}
		}

		private static void	UpdateInspector()
		{
			if (Selection.objects.Length != 1 || (Selection.objects[0] is MonoScript) == false)
				return;

			Object[]	o = Selection.objects;
			Selection.objects = new Object[0];
			InternalEditorUtility.RepaintAllViews();

			EditorApplication.delayCall += () => Selection.objects = o;
		}

		public override bool	UseDefaultMargins()
		{
			return false;
		}

		public override void	OnInspectorGUI()
		{
			if (string.IsNullOrEmpty(this.content) == true)
			{
				if ((this.target as MonoScript).text.Length >= MonoScriptEditor.MaxChars)
					this.content = (this.target as MonoScript).text.Substring(0, MonoScriptEditor.MaxChars);
				else
					this.content = (this.target as MonoScript).text;
			}

			if ((this.target as MonoScript).text.Length >= MonoScriptEditor.MaxChars)
				EditorGUILayout.HelpBox("The script has more than " + MonoScriptEditor.MaxChars + " chars, it has been cut. Unity Editor can not handle more.", MessageType.Warning);

			if (Event.current.type == EventType.ExecuteCommand)
			{
				if (Event.current.commandName == "Paste")
					Event.current.Use();
			}
			else if (Event.current.type == EventType.KeyDown)
				Event.current.Use();

			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			EditorGUILayout.TextArea(this.content, GUILayoutOptionPool.ExpandWidthTrue, GUILayoutOptionPool.ExpandHeightTrue);
			EditorGUILayout.EndScrollView();
		}
	}
}