using NGTools;
using NGTools.Network;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace NGToolsEditor.Network
{
	public sealed class AutoDetectUDPListener
	{
		public const double	UDPServerPingLifetime = AutoDetectUDPClient.UDPPingInterval * 3D;

		public event Action<NGServerInstance>	NewServer;
		public event Action<NGServerInstance>	UpdateServer;
		public event Action<NGServerInstance>	KillServer;

		public List<NGServerInstance>	NGServerInstances = new List<NGServerInstance>();

		private UdpClient				UDPBroadcastServer;
		private IPEndPoint				clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
		private AsyncCallback			callback;

		public	AutoDetectUDPListener(int portMin, int portMax)
		{
			this.callback = new AsyncCallback(this.OnUDPBroadcastPacketReceived);

			if (this.UDPBroadcastServer != null && this.UDPBroadcastServer.Client.Connected == true)
				this.UDPBroadcastServer.Close();

			for (int port = portMin; port <= portMax; port++)
			{
				try
				{
					this.UDPBroadcastServer = new UdpClient(port);
					this.UDPBroadcastServer.BeginReceive(this.callback, null);
					break;
				}
				catch (SocketException)
				{
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException(ex);
					this.UDPBroadcastServer = null;
				}
			}

			Utility.RegisterIntervalCallback(this.CheckNGServersAlive, 200);
		}

		public void	Stop()
		{
			Utility.UnregisterIntervalCallback(this.CheckNGServersAlive);
			if (this.UDPBroadcastServer != null)
				this.UDPBroadcastServer.Close();
		}

		public NGServerInstance	Find(string endPoint)
		{
			NGServerInstance	instance = null;

			lock (this.NGServerInstances)
			{
				instance = this.NGServerInstances.Find(s => s.endPoint == endPoint);
			}

			return instance;
		}

		public NGServerInstance	Find(Client client)
		{
			NGServerInstance	instance = null;

			lock (this.NGServerInstances)
			{
				instance = this.NGServerInstances.Find(s => s.client == client);
			}

			return instance;
		}

		public NGServerInstance	AddServer(string deviceName, string endPoint)
		{
			NGServerInstance	instance = new NGServerInstance() { deviceName = deviceName, endPoint = endPoint, pingMaxLastTime = Utility.ConvertToUnixTimestamp(DateTime.Now) + AutoDetectUDPListener.UDPServerPingLifetime };

			lock (this.NGServerInstances)
			{
				this.NGServerInstances.Add(instance);
			}

			if (this.NewServer != null)
				this.NewServer(instance);

			return instance;
		}

		private void	CheckNGServersAlive()
		{
			lock (this.NGServerInstances)
			{
				double	now = Utility.ConvertToUnixTimestamp(DateTime.Now);

				for (int i = 0; i < this.NGServerInstances.Count; i++)
				{
					if (this.NGServerInstances[i].pingMaxLastTime + AutoDetectUDPListener.UDPServerPingLifetime < now)
					{
						//InternalNGDebug.InternalLog(this.NGServerInstances[i].endPoint + " is dead");
						NGServerInstance	instance = this.NGServerInstances[i];

						if (this.TryKillServer(instance) == true)
							--i;
					}
				}
			}
		}

		private bool	TryKillServer(NGServerInstance instance)
		{
			if (instance.client == null)
			{
				this.NGServerInstances.Remove(instance);

				if (this.KillServer != null)
					this.KillServer(instance);

				return true;
			}

			return false;
		}

		private void	OnUDPBroadcastPacketReceived(IAsyncResult ar)
		{
			lock (this.clientEndPoint)
			{
				byte[]	data = this.UDPBroadcastServer.EndReceive(ar, ref this.clientEndPoint);
				string	endPoint = this.clientEndPoint.ToString();

				lock (this.NGServerInstances)
				{
					try
					{
						int	n = this.NGServerInstances.FindIndex((s) => s.endPoint.EndsWith(endPoint));

						if (n == -1 && this.FirstBytesAreEqual(data, AutoDetectUDPClient.UDPPingMessage) == true)
						{
							ByteBuffer	b = Utility.GetBBuffer(data);
							b.Position = AutoDetectUDPClient.UDPPingMessage.Length;
							string		deviceName = b.ReadUnicodeString();
							string		additionalInformation = b.ReadUnicodeString();

							Utility.RestoreBBuffer(b);
							//InternalNGDebug.InternalLog("Add " + name + " (" + BitConverter.ToString(data) + ").");
							this.NGServerInstances.Add(new NGServerInstance() { deviceName = deviceName, endPoint = endPoint, additionalInformation = additionalInformation, pingMaxLastTime = Utility.ConvertToUnixTimestamp(DateTime.Now) + AutoDetectUDPListener.UDPServerPingLifetime });

							if (this.NewServer != null)
								this.NewServer(this.NGServerInstances[this.NGServerInstances.Count - 1]);
						}
						else
						{
							if (this.FirstBytesAreEqual(data, AutoDetectUDPClient.UDPEndMessage) == true)
							{
								//InternalNGDebug.InternalLog("Kill " + content + " (" + BitConverter.ToString(data) + ").");
								this.TryKillServer(this.NGServerInstances[n]);
							}
							else if (this.FirstBytesAreEqual(data, AutoDetectUDPClient.UDPPingMessage) == true)
							{
								ByteBuffer	b = Utility.GetBBuffer(data);
								b.Position = AutoDetectUDPClient.UDPPingMessage.Length;
								string		deviceName = b.ReadUnicodeString();
								string		additionalInformation = b.ReadUnicodeString();

								Utility.RestoreBBuffer(b);
								//InternalNGDebug.InternalLog("Alive " + content + " (" + BitConverter.ToString(data) + ").");
								this.NGServerInstances[n].pingMaxLastTime = Utility.ConvertToUnixTimestamp(DateTime.Now) + AutoDetectUDPListener.UDPServerPingLifetime;

								if (this.NGServerInstances[n].deviceName != deviceName ||
									this.NGServerInstances[n].additionalInformation != additionalInformation)
								{
									if (this.NGServerInstances[n].deviceName != deviceName)
										this.NGServerInstances[n].deviceName = deviceName;

									if (this.NGServerInstances[n].additionalInformation != additionalInformation)
										this.NGServerInstances[n].additionalInformation = additionalInformation;

									if (this.UpdateServer != null)
										this.UpdateServer(this.NGServerInstances[n]);
								}
							}
							else
								InternalNGDebug.InternalLog("Unknown UDP ping (" + BitConverter.ToString(data) + ").");
						}
					}
					catch
					{
					}
				}

				this.UDPBroadcastServer.BeginReceive(this.callback, null);
			}
		}

		private bool	FirstBytesAreEqual(byte[] a, byte[] b)
		{
			for (int i = 0; i < b.Length; i++)
			{
				if (a[i] != b[i])
					return false;
			}
			return true;
		}
	}
}