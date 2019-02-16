using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;

namespace NGToolsEditor.NGHierarchyEnhancer
{
	using UnityEngine;

	[InitializeOnLoad]
	internal static class NGHierarchyEnhancer
	{
		private const string	Title = "NG Hierarchy Enhancer";
		private const string	HierarchyMethodName = "OnHierarchyGUI";
		private const float		width = 120F;

		private static Type			hierarchyType = null;
		private static EditorWindow	instance = null;

		private static int	lastInstanceId = 0;
		private static bool	menuOpen = false;
		private static bool	holding = false;
		private static bool	selectionHolding = false;

		private static DynamicObjectMenu[]	objectMenus;

		private static int							lastInstanceID;
		private static Object						lastObject;
		private static Behaviour[]					lastBehaviours;
		private static Rect							indentRect = new Rect(0f, 0f, 16F, 16F);
		private static Dictionary<string, float>	cacheXOffset = new Dictionary<string, float>();
		private static List<Component>				cacheComponents = new List<Component>();

		private static List<HierarchyEnhancerSettings.ComponentColor>	colors = new List<HierarchyEnhancerSettings.ComponentColor>();
		private static ReorderableList									reorder;
		private static int												pickTypeIndex = 0;

		private static string[]	eventModifierNames;

		private static List<HierarchyEnhancerSettings.ComponentColor>	unityComponentData = new List<HierarchyEnhancerSettings.ComponentColor>();

		static	NGHierarchyEnhancer()
		{
			HQ.SettingsChanged += NGHierarchyEnhancer.Preferences_SettingsChanged;

			NGHierarchyEnhancer.hierarchyType = UnityAssemblyVerifier.TryGetType(typeof(EditorWindow).Assembly, "UnityEditor.SceneHierarchyWindow");

			string[]	names = Enum.GetNames(typeof(EventModifiers));

			NGHierarchyEnhancer.eventModifierNames = new string[names.Length - 1];
			for (int i = 1; i < names.Length; i++)
				NGHierarchyEnhancer.eventModifierNames[i - 1] = names[i];
		}

		private static void	Preferences_SettingsChanged()
		{
			EditorApplication.hierarchyWindowItemOnGUI -= NGHierarchyEnhancer.DrawOverlay;

			if (HQ.Settings != null)
			{
				HierarchyEnhancerSettings	settings = HQ.Settings.Get<HierarchyEnhancerSettings>();
				settings.InitializeLayers();
				if (settings.enable == true)
					EditorApplication.hierarchyWindowItemOnGUI += NGHierarchyEnhancer.DrawOverlay;
			}
		}

