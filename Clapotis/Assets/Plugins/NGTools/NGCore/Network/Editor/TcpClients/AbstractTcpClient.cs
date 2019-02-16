using NGTools.Network;

namespace NGToolsEditor.Network
{
	public abstract class AbstractTcpClient
	{
		public abstract Client	CreateClient(string address, int port);
	}
}