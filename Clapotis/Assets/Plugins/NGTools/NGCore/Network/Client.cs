using System;
using System.Collections.Generic;
#if !NETFX_CORE
using System.Net.Sockets;
#endif
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;

namespace NGTools.Network
{
	public class Client
	{
		public class InPipePacket
		{
			public readonly Packet				packet;
			public Action<ResponsePacket>		onComplete;
			public Action<ProgressionUpdate>	onUpdate;

			private string	readableSendTime;
			public string	ReadableSendTime { get { return this.readableSendTime ?? (this.readableSendTime = new DateTime(this.sendTime).ToString("HH:mm:ss.fff")); } }
			public long		sendTime;
			public long		timeDelay;

			public	InPipePacket(Packet packet, Action<ResponsePacket> onComplete, Action<ProgressionUpdate> onUpdate)
			{
				this.packet = packet;
				this.onComplete = onComplete;
				this.onUpdate = onUpdate;
				this.readableSendTime = null;
				this.sendTime = 0L;
			}
		}

		private class BigPacketReceiver
		{
			public enum State
			{
				NotReady,
				Error,
				InvalidChecksum,
				Ready
			}

			public readonly int					finalPacketId;
			public readonly int					networkId;
			public readonly InPipePacket		inPipePacket;
			public readonly byte[]				data;
			public readonly byte[]				checksum;
			public readonly ProgressionUpdate	updatePacket;

			public int	totalBytesReceived;

			public	BigPacketReceiver(int packetId, int networkId, InPipePacket inPipePacket, int totalBytes, byte[] checksum)
			{
				this.finalPacketId = packetId;
				this.networkId = networkId;
				this.inPipePacket = inPipePacket;
				this.checksum = checksum;
				this.data = new byte[totalBytes];
				this.updatePacket = new ProgressionUpdate();
			}

			public void	Aggregate(PartialPacket partialPacket)
			{
				Buffer.BlockCopy(partialPacket.data, 0, this.data, partialPacket.position, partialPacket.data.Length);
				this.totalBytesReceived += partialPacket.data.Length;

				if (this.totalBytesReceived > this.data.Length)
					throw new Exception("Big Packet \"" + this.networkId + "\" received more data than expected.");
			}

			public State	Finalize(ByteBuffer buffer)
			{
				if (this.totalBytesReceived == this.data.Length)
				{
					try
					{
						byte[]	checksum = Client.md5.ComputeHash(this.data);

						for (int i = 0; i < checksum.Length; i++)
						{
							if (checksum[i] != this.checksum[i])
								return State.InvalidChecksum;
						}
					}
					catch (Exception ex)
					{
						InternalNGDebug.LogException(ex);
						return State.Error;
					}

					buffer.Clear();

					if (buffer.Capacity < this.data.Length)
					{
						if (this.data.Length > 1024 * 1024 * 10) // >10 MiB
							buffer.resizeMode = ByteBuffer.ResizeMode.Strict;
						buffer.Resize(this.data.Length);
					}

					Buffer.BlockCopy(this.data, 0, buffer.GetRawBuffer(), 0, this.data.Length);
					buffer.Length = this.data.Length;

					return State.Ready;
				}

				return State.NotReady;
			}
		}

		private class BigPacketSender
		{
			private class Frame
			{
				public int	position;
				public bool	available = true;

				// TODO Implement timeout/failsafe/fallback check
				//public bool		failed = false;
				//public float	sendTime;
			}

			public const int	DefaultMaxFrames = 10;

			public readonly int			packetId;
			public readonly int			networkId;
			public readonly ByteBuffer	buffer;

			private int			farthestPosition = 0;
			private List<Frame>	frames;

			public	BigPacketSender(int packetId, int networkId, ByteBuffer buffer)
			{
				this.packetId = packetId;
				this.networkId = networkId;
				this.buffer = buffer;

				this.frames = new List<Frame>(BigPacketSender.DefaultMaxFrames);
				for (int i = 0; i < BigPacketSender.DefaultMaxFrames; i++)
					this.frames.Add(new Frame());
			}

			public byte[]	GetData(int position, int length)
			{
				byte[]	data = new byte[length];
				byte[]	rawBuffer = this.buffer.GetRawBuffer();

				Buffer.BlockCopy(rawBuffer, position, data, 0, length);
				return data;
			}

