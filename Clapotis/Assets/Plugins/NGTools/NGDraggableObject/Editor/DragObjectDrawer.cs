using NGTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;

namespace NGToolsEditor.NGDraggableObject
{
	using UnityEngine;

	[InitializeOnLoad]
	public sealed class DragObjectDrawer : PropertyDrawer
	{
		private class PopupGenericMenu : PopupWindowContent
		{
			private struct Item
			{
				public string			path;
				public bool				set;
				public bool				selectable;
				public Action<object>	callback;
				public object			argument;
				public bool				disable;
			}

			public static readonly Color	HighlightBackgroundColor = Color.cyan;

			private List<Item>				items = new List<Item>();

			public override void	OnOpen()
			{
				this.editorWindow.wantsMouseMove = true;
			}

			public override Vector2	GetWindowSize()
			{
				return new Vector2(this.editorWindow.position.width, this.items.Count * 18F);
			}

			public override void	OnGUI(Rect r)
			{
				if (Event.current.type == EventType.MouseMove)
					this.editorWindow.Repaint();

				r.height = 16F;

				for (int i = 0; i < this.items.Count; i++)
				{
					bool	isDisabled = this.items[i].disable == true || this.items[i].callback == null;

					if (Event.current.type == EventType.Repaint &&
						r.Contains(Event.current.mousePosition) == true)
					{
						if (isDisabled == true)
							EditorGUI.DrawRect(r, PopupGenericMenu.HighlightBackgroundColor * .5F);
						else
							EditorGUI.DrawRect(r, PopupGenericMenu.HighlightBackgroundColor * .7F);
					}

					if (this.items[i].selectable == true)
					{
						Rect	r2 = r;
						Color	c = isDisabled ? Color.grey : Color.black;

						r2.width = r2.height;
						r2.x += 2F;
						r2.width -= 4F;
						r2.y += 2F;
						r2.height -= 4F;
						Utility.DrawUnfillRect(r2, c);

						if (this.items[i].set == true)
						{
							r2.x += 2F;
							r2.width -= 4F;
							r2.y += 2F;
							r2.height -= 4F;
							EditorGUI.DrawRect(r2, c);
						}
					}

					r.xMin += r.height + 2F + 2F;

					EditorGUI.BeginDisabledGroup(isDisabled);
					{
						GUI.Label(r, this.items[i].path);
						if (Event.current.type == EventType.MouseDown &&
							this.items[i].callback != null &&
							r.Contains(Event.current.mousePosition) == true)
						{
							this.items[i].callback(this.items[i].argument);
							this.editorWindow.Close();
							return;
						}
					}
					EditorGUI.EndDisabledGroup();

					r.xMin -= r.height + 2F + 2F;
					r.y += r.height + 2F;
				}
			}

			public void	Add(string path, bool set, bool selectable, Action<object> callback, object argument)
			{
				this.items.Add(new Item() { path = path, set = set, selectable = selectable, callback = callback, argument = argument });
			}

			public void AddDisable(string path, bool set, bool selectable, Action<object> callback, object argument)
			{
				this.items.Add(new Item() { path = path, set = set, selectable = selectable, callback = callback, argument = argument, disable = true });
			}

			public int	Count()
			{
				return this.items.Count;
			}
		}

		private class DataMenu
		{
			public SerializedProperty	property;
			public Object				target;
			public Rect					position;
			public int					offset;
		}

		public const string	Title = "NG Draggable Object";
		public const string	LastSelectedPath = "Last selected/";

		private static readonly MethodInfo	methodUpdateIfRequiredOrScript = typeof(SerializedObject).GetMethod("UpdateIfRequiredOrScript") ?? typeof(SerializedObject).GetMethod("UpdateIfDirtyOrScript");
		private static Object				copiedObject;

		private static readonly Type		ScriptAttributeUtility = UnityAssemblyVerifier.TryGetType(typeof(Editor).Assembly, "UnityEditor.ScriptAttributeUtility");
		private static readonly FieldInfo	s_DrawerTypeForType;
		private static readonly MethodInfo	BuildDrawerTypeForTypeDictionary;

		private static readonly Type		DrawerKeySet;
		private static readonly FieldInfo	drawer;
		private static readonly FieldInfo	type;

		private static object		initialDictionary;
		private static IDictionary	overridenDictionary;

