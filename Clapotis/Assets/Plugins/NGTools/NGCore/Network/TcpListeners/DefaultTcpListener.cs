using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace NGTools.Network
{
	public class DefaultTcpListener : AbstractTcpListener
	{
		public override void	StartServer()
		{
			if (this.tcpListener != null)
				return;

			try
			{
#if NETFX_CORE
				this.tcpListener = new Windows.Networking.Sockets.StreamSocketListener();
				this.tcpListener.ConnectionReceived += OnConnection;
				this.BindEndPointAsync();
#else
				this.tcpListener = new TcpListener(IPAddress.Any, this.port);
				this.tcpListener.Start(this.backLog);
				this.tcpListener.BeginAcceptTcpClient(new AsyncCallback(this.AcceptClient), null);
#endif

				InternalNGDebug.LogFile("Started TCPListener IPAddress.Any:" + this.port);
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException(ex);
			}
		}

#if NETFX_CORE
		private async void	BindEndPointAsync()
		{
			Debug.LogError("Binding " + this.port);
			//await this.tcpListener.BindEndpointAsync(new Windows.Networking.HostName("127.0.0.1"), this.port.ToString());
			await this.tcpListener.BindServiceNameAsync(this.port.ToString(), Windows.Networking.Sockets.SocketProtectionLevel.PlainSocket);
			Debug.LogError("Binded");
		}

		private async void	OnConnection(Windows.Networking.Sockets.StreamSocketListener sender,
										 Windows.Networking.Sockets.StreamSocketListenerConnectionReceivedEventArgs args)
		{
			Debug.LogError("New client" + sender.Information.LocalPort);

			var	reader = new Windows.Storage.Streams.DataReader(args.Socket.InputStream);
			try
			{
				while (true)
				{
					// Read first 4 bytes (length of the subsequent string).
					uint sizeFieldCount = await reader.LoadAsync(sizeof(uint));
					if (sizeFieldCount != sizeof(uint))
					{
						// The underlying socket was closed before we were able to read the whole data.
						return;
					}

					// Read the string.
					uint stringLength = reader.ReadUInt32();
					uint actualStringLength = await reader.LoadAsync(stringLength);
					if (stringLength != actualStringLength)
					{
						// The underlying socket was closed before we were able to read the whole data.
						return;
					}

					// Display the string on the screen. The event is invoked on a non-UI thread, so we need to marshal
					// the text back to the UI thread.
					//NotifyUserFromAsyncThread(
					//	String.Format("Received data: \"{0}\"", reader.ReadString(actualStringLength)),
					//	NotifyType.StatusMessage);
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				// If this is an unknown status it means that the error is fatal and retry will likely fail.
				if (Windows.Networking.Sockets.SocketError.GetStatus(ex.HResult) == Windows.Networking.Sockets.SocketErrorStatus.Unknown)
				{
					throw;
				}
			}
		}
#else
		private void	AcceptClient(IAsyncResult ar)
		{
			// TcpListener is null when the server stops.
			if (this.tcpListener == null)
				return;

			try
			{
				Client	client = new Client(this.tcpListener.EndAcceptTcpClient(ar), Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor) { debugPrefix = "S:" };
				this.clients.Add(client);

				InternalNGDebug.LogFile("Accepted Client " + client.tcpClient.Client.RemoteEndPoint);
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogFileException(ex);
			}
			finally
			{
				this.tcpListener.BeginAcceptTcpClient(new AsyncCallback(this.AcceptClient), null);
			}
		}
#endif
	}
}