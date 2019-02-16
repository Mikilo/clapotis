using NGTools;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace NGToolsEditor
{
	using UnityEngine;

	public static class NGEditorApplication
	{
		private const string	InvisibleGameObjectName = "NGEditorExit";

		public static event Action	ChangeScene;
		public static event Action	EditorExit;

		private static Scene	scene;
		private static int		frameCount;
		private static float	realtimeSinceStartup;
		private static int		renderedFrameCount;

		static	NGEditorApplication()
		{
			EditorApplication.update += NGEditorApplication.DetectChangeScene;

			Utility.SafeDelayCall(() =>
			{
				GameObject[]	gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();

				for (int i = 0; i < gameObjects.Length; i++)
				{
					if (gameObjects[i].name.Equals(NGEditorApplication.InvisibleGameObjectName) == true)
						Object.DestroyImmediate(gameObjects[i]);
				}

				GameObject	gameObject = new GameObject(NGEditorApplication.InvisibleGameObjectName, typeof(EditorExitBehaviour));
				gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.HideInHierarchy;
				gameObject.GetComponent<EditorExitBehaviour>().callback = () =>
				{
					if (NGEditorApplication.EditorExit != null)
						NGEditorApplication.EditorExit();
				};
			});
		}

		private static void	DetectChangeScene()
		{
			if (NGEditorApplication.scene != EditorSceneManager.GetActiveScene() ||
				// Detect change with the same scene, the real time should fill 99% of cases. Not tested but if you are able to load 2 scenes in a single frame, the time might not work.
				NGEditorApplication.realtimeSinceStartup > Time.realtimeSinceStartup ||
				NGEditorApplication.frameCount > Time.frameCount ||
				NGEditorApplication.renderedFrameCount > Time.renderedFrameCount)
			{
				NGEditorApplication.scene = EditorSceneManager.GetActiveScene();

				if (NGEditorApplication.ChangeScene != null)
					NGEditorApplication.ChangeScene();
			}

			NGEditorApplication.frameCount = Time.frameCount;
			NGEditorApplication.realtimeSinceStartup = Time.realtimeSinceStartup;
			NGEditorApplication.renderedFrameCount = Time.renderedFrameCount;
		}
	}
}