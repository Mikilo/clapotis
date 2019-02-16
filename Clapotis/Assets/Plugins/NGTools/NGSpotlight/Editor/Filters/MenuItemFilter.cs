using UnityEngine;

namespace NGToolsEditor.NGSpotlight
{
	internal sealed class MenuItemFilter : AssetFilter, IFilterInstance
	{
		int	IFilterInstance.FilterLevel { get { return 0; } }
		int	IFilterInstance.FamilyMask { get { return 1 << 1; } }

		public	MenuItemFilter() : base(":mi", "Menu items.")
		{
		}

		public override IFilterInstance	Identify(NGSpotlightWindow window, string keywords, string lowerKeywords)
		{
			if (lowerKeywords.Length == 3 && lowerKeywords[0] == ':' && lowerKeywords[1] == 'm' && lowerKeywords[2] == 'i')
				return this;
			return null;
		}

		public override bool	CheckFilterRequirements(NGSpotlightWindow window)
		{
			for (int i = 0; i < window.filterInstances.Count; i++)
			{
				if (window.filterInstances[i] is MenuItemFilter)
					return false;
			}

			return true;
		}

		bool	IFilterInstance.CheckFilterIn(NGSpotlightWindow window, IDrawableElement element)
		{
			return element is MenuItemDrawer;
		}

		float	IFilterInstance.GetWidth()
		{
			return 40F;
		}

		void	IFilterInstance.OnGUI(Rect r, NGSpotlightWindow window)
		{
			if (GUI.Button(r, "Menu", GeneralStyles.ToolbarButton) == true)
				window.RemoveFilterInstance(this);
		}

		public override string	ToString()
		{
			return this.key;
		}
	}
}