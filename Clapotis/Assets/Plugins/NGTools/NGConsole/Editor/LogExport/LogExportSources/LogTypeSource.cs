namespace NGToolsEditor.NGConsole
{
	public sealed class LogTypeSource : ILogExportSource
	{
		string	ILogExportSource.GetName()
		{
			return "Log Type";
		}

		string	ILogExportSource.GetKey()
		{
			return "logType";
		}

		string	ILogExportSource.GetValue(Row row, int i)
		{
			if ((row.log.mode & Mode.ScriptingException) != 0)
				return "Exception";
			else if ((row.log.mode & (Mode.ScriptCompileError | Mode.ScriptingError | Mode.Fatal | Mode.Error | Mode.Assert | Mode.AssetImportError | Mode.ScriptingAssertion)) != 0)
				return "Error";
			else if ((row.log.mode & (Mode.ScriptCompileWarning | Mode.ScriptingWarning | Mode.AssetImportWarning)) != 0)
				return "Warning";
			return "Log";
		}
	}
}