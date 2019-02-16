using NGTools;
using NGTools.Network;
using NGTools.NGRemoteScene;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	public sealed class ClientMaterial
	{
		public const float	MaterialTitleHeight = 16F;
		public const float	ShaderTitleHeight = 16F;
		public const float	ShaderPropertiesNotAvailableMargin = 4F;
		public const float	ShaderPropertiesNotAvailableHeight = 32F;

		private static TypeHandler	floatHandler;
		private static TypeHandler	colorHandler;
		private static TypeHandler	vector4Handler;

		public readonly int		instanceID;
		public readonly string	name;

		public string					shader;
		public NetMaterialProperty[]	properties;

		private bool	open = true;
		private int		originalShader = -1;
		private int		selectedShader = -1;

		static	ClientMaterial()
		{
			ClientMaterial.floatHandler = TypeHandlersManager.GetTypeHandler<float>();
			ClientMaterial.colorHandler = TypeHandlersManager.GetTypeHandler<Color>();
			ClientMaterial.vector4Handler = TypeHandlersManager.GetTypeHandler<Vector4>();
		}

		public static float	GetHeight(NGRemoteHierarchyWindow hierarchy, int instanceID)
		{
			float			height = ClientMaterial.MaterialTitleHeight + ClientMaterial.ShaderTitleHeight + 2F; // Material title + Shader header + Top/Bottom line separator.
			ClientMaterial	material = hierarchy.GetMaterial(instanceID);

			if (material != null)
			{
				if (material.properties == null)
					height += ClientMaterial.ShaderPropertiesNotAvailableMargin + ClientMaterial.ShaderPropertiesNotAvailableHeight + ClientMaterial.ShaderPropertiesNotAvailableMargin;
				else if (material.open == true)
				{
					int	n = 0;

					for (int j = 0; j < material.properties.Length; j++)
					{
						if (material.properties[j].hidden == true)
							continue;

						if (material.properties[j].type == NGShader.ShaderPropertyType.TexEnv)
							n += 3;
						else
							++n;
					}

					height += n * (Constants.SingleLineHeight + ClientComponent.Spacing); // Properties + Title spacing.
				}
			}

			return height;
		}

		public static void	Draw(Rect r, NGRemoteHierarchyWindow hierarchy, int instanceID)
		{
			ClientMaterial	material = hierarchy.GetMaterial(instanceID);

			r.height = 1F;
			r.y += 1F;
			EditorGUI.DrawRect(r, Color.black);
			r.y -= 1F;
			r.height = ClientMaterial.MaterialTitleHeight;

			r.y += ClientComponent.Spacing;

			if (Event.current.type == EventType.Repaint)
			{
				r.height += r.height;
				EditorGUI.DrawRect(r, NGRemoteInspectorWindow.MaterialHeaderBackgroundColor);
				r.height = ClientMaterial.MaterialTitleHeight;
			}

			if (material != null)
			{
				string[]	shaderNames;
				int[]		shaderInstanceIDs;
				float		width = r.width;
				Utility.content.text = LC.G("Change");
				float		changeWidth = GUI.skin.button.CalcSize(Utility.content).x;

				hierarchy.GetResources(typeof(Shader), out shaderNames, out shaderInstanceIDs);

				Utility.content.text = "Material";
				Utility.content.image = UtilityResources.MaterialIcon;
				material.open = EditorGUI.Foldout(r, material.open, Utility.content, true);
				Utility.content.image = null;

				r.xMin += 85F;
				GUI.Label(r, material.name, GeneralStyles.ComponentName);
				r.xMin -= 85F;
				r.y += r.height;

				++EditorGUI.indentLevel;

				r.height = ClientMaterial.ShaderTitleHeight;

				if (shaderNames != null)
				{
					if (material.selectedShader == -1)
					{
						for (int j = 0; j < shaderNames.Length; j++)
						{
							if (shaderNames[j].Equals(material.shader) == true)
							{
								material.originalShader = j;
								material.selectedShader = j;
								break;
							}
						}
					}

					r.width -= changeWidth;
					r.xMin += 11F;
					using (LabelWidthRestorer.Get(75F))
						material.selectedShader = EditorGUI.Popup(r, "Shader", material.selectedShader, shaderNames);
					r.xMin -= 11F;

					float	w = r.width;
					r.width = 16F;
					r.x += 13F;
					GUI.DrawTexture(r, UtilityResources.ShaderIcon);
					r.width = w;
					r.x += r.width - 13F;

					r.width = changeWidth;
					EditorGUI.BeginDisabledGroup(material.originalShader == material.selectedShader);
					if (GUI.Button(r, LC.G("Change")) == true)
					{
						hierarchy.AddPacket(new ClientChangeMaterialShaderPacket(instanceID, shaderInstanceIDs[material.selectedShader]), p =>
						{
							if (p.CheckPacketStatus() == true)
								material.Reset((p as ServerSendMaterialDataPacket).netMaterial);
						});
					}
					EditorGUI.EndDisabledGroup();

					r.x = 0F;
					r.width = width;
				}
				else
				{
					EditorGUI.LabelField(r, "Shader", LC.G("NGInspector_NotAvailableYet"));

					if (hierarchy.IsChannelBlocked(typeof(Shader).GetHashCode()) == true)
						GUILayout.Label(GeneralStyles.StatusWheel, GUILayoutOptionPool.Width(20F));
				}

				r.y += r.height;

				r.height = 1F;
				r.y += 1F;
				EditorGUI.DrawRect(r, Color.gray);
				r.y += 1F;

				if (material.properties == null)
				{
					r.y += ClientMaterial.ShaderPropertiesNotAvailableMargin;
					r.height = ClientMaterial.ShaderPropertiesNotAvailableHeight;
					r.xMin += 5F;
					r.xMax -= 5F;
					EditorGUI.HelpBox(r, "Shader properties are not available. (NG Server Scene requires to scan & save shaders!)", MessageType.Info);
					--EditorGUI.indentLevel;
					return;
				}

				if (material.open == false)
				{
					--EditorGUI.indentLevel;
					return;
				}

				r.height = Constants.SingleLineHeight;

				for (int j = 0; j < material.properties.Length; j++)
				{
					NetMaterialProperty	properties = material.properties[j];

					if (properties.hidden == true)
						continue;

					if (properties.type == NGShader.ShaderPropertyType.Color)
					{
						EditorGUI.BeginChangeCheck();
						Color	newValue = EditorGUI.ColorField(r, properties.displayName, properties.colorValue);
						if (EditorGUI.EndChangeCheck() == true)
						{
							hierarchy.AddPacket(new ClientUpdateMaterialPropertyPacket(instanceID, properties.name, ClientMaterial.colorHandler.Serialize(newValue)), p =>
							{
								if (p.CheckPacketStatus() == true)
								{
									ByteBuffer	buffer = Utility.GetBBuffer((p as ServerUpdateMaterialPropertyPacket).rawValue);

									properties.colorValue = (Color)ClientMaterial.colorHandler.Deserialize(buffer, typeof(Color));
									Utility.RestoreBBuffer(buffer);
								}
							});
						}
					}
					else if (properties.type == NGShader.ShaderPropertyType.Float)
					{
						EditorGUI.BeginChangeCheck();
						float	newValue = EditorGUI.FloatField(r, properties.displayName, properties.floatValue);
						if (EditorGUI.EndChangeCheck() == true)
						{
							hierarchy.AddPacket(new ClientUpdateMaterialPropertyPacket(instanceID, properties.name, ClientMaterial.floatHandler.Serialize(newValue)), p =>
							{
								if (p.CheckPacketStatus() == true)
								{
									ByteBuffer	buffer = Utility.GetBBuffer((p as ServerUpdateMaterialPropertyPacket).rawValue);

									properties.floatValue = (float)ClientMaterial.floatHandler.Deserialize(buffer, typeof(float));
									Utility.RestoreBBuffer(buffer);
								}
							});
						}
					}
					else if (properties.type == NGShader.ShaderPropertyType.Range)
					{
						EditorGUI.BeginChangeCheck();
						float	newValue = EditorGUI.Slider(r, properties.displayName, properties.floatValue, properties.rangeMin, properties.rangeMax);
						if (EditorGUI.EndChangeCheck() == true)
						{
							hierarchy.AddPacket(new ClientUpdateMaterialPropertyPacket(instanceID, properties.name, ClientMaterial.floatHandler.Serialize(newValue)), p =>
							{
								if (p.CheckPacketStatus() == true)
								{
									ByteBuffer	buffer = Utility.GetBBuffer((p as ServerUpdateMaterialPropertyPacket).rawValue);

									properties.floatValue = (float)ClientMaterial.floatHandler.Deserialize(buffer, typeof(float));
									Utility.RestoreBBuffer(buffer);
								}
							});
						}
					}
					else if (properties.type == NGShader.ShaderPropertyType.TexEnv)
					{
						UnityObject	unityObject = properties.textureValue;
						int			controlID = GUIUtility.GetControlID("NGObjectFieldHash".GetHashCode(), FocusType.Keyboard, r);
						float		x = r.x;

						r.width = UnityObjectDrawer.PickerButtonWidth;
						r.x = width - UnityObjectDrawer.PickerButtonWidth;

						if (Event.current.type == EventType.KeyDown &&
							Event.current.keyCode == KeyCode.Delete &&
							GUIUtility.keyboardControl == controlID)
						{
							UnityObject nullObject = new UnityObject(unityObject.type, 0);

							hierarchy.AddPacket(new ClientUpdateMaterialPropertyPacket(instanceID, properties.name, TypeHandlersManager.GetTypeHandler<UnityObject>().Serialize(nullObject.type, nullObject)), p =>
							{
								if (p.CheckPacketStatus() == true)
								{
									ByteBuffer	buffer = Utility.GetBBuffer((p as ServerUpdateMaterialPropertyPacket).rawValue);
									UnityObject	newUnityObject = (UnityObject)TypeHandlersManager.GetTypeHandler<UnityObject>().Deserialize(buffer, typeof(UnityObject));

									unityObject.Assign(newUnityObject.type, newUnityObject.gameObjectInstanceID, newUnityObject.instanceID, newUnityObject.name);
									Utility.RestoreBBuffer(buffer);
								}
							});

							Event.current.Use();
						}

						if (Event.current.type == EventType.MouseDown &&
							r.Contains(Event.current.mousePosition) == true)
						{
							hierarchy.PickupResource(typeof(Texture), instanceID + '.' + properties.name, ClientMaterial.UpdateMaterialTexture, p =>
							{
								if (p.CheckPacketStatus() == true)
								{
									ByteBuffer	buffer = Utility.GetBBuffer((p as ServerUpdateMaterialPropertyPacket).rawValue);
									UnityObject	newUnityObject = (UnityObject)TypeHandlersManager.GetTypeHandler<UnityObject>().Deserialize(buffer, typeof(UnityObject));

									properties.textureValue.Assign(newUnityObject.type, newUnityObject.gameObjectInstanceID, newUnityObject.instanceID, newUnityObject.name);
									Utility.RestoreBBuffer(buffer);
								}
							},
							unityObject.instanceID);
							Event.current.Use();
						}

						r.width = width;
						r.x = x;

						Utility.content.text = properties.displayName;

						Rect	prefixRect = EditorGUI.PrefixLabel(r, Utility.content);

						if (unityObject.instanceID != 0)
							Utility.content.text = unityObject.name + " (" + unityObject.type.Name + ")";
						else
							Utility.content.text = "None (" + unityObject.type.Name + ")";

						if (GUI.Button(prefixRect, GUIContent.none, GUI.skin.label) == true)
							GUIUtility.keyboardControl = controlID;

						if (Event.current.type == EventType.Repaint)
							GeneralStyles.UnityObjectPicker.Draw(prefixRect, Utility.content, controlID);

						++EditorGUI.indentLevel;
						r.x = 0F;
						r.width = width;

						r.y += r.height + ClientComponent.Spacing;
						EditorGUI.BeginChangeCheck();
						Vector2	newValue = EditorGUI.Vector2Field(r, "Tiling", properties.textureScale);
						if (EditorGUI.EndChangeCheck() == true)
						{
							hierarchy.AddPacket(new ClientUpdateMaterialVector2Packet(instanceID, properties.name, newValue, MaterialVector2Type.Scale), p =>
							{
								if (p.CheckPacketStatus() == true)
									properties.textureScale = (p as ServerUpdateMaterialVector2Packet).value;
							});
						}

						r.y += r.height + ClientComponent.Spacing;
						EditorGUI.BeginChangeCheck();
						newValue = EditorGUI.Vector2Field(r, "Offset", properties.textureOffset);
						if (EditorGUI.EndChangeCheck() == true)
						{
							hierarchy.AddPacket(new ClientUpdateMaterialVector2Packet(instanceID, properties.name, newValue, MaterialVector2Type.Offset), p =>
							{
								if (p.CheckPacketStatus() == true)
									properties.textureOffset = (p as ServerUpdateMaterialVector2Packet).value;
							});
						}
						--EditorGUI.indentLevel;

						r.x = 0F;
						r.width = width;
					}
					else if (properties.type == NGShader.ShaderPropertyType.Vector)
						ClientMaterial.DrawVector4(r, hierarchy, instanceID, properties);

					r.y += r.height + ClientComponent.Spacing;
				}

				--EditorGUI.indentLevel;
			}
			else
			{
				if (hierarchy.IsChannelBlocked(instanceID) == true)
					GUI.Label(r, GeneralStyles.StatusWheel);

				r.xMin += 16F;
				r.height = ClientMaterial.MaterialTitleHeight;

				Utility.content.text = "Material";
				Utility.content.image = UtilityResources.MaterialIcon;
				EditorGUI.LabelField(r, Utility.content, new GUIContent(LC.G("NGInspector_NotAvailableYet")));
				Utility.content.image = null;
				r.y += r.height;

				r.height = ClientMaterial.ShaderTitleHeight;
				r.xMin += 16F;
				EditorGUI.LabelField(r, "Shader", LC.G("NGInspector_NotAvailableYet"));
				r.xMin -= 16F;

				r.width = 16F;
				GUI.DrawTexture(r, UtilityResources.ShaderIcon);
			}
		}

		private static void	DrawVector4(Rect r, NGRemoteHierarchyWindow hierarchy, int materialInstanceID, NetMaterialProperty property)
		{
			Vector4	vector = property.vectorValue;
			float	labelWidth;
			float	controlWidth;

			Utility.CalculSubFieldsWidth(r.width, 44F, 4, out labelWidth, out controlWidth);

			r.width = labelWidth;
			EditorGUI.LabelField(r, Utility.NicifyVariableName(property.name));
			r.x += r.width;

			using (IndentLevelRestorer.Get(0))
			using (LabelWidthRestorer.Get(14F))
			{
				r.width = controlWidth;

				EditorGUI.BeginChangeCheck();
				Single	v = EditorGUI.FloatField(r, "X", vector.x);
				r.x += r.width;
				if (EditorGUI.EndChangeCheck() == true)
				{
					vector.x = v;
					hierarchy.AddPacket(new ClientUpdateMaterialPropertyPacket(materialInstanceID, property.name, ClientMaterial.vector4Handler.Serialize(vector)), p =>
					{
						if (p.CheckPacketStatus() == true)
						{
							ByteBuffer	buffer = Utility.GetBBuffer((p as ServerUpdateMaterialPropertyPacket).rawValue);

							property.vectorValue = (Vector4)ClientMaterial.vector4Handler.Deserialize(buffer, typeof(Vector4));
							Utility.RestoreBBuffer(buffer);
						}
					});
				}

				EditorGUI.BeginChangeCheck();
				v = EditorGUI.FloatField(r, "Y", vector.y);
				r.x += r.width;
				if (EditorGUI.EndChangeCheck() == true)
				{
					vector.y = v;
					hierarchy.AddPacket(new ClientUpdateMaterialPropertyPacket(materialInstanceID, property.name, ClientMaterial.vector4Handler.Serialize(vector)), p =>
					{
						if (p.CheckPacketStatus() == true)
						{
							ByteBuffer	buffer = Utility.GetBBuffer((p as ServerUpdateMaterialPropertyPacket).rawValue);

							property.vectorValue = (Vector4)ClientMaterial.vector4Handler.Deserialize(buffer, typeof(Vector4));
							Utility.RestoreBBuffer(buffer);
						}
					});
				}

				EditorGUI.BeginChangeCheck();
				v = EditorGUI.FloatField(r, "Z", vector.z);
				r.x += r.width;
				if (EditorGUI.EndChangeCheck() == true)
				{
					vector.z = v;
					hierarchy.AddPacket(new ClientUpdateMaterialPropertyPacket(materialInstanceID, property.name, ClientMaterial.vector4Handler.Serialize(vector)), p =>
					{
						if (p.CheckPacketStatus() == true)
						{
							ByteBuffer	buffer = Utility.GetBBuffer((p as ServerUpdateMaterialPropertyPacket).rawValue);

							property.vectorValue = (Vector4)ClientMaterial.vector4Handler.Deserialize(buffer, typeof(Vector4));
							Utility.RestoreBBuffer(buffer);
						}
					});
				}

				EditorGUI.BeginChangeCheck();
				v = EditorGUI.FloatField(r, "W", vector.w);
				if (EditorGUI.EndChangeCheck() == true)
				{
					vector.w = v;
					hierarchy.AddPacket(new ClientUpdateMaterialPropertyPacket(materialInstanceID, property.name, ClientMaterial.vector4Handler.Serialize(vector)), p =>
					{
						if (p.CheckPacketStatus() == true)
						{
							ByteBuffer	buffer = Utility.GetBBuffer((p as ServerUpdateMaterialPropertyPacket).rawValue);

							property.vectorValue = (Vector4)ClientMaterial.vector4Handler.Deserialize(buffer, typeof(Vector4));
							Utility.RestoreBBuffer(buffer);
						}
					});
				}
			}
		}

		private static Packet	UpdateMaterialTexture(string valuePath, byte[] rawValue)
		{
			int		n = valuePath.IndexOf('.');
			int		instanceID = int.Parse(valuePath.Substring(0, n));
			string	propertyName = valuePath.Substring(n + 1);

			return new ClientUpdateMaterialPropertyPacket(instanceID, propertyName, rawValue);
		}

		public	ClientMaterial(int instanceID, string name)
		{
			this.instanceID = instanceID;
			this.name = name;
		}

		public	ClientMaterial(NetMaterial material)
		{
			this.instanceID = material.instanceID;
			this.name = material.name;
			this.shader = material.shader;
			this.properties = material.properties;
		}

		public void	Reset(NetMaterial material)
		{
			this.shader = material.shader;
			this.properties = material.properties;
			this.originalShader = -1;
			this.selectedShader = -1;
		}
	}
}