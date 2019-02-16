namespace NGToolsEditor.NGConsole
{
	public sealed class IndexSource : ILogExportSource
	{
		string	ILogExportSource.GetName()
		{
			return "Index";
		}

		string	ILogExportSource.GetKey()
		{
			return "index";
		}

		string ILogExportSource.GetValue(Row row, int i)
		{
			return i.ToString();
		}
	}
}