using System;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;

namespace NGToolsEditor.NGConsole
{
	/// <summary>
	/// Class cloned from Unity's editor internal class "UnityEditorInternal.LogEntries".
	/// </summary>
	internal static class UnityLogEntries
	{
		public static int	consoleFlags
		{
			get { return (int)UnityLogEntries.GetConsoleFlags(); }
			set { UnityLogEntries.SetConsoleFlags(value); }
		}

		public delegate void	getCountsByType(ref int errorCount, ref int warningCount, ref int logCount);
		public delegate void	getFirstTwoLinesEntryTextAndModeInternal(int row, ref int mask, ref string outString);

		public static Action<int>	SetConsoleFlags;
		public static Func<int>		GetConsoleFlags;

		public static Func<int>			StartGettingEntries;
		public static Action<int, bool>	SetConsoleFlag;
		public static Action			EndGettingEntries;
		public static Func<int>			GetCount;
		public static bool				GetEntryInternal(int row, object outputEntry) { return (bool)GetEntryInternalMethod.DynamicInvoke(row, outputEntry); }
		private static Delegate			GetEntryInternalMethod;
		public static Func<int, int>	GetEntryCount;
		public static Action			Clear;

		static	UnityLogEntries()
		{
			// TODO Unity <5.6 backward compatibility?
			Type	logEntriesType = typeof(InternalEditorUtility).Assembly.GetType("UnityEditorInternal.LogEntries") ?? UnityAssemblyVerifier.TryGetType(typeof(Editor).Assembly, "UnityEditor.LogEntries");

			if (logEntriesType != null)
			{
				PropertyInfo	consoleFlagsProperty = UnityAssemblyVerifier.TryGetProperty(logEntriesType, "consoleFlags", BindingFlags.Static | BindingFlags.Public);

				if (consoleFlagsProperty != null)
				{
					UnityLogEntries.SetConsoleFlags = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), null, consoleFlagsProperty.GetSetMethod());
					UnityLogEntries.GetConsoleFlags = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), null, consoleFlagsProperty.GetGetMethod());
				}

				UnityLogEntries.StartGettingEntries = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), null, UnityAssemblyVerifier.TryGetMethod(logEntriesType, "StartGettingEntries", BindingFlags.Static | BindingFlags.Public));
				UnityLogEntries.SetConsoleFlag = (Action<int, bool>)Delegate.CreateDelegate(typeof(Action<int, bool>), null, UnityAssemblyVerifier.TryGetMethod(logEntriesType, "SetConsoleFlag", BindingFlags.Static | BindingFlags.Public));
				UnityLogEntries.EndGettingEntries = (Action)Delegate.CreateDelegate(typeof(Action), null, UnityAssemblyVerifier.TryGetMethod(logEntriesType, "EndGettingEntries", BindingFlags.Static | BindingFlags.Public));
				UnityLogEntries.GetCount = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), null, UnityAssemblyVerifier.TryGetMethod(logEntriesType, "GetCount", BindingFlags.Static | BindingFlags.Public));

				Type		logEntryType = typeof(InternalEditorUtility).Assembly.GetType("UnityEditorInternal.LogEntry") ?? UnityAssemblyVerifier.TryGetType(typeof(Editor).Assembly, "UnityEditor.LogEntry");
				Type		delegateType = Type.GetType("System.Func`3[[System.Int32, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[" + logEntryType.AssemblyQualifiedName + "],[System.Boolean, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				MethodInfo	GetEntryInternal = UnityAssemblyVerifier.TryGetMethod(logEntriesType, "GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
				UnityLogEntries.GetEntryInternalMethod = Delegate.CreateDelegate(delegateType, null, GetEntryInternal);

				UnityLogEntries.GetEntryCount = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), null, UnityAssemblyVerifier.TryGetMethod(logEntriesType, "GetEntryCount", BindingFlags.Static | BindingFlags.Public));
				UnityLogEntries.Clear = (Action)Delegate.CreateDelegate(typeof(Action), null, UnityAssemblyVerifier.TryGetMethod(logEntriesType, "Clear", BindingFlags.Static | BindingFlags.Public));
			}
		}
	}
}