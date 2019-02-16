using UnityEditor;

namespace NGToolsEditor.NGRemoteScene
{
	using UnityEngine;

	public class ImportAssetsWindow : NGRemoteWindow
	{
		private sealed class OptionsPopup : PopupWindowContent
		{
			private readonly ImportAssetsWindow	window;

			public	OptionsPopup(ImportAssetsWindow window)
			{
				this.window = window;
			}

			public override Vector2	GetWindowSize()
			{
				return new Vector2(425F, (Constants.SingleLineHeight + 2F) * 15F);
			}

			public override void	OnGUI(Rect r)
			{
				using (LabelWidthRestorer.Get(175F))
				{
					EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
					{
						GUILayout.Label("General");
					}
					EditorGUILayout.EndHorizontal();

					EditorGUI.BeginChangeCheck();
					this.window.Hierarchy.displayNonSuppported = EditorGUILayout.Toggle("Display Non-Suppported", this.window.Hierarchy.displayNonSuppported);
					GUILayout.Label("Only Texture2D & Mesh are supported, others will be discarded.", GeneralStyles.WrapLabel);
					TooltipHelper.HelpBox("If you need to implement your own,\nlook for the C# interface IObjectImporter in NGTools.NGRemoteScene.", MessageType.Info);

					this.window.Hierarchy.overridePrefab = EditorGUILayout.Toggle("Override Prefab", this.window.Hierarchy.overridePrefab);
					GUILayout.Label("If a prefab exists at the location, it will be overridden. Otherwise a copy nearby will be created.", GeneralStyles.WrapLabel);
					if (EditorGUI.EndChangeCheck() == true)
						this.window.Repaint();

					GUILayout.Space(5F);

					EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
					{
						GUILayout.Label("Auto Mode Settings");
					}
					EditorGUILayout.EndHorizontal();
					TooltipHelper.HelpBox("The following settings apply when Auto mode is selected.", MessageType.Info);

					EditorGUI.BeginChangeCheck();
					this.window.Hierarchy.specificSharedSubFolder = EditorGUILayout.TextField("Specific Import Folder", this.window.Hierarchy.specificSharedSubFolder);
					GUILayout.Label("Set a path to force import of each asset into the root of the given folder, leave it blank to import near prefab's location.", GeneralStyles.WrapLabel);

					GUILayout.Space(5F);

					//this.window.hierarchy.rawCopyAssetsToSubFolder = EditorGUILayout.Toggle("Import Asset Into Sub-Folder", this.window.hierarchy.rawCopyAssetsToSubFolder);
					//GUILayout.Label("Import every assets into a sub-folder using its prefab name.", GeneralStyles.WrapLabel);

					//this.window.hierarchy.prefixAsset = EditorGUILayout.Toggle("Prefix Asset", this.window.hierarchy.prefixAsset);
					//GUILayout.Label("Prefix each asset with its prefab name.", GeneralStyles.WrapLabel);
					//if (EditorGUI.EndChangeCheck() == true)
					//{
					//	for (int i = 0; i < this.window.hierarchy.ImportingAssetsParams.Count; i++)
					//		this.window.hierarchy.ImportingAssetsParams[i].autoPath = null;
					//	this.window.Repaint();
					//}
				}

				TooltipHelper.PostOnGUI();
			}
		}

		private enum Tab
		{
			Prefabs,
			Assets
		}

		public new const string	Title = "Import Assets";
		public const float	GameObjectIconWidth = 22F;

		private Tab		tab = Tab.Prefabs;
		private Vector2	scrollPosition;

		private int					selectedPrefab = 0;
		internal PrefabGameObject	selectedGameObject;
		internal PrefabField		selectedField;
		private Vector2				hierarchyScrollPosition;
		private Vector2				inspectorScrollPosition;

		public static void	Open(NGRemoteHierarchyWindow hierarchy)
		{
			Utility.OpenWindow<ImportAssetsWindow>(true, ImportAssetsWindow.Title, true, null, window =>
			{
				window.SetHierarchy(hierarchy);
			});
		}