		static	DragObjectDrawer()
		{
			if (DragObjectDrawer.ScriptAttributeUtility != null)
			{
				DragObjectDrawer.s_DrawerTypeForType = UnityAssemblyVerifier.TryGetField(DragObjectDrawer.ScriptAttributeUtility, "s_DrawerTypeForType", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
				DragObjectDrawer.BuildDrawerTypeForTypeDictionary = UnityAssemblyVerifier.TryGetMethod(DragObjectDrawer.ScriptAttributeUtility, "BuildDrawerTypeForTypeDictionary", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
				DragObjectDrawer.DrawerKeySet = UnityAssemblyVerifier.TryGetNestedType(DragObjectDrawer.ScriptAttributeUtility, "DrawerKeySet", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

				if (DragObjectDrawer.DrawerKeySet != null)
				{
					DragObjectDrawer.drawer = UnityAssemblyVerifier.TryGetField(DragObjectDrawer.DrawerKeySet, "drawer", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
					DragObjectDrawer.type = UnityAssemblyVerifier.TryGetField(DragObjectDrawer.DrawerKeySet, "type", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				}

				if (DragObjectDrawer.s_DrawerTypeForType == null ||
					DragObjectDrawer.BuildDrawerTypeForTypeDictionary == null ||
					DragObjectDrawer.DrawerKeySet == null ||
					DragObjectDrawer.drawer == null ||
					DragObjectDrawer.type == null)
				{
					DragObjectDrawer.ScriptAttributeUtility = null;
				}
			}

			HQ.SettingsChanged += () =>
			{
				if (HQ.Settings == null)
					return;

				if (HQ.Settings.Get<DraggableObjectSettings>().active == true)
					DragObjectDrawer.AddType();
				else
					DragObjectDrawer.RemoveType();
			};
		}

		[NGSettings(DragObjectDrawer.Title)]
		private static void	OnGUISettings()
		{
			if (HQ.Settings == null)
				return;

			if (DragObjectDrawer.ScriptAttributeUtility == null)
			{
				EditorGUILayout.HelpBox("NG Tools has detected a change in Unity code. Please contact the author.", MessageType.Error);

				if (GUILayout.Button("Contact the author") == true)
					ContactFormWizard.Open(ContactFormWizard.Subject.BugReport, "DragObjectDrawer is incompatible with " + Utility.UnityVersion + ".");
				return;
			}

			DraggableObjectSettings	settings = HQ.Settings.Get<DraggableObjectSettings>();

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Allows to drag & drop between Object fields in Inspector.", GeneralStyles.WrapLabel);
			bool	active = EditorGUILayout.Toggle("Drag & Drop with Object", settings.active);
			if (EditorGUI.EndChangeCheck() == true)
			{
				HQ.InvalidateSettings();

				if (active == true)
					DragObjectDrawer.AddType();
				else
					DragObjectDrawer.RemoveType();

				settings.active = active;
			}
		}

		private static void	AddType()
		{
			if (DragObjectDrawer.overridenDictionary == null)
				DragObjectDrawer.InitializeDragDictionaries();

			Selection.selectionChanged += DragObjectDrawer.OnSelectionChanged;
			DragObjectDrawer.s_DrawerTypeForType.SetValue(null, DragObjectDrawer.overridenDictionary);
			DragObjectDrawer.UpdateInspector();
		}

		private static void	RemoveType()
		{
			if (DragObjectDrawer.initialDictionary == null)
				DragObjectDrawer.InitializeInitialDictionaries();

			Selection.selectionChanged -= DragObjectDrawer.OnSelectionChanged;
			DragObjectDrawer.s_DrawerTypeForType.SetValue(null, DragObjectDrawer.initialDictionary);
			DragObjectDrawer.UpdateInspector();
		}

		private static Object[]	lastSelected = new Object[3];

		private static void	OnSelectionChanged()
		{
			if (Selection.activeObject != null)
			{
				for (int i = 0; i < DragObjectDrawer.lastSelected.Length; i++)
				{
					if (DragObjectDrawer.lastSelected[i] == Selection.activeObject)
						return;

					if (DragObjectDrawer.lastSelected[i] == null)
					{
						DragObjectDrawer.lastSelected[i] = Selection.activeObject;
						return;
					}
				}

				if (DragObjectDrawer.lastSelected[0] != Selection.activeObject)
				{
					for (int i = DragObjectDrawer.lastSelected.Length - 2; i >= 0; --i)
						DragObjectDrawer.lastSelected[i + 1] = DragObjectDrawer.lastSelected[i];

					DragObjectDrawer.lastSelected[0] = Selection.activeObject;
				}
			}
		}

		private static void	UpdateInspector()
		{
			if (Selection.objects.Length > 0 && (Selection.objects[0] is GameObject) == false)
				return;

			Object[]	o = Selection.objects;
			Selection.objects = new Object[0];
			InternalEditorUtility.RepaintAllViews();

			EditorApplication.delayCall += () => Selection.objects = o;
		}

		public	DragObjectDrawer()
		{
			Metrics.UseTool(5); // NGDraggableObject
			NGChangeLogWindow.CheckLatestVersion(NGAssemblyInfo.Name);
		}

		public override void	OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Type	realType = this.fieldInfo.FieldType;
			Color	restore = GUI.backgroundColor;

			if (this.fieldInfo.FieldType.IsUnityArray() == true)
				realType = Utility.GetArraySubType(this.fieldInfo.FieldType);

			if (Event.current.type == EventType.Repaint &&
				DragAndDrop.visualMode == DragAndDropVisualMode.Copy &&
				position.Contains(Event.current.mousePosition) == true)
			{
				GUI.backgroundColor = Color.yellow;
			}
			else if ((Event.current.type == EventType.DragUpdated ||
					  Event.current.type == EventType.DragPerform) &&
					 position.Contains(Event.current.mousePosition) == true)
			{
				if (DragAndDrop.objectReferences.Length > 0 && this.CanDrop(property, DragAndDrop.objectReferences[0]) == true)
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				else
					DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;

				if (Event.current.type == EventType.DragPerform)
				{
					DragAndDrop.AcceptDrag();

					this.ExtractAsset(position, this.fieldInfo.FieldType, DragAndDrop.objectReferences[0], property);

					DragAndDrop.PrepareStartDrag();
				}

				Event.current.Use();
			}
			else if (Event.current.type == EventType.MouseDrag &&
					 (Utility.position2D - Event.current.mousePosition).sqrMagnitude >= Constants.MinStartDragDistance &&
					 property.objectReferenceInstanceIDValue.Equals(DragAndDrop.GetGenericData(Utility.DragObjectDataName)) == true)
			{
				DragAndDrop.StartDrag("Drag Object");
				Event.current.Use();
			}
			else if (Event.current.type == EventType.MouseDown)
			{
				Utility.position2D = Event.current.mousePosition;

				if (Event.current.button == 0)
				{
					DragAndDrop.PrepareStartDrag();

					if (position.Contains(Event.current.mousePosition) == true)
					{
						if (property.objectReferenceInstanceIDValue != 0)
						{
							DragAndDrop.objectReferences = new Object[] { property.objectReferenceValue };
							DragAndDrop.SetGenericData(Utility.DragObjectDataName, property.objectReferenceInstanceIDValue);
						}
					}
				}
			}
			else if (Event.current.type == EventType.MouseUp)
			{
				if (Event.current.button == 1 &&
					position.Contains(Event.current.mousePosition) == true)
				{
					GameObject	cgo = property.objectReferenceValue as GameObject;

					position.xMin += EditorGUIUtility.labelWidth;
					if (cgo != null)
						this.TryDropDownComponents(position, realType, property, cgo, false, false);
					else
					{
						Component	cc = property.objectReferenceValue as Component;

						if (cc != null && cc.gameObject != null)
							this.TryDropDownComponents(position, realType, property, cc.gameObject, false, false);
						else
							this.DefaultMenu(position, property);
					}
					position.xMin -= EditorGUIUtility.labelWidth;

					Event.current.Use();
				}

				DragAndDrop.PrepareStartDrag();
			}
			else if (Event.current.type == EventType.ContextClick)
				Event.current.Use();

			GUI.SetNextControlName(property.serializedObject.GetHashCode() + property.propertyPath);
			Type	fieldType = this.fieldInfo.FieldType.IsUnityArray() == false ? this.fieldInfo.FieldType : fieldType = Utility.GetArraySubType(this.fieldInfo.FieldType);
			EditorGUI.ObjectField(position, property, fieldType, label);

			if (Event.current.type == EventType.ValidateCommand &&
				(Event.current.commandName == "Copy" ||
				Event.current.commandName == "Cut" ||
				Event.current.commandName == "Paste") &&
				GUI.GetNameOfFocusedControl() == property.serializedObject.GetHashCode() + property.propertyPath)
			{
				Event.current.Use();
			}
			else if (Event.current.type == EventType.ExecuteCommand &&
					 Event.current.commandName == "Copy" &&
					 GUI.GetNameOfFocusedControl() == property.serializedObject.GetHashCode() + property.propertyPath)
			{
				DragObjectDrawer.copiedObject = property.objectReferenceValue;
				Event.current.Use();
			}
			else if (Event.current.type == EventType.ExecuteCommand &&
					 Event.current.commandName == "Cut" &&
					 GUI.GetNameOfFocusedControl() == property.serializedObject.GetHashCode() + property.propertyPath)
			{
				DragObjectDrawer.copiedObject = property.objectReferenceValue;
				property.objectReferenceValue = null;
				Event.current.Use();
			}
			else if (Event.current.type == EventType.ExecuteCommand &&
					 Event.current.commandName == "Paste" &&
					 GUI.GetNameOfFocusedControl() == property.serializedObject.GetHashCode() + property.propertyPath)
			{
				if (DragObjectDrawer.copiedObject != null)
				{
					property.objectReferenceValue = DragObjectDrawer.copiedObject;
					Event.current.Use();
				}
			}

			// Display the position of the Component in its GameObject if there is many.
			Component	c = property.objectReferenceValue as Component;

			if (c != null && c.gameObject != null)
			{
				Component[]	cs = c.gameObject.GetComponents(typeof(Component));

				for (int i = 0, j = 0; i < cs.Length; i++)
				{
					if (cs[i] == null)
						continue;

					if (realType.IsAssignableFrom(cs[i].GetType()) == true)
						++j;

					if (j >= 2)
					{
						EditorGUI.indentLevel = 0;
						for (int k = 0; k < cs.Length; k++)
						{
							if (cs[k] == null)
								continue;

							if (cs[k].GetInstanceID() == property.objectReferenceInstanceIDValue)
							{
								if (k < 9)
									position.width = 20F;
								else
									position.width = 28F;

								if (string.IsNullOrEmpty(label.text) == false)
									position.x += EditorGUIUtility.labelWidth - position.width;
								else
									position.x -= position.width;

								if (Event.current.type == EventType.MouseDown &&
									position.Contains(Event.current.mousePosition) == true)
								{
									this.DropDownMultiComponents(position, c.gameObject, cs, realType, property, false);
									Event.current.Use();
								}

								EditorGUI.LabelField(position, "#" + (k + 1).ToString());
								break;
							}
						}
						break;
					}
				}
			}

			GUI.backgroundColor = restore;
		}

		private bool	CanDrop(SerializedProperty property, Object asset)
		{
			PrefabType	type = PrefabUtility.GetPrefabType(property.serializedObject.targetObject);

			if (type == PrefabType.Prefab ||
				type == PrefabType.ModelPrefab ||
				(type == PrefabType.None && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(property.serializedObject.targetObject)) == false))
			{
				PrefabType	dragType = PrefabUtility.GetPrefabType(asset);

				if (dragType != PrefabType.Prefab &&
					dragType != PrefabType.ModelPrefab &&
					(dragType != PrefabType.None || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset)) == true))
				{
					return false;
				}
			}

			return this.CanDrop(this.fieldInfo.FieldType, asset);
		}

		private bool	CanDrop(Type type, Object asset)
		{
			if (asset == null)
				return false;

			if (type.IsUnityArray() == true)
				type = Utility.GetArraySubType(type);

			if (type.IsAssignableFrom(asset.GetType()) == true)
				return true;

			Object[]	assets;
			string		assetPath = AssetDatabase.GetAssetPath(asset);

			// Avoid Unity scenes. They throw error "Do not use ReadObjectThreaded on scene objects!".
			if (assetPath != null && assetPath.EndsWith(".unity") == true && (asset.GetType() == typeof(DefaultAsset) || asset.GetType() == typeof(SceneAsset)))
				return false;

			if (string.IsNullOrEmpty(assetPath) == true)
				assets = new Object[] { asset };
			else
			{
				// In the case of a prefab, we need to enforce fetching assets of the focused GameObject.
				if (assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) == true)
				{
					assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

					List<Object>	relatedAssets = new List<Object>();
					GameObject		targetGameObject = asset as GameObject;

					if (targetGameObject == null)
						targetGameObject = (asset as Component).gameObject;

					for (int i = 0; i < assets.Length; i++)
					{
						Component	component = assets[i] as Component;

						if (component != null)
						{
							if (component.gameObject == targetGameObject)
								relatedAssets.Add(assets[i]);
						}
						else
						{
							GameObject	go = assets[i] as GameObject;

							if (go != null && go == targetGameObject)
								relatedAssets.Add(assets[i]);
						}
					}

					assets = relatedAssets.ToArray();
				}
				else
					assets = Utility.SafeLoadAllAssetsAtPath(assetPath);
			}

			for (int i = 0; i < assets.Length; i++)
			{
				if (type.IsAssignableFrom(assets[i].GetType()) == true)
					return true;

				GameObject	gameObject = assets[i] as GameObject;

				if (gameObject != null &&
					(typeof(Component).IsAssignableFrom(type) == true ||
					 type.IsInterface == true))
				{
					Component	subComponent = gameObject.GetComponent(type);

					if (subComponent != null)
						return true;
				}

				Component	component = assets[i] as Component;

				if (component != null)
				{
					if (type == typeof(GameObject))
						return true;

					gameObject = component.gameObject;

					if (gameObject != null &&
						(typeof(Component).IsAssignableFrom(type) == true ||
						 type.IsInterface == true))
					{
						Component	subComponent = gameObject.GetComponent(type);

						if (subComponent != null)
							return true;
					}
				}
			}

			return false;
		}

