namespace NGToolsEditor.NGConsole
{
	public sealed class LineSource : ILogExportSource
	{
		string	ILogExportSource.GetName()
		{
			return "Line";
		}

		string	ILogExportSource.GetKey()
		{
			return "line";
		}

		string	ILogExportSource.GetValue(Row row, int i)
		{
			return row.log.line.ToString();
		}
	}
}