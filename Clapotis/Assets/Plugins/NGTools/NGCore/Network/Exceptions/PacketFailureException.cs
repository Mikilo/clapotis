using System;

namespace NGTools.Network
{
	[Serializable]
	public class PacketFailureException : Exception
	{
		public int		errorCode;
		public string	errorMessage;

		public	PacketFailureException(int errorCode, string errorMessage)
		{
			this.errorCode = errorCode;
			this.errorMessage = errorMessage;
		}
	}
}