		private void	ExtractAsset(Rect r, Type type, Object asset, SerializedProperty property)
		{
			if (type.IsUnityArray() == true)
				type = Utility.GetArraySubType(type);

			if (type.IsAssignableFrom(asset.GetType()) == true)
			{
				property.objectReferenceValue = asset;
				return;
			}

			Object[]	assets;
			string		assetPath = AssetDatabase.GetAssetPath(asset);
			int			assignables = 0;

			if (string.IsNullOrEmpty(assetPath) == true)
				assets = new Object[] { asset };
			else
			{
				// In the case of a prefab, we need to enforce fetching assets of the focused GameObject.
				if (assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) == true)
				{
					assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

					List<Object>	relatedAssets = new List<Object>();
					GameObject		targetGameObject = asset as GameObject;

					if (targetGameObject == null)
						targetGameObject = (asset as Component).gameObject;

					for (int i = 0; i < assets.Length; i++)
					{
						Component	component = assets[i] as Component;

						if (component != null)
						{
							if (component.gameObject == targetGameObject)
								relatedAssets.Add(assets[i]);
						}
						else
						{
							GameObject	go = assets[i] as GameObject;

							if (go != null && go == targetGameObject)
								relatedAssets.Add(assets[i]);
						}
					}

					assets = relatedAssets.ToArray();
				}
				else
					assets = Utility.SafeLoadAllAssetsAtPath(assetPath);
			}

