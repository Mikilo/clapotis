namespace NGToolsEditor.NGConsole
{
	public interface ILogExportSource
	{
		string	GetName();
		string	GetKey();
		string	GetValue(Row row, int i);
	}
}