			public bool	PeekAvailableFrame(out int position, out int length)
			{
				if (this.farthestPosition < this.buffer.Length)
				{
					for (int i = 0; i < this.frames.Count; i++)
					{
						if (this.frames[i].available == true)
						{
							this.frames[i].available = false;
							//this.frames[i].failed = false;
							this.frames[i].position = this.farthestPosition;
							position = this.farthestPosition;

							if (this.farthestPosition + Client.MaxSmallPacketSize <= this.buffer.Length)
								length = Client.MaxSmallPacketSize;
							else
								length = this.buffer.Length - this.farthestPosition;

							this.farthestPosition += length;

							return true;
						}
					}
				}

				position = 0;
				length = 0;

				return false;
			}

			public bool	AcknowledgeFrame(int position)
			{
				for (int i = 0; i < this.frames.Count; i++)
				{
					if (this.frames[i].position == position &&
						this.frames[i].available == false)
					{
						this.frames[i].available = true;
						return true;
					}
				}

				return false;
			}

			public bool	IsCompleted()
			{
				return this.farthestPosition == this.buffer.Length;
			}
		}

		public class Batch
		{
			public string					name;
			public readonly InPipePacket[]	batchedPackets;

			public	Batch(string name, InPipePacket[] batch)
			{
				this.name = name;
				this.batchedPackets = batch;
			}
		}

		public enum BatchMode
		{
			Off,
			On
		}

		public const int	SendBufferCapacity = 1024;
		public const int	ReadBufferSize = 16384;
		public const int	TempBufferSize = 4048;
		public const int	MaxSmallPacketSize = 3000;
		public const long	PacketTimeoutDuration = 10000000L * 10L; // 10 seconds in ticks (1 tick = 100 ns)
		public const long	DelayBusyServerDuration = 10000000L * 5L; // 5 seconds in ticks (1 tick = 100 ns)

		private readonly static Dictionary<int, Type>	packetTypes = new Dictionary<int, Type>();
		private readonly static MD5						md5 = MD5.Create();

		public string	debugPrefix;

		public Int64	BytesSent { get; private set; }
		public Int64	BytesReceived { get; private set; }
		public int		PendingPacketsCount { get { return this.pendingPackets.Count; } }

#if NETFX_CORE
		public readonly Windows.Networking.Sockets.StreamSocket	tcpClient;
#else
		public readonly TcpClient	tcpClient;
#endif

		public BatchMode					batchMode;
		public readonly List<InPipePacket>	batchedPackets;
		private string[]					batchNames;
		public string[]						BatchNames { get { return this.batchNames; } }
		private readonly List<Batch>		batchesHistoric;

		public readonly bool				saveSentPackets;
		public readonly List<string>		receivedPacketsHistoric;
		public readonly List<InPipePacket>	sentPacketsHistoric;
		public readonly List<InPipePacket>	sentPacketsPending;
		private List<InPipePacket>	pendingPackets;
		private List<InPipePacket>	workingPendingPackets;
		private readonly List<Packet>	receivedPackets;
		private readonly Stack<Packet>	receivedPotentialMemoryDemandingPackets;
		private readonly List<Packet>	receivedFailurePackets;

		private readonly ByteBuffer		packetBuffer;
		private readonly ByteBuffer		receiveBuffer;
		private readonly ByteBuffer		fullPacketBuffer;
#if NETFX_CORE
		private readonly Windows.Storage.Streams.IInputStream	reader;
		private readonly Windows.Storage.Streams.IOutputStream	writer;
#else
		private readonly NetworkStream	reader;
		private readonly NetworkStream	writer;
#endif
		private readonly ByteBuffer		sendBuffer;
		private ByteBuffer				tinySendBuffer;
		private readonly List<BigPacketSender>		sendingBigPackets = new List<BigPacketSender>();
		private readonly List<BigPacketReceiver>	receivingBigPackets = new List<BigPacketReceiver>();
#if NETFX_CORE
		private readonly Windows.Storage.Streams.IBuffer	tempBuffer;
#else
		private readonly byte[]			tempBuffer;
#endif