			for (int i = 0; i < assets.Length; i++)
			{
				if (type.IsAssignableFrom(assets[i].GetType()) == true)
					++assignables;
				else
				{
					GameObject	gameObject = assets[i] as GameObject;

					if (gameObject != null &&
						(typeof(Component).IsAssignableFrom(type) == true ||
						 type.IsInterface == true))
					{
						Component	subComponent = gameObject.GetComponent(type);

						if (subComponent != null)
						{
							this.TryDropDownComponents(r, type, property, assets[i] as GameObject, true, false);
							return;
						}
					}

					Component	component = assets[i] as Component;
					// When dropping component, always drop its GameObject, it gathers all cases (Both on GameObject and Component).
					if (component != null)
					{
						if (type == typeof(GameObject))
						{
							property.objectReferenceValue = component.gameObject;
							return;
						}
						else if (typeof(Component).IsAssignableFrom(type) == true ||
								 type.IsInterface == true)
						{
							this.TryDropDownComponents(r, type, property, component.gameObject, true, false);
							return;
						}
					}
				}
			}

			if (assignables == 1)
			{
				for (int i = 0; i < assets.Length; i++)
				{
					if (type.IsAssignableFrom(assets[i].GetType()) == true)
					{
						property.objectReferenceValue = assets[i];
						break;
					}
				}
			}
			else if (assignables >= 2)
				this.DropDownMultiComponents(r, null, assets, type, property, false);
		}