		private static void	DrawOverlay(int instanceID, Rect selectionRect)
		{
			HierarchyEnhancerSettings	settings = HQ.Settings.Get<HierarchyEnhancerSettings>();

			if ((NGHierarchyEnhancer.instance == null ||
				 // When an Object is destroyed, it returns null but is not null...
				 NGHierarchyEnhancer.instance.Equals(null) == true) &&
				NGHierarchyEnhancer.hierarchyType != null)
			{
				Object[]	consoles = Resources.FindObjectsOfTypeAll(NGHierarchyEnhancer.hierarchyType);

				if (consoles.Length > 0)
				{
					NGHierarchyEnhancer.instance = consoles[0] as EditorWindow;
					NGHierarchyEnhancer.instance.wantsMouseMove = true;
				}
			}

			if (EditorWindow.mouseOverWindow == NGHierarchyEnhancer.instance)
			{
				// HACK Need to shift by one.
				// Ref Bug #720211_8cg6m8s7akdbf1r5
				if (settings.holdModifiers > 0)
					NGHierarchyEnhancer.holding = ((int)Event.current.modifiers & ((int)settings.holdModifiers)) == ((int)settings.holdModifiers);
				if (settings.selectionHoldModifiers > 0)
					NGHierarchyEnhancer.selectionHolding = ((int)Event.current.modifiers & ((int)settings.selectionHoldModifiers)) == ((int)settings.selectionHoldModifiers);
			}

			selectionRect.width += selectionRect.x;
			selectionRect.x = 0F;

			Object	obj;

			if (instanceID == NGHierarchyEnhancer.lastInstanceID)
				obj = NGHierarchyEnhancer.lastObject;
			else
			{
				obj = EditorUtility.InstanceIDToObject(instanceID);
				NGHierarchyEnhancer.lastInstanceID = instanceID;
				NGHierarchyEnhancer.lastObject = obj;
				NGHierarchyEnhancer.lastBehaviours = null;
			}

			if (obj != null)
			{
				GameObject	go = obj as GameObject;

				if (settings.layers != null &&
					settings.layers.Length > go.layer &&
					settings.layers[go.layer].a > 0F)
				{
					EditorGUI.DrawRect(selectionRect, settings.layers[go.layer]);
				}

				if (settings.layersIcon != null &&
					settings.layersIcon.Length > go.layer &&
					settings.layersIcon[go.layer] != null)
				{
					NGHierarchyEnhancer.ProcessIndentLevel(selectionRect.y, go);
					GUI.DrawTexture(NGHierarchyEnhancer.indentRect, settings.layersIcon[go.layer], ScaleMode.ScaleToFit);
				}

				// Draw Component' color over layer's background color.
				go.GetComponents<Component>(cacheComponents);

				Rect	r = selectionRect;

				if (settings.widthPerComponent > 0F)
				{
					NGHierarchyEnhancer.ProcessIndentLevel(selectionRect.y, go);
					r = NGHierarchyEnhancer.indentRect;

					for (int i = 1; i < cacheComponents.Count; i++) // Skip Transform.
					{
						if (cacheComponents[i] == null)
							continue;

						bool	drawn = false;
						Type	t = cacheComponents[i].GetType();

						if (settings.drawUnityComponents == true)
						{
							int	k = 0;

							r.width = 16F;

							for (; k < NGHierarchyEnhancer.unityComponentData.Count; k++)
							{
								if (t == NGHierarchyEnhancer.unityComponentData[k].type)
								{
									if (NGHierarchyEnhancer.unityComponentData[k].icon != null)
									{
										GUI.DrawTexture(r, NGHierarchyEnhancer.unityComponentData[k].icon);
										r.x += r.width;
										drawn = true;
									}
									break;
								}
							}

							if (k < NGHierarchyEnhancer.unityComponentData.Count)
							{
								if (drawn == true)
									continue;
							}
							else if (t.Assembly != typeof(Editor).Assembly)
								NGHierarchyEnhancer.unityComponentData.Add(new HierarchyEnhancerSettings.ComponentColor() { type = t, icon = EditorGUIUtility.ObjectContent(null, t).image });
						}

						for (int j = 0; j < settings.componentData.Length; j++)
						{
							if (settings.componentData[j].type == null)
								continue;

							if (t == settings.componentData[j].type)
							{
								if (settings.componentData[j].icon != null)
								{
									r.width = 16F;
									GUI.DrawTexture(r, settings.componentData[j].icon);
								}
								else
								{
									r.width = settings.widthPerComponent;
									EditorGUI.DrawRect(r, settings.componentData[j].color);
								}

								r.x += r.width;
								break;
							}
						}
					}
				}
				else
				{
					for (int j = 0; j < settings.componentData.Length; j++)
					{
						if (settings.componentData[j].type == null)
							continue;

						int	i = 0;

						for (; i < cacheComponents.Count; i++)
						{
							if (cacheComponents[i].GetType() == settings.componentData[j].type)
							{
								EditorGUI.DrawRect(r, settings.componentData[j].color);
								break;
							}
						}

						if (i < cacheComponents.Count)
							break;
					}
				}
			}

			if (NGHierarchyEnhancer.IsInSelection(obj) ||
				(selectionRect.Contains(Event.current.mousePosition) == true && NGHierarchyEnhancer.holding == false) ||
				(NGHierarchyEnhancer.holding == true && NGHierarchyEnhancer.lastInstanceId == instanceID))
			{
				if (NGHierarchyEnhancer.lastInstanceId != instanceID &&
					NGHierarchyEnhancer.holding == false)
				{
					NGHierarchyEnhancer.lastInstanceId = instanceID;
					NGHierarchyEnhancer.menuOpen = false;
				}

				if (Event.current.type == EventType.MouseMove)
					NGHierarchyEnhancer.instance.Repaint();

				if (obj != null)
				{
					float	x = selectionRect.x;
					float	width = selectionRect.width;

					selectionRect.x += selectionRect.width - 30F - settings.margin;
					selectionRect.width = 30F;

					if ((NGHierarchyEnhancer.selectionHolding == true && NGHierarchyEnhancer.IsInSelection(obj)) ||
						NGHierarchyEnhancer.holding == true ||
						selectionRect.Contains(Event.current.mousePosition) == true)
					{
						if (NGHierarchyEnhancer.menuOpen == false)
						{
							NGHierarchyEnhancer.menuOpen = true;
							NGHierarchyEnhancer.instance.Repaint();
						}
					}

					if (NGHierarchyEnhancer.menuOpen == false)
						GUI.Button(selectionRect, "NG");
					else
					{
						selectionRect.x = 0F;
						selectionRect.width = width + x - settings.margin;

						EditorGUI.BeginChangeCheck();

						// Draws DynamicObjectMenu first.

						if (NGHierarchyEnhancer.objectMenus == null)
						{
							List<DynamicObjectMenu>	menus = new List<DynamicObjectMenu>();

							foreach (Type c in Utility.EachNGTSubClassesOf(typeof(DynamicObjectMenu)))
								menus.Add(Activator.CreateInstance(c) as DynamicObjectMenu);

							menus.Sort((a, b) => a.priority - b.priority);
							NGHierarchyEnhancer.objectMenus = menus.ToArray();
						}

						for (int i = 0; i < NGHierarchyEnhancer.objectMenus.Length; i++)
						{
							// Shrink available width with new end point on X axis.
							selectionRect.width = NGHierarchyEnhancer.objectMenus[i].DrawHierarchy(selectionRect, obj) - selectionRect.x;
						}

						if (EditorGUI.EndChangeCheck() == true)
						{
							Metrics.UseTool(12); // NGHierarchyEnhancer
							NGChangeLogWindow.CheckLatestVersion(NGAssemblyInfo.Name);
						}

						// Then all sub-implementations.
						GameObject	gameObject = obj as GameObject;

						if (gameObject != null)
						{
							Behaviour[]	behaviours;

							if (NGHierarchyEnhancer.lastBehaviours == null)
							{
								behaviours = gameObject.GetComponents<Behaviour>();
								NGHierarchyEnhancer.lastBehaviours = behaviours;
							}
							else
								behaviours = NGHierarchyEnhancer.lastBehaviours;

							for (int i = 0; i < behaviours.Length; i++)
							{
								if (behaviours[i] == null)
									continue;

								INGHierarchyEnhancerGUI	drawer = behaviours[i] as INGHierarchyEnhancerGUI;

								if (drawer != null)
									selectionRect.width = drawer.OnHierarchyGUI(selectionRect) - selectionRect.x;
							}
						}
					}
				}
			}
		}

