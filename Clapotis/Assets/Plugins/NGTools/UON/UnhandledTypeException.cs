using System;

namespace NGTools.UON
{
	[Serializable]
	public class UnhandledTypeException : Exception
	{
		private readonly string	message;
		public override string	Message { get { return this.message; } }

		public	UnhandledTypeException(string message)
		{
			this.message = message;
		}
	}
}