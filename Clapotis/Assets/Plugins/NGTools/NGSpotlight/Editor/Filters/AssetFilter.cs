using UnityEngine;

namespace NGToolsEditor.NGSpotlight
{
	public abstract class AssetFilter
	{
		public Texture2D	icon;
		public readonly string	key;
		public readonly string	description;

		protected	AssetFilter(string key, string description)
		{
			this.key = key;
			this.description = description;
		}

		/// <summary>Defines if the current keywords match this filter.</summary>
		/// <param name="window"></param>
		/// <param name="keywords"></param>
		/// <param name="lowerKeywords"></param>
		/// <returns></returns>
		public abstract IFilterInstance	Identify(NGSpotlightWindow window, string keywords, string lowerKeywords);

		/// <summary>Defines if the filter can show up in the available list of filters.</summary>
		/// <param name="window"></param>
		/// <param name="parent"></param>
		/// <returns></returns>
		public abstract bool	CheckFilterRequirements(NGSpotlightWindow window);
	}
}