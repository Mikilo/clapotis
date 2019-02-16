using UnityEngine;

namespace NGToolsEditor.NGSpotlight
{
	internal sealed class ExtensionFileFilter : AssetFilter
	{
		private class ExtensionFileInstance : IFilterInstance
		{
			int	IFilterInstance.FilterLevel { get { return 0; } }
			int	IFilterInstance.FamilyMask { get { return 1 << 2; } }

			public string	extension;

			private ExtensionFileFilter	parent;
			private string				label;
			private float				width;

			public	ExtensionFileInstance(ExtensionFileFilter parent, string extension)
			{
				this.parent = parent;
				this.extension = extension;
				this.label = "Ext:" + extension;
				Utility.content.text = this.label;
				this.width = GeneralStyles.ToolbarButton.CalcSize(Utility.content).x;
			}

			bool	IFilterInstance.CheckFilterIn(NGSpotlightWindow window, IDrawableElement element)
			{
				return element.LowerStringContent.EndsWith(this.extension);
			}

			float	IFilterInstance.GetWidth()
			{
				return this.width;
			}

			void	IFilterInstance.OnGUI(Rect r, NGSpotlightWindow window)
			{
				if (GUI.Button(r, this.label, GeneralStyles.ToolbarButton) == true)
					window.RemoveFilterInstance(this);
			}

			bool	IFilterInstance.CheckFilterRequirements(NGSpotlightWindow window)
			{
				return this.parent.CheckFilterRequirements(window);
			}

			public override string	ToString()
			{
				return "Extension:" + this.extension;
			}
		}

		public	ExtensionFileFilter() : base(":.{EXT}", "Extension of the file.")
		{
		}

		public override IFilterInstance	Identify(NGSpotlightWindow window, string keywords, string lowerKeywords)
		{
			if (lowerKeywords.Length > 2 && lowerKeywords[0] == ':' && lowerKeywords[1] == '.')
			{
				string	ext = lowerKeywords.Substring(1);

				for (int i = 0; i < window.filterInstances.Count; i++)
				{
					ExtensionFileInstance	filter = window.filterInstances[i] as ExtensionFileInstance;

					if (filter != null && filter.extension != ext)
					{
						window.error.Add("Extension \"" + ext + "\" is already present.");
						return null;
					}
				}

				return new ExtensionFileInstance(this, ext);
			}

			return null;
		}

		public override bool	CheckFilterRequirements(NGSpotlightWindow window)
		{
			return true;
		}
	}
}