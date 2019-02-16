using NGTools;
using NGTools.Network;
using System.Collections.Generic;
using UnityEditor;

namespace NGToolsEditor.Network
{
	public sealed class NGServerInstance
	{
		public const double	PingInterval = 2F;
		public const double	MaxPingTimeBeforeShutdown = 20F;

		#region Server status
		public double	lastPing = -1F;

		private bool	isPinging;
		private double	nextPingTime;
		#endregion

		public Client				client;
		public List<EditorWindow>	users = new List<EditorWindow>();
		public string				deviceName;
		public string				endPoint;
		public string				additionalInformation;
		public double				pingMaxLastTime;

		public void	Update()
		{
			// Some deeper code can close the client connection, we need to maintain a reference.
			Client	c = this.client;

			if (this.isPinging == false)
			{
				if (Conf.DebugMode != Conf.DebugState.Verbose && this.nextPingTime < EditorApplication.timeSinceStartup)
				{
					c.AddPacket(new ClientSendPingPacket(), this.OnPingReceived);
					this.isPinging = true;
					this.nextPingTime = EditorApplication.timeSinceStartup;
				}
			}
			else if (Conf.DebugMode != Conf.DebugState.Verbose && EditorApplication.timeSinceStartup - this.nextPingTime > NGServerInstance.MaxPingTimeBeforeShutdown)
			{
				InternalNGDebug.Log(LC.G("NGHierarchy_ClientDisconnected") + " Due to expired ping. (" + c.tcpClient.Client.LocalEndPoint + ") "/* + EditorApplication.timeSinceStartup + " - " + this.nextPingTime*/);
				ConnectionsManager.Close(c);
				return;
			}

			if (this.DetectClientDisced(c) == true)
			{
				InternalNGDebug.LogError(LC.G("RemoteModule_ClientDisconnected"));
				ConnectionsManager.Close(c);
				return;
			}

			c.ExecuteReceivedCommands(ConnectionsManager.Executer);
			c.Write(ConnectionsManager.Executer);
		}

		private bool	DetectClientDisced(Client client)
		{
			if (client.tcpClient.Connected == false)
				return true;

			//try
			//{
			//	if (client.tcpClient.Client.Poll(0, SelectMode.SelectRead) == true &&
			//		client.tcpClient.Client.Receive(pollDiscBuffer, SocketFlags.Peek) == 0)
			//	{
			//		return true;
			//	}
			//}
			//catch (Exception ex)
			//{
			//	InternalNGDebug.LogException(ex);
			//}

			return false;
		}

		private void	OnPingReceived(ResponsePacket p)
		{
			this.isPinging = false;
			this.lastPing = EditorApplication.timeSinceStartup - this.nextPingTime;
			this.nextPingTime = EditorApplication.timeSinceStartup + NGServerInstance.PingInterval;
		}
	}
}