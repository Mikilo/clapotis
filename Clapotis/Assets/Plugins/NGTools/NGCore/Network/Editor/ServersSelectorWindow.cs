using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.Network
{
	public class ServersSelectorWindow : PopupWindowContent
	{
		public const string	Title = "Servers";
		public const float	WindowWidth = 450F;
		public const float	WindowMargin = 2F;
		public const float	ElementHeight = 64F;
		public const float	DeviceNameHeight = 24F;
		public const float	EndPointHeight = 24F;
		public const float	AdditionalInformationHeight = ServersSelectorWindow.ElementHeight - ServersSelectorWindow.DeviceNameHeight - ServersSelectorWindow.EndPointHeight;
		public const float	UsersWidth = 140F;
		public const float	ButtonActionWidth = 100F;

		private readonly INGServerConnectable	connectable;

		public	ServersSelectorWindow(INGServerConnectable connectable)
		{
			this.connectable = connectable;
		}

		public override void	OnOpen()
		{
			this.editorWindow.wantsMouseMove = true;
			this.editorWindow.wantsMouseEnterLeaveWindow = true;

			ConnectionsManager.UpdateServer += this.OnInstanceUpdated;
		}

		public override void	OnClose()
		{
			ConnectionsManager.UpdateServer -= this.OnInstanceUpdated;
		}

		public override Vector2	GetWindowSize()
		{
			lock (ConnectionsManager.Servers)
			{
				return new Vector2(ServersSelectorWindow.WindowWidth, ConnectionsManager.Servers.Count * ServersSelectorWindow.ElementHeight + 4F);
			}
		}

		public override void	OnGUI(Rect r)
		{
			List<NGServerInstance>	servers = ConnectionsManager.Servers;

			if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseLeaveWindow)
				this.editorWindow.Repaint();

			lock (ConnectionsManager.Servers)
			{
				r.yMin += ServersSelectorWindow.WindowMargin;
				r.yMax -= ServersSelectorWindow.WindowMargin;
				r.xMin += ServersSelectorWindow.WindowMargin;
				r.xMax -= ServersSelectorWindow.WindowMargin;

				float	w = r.width;

				for (int i = 0; i < servers.Count; i++)
				{
					r.height = ServersSelectorWindow.ElementHeight;
					if (Event.current.type == EventType.Repaint)
					{
						if (servers[i].client != null && this.connectable.IsConnected(servers[i].client) == true)
							Utility.DrawUnfillRect(r, Color.green);
						else if (r.Contains(Event.current.mousePosition) == true)
							Utility.DrawUnfillRect(r, Color.cyan);
						else
							GUI.Box(r, GUIContent.none);
					}

					if (servers[i].users.Count > 0)
						r.width = w - ServersSelectorWindow.UsersWidth;
					else
						r.width = w;

					r.height = ServersSelectorWindow.DeviceNameHeight;
					GUI.Label(r, servers[i].deviceName, GeneralStyles.MainTitle);
					r.y += r.height;

					r.height = ServersSelectorWindow.EndPointHeight;
					GUI.Label(r, servers[i].endPoint, GeneralStyles.MainTitle);
					r.y += r.height;

					r.height = ServersSelectorWindow.AdditionalInformationHeight;

					Rect	r4 = r;
					bool	hoveringAdditionalInformation = r4.Contains(Event.current.mousePosition);

					if (hoveringAdditionalInformation == false)
						GUI.Label(r, servers[i].additionalInformation, GeneralStyles.SmallLabel);

					r.y -= ServersSelectorWindow.DeviceNameHeight + ServersSelectorWindow.EndPointHeight;
					r.height = ServersSelectorWindow.ElementHeight;

					if (servers[i].users.Count > 0)
					{
						Rect	r3 = r;

						r3.x += r3.width;
						r3.width = 1F;
						EditorGUI.DrawRect(r3, Color.gray);

						r3.x += r3.width;
						r3.width = ServersSelectorWindow.UsersWidth - r3.width;

						r3.height = 14F;
						GUI.Label(r3, "Users:", GeneralStyles.SmallLabel);
						r3.y += r3.height;

						r3.height = 16F;

						for (int j = 0; j < servers[i].users.Count; j++)
						{
							GUI.Label(r3, servers[i].users[j].titleContent);
							r3.y += r3.height;
						}
					}

					r.width = w;

					if (r.Contains(Event.current.mousePosition) == true &&
						(servers[i].client == null ||
						 this.connectable.IsConnected(servers[i].client) == false))
					{
						Rect	r2 = r;

						r2.xMin = r2.xMax - ServersSelectorWindow.ButtonActionWidth;
						r2.x -= ServersSelectorWindow.WindowMargin;
						r2.yMin += ServersSelectorWindow.WindowMargin;
						r2.yMax -= ServersSelectorWindow.WindowMargin;

						if (GUI.Button(r2, servers[i].users.Count == 0 ? "Connect" : "Use") == true)
						{
							string	address = servers[i].endPoint.Split(':')[0];
							int		port = int.Parse(servers[i].endPoint.Split(':')[1]);
							this.connectable.Connect(address, port);
						}
					}

					if (hoveringAdditionalInformation == true)
					{
						r4.width = w;
						Utility.content.text = servers[i].additionalInformation;
						Utility.content.tooltip = servers[i].additionalInformation;
						GUI.Label(r4, Utility.content, GeneralStyles.SmallLabel);
						Utility.content.tooltip = null;
					}

					r.y += r.height;
				}
			}
		}

		private void	OnInstanceUpdated(NGServerInstance instance)
		{
			EditorApplication.delayCall += this.editorWindow.Repaint;
		}
	}
}