		private readonly object[]	packetArgument;
		
		static	Client()
		{
			foreach (Type t in Utility.EachAllSubClassesOf(typeof(Packet)))
			{
				object[]	attributes = t.GetCustomAttributes(typeof(PacketLinkToAttribute), false);

				if (attributes.Length == 0)
					continue;

				if (Client.packetTypes.ContainsKey((attributes[0] as PacketLinkToAttribute).packetId) == true)
					InternalNGDebug.LogError("Packet \"" + t.FullName + "\" shares the same ID as \"" + Client.packetTypes[(attributes[0] as PacketLinkToAttribute).packetId] + "\".");
				else
					Client.packetTypes.Add((attributes[0] as PacketLinkToAttribute).packetId, t);
			}
		}

		public	Client(
#if NETFX_CORE
			Windows.Networking.Sockets.StreamSocket tcpClient
#else
			TcpClient tcpClient
#endif
			, bool saveSentPackets = true)
		{
			this.tcpClient = tcpClient;
#if NETFX_CORE
			this.reader = this.tcpClient.InputStream;
			this.writer = this.tcpClient.OutputStream;
#else
			this.reader = this.tcpClient.GetStream();
			this.writer = this.reader;
#endif

			this.saveSentPackets = saveSentPackets;
			this.receivedPacketsHistoric = new List<string>(512);
			if (this.saveSentPackets == true)
				this.sentPacketsHistoric = new List<InPipePacket>(512);
			this.sentPacketsPending = new List<InPipePacket>(128);
			this.pendingPackets = new List<InPipePacket>(4);
			this.workingPendingPackets = new List<InPipePacket>(4);
			this.receivedPackets = new List<Packet>(4);
			this.receivedPotentialMemoryDemandingPackets = new Stack<Packet>(2);
			this.receivedFailurePackets = new List<Packet>(4);

			this.batchMode = BatchMode.Off;
			this.batchNames = new string[0];
			this.batchedPackets = new List<InPipePacket>(64);
			this.batchesHistoric = new List<Batch>(4);

			this.packetBuffer = new ByteBuffer(Client.ReadBufferSize);
			this.receiveBuffer = new ByteBuffer(Client.ReadBufferSize);
			this.fullPacketBuffer = new ByteBuffer(Client.ReadBufferSize);
			this.sendBuffer = new ByteBuffer(Client.SendBufferCapacity);
			this.tinySendBuffer = new ByteBuffer(Client.SendBufferCapacity);
#if NETFX_CORE
			this.tempBuffer = new Windows.Storage.Streams.DataReader(this.reader).DetachBuffer();
#else
			this.tempBuffer = new byte[Client.TempBufferSize];
#endif

			this.packetArgument = new object[1] { this.packetBuffer };

#if NETFX_CORE
			System.Threading.Tasks.Task.Run(new Action(this.StartRead));
#else
			if (this.reader.CanRead == true)
				this.reader.BeginRead(this.tempBuffer, 0, this.tempBuffer.Length, new AsyncCallback(this.ReadCallBack), this);
			else
				InternalNGDebug.LogError("Client has a non readable NetworkStream.");

			//new Thread(this.Aaa)
			//{
			//	Name = "NG Network Write"
			//}.Start();
#endif
		}

		private void	Aaa()
		{
			this.reader.BeginWrite(this.tempBuffer, 0, this.tempBuffer.Length, new AsyncCallback(this.WriteCallBack), this);
		}

		public void	Close()
		{
#if NETFX_CORE
			this.reader.Dispose();
			this.writer.Dispose();
			this.tcpClient.Dispose();
#else
			this.reader.Close();
			this.tcpClient.Close();
#endif
		}

#if NETFX_CORE
		private async void	StartRead()
		{
			while (true)
			{
				var	buffer = await this.reader.ReadAsync(this.tempBuffer, this.tempBuffer.Capacity, Windows.Storage.Streams.InputStreamOptions.None);

				lock (this.receiveBuffer)
				{
					var	reader = Windows.Storage.Streams.DataReader.FromBuffer(this.tempBuffer);
					var	tBuf = new byte[buffer.Length];
					reader.ReadBytes(tBuf);
					this.receiveBuffer.Append(tBuf, 0, tBuf.Length);
				}

				lock (this.fullPacketBuffer)
				{
					System.Threading.Tasks.Task.Run(new Action(this.StartRead));

					this.ExecuteBuffer();
				}
			}
		}
#else
		private void	ReadCallBack(IAsyncResult ar)
		{
			int	bytesCount = this.reader.EndRead(ar);

			lock (this.receiveBuffer)
			{
				this.receiveBuffer.Append(this.tempBuffer, 0, bytesCount);
			}

			lock (this.fullPacketBuffer)
			{
				this.reader.BeginRead(this.tempBuffer, 0, this.tempBuffer.Length, new AsyncCallback(this.ReadCallBack), this);

				if (this.reader.DataAvailable == false)
					this.ExecuteBuffer();
			}
		}
#endif

