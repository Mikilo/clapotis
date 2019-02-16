using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace NGToolsEditor
{
	using UnityEngine;

	public static class RootGameObjectsManager
	{
		public static Action	RootChanged;

		private static List<GameObject>	rootObjects = new List<GameObject>();

		public static int				UpdateHierarchyCounter = 0;
		public static List<GameObject>	RootObjects
		{
			get
			{
				if (RootGameObjectsManager.rootObjects.Count == 0)
				{
					List<GameObject>	roots = new List<GameObject>();

					for (int i = 0; i < EditorSceneManager.sceneCount; i++)
					{
						Scene	scene = EditorSceneManager.GetSceneAt(i);

						if (scene.isLoaded == true)
						{
							scene.GetRootGameObjects(roots);
							RootGameObjectsManager.rootObjects.AddRange(roots);
						}
					}
				}

				return RootGameObjectsManager.rootObjects;
			}
		}

		static	RootGameObjectsManager()
		{
			// TODO Unity <5.6 backward compatibility?
			MethodInfo	ResetRootObjectsMethod = typeof(RootGameObjectsManager).GetMethod("ResetRootObjects", BindingFlags.Static | BindingFlags.NonPublic);

			try
			{
				EventInfo	hierarchyChangedEvent = typeof(EditorApplication).GetEvent("hierarchyChanged");
				hierarchyChangedEvent.AddEventHandler(null, Delegate.CreateDelegate(hierarchyChangedEvent.EventHandlerType, null, ResetRootObjectsMethod));
				//EditorApplication.hierarchyChanged += RootGameObjectsManager.ResetRootObjects;
			}
			catch
			{
				FieldInfo	hierarchyWindowChangedField = UnityAssemblyVerifier.TryGetField(typeof(EditorApplication), "hierarchyWindowChanged", BindingFlags.Static | BindingFlags.Public);
				if (hierarchyWindowChangedField != null)
					hierarchyWindowChangedField.SetValue(null, Delegate.Combine((Delegate)hierarchyWindowChangedField.GetValue(null), Delegate.CreateDelegate(hierarchyWindowChangedField.FieldType, null, ResetRootObjectsMethod)));
				//EditorApplication.hierarchyWindowChanged += RootGameObjectsManager.ResetRootObjects;
			}

			NGEditorApplication.ChangeScene += RootGameObjectsManager.ClearRootGameObjects;
		}

		private static void	ClearRootGameObjects()
		{
			RootGameObjectsManager.rootObjects.Clear();

			if (RootGameObjectsManager.RootChanged != null)
				RootGameObjectsManager.RootChanged();
		}

		private static void	ResetRootObjects()
		{
			++RootGameObjectsManager.UpdateHierarchyCounter;
			RootGameObjectsManager.rootObjects.Clear();
		}
	}
}