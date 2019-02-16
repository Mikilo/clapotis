using NGTools.Network;

namespace NGToolsEditor.Network
{
	public interface INGServerConnectable
	{
		Client	Client { get; }

		void	Connect(string address, int port);
		bool	IsConnected(Client client);
	}
}