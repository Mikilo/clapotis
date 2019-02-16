using NGTools;
using NGTools.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;

namespace NGToolsEditor.Network
{
	public static class ConnectionsManager
	{
		public static event Action<Client>	ClientClosed;

		public static event Action<NGServerInstance>	NewServer;
		public static event Action<NGServerInstance>	UpdateServer;
		public static event Action<NGServerInstance>	KillServer;

		public static List<NGServerInstance>	Servers
		{
			get
			{
				return ConnectionsManager.udpListener.NGServerInstances;
			}
		}

		public static readonly PacketExecuter	Executer = new PacketExecuter();

		private static AutoDetectUDPListener	udpListener;
		private static List<Client>				clients = new List<Client>();

		static	ConnectionsManager()
		{
			ConnectionsManager.udpListener = new AutoDetectUDPListener(AutoDetectUDPClient.UDPPortBroadcastMin, AutoDetectUDPClient.UDPPortBroadcastMax);
			ConnectionsManager.udpListener.NewServer += OnServerAdded;
			ConnectionsManager.udpListener.UpdateServer += OnServerUpdated;
			ConnectionsManager.udpListener.KillServer += OnServerKilled;
		}
		
		public static Thread	OpenClient(EditorWindow user, AbstractTcpClient clientProvider, string address, int port, Action<Client> onComplete)
		{
			string				server = address + ':' + port;
			NGServerInstance	instance = ConnectionsManager.udpListener.Find(server);

			if (instance != null && instance.client != null)
			{
				instance.users.Add(user);
				onComplete(instance.client);
				return null;
			}
			else
			{
				Thread	connectingThread = new Thread(ConnectionsManager.AsyncOpenClient) { Name = "Connecting Client" };
				connectingThread.Start(new object[] { user, clientProvider, address, port, onComplete });
				return connectingThread;
			}
		}

		/// <summary>Removes a user from the connection. Closes the Client in the very near future if no one is using it, giving time for ultimate packets.</summary>
		/// <param name="client"></param>
		public static void	Close(Client client, EditorWindow user = null)
		{
			NGServerInstance	instance = ConnectionsManager.udpListener.Find(client);

			if (instance == null)
				return;

			if (user != null)
			{
				user.Repaint();
				instance.users.Remove(user);
			}
			else
			{
				for (int i = 0; i < instance.users.Count; i++)
					instance.users[i].Repaint();
				instance.users.Clear();
			}

			// Nobody is on this connection anymore. Drop it.
			if (instance.users.Count == 0)
			{
				if (ConnectionsManager.ClientClosed != null)
					ConnectionsManager.ClientClosed(client);

				// Give Client the time to send the disconnect packet.
				EditorApplication.delayCall += () => Utility.StartBackgroundTask(ConnectionsManager.DelayDiscAndClean(user, client));
				instance.client = null;
			}
		}

		private static void	AsyncOpenClient(object credentials)
		{
			EditorWindow		window = (credentials as object[])[0] as EditorWindow;
			AbstractTcpClient	clientProvider = (credentials as object[])[1] as AbstractTcpClient;
			string				address = (string)(credentials as object[])[2];
			int					port = (int)(credentials as object[])[3];
			Action<Client>		onComplete = (credentials as object[])[4] as Action<Client>;
			Client				client = clientProvider.CreateClient(address, port);

			try
			{
				if (client != null)
				{
					Utility.RegisterIntervalCallback(ConnectionsManager.Update, 0);

					ConnectionsManager.clients.Add(client);

					string				server = address + ':' + port;
					NGServerInstance	instance = ConnectionsManager.udpListener.Find(server);

					if (instance == null)
						instance = ConnectionsManager.udpListener.AddServer(server, server);

					instance.users.Add(window);
					instance.client = client;
				}

				onComplete(client);
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException(ex);
				InternalNGDebug.LogError("Connection on " + address + ":" + port  + " failed.");
				InternalNGDebug.LogError("Make sure your firewall allows TCP connection on " + port + ".");
				InternalNGDebug.LogError("Check if Stripping Level is not set on \"Use micro mscorlib\".");
				InternalNGDebug.LogError("Try to connect Unity Profiler to guarantee the device is reachable.");
				InternalNGDebug.LogError("Find more tips at: https://bitbucket.org/Mikilo/neguen-tools/wiki/Home#markdown-header-31-guidances");
			}
		}

		private static void	Update()
		{
			List<NGServerInstance>	servers = ConnectionsManager.udpListener.NGServerInstances;

			lock (servers)
			{
				for (int i = 0; i < servers.Count; i++)
				{
					if (servers[i].client != null)
						servers[i].Update();
				}
			}
		}
		
		private static IEnumerator	DelayDiscAndClean(EditorWindow user, Client client)
		{
			double	time = EditorApplication.timeSinceStartup + 10D; // Give　time to send last packets before closing the client.

			while (client.tcpClient.Connected == true && client.PendingPacketsCount > 0 && time > EditorApplication.timeSinceStartup)
				yield return null;

			// HACK Calling Close() seems to block the instance, even when manually closing the inner stream.
			client.Close();
		}

		private static void	OnServerAdded(NGServerInstance instance)
		{
			if (ConnectionsManager.NewServer != null)
				ConnectionsManager.NewServer(instance);
		}

		private static void	OnServerUpdated(NGServerInstance instance)
		{
			if (ConnectionsManager.UpdateServer != null)
				ConnectionsManager.UpdateServer(instance);
		}

		private static void	OnServerKilled(NGServerInstance instance)
		{
			if (ConnectionsManager.KillServer != null)
				ConnectionsManager.KillServer(instance);
		}
	}
}