using System;
using UnityEditor;

namespace NGToolsEditor.NGConsole
{
	using UnityEngine;

	[Serializable]
	internal sealed class MaskTypeFilter : ILogFilter
	{
		public string	Name { get { return "By type"; } }
		[Exportable]
		private bool	enabled;
		public bool		Enabled { get { return this.enabled; } set { if (this.enabled != value) { this.enabled = value; if (this.ToggleEnable != null) this.ToggleEnable(); } } }

		public event Action	ToggleEnable;

		[Exportable]
		private int		maskType;

		[NonSerialized]
		private GUIContent	logContent;
		[NonSerialized]
		private GUIContent	warningContent;
		[NonSerialized]
		private GUIContent	errorContent;
		[NonSerialized]
		private GUIContent	exceptionContent;

		public void	Init()
		{
			this.logContent = new GUIContent(UtilityResources.InfoIcon, "Log");
			this.warningContent = new GUIContent(UtilityResources.WarningIcon, "Warning");
			this.errorContent = new GUIContent(UtilityResources.ErrorIcon, "Error");
			this.exceptionContent = new GUIContent(this.errorContent.image, "Exception");
		}

		public FilterResult	CanDisplay(Row row)
		{
			int	mask;

			if ((row.log.mode & Mode.ScriptingException) != 0)
			{
				mask = 1 << (int)LogType.Exception;
				if (HQ.Settings.Get<GeneralSettings>().differentiateException == false)
					mask = 1 << (int)LogType.Error;
			}
			else if ((row.log.mode & (Mode.ScriptCompileError | Mode.ScriptingError | Mode.Fatal | Mode.Error | Mode.Assert | Mode.AssetImportError | Mode.ScriptingAssertion)) != 0)
				mask = 1 << (int)LogType.Error;
			else if ((row.log.mode & (Mode.ScriptCompileWarning | Mode.ScriptingWarning | Mode.AssetImportWarning)) != 0)
				mask = 1 << (int)LogType.Warning;
			else
				mask = 1 << (int)LogType.Log;

			if ((this.maskType & mask) != 0)
				return FilterResult.Accepted;
			return FilterResult.Refused;
		}

		public Rect	OnGUI(Rect r, bool compact)
		{
			if (this.logContent == null)
				this.Init();

			float	width = r.width;

			GUI.Box(r, string.Empty, GeneralStyles.Toolbar);

			GeneralSettings	settings = HQ.Settings.Get<GeneralSettings>();

			if (compact == false)
			{
				Utility.content.text = LC.G("AcceptedTypes");
				r.width = GUI.skin.label.CalcSize(Utility.content).x + 10F; // Add a small margin for clarity.
				GUI.Label(r, Utility.content);
				r.x += r.width;
			}
			else
				r.width = 0F;

			if (settings.differentiateException == true)
				r.width = (width - r.width) / 4F;
			else
				r.width = (width - r.width) / 3F;

			EditorGUI.BeginChangeCheck();
			GUI.Toggle(r, (this.maskType & (1 << (int)LogType.Log)) != 0, this.logContent, settings.MenuButtonStyle);
			if (EditorGUI.EndChangeCheck())
				this.maskType = ((int)this.maskType ^ (1 << (int)LogType.Log));
			r.x += r.width;

			EditorGUI.BeginChangeCheck();
			using (BgColorContentRestorer.Get(ConsoleConstants.WarningFoldoutColor))
			{
				GUI.Toggle(r, (this.maskType & (1 << (int)LogType.Warning)) != 0, this.warningContent, settings.MenuButtonStyle);
			}
			if (EditorGUI.EndChangeCheck())
				this.maskType = ((int)this.maskType ^ (1 << (int)LogType.Warning));
			r.x += r.width;

			EditorGUI.BeginChangeCheck();
			using (BgColorContentRestorer.Get(ConsoleConstants.ErrorFoldoutColor))
			{
				GUI.Toggle(r, (this.maskType & (1 << (int)LogType.Error)) != 0, this.errorContent, settings.MenuButtonStyle);
			}
			if (EditorGUI.EndChangeCheck())
				this.maskType = ((int)this.maskType ^ (1 << (int)LogType.Error));
			r.x += r.width;

			if (settings.differentiateException == true)
			{
				EditorGUI.BeginChangeCheck();
				using (ColorContentRestorer.Get(ConsoleConstants.ExceptionFoldoutColor))
				{
					GUI.Toggle(r, (this.maskType & (1 << (int)LogType.Exception)) != 0, this.exceptionContent, settings.MenuButtonStyle);
				}
				if (EditorGUI.EndChangeCheck())
					this.maskType = ((int)this.maskType ^ (1 << (int)LogType.Exception));
			}

			r.y += r.height + 2F;

			return r;
		}

		public void	ContextMenu(GenericMenu menu, Row row, int i)
		{
		}
	}
}