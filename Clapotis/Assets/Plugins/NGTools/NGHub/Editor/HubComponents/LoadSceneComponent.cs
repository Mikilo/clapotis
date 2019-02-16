using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace NGToolsEditor.NGHub
{
	using UnityEngine;

	[Serializable, Category("Scene")]
	internal sealed class LoadSceneComponent : HubComponent
	{
		private static readonly Color	InvalidScene = Color.red * .7F;

		[Exportable]
		public string	scene;
		[Exportable]
		public string	alias;

		[NonSerialized]
		private GUIContent	content;
		[NonSerialized]
		private GUIStyle	buttonStyle;
		[NonSerialized]
		private GUIStyle	menuStyle;

		[NonSerialized]
		private bool	isSceneValid;

		public	LoadSceneComponent() : base("Load Scene", true, true)
		{
		}

		public override void	Init(NGHubWindow hub)
		{
			base.Init(hub);

			this.content = new GUIContent(string.IsNullOrEmpty(this.alias) == true ? this.scene : this.alias, this.scene);
			this.isSceneValid = this.IsValid(this.scene);
		}

		public override void	OnPreviewGUI(Rect r)
		{
			if (this.isSceneValid == true)
				GUI.Label(r, "Scene \"" + this.scene + "\"");
			else
				GUI.Label(r, "Scene \"" + this.scene + "\"", GeneralStyles.ErrorLabel);
		}

		public override void	OnEditionGUI()
		{
			using (LabelWidthRestorer.Get(80F))
			{
				EditorGUI.BeginChangeCheck();
				EditorGUI.BeginChangeCheck();
				this.scene = NGEditorGUILayout.OpenFileField("Scene Path", this.scene, "unity", NGEditorGUILayout.FieldButtons.Browse);
				if (EditorGUI.EndChangeCheck() == true)
				{
					int	n = this.scene.IndexOf("/Assets/");

					if (n != -1)
						this.scene = this.scene.Substring(n + 1); // +1 because of the first slash.
				}

				if (this.isSceneValid == false)
					EditorGUILayout.HelpBox("Scene does not exist.", MessageType.Warning);

				this.alias = EditorGUILayout.TextField("Alias", this.alias);
				if (EditorGUI.EndChangeCheck() == true)
				{
					this.isSceneValid = this.IsValid(this.scene);
					this.content.text = string.IsNullOrEmpty(this.alias) == true ? this.scene : this.alias;
					this.content.tooltip = this.scene;
				}
			}
		}

		public override void	OnGUI()
		{
			if (this.buttonStyle == null)
			{
				this.buttonStyle = new GUIStyle("ButtonLeft");
				this.menuStyle = new GUIStyle("DropDownButton");
			}

			float	w = this.buttonStyle.CalcSize(this.content).x;

			this.buttonStyle.padding.left = (int)this.hub.height;
			w += 12F; // Remove texture width, because Button calculates using the whole height.

			Rect	r = GUILayoutUtility.GetRect(w, this.hub.height, GUI.skin.button);
			r.width += 6F;

			if (Event.current.type == EventType.MouseDrag &&
				Utility.position2D != Vector2.zero &&
				DragAndDrop.GetGenericData(Utility.DragObjectDataName) != null &&
				(Utility.position2D - Event.current.mousePosition).sqrMagnitude >= Constants.MinStartDragDistance)
			{
				Utility.position2D = Vector2.zero;
				DragAndDrop.StartDrag("Drag Scene");
				Event.current.Use();
			}
			else if (Event.current.type == EventType.MouseUp && r.Contains(Event.current.mousePosition) == true)
			{
				// Check just before, since we don't actively check.
				this.isSceneValid = this.IsValid(this.scene);
				if (this.isSceneValid == true)
					this.LoadScene(this.scene, (int)OpenSceneMode.Single);
			}
			else if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition) == true)
			{
				Utility.position2D = Event.current.mousePosition;
				DragAndDrop.PrepareStartDrag();
				DragAndDrop.objectReferences = new Object[] { AssetDatabase.LoadMainAssetAtPath(this.scene) };
				DragAndDrop.SetGenericData(Utility.DragObjectDataName, 1);
			}

			if (this.isSceneValid == false)
				this.buttonStyle.normal.textColor = LoadSceneComponent.InvalidScene;
			else
				this.buttonStyle.normal.textColor = GUI.skin.button.normal.textColor;

			GUI.Button(r, this.content, this.buttonStyle);

			r = GUILayoutUtility.GetLastRect();

			r.x += 4F;
			r.width = r.height;
			GUI.DrawTexture(r, UtilityResources.UnityIcon);

			menuStyle.fixedHeight = this.hub.height;
			menuStyle.padding.left = 0;
			menuStyle.margin.left = 0;
			menuStyle.border.left = 0;

			r = GUILayoutUtility.GetRect(20F, this.hub.height, menuStyle);
			if (GUI.Button(r, "", menuStyle) == true)
			{
				GenericMenu	menu = new GenericMenu();

				if (this.isSceneValid == true)
				{
					menu.AddItem(new GUIContent("Load single"), false, this.LoadScene);
					menu.AddItem(new GUIContent("Load additive"), false, this.LoadSceneAdditive);
					menu.AddItem(new GUIContent("Load additive without loading"), false, this.LoadSceneAdditiveWithoutLoading);
					if (AssetDatabase.LoadAssetAtPath(this.scene, typeof(Object)) != null)
						menu.AddItem(new GUIContent("Ping"), false, this.PingScene);
				}
				else
				{
					menu.AddDisabledItem(new GUIContent("Load single"));
					menu.AddDisabledItem(new GUIContent("Load additive"));
					menu.AddDisabledItem(new GUIContent("Load additive without loading"));
					if (AssetDatabase.LoadAssetAtPath(this.scene, typeof(Object)) != null)
						menu.AddDisabledItem(new GUIContent("Ping"));
				}

				menu.DropDown(r);
			}
		}

		public override void	InitDrop(NGHubWindow hub)
		{
			base.InitDrop(hub);

			this.scene = AssetDatabase.GetAssetPath(DragAndDrop.objectReferences[0]);
			this.alias = DragAndDrop.objectReferences[0].name;
			this.content = new GUIContent(string.IsNullOrEmpty(this.alias) == true ? this.scene : this.alias, this.scene);
			this.isSceneValid = this.IsValid(this.scene);
		}

		private void	LoadScene()
		{
			bool	exist = File.Exists(this.scene);

			if (exist == true)
				this.LoadScene(this.scene, (int)OpenSceneMode.Single);
		}

		private void	LoadSceneAdditive()
		{
			bool	exist = File.Exists(this.scene);

			if (exist == true)
				this.LoadScene(this.scene, (int)OpenSceneMode.Additive);
		}

		private void	LoadSceneAdditiveWithoutLoading()
		{
			bool	exist = File.Exists(this.scene);

			if (exist == true)
				this.LoadScene(this.scene, (int)OpenSceneMode.AdditiveWithoutLoading);
		}

		private void	LoadScene(string path, int mode)
		{
			bool	exist = File.Exists(path);

			if (exist == true)
			{
				if (EditorApplication.isPlaying == true)
				{
					Scene	scene = SceneManager.GetSceneByPath(path);

					if (scene.IsValid() == true)
						SceneManager.LoadScene(scene.name, (LoadSceneMode)Mathf.Min(mode, 1));
					else
					{
						EditorBuildSettingsScene[]	buildScenes = EditorBuildSettings.scenes;

						for (int i = 0; i < buildScenes.Length; i++)
						{
							if (buildScenes[i].path == scene.path)
								return;
						}
					}
				}
				else if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == true)
					EditorSceneManager.OpenScene(path, (OpenSceneMode)mode);
			}
		}

		private void	PingScene()
		{
			EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(this.scene, typeof(Object)));
		}

		private bool	IsValid(string path)
		{
			return AssetDatabase.LoadAssetAtPath<SceneAsset>(this.scene) != null;
		}

		private static bool	CanDrop()
		{
			return DragAndDrop.objectReferences.Length > 0 && DragAndDrop.objectReferences[0] is SceneAsset;
		}
	}
}