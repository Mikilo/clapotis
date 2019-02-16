using NGTools;
using NGToolsEditor.NGConsole;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NGToolsEditor.NGGameConsole
{
	[NGSettings("Remote Module", 40)]
	public class RemoteModuleSettings : ScriptableObject
	{
		[Header("Clears the space on a new connection.")]
		public bool		addBlankRowOnConnection = true;
		public Color	completionBackgroundColor = Color.black;
		public Color	hoverCompletionBackgroundColor = new Color(41F / 255F, 41F / 255F, 41F / 255F, 1F);
		public Color	partialCompletionColor = new Color(72F / 255F, 255F / 255F, 0F / 255F, 1F);
		[SerializeField]
		internal GUIStyleOverride	highlightedMatchStyleOverride = new GUIStyleOverride() { baseStyleName = "label" };
		public GUIStyle				HighlightedMatchStyle { get { return this.highlightedMatchStyleOverride.GetStyle(); } set { } }
		[SerializeField]
		internal GUIStyleOverride	commandInputStyleOverride = new GUIStyleOverride() { baseStyleName = "textfield" };
		public GUIStyle				CommandInputStyle { get { return this.commandInputStyleOverride.GetStyle(); } set { } }
		public float	execButtonWidth = 70F;
		[LocaleHeader("StackTrace_Style"), SerializeField]
		internal GUIStyleOverride	execButtonStyleOverride = new GUIStyleOverride() { baseStyleName = "ToolbarButton" };
		public GUIStyle				ExecButtonStyle { get { return this.execButtonStyleOverride.GetStyle(); } set { } }
		[LocaleHeader("NGSettings_MainModule_CompactStreamsLimit"), Range(0, 10)]
		public int		compactStreamsLimit = 1;
		[LocaleHeader("NGSettings_MainModule_DefaultFiltersInStream"), SubClassesOf(typeof(ILogFilter))]
		public string[]	defaultFiltersInStream = new string[] { typeof(ContentFilter).FullName };

		[NGSettingsChanged]
		private static void	OnSettingsGenerated(ScriptableObject asset)
		{
			RemoteModuleSettings	remoteSettings = asset as RemoteModuleSettings;

			if (remoteSettings != null)
			{
				Action	OverrideSettings = () =>
				{
					remoteSettings.HighlightedMatchStyle = new GUIStyle(GUI.skin.label);
					remoteSettings.HighlightedMatchStyle.normal.textColor = new Color(219F / 255F, 219F / 255F, 219F / 255F, 1F);
					remoteSettings.HighlightedMatchStyle.richText = true;
					remoteSettings.CommandInputStyle = new GUIStyle(GUI.skin.textField);
					//remoteSettings.commandInputStyle.normal.background = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(HQ.RootPath, "NGGameConsole/Textures/commandInputBg.png"));
					remoteSettings.ExecButtonStyle = new GUIStyle("ToolbarButton");
					remoteSettings.ExecButtonStyle.fontStyle = FontStyle.Bold;
				};

				if (Utility.CheckOnGUI() == false)
					GUICallbackWindow.Open(OverrideSettings);
				else
					OverrideSettings();
			}
		}

		public IEnumerable<ILogFilter>	GenerateFilters()
		{
			for (int i = 0; i < this.defaultFiltersInStream.Length; i++)
			{
				Type	t = Type.GetType(this.defaultFiltersInStream[i]);

				if (t != null)
				{
					ILogFilter	filter = Activator.CreateInstance(t) as ILogFilter;

					if (filter != null)
						yield return filter;
				}
			}
		}
	}
}