using UnityEngine;
using UnityEditor;

namespace NGToolsEditor.NGMissingScriptRecovery
{
	[CustomEditor(typeof(MonoBehaviour), true)]
	public class MissingGUIEditor : Editor
	{
		private bool	isNull;
		private bool	isPrefab;

		protected virtual void	OnEnable()
		{
			if (this.target == null || this.serializedObject.targetObject != null)
				return;

			this.isNull = true;

			try
			{
				GameObject	active = Selection.activeGameObject;
				PrefabType	prefabType = PrefabUtility.GetPrefabType(active);
				Object		prefab = null;

				if (prefabType == PrefabType.Prefab)
					prefab = active;
				else if (prefabType == PrefabType.PrefabInstance)
					prefab = PrefabUtility.GetPrefabParent(active);
				else if (prefabType == PrefabType.DisconnectedPrefabInstance)
					prefab = PrefabUtility.GetPrefabParent(active);
				this.isPrefab = prefab != null;
			}
			catch
			{
			}
		}

		public override void	OnInspectorGUI()
		{
			if (this.isNull == true)
			{
				EditorGUILayout.HelpBox("The associated script can not be loaded.\nPlease fix any compile errors\nand assign a valid script.", MessageType.Warning);

				if (this.isPrefab == true)
				{
					if (GUILayout.Button("Recover Missing Component") == true)
					{
						NGMissingScriptRecoveryWindow	instance = EditorWindow.GetWindow<NGMissingScriptRecoveryWindow>(true, NGMissingScriptRecoveryWindow.NormalTitle);

						if (instance.IsRecovering == false)
						{
							instance.Diagnose(Selection.activeGameObject);
							instance.tab = NGMissingScriptRecoveryWindow.Tab.Selection;
							instance.Show();
						}
						else
						{
							EditorUtility.DisplayDialog(NGMissingScriptRecoveryWindow.NormalTitle, "A recovery process is still running.", "OK");
						}
					}
				}
				else
				{
					EditorGUILayout.HelpBox(NGMissingScriptRecoveryWindow.NormalTitle + " can only recover from prefabs.\nCreate a prefab of your broken asset and recover from it.", MessageType.Warning);
				}
			}
			else
				base.OnInspectorGUI();
		}
	}
}