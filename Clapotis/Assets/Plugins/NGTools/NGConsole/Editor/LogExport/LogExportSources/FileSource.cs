namespace NGToolsEditor.NGConsole
{
	public sealed class FileSource : ILogExportSource
	{
		string	ILogExportSource.GetName()
		{
			return "File";
		}

		string	ILogExportSource.GetKey()
		{
			return "file";
		}

		string	ILogExportSource.GetValue(Row row, int i)
		{
			return row.log.file;
		}
	}
}