using System;
using System.Collections;

namespace NGTools
{
	public class ListModifier : ICollectionModifier
	{
		public int	Size
		{
			get
			{
				return this.list.Count;
			}
		}

		internal Type	subType;
		public Type		SubType
		{
			get
			{
				if (this.subType == null)
					this.subType = Utility.GetArraySubType(this.list.GetType());
				return this.subType;
			}
		}

		public IList	list;

		public	ListModifier(IList list)
		{
			this.list = list;
		}

		public object	Get(int index)
		{
			return this.list[index];
		}

		public void		Set(int index, object value)
		{
			this.list[index] = value;
		}
	}
}