		private void	ExecuteBuffer()
		{
			lock (this.receiveBuffer)
			{
				this.fullPacketBuffer.Append(this.receiveBuffer);

				this.BytesReceived += this.receiveBuffer.Length;

				this.receiveBuffer.Clear();
			}

			// 8 corresponds to the minimum length for a command.
			for (uint i = (uint)this.fullPacketBuffer.Position; this.fullPacketBuffer.Length >= i && (uint)this.fullPacketBuffer.Length - i >= 8U;)
			{
				int		packetId = this.fullPacketBuffer.ReadInt32();
				uint	length = this.fullPacketBuffer.ReadUInt32();

				i = (uint)this.fullPacketBuffer.Position;

				if (length <= this.fullPacketBuffer.Length - this.fullPacketBuffer.Position)
				{
					Type	packetType;

					if (Client.packetTypes.TryGetValue(packetId, out packetType) == true)
					{
						try
						{
							this.fullPacketBuffer.CopyBuffer(this.packetBuffer, (int)i, (int)length);

							lock (this.receivedPackets)
							{
								Packet	packet = Activator.CreateInstance(packetType, BindingFlags.NonPublic | BindingFlags.Instance, null, this.packetArgument, null) as Packet;
								// TODO Refactor packet constructor?
								//packet = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(packetType) as Packet;

								InternalNGDebug.LogFile(this.debugPrefix + "R " + PacketId.GetPacketName(packetId) + " #" + packet.NetworkId + " (" + packetId + ") " + length + " B @ " + this.fullPacketBuffer.Position + '/' + this.fullPacketBuffer.Length);

								if (this.packetBuffer.Position != this.packetBuffer.Length)
									InternalNGDebug.LogFile(this.debugPrefix + "Packet " + PacketId.GetPacketName(packetId) + " (" + packetId + ") was not fully consummed, " + (this.packetBuffer.Length - this.packetBuffer.Position) + " bytes remaining.");

								this.receivedPackets.Add(packet);
								this.receivedPacketsHistoric.Add(DateTime.Now.ToString("HH:mm:ss.fff") + " #" + packet.NetworkId + ' ' + packet);

								ResponsePacket	responsePacket = packet as ResponsePacket;

								if (responsePacket != null && responsePacket.errorCode != 0)
									this.receivedFailurePackets.Add(responsePacket);
							}
						}
						catch (Exception ex)
						{
							InternalNGDebug.LogFileException(this.debugPrefix + "Packet parsing failed: Type: " + packetType, ex);
							this.fullPacketBuffer.Clear();
							break;
						}
					}
					else
					{
						InternalNGDebug.LogFile(this.debugPrefix + "Unknown command " + PacketId.GetPacketName(packetId) + " (" + packetId + ") of " + length + " chars.");
					}

					i += length;
					this.fullPacketBuffer.Position = (int)i;

					packetId = -1;

					if (this.fullPacketBuffer.Length == this.fullPacketBuffer.Position)
						this.fullPacketBuffer.Clear();
				}
				else
				{
					InternalNGDebug.LogFile(this.debugPrefix + "R... " + PacketId.GetPacketName(packetId) + " (" + packetId + ") " + length + " B @ " + this.fullPacketBuffer.Position + '/' + this.fullPacketBuffer.Length);
					this.fullPacketBuffer.Position -= 8;
					break;
				}
			}
		}

