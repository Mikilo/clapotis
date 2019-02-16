using System;

namespace NGTools
{
	public class ArrayModifier : ICollectionModifier
	{
		public int	Size
		{
			get
			{
				return this.array.Length;
			}
		}

		internal Type	subType;
		public Type		SubType
		{
			get
			{
				if (this.subType == null)
					this.subType = Utility.GetArraySubType(this.array.GetType());
				return this.subType;
			}
		}

		public Array	array;

		public	ArrayModifier(Array array)
		{
			this.array = array;
		}

		public object	Get(int index)
		{
			return this.array.GetValue(index);
		}

		public void		Set(int index, object value)
		{
			this.array.SetValue(value, index);
		}
	}
}