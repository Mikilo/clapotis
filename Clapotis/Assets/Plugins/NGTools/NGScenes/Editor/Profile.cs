using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace NGToolsEditor.NGScenes
{
	[Serializable]
	public sealed class Profile
	{
		[Serializable]
		public sealed class Scene
		{
			public string	path;
			public bool		active;
		}

		public string		name;
		public List<Scene>	scenes = new List<Scene>();

		public void	Load()
		{
			List<EditorBuildSettingsScene>	buildScenes = new List<EditorBuildSettingsScene>(this.scenes.Count);

			for (int i = 0; i < this.scenes.Count; i++)
				buildScenes.Add(new EditorBuildSettingsScene(this.scenes[i].path, this.scenes[i].active == true && File.Exists(this.scenes[i].path) == true));

			EditorBuildSettings.scenes = buildScenes.ToArray();
		}

		public void	Save()
		{
			this.scenes.Clear();

			for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
				this.scenes.Add(new Scene() { path = EditorBuildSettings.scenes[i].path, active = EditorBuildSettings.scenes[i].enabled });
		}
	}
}