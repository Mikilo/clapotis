using System;

namespace NGToolsEditor.NGAssetFinder
{
	[Flags]
	public enum SearchAssets
	{
		Asset = 1 << 0,
		Prefab = 1 << 1,
		Scene = 1 << 2,
	}
}