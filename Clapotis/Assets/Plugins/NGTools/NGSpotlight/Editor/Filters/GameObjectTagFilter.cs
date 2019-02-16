using UnityEngine;

namespace NGToolsEditor.NGSpotlight
{
	internal sealed class GameObjectTagFilter : AssetFilter
	{
		private class GameObjectTagInstance : IFilterInstance
		{
			int	IFilterInstance.FilterLevel { get { return 1; } }
			int	IFilterInstance.FamilyMask { get { return 1 << 0; } }

			private GameObjectTagFilter	parent;
			private string				tag;
			private string				label;
			private float				width;

			public	GameObjectTagInstance(GameObjectTagFilter parent, string tag)
			{
				this.parent = parent;
				this.tag = tag;
				this.label = "Tag:" + tag;

				Utility.content.text = this.label;
				this.width = GeneralStyles.ToolbarButton.CalcSize(Utility.content).x;
			}

			bool	IFilterInstance.CheckFilterIn(NGSpotlightWindow window, IDrawableElement element)
			{
				IHasGameObject	hasGameObject = element as IHasGameObject;

				try
				{
					return hasGameObject != null && hasGameObject.GameObject != null && hasGameObject.GameObject.CompareTag(this.tag) == true;
				}
				catch
				{
					// In case tags are programmatically changed... We never know.
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

			public override string ToString()
			{
				return ":tag=" + this.tag;
			}
		}

		public	GameObjectTagFilter() : base(":tag=\"\"", "Tag of a Game Object.")
		{
		}

		public override IFilterInstance	Identify(NGSpotlightWindow window, string keywords, string lowerKeywords)
		{
			if (lowerKeywords.Length >= 5 && lowerKeywords[0] == ':' && lowerKeywords[1] == 't' && lowerKeywords[2] == 'a' && lowerKeywords[3] == 'g' && lowerKeywords[4] == '=')
			{
				if (lowerKeywords.Length == 5)
					window.error.Add("A tag is required.");
				else
				{
					string	tag = keywords.Substring(5);

					for (int i = 0; i < UnityEditorInternal.InternalEditorUtility.tags.Length; i++)
					{
						if (UnityEditorInternal.InternalEditorUtility.tags[i] == tag)
							return new GameObjectTagInstance(this, tag);
					}

					window.error.Add("Tag \"" + tag + "\" does not exist. (" + string.Join(", ", UnityEditorInternal.InternalEditorUtility.tags) + ")");
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