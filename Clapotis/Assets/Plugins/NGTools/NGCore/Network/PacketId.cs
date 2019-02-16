using System;
using System.Collections.Generic;
using System.Reflection;

namespace NGTools.Network
{
	[RegisterPacketIds]
	internal static class PacketId
	{
		public const int	Ack = 1;
		public const int	BusyAck = 2;
		public const int	PartialPacket = 3;
		public const int	PartialResponsePacket = 4;
		public const int	NotifyErrors = 5;
		public const int	ServerHasDisconnect = 6;
		public const int	ClientHasDisconnect = 7;
		public const int	ClientSendPing = 8;
		public const int	ServerAnswerPing = 9;
		public const int	ClientRequestServices = 10;
		public const int	ServerSendServices = 11;

		private static Dictionary<int, string>	packetNames = new Dictionary<int, string>(64);

		static	PacketId()
		{
			foreach (Type t in Utility.EachAllSubClassesOf(typeof(object)))
			{
				if (t.IsDefined(typeof(RegisterPacketIdsAttribute), false) == true)
					PacketId.RegisterPackets(t);
			}
		}

		public static string	GetPacketName(int id)
		{
			return packetNames[id];
		}

		public static void	RegisterPackets(Type type)
		{
			FieldInfo[]	fis = type.GetFields();

			for (int i = 0; i < fis.Length; i++)
				packetNames.Add((int)fis[i].GetRawConstantValue(), fis[i].Name);
		}
	}
}