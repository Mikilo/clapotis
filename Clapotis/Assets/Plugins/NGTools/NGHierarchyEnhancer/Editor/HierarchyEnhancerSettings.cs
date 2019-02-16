using System;
using UnityEngine;

namespace NGToolsEditor.NGHierarchyEnhancer
{
	public class HierarchyEnhancerSettings : ScriptableObject
	{
		[Serializable]
		public class ComponentColor
		{
			public Type		type;
			public Color	color;
			public Texture	icon;
		}

		public const int	TotalLayers = 32;

		public bool				enable = true;
		public float			margin = 0F;
		public EventModifiers	holdModifiers = EventModifiers.Shift;
		public EventModifiers	selectionHoldModifiers = EventModifiers.Alt;
		public Color[]			layers;
		public Texture2D[]		layersIcon;
		public float			widthPerComponent = 16F;
		public ComponentColor[]	componentData;
		public bool				drawUnityComponents = false;

		public void	InitializeLayers()
		{
			if (this.layersIcon == null ||
				this.layersIcon.Length != HierarchyEnhancerSettings.TotalLayers)
			{
				this.layersIcon = new Texture2D[HierarchyEnhancerSettings.TotalLayers];
			}

			if (this.layers == null ||
				this.layers.Length != HierarchyEnhancerSettings.TotalLayers)
			{
				this.layers = new Color[HierarchyEnhancerSettings.TotalLayers];
			}

			if (this.componentData == null)
				this.componentData = new ComponentColor[0];
		}
	}
}