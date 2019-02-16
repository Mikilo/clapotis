namespace NGToolsEditor.NGConsole
{
	public sealed class TimeSource : ILogExportSource
	{
		string	ILogExportSource.GetName()
		{
			return "Time";
		}

		string	ILogExportSource.GetKey()
		{
			return "time";
		}

		string	ILogExportSource.GetValue(Row row, int i)
		{
			return row.log.time;
		}
	}
}