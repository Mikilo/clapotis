using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	public static class ConsoleConstants
	{
		#region Console Settings
		public const string	ScriptDefaultApp = "kScriptsDefaultApp";

		/// <summary>
		/// Use this control name to prevent RowsDrawer from overwritting copy command.
		/// </summary>
		public const string	CopyControlName = "copyControl";

		public const int	PreAllocatedArray = 512;
		public const float	HorizontalScrollbarWidth = 15F;
		public const float	VerticalScrollbarWidth = 15F;
		public const float	DefaultSingleLineHeight = 16F;

		public static readonly Color	NormalFoldoutColor = new Color(165F / 255F, 165F / 255F, 165F / 255F);
		public static readonly Color	WarningFoldoutColor = new Color(255F / 255F, 187F / 255F, 0F / 255F);
		public static readonly Color	ErrorFoldoutColor = new Color(236F / 255F, 0F / 255F, 0F / 255F);
		public static readonly Color	ExceptionFoldoutColor = new Color(0F / 255F, 0F / 255F, 0F / 255F);
		#endregion

		#region RowsDrawer
		public const float	RowContentSplitterHeight = 5F;
		public const float	MinRowContentHeight = 32F;
		public const float	MaxRowContentHeightLeft = 100F;
		public const float	CriticalMinimumContentHeight = 5F;
		#endregion

		#region NG Console Commands
		public const string	SwitchNextStreamCommand = "SwitchNextStream";
		public const string	SwitchPreviousStreamCommand = "SwitchPreviousStream";
		public const string	OpenLogCommand = "OpenLog";
		public const string	CloseLogCommand = "CloseLog";
		public const string	FocusTopLogCommand = "FocusTopLog";
		public const string	FocusBottomLogCommand = "FocusBottomLog";
		public const string	MoveUpLogCommand = "MoveUpLog";
		public const string	MoveDownLogcommand = "MoveDownLog";
		public const string	LongMoveUpLogCommand = "LongMoveUpLog";
		public const string	LongMoveDownLogCommand = "LongMoveDownLog";
		public const string	GoToLineCommand = "GoToLine";
		public const string	DeleteLogCommand = "DeleteLog";
		#endregion

		#region Export
		public const int	PreviewRowsCount = 3;
		#endregion
	}
}