		public void	Write(PacketExecuter executer)
		{
			// TODO Perhaps move the following code to Update()?
			if (this.sentPacketsPending.Count > 0)
			{
				long	now = DateTime.Now.Ticks;

				for (int i = 0; i < this.sentPacketsPending.Count; i++)
				{
					float	diff = now - (this.sentPacketsPending[i].sendTime + this.sentPacketsPending[i].timeDelay);

					if (diff > Client.PacketTimeoutDuration)
					{
						try
						{
							if (this.sentPacketsPending[i].onComplete != null)
								this.sentPacketsPending[i].onComplete(new AckPacket(this.sentPacketsPending[i].packet.NetworkId, Errors.Timeout, "Timeout for " + PacketId.GetPacketName(this.sentPacketsPending[i].packet.packetId) + " #" + this.sentPacketsPending[i].packet.NetworkId + " (" + this.sentPacketsPending[i].packet.packetId + ")."));
						}
						finally
						{
							for (int j = 0; j < this.sendingBigPackets.Count; j++)
							{
								if (this.sendingBigPackets[j].networkId == this.sentPacketsPending[i].packet.NetworkId)
								{
									this.sendingBigPackets.RemoveAt(j);
									break;
								}
							}

							this.sentPacketsPending.RemoveAt(i--);
						}
					}
				}
			}

			if (this.pendingPackets.Count > 0)
			{
				List<InPipePacket>	sendingPackets = this.pendingPackets;
				this.pendingPackets = this.workingPendingPackets;
				this.workingPendingPackets = sendingPackets;

				InternalNGDebug.LogFile(this.debugPrefix + "W " + this.workingPendingPackets.Count + " packet(s).");

				for (int i = 0; i < this.workingPendingPackets.Count; i++)
				{
					try
					{
						this.tinySendBuffer.Clear();
						this.workingPendingPackets[i].packet.Out(this.tinySendBuffer);

						if (this.tinySendBuffer.Length > Client.MaxSmallPacketSize)
						{
							// Split Big Packet
							InternalNGDebug.LogFile(this.debugPrefix + "SBP " + PacketId.GetPacketName(this.workingPendingPackets[i].packet.packetId) + " #" + this.workingPendingPackets[i].packet.NetworkId + " (" + this.workingPendingPackets[i].packet.packetId + ") " + (uint)this.tinySendBuffer.Length + " B.");
							this.sendingBigPackets.Add(new BigPacketSender(this.workingPendingPackets[i].packet.packetId, this.workingPendingPackets[i].packet.NetworkId, this.tinySendBuffer));
							this.tinySendBuffer = Utility.GetBBuffer();
						}
						else
						{
							InternalNGDebug.LogFile(this.debugPrefix + "W " + PacketId.GetPacketName(this.workingPendingPackets[i].packet.packetId) + " #" + this.workingPendingPackets[i].packet.NetworkId + " (" + this.workingPendingPackets[i].packet.packetId + ") " + (uint)this.tinySendBuffer.Length + " B.");
							this.sendBuffer.Append(this.workingPendingPackets[i].packet.packetId);
							this.sendBuffer.Append((uint)this.tinySendBuffer.Length);
							this.sendBuffer.Append(this.tinySendBuffer);

							if (executer.WorkingOnDemandingPacket == true && this.workingPendingPackets[i].packet.packetId == executer.TargetPacketId)
								executer.ClearDemandingPacket();
						}
					}
					catch (Exception ex)
					{
						InternalNGDebug.LogException(this.debugPrefix + "Packet " + this.workingPendingPackets[i].packet + " has thrown an exception.", ex);
						this.AddPacket(new ErrorNotificationPacket(0, "Packet " + this.workingPendingPackets[i].packet + " has thrown an exception:" + Environment.NewLine + ex.ToString()));
					}
				}
			}

			for (int i = 0; i < this.sendingBigPackets.Count; i++)
			{
				int	position;
				int	length;

				while (this.sendingBigPackets[i].PeekAvailableFrame(out position, out length) == true)
				{
					BigPacketSender	sender = this.sendingBigPackets[i];
					PartialPacket	p = new PartialPacket()
					{
						targetNetworkId = sender.networkId,
						position = position,
						data = sender.GetData(position, length)
					};

					if (position == 0)
					{
						p.finalPacketId = sender.packetId;
						p.totalBytes = sender.buffer.Length;
						p.checksum = Client.md5.ComputeHash(sender.buffer.GetRawBuffer(), 0, sender.buffer.Length);
					}

					this.tinySendBuffer.Clear();
					p.Out(this.tinySendBuffer);

					// Write Partial Packet
					InternalNGDebug.LogFile(this.debugPrefix + "WPP " + PacketId.GetPacketName(p.packetId) + " #" + p.NetworkId + " (" + p.packetId + ") " + (uint)this.tinySendBuffer.Length + " B (" + position + "/" + sender.buffer.Length + ").");
					this.sendBuffer.Append(p.packetId);
					this.sendBuffer.Append((uint)this.tinySendBuffer.Length);
					this.sendBuffer.Append(this.tinySendBuffer);

					this.workingPendingPackets.Add(new InPipePacket(p, this.ClosureCallback(position, sender, executer), null));
				}
			}

			if (this.sendBuffer.Length > 0)
			{
				try
				{
					byte[]	buffer = this.sendBuffer.Flush();
#if NETFX_CORE
					var	writer = new Windows.Storage.Streams.DataWriter(this.writer);
					writer.WriteBytes(buffer);
					writer.StoreAsync().AsTask().Start();
#else
					this.writer.BeginWrite(buffer, 0, buffer.Length, new AsyncCallback(this.WriteCallBack), this);
#endif
					this.BytesSent += buffer.Length;

					// Only add to historic after they are all buffered and sent. In case of raising exception.
					long	now = DateTime.Now.Ticks;

					for (int i = 0; i < this.workingPendingPackets.Count; i++)
					{
						InPipePacket	inPipePacket = this.workingPendingPackets[i];

						inPipePacket.sendTime = now;

						if ((inPipePacket.packet is ResponsePacket) == false)
							this.sentPacketsPending.Add(inPipePacket);

						if (this.saveSentPackets == true && (inPipePacket.packet is AckPacket) == false)
							this.sentPacketsHistoric.Add(inPipePacket);
					}
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException("Async Write failed.", ex);
				}
				finally
				{
					this.workingPendingPackets.Clear();
				}
			}
		}

