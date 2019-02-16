using System;

namespace NGToolsEditor
{
	[Serializable]
	public abstract class Filter
	{
		public enum Type
		{
			Inclusive,
			Exclusive
		}

		public bool	active;
		public Type	type = Type.Inclusive;
	}
}