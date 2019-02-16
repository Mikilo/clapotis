using System;
using System.Net;
#if !NETFX_CORE
using System.Net.Sockets;
#endif

namespace NGTools.Network
{
	public class UDPListener : NetworkListener
	{
#if NETFX_CORE
		private Windows.Networking.Sockets.DatagramSocket	client;
#else
		public UdpClient	client;
#endif

#if NETFX_CORE
		private Windows.Storage.Streams.IOutputStream  writer;
#else
		private IPEndPoint	endPoint;
		private IPEndPoint	clientEndPoint;
#endif
		private ByteBuffer	packetBuffer = new ByteBuffer(1920 * 1080 * 3);
		private ByteBuffer	sendBuffer = new ByteBuffer(1920 * 1080 * 3);

		public override void	StartServer()
		{
#if NETFX_CORE
			this.client = new Windows.Networking.Sockets.DatagramSocket();
			this.client.MessageReceived += this.MessageReceived;
			this.client.BindServiceNameAsync(this.port.ToString()).AsTask().Wait();
#else
			this.endPoint = new IPEndPoint(IPAddress.Any, this.port);
			this.clientEndPoint = new IPEndPoint(IPAddress.Any, this.port);

			this.client = new UdpClient(this.endPoint);
			this.client.EnableBroadcast = true;
			this.client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			this.client.BeginReceive(new AsyncCallback(this.ReceivedPacket), null);
#endif
			InternalNGDebug.LogFile("Started UDPListener.");
		}

		public override void	StopServer()
		{
#if NETFX_CORE
			this.client.Dispose();
#else
			this.client.Close();
#endif
			this.client = null;

			InternalNGDebug.LogFile("Stopped UDPListener.");
		}

#if NETFX_CORE
		public void	Send(Packet packet)
		{
			this.sendBuffer.Clear();
			this.packetBuffer.Clear();

			packet.Out(this.packetBuffer);
			this.sendBuffer.Append(packet.packetId);
			this.sendBuffer.Append((uint)this.packetBuffer.Length);
			this.sendBuffer.Append(this.packetBuffer);
			//Debug.Log("Sending " + packet.GetType().Name + " of " + this.sendBuffer.Length + " bytes.");

			var	writer = new Windows.Storage.Streams.DataWriter(this.writer);
			writer.WriteBytes(this.sendBuffer.Flush());
			writer.StoreAsync().AsTask().Wait();
		}

		private async void	MessageReceived(Windows.Networking.Sockets.DatagramSocket sender, Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs args)
		{
			this.writer = await this.client.GetOutputStreamAsync(args.RemoteAddress, args.RemotePort);
		}
#else
		public void	Send(Packet packet)
		{
			this.sendBuffer.Clear();
			this.packetBuffer.Clear();

			packet.Out(this.packetBuffer);
			this.sendBuffer.Append(packet.packetId);
			this.sendBuffer.Append((uint)this.packetBuffer.Length);
			this.sendBuffer.Append(this.packetBuffer);
			//Debug.Log("Sending " + packet.GetType().Name + " of " + this.sendBuffer.Length + " bytes.");

			byte[]	data = this.sendBuffer.Flush();
			this.client.BeginSend(data, data.Length, this.clientEndPoint, new AsyncCallback(this.SendPacket), null);
		}

		private void	SendPacket(IAsyncResult ar)
		{
			this.client.EndSend(ar);
			//Debug.Log("Server sent " + this.client.EndSend(ar) + " bytes.");
		}

		private void	ReceivedPacket(IAsyncResult ar)
		{
			/*byte[]	data = */this.client.EndReceive(ar, ref this.clientEndPoint);
			//Debug.Log("Server received " + data.Length + " bytes.");
			this.client.BeginReceive(new AsyncCallback(this.ReceivedPacket), null);
		}
#endif
	}
}