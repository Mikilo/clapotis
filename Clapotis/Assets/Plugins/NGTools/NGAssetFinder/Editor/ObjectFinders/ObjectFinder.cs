using UnityEngine;

namespace NGToolsEditor.NGAssetFinder
{
	internal abstract class ObjectFinder
	{
		protected readonly NGAssetFinderWindow	window;

		protected	ObjectFinder(NGAssetFinderWindow window)
		{
			this.window = window;
		}

		public abstract bool	CanFind(Object asset);
		public abstract void	Find(AssetMatches assetMatches, Object asset, AssetFinder finder, SearchResult result);
	}
}