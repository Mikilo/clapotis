namespace NGTools.Network
{
	public abstract class ResponsePacket : Packet
	{
		[StripFromNetwork]
		public int		errorCode;
		[StripFromNetwork]
		public string	errorMessage;

		public	ResponsePacket(int networkId) : base(networkId)
		{
		}

		public	ResponsePacket(int networkId, int errorCode, string errorMessage) : base(networkId)
		{
			this.errorCode = errorCode;
			this.errorMessage = errorMessage;
		}

		protected	ResponsePacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override void	Out(ByteBuffer buffer)
		{
			if (this.OutResponseStatus(buffer) == true)
				NetworkUtility.ObjectToBuffer(this, buffer);
		}

		public override void	In(ByteBuffer buffer)
		{
			if (this.InResponseStatus(buffer) == true)
				NetworkUtility.BufferToObject(this, buffer);
		}

		/// <summary>
		/// <para>Verifies if the packet is safe to use.</para>
		/// <para>Prints a log in case of failure.</para>
		/// </summary>
		/// <returns></returns>
		public bool	CheckPacketStatus()
		{
			if (this.errorCode == Errors.None)
				return true;
			else if (this.errorCode > Errors.None)
			{
				InternalNGDebug.LogWarning(this.errorCode, this.errorMessage);
				return true;
			}

			InternalNGDebug.LogError(this.errorCode, this.errorMessage);
			return false;
		}

		/// <summary>Writes response (network ID & error) data to the buffer. Returns whether the remaining packet should be written.</summary>
		/// <param name="buffer"></param>
		/// <returns></returns>
		protected bool	OutResponseStatus(ByteBuffer buffer)
		{
			buffer.Append(this.networkId);
			buffer.Append(this.errorCode);

			if (this.errorCode != 0)
				buffer.AppendUnicodeString(this.errorMessage);

			return this.errorCode >= Errors.None;
		}

		/// <summary>Reads response (network ID & error) data from the buffer. Returns whether the remaining packet should be read.</summary>
		/// <param name="buffer"></param>
		/// <returns></returns>
		protected bool	InResponseStatus(ByteBuffer buffer)
		{
			this.networkId = buffer.ReadInt32();
			this.errorCode = buffer.ReadInt32();

			if (this.errorCode != 0)
				this.errorMessage = buffer.ReadUnicodeString();

			return this.errorCode >= Errors.None;
		}
	}
}