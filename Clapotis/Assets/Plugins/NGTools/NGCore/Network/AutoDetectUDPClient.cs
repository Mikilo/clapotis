using System;
using System.Collections;
using System.Net;
#if !NETFX_CORE
using System.Net.Sockets;
#endif
using UnityEngine;

namespace NGTools.Network
{
	public sealed class AutoDetectUDPClient
	{
		public const int				UDPPortBroadcastMin = 6550;
		public const int				UDPPortBroadcastMax = 6557;
		public const int				DefaultPort = 17255;
		public const float				UDPPingInterval = 3F;
		public readonly static byte[]	UDPPingMessage = new byte[] { (byte)'N', (byte)'G', (byte)'S', (byte)'S' };
		public readonly static byte[]	UDPEndMessage = new byte[] { (byte)'E', (byte)'N' };
		public const char				AdditionalInformationCharSplitter = (char)2;

		private MonoBehaviour	behaviour;

#if NETFX_CORE
		private Windows.Networking.Sockets.DatagramSocket	Client;
#else
		private UdpClient		Client;
#endif
		private IPEndPoint[]	BroadcastEndPoint;
		private float			pingInterval;

		private Coroutine	pingCoroutine;

		public byte[]	pingContent;

		public	AutoDetectUDPClient(MonoBehaviour behaviour, int port, int targetPortMin, int targetPortMax, float pingInterval, string additionalInformation)
		{
			this.behaviour = behaviour;
			this.pingInterval = pingInterval;

#if NETFX_CORE
			this.Client = new Windows.Networking.Sockets.DatagramSocket();
			this.Client.BindServiceNameAsync(port.ToString()).AsTask().Wait();
#else
			this.Client = new UdpClient(port);
			this.Client.EnableBroadcast = true;
#endif

			this.BroadcastEndPoint = new IPEndPoint[targetPortMax - targetPortMin + 1];

			for (int i = 0; i < this.BroadcastEndPoint.Length; i++)
#if UNITY_WSA_8_1 || UNITY_WP_8_1
			{
				this.BroadcastEndPoint[i] = new IPEndPoint();
				this.BroadcastEndPoint[i].Port = targetPortMin + i;
			}
#else
				this.BroadcastEndPoint[i] = new IPEndPoint(IPAddress.Broadcast, targetPortMin + i);
#endif

			this.SetAdditionalInformation(additionalInformation);
			this.pingCoroutine = this.behaviour.StartCoroutine(this.AsyncSendPing());
		}

		public void	SetAdditionalInformation(string content)
		{
			ByteBuffer	b = Utility.GetBBuffer(AutoDetectUDPClient.UDPPingMessage);
			b.AppendUnicodeString(SystemInfo.deviceName);
			b.AppendUnicodeString(content);
			this.pingContent = Utility.ReturnBBuffer(b);
		}

		public void	Stop()
		{
			this.behaviour.StopCoroutine(this.pingCoroutine);

			for (int i = 0; i < this.BroadcastEndPoint.Length; i++)
#if NETFX_CORE
			{
				var action = this.Client.GetOutputStreamAsync(new Windows.Networking.HostName("127.0.0.1"), this.BroadcastEndPoint[i].Port.ToString());
				//var action = this.Client.GetOutputStreamAsync(this.Client.Information.LocalAddress, this.BroadcastEndPoint[i].Port.ToString());
				action.AsTask().Wait();
				var	outputStream = action.GetResults();
				var	writer = new Windows.Storage.Streams.DataWriter(outputStream);
				writer.WriteBytes(AutoDetectUDPClient.UDPEndMessage);
				writer.StoreAsync().AsTask().Wait();
			}
#else
				this.Client.Send(AutoDetectUDPClient.UDPEndMessage, AutoDetectUDPClient.UDPEndMessage.Length, this.BroadcastEndPoint[i]);
#endif

#if NETFX_CORE
			this.Client.Dispose();
#else
			this.Client.Close();
#endif
		}

		private IEnumerator	AsyncSendPing()
		{
#if !NETFX_CORE
			AsyncCallback	callback = new AsyncCallback(this.SendPresence);
#endif
			WaitForSeconds	wait = new WaitForSeconds(this.pingInterval);

			while (true)
			{
				for (int i = 0; i < this.BroadcastEndPoint.Length; i++)
#if NETFX_CORE
				{
					var action = this.Client.GetOutputStreamAsync(new Windows.Networking.HostName("127.0.0.1"), this.BroadcastEndPoint[i].Port.ToString());
					//var action = this.Client.GetOutputStreamAsync(this.Client.Information.LocalAddress, this.BroadcastEndPoint[i].Port.ToString());
					action.AsTask().Wait();
					var	outputStream = action.GetResults();
					var	writer = new Windows.Storage.Streams.DataWriter(outputStream);
					writer.WriteBytes(AutoDetectUDPClient.UDPPingMessage);
					writer.StoreAsync().AsTask().Wait();
				}
#else
					this.Client.BeginSend(this.pingContent, this.pingContent.Length, this.BroadcastEndPoint[i], callback, null);
#endif

				yield return wait;
			}
		}

#if !NETFX_CORE
		private void	SendPresence(IAsyncResult ar)
		{
			this.Client.EndSend(ar);
		}
#endif
	}
}