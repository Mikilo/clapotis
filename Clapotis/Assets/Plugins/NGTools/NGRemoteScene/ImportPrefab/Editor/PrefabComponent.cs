using NGTools.NGRemoteScene;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	public class PrefabComponent
	{
		private static List<PrefabField>	pool = new List<PrefabField>(8);

		public ClientComponent	component;
		public PrefabField[]	fields;

		public bool	open = true;
		public bool	hasImportableAssets = false;

		public	PrefabComponent(NGRemoteHierarchyWindow hierarchy, List<AssetImportParameters> existingRefs, PrefabConstruct prefab, ClientComponent component)
		{
			this.component = component;

			for (int i = 0; i < this.component.fields.Length; i++)
			{
				PrefabField field = this.FetchUnityObjectReferences(hierarchy, existingRefs, prefab, this.component.fields[i].name, this.component.fields[i].value);

				if (field != null)
				{
					field.UpdateIsSupported();
					if (field.isSupported == true)
						this.hasImportableAssets = true;

					PrefabComponent.pool.Add(field);
				}
			}

			if (PrefabComponent.pool.Count > 0)
			{
				this.fields = PrefabComponent.pool.ToArray();
				PrefabComponent.pool.Clear();
			}
		}

		public float	GetHeight(ImportAssetsWindow importAssets, NGRemoteHierarchyWindow hierarchy)
		{
			float	height = Constants.SingleLineHeight;

			for (int i = 0; i < this.fields.Length; i++)
				height += this.fields[i].GetHeight(importAssets, hierarchy);

			return height;
		}

		public void	DrawComponent(Rect r, ImportAssetsWindow importAssets, NGRemoteHierarchyWindow hierarchy)
		{
			r.height = Constants.SingleLineHeight;
			this.open = EditorGUI.Foldout(r, this.open, this.component.name, false);

			if (this.open == true)
			{
				r.y += r.height;

				if (hierarchy.displayNonSuppported == true || this.hasImportableAssets == true)
				{
					++EditorGUI.indentLevel;
					for (int i = 0; i < this.fields.Length; i++)
					{
						r.height = this.fields[i].GetHeight(importAssets, hierarchy);
						this.fields[i].DrawField(r, importAssets, hierarchy);
						r.y += r.height;
					}
					--EditorGUI.indentLevel;
				}
				else
				{
					r.xMin += 5F;
					r.xMax -= 5F;
					r.height = 24F;
					EditorGUI.HelpBox(r, "Does not contain importable assets.", MessageType.Info);
				}
			}
		}

		private PrefabField	FetchUnityObjectReferences(NGRemoteHierarchyWindow hierarchy, List<AssetImportParameters> globalRefs, PrefabConstruct prefab, string name, object value)
		{
			UnityObject	uo = value as UnityObject;

			if (uo != null)
			{
				if (uo.instanceID == 0)
					return null;

				AssetImportParameters	importParameters = null;

				// AssetImportParameters must be known in 3 places to be reused: global, prefab & fields.
				for (int i = 0; i < prefab.importParameters.Count; i++)
				{
					if (prefab.importParameters[i].instanceID == uo.instanceID)
					{
						importParameters = prefab.importParameters[i];
						break;
					}
				}

				if (importParameters == null)
				{
					for (int i = 0; i < globalRefs.Count; i++)
					{
						if (globalRefs[i].instanceID == uo.instanceID)
						{
							importParameters = globalRefs[i];
							break;
						}
					}

					if (importParameters != null)
						prefab.importParameters.Add(importParameters);
				}

				if (importParameters == null)
				{
					Debug.Log("C");
					importParameters = new AssetImportParameters(hierarchy, name, uo.type, this.component.parent.instanceID, this.component.instanceID, uo.instanceID, RemoteUtility.GetImportAssetTypeSupported(uo.type) != null, null) { prefabPath = Path.GetDirectoryName(prefab.path) };
					prefab.importParameters.Add(importParameters);
					globalRefs.Add(importParameters);
				}
				else
					importParameters.originPath.Add(new AssetImportParameters.MyClass() { gameObjectInstanceID = this.component.parent.instanceID, componentInstanceID = this.component.instanceID, path = name });

				return new PrefabField(name, importParameters);
			}

			ClientClass	gc = value as ClientClass;

			if (gc != null)
			{
				PrefabField	field = null;

				for (int j = 0; j < gc.fields.Length; j++)
				{
					PrefabField	child = this.FetchUnityObjectReferences(hierarchy, globalRefs, prefab, gc.fields[j].name, gc.fields[j].value);

					if (child != null)
					{
						if (field == null)
							field = new PrefabField(name);
						field.AddChild(child);
					}
				}

				return field;
			}

			ArrayData	a = value as ArrayData;

			if (a != null && a.array != null)
			{
				PrefabField	field = null;

				for (int j = 0; j < a.array.Length; j++)
				{
					PrefabField	child = this.FetchUnityObjectReferences(hierarchy, globalRefs, prefab, j.ToCachedString(), a.array.GetValue(j));

					if (child != null)
					{
						if (field == null)
							field = new PrefabField(name);
						field.AddChild(child);
					}
				}

				return field;
			}

			return null;
		}
	}
}