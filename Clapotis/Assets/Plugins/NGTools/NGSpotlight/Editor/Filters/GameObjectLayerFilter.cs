using UnityEngine;

namespace NGToolsEditor.NGSpotlight
{
	internal sealed class GameObjectLayerFilter : AssetFilter
	{
		private class GameObjectLayerInstance : IFilterInstance
		{
			int	IFilterInstance.FilterLevel { get { return 1; } }
			int	IFilterInstance.FamilyMask { get { return 1 << 0; } }

			private GameObjectLayerFilter	parent;
			private int						layer;
			private string					label;
			private float					width;

			public	GameObjectLayerInstance(GameObjectLayerFilter parent, string layer)
			{
				this.parent = parent;
				this.layer = LayerMask.NameToLayer(layer);
				this.label = "Layer:" + layer;

				Utility.content.text = this.label;
				this.width = GeneralStyles.ToolbarButton.CalcSize(Utility.content).x;
			}

			bool	IFilterInstance.CheckFilterIn(NGSpotlightWindow window, IDrawableElement element)
			{
				IHasGameObject	hasGameObject = element as IHasGameObject;

				try
				{
					return hasGameObject != null && hasGameObject.GameObject != null && hasGameObject.GameObject.layer == this.layer;
				}
				catch
				{
					// In case layers are programmatically changed... We never know.
				}

				return false;
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
				return ":layer=" + this.layer;
			}
		}

		public	GameObjectLayerFilter() : base(":layer=\"\"", "Layer of a Game Object.")
		{
		}

		public override IFilterInstance	Identify(NGSpotlightWindow window, string keywords, string lowerKeywords)
		{
			if (lowerKeywords.Length >= 7 && lowerKeywords[0] == ':' && lowerKeywords[1] == 'l' && lowerKeywords[2] == 'a' && lowerKeywords[3] == 'y' && lowerKeywords[4] == 'e' && lowerKeywords[5] == 'r' && lowerKeywords[6] == '=')
			{
				if (lowerKeywords.Length == 7)
					window.error.Add("A layer is required.");
				else
				{
					string	layer = keywords.Substring(7);

					for (int i = 0; i < UnityEditorInternal.InternalEditorUtility.layers.Length; i++)
					{
						if (UnityEditorInternal.InternalEditorUtility.layers[i] == layer)
							return new GameObjectLayerInstance(this, layer);
					}

					window.error.Add("Layer \"" + layer + "\" does not exist. (" + string.Join(", ", UnityEditorInternal.InternalEditorUtility.layers) + ")");
				}
			}

			return null;
		}

		public override bool	CheckFilterRequirements(NGSpotlightWindow window)
		{
			for (int i = 0; i < window.filterInstances.Count; i++)
			{
				if (window.filterInstances[i].FilterLevel == 0 &&
					window.filterInstances[i].FamilyMask == 1 << 0)
				{
					return true;
				}
			}

			return false;
		}
	}
}