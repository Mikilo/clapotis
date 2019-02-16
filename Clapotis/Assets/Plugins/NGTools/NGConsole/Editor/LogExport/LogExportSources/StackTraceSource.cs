namespace NGToolsEditor.NGConsole
{
	public sealed class StackTraceSource : ILogExportSource
	{
		string	ILogExportSource.GetName()
		{
			return "Stack Trace";
		}

		string	ILogExportSource.GetKey()
		{
			return "stackTrace";
		}

		string	ILogExportSource.GetValue(Row row, int i)
		{
			ILogContentGetter	logContent = row as ILogContentGetter;

			return logContent != null ? logContent.StackTrace : string.Empty;
		}
	}
}