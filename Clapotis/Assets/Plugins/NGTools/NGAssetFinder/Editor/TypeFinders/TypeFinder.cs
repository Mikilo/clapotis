using System;

namespace NGToolsEditor.NGAssetFinder
{
	public abstract class TypeFinder
	{
		protected readonly NGAssetFinderWindow	window;

		protected	TypeFinder(NGAssetFinderWindow window)
		{
			this.window = window;
		}

		public abstract bool					CanFind(Type type);
		internal abstract void					Find(Type type, object instance, Match subMatch, IMatchCounter matchCounter);
		internal abstract UnityEngine.Object	Get(Type type, Match match, int index);
		internal abstract void					Set(Type type, UnityEngine.Object reference, Match match, int index);
		internal abstract Type					GetType(Type type);
	}
}