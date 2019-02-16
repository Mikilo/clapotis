using NGTools.Network;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	public class PrefabGameObject
	{
		private static List<PrefabComponent>	pool = new List<PrefabComponent>(4);

		public readonly ClientGameObject	gameObject;
		public readonly PrefabGameObject[]	children;
		public PrefabComponent[]			components;

		public bool	open = true;

		public	PrefabGameObject(ClientGameObject gameObject)
		{
			this.gameObject = gameObject;

			this.children = new PrefabGameObject[gameObject.children.Count];

			for (int i = 0; i < this.children.Length; i++)
				this.children[i] = new PrefabGameObject(gameObject.children[i]);
		}

		public bool	VerifyComponentsReady(NGRemoteHierarchyWindow hierarchy, List<AssetImportParameters> globalRefs, PrefabConstruct prefab, Client client)
		{
			bool	verified = true;

			if (this.gameObject.components == null)
			{
				this.gameObject.RequestComponents(client, go =>
				{
					this.ConstructComponents(hierarchy, globalRefs, prefab);
					Utility.RepaintEditorWindow(typeof(NGRemoteWindow));
				});

				verified = false;
			}
			else if (this.components == null)
				this.ConstructComponents(hierarchy, globalRefs, prefab);

			for (int i = 0; i < this.children.Length; i++)
			{
				if (this.children[i].VerifyComponentsReady(hierarchy, globalRefs, prefab, client) == false)
					verified = false;
			}

			return verified;
		}

		private void	ConstructComponents(NGRemoteHierarchyWindow hierarchy, List<AssetImportParameters> globalRefs, PrefabConstruct prefab)
		{
			for (int i = 0; i < this.gameObject.components.Count; i++)
			{
				PrefabComponent	component = new PrefabComponent(hierarchy, globalRefs, prefab, this.gameObject.components[i]);

				if (component.fields != null)
					PrefabGameObject.pool.Add(component);
			}

			this.components = PrefabGameObject.pool.ToArray();
			PrefabGameObject.pool.Clear();
		}

		public float	GetHeight()
		{
			int						count = 0;
			Stack<ClientGameObject>	parents = new Stack<ClientGameObject>();

			parents.Push(this.gameObject);

			while (parents.Count > 0)
			{
				ClientGameObject	parent = parents.Pop();

				for (int i = 0; i < parent.children.Count; i++)
				{
					if (parent.children[i].children.Count > 0)
						parents.Push(parent.children[i]);
					else
						++count;
				}

				++count;
			}

			return count * Constants.SingleLineHeight;
		}

		public void	DrawGameObject(Rect r, ImportAssetsWindow importAssets)
		{
			//Rect	r = GUILayoutUtility.GetRect(0F, Constants.SingleLineHeight);
			if (Event.current.type == EventType.MouseDown)
			{
				float	w = r.width;

				r.xMin += 16F * EditorGUI.indentLevel;
				r.width = 16F;

				if (Event.current.mousePosition.y > r.y &&
					Event.current.mousePosition.y < r.y + r.height &&
					(this.children.Length == 0 || r.Contains(Event.current.mousePosition) == false))
				{
					importAssets.selectedGameObject = this;
					Event.current.Use();
				}

				r.xMin -= 16F * EditorGUI.indentLevel;
				r.width = w;
			}
			else if (Event.current.type == EventType.Repaint && importAssets.selectedGameObject == this)
				EditorGUI.DrawRect(r, NGRemoteHierarchyWindow.SelectedObjectBackgroundColor);

			Utility.content.text = this.gameObject.name;
			Utility.content.image = this.IsRequestingAttention() == true ? UtilityResources.WarningIcon : null;
			if (this.children.Length > 0)
				this.open = EditorGUI.Foldout(r, this.open, Utility.content, false);
			else
				EditorGUI.LabelField(r, Utility.content);
			Utility.content.image = null;

			if (this.open == true)
			{
				++EditorGUI.indentLevel;

				for (int i = 0; i < this.children.Length; i++)
				{
					r.y += r.height;
					this.children[i].DrawGameObject(r, importAssets);
				}

				--EditorGUI.indentLevel;
			}
		}

		private bool	IsRequestingAttention()
		{
			if (this.components != null)
			{
				for (int i = 0; i < this.components.Length; i++)
				{
					if (this.components[i].hasImportableAssets == true)
					{
						for (int j = 0; j < this.components[i].fields.Length; j++)
						{
							if (this.components[i].fields[j].isSupported == true)
							{
								if (this.components[i].fields[j].importParameters != null &&
									//this.components[i].fields[j].importParameters.finalized == false ||
									this.components[i].fields[j].importParameters.ParametersConfirmed == false)
								{
									return true;
								}
							}
						}
					}
				}
			}

			if (this.children.Length > 0)
			{
				for (int i = 0; i < this.children.Length; i++)
				{
					if (this.children[i].IsRequestingAttention() == true)
						return true;
				}
			}

			return false;
		}
	}
}