		protected override void	OnEnable()
		{
			base.OnEnable();

			Utility.LoadEditorPref(this, NGEditorPrefs.GetPerProjectPrefix());
		}

		protected override void	OnDisable()
		{
			base.OnDisable();

			Utility.SaveEditorPref(this, NGEditorPrefs.GetPerProjectPrefix());
		}

		protected override void	OnGUIHeader()
		{
			bool	has = false;

			for (int i = 0; i < this.Hierarchy.ImportingAssetsParams.Count; i++)
			{
				if (this.Hierarchy.ImportingAssetsParams[i].localAsset == null &&
					this.Hierarchy.ImportingAssetsParams[i].isSupported == true &&
					this.Hierarchy.ImportingAssetsParams[i].originPath != null)
				{
					has = true;
					break;
				}
			}

			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				if (GUILayout.Button("☰", "GV Gizmo DropDown", GUILayoutOptionPool.ExpandWidthFalse) == true)
					PopupWindow.Show(new Rect(0F, 16F, 0F, 0F), new OptionsPopup(this));

				EditorGUI.BeginChangeCheck();
				NGEditorGUILayout.OutlineToggle("Prefabs", this.tab == Tab.Prefabs);
				if (EditorGUI.EndChangeCheck() == true)
					this.tab = Tab.Prefabs;

				EditorGUI.BeginChangeCheck();
				NGEditorGUILayout.OutlineToggle("Assets", this.tab == Tab.Assets);
				if (EditorGUI.EndChangeCheck() == true)
					this.tab = Tab.Assets;

				GUILayout.FlexibleSpace();

				if (has == true && this.Hierarchy.IsClientConnected() == true)
				{
					if (GUILayout.Button("Set Auto to all", GeneralStyles.ToolbarButton) == true)
					{
						for (int i = 0; i < this.Hierarchy.ImportingAssetsParams.Count; i++)
						{
							if (this.Hierarchy.ImportingAssetsParams[i].localAsset == null &&
								this.Hierarchy.ImportingAssetsParams[i].isSupported == true &&
								this.Hierarchy.ImportingAssetsParams[i].originPath != null)
							{
								this.Hierarchy.ImportingAssetsParams[i].importMode = ImportMode.Auto;
							}
						}
					}

					GUILayout.Space(10F);

					if (GUILayout.Button("Confirm all", GeneralStyles.ToolbarButton) == true)
					{
						for (int i = 0; i < this.Hierarchy.ImportingAssetsParams.Count; i++)
							this.Hierarchy.ImportingAssetsParams[i].Confirm();
					}
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		protected override void	OnGUIConnected()
		{
			this.scrollPosition = EditorGUILayout.BeginScrollView(this.scrollPosition);
			{

				if (this.tab == Tab.Prefabs)
					this.DrawPrefabs();
				else
					this.DrawAllAssets();
			}
			EditorGUILayout.EndScrollView();
		}

		private void	DrawAllAssets()
		{
			Rect	r = new Rect(0F, 18F, this.position.width, this.position.height - 18F);

			for (int i = 0; i < this.Hierarchy.ImportingAssetsParams.Count; i++)
			{
				if (this.Hierarchy.displayNonSuppported == false && this.Hierarchy.ImportingAssetsParams[i].isSupported == false)
					continue;

				r.height = this.Hierarchy.ImportingAssetsParams[i].GetHeight();
				this.Hierarchy.ImportingAssetsParams[i].DrawAssetImportParams(r, this);
				r.y += r.height;
			}
		}

		private void	DrawPrefabs()
		{
			EditorGUILayout.BeginHorizontal();
			{
				for (int i = 0; i < this.Hierarchy.PendingPrefabs.Count; i++)
				{
					EditorGUI.BeginChangeCheck();
					NGEditorGUILayout.OutlineToggle(this.Hierarchy.PendingPrefabs[i].rootGameObject.gameObject.name, this.selectedPrefab == i);
					if (EditorGUI.EndChangeCheck() == true)
						this.selectedPrefab = i;
				}

				GUILayout.FlexibleSpace();
			}
			EditorGUILayout.EndHorizontal();

			if (this.selectedPrefab < this.Hierarchy.PendingPrefabs.Count)
			{
				PrefabConstruct	prefab = this.Hierarchy.PendingPrefabs[this.selectedPrefab];

				GUILayout.Space(10F + 32F);

				Rect	r = GUILayoutUtility.GetLastRect();

				r.xMin += 20F;
				r.width = 150F;
				if (GUI.Button(r, prefab.rootGameObject.gameObject.name) == true)
					this.Hierarchy.PingObject(prefab.rootGameObject.gameObject.instanceID);

				r.x += r.width + 10F;

				if (prefab.constructionError != null)
					EditorGUI.HelpBox(r, prefab.constructionError, MessageType.Error);
				else if (prefab.outputPath != null)
				{
					if (GUI.Button(r, "Local Prefab", GeneralStyles.ToolbarDropDown) == true)
						EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(prefab.outputPath, typeof(Object)));
				}

				r.width = 20F;
				if (GUI.Button(r, GUIContent.none, GeneralStyles.ToolbarDropDown) == true)
				{
				}

				GUILayout.Space(11F);

				r = GUILayoutUtility.GetLastRect();

				r.y += r.height - 1F;
				r.height = 1F;
				r.width = this.position.width;
				EditorGUI.DrawRect(r, Color.cyan);
				r.y += 1F;
				r.height = 20F;

				Rect	rH = r;
				rH.width = Mathf.Round(rH.width * .33F);
				GUI.Label(rH, "Hierarchy");

				rH.y += rH.height;
				rH.height = prefab.rootGameObject.GetHeight();

				Rect	view = new Rect(0F, 0F, 0F, rH.height);

				this.hierarchyScrollPosition = GUI.BeginScrollView(rH, this.hierarchyScrollPosition, view);
				{
					rH.y = 0F;
					rH.height = Constants.SingleLineHeight;
					prefab.rootGameObject.DrawGameObject(rH, this);
				}
				GUI.EndScrollView();

				Rect	rI = r;
				rI.x += rH.width;

				rI.width = 1F;
				rI.height = this.position.height - rI.y;
				rI.y += 1F;
				EditorGUI.DrawRect(rI, Color.cyan);
				rI.y -= 1F;
				rI.x += 1F;

				rI.width = this.position.width - rH.xMax - 1F;
				rI.height = Constants.SingleLineHeight;
				GUI.Label(rI, "Inspector");

				rI.y += rI.height;
				rI.height = this.position.height - rI.yMax;

				if (this.selectedGameObject != null)
				{
					if (this.selectedGameObject.components != null)
					{
						if (this.selectedGameObject.components.Length > 0)
						{
							view.height = 0F;
							for (int i = 0; i < this.selectedGameObject.components.Length; i++)
								view.height += this.selectedGameObject.components[i].GetHeight(this, Hierarchy);
						}
						else
							view.height = 32F;

						this.inspectorScrollPosition = GUI.BeginScrollView(rI, this.inspectorScrollPosition, view);
						{
							rI.x = 0F;
							rI.y = 0F;

							if (this.selectedGameObject.components.Length > 0)
							{
								for (int i = 0; i < this.selectedGameObject.components.Length; i++)
								{
									rI.height = this.selectedGameObject.components[i].GetHeight(this, Hierarchy);
									this.selectedGameObject.components[i].DrawComponent(rI, this, this.Hierarchy);
									rI.y += rI.height;
								}
							}
							else
							{
								rI.height = 32F;
								rI.xMin += 5F;
								rI.xMax -= 5F;
								EditorGUI.HelpBox(rI, "Does not contain a Component with importable assets.", MessageType.Info);
							}
						}
						GUI.EndScrollView();
					}
					else
					{
						EditorGUI.HelpBox(rI, "Loading Components.", MessageType.Info);
						GUI.Label(rI, GeneralStyles.StatusWheel);
					}
				}
			}
		}
	}
}