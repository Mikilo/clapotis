namespace NGToolsEditor.NGConsole
{
	public sealed class FirstLineSource : ILogExportSource
	{
		string	ILogExportSource.GetName()
		{
			return "First Line";
		}

		string	ILogExportSource.GetKey()
		{
			return "content";
		}

		string	ILogExportSource.GetValue(Row row, int i)
		{
			ILogContentGetter	logContent = row as ILogContentGetter;

			return logContent != null ? logContent.HeadMessage : string.Empty;
		}
	}
}