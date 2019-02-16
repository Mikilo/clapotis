using System;
using System.Collections.Generic;

namespace NGTools.Network
{
	public static class ResponsePacketHandler
	{
		public sealed class Handler<T> : IDisposable where T : ResponsePacket
		{
			public T	response;

			private Client	client;

			public	Handler()
			{
			}

			public void	Set(Client client, Packet inputPacket)
			{
				if (inputPacket == null)
					throw new Exception("Packet is not recognized.");

				this.client = client;
				this.response = Activator.CreateInstance(typeof(T), inputPacket.NetworkId) as T;
			}

			public void	Warning(int errorCode, string errorMessage)
			{
				this.response.errorCode = errorCode;
				this.response.errorMessage = errorMessage;
			}

			public void	Throw(int errorCode, string errorMessage)
			{
				throw new PacketFailureException(errorCode, errorMessage);
			}

			public void	HandleException(Exception ex)
			{
				PacketFailureException	myException = ex as PacketFailureException;

				if (myException != null)
				{
					this.response.errorCode = myException.errorCode;
					this.response.errorMessage = myException.errorMessage;
				}
				else
				{
					this.response.errorCode = Errors.ServerException;
					this.response.errorMessage = ex.ToString();
				}
			}

			public void	Dispose()
			{
				this.client.AddPacket(this.response);
			}
		}

		private readonly static Dictionary<Type, object>	cachedHandlers = new Dictionary<Type, object>();

		/// <summary></summary>
		/// <typeparam name="T">A Type deriving from ResponsePacket having a constructor with one parameter for NetworkId.</typeparam>
		/// <param name="client"></param>
		/// <param name="inPacket"></param>
		/// <returns></returns>
		public static Handler<T>	Get<T>(Client client, Packet inPacket) where T : ResponsePacket
		{
			object	rawHandler;

			if (ResponsePacketHandler.cachedHandlers.TryGetValue(typeof(T), out rawHandler) == false)
			{
				rawHandler = new Handler<T>();
				ResponsePacketHandler.cachedHandlers.Add(typeof(T), rawHandler);
			}

			Handler<T>	handler = (Handler<T>)rawHandler;

			handler.Set(client, inPacket);

			return handler;
		}
	}
}