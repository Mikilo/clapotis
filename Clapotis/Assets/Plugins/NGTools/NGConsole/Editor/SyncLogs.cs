using System;
using UnityEditor;

namespace NGToolsEditor.NGConsole
{
	/// <summary>
	/// Interface between Unity's internal Console and NGConsole.
	/// </summary>
	internal sealed class SyncLogs
	{
		public const string	LastFlagsPrefKey = "SyncLogs_LastFlags";

		public Action<int, UnityLogEntry>	NewLog;
		public Action<int, UnityLogEntry>	UpdateLog;
		public Action						EndNewLog;
		public Action						ResetLog;
		public Action						ClearLog;
		public Action						OptionAltered;

		internal UnityLogEntry	logEntry = new UnityLogEntry();

		private NGConsoleWindow	console;
		private int				lastRawEntryCount = -1;
		private bool			previousCollapse;
		private int				lastFlags;

		public	SyncLogs(NGConsoleWindow editor)
		{
			this.console = editor;
			this.lastFlags = EditorPrefs.GetInt(SyncLogs.LastFlagsPrefKey);
		}

		/// <summary>
		/// Handles incoming logs. Synchronizes only when required.
		/// </summary>
		public void	Sync()
		{
			int	backupFlag = UnityLogEntries.consoleFlags;

			UnityLogEntries.SetConsoleFlag((int)ConsoleFlags.LogLevelLog, true);
			UnityLogEntries.SetConsoleFlag((int)ConsoleFlags.LogLevelWarning, true);
			UnityLogEntries.SetConsoleFlag((int)ConsoleFlags.LogLevelError, true);
			//UnityLogEntries.SetConsoleFlag((int)ConsoleFlags.Collapse, true);

			int	totalLogCount = UnityLogEntries.GetCount();
			if (totalLogCount == this.lastRawEntryCount)
			{
				// Update rows with new collapse count.
				if (this.UpdateLog != null &&
					(UnityLogEntries.consoleFlags & (int)ConsoleFlags.Collapse) != 0 &&
					this.lastRawEntryCount > 0)
				{
					for (int consoleIndex = 0; consoleIndex < totalLogCount; consoleIndex++)
					{
						logEntry.collapseCount = UnityLogEntries.GetEntryCount(consoleIndex);
						this.UpdateLog(consoleIndex, logEntry);
					}

					Utility.RepaintEditorWindow(typeof(ModuleWindow));
					this.console.Repaint();
				}

				UnityLogEntries.consoleFlags = backupFlag;
				// Repaint console if an option was altered.
				if (backupFlag != this.lastFlags)
				{
					this.lastFlags = backupFlag;
					EditorPrefs.SetInt(SyncLogs.LastFlagsPrefKey, this.lastFlags);
					if (this.OptionAltered != null)
						this.OptionAltered();
					Utility.RepaintEditorWindow(typeof(ModuleWindow));
					this.console.Repaint();
				}
				return;
			}

			if (this.lastRawEntryCount > totalLogCount)
			{
				// If collapse is disabled, it means this is a call to Clear.
				if ((UnityLogEntries.consoleFlags & (int)ConsoleFlags.Collapse) == 0)
				{
					this.lastRawEntryCount = 0;
					UnityLogEntries.consoleFlags = backupFlag;
					if (this.ClearLog != null)
						this.ClearLog();
					return;
				}

				// Otherwise collapse has just been enabled.
				this.lastRawEntryCount = 0;
				if (this.ResetLog != null)
					this.ResetLog();
			}
			// If collapse was just enabled, we must force the refresh of previous logs.
			else if ((UnityLogEntries.consoleFlags & (int)ConsoleFlags.Collapse) == 0 &&
					 this.previousCollapse == true)
			{
				this.lastRawEntryCount = 0;
				if (this.ResetLog != null)
					this.ResetLog();
			}

			UnityLogEntries.StartGettingEntries();

			for (int consoleIndex = this.lastRawEntryCount; consoleIndex < totalLogCount; consoleIndex++)
			{
				if (UnityLogEntries.GetEntryInternal(consoleIndex, logEntry.instance) == true)
				{
					logEntry.collapseCount = UnityLogEntries.GetEntryCount(consoleIndex);

					if (this.NewLog != null)
						this.NewLog(consoleIndex, logEntry);
				}
			}

			UnityLogEntries.EndGettingEntries();
			this.lastRawEntryCount = totalLogCount;
			UnityLogEntries.consoleFlags = backupFlag;
			this.EndNewLog();
			this.previousCollapse = (backupFlag & (int)ConsoleFlags.Collapse) != 0;
		}

		/// <summary>
		/// Resets SyncLogs entry count. Forcing it to resynchronize all logs.
		/// </summary>
		public void	LocalClear()
		{
			this.lastRawEntryCount = 0;
		}

		/// <summary>
		/// Resets local data, then clears and repaints Unity's Console.
		/// </summary>
		public void	Clear()
		{
			this.LocalClear();
			UnityLogEntries.EndGettingEntries();
			UnityLogEntries.Clear();

			// An issue happens when a sticky compile error is present and we want to clear, it does not keep the log whereas it was sticky.
			// Just force the refresh.
			UnityLogEntries.consoleFlags = UnityLogEntries.consoleFlags;

			Utility.RepaintEditorWindow(typeof(ModuleWindow));
			Utility.RepaintConsoleWindow();
		}
	}
}