using NGTools.Network;
using NGTools.NGRemoteScene;
using System.Collections.Generic;

namespace NGToolsEditor.NGRemoteScene
{
	public sealed class ClientScene
	{
		public readonly int						buildIndex;
		public readonly string					name;
		public readonly List<ClientGameObject>	roots = new List<ClientGameObject>(8);

		public bool	fold = true;

		private bool	hasSelection;
		/// <summary>
		/// Defines if this game object is selected or contains selected children.
		/// </summary>
		public bool		HasSelection { get { return this.hasSelection; } }

		private bool	selected;
		public bool		Selected
		{
			get
			{
				return this.selected;
			}
			set
			{
				if (this.selected != value)
				{
					this.selected = value;
					this.UpdateHasSelection();
				}
			}
		}

		/// <summary>Used to discard deleted GameObject on the server-side when this is not updated.</summary>
		public int	lastHierarchyUpdate;

		public	ClientScene(NetScene scene, IUnityData unityData)
		{
			this.buildIndex = scene.buildIndex;
			this.name = scene.name;

			for (int i = 0; i < scene.roots.Length; i++)
				this.roots.Add(new ClientGameObject(this, null, scene.roots[i], unityData));
		}

		public void	ClearSelection()
		{
			if (this.selected == true || this.hasSelection == true)
			{
				this.selected = false;
				this.hasSelection = false;

				for (int i = 0; i < this.roots.Count; i++)
					this.roots[i].ClearSelection();
			}
		}

		public void	UpdateHasSelection()
		{
			this.hasSelection = this.selected;

			// If not selected, look into children.
			if (this.hasSelection == false)
			{
				for (int i = 0; i < this.roots.Count; i++)
				{
					if (this.roots[i].HasSelection == true)
					{
						this.hasSelection = true;
						break;
					}
				}
			}
		}
	}
}