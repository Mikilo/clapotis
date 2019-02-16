using System;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;

namespace NGToolsEditor.NGConsole
{
	/// <summary>
	/// Class cloned from Unity's editor internal class "UnityEditorInternal.LogEntry". It contains the collapseCount.
	/// </summary>
	public sealed class UnityLogEntry
	{
		public readonly object	instance;

		public string	condition { get { return (string)this.conditionField.GetValue(instance); } }
		public int		errorNum { get { return (int)this.errorNumField.GetValue(instance); } }
		public string	file { get { return (string)this.fileField.GetValue(instance); } }
		public int		line { get { return (int)this.lineField.GetValue(instance); } }
		public int		mode { get { return (int)this.modeField.GetValue(instance); } }
		public int		instanceID { get { return (int)this.instanceIDField.GetValue(instance); } }
		public int		identifier { get { return (int)this.identifierField.GetValue(instance); } }
		public int		isWorldPlaying { get { return (int)this.isWorldPlayingField.GetValue(instance); } }

		public int		collapseCount;

		private readonly FieldInfo	conditionField;
		private readonly FieldInfo	errorNumField;
		private readonly FieldInfo	fileField;
		private readonly FieldInfo	lineField;
		private readonly FieldInfo	modeField;
		private readonly FieldInfo	instanceIDField;
		private readonly FieldInfo	identifierField;
		private readonly FieldInfo	isWorldPlayingField;

		public	UnityLogEntry()
		{
			// TODO Unity <5.6 backward compatibility?
			Type	logEntryType = typeof(InternalEditorUtility).Assembly.GetType("UnityEditorInternal.LogEntry") ?? UnityAssemblyVerifier.TryGetType(typeof(Editor).Assembly, "UnityEditor.LogEntry");

			if (logEntryType != null)
			{
				this.instance = Activator.CreateInstance(logEntryType);

				this.conditionField = UnityAssemblyVerifier.TryGetField(logEntryType, "condition", BindingFlags.Instance | BindingFlags.Public);
				this.errorNumField = UnityAssemblyVerifier.TryGetField(logEntryType, "errorNum", BindingFlags.Instance | BindingFlags.Public);
				this.fileField = UnityAssemblyVerifier.TryGetField(logEntryType, "file", BindingFlags.Instance | BindingFlags.Public);
				this.lineField = UnityAssemblyVerifier.TryGetField(logEntryType, "line", BindingFlags.Instance | BindingFlags.Public);
				this.modeField = UnityAssemblyVerifier.TryGetField(logEntryType, "mode", BindingFlags.Instance | BindingFlags.Public);
				this.instanceIDField = UnityAssemblyVerifier.TryGetField(logEntryType, "instanceID", BindingFlags.Instance | BindingFlags.Public);
				this.identifierField = UnityAssemblyVerifier.TryGetField(logEntryType, "identifier", BindingFlags.Instance | BindingFlags.Public);
				this.isWorldPlayingField = UnityAssemblyVerifier.TryGetField(logEntryType, "isWorldPlaying", BindingFlags.Instance | BindingFlags.Public);
			}
		}

		public override string	ToString()
		{
			return
				"Condition=" + this.condition + Environment.NewLine +
				"ErrorNum=" + this.errorNum + Environment.NewLine +
				"File=" + this.file + Environment.NewLine +
				"Line=" + this.line + Environment.NewLine +
				"Mode=" + this.mode + Environment.NewLine +
				"InstanceID=" + this.instanceID + Environment.NewLine +
				"Identifier=" + this.identifier + Environment.NewLine +
				"IsWorldPlaying=" + this.isWorldPlaying + Environment.NewLine;
		}
	}
}