		private void	TryDropDownComponents(Rect r, Type type, SerializedProperty property, GameObject gameObject, bool fromDragAndDrop, bool onlyAsset)
		{
			Type	safeType = type;

			if (typeof(Component).IsAssignableFrom(safeType) == false)
				safeType = typeof(Component);

			Component[]	components = gameObject.GetComponents(safeType);

			if (fromDragAndDrop == true && components.Length == 1 && type != typeof(Object))
				property.objectReferenceValue = components[0];
			else
				this.DropDownMultiComponents(r, gameObject, type, property, onlyAsset);
		}

		private void	DefaultMenu(Rect r, SerializedProperty property)
		{
			PopupGenericMenu	menu = new PopupGenericMenu();

			this.PrependLastSelectionToMenu(r, menu, property);

			if (PrefabUtility.GetPrefabType(property.serializedObject.targetObject) != PrefabType.None)
				menu.Add("Revert Value to Prefab", false, false, this.RevertValueToPrefab, property);

			PopupWindow.Show(r, menu);
		}

		private void	DropDownMultiComponents(Rect r, GameObject gameObject, Type targetType, SerializedProperty property, bool onlyAsset)
		{
			this.DropDownMultiComponents(r, gameObject, gameObject.GetComponents<Component>(), targetType, property, onlyAsset);
		}

