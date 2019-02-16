using System.Collections.Generic;

namespace NGTools.Network
{
	[PacketLinkTo(PacketId.NotifyErrors)]
	internal sealed class ErrorNotificationPacket : Packet
	{
		public List<int>	errors;
		public List<string>	messages;

		public	ErrorNotificationPacket()
		{
			this.errors = new List<int>();
			this.messages = new List<string>();
		}

		public	ErrorNotificationPacket(int error, string message)
		{
			this.errors = new List<int>() { error };
			this.messages = new List<string>() { message };
		}

		private	ErrorNotificationPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override bool	AggregateInto(Packet pendingPacket)
		{
			ErrorNotificationPacket	packet = pendingPacket as ErrorNotificationPacket;

			if (packet != null)
			{
				for (int j = 0; j < this.errors.Count; j++)
				{
					bool	found = false;

					for (int i = 0; i < packet.errors.Count; i++)
					{
						if (packet.errors[i] == this.errors[j] &&
							packet.messages[i] == this.messages[j])
						{
							found = true;
							break;
						}
					}

					if (found == false)
					{
						packet.errors.Add(this.errors[j]);
						packet.messages.Add(this.messages[j]);
					}
				}

				return true;
			}

			return false;
		}
	}
}