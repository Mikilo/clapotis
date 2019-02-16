using System;

namespace NGToolsEditor.NGAssetFinder
{
	[Flags]
	public enum SearchOptions
	{
		InCurrentScene = 1 << 0,
		InProject = 1 << 1,
		SerializeField = 1 << 3,
		NonPublic = 1 << 4,
		Property = 1 << 5,
		ByInstance = 1 << 6,
		ByComponentType = 1 << 7,
	}
}