		private void	DropDownMultiComponents(Rect r, GameObject gameObject, Object[] values, Type targetType, SerializedProperty property, bool onlyAsset)
		{
			PopupGenericMenu	menu = new PopupGenericMenu();

			if (onlyAsset == false)
			{
				this.PrependLastSelectionToMenu(r, menu, property);

				if (PrefabUtility.GetPrefabType(property.serializedObject.targetObject) != PrefabType.None)
					menu.Add("Revert Value to Prefab", false, false, this.RevertValueToPrefab, property);
			}

			if (gameObject != null)
			{
				Action<object>	cb = null;

				if (targetType.IsAssignableFrom(typeof(GameObject)) == true)
					cb = this.Set;

				menu.Add(gameObject.name, property.objectReferenceValue == gameObject, true, cb, new DataMenu() { property = property, target = gameObject });
			}

			int	offset = menu.Count();

			for (int i = 0; i < values.Length; i++)
			{
				if (values[i] == null)
					continue;

				Type	type = values[i].GetType();

				if (targetType.IsAssignableFrom(type) == true)
					menu.Add("#" + (i + 1).ToString() + " " + type.Name, property.objectReferenceValue == values[i], true, this.Set, new DataMenu() { property = property, target = values[i], position = r, offset = offset + i });
				else
					menu.AddDisable("#" + (i + 1).ToString() + " " + type.Name, property.objectReferenceValue == values[i], true, this.Set, new DataMenu() { property = property, target = values[i], position = r, offset = offset + i });
			}

			PopupWindow.Show(r, menu);
		}

		private void	PrependLastSelectionToMenu(Rect r, PopupGenericMenu menu, SerializedProperty property)
		{
			for (int i = 0; i < DragObjectDrawer.lastSelected.Length; i++)
			{
				if (DragObjectDrawer.lastSelected[i] != null)
					menu.Add(DragObjectDrawer.LastSelectedPath + "#" + (i + 1).ToString() + " " + DragObjectDrawer.lastSelected[i].name, false, true, this.DropObject, new DataMenu() { property = property, target = DragObjectDrawer.lastSelected[i], position = r, offset = i });
			}
			//menu.AddSeparator(string.Empty);
		}

		private void	RevertValueToPrefab(object o)
		{
			SerializedProperty	p = o as SerializedProperty;

			p.prefabOverride = false;
			p.serializedObject.ApplyModifiedProperties();
		}

