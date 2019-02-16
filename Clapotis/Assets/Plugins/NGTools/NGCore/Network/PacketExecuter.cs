using System;
using System.Collections.Generic;

namespace NGTools.Network
{
	public class PacketExecuter
	{
		public enum ExecutionStatus
		{
			Handled,
			Unhandled,
			NewlyDemandingPacket,
			AlreadyDemandingPacket
		}

		private Dictionary<int, Action<Client, Packet>>	packets = new Dictionary<int, Action<Client, Packet>>();
		private bool									workingOnDemandingPacket;
		public bool										WorkingOnDemandingPacket { get { return this.workingOnDemandingPacket; } }

		private int	targetPacketId;
		public int	TargetPacketId { get { return this.targetPacketId; } }

		public ExecutionStatus	ExecutePacket(Client sender, Packet packet)
		{
			Action<Client, Packet>	callback;
			bool					isPotentialBigPacket = packet is IPotentialMemoryDemandingPacket;

			if (isPotentialBigPacket == true)
			{
				if (this.workingOnDemandingPacket == true)
					return ExecutionStatus.AlreadyDemandingPacket;

				this.workingOnDemandingPacket = true;
			}

			if (this.packets.TryGetValue(packet.packetId, out callback) == true)
			{
				callback(sender, packet);

				if (isPotentialBigPacket == true)
				{
					Packet	p = sender.GetPacketFromNetworkIdInPendingPackets(packet.NetworkId);

					if (p == null)
						throw new Exception();

					this.targetPacketId = p.packetId;

					return ExecutionStatus.NewlyDemandingPacket;
				}

				return ExecutionStatus.Handled;
			}

			return ExecutionStatus.Unhandled;
		}

		public void	ClearDemandingPacket()
		{
			this.workingOnDemandingPacket = false;
		}

		public void	HandlePacket(int packetId, Action<Client, Packet> callback)
		{
			if (this.packets.ContainsKey(packetId) == true)
				this.packets[packetId] += callback;
			else
				this.packets.Add(packetId, callback);
		}

		public void	UnhandlePacket(int packetId, Action<Client, Packet> callback)
		{
			if (this.packets.ContainsKey(packetId) == true)
			{
				this.packets[packetId] -= callback;
				if (this.packets[packetId] == null)
					this.packets.Remove(packetId);
			}
		}
	}
}