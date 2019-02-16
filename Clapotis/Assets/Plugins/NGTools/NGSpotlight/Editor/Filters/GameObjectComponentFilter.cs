using System;
using UnityEngine;

namespace NGToolsEditor.NGSpotlight
{
	internal sealed class GameObjectComponentFilter : AssetFilter
	{
		private class GameObjectComponentInstance : IFilterInstance
		{
			int	IFilterInstance.FilterLevel { get { return 1; } }
			int	IFilterInstance.FamilyMask { get { return 1 << 0; } }

			private GameObjectComponentFilter	parent;
			private Type						type;
			private string						label;
			private float						width;

			public	GameObjectComponentInstance(GameObjectComponentFilter parent, Type type)
			{
				this.parent = parent;
				this.type = type;
				this.label = "Component:" + type.Name;

				Utility.content.text = this.label;
				this.width = GeneralStyles.ToolbarButton.CalcSize(Utility.content).x;
			}

			bool	IFilterInstance.CheckFilterIn(NGSpotlightWindow window, IDrawableElement element)
			{
				IHasGameObject	hasGameObject = element as IHasGameObject;

				return hasGameObject != null && hasGameObject.GameObject != null && hasGameObject.GameObject.GetComponent(this.type) != null;
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
				return ":comp=" + this.type.Name;
			}
		}

		public	GameObjectComponentFilter() : base(":comp=\"\"", "Name of a Component.")
		{
		}

		public override IFilterInstance	Identify(NGSpotlightWindow window, string keywords, string lowerKeywords)
		{
			if (lowerKeywords.Length >= 6 && lowerKeywords[0] == ':' && lowerKeywords[1] == 'c' && lowerKeywords[2] == 'o' && lowerKeywords[3] == 'm' && lowerKeywords[4] == 'p' && lowerKeywords[5] == '=')
			{
				if (lowerKeywords.Length == 6)
					window.error.Add("A type Component is required.");
				else
				{
					keywords = keywords.Substring(6);
					Type	type = Type.GetType(keywords);

					if (type == null)
						type = Utility.GetType(keywords);

					if (type == null)
						window.error.Add("Component \"" + keywords + "\" does not exist. (MeshRenderer, Rigidbody, Light, Camera, etc...)");
					else if (typeof(Component).IsAssignableFrom(type) == false)
						window.error.Add("Type \"" + type.FullName + "\" must derive from Component.");
					else
						return new GameObjectComponentInstance(this, type);
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