		private void	DropObject(object o)
		{
			DataMenu	data = o as DataMenu;

			data.position.x = 18F;
			data.position.y = data.offset * 18F;

			GameObject	gameObject = data.target as GameObject;

			if (gameObject == null)
			{
				Component c = data.target as Component;

				if (c != null)
					gameObject = c.gameObject;
			}

			if (gameObject != null)
			{
				if (typeof(GameObject).IsAssignableFrom(this.fieldInfo.FieldType) == true)
				{
					data.property.objectReferenceValue = gameObject;
					data.property.serializedObject.ApplyModifiedProperties();
				}
				else
				{
					Type	safeType = this.fieldInfo.FieldType;

					if (typeof(Component).IsAssignableFrom(safeType) == false)
						safeType = typeof(Component);

					Component[]	components = gameObject.GetComponents(safeType);

					if (components.Length == 1 && this.fieldInfo.FieldType != typeof(Object))
					{
						data.property.objectReferenceValue = components[0];
						data.property.serializedObject.ApplyModifiedProperties();
					}
					else
						this.DropDownMultiComponents(data.position, gameObject, this.fieldInfo.FieldType, data.property, true);
				}
			}
		}

		private void	Set(object _data)
		{
			DataMenu	data = _data as DataMenu;

			DragObjectDrawer.methodUpdateIfRequiredOrScript.Invoke(data.property.serializedObject, null); // #UNITY_MULTI_VERSION

			data.property.objectReferenceValue = data.target;
			data.property.serializedObject.ApplyModifiedProperties();
		}

		private static void	InitializeInitialDictionaries()
		{
			// Force CustomEditorAttributes to rebuild at least once its cached.
			if (DragObjectDrawer.s_DrawerTypeForType != null &&
				DragObjectDrawer.BuildDrawerTypeForTypeDictionary != null)
			{
				DragObjectDrawer.BuildDrawerTypeForTypeDictionary.Invoke(null, null);
				DragObjectDrawer.initialDictionary = DragObjectDrawer.s_DrawerTypeForType.GetValue(null);
			}
		}

		private static void	InitializeDragDictionaries()
		{
			// Force CustomEditorAttributes to rebuild at least once its cached.
			if (DragObjectDrawer.s_DrawerTypeForType != null &&
				DragObjectDrawer.BuildDrawerTypeForTypeDictionary != null)
			{
				DragObjectDrawer.s_DrawerTypeForType.SetValue(null, null);
				DragObjectDrawer.BuildDrawerTypeForTypeDictionary.Invoke(null, null);
				DragObjectDrawer.overridenDictionary = DragObjectDrawer.s_DrawerTypeForType.GetValue(null) as IDictionary;

				if (DragObjectDrawer.overridenDictionary.Contains(typeof(Object)) == true)
				{
					object	value = DragObjectDrawer.overridenDictionary[typeof(Object)];
					DragObjectDrawer.drawer.SetValue(value, typeof(DragObjectDrawer));
					DragObjectDrawer.type.SetValue(value, typeof(Object));
					DragObjectDrawer.overridenDictionary[typeof(Object)] = value;
				}
				else
				{
					object	value = Activator.CreateInstance(DragObjectDrawer.DrawerKeySet);
					DragObjectDrawer.drawer.SetValue(value, typeof(DragObjectDrawer));
					DragObjectDrawer.type.SetValue(value, typeof(Object));
					DragObjectDrawer.overridenDictionary.Add(typeof(Object), value);
				}

				foreach (Type type in Utility.EachAllSubClassesOf(typeof(Object)))
				{
					if (DragObjectDrawer.overridenDictionary.Contains(type) == true)
					{
						object	value = DragObjectDrawer.overridenDictionary[type];
						DragObjectDrawer.drawer.SetValue(value, typeof(DragObjectDrawer));
						DragObjectDrawer.type.SetValue(value, typeof(Object));
						DragObjectDrawer.overridenDictionary[type] = value;
					}
					else
					{
						object	value = Activator.CreateInstance(DragObjectDrawer.DrawerKeySet);
						DragObjectDrawer.drawer.SetValue(value, typeof(DragObjectDrawer));
						DragObjectDrawer.type.SetValue(value, typeof(Object));
						DragObjectDrawer.overridenDictionary.Add(type, value);
					}
				}
			}
		}
	}
}