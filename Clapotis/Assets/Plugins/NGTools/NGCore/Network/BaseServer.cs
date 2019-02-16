using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace NGTools.Network
{
	using UnityEngine;

	public class BaseServer : MonoBehaviour
	{
		private static List<BaseServer>	instances = new List<BaseServer>();

		[Header("Starts the server when awaking.")]
		public bool	autoStart = true;
		[Header("Keep the server alive between scenes.")]
		public bool	dontDestroyOnLoad = true;

		[Header("[Required] A listener to communicate via network.")]
		public NetworkListener	listener;

		public readonly PacketExecuter	executer = new PacketExecuter();

		private AutoDetectUDPClient	udpClient;
		private List<string[]>		servicesVersions = new List<string[]>();

		protected virtual void	Awake()
		{
			for (int i = 0; i < BaseServer.instances.Count; i++)
			{
				if (BaseServer.instances[i] != this &&
					BaseServer.instances[i].GetType() == this.GetType())
				{
					Object.Destroy(this.gameObject);
					return;
				}
			}

			if (this.dontDestroyOnLoad == true)
			{
				BaseServer.instances.Add(this);
				Object.DontDestroyOnLoad(this.transform.root.gameObject);

				this.executer.HandlePacket(PacketId.ClientRequestServices, this.HandleClientRequestServices);
			}
		}

		protected virtual void	Start()
		{
			if (this.listener == null)
				InternalNGDebug.LogError("A NetworkListener is required.", this);
			else
			{
				this.listener.SetServer(this);

				try
				{
					this.udpClient = new AutoDetectUDPClient(this, this.listener.port, AutoDetectUDPClient.UDPPortBroadcastMin, AutoDetectUDPClient.UDPPortBroadcastMax, AutoDetectUDPClient.UDPPingInterval, this.StringifyServices());
				}
				catch (SocketException ex)
				{
					InternalNGDebug.LogException("The UDP client has failed, it may be caused by port " + this.listener.port + " already being used.", ex);
				}

				if (this.autoStart == true)
					this.StartServer();
			}
		}

		protected virtual void	OnDestroy()
		{
			if (this.listener != null)
			{
				this.listener.StopServer();
				this.listener = null;
			}

			if (this.udpClient != null)
				this.udpClient.Stop();
		}

		public void	RegisterService(string service, string version)
		{
			this.servicesVersions.Add(new string[] { service, version });

			if (this.udpClient != null)
				this.udpClient.SetAdditionalInformation(this.StringifyServices());
		}

		public void	UnregisterService(string service, string version)
		{
			for (int i = 0; i < this.servicesVersions.Count; i++)
			{
				if (this.servicesVersions[i][0] == service &&
					this.servicesVersions[i][1] == version)
				{
					this.servicesVersions.RemoveAt(i);
					if (this.udpClient != null)
						this.udpClient.SetAdditionalInformation(this.StringifyServices());
					break;
				}
			}
		}

		public void	StartServer()
		{
			this.listener.StartServer();
		}

		public void	StopServer()
		{
			this.OnDestroy();
		}

		private string	StringifyServices()
		{
			List<int>		nonDupplicatedServices = new List<int>();
			StringBuilder	buffer = Utility.GetBuffer();

			for (int i = 0; i < this.servicesVersions.Count; i++)
			{
				if (nonDupplicatedServices.Exists(j => this.servicesVersions[j][0] == this.servicesVersions[i][0] && this.servicesVersions[j][1] == this.servicesVersions[i][1]) == false)
				{
					nonDupplicatedServices.Add(i);

					if (nonDupplicatedServices.Count > 1)
						buffer.Append(',');
					buffer.Append(this.servicesVersions[i][0]);
					buffer.Append(':');
					buffer.Append(this.servicesVersions[i][1]);
				}
			}

			return Utility.ReturnBuffer(buffer);
		}

		private void	HandleClientRequestServices(Client sender, Packet _packet)
		{
			ClientRequestServicesPacket	packet = _packet as ClientRequestServicesPacket;

			using (var h = ResponsePacketHandler.Get<ServerSendServicesPacket>(sender, packet))
			{
				try
				{
					List<int>	nonDupplicatedServices = new List<int>();

					for (int i = 0; i < this.servicesVersions.Count; i++)
					{
						if (nonDupplicatedServices.Exists(j => this.servicesVersions[j][0] == this.servicesVersions[i][0] && this.servicesVersions[j][1] == this.servicesVersions[i][1]) == false)
							nonDupplicatedServices.Add(i);
					}

					h.response.services = new string[nonDupplicatedServices.Count];
					h.response.versions = new string[nonDupplicatedServices.Count];

					for (int i = 0; i < nonDupplicatedServices.Count; i++)
					{
						h.response.services[i] = this.servicesVersions[nonDupplicatedServices[i]][0];
						h.response.versions[i] = this.servicesVersions[nonDupplicatedServices[i]][1];
					}
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}
	}
}