		private static void	ProcessIndentLevel(float y, GameObject go)
		{
			NGHierarchyEnhancer.indentRect.y = y;

			int			indentLevel = 0;
			Transform	t = go.transform;

			for (int i = 0; t != null; i++)
			{
				t = t.parent;
				++indentLevel;
			}

			float	offset;
			if (NGHierarchyEnhancer.cacheXOffset.TryGetValue(go.name, out offset) == false)
			{
				Utility.content.text = go.name;
				offset = GUI.skin.label.CalcSize(Utility.content).x;
			}

			string	unityVersion = Utility.UnityVersion;

			if (unityVersion[0] == '4' ||
				(unityVersion[0] == '5' &&
				 (unityVersion[2] == '0' || unityVersion[2] == '1' || unityVersion[2] == '2' ||
				  (unityVersion[2] == '3' && unityVersion[4] <= '7'))))
			{
				NGHierarchyEnhancer.indentRect.x = offset + indentLevel * 14F;
			}
			else
				NGHierarchyEnhancer.indentRect.x = 16F + offset + indentLevel * 14F;
		}

		private static bool	IsInSelection(Object obj)
		{
			if (NGHierarchyEnhancer.selectionHolding == true && obj != null)
			{
				for (int i = 0; i < Selection.objects.Length; i++)
				{
					if (Selection.objects[i] == obj)
						return true;
				}
			}

			return false;
		}

