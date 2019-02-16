using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NGTools.Network
{
	public abstract class Packet
	{
		private static readonly Dictionary<Type, PacketLinkToAttribute>	cachedPacketId = new Dictionary<Type, PacketLinkToAttribute>();
		private static int												networkIdCounter = 0;

		protected int	networkId;
		public int		NetworkId { get { return this.networkId; } }

		[StripFromNetwork]
		public readonly int		packetId;
		[StripFromNetwork]
		public readonly bool	isBatchable;

		protected	Packet(int networkId)
		{
			PacketLinkToAttribute	attribute;
			Type					type = this.GetType();

			if (Packet.cachedPacketId.TryGetValue(type, out attribute) == false)
			{
				PacketLinkToAttribute[]	attributes = type.GetCustomAttributes(typeof(PacketLinkToAttribute), true) as PacketLinkToAttribute[];

				if (attributes.Length != 1)
					throw new MissingComponentException("Missing attribute PacketLinkToAttribute on " + this.ToString());

				attribute = attributes[0];
				Packet.cachedPacketId.Add(type, attributes[0]);
			}

			this.networkId = networkId;
			this.packetId = attribute.packetId;
			this.isBatchable = attribute.isBatchable;
		}

		protected	Packet() : this(++Packet.networkIdCounter)
		{
		}

		protected	Packet(ByteBuffer buffer) : this(0)
		{
			try
			{
				this.In(buffer);
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogFileException(this.GetType().ToString(), ex);
				throw;
			}
		}

		public virtual IEnumerable	ProgressiveOut(ByteBuffer buffer)
		{
			//if (buffer.Length + this.networkId <= buffer.Capacity)
			//{
			//	this.Out(buffer);
			//	yield break;
			//}

			while (buffer.ProgressiveAppend(this.networkId) == false)
				yield return null;

			IEnumerator	it = NetworkUtility.ProgressiveObjectToBuffer(this, buffer);

			while (it.MoveNext() == true)
				yield return null;
		}

		public virtual IEnumerable	ProgressiveIn(ByteBuffer buffer)
		{
			//if (buffer.Position + this.networkId <= buffer.Length)
			//{
			//	this.In(buffer);
			//	yield break;
			//}

			while (buffer.ProgressiveReadInt32(ref this.networkId) == false)
				yield return null;

			IEnumerator	it = NetworkUtility.ProgressiveBufferToObject(this, buffer);

			while (it.MoveNext() == true)
				yield return null;
		}

		public virtual void	Out(ByteBuffer buffer)
		{
			buffer.Append(this.networkId);
			NetworkUtility.ObjectToBuffer(this, buffer);
		}

		public virtual void	In(ByteBuffer buffer)
		{
			this.networkId = buffer.ReadInt32();
			NetworkUtility.BufferToObject(this, buffer);
		}

		/// <summary>Checks if an existing Packet can be aggregated, therefore merging data into it and then be discarded.</summary>
		/// <param name="pendingPacket">A pending Packet.</param>
		/// <returns>True when aggregated, otherwise false.</returns>
		public virtual bool	AggregateInto(Packet pendingPacket)
		{
			return false;
		}
	}
}