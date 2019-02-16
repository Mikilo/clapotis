using NGTools;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	[Serializable]
	public class GeneralSettings : ScriptableObject
	{
		public enum ModeOpen
		{
			AssetDatabaseOpenAsset,
			NGConsoleOpener
		}

		[Serializable]
		public class EditorExtensions
		{
			[File(FileAttribute.Mode.Open, "*")]
			public string	editor;
			public string	arguments;
			public string[]	extensions;
		}

		[LocaleHeader("General_AutoReplaceUnityConsole")]
		public bool					autoReplaceUnityConsole = false;
		[LocaleHeader("General_Clear")]
		public string				clearLabel = "Clear";
		[LocaleHeader("General_Collapse")]
		public string				collapseLabel = "Collapse";
		[LocaleHeader("General_ClearOnPlay")]
		public string				clearOnPlayLabel = "Clear on Play";
		[LocaleHeader("General_ErrorPause")]
		public string				errorPauseLabel = "Error Pause";
		[LocaleHeader("General_OpenMode")]
		public ModeOpen				openMode = ModeOpen.AssetDatabaseOpenAsset;
		[LocaleHeader("General_EditorExtensions")]
		public EditorExtensions[]	editorExtensions = new EditorExtensions[0];
		[LocaleHeader("General_SmoothScrolling")]
		public bool					smoothScrolling = true;
		//[LocaleHeader("General_HorizontalScrollbar")]
		//public bool					horizontalScrollbar = false;
		[LocaleHeader("General_FilterUselessStackFrame")]
		public bool					filterUselessStackFrame = true;
		[LocaleHeader("General_DifferentiateException")]
		public bool					differentiateException = false;
		[LocaleHeader("General_DrawLogTypesInHeader")]
		public bool					drawLogTypesInHeader = true;
		[LocaleHeader("General_MenuHeight")]
		public float				menuHeight = ConsoleConstants.DefaultSingleLineHeight;
		[LocaleHeader("General_ConsoleBackground")]
		public Color				consoleBackground = new Color(0F, 0F, 0F, 0F);
		[LocaleHeader("General_MenuButtonStyle"), SerializeField]
		internal GUIStyleOverride	menuButtonStyleOverride = new GUIStyleOverride() { baseStyleName = "ToolbarButton" };
		public GUIStyle				MenuButtonStyle { get { return this.menuButtonStyleOverride.GetStyle(); } set { } }
		[LocaleHeader("General_ToolbarStyle"), SerializeField]
		internal GUIStyleOverride	toolbarStyleOverride = new GUIStyleOverride() { baseStyleName = "Toolbar" };
		public GUIStyle				ToolbarStyle { get { return this.toolbarStyleOverride.GetStyle(); } set { } }
	}
}