		[NGSettings(NGHierarchyEnhancer.Title)]
		private static void	OnGUISettings()
		{
			if (HQ.Settings == null)
				return;

			HierarchyEnhancerSettings	settings = HQ.Settings.Get<HierarchyEnhancerSettings>();

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.Space();
			using (BgColorContentRestorer.Get(settings.enable == true ? Color.green : Color.red))
			{
				EditorGUILayout.BeginVertical("ButtonLeft");
				{
					EditorGUILayout.BeginHorizontal();
					{
						settings.enable = NGEditorGUILayout.Switch(LC.G("Enable"), settings.enable);
					}
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.LabelField(LC.G("NGHierarchyEnhancer_EnableDescription"), GeneralStyles.WrapLabel);
				}
				EditorGUILayout.EndVertical();
			}

			if (EditorGUI.EndChangeCheck() == true)
			{
				if (settings.enable == false)
					EditorApplication.hierarchyWindowItemOnGUI -= NGHierarchyEnhancer.DrawOverlay;
				else
					EditorApplication.hierarchyWindowItemOnGUI += NGHierarchyEnhancer.DrawOverlay;
				HQ.InvalidateSettings();
			}

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField(LC.G("NGHierarchyEnhancer_MarginDescription"), GeneralStyles.WrapLabel);
			settings.margin = EditorGUILayout.FloatField(LC.G("NGHierarchyEnhancer_Margin"), settings.margin);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField(LC.G("NGHierarchyEnhancer_HoldModifiersDescription"), GeneralStyles.WrapLabel);
			settings.holdModifiers = (EventModifiers)EditorGUILayout.MaskField(new GUIContent(LC.G("NGHierarchyEnhancer_HoldModifiers")), (int)settings.holdModifiers, NGHierarchyEnhancer.eventModifierNames);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField(LC.G("NGHierarchyEnhancer_SelectionHoldModifiersDescription"), GeneralStyles.WrapLabel);
			settings.selectionHoldModifiers = (EventModifiers)EditorGUILayout.MaskField(new GUIContent(LC.G("NGHierarchyEnhancer_SelectionHoldModifiers")), (int)settings.selectionHoldModifiers, NGHierarchyEnhancer.eventModifierNames);
			if (EditorGUI.EndChangeCheck() == true)
				HQ.InvalidateSettings();

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField(LC.G("NGHierarchyEnhancer_LayersDescription"), GeneralStyles.WrapLabel);

			float	maxLabelWidth = NGHierarchyEnhancer.width;

			for (int i = 0; i < HierarchyEnhancerSettings.TotalLayers; i++)
			{
				string	layerName = LayerMask.LayerToName(i);

				if (layerName == string.Empty)
					layerName = "Layer " + i;

				Utility.content.text = layerName;
				float	width = GUI.skin.label.CalcSize(Utility.content).x;
				if (maxLabelWidth < width + 20F) // Add width for the icon.
					maxLabelWidth = width + 20F;
			}

			using (LabelWidthRestorer.Get(maxLabelWidth))
			{
				for (int i = 0; i < HierarchyEnhancerSettings.TotalLayers; i++)
				{
					string	layerName = LayerMask.LayerToName(i);

					if (layerName == string.Empty)
						layerName = "Layer " + i;

					EditorGUILayout.BeginHorizontal();

					// (Label + icon) + color picker
					Rect	r = GUILayoutUtility.GetRect(maxLabelWidth + 40F, 16F, GUI.skin.label);

					Utility.content.text = layerName;
					float	width = GUI.skin.label.CalcSize(Utility.content).x;

					settings.layers[i] = EditorGUI.ColorField(r, layerName, settings.layers[i]);
					r.width = maxLabelWidth;
					EditorGUI.DrawRect(r, settings.layers[i]);

					if (settings.layersIcon[i] != null)
					{
						r.x += width + 2F; // Little space before the icon.
						r.width = 16F;
						GUI.DrawTexture(r, settings.layersIcon[i], ScaleMode.ScaleToFit);
					}

					settings.layersIcon[i] = EditorGUILayout.ObjectField(settings.layersIcon[i], typeof(Texture2D), false) as Texture2D;
					EditorGUILayout.EndHorizontal();
				}
			}

			if (EditorGUI.EndChangeCheck() == true)
			{
				HQ.InvalidateSettings();
				if (NGHierarchyEnhancer.instance != null)
					NGHierarchyEnhancer.instance.Repaint();
			}

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.LabelField(LC.G("NGHierarchyEnhancer_WidthPerComponentDescription"), GeneralStyles.WrapLabel);
			settings.widthPerComponent = EditorGUILayout.FloatField(LC.G("NGHierarchyEnhancer_WidthPerComponent"), settings.widthPerComponent);
			if (EditorGUI.EndChangeCheck() == true)
			{
				if (settings.widthPerComponent < -1F)
					settings.widthPerComponent = -1F;

				HQ.InvalidateSettings();
				if (NGHierarchyEnhancer.instance != null)
					NGHierarchyEnhancer.instance.Repaint();
			}

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField(LC.G("NGHierarchyEnhancer_DrawUnityComponentsDescription"), GeneralStyles.WrapLabel);
			settings.drawUnityComponents = EditorGUILayout.Toggle(LC.G("NGHierarchyEnhancer_DrawUnityComponents"), settings.drawUnityComponents);
			if (EditorGUI.EndChangeCheck() == true)
			{
				HQ.InvalidateSettings();
				if (NGHierarchyEnhancer.instance != null)
					NGHierarchyEnhancer.instance.Repaint();
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField(LC.G("NGHierarchyEnhancer_ComponentColorsDescription"), GeneralStyles.WrapLabel);

			if (reorder == null)
			{
				NGHierarchyEnhancer.colors = new List<HierarchyEnhancerSettings.ComponentColor>(settings.componentData);
				NGHierarchyEnhancer.reorder = new ReorderableList(NGHierarchyEnhancer.colors, typeof(HierarchyEnhancerSettings.ComponentColor), true, false, true, true);
				NGHierarchyEnhancer.reorder.headerHeight = 0F;
				NGHierarchyEnhancer.reorder.drawElementCallback += NGHierarchyEnhancer.DrawComponentType;
				NGHierarchyEnhancer.reorder.onReorderCallback += (r) => NGHierarchyEnhancer.SerializeComponentColors();
				NGHierarchyEnhancer.reorder.onRemoveCallback += (r) => {
					r.list.RemoveAt(r.index);
					NGHierarchyEnhancer.SerializeComponentColors();
				};
				NGHierarchyEnhancer.reorder.onAddCallback += (r) => {
					colors.Add(new HierarchyEnhancerSettings.ComponentColor());
					NGHierarchyEnhancer.SerializeComponentColors();
				};
			}

			reorder.DoLayoutList();
		}

		private static void	DrawComponentType(Rect rect, int index, bool isActive, bool isFocused)
		{
			float	w = rect.width;

			rect.width = 60F;
			if (GUI.Button(rect, "Type") == true)
			{
				pickTypeIndex = index;
				GenericTypesSelectorWizard.Start("Pick Type", typeof(Component), OnCreate, true, true);
			}
			rect.x += rect.width;
			rect.width = w - 180F;

			GUI.Label(rect, NGHierarchyEnhancer.colors[index].type == null ? "None" : NGHierarchyEnhancer.colors[index].type.Name, GeneralStyles.VerticalCenterLabel);
			rect.x += rect.width;

			rect.width = 60F;
			EditorGUI.BeginChangeCheck();
			NGHierarchyEnhancer.colors[pickTypeIndex].color = EditorGUI.ColorField(rect, NGHierarchyEnhancer.colors[pickTypeIndex].color);
			if (EditorGUI.EndChangeCheck() == true)
				NGHierarchyEnhancer.SerializeComponentColors();

			rect.x += rect.width;
			EditorGUI.BeginChangeCheck();
			NGHierarchyEnhancer.colors[pickTypeIndex].icon = EditorGUI.ObjectField(rect, NGHierarchyEnhancer.colors[pickTypeIndex].icon, typeof(Texture2D), false) as Texture2D;
			if (EditorGUI.EndChangeCheck() == true)
				NGHierarchyEnhancer.SerializeComponentColors();
		}

		private static void	OnCreate(Type type)
		{
			NGHierarchyEnhancer.colors[pickTypeIndex].type = type;
			NGHierarchyEnhancer.SerializeComponentColors();
		}

		private static void	SerializeComponentColors()
		{
			HierarchyEnhancerSettings	settings = HQ.Settings.Get<HierarchyEnhancerSettings>();

			settings.componentData = NGHierarchyEnhancer.colors.ToArray();

			if (NGHierarchyEnhancer.instance != null)
				NGHierarchyEnhancer.instance.Repaint();
		}
	}
}