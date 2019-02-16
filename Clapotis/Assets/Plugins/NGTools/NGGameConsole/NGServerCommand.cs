using NGTools.Network;
using System;
using System.Collections.Generic;

namespace NGTools.NGGameConsole
{
	using UnityEngine;

	public class NGServerCommand : MonoBehaviour
	{
		public BaseServer	server;

		[Header("[Optional] Allow to execute commands from NG Console.")]
		public NGCLI	cli;
		[Header("Buffer logs if no Client connected, and send them to the first Client. [-1=No buffer, 0=Infinite, >0=Max logs]")]
		public int		logsBufferSize;

		public AbstractTcpListener	listener { get { return (AbstractTcpListener)this.server.listener; } }

		private List<Client>		listeningLogsClients = new List<Client>();
		private Queue<LogPacket>	logsBuffer = new Queue<LogPacket>();

		protected virtual void	Start()
		{
			if (this.server == null)
				InternalNGDebug.LogError("NG Server Command requires field \"Server\".", this);
			else
			{
				if (this.listener != null)
					this.listener.ClientDisconnected += this.OnClientDisconnected;

				this.server.RegisterService(NGTools.NGAssemblyInfo.Name, NGTools.NGAssemblyInfo.Version);
				this.server.RegisterService(NGAssemblyInfo.Name, NGAssemblyInfo.Version);

				this.server.executer.HandlePacket(GameConsolePacketId.Logger_ClientSubscribeLogs, this.HandleClientSubscribeLogs);
				this.server.executer.HandlePacket(GameConsolePacketId.Logger_ClientUnsubscribeLogs, this.HandleClientUnsubscribeLogs);

				this.server.executer.HandlePacket(GameConsolePacketId.CLI_ClientAskCLIAvailable, this.HandleClientAskCLIAvailable);
				this.server.executer.HandlePacket(GameConsolePacketId.CLI_ClientRequestCommandNodes, this.HandleClientRequestCommandNodesPacket);
				this.server.executer.HandlePacket(GameConsolePacketId.CLI_ClientSendCommand, this.HandleClientSendCommandPacket);
			}
		}

		protected virtual void	OnEnable()
		{
			Application.logMessageReceived += this.HandleLog;
		}

		protected virtual void	OnDisable()
		{
			Application.logMessageReceived -= this.HandleLog;
		}

		protected virtual void	OnDestroy()
		{
			if (this.listener != null)
				this.listener.ClientDisconnected -= this.OnClientDisconnected;

			if (this.server != null)
			{
				this.server.UnregisterService(NGTools.NGAssemblyInfo.Name, NGTools.NGAssemblyInfo.Version);
				this.server.UnregisterService(NGAssemblyInfo.Name, NGAssemblyInfo.Version);
			}
		}

		public void	SubscribeClient(Client client)
		{
			if (this.listeningLogsClients.Contains(client) == false)
				this.listeningLogsClients.Add(client);
		}

		public void	UnsubscribeClient(Client client)
		{
			this.listeningLogsClients.Remove(client);
		}

		private void	HandleLog(string condition, string stackTrace, LogType type)
		{
			LogPacket	log = new LogPacket(condition, stackTrace, type);

			if (this.listeningLogsClients.Count == 0 && this.logsBufferSize >= 0)
			{
				this.logsBuffer.Enqueue(log);
				while (this.logsBufferSize > 0 && this.logsBufferSize < this.logsBuffer.Count)
					this.logsBuffer.Dequeue();
			}
			else
			{
				while (this.logsBuffer.Count != 0)
				{
					LogPacket	bufferedLog = this.logsBuffer.Dequeue();
					for (int i = 0; i < this.listeningLogsClients.Count; i++)
						this.listeningLogsClients[i].AddPacket(bufferedLog);
				}

				for (int i = 0; i < this.listeningLogsClients.Count; i++)
					this.listeningLogsClients[i].AddPacket(log);
			}
		}

		private void	OnClientDisconnected(Client client)
		{
			// Make sure client gets removed from listening list if connection got unwillingly cut.
			this.UnsubscribeClient(client);
		}

		private void	HandleClientAskCLIAvailable(Client sender, Packet _packet)
		{
			using (var h = ResponsePacketHandler.Get<ServerAnswerCLIAvailablePacket>(sender, _packet as ClientAskCLIAvailablePacket))
			{
				try
				{
					h.response.hasCLI = this.cli != null;
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	HandleClientRequestCommandNodesPacket(Client sender, Packet _packet)
		{
			ClientRequestCommandNodesPacket	packet = _packet as ClientRequestCommandNodesPacket;

			using (var h = ResponsePacketHandler.Get<ServerSendCommandNodesPacket>(sender, packet))
			{
				try
				{
					if (this.cli == null)
						h.Throw(Errors.CLI_NotAvailable, "Root commands has been requested but there is no CommandLineInterpreter.");

					h.response.root = new RemoteCommand(this.cli.parser.Root);
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	HandleClientSendCommandPacket(Client sender, Packet _packet)
		{
			ClientSendCommandPacket	packet = _packet as ClientSendCommandPacket;

			using (var h = ResponsePacketHandler.Get<ServerSendCommandResponsePacket>(sender, packet))
			{
				try
				{
					if (this.cli == null)
						h.Throw(Errors.CLI_NotAvailable, "A command has been requested but there is no CommandLineInterpreter.");

					h.response.requestId = packet.requestId;
					h.response.returnValue = this.cli.parser.Exec(packet.command, ref h.response.response);
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	HandleClientSubscribeLogs(Client sender, Packet _packet)
		{
			ClientSubscribeLogsPacket	packet = _packet as ClientSubscribeLogsPacket;

			using (var h = ResponsePacketHandler.Get<AckPacket>(sender, packet))
			{
				try
				{
					this.SubscribeClient(sender);
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	HandleClientUnsubscribeLogs(Client sender, Packet _packet)
		{
			ClientUnsubscribeLogsPacket	packet = _packet as ClientUnsubscribeLogsPacket;

			using (var h = ResponsePacketHandler.Get<AckPacket>(sender, packet))
			{
				try
				{
					this.UnsubscribeClient(sender);
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}
	}
}