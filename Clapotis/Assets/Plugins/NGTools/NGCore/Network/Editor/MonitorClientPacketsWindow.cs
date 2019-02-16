using NGTools;
using NGTools.Network;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.Network
{
	public class MonitorClientPacketsWindow : EditorWindow
	{
		public int	offsetSent = 0;
		public int	offsetReceived = 0;
		public int	maxPacketsDisplay = 100;

		private Client	client;
		private string	localEndPoint;
		private Vector2	sendScrollPosition;
		private Vector2	receiveScrollPosition;
		private Vector2	pendingScrollPosition;

		public static void	Open(Client client)
		{
			MonitorClientPacketsWindow	window = EditorWindow.CreateInstance<MonitorClientPacketsWindow>();

			window.client = client;
			window.localEndPoint = client.tcpClient.Client.LocalEndPoint.ToString();
			window.titleContent.text = "Monitor Client";
			window.Show();
		}

		protected virtual void	OnEnable()
		{
			Utility.LoadEditorPref(this);
			Utility.RegisterIntervalCallback(this.Repaint, 50);
		}

		protected virtual void	OnDisable()
		{
			Utility.SaveEditorPref(this);
			Utility.UnregisterIntervalCallback(this.Repaint);
		}

		protected virtual void	OnGUI()
		{
			if (this.client == null)
			{
				foreach (EditorWindow window in Resources.FindObjectsOfTypeAll(typeof(EditorWindow)))
				{
					INGServerConnectable	w = window as INGServerConnectable;

					if (w != null && w.Client != null && GUILayout.Button(window.titleContent.text + ' ' + w.Client) == true)
					{
						MonitorClientPacketsWindow.Open(w.Client);
						break;
					}
				}

				return;
			}

			EditorGUILayout.BeginHorizontal();
			{
				this.offsetSent = EditorGUILayout.IntField("Offset Sent", this.offsetSent);
				this.offsetReceived = EditorGUILayout.IntField("Offset Received", this.offsetReceived);
				this.maxPacketsDisplay = EditorGUILayout.IntField("Max Packets Display", this.maxPacketsDisplay);
			}
			EditorGUILayout.EndHorizontal();

			if (this.client.tcpClient.Client.Connected == true)
				GUILayout.Label("Client " + this.localEndPoint.ToString() + " " + DateTime.Now.ToString("HH:mm:ss"));
			else
				GUILayout.Label("Client " + this.localEndPoint.ToString() + " Disconnected " + DateTime.Now.ToString("HH:mm:ss"));
			GUILayout.Label(this.client.ToString());

			if (SafeUnwrapByteBuffer.dumps.Count > 0)
			{
				for (int i = 0; i < SafeUnwrapByteBuffer.dumps.Count; i++)
				{
					string	fileName = "SafeUnwrapDump_" + SafeUnwrapByteBuffer.dumps[i].error + "_" + SafeUnwrapByteBuffer.dumps[i].start + "_" + SafeUnwrapByteBuffer.dumps[i].position + "_" + SafeUnwrapByteBuffer.dumps[i].end + ".dump";

					if (File.Exists(fileName) == true)
					{
						if (GUILayout.Button("Open " + SafeUnwrapByteBuffer.dumps[i].error) == true)
							EditorUtility.OpenWithDefaultApp(fileName);
					}
					else if (GUILayout.Button(SafeUnwrapByteBuffer.dumps[i].error) == true)
					{
						using (FileStream dumpStream = File.OpenWrite(fileName))
						{
							dumpStream.Write(SafeUnwrapByteBuffer.dumps[i].buffer, SafeUnwrapByteBuffer.dumps[i].start, SafeUnwrapByteBuffer.dumps[i].end - SafeUnwrapByteBuffer.dumps[i].start);
						}
					}
				}
			}

			GUILayout.BeginHorizontal();
			{
				GUILayout.BeginVertical();
				{
					GUILayout.Label("Sent (" + this.client.sentPacketsHistoric.Count + ")");
					this.sendScrollPosition = GUILayout.BeginScrollView(this.sendScrollPosition);
					{
						for (int i = this.client.sentPacketsHistoric.Count - 1 - this.offsetSent, j = 0; i >= 0 && j < this.maxPacketsDisplay; --i, ++j)
							GUILayout.Label(this.client.sentPacketsHistoric[i].ReadableSendTime + " #" + this.client.sentPacketsHistoric[i].packet.NetworkId + " " + this.client.sentPacketsHistoric[i].packet.ToString());
					}
					GUILayout.EndScrollView();
				}
				GUILayout.EndVertical();

				GUILayout.BeginVertical();
				{
					GUILayout.Label("Received (" + this.client.receivedPacketsHistoric.Count + ")");
					this.receiveScrollPosition = GUILayout.BeginScrollView(this.receiveScrollPosition);
					{
						for (int i = this.client.receivedPacketsHistoric.Count - 1 - this.offsetReceived, j = 0; i >= 0 && j < this.maxPacketsDisplay; --i, ++j)
							GUILayout.Label(this.client.receivedPacketsHistoric[i]);
					}
					GUILayout.EndScrollView();
				}
				GUILayout.EndVertical();

				GUILayout.BeginVertical();
				{
					GUILayout.Label("Pending (" + this.client.sentPacketsPending.Count + ")");
					this.pendingScrollPosition = GUILayout.BeginScrollView(this.pendingScrollPosition);
					{
						for (int i = this.client.sentPacketsPending.Count - 1 - this.offsetReceived, j = 0; i >= 0 && j < this.maxPacketsDisplay; --i, ++j)
							GUILayout.Label(this.client.sentPacketsPending[i].ReadableSendTime + " #" + this.client.sentPacketsPending[i].packet.NetworkId + " " + this.client.sentPacketsPending[i].packet.ToString());
					}
					GUILayout.EndScrollView();
				}
				GUILayout.EndVertical();
			}
			GUILayout.EndHorizontal();
		}
	}
}