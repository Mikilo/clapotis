#if UNITY_XBOXONE
using Multiplayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace NGTools.Network
{
	public class XboxOneTcpListener : AbstractTcpListener
	{
		private static SecureDeviceAddress	localSda = new SecureDeviceAddress();

		string	outputPath = @"T:\"; // Temporary partition - we store the SDA's here

		private SecureTunnelListener	TcpCsSocketDetectIncoming;
		private List<SecureTunnel>		TcpCsSocketIncomingTunnels = new List<SecureTunnel>();
		private String					strLocalSda;

		public override void	StartServer()
		{
			if (string.IsNullOrEmpty(strLocalSda) == true)
				strLocalSda = localSda.ToString(); // Convert the SDA to a base64 string for easy display and transport

			Debug.Log("Local Device Address: " + strLocalSda);
			File.WriteAllText(outputPath + "serverSDA.txt", strLocalSda);

			this.tcpListener = new TcpListener(IPAddress.IPv6Any, port);
			this.tcpListener.Server.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, 0); // MANDATORY - Allow incoming IPv4 packets (which includes Teredo-wrapped IPv4 packets)
			this.tcpListener.Start();
			this.tcpListener.BeginAcceptSocket(new AsyncCallback(this.DataReceivedTCP), this.tcpListener);
			this.TcpCsSocketDetectIncoming = new SecureTunnelListener("TcpCsSocketServer");
			this.TcpCsSocketDetectIncoming.OnIncomingSecureTunnel += this.DetectTcpCsSocketIncomingConnection;
		}

		private void	DataReceivedTCP(IAsyncResult ar)
		{
			Debug.Log("CsSocketTcp Incoming connection");
			try
			{
				Client	client = new Client(this.tcpListener.EndAcceptTcpClient(ar));

				this.clients.Add(client);

				//TcpListener listener = (TcpListener)ar.AsyncState; // Get the passed-in listener

				//TcpClient client = listener.EndAcceptTcpClient(ar); // Accept the connect, and retrieve the remote client object

				//// Get client client stream
				//NetworkStream stream = client.GetStream();

				//Debug.Log("Sending greeting");
				//// Write our server greeting
				//Byte[] OutData = System.Text.Encoding.ASCII.GetBytes("A1A");
				//stream.Write(OutData, 0, OutData.Length);

				//Debug.Log("Waiting for reply");
				//// Get the client's reply
				//Byte[]data = new Byte[256];
				//String responseData = String.Empty;
				//Int32 bytes = stream.Read(data, 0, data.Length);
				//responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

				//Debug.Log("Disconnecting");
				//// Disconnect
				//stream.Close();
				//client.Close();
				//Debug.Log("Received a message from client: " + responseData);
			}
			catch (ArgumentNullException e)
			{
				Debug.Log("ArgumentNullException: " + e);
			}
			catch (SocketException e)
			{
				Debug.Log("SocketException: " + e);
			}
			catch (Exception e)
			{
				Debug.Log("Exception: " + e);
			}
			finally
			{
				this.tcpListener.BeginAcceptTcpClient(new AsyncCallback(this.DataReceivedTCP), this.tcpListener);
				//listener.BeginAcceptSocket(new AsyncCallback(DataReceivedTCP), listener); // Set up to receive the next packet
			}
		}

		private void	DetectTcpCsSocketIncomingConnection(uint hresult, SecureTunnelListener listener, SecureTunnel tunnel)
		{
			if (this.TcpCsSocketIncomingTunnels.Contains(tunnel))
				return;
			if (hresult != 0)
			{
				Debug.Log("CsSocketTcp Incoming Connection Exception HResult: " + hresult);
				return;
			}

			Debug.Log("CsSocketTcp Incoming Connection to our port " + tunnel.LocalPort + ": " + tunnel.State + "," + tunnel.RemoteHostName.CanonicalName + ":" + tunnel.RemotePort);
			tunnel.OnStateChanged += StateChanged; // Notify us if the state of this connection changes
			this.TcpCsSocketIncomingTunnels.Add(tunnel);
		}

		private void	StateChanged(SecureTunnel secureTunnel, SecureTunnelState oldState, SecureTunnelState newState)
		{
			Debug.Log("Connection State Changed from " + oldState.ToString() + " to " + newState.ToString());
		}
	}
}
#endif