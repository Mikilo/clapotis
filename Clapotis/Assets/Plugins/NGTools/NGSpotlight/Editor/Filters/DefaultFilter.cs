using System;
using UnityEditorInternal;
using UnityEngine;

namespace NGToolsEditor.NGSpotlight
{
	internal sealed class DefaultFilter : AssetFilter, IFilterInstance
	{
		int	IFilterInstance.FilterLevel { get { return this.level; } }
		int	IFilterInstance.FamilyMask { get { return this.family; } }

		private int			level;
		private int			family;
		private string		extension;
		private Type		type;

		public	DefaultFilter(int level, int family, Texture2D icon, Type type, string key, string description) : base(key, description)
		{
			this.level = level;
			this.family = family;
			this.type = type;
			this.icon = icon;
		}

		public	DefaultFilter(int level, int family, string extension, Type type, string key, string description) : base(key, description)
		{
			this.level = level;
			this.family = family;
			this.type = type;
			this.extension = extension;
			this.icon = InternalEditorUtility.GetIconForFile(this.extension);
		}

		public override IFilterInstance	Identify(NGSpotlightWindow window, string keywords, string lowerKeywords)
		{
			if (lowerKeywords == this.key)
				return this;
			return null;
		}

		public override bool	CheckFilterRequirements(NGSpotlightWindow window)
		{
			for (int i = 0; i < window.filterInstances.Count; i++)
			{
				DefaultFilter	filter = window.filterInstances[i] as DefaultFilter;
				if (filter != null && filter.key == this.key)
					return false;
			}

			return true;
		}

		bool	IFilterInstance.CheckFilterIn(NGSpotlightWindow window, IDrawableElement element)
		{
			DefaultAssetDrawer	drawer = element as DefaultAssetDrawer;

			if (drawer != null)
				return drawer.type == this.type;

			return element.GetType() == type;
		}

		float	IFilterInstance.GetWidth()
		{
			return 24F;
		}

		void	IFilterInstance.OnGUI(Rect r, NGSpotlightWindow window)
		{
			if (GUI.Button(r, string.Empty, GeneralStyles.ToolbarButton) == true)
				window.RemoveFilterInstance(this);
			GUI.DrawTexture(r, this.icon, ScaleMode.ScaleToFit);
		}

		public override string	ToString()
		{
			return this.key;
		}
	}
}