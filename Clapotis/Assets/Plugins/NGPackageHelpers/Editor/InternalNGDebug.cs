using System;
using System.ComponentModel;

namespace NGPackageHelpers
{
	[Browsable(false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class InternalNGDebug
	{
		public static string	LogPath = Constants.DefaultDebugLogFilepath;

		public static void	Log(object message)
		{
			UnityEngine.Debug.Log("[NGPH] " + message);
		}

		public static void	LogWarning(string message)
		{
			UnityEngine.Debug.LogWarning("[NGPH] " + message);
		}

		public static void	LogError(object message)
		{
			UnityEngine.Debug.LogError("[NGPH] " + message);
		}

		public static void	LogException(string message, Exception exception)
		{
			UnityEngine.Debug.LogException(new Exception("[NGPH] " + message + Environment.NewLine + exception.Message + Environment.NewLine + exception.StackTrace, exception));
		}

		public static void	LogException(Exception exception)
		{
			UnityEngine.Debug.LogException(new Exception("[NGPH] " + exception.Message + Environment.NewLine + exception.StackTrace, exception));
		}
	}
}