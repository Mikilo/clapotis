using UnityEngine;

namespace NGToolsEditor.NGSpotlight
{
	public interface IFilterInstance
	{
		/// <summary>Level 0 define a main filter, from which sub-filters can relate. An entry needs to be in at least one level 0 filter.</summary>
		int	FilterLevel { get; }
		/// <summary>Links a filter to a level 0 filter, allowing to filter in only if the level 0 filter is filtering in the entry.</summary>
		int	FamilyMask { get; }

		bool	CheckFilterRequirements(NGSpotlightWindow window);
		bool	CheckFilterIn(NGSpotlightWindow window, IDrawableElement element);
		float	GetWidth();
		void	OnGUI(Rect r, NGSpotlightWindow window);
	}
}