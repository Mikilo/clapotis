using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NGToolsEditor.NGSpotlight
{
	public static class ScenesImporter
	{
		private static List<GameObject>	roots = new List<GameObject>(32);
		private static bool				mustScanScenes = true;

		static	ScenesImporter()
		{
			EditorSceneManager.sceneLoaded += ScenesImporter.OnSceneLoaded;
			EditorSceneManager.sceneUnloaded += ScenesImporter.OnSceneUnloaded;
			EditorSceneManager.newSceneCreated += ScenesImporter.OnNewSceneCreated;
			EditorSceneManager.sceneOpened += ScenesImporter.OnSceneOpened;
			EditorSceneManager.sceneClosed += ScenesImporter.OnSceneClosed;

			// TODO Unity <5.6 backward compatibility?
			MethodInfo	OnActiveSceneChangedInEditModeMethod = typeof(ScenesImporter).GetMethod("OnActiveSceneChangedInEditMode", BindingFlags.Static | BindingFlags.NonPublic);

			try
			{
				EventInfo	activeSceneChangedInEditModeEvent = typeof(EditorSceneManager).GetEvent("activeSceneChangedInEditMode");
				activeSceneChangedInEditModeEvent.AddEventHandler(null, Delegate.CreateDelegate(activeSceneChangedInEditModeEvent.EventHandlerType, null, OnActiveSceneChangedInEditModeMethod));
				//EditorSceneManager.activeSceneChangedInEditMode += ScenesImporter.OnActiveSceneChangedInEditMode;
			}
			catch
			{
			}
			
			// TODO Unity <5.6 backward compatibility?
			MethodInfo	OnHierarchyChangedMethod = typeof(ScenesImporter).GetMethod("OnHierarchyChanged", BindingFlags.Static | BindingFlags.NonPublic);

			try
			{
				EventInfo	hierarchyChangedEvent = typeof(EditorApplication).GetEvent("hierarchyChanged");
				hierarchyChangedEvent.AddEventHandler(null, Delegate.CreateDelegate(hierarchyChangedEvent.EventHandlerType, null, OnHierarchyChangedMethod));
				//EditorApplication.hierarchyChanged += ScenesImporter.OnHierarchyChanged;
			}
			catch
			{
				FieldInfo	hierarchyWindowChangedField = UnityAssemblyVerifier.TryGetField(typeof(EditorApplication), "hierarchyWindowChanged", BindingFlags.Static | BindingFlags.Public);
				if (hierarchyWindowChangedField != null)
					hierarchyWindowChangedField.SetValue(null, Delegate.Combine((Delegate)hierarchyWindowChangedField.GetValue(null), Delegate.CreateDelegate(hierarchyWindowChangedField.FieldType, null, OnHierarchyChangedMethod)));
				//EditorApplication.hierarchyWindowChanged += ScenesImporter.OnHierarchyChanged;
			}
		}

		private static void	ScanScene(UnityEngine.SceneManagement.Scene scene)
		{
			NGSpotlightWindow.DeleteKey(scene.path);

			if (scene.isLoaded == true)
			{
				scene.GetRootGameObjects(ScenesImporter.roots);

				for (int j = 0; j < ScenesImporter.roots.Count; j++)
					ScenesImporter.BrowseGameObject(scene.path, ScenesImporter.roots[j]);
			}
		}

		private static void	BrowseGameObject(string key, GameObject go)
		{
			NGSpotlightWindow.AddEntry(key, new SceneGameObjectDrawer(go));

			for (int i = 0; i < go.transform.childCount; i++)
				ScenesImporter.BrowseGameObject(key, go.transform.GetChild(i).gameObject);
		}

		[SpotlightUpdatingResult]
		private static void	OnResultUpdating()
		{
			if (ScenesImporter.mustScanScenes == true)
			{
				ScenesImporter.mustScanScenes = false;

				for (int i = 0; i < EditorSceneManager.sceneCount; i++)
					ScenesImporter.ScanScene(EditorSceneManager.GetSceneAt(i));
			}
		}

		private static void	OnActiveSceneChangedInEditMode(UnityEngine.SceneManagement.Scene a, UnityEngine.SceneManagement.Scene b)
		{
			//Debug.Log("ActiveChange " + a.path + " <> " + b.path);
			ScenesImporter.mustScanScenes = true;
		}

		private static void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
		{
			//Debug.Log("Unloaded " + scene.path);
			ScenesImporter.mustScanScenes = true;
		}

		private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
		{
			//Debug.Log("Loaded " + scene.path);
			ScenesImporter.mustScanScenes = true;
		}

		private static void	OnHierarchyChanged()
		{
			//Debug.Log("Change");
			ScenesImporter.mustScanScenes = true;
		}

		private static void	OnNewSceneCreated(UnityEngine.SceneManagement.Scene scene, NewSceneSetup setup, NewSceneMode mode)
		{
			//Debug.Log("New " + scene.path);
			ScenesImporter.mustScanScenes = true;
		}

		private static void	OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
		{
			//Debug.Log("Opened " + scene.path);
			ScenesImporter.mustScanScenes = true;
		}

		private static void	OnSceneClosed(UnityEngine.SceneManagement.Scene scene)
		{
			//Debug.Log("Close " + scene.path);
			NGSpotlightWindow.DeleteKey(scene.path);
		}
	}
}