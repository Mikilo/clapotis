using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace NGTools.NGRemoteScene
{
	public sealed class ServerScene
	{
		public int						buildIndex;
		public string					name;
		public List<ServerGameObject>	roots = new List<ServerGameObject>(8);

		public	ServerScene(Scene scene)
		{
			this.buildIndex = scene.buildIndex;
			this.name = scene.name;
		}

		public void	Reset(Scene scene)
		{
			this.buildIndex = scene.buildIndex;
			this.name = scene.name;
			this.roots.Clear();
		}
	}
}