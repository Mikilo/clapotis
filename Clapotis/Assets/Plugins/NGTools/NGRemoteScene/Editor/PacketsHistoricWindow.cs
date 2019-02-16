using NGTools;
using NGTools.Network;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	public class PacketsHistoricWindow : NGRemoteWindow
	{
		private new const string	Title = "Packets Historic";

		private Vector2	scrollBatchPosition;
		private int		lastPacketsCount;
		private int		filterByPacketId;
		private bool	pickingFilter;
		private bool	hidePingPackets;

		public static void	Open(NGRemoteHierarchyWindow hierarchy)
		{
			Utility.OpenWindow<PacketsHistoricWindow>(true, PacketsHistoricWindow.Title, true, null, w => w.SetHierarchy(hierarchy));
		}

		protected override void	OnEnable()
		{
			base.OnEnable();

			this.wantsMouseMove = true;
		}

		protected override void	OnGUIHeader()
		{
			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				using (LabelWidthRestorer.Get(70F))
				{
					Utility.content.text = "Filter By";
					Rect	r = GUILayoutUtility.GetRect(Utility.content, GUI.skin.label, GUILayoutOptionPool.Width(80F));
					r.y -= 2F;
					if (GUI.Button(r, Utility.content.text) == true)
					{
						this.filterByPacketId = 0;

						if (this.Hierarchy.IsClientConnected() == true)
							this.pickingFilter = !this.pickingFilter;
						else
							this.pickingFilter = false;
					}

					if (this.pickingFilter == true)
					{
						Utility.content.text = "Click on a packet";
						r = GUILayoutUtility.GetRect(Utility.content, GUI.skin.label);
						r.width += 20F;
						Utility.DrawRectDotted(r, this.position, Color.grey, 0.02F, 0F);
						r.x += 10F;
						GUI.Label(r, Utility.content.text);
					}
					else if (this.filterByPacketId != 0)
					{
						Utility.content.text = PacketId.GetPacketName(this.filterByPacketId);
						r = GUILayoutUtility.GetRect(Utility.content, GUI.skin.label);
						GUI.Label(r, Utility.content.text);

						if (r.Contains(Event.current.mousePosition) == true && Event.current.type == EventType.MouseDown)
						{
							this.filterByPacketId = 0;
							this.Repaint();
							Event.current.Use();
						}
					}
				}

				GUILayout.FlexibleSpace();

				this.hidePingPackets = GUILayout.Toggle(this.hidePingPackets, "Hide Ping", GeneralStyles.ToolbarToggle);

				GUILayout.FlexibleSpace();
			}
			EditorGUILayout.EndHorizontal();
		}

		protected override void	OnGUIConnected()
		{
			if (Event.current.type == EventType.MouseMove)
				this.Repaint();
			else if (Event.current.type == EventType.KeyUp)
			{
				if (Event.current.keyCode == KeyCode.LeftControl || Event.current.keyCode == KeyCode.RightControl)
				{
					this.filterByPacketId = 0;
					this.pickingFilter = !this.pickingFilter;
					this.Repaint();
					Event.current.Use();
				}
			}

			float	height = 0F;

			for (int i = this.Hierarchy.Client.sentPacketsHistoric.Count - 1; i >= 0; --i)
			{
				Client.InPipePacket	inPacket = this.Hierarchy.Client.sentPacketsHistoric[i];

				if (this.hidePingPackets == true && inPacket.packet is ClientSendPingPacket)
					continue;

				if (this.filterByPacketId != 0 && this.filterByPacketId != inPacket.packet.packetId)
					continue;

				height += Constants.SingleLineHeight;
			}

			Rect bodyRect = new Rect(0F, Constants.SingleLineHeight*2F+4F, this.position.width, this.position.height - (Constants.SingleLineHeight*2F+4F));
			Rect	viewRect = new Rect(0F, 0F, 0F, height);
			Rect	r = new Rect(0F, 0F, this.position.width, Constants.SingleLineHeight);

			this.scrollBatchPosition = GUI.BeginScrollView(bodyRect, this.scrollBatchPosition, viewRect);
			{
				if (viewRect.height >= bodyRect.height)
					bodyRect.width -= 16F;

				float	indexWidth = 18F;
				int		max = this.Hierarchy.Client.sentPacketsHistoric.Count;

				while (max >= 10)
				{
					max /= 10;
					indexWidth += 8F;
				}

				for (int i = this.Hierarchy.Client.sentPacketsHistoric.Count - 1; i >= 0; --i)
				{
					Client.InPipePacket	inPacket = this.Hierarchy.Client.sentPacketsHistoric[i];

					if (this.hidePingPackets == true && inPacket.packet is ClientSendPingPacket)
						continue;

					if (this.filterByPacketId != 0 && this.filterByPacketId != inPacket.packet.packetId)
						continue;

					if (r.y + r.height <= this.scrollBatchPosition.y)
					{
						r.y += r.height;
						continue;
					}

					r.x = 0F;
					r.width = this.position.width;

					if (this.pickingFilter == true)
					{
						if (r.Contains(Event.current.mousePosition) == true)
						{
							if (Event.current.type == EventType.Repaint)
								Utility.DrawUnfillRect(r, Color.yellow);
							else if (Event.current.type == EventType.MouseDown)
							{
								this.filterByPacketId = inPacket.packet.packetId;
								this.pickingFilter = false;
								this.Repaint();
							}
						}
					}

					if (Conf.DebugMode != Conf.DebugState.None && r.yMin < Event.current.mousePosition.y && r.yMax > Event.current.mousePosition.y)
					{
						r.width = 60F;
						if (Event.current.type == EventType.MouseDown)
							this.Hierarchy.Client.AddPacket(inPacket.packet);
					}

					r.width = indexWidth;

					EditorGUI.LabelField(r, (i + 1).ToCachedString());
					r.x += r.width;

					r.width = 90F;
					GUI.Label(r, inPacket.ReadableSendTime);
					r.x += r.width;

					r.width = bodyRect.width - r.x;
					GUILayout.BeginArea(r);
					{
						GUILayout.BeginHorizontal();
						{
							IGUIPacket	clientPacket = inPacket.packet as IGUIPacket;

							if (clientPacket != null)
								clientPacket.OnGUI(this.Hierarchy);
							else
								GUILayout.Label(inPacket.packet.GetType().Name);
						}
						GUILayout.EndHorizontal();
					}
					GUILayout.EndArea();

					r.x = 0F;
					r.width = this.position.width;

					if (Conf.DebugMode != Conf.DebugState.None && r.yMin < Event.current.mousePosition.y && r.yMax > Event.current.mousePosition.y)
					{
						r.width = 60F;
						GUI.Button(r, LC.G("NGInspector_Resend"));
					}

					r.y += r.height;

					if (r.y - this.scrollBatchPosition.y > bodyRect.height)
						break;
				}
			}
			GUI.EndScrollView();
		}

		protected virtual void	Update()
		{
			if (this.Hierarchy != null && this.Hierarchy.IsClientConnected() == true &&
				this.lastPacketsCount != this.Hierarchy.Client.sentPacketsHistoric.Count)
			{
				this.lastPacketsCount = this.Hierarchy.Client.sentPacketsHistoric.Count;
				this.Repaint();
			}
		}
	}
}