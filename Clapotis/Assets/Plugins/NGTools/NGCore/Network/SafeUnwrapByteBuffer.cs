using System;
using System.Collections.Generic;

namespace NGTools
{
	public sealed class SafeUnwrapByteBuffer : IDisposable
	{
		internal struct UnwrapDump
		{
			public string	error;
			public int		start;
			public int		position;
			public int		end;
			public byte[]	buffer;
		}

		internal static List<UnwrapDump>	dumps = new List<UnwrapDump>();

		private static Stack<SafeUnwrapByteBuffer>	pool = new Stack<SafeUnwrapByteBuffer>(4);

		private Func<string>	getMessage;
		private ByteBuffer		buffer;
		private int				chunkFieldLength;
		private int				fallbackEndPosition;

		public static SafeUnwrapByteBuffer	Get(ByteBuffer buffer, Func<string> getMessage)
		{
			SafeUnwrapByteBuffer	restorer;

			if (SafeUnwrapByteBuffer.pool.Count == 0)
				restorer = new SafeUnwrapByteBuffer(buffer, getMessage);
			else
			{
				restorer = SafeUnwrapByteBuffer.pool.Pop();
				restorer.Set(buffer, getMessage);
			}

			return restorer;
		}

		private	SafeUnwrapByteBuffer(ByteBuffer buffer, Func<string> getMessage)
		{
			this.Set(buffer, getMessage);
		}

		/// <summary>Checks if there is any content.</summary>
		public bool	IsValid()
		{
			return this.buffer.Position != this.fallbackEndPosition;
		}

		public void	ForceFallback()
		{
			this.buffer.Position = fallbackEndPosition;
		}

		private void	Set(ByteBuffer buffer, Func<string> getMessage)
		{
			this.getMessage = getMessage;
			this.buffer = buffer;
			this.chunkFieldLength = buffer.ReadInt32();
			this.fallbackEndPosition = buffer.Position + chunkFieldLength;
		}

		public void	Dispose()
		{
			SafeUnwrapByteBuffer.pool.Push(this);

			if (this.buffer.Position != fallbackEndPosition)
			{
				if (Conf.DebugMode >= Conf.DebugState.Active)
				{
					SafeUnwrapByteBuffer.dumps.Add(new UnwrapDump()
					{
						error = this.getMessage(),
						start = this.fallbackEndPosition - this.chunkFieldLength - sizeof(Int32),
						position = this.buffer.Position,
						end = this.fallbackEndPosition,
						buffer = buffer.GetRawBuffer()
					});
				}

				InternalNGDebug.LogError(this.getMessage() + ": Start " + (this.fallbackEndPosition - this.chunkFieldLength) + " > Pos " + this.buffer.Position + " > End " + this.fallbackEndPosition + ".");
				this.buffer.Position = fallbackEndPosition;
			}
		}
	}
}