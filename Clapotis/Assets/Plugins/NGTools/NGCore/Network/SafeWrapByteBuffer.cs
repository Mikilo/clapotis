using System;
using System.Collections.Generic;

namespace NGTools
{
	public sealed class SafeWrapByteBuffer : IDisposable
	{
		private static Stack<SafeWrapByteBuffer>	pool = new Stack<SafeWrapByteBuffer>(4);

		private ByteBuffer	buffer;
		private int			originalPosition;
		
		public static SafeWrapByteBuffer	Get(ByteBuffer buffer)
		{
			SafeWrapByteBuffer	restorer;

			if (SafeWrapByteBuffer.pool.Count == 0)
				restorer = new SafeWrapByteBuffer(buffer);
			else
			{
				restorer = SafeWrapByteBuffer.pool.Pop();
				restorer.buffer = buffer;
			}

			restorer.originalPosition = buffer.Length;
			// Insert a dummy value.
			buffer.Append(0);

			return restorer;
		}

		private	SafeWrapByteBuffer(ByteBuffer parent)
		{
			this.buffer = parent;
		}

		public void	Dispose()
		{
			SafeWrapByteBuffer.pool.Push(this);

			int	currentPosition = buffer.Length;
			buffer.Length = this.originalPosition;
			buffer.Append(currentPosition - this.originalPosition - sizeof(Int32));
			buffer.Length = currentPosition;
		}

		/// <summary>Restores the buffer to its original state, cleaning any appended content.</summary>
		public void	Erase()
		{
			buffer.Length = this.originalPosition + sizeof(Int32);
		}
	}
}