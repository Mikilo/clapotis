namespace NGTools
{
	/// <summary>
	/// Provides an optimized array.
	/// Use BringToTop to rearrange the array each time you access an element.
	/// BE CAREFUL when BringToTop is invoked in a nested way!
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class DynamicOrderedArray<T>
	{
		public T[]		array;
		public int[]	fixedIndexes = { };

		public	DynamicOrderedArray(T[] array)
		{
			this.array = array;
		}

		public void	BringToTop(int i)
		{
			if (i > 0)
			{
				for (int j = 0; j < this.fixedIndexes.Length; j++)
				{
					if (this.fixedIndexes[j] == i)
						return;
				}

				T	temp = this.array[i];

				for (int j = i; j > 0; --j)
					this.array[j] = this.array[j - 1];

				this.array[0] = temp;
			}
		}
	}
}