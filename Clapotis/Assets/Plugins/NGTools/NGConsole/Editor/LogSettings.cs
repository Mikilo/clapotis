using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	public class LogSettings : ScriptableObject
	{
		[LocaleHeader("Log_GiveFocusToEditor")]
		public bool			giveFocusToEditor = true;
		[LocaleHeader("Log_ForceFocusOnModifier")]
		public EventModifiers	forceFocusOnModifier = EventModifiers.Alt;
		[LocaleHeader("Log_SelectObjectOnModifier")]
		public EventModifiers	selectObjectOnModifier = EventModifiers.Shift;
		[LocaleHeader("Log_AlwaysDisplayLogContent")]
		public bool			alwaysDisplayLogContent = true;
		[LocaleHeader("Log_Style"), SerializeField]
		internal GUIStyleOverride	styleOverride = new GUIStyleOverride() { baseStyleName = "label" };
		public GUIStyle				Style { get { return this.styleOverride.GetStyle(); } set { } }
		[LocaleHeader("Log_Height")]
		public float		height = ConsoleConstants.DefaultSingleLineHeight;
		[LocaleHeader("Log_SelectedBackground")]
		public Color		selectedBackground ;
		[LocaleHeader("Log_EvenBackground")]
		public Color		evenBackground;
		[LocaleHeader("Log_OddBackground")]
		public Color		oddBackground;
		[LocaleHeader("Log_DisplayTime")]
		public bool			displayTime = false;
		[LocaleHeader("Log_TimeFormat")]
		public string		timeFormat = "HH:mm:ss.fff";
		[LocaleHeader("Log_TimeStyle"), SerializeField]
		internal GUIStyleOverride	timeStyleOverride = new GUIStyleOverride() { baseStyleName = "label" };
		public GUIStyle				TimeStyle { get { return this.timeStyleOverride.GetStyle(); } set { } }
		[LocaleHeader("Log_DisplayFrameCount")]
		public bool			displayFrameCount = false;
		[LocaleHeader("Log_DisplayRenderedFrameCount")]
		public bool			displayRenderedFrameCount = false;

		[LocaleHeader("Log_CollapseLabelStyle"), SerializeField]
		internal GUIStyleOverride	collapseLabelStyleOverride = new GUIStyleOverride() { baseStyleName = "CN CountBadge" };
		public GUIStyle				CollapseLabelStyle { get { return this.collapseLabelStyleOverride.GetStyle(); } set { } }
		[LocaleHeader("Log_ContentStyle"), SerializeField]
		internal GUIStyleOverride	contentStyleOverride = new GUIStyleOverride() { baseStyleName = "label" };
		public GUIStyle				ContentStyle { get { return this.contentStyleOverride.GetStyle(); } set { } }
	}
}