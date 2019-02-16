namespace NGToolsEditor.NGConsole
{
	public sealed class FullMessageSource : ILogExportSource
	{
		string	ILogExportSource.GetName()
		{
			return "Full Message";
		}

		string	ILogExportSource.GetKey()
		{
			return "content";
		}

		string	ILogExportSource.GetValue(Row row, int i)
		{
			ILogContentGetter	logContent = row as ILogContentGetter;

			return logContent != null ? logContent.FullMessage : string.Empty;
		}
	}
}