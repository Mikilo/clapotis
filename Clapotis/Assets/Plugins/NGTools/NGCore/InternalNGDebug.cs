using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using UnityEngine;

namespace NGTools
{
#if !UNITY_WSA_10_0 && !UNITY_WSA_8_1 && !UNITY_WP_8_1
	[Browsable(false)]
#endif
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static class InternalNGDebug
	{
		[Serializable]
		public class NGTools : Exception
		{
			public	NGTools(string message) : base(message)
			{
			}
		}

		#region Loggers' variables
		internal const char	MultiContextsStartChar = (char)1;
		internal const char	MultiContextsEndChar = (char)4;
		internal const char	MultiContextsSeparator = ';';

		internal const char	DataStartChar = (char)2;
		internal const char	DataEndChar = (char)4;
		internal const char	DataSeparator = '\n';
		internal const char	DataSeparatorReplace = (char)5;

		internal const char	MultiTagsStartChar = (char)3;
		internal const char	MultiTagsEndChar = (char)4;
		internal const char	MultiTagsSeparator = ';';

		internal const char	JSONStartChar = (char)4;
		internal const char	JSONEndChar = (char)5;
		internal const char	JSONSeparator = (char)6;
		#endregion

		private static EventWaitHandle	waitHandle;

		private static string	logPath = null;
		public static string	LogPath
		{
			get
			{
				if (InternalNGDebug.logPath == null)
				{
					try
					{
						InternalNGDebug.logPath = File.ReadAllText(InternalNGDebug.GetLogFilePath());
					}
					catch
					{
					}

					if (InternalNGDebug.logPath == null)
						InternalNGDebug.logPath = Constants.DefaultDebugLogFilepath;
				}

				return InternalNGDebug.logPath;
			}
			set
			{
				InternalNGDebug.logPath = value;
				File.WriteAllText(InternalNGDebug.GetLogFilePath(), InternalNGDebug.logPath);
			}
		}

		private static string	GetLogFilePath()
		{
			return Path.Combine(Application.persistentDataPath, Path.Combine(Constants.InternalPackageTitle, Constants.DebugLogFilepath));
		}

		public static RuntimePlatform	Platform = Application.platform;

		static	InternalNGDebug()
		{
			if (InternalNGDebug.Platform == RuntimePlatform.WindowsEditor ||
				InternalNGDebug.Platform == RuntimePlatform.OSXEditor ||
				InternalNGDebug.Platform == RuntimePlatform.LinuxEditor)
			{
				InternalNGDebug.waitHandle = new EventWaitHandle(true, EventResetMode.AutoReset, "578943af-6fd1-4792-b36c-1713c20a37d9");
			}
		}

		public static void	LogFormat(string format, params object[] args)
		{
			Debug.LogFormat("[" + Constants.PackageTitle + "] " + string.Format(format, args));
		}

		public static void	Log(object message)
		{
			Debug.Log("[" + Constants.PackageTitle + "] " + message);
		}

		public static void	Log(object message, UnityEngine.Object context)
		{
			Debug.Log("[" + Constants.PackageTitle + "] " + message, context);
		}

		public static void	Log(int error, string message)
		{
			Debug.Log("[" + Constants.PackageTitle + "] #" + error + " - " + message);
		}

		public static void	Log(int error, string message, UnityEngine.Object context)
		{
			Debug.Log("[" + Constants.PackageTitle + "] #" + error + " - " + message, context);
		}

		public static void	LogWarning(string message)
		{
			Debug.LogWarning("[" + Constants.PackageTitle + "] " + message);
		}

		public static void	LogWarning(string message, UnityEngine.Object context)
		{
			Debug.LogWarning("[" + Constants.PackageTitle + "] " + message, context);
		}

		public static void	LogWarning(int error, string message)
		{
			Debug.LogWarning("[" + Constants.PackageTitle + "] #" + error + " - " + message);
		}

		public static void	LogWarning(int error, string message, UnityEngine.Object context)
		{
			Debug.LogWarning("[" + Constants.PackageTitle + "] #" + error + " - " + message, context);
		}

		public static void	LogError(object message)
		{
			Debug.LogError("[" + Constants.PackageTitle + "] " + message);
		}

		public static void	LogError(object message, UnityEngine.Object context)
		{
			Debug.LogError("[" + Constants.PackageTitle + "] " + message, context);
		}

		public static void	LogError(int error, string message)
		{
			Debug.LogError("[" + Constants.PackageTitle + "] #" + error + " - " + message);
		}

		public static void	LogError(int error, string message, UnityEngine.Object context)
		{
			Debug.LogError("[" + Constants.PackageTitle + "] #" + error + " - " + message, context);
		}

		public static void	LogException(string message, Exception exception)
		{
			Debug.LogException(new NGTools(exception.GetType().Name + ": " + message + Environment.NewLine + exception.Message + Environment.NewLine + exception.StackTrace));
		}

		public static void	LogException(Exception exception)
		{
			Debug.LogException(new NGTools(exception.Message + Environment.NewLine + exception.StackTrace));
		}

		public static void	LogException(Exception exception, UnityEngine.Object context)
		{
			Debug.LogException(new NGTools(exception.Message + Environment.NewLine + exception.StackTrace), context);
		}

		public static void	LogException(string message, Exception exception, UnityEngine.Object context)
		{
			Debug.LogException(new NGTools(message + Environment.NewLine + exception.Message + Environment.NewLine + exception.StackTrace), context);
		}

		public static void	LogException(int error, Exception exception)
		{
			Debug.LogException(new NGTools("[E" + error + "] " + exception.GetType().Name + ": " + exception.Message + Environment.NewLine + exception.StackTrace));
		}

		public static void	LogException(int error, Exception exception, UnityEngine.Object context)
		{
			Debug.LogException(new NGTools("[E" + error + "] " + exception.GetType().Name + ": " + exception.Message + Environment.NewLine + exception.StackTrace), context);
		}

		public static void	LogException(int error, string message, Exception exception)
		{
			Debug.LogException(new NGTools("[E" + error + "] " + message + Environment.NewLine + exception.GetType().Name + ": " + exception.Message + Environment.NewLine + exception.StackTrace));
		}

		public static void	LogException(int error, string message, Exception exception, UnityEngine.Object context)
		{
			Debug.LogException(new NGTools("[E" + error + "] " + message + Environment.NewLine + exception.GetType().Name + ": " + exception.Message + Environment.NewLine + exception.StackTrace), context);
		}

		public static void	InternalLogFormat(string format, params object[] arg)
		{
			if (Conf.DebugMode == Conf.DebugState.None)
				return;

			Debug.LogFormat("[" + Constants.PackageTitle + ":internal] " + string.Format(format, arg));
		}

		public static void	InternalLogFormat(string format, object arg0, object arg1)
		{
			if (Conf.DebugMode == Conf.DebugState.None)
				return;

			Debug.LogFormat("[" + Constants.PackageTitle + ":internal] " + string.Format(format, arg0, arg1));
		}

		public static void	InternalLogFormat(string format, object arg0)
		{
			if (Conf.DebugMode == Conf.DebugState.None)
				return;

			Debug.LogFormat("[" + Constants.PackageTitle + ":internal] " + string.Format(format, arg0));
		}

		public static void	InternalLog(object message)
		{
			if (Conf.DebugMode == Conf.DebugState.None)
				return;

			Debug.Log("[" + Constants.PackageTitle + ":internal] " + message);
		}

		public static void	InternalLogWarning(object message)
		{
			if (Conf.DebugMode == Conf.DebugState.None)
				return;

			Debug.LogWarning("[" + Constants.PackageTitle + ":internal] " + message);
		}

		public static void	Assert(bool assertion, object message, UnityEngine.Object context)
		{
			if (Conf.DebugMode == Conf.DebugState.None)
				return;

			if (assertion == false)
				Debug.LogError("[" + Constants.PackageTitle + "] " + message, context);
		}

		public static void	Assert(bool assertion, object message)
		{
			if (Conf.DebugMode != Conf.DebugState.None)
				if (assertion == false)
					Debug.LogError("[" + Constants.PackageTitle + "] " + message);
		}

		public static void	AssertFormat(bool assertion, string format, params object[] args)
		{
			if (Conf.DebugMode != Conf.DebugState.None)
				if (assertion == false)
					Debug.LogError("[" + Constants.PackageTitle + "] " + string.Format(format, args));
		}

		private static int	lastLogHash;
		private static int	lastLogCounter;

		public static void	LogFile(object log)
		{
			if (Conf.DebugMode == Conf.DebugState.None)
				return;

			if (InternalNGDebug.Platform == RuntimePlatform.WindowsEditor || InternalNGDebug.Platform == RuntimePlatform.OSXEditor || InternalNGDebug.Platform == RuntimePlatform.LinuxEditor)
			{
				InternalNGDebug.VerboseLog(log);

				if (log != null)
				{
					try
					{
						InternalNGDebug.waitHandle.WaitOne();

						int	logHash = log.GetHashCode();
						if (logHash != InternalNGDebug.lastLogHash)
						{
							InternalNGDebug.lastLogHash = logHash;
							InternalNGDebug.lastLogCounter = 0;
							File.AppendAllText(InternalNGDebug.LogPath, log + Environment.NewLine);
						}
						else
						{
							++InternalNGDebug.lastLogCounter;
							if (InternalNGDebug.lastLogCounter <= 2)
								File.AppendAllText(InternalNGDebug.LogPath, log + Environment.NewLine);
							else if (InternalNGDebug.lastLogCounter == 3)
								File.AppendAllText(InternalNGDebug.LogPath, "…" + Environment.NewLine);
						}
					}
					finally
					{
						InternalNGDebug.waitHandle.Set();
					}
				}
			}
			else
			{
				Debug.Log("[" + Constants.PackageTitle + "] " + log);
			}
		}

		public static void	LogFileException(string message, Exception exception)
		{
			if (Conf.DebugMode == Conf.DebugState.None)
				return;

			if (InternalNGDebug.Platform == RuntimePlatform.WindowsEditor || InternalNGDebug.Platform == RuntimePlatform.OSXEditor || InternalNGDebug.Platform == RuntimePlatform.LinuxEditor)
			{
				InternalNGDebug.VerboseLog(message);
				InternalNGDebug.VerboseLogException(exception);

				try
				{
					InternalNGDebug.waitHandle.WaitOne();

					int logHash = message.GetHashCode() + exception.GetHashCode();
					if (logHash != InternalNGDebug.lastLogHash)
					{
						InternalNGDebug.lastLogHash = logHash;
						InternalNGDebug.lastLogCounter = 0;
						File.AppendAllText(InternalNGDebug.LogPath, message + Environment.NewLine + exception.Message + Environment.NewLine + exception.StackTrace + Environment.NewLine);
					}
					else
					{
						++InternalNGDebug.lastLogCounter;
						if (InternalNGDebug.lastLogCounter <= 2)
							File.AppendAllText(InternalNGDebug.LogPath, message + Environment.NewLine + exception.Message + Environment.NewLine + exception.StackTrace + Environment.NewLine);
						else if (InternalNGDebug.lastLogCounter == 3)
							File.AppendAllText(InternalNGDebug.LogPath, "…" + Environment.NewLine);
					}
				}
				finally
				{
					InternalNGDebug.waitHandle.Set();
				}
			}
			else
			{
				Debug.LogError("[" + Constants.PackageTitle + "] " + message);
				Debug.LogError("[" + Constants.PackageTitle + "] " + exception.Message + Environment.NewLine + exception.StackTrace);
			}
		}

		public static void	LogFileException(Exception exception)
		{
			if (Conf.DebugMode == Conf.DebugState.None)
				return;

			if (InternalNGDebug.Platform == RuntimePlatform.WindowsEditor || InternalNGDebug.Platform == RuntimePlatform.OSXEditor || InternalNGDebug.Platform == RuntimePlatform.LinuxEditor)
			{
				InternalNGDebug.VerboseLogException(exception);

				try
				{
					InternalNGDebug.waitHandle.WaitOne();

					int	logHash = exception.GetHashCode();
					if (logHash != InternalNGDebug.lastLogHash)
					{
						InternalNGDebug.lastLogHash = logHash;
						InternalNGDebug.lastLogCounter = 0;
						File.AppendAllText(InternalNGDebug.LogPath, exception.Message + Environment.NewLine + exception.StackTrace + Environment.NewLine);
					}
					else
					{
						++InternalNGDebug.lastLogCounter;
						if (InternalNGDebug.lastLogCounter <= 2)
							File.AppendAllText(InternalNGDebug.LogPath, exception.Message + Environment.NewLine + exception.StackTrace + Environment.NewLine);
						else if (InternalNGDebug.lastLogCounter == 3)
							File.AppendAllText(InternalNGDebug.LogPath, "…" + Environment.NewLine);
					}
				}
				finally
				{
					InternalNGDebug.waitHandle.Set();
				}
			}
			else
				Debug.LogError("[" + Constants.PackageTitle + "] " + exception.Message + Environment.NewLine + exception.StackTrace);
		}

		public static void	AssertFile(bool assertion, object message)
		{
			if (Conf.DebugMode == Conf.DebugState.None)
				return;

			if (assertion == false)
			{
				if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor)
				{
					InternalNGDebug.VerboseError((message ?? "NULL").ToString());
					InternalNGDebug.LogFile((message ?? "NULL").ToString());
				}
				else
					Debug.LogError("[" + Constants.PackageTitle + "] " + (message ?? "NULL").ToString());
			}
		}

		public static void	VerboseError(object message)
		{
			if (Conf.DebugMode != Conf.DebugState.Verbose)
				return;

			InternalNGDebug.LogError(message);
		}

		public static void	VerboseLogFormat(string format, params object[] args)
		{
			if (Conf.DebugMode != Conf.DebugState.Verbose)
				return;

			InternalNGDebug.LogFormat(format, args);
		}

		public static void	VerboseLog(object message)
		{
			if (Conf.DebugMode != Conf.DebugState.Verbose)
				return;

			InternalNGDebug.Log(message);
		}

		public static void	VerboseLogException(Exception exception)
		{
			if (Conf.DebugMode != Conf.DebugState.Verbose)
				return;

			InternalNGDebug.LogException(exception);
		}

		public static void	VerboseLogException(string message, Exception exception)
		{
			if (Conf.DebugMode != Conf.DebugState.Verbose)
				return;

			InternalNGDebug.LogException(message, exception);
		}

		public static void	VerboseLogException(string message, Exception exception, UnityEngine.Object context)
		{
			if (Conf.DebugMode != Conf.DebugState.Verbose)
				return;

			InternalNGDebug.LogException(message, exception, context);
		}
	}
}