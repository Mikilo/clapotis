using UnityEngine;

namespace NGToolsEditor.NGAssetFinder
{
	internal class GameObjectFinder : ObjectFinder
	{
		public	GameObjectFinder(NGAssetFinderWindow window) : base(window)
		{
		}

		public override bool	CanFind(Object asset)
		{
			return asset is GameObject;
		}

		public override void	Find(AssetMatches assetMatches, Object asset, AssetFinder finder, SearchResult result)
		{
			GameObject	go = asset as GameObject;

			finder.BrowseGameObject(result, assetMatches, go.transform);
		}
	}
}