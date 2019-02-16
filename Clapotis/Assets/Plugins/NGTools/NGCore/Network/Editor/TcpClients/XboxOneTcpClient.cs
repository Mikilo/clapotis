#if UNITY_XBOXONE
using Multiplayer;
using NGTools;
using NGTools.Network;
using NGToolsEditor;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityAOT;
using UnityEditor.XboxOne;
using UnityEngine;
using UnityEngine.XboxOne;

namespace NGToolsEditor.Network
{
	public class XboxOneTcpClient : AbstractTcpClient
	{
		SecureTunnel					OurTcpCsSocketConnection;
		GetObjectAsyncOp<SecureTunnel>	TcpCsSocketSdaConnect;
		XboxOneEndPoint					OurTcpCsSocketServerEndpoint;

		private Client	clientAvailable;
		private string	address;
		private int		port;

		private bool	XBCopyFile(string filenames)
		{
			string	stdout = XboxOneUtils.xbcp(@"/x:" + this.address + @"/title " + filenames,
																	"",
																	"",
																	logOpts: XboxOneUtils.LogOpts.kNone);
			if (stdout.IndexOf("1 file(s) copied.") < 0)
			{
				Debug.Log("Error copying on " + this.address + ": " + filenames + Environment.NewLine + stdout);
				return false;
			}
			return true;
		}

		public override Client	CreateClient(string address, int port)
		{
			if (this.clientAvailable != null)
			{
				Client	tmp = this.clientAvailable;

				this.clientAvailable = null;
				return tmp;
			}

			string	outPath = Path.Combine(Environment.CurrentDirectory, "XboxOnePlayerBuild");

			Debug.Log("We're Box - Creating SecureTunnel to Server...");

			this.address = address;
			this.port = port;

			if (!XBCopyFile(@"xt:\serverSDA.txt " + "\"" + outPath + @"\serverSDA.txt" + "\"")) // Copy over the machine's SDA
			{
				Debug.Log("Fetching SDA failed.");
				return null;
			}


			try
			{
				string	strServerSda = File.ReadAllText(outPath + @"\serverSDA.txt");

				Debug.Log("ServerSDA=" + strServerSda);

				SecureDeviceAddress	serverSda = new SecureDeviceAddress(strServerSda);

				// Setup a Secure Tunnel for TCP
				Debug.Log("Initiating TcpC#Socket SecureTunnel connection...");
				TcpCsSocketSdaConnect = NetworkingManager.CreateSecureTunnelAsync("TcpCsSocketServer", serverSda);
				if (TcpCsSocketSdaConnect == null)
					Debug.Log("Error creating TCP connection to server (Connection initiation is null)");
				else
					TcpCsSocketSdaConnect.Callback += this.CreatedTcpCsSocketSecureTunnelToServer;

				Debug.Log("Waiting for an answer...");
			}
			catch (Exception ex)
			{
				Debug.Log("ERROR: Unable to create server SDA" + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace);
			}

			return null;
		}

		private void	CreatedTcpCsSocketSecureTunnelToServer(SecureTunnel st, GetObjectAsyncOp<SecureTunnel> op)
		{
			if (op.Success)
			{
				Debug.Log("CsSocketTcp Secure Tunnel to server created!");
				this.OurTcpCsSocketConnection = st;
				this.OurTcpCsSocketConnection.OnStateChanged += this.StateChanged; // Notify us if the state of this connection changes
				this.TcpCsSocketConnectToServer(st);
			}
			else
			{
				Debug.Log("CsSocketTcp Error creating Secure Tunnel to server: " + op.Result.ToString("X"));
			}
		}

		private void	TcpCsSocketConnectToServer(SecureTunnel st)
		{
			Debug.Log("CsSocketTcp Creating XboxOneEndPoint...");
			if (this.OurTcpCsSocketConnection.State != SecureTunnelState.Ready)
			{
				Debug.Log("CsSocketTcp Aborting - SecureTunnel is not Ready.");
				return;
			}

			this.OurTcpCsSocketServerEndpoint = new XboxOneEndPoint(st);
			this.TcpSendMessageToServer(this.OurTcpCsSocketServerEndpoint);
		}

		private void	StateChanged(SecureTunnel secureTunnel, SecureTunnelState oldState, SecureTunnelState newState)
		{
			Debug.Log("Connection State Changed from " + oldState.ToString() + " to " + newState.ToString());
		}

		private void	TcpSendMessageToServer(XboxOneEndPoint ServerEndpoint)
		{
			// If you call this function again too quickly, the local port we bound to may not have been freed up yet, and you'll get an exception (10048)
			try
			{
				TcpClient	tcpClient = new TcpClient();
				tcpClient.Client = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
				tcpClient.Client.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, 0); // Mandatory, as discussed above
				tcpClient.Client.Bind(new IPEndPoint(IPAddress.IPv6Any, this.port)); //
				tcpClient.Client.Connect(ServerEndpoint);

				this.clientAvailable = new Client(tcpClient);

				Debug.Log("Client is available. Please click Connect again to start the transmission.");

				//// Get the stream to the server
				//NetworkStream stream = new NetworkStream(client);

				//// Read the server greeting
				//Byte[]data = new Byte[256];
				//String responseData = String.Empty;
				//Int32 bytes = stream.Read(data, 0, data.Length);
				//responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

				//// Write our response to the server
				//Byte[] OutData = System.Text.Encoding.ASCII.GetBytes("TCP Client Response");
				//stream.Write(OutData, 0, OutData.Length);

				//// Disconnect
				//stream.Close();
				//client.Shutdown(SocketShutdown.Both);
				//client.Disconnect(true);
				//client.Close();
				//Debug.Log("Sent message to server, received response: " + responseData);
			}
			catch (ArgumentNullException e)
			{
				Debug.Log("ArgumentNullException: " + e);
			}
			catch (SocketException e)
			{
				Debug.Log("SocketException: " + e.ErrorCode + "," + e.Message);
			}
			catch (Exception e)
			{
				Debug.Log("Exception: " + e);
			}
		}
	}
}
#endif