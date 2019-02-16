using System.Collections;

namespace NGTools.Network
{
	public enum MyEnum
	{
		Continue,
		NotEnoughSpace,
		Error
	}

	[PacketLinkTo(PacketId.PartialPacket)]
	internal class PartialPacket : Packet
	{
		public int		targetNetworkId;
		public int		position;
		public byte[]	data;

		// Send only at the first frame.
		public int		finalPacketId;
		public int		totalBytes;
		public byte[]	checksum;

		public	PartialPacket()
		{
		}

		protected	PartialPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override IEnumerable	ProgressiveOut(ByteBuffer buffer)
		{
			while (buffer.ProgressiveAppend(this.networkId) == false)
				yield return null;
			while (buffer.ProgressiveAppend(this.targetNetworkId) == false)
				yield return null;
			while (buffer.ProgressiveAppend(this.position) == false)
				yield return null;

			if (this.position == 0)
			{
				while (buffer.ProgressiveAppend(this.finalPacketId) == false)
					yield return null;
				while (buffer.ProgressiveAppend(this.totalBytes) == false)
					yield return null;
				foreach (var item in buffer.ProgressiveAppendBytes(this.checksum))
					yield return null;
			}
			//while (buffer.ProgressiveAppendBytes(this.data) == false)
			//	yield return null;
		}

		public override IEnumerable	ProgressiveIn(ByteBuffer buffer)
		{
			return base.ProgressiveIn(buffer);
		}

		public override void	Out(ByteBuffer buffer)
		{
			buffer.Append(this.networkId);
			buffer.Append(this.targetNetworkId);
			buffer.Append(this.position);
			if (this.position == 0)
			{
				buffer.Append(this.finalPacketId);
				buffer.Append(this.totalBytes);
				buffer.AppendBytes(this.checksum);
			}
			buffer.AppendBytes(this.data);
		}

		public override void	In(ByteBuffer buffer)
		{
			this.networkId = buffer.ReadInt32();
			this.targetNetworkId = buffer.ReadInt32();
			this.position = buffer.ReadInt32();
			if (this.position == 0)
			{
				this.finalPacketId = buffer.ReadInt32();
				this.totalBytes = buffer.ReadInt32();
				this.checksum = buffer.ReadBytes();
			}
			this.data = buffer.ReadBytes();
		}
	}
}