		private Action<ResponsePacket>	ClosureCallback(int position, BigPacketSender sender, PacketExecuter executer)
		{
			return r =>
			{
				sender.AcknowledgeFrame(position);
				if (sender.IsCompleted() == true)
				{
					if (sender.buffer.Capacity < 1024 * 1024 * 10) // Keep if under 10 MiB, otherwise discard it to release memory.
						Utility.RestoreBBuffer(sender.buffer);

					if (executer.WorkingOnDemandingPacket == true && sender.packetId == executer.TargetPacketId)
						executer.ClearDemandingPacket();

					this.sendingBigPackets.Remove(sender);

				}
			};
		}

#if !NETFX_CORE
		private void	WriteCallBack(IAsyncResult ar)
		{
			this.reader.EndWrite(ar);
		}
#endif
		/// <summary>
		/// Executes commands received during the last frames. Is called in Update, thus the main-thread context is guaranteed.
		/// </summary>
		/// <param name="executer"></param>
		public void	ExecuteReceivedCommands(PacketExecuter executer)
		{
			lock (this.receivedPackets)
			{
				if (this.receivedPotentialMemoryDemandingPackets.Count > 0)
				{
					if (executer.WorkingOnDemandingPacket == false)
						this.receivedPackets.Insert(0, this.receivedPotentialMemoryDemandingPackets.Pop());
					//TODO Send BusyPacket every X seconds in parallel thread.
				}

				if (this.receivedPackets.Count == 0)
					return;

				for (int j = 0; j < this.receivedPackets.Count; j++)
				{
					ResponsePacket	responsePacket = this.receivedPackets[j] as ResponsePacket;
					bool			packetHandled = false;
					Exception		exception = null;

					try
					{
						InternalNGDebug.LogFile(this.debugPrefix + "X " + PacketId.GetPacketName(this.receivedPackets[j].packetId) + " #" + this.receivedPackets[j].NetworkId + " (" + this.receivedPackets[j].packetId + ").");

						if (this.receivedPackets[j].packetId == PacketId.BusyAck)
						{
							BusyAckPacket	busyAckPacket = this.receivedPackets[j] as BusyAckPacket;
							
							for (int i = 0; i < this.sentPacketsPending.Count; i++)
							{
								if (this.sentPacketsPending[i].packet.NetworkId == busyAckPacket.NetworkId)
								{
									InternalNGDebug.Log("Delay on " + busyAckPacket.NetworkId);
									this.sentPacketsPending[i].timeDelay += Client.DelayBusyServerDuration * 20L;
									break;
								}
							}
						}
						else if (this.receivedPackets[j].packetId == PacketId.PartialPacket)
						{
							PartialPacket		partialPacket = this.receivedPackets[j] as PartialPacket;
							BigPacketReceiver	receiver = null;
							int					k = 0;

							for (; k < this.receivingBigPackets.Count; k++)
							{
								if (this.receivingBigPackets[k].networkId == partialPacket.targetNetworkId)
								{
									receiver = this.receivingBigPackets[k];
									break;
								}
							}

							if (receiver == null)
							{
								for (int i = 0; i < this.sentPacketsPending.Count; i++)
								{
									if (this.sentPacketsPending[i].packet.NetworkId == partialPacket.targetNetworkId)
									{
										receiver = new BigPacketReceiver(partialPacket.finalPacketId, partialPacket.targetNetworkId, this.sentPacketsPending[i], partialPacket.totalBytes, partialPacket.checksum);
										this.receivingBigPackets.Add(receiver);
										break;
									}
								}
							}

							if (receiver != null)
							{
								receiver.inPipePacket.timeDelay = DateTime.Now.Ticks - receiver.inPipePacket.sendTime + receiver.data.Length; // Add 1 sec per MB.
								receiver.Aggregate(partialPacket);

								switch (receiver.Finalize(this.packetBuffer))
								{
									case BigPacketReceiver.State.Ready:
										int		packetId = receiver.finalPacketId;
										Type	packetType;
										
										if (receiver.inPipePacket.onUpdate != null)
										{
											receiver.updatePacket.bytesReceived = receiver.totalBytesReceived;
											receiver.updatePacket.totalBytes = receiver.data.Length;
											receiver.inPipePacket.onUpdate(receiver.updatePacket);
										}

										if (Client.packetTypes.TryGetValue(packetId, out packetType) == true)
											this.receivedPackets.Add(Activator.CreateInstance(packetType, BindingFlags.NonPublic | BindingFlags.Instance, null, this.packetArgument, null) as ResponsePacket);
										break;

									case BigPacketReceiver.State.Error:
										this.receivingBigPackets.Remove(receiver);
										break;

									case BigPacketReceiver.State.InvalidChecksum:
										this.receivingBigPackets.Remove(receiver);
										break;

									case BigPacketReceiver.State.NotReady:
										if (receiver.inPipePacket.onUpdate != null)
										{
											receiver.updatePacket.bytesReceived = receiver.totalBytesReceived;
											receiver.updatePacket.totalBytes = receiver.data.Length;
											receiver.inPipePacket.onUpdate(receiver.updatePacket);
										}
										break;
								}

								packetHandled = true;
							}
						}
						else if (responsePacket != null)
						{
							for (int i = 0; i < this.sentPacketsPending.Count; i++)
							{
								if (this.sentPacketsPending[i].packet.NetworkId == this.receivedPackets[j].NetworkId)
								{
									try
									{
										if (this.sentPacketsPending[i].onComplete != null)
										{
											this.sentPacketsPending[i].onComplete(responsePacket);
											packetHandled = true;
										}
									}
									finally
									{
										this.sentPacketsPending.RemoveAt(i);
									}

									break;
								}
							}
						}

						if (this.receivedPackets[j].packetId == PacketId.Ack || this.receivedPackets[j].packetId == PacketId.BusyAck)
							packetHandled = true;
						else
						{
							switch (executer.ExecutePacket(this, this.receivedPackets[j]))
							{
								case PacketExecuter.ExecutionStatus.NewlyDemandingPacket:
								case PacketExecuter.ExecutionStatus.Handled:
									packetHandled = true;
									break;

								case PacketExecuter.ExecutionStatus.Unhandled:
									break;

								case PacketExecuter.ExecutionStatus.AlreadyDemandingPacket:
									this.receivedPotentialMemoryDemandingPackets.Push(this.receivedPackets[j]);
									this.AddPacket(new BusyAckPacket(this.receivedPackets[j].NetworkId));
									break;
							}
						}
					}
					catch (Exception ex)
					{
						InternalNGDebug.LogException(ex);
						exception = ex;
					}

					if (responsePacket == null)
					{
						int	k = 0;

						// Check if someone answered the packet.
						for (; k < this.pendingPackets.Count; k++)
						{
							if (this.pendingPackets[k].packet.NetworkId == this.receivedPackets[j].NetworkId)
								break;
						}

						// Otherwise, return something.
						if (k >= this.pendingPackets.Count)
						{
							// Return ACK.
							if (exception != null)
								this.AddPacket(new AckPacket(this.receivedPackets[j].NetworkId, Errors.ServerException, exception.ToString()));
							else if (packetHandled == true)
								this.AddPacket(new AckPacket(this.receivedPackets[j].NetworkId, Errors.None, null));
							else // Or unhandled.
								this.AddPacket(new AckPacket(this.receivedPackets[j].NetworkId, Errors.UnhandledPacket, this.receivedPackets[j].GetType().Name));
						}
					}
					else if (packetHandled == false)
						InternalNGDebug.Log(responsePacket.errorCode, "Unhandled \"" + responsePacket.GetType().Name + "\": " + responsePacket.errorMessage);
				}

				this.receivedPackets.Clear();
			}
		}

		public void	AddPacket(Packet packet, Action<ResponsePacket> onComplete = null, Action<ProgressionUpdate> onUpdate = null)
		{
			List<InPipePacket>	list;

			if (this.batchMode == BatchMode.On && packet.isBatchable == true)
				list = this.batchedPackets;
			else
				list = this.pendingPackets;

			for (int i = 0; i < list.Count; i++)
			{
				if (packet.AggregateInto(list[i].packet) == true)
				{
					if (onComplete != null)
					{
						InPipePacket	ep = list[i];
						ep.onComplete -= onComplete;
						ep.onComplete += onComplete;
						list[i] = ep;
					}

					if (onUpdate != null)
					{
						InPipePacket ep = list[i];
						ep.onUpdate -= onUpdate;
						ep.onUpdate += onUpdate;
						list[i] = ep;
					}

					return;
				}
			}

			list.Add(new InPipePacket(packet, onComplete, onUpdate));
		}

		public Packet	GetPacketFromNetworkIdInPendingPackets(int networkId)
		{
			for (int k = 0; k < this.pendingPackets.Count; k++)
			{
				if (this.pendingPackets[k].packet.NetworkId == networkId)
					return this.pendingPackets[k].packet;
			}

			return null;
		}

		public void	SaveBatch(string name)
		{
			if (this.batchedPackets.Count > 0)
			{
				this.batchesHistoric.Add(new Batch(name, this.batchedPackets.ToArray()));

				this.batchNames = new string[this.batchesHistoric.Count];

				for (int i = 0; i < this.batchesHistoric.Count; i++)
					this.batchNames[i] = this.batchesHistoric[i].name + " (" + this.batchesHistoric[i].batchedPackets.Length + ')';
			}
		}

		public void	ExecuteBatch()
		{
			if (this.batchedPackets.Count > 0)
			{
				for (int i = 0; i < this.batchedPackets.Count; i++)
					this.pendingPackets.Add(this.batchedPackets[i]);

				this.batchedPackets.Clear();
			}
		}

		public void	LoadBatch(int i)
		{
			if (0 <= i && i < this.batchesHistoric.Count)
			{
				this.batchedPackets.Clear();

				for (int j = 0; j < this.batchesHistoric[i].batchedPackets.Length; j++)
					this.batchedPackets.Add(this.batchesHistoric[i].batchedPackets[j]);
			}
		}

		public override string	ToString()
		{
			return "FPL=" + this.fullPacketBuffer.Position + '/' + this.fullPacketBuffer.Length + " BRecv=" + this.BytesReceived + " PP=" + this.pendingPackets.Count;
		}
	}
}