using NGTools;
using NGTools.Network;
using NGTools.NGRemoteScene;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using InnerUtility = NGTools.Utility;

namespace NGToolsEditor.NGRemoteScene
{
	using UnityEngine;

	public sealed class ClientComponent
	{
		public const float	Spacing = 3F;
		public const float	MemberSpacing = 2F;
		public const float	ComponentHeaderHeight = 16F;
		public const float	MethodsWidth = 170F;
		public const float	OpenMethodButtonWidth = 20F;
		public static Color	BackgroundColorBar = new Color(102F / 255F, 102F / 255F, 102F / 255F);

		private static List<ClientField>			reuseFields = new List<ClientField>(16);
		private static List<Component>				reuseComponents = new List<Component>(2);
		private static Dictionary<Type, Object[]>	cachedResources = new Dictionary<Type, Object[]>();

		public readonly ClientGameObject	parent;
		public readonly Type				type;
		public readonly int					instanceID;
		public readonly bool				togglable;
		public readonly bool				deletable;
		public readonly string				name;
		public readonly ClientField[]		fields;
		public readonly ClientMethod[]		methods;
		public readonly TypeHandler			booleanHandler;
		public readonly int					enabledFieldIndex;

		private readonly string[]	methodNames;

		private BgColorContentAnimator	animEnable;

		#region Editor
		private readonly Texture2D	icon = null;
		private readonly IUnityData	unityData;
		private bool				fold = true;

		private int					selectedMethod;
		#endregion Editor

		public static void	ClearCache()
		{
			ClientComponent.cachedResources.Clear();
		}

		public	ClientComponent(ClientGameObject parent, NetComponent component, IUnityData unityData)
		{
			this.parent = parent;
			this.type = component.type;
			this.unityData = unityData;
			this.instanceID = component.instanceID;
			this.togglable = component.togglable;
			this.deletable = component.deletable;
			this.name = component.name;

			ClientComponent.reuseFields.Clear();

			this.enabledFieldIndex = -1;

			for (int i = 0; i < component.fields.Length; i++)
			{
				if (component.fields[i].name.Equals("enabled") == true)
					this.enabledFieldIndex = i;

				ClientComponent.reuseFields.Add(new ClientField(this, i, component.fields[i], this.unityData));
			}

			this.fields = ClientComponent.reuseFields.ToArray();

			this.methods = new ClientMethod[component.methods.Length];
			this.methodNames = new string[this.methods.Length];

			for (int i = 0; i < this.methods.Length; i++)
			{
				try
				{
					this.methods[i] = new ClientMethod(this, component.methods[i]);

					StringBuilder	buffer = Utility.GetBuffer();

					if (component.methods[i].returnType != null)
						buffer.Append(component.methods[i].returnType.Name);
					else
						buffer.Append(component.methods[i].returnTypeRaw);
					buffer.Append('	');
					buffer.Append(component.methods[i].name);
					buffer.Append('(');

					string	comma = string.Empty;

					for (int j = 0; j < component.methods[i].argumentTypes.Length; j++)
					{
						buffer.Append(comma);
						buffer.Append(component.methods[i].argumentTypes[j].Name);

						comma = ", ";
					}

					buffer.Append(')');

					this.methodNames[i] = Utility.ReturnBuffer(buffer);
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException("Method " + i + " " + component.methods[i] + " in Component " + this.name + " (" + this.type + ") failed.", ex);
				}
			}

			this.booleanHandler = TypeHandlersManager.GetTypeHandler<bool>();
			this.animEnable = new BgColorContentAnimator(null, 1F, 0F);

			if (this.type != null)
				this.icon = AssetPreview.GetMiniTypeThumbnail(this.type);
			if (this.icon == null)
				this.icon = UtilityResources.CSharpIcon;
		}

		public ClientField	GetField(string name)
		{
			for (int i = 0; i < this.fields.Length; i++)
			{
				if (this.fields[i].name == name)
					return this.fields[i];
			}

			return null;
		}

		public float	GetHeight(NGRemoteInspectorWindow inspector)
		{
			float	height = ClientComponent.Spacing + ClientComponent.ComponentHeaderHeight; // Top spacing + Component bar

			if (this.fold == true)
			{
				for (int i = 0; i < this.fields.Length; i++)
				{
					if (this.fields[i].isPublic == true && this.enabledFieldIndex != i)
						height += ClientComponent.MemberSpacing + this.fields[i].GetHeight(inspector);
				}
			}

			return height;
		}

		public void		OnGUI(Rect r, NGRemoteInspectorWindow inspector)
		{
			this.DrawHeader(r, inspector);

			if (this.fold == false)
				return;

			r.y += ClientComponent.Spacing + ClientComponent.ComponentHeaderHeight;

			++EditorGUI.indentLevel;
			for (int i = 0; i < this.fields.Length; i++)
			{
				if (this.fields[i].isPublic == true && this.enabledFieldIndex != i)
				{
					float	height = this.fields[i].GetHeight(inspector);

					r.y += ClientComponent.MemberSpacing;

					if (r.y + height <= inspector.ScrollPosition.y)
					{
						r.y += height;
						continue;
					}

					r.height = height;
					this.fields[i].Draw(r, inspector);

					r.y += height;
					if (r.y - inspector.ScrollPosition.y > inspector.BodyRect.height)
						break;
				}
			}
			--EditorGUI.indentLevel;
		}

		public bool		CopyComponentToGameObjectAndClipboard(GameObject go)
		{
			GameObject	newGO = null;
			if (go == null)
				go = newGO = new GameObject();

			try
			{
				IncompleteGameObjectException	incompleteEx = null;
				Component						c;

				go.GetComponents(this.type, ClientComponent.reuseComponents);

				int	componentIndex = 0;

				if (newGO != null)
					c = go.AddComponent(this.type);
				else
				{
					for (int i = 0; i < this.parent.components.Count; i++)
					{
						if (this.parent.components[i].type == this.type)
						{
							if (this.parent.components[i] == this)
								break;

							++componentIndex;
						}
					}

					if (ClientComponent.reuseComponents.Count <= componentIndex)
						c = go.AddComponent(this.type);
					else
						c = ClientComponent.reuseComponents[componentIndex];
				}

				for (int i = 0; i < this.fields.Length; i++)
				{
					try
					{
						this.SetValue(c.GetType().FullName + "#" + componentIndex + "." + this.fields[i].name, c, this.fields[i].name, this.fields[i].value);
					}
					catch (IncompleteGameObjectException ex)
					{
						if (incompleteEx == null)
							incompleteEx = ex;
						else
							incompleteEx.Aggregate(ex);
					}
				}

				if (incompleteEx != null)
					throw incompleteEx;

				if (ComponentUtility.CopyComponent(c) == false)
					Debug.LogError("Copy component failed.");
				else
					return true;
			}
			catch (IncompleteGameObjectException)
			{
				throw;
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException("Copy component failed.", ex);
			}
			finally
			{
				if (newGO != null)
					GameObject.DestroyImmediate(newGO);
			}

			return false;
		}

		private void	RequestRemoveComponent()
		{
			this.unityData.AddPacket(new ClientDeleteComponentsPacket(this.parent.instanceID, this.instanceID), this.OnComponentsDeleted);
		}

		private void	OnComponentsDeleted(ResponsePacket p)
		{
			// Deletion of Component is handled using NotifyDeletedComponents.
			p.CheckPacketStatus();
		}

		private void	CopyComponent()
		{
			this.CopyComponentToGameObjectAndClipboard(null);
		}

		private object	ConvertValue(string path, Type type, object value)
		{
			ClientClass	gc = value as ClientClass;

			if (gc != null)
			{
				IncompleteGameObjectException	incompleteEx = null;
				object							result = Activator.CreateInstance(type);

				for (int j = 0; j < gc.fields.Length; j++)
				{
					FieldInfo	subField = type.GetField(gc.fields[j].name);
					InternalNGDebug.Assert(subField != null, "Field \"" + gc.fields[j].name + "\" was not found in type \"" + type + "\".");

					if (subField.IsLiteral == true)
						continue;

					try
					{
						result = this.SetValue(path + "." + gc.fields[j].name, result, gc.fields[j].name, gc.fields[j].value);
					}
					catch (IncompleteGameObjectException ex)
					{
						if (incompleteEx == null)
							incompleteEx = ex;
						else
							incompleteEx.Aggregate(ex);
					}
				}

				if (incompleteEx != null)
					throw incompleteEx;

				return result;
			}

			ArrayData	a = value as ArrayData;

			if (a != null)
			{
				if (a.array != null)
				{
					if (type.IsArray == true)
					{
						IncompleteGameObjectException	incompleteEx = null;
						Type							subType = Utility.GetArraySubType(type);
						Array							result = Array.CreateInstance(type, a.array.Length);

						for (int j = 0; j < a.array.Length; j++)
						{
							try
							{
								result.SetValue(this.ConvertValue(path + "#" + j, subType, a.array.GetValue(j)), j);
							}
							catch (IncompleteGameObjectException ex)
							{
								if (incompleteEx == null)
									incompleteEx = ex;
								else
									incompleteEx.Aggregate(ex);
							}
						}

						if (incompleteEx != null)
							throw incompleteEx;

						return result;
					}
					else if (typeof(IList).IsAssignableFrom(type) == true)
					{
						IncompleteGameObjectException	incompleteEx = null;
						IList							result = Activator.CreateInstance(type) as IList;
						Type							subType = Utility.GetArraySubType(type);

						for (int j = 0; j < a.array.Length; j++)
						{
							try
							{
								result.Add(this.ConvertValue(path + "#" + j, subType, a.array.GetValue(j)));
							}
							catch (IncompleteGameObjectException ex)
							{
								if (incompleteEx == null)
									incompleteEx = ex;
								else
									incompleteEx.Aggregate(ex);
							}
						}

						if (incompleteEx != null)
							throw incompleteEx;

						return result;
					}
					else
						throw new InvalidCastException("Type \"" + type + "\" is not supported as an array.");
				}

				return null;
			}

			UnityObject	uo = value as UnityObject;

			if (uo != null)
				return this.TryFetchFromProject(path, uo);

			return null;
		}

		private Object	TryFetchFromProject(string path, UnityObject unityObject)
		{
			if (unityObject.instanceID == 0)
				return null;

			Object				localAsset;
			ImportAssetState	state = this.unityData.GetAssetFromImportParameters(unityObject.instanceID, out localAsset, true);

			if (state == ImportAssetState.Waiting || state == ImportAssetState.Requesting)
				throw new IncompleteGameObjectException(path, unityObject.type, unityObject.instanceID, this.parent.instanceID, this.instanceID);
				//throw new IncompleteGameObjectException();

			if (state == ImportAssetState.Ready)
				return localAsset;

			string	name = this.unityData.GetResourceName(unityObject.type, unityObject.instanceID);
			if (name == null)
				throw new IncompleteGameObjectException(path, unityObject.type, unityObject.instanceID, this.parent.instanceID, this.instanceID);

			Object[]	assets;

			if (ClientComponent.cachedResources.TryGetValue(unityObject.type, out assets) == false)
			{
				assets = Resources.FindObjectsOfTypeAll(unityObject.type);
				ClientComponent.cachedResources.Add(unityObject.type, assets);
			}

			for (int i = 0; i < assets.Length; i++)
			{
				// TODO Maybe improve the comparison with more than name?
				if (assets[i] != null && assets[i].name == name)
				{
					if (assets[i].hideFlags == HideFlags.None && AssetDatabase.IsForeignAsset(assets[i]) == false && AssetDatabase.IsNativeAsset(assets[i]) == false)
						break;
					this.unityData.ImportAsset(path, unityObject.type, unityObject.instanceID, this.parent.instanceID, this.instanceID, assets[i]);
					return assets[i];
				}
			}

			throw new IncompleteGameObjectException(path, unityObject.type, unityObject.instanceID, this.parent.instanceID, this.instanceID, true);
		}

		private object	SetValue(string path, object instance, string name, object value)
		{
			IFieldModifier	field = InnerUtility.GetFieldInfo(instance.GetType(), name);
			InternalNGDebug.Assert(field != null, "Field \"" + name + "\" was not found in type \"" + instance.GetType() + "\".");
			object			fieldValue = field.GetValue(instance);

			UnityObject	uo = value as UnityObject;

			if (uo != null)
			{
				field.SetValue(instance, this.TryFetchFromProject(path, uo));
				return instance;
			}

			ClientClass	gc = value as ClientClass;

			if (gc != null)
			{
				if (fieldValue == null)
					fieldValue = Activator.CreateInstance(field.Type);

				IncompleteGameObjectException	incompleteEx = null;

				for (int j = 0; j < gc.fields.Length; j++)
				{
					FieldInfo	subField = field.Type.GetField(gc.fields[j].name);
					InternalNGDebug.Assert(subField != null, "Field \"" + gc.fields[j].name + "\" was not found in type \"" + field.Type + "\".");

					if (subField.IsLiteral == true)
						continue;

					try
					{
						fieldValue = this.SetValue(path + '.' + gc.fields[j].name, fieldValue, gc.fields[j].name, gc.fields[j].value);
					}
					catch (IncompleteGameObjectException ex)
					{
						if (incompleteEx == null)
							incompleteEx = ex;
						else
							incompleteEx.Aggregate(ex);
					}
				}

				if (incompleteEx != null)
					throw incompleteEx;

				field.SetValue(instance, fieldValue);

				return instance;
			}

			ArrayData	a = value as ArrayData;

			if (a != null)
			{
				if (a.array != null)
				{
					Type	subType = Utility.GetArraySubType(field.Type);
					if (fieldValue == null)
						fieldValue = Array.CreateInstance(subType, a.array.Length);

					if (field.Type.IsArray == true)
					{
						IncompleteGameObjectException	incompleteEx = null;
						Array							fieldArray = fieldValue as Array;

						if (fieldArray.Length != a.array.Length)
						{
							fieldValue = Array.CreateInstance(subType, a.array.Length);
							fieldArray = fieldValue as Array;
						}

						for (int j = 0; j < a.array.Length; j++)
						{
							try
							{
								fieldArray.SetValue(this.ConvertValue(path + "#" + j, subType, a.array.GetValue(j)), j);
							}
							catch (IncompleteGameObjectException ex)
							{
								if (incompleteEx == null)
									incompleteEx = ex;
								else
									incompleteEx.Aggregate(ex);
							}
						}

						if (incompleteEx != null)
							throw incompleteEx;
					}
					else if (typeof(IList).IsAssignableFrom(field.Type) == true)
					{
						IncompleteGameObjectException	incompleteEx = null;
						IList							fieldArray = fieldValue as IList;

						for (int j = 0; j < a.array.Length; j++)
						{
							try
							{
								fieldArray[j] = this.ConvertValue(path + "#" + j, subType, a.array.GetValue(j));
							}
							catch (IncompleteGameObjectException ex)
							{
								if (incompleteEx == null)
									incompleteEx = ex;
								else
									incompleteEx.Aggregate(ex);
							}
						}

						if (incompleteEx != null)
							throw incompleteEx;
					}
					else
						throw new InvalidCastException("Type \"" + field.Type + "\" is not supported as an array.");

					field.SetValue(instance, fieldValue);
				}

				return instance;
			}

			EnumInstance	e = value as EnumInstance;

			if (e != null)
				field.SetValue(instance, e.value);
			else
				field.SetValue(instance, value);

			return instance;
		}

		private void	DrawHeader(Rect r, NGRemoteInspectorWindow inspector)
		{
			if (Event.current.type == EventType.Repaint)
			{
				Rect	bar = r;

				bar.y += 1F;
				bar.x = 0F;
				bar.width = inspector.position.width;
				bar.height = 1F;

				EditorGUI.DrawRect(bar, ClientComponent.BackgroundColorBar);
			}

			r.y += ClientComponent.Spacing;
			r.height = ClientComponent.ComponentHeaderHeight;

			if (this.type != null &&
				Event.current.type == EventType.MouseDown &&
				Event.current.button == 1 &&
				r.Contains(Event.current.mousePosition) == true)
			{
				GenericMenu	menu = new GenericMenu();

				if (this.deletable == true)
					menu.AddItem(new GUIContent("Remove Component"), false, this.RequestRemoveComponent);
				menu.AddItem(new GUIContent("Copy Component"), false, this.CopyComponent);
				menu.ShowAsContext();

				Event.current.Use();
			}

			if (this.togglable == true && this.enabledFieldIndex != -1 && this.fields[this.enabledFieldIndex].value != null)
			{
				Rect	r2 = r;

				r2.width = 16F;
				r2.x += 34F;

				if (inspector.Hierarchy.GetUpdateNotification(this.parent.instanceID.ToString() + NGServerScene.ValuePathSeparator + this.instanceID.ToString() + NGServerScene.ValuePathSeparator + this.enabledFieldIndex) != NotificationPath.None)
				{
					this.animEnable.af.valueChanged.RemoveAllListeners();
					this.animEnable.af.valueChanged.AddListener(inspector.Repaint);
					this.animEnable.Start();
				}

				using (this.animEnable.Restorer(0F, .8F + this.animEnable.Value, 0F, 1F))
				{
					EditorGUI.BeginChangeCheck();
					bool	enable = EditorGUI.Toggle(r2, (bool)this.fields[this.enabledFieldIndex].value);
					if (EditorGUI.EndChangeCheck() == true)
						this.unityData.AddPacket(new ClientUpdateFieldValuePacket(this.parent.instanceID.ToString() + NGServerScene.ValuePathSeparator + this.instanceID.ToString() + NGServerScene.ValuePathSeparator + this.enabledFieldIndex, this.booleanHandler.Serialize(enable), this.booleanHandler), this.OnComponentToggled);
				}
			}

			if (this.methodNames.Length > 0)
				r.width -= ClientComponent.MethodsWidth;

			this.fold = EditorGUI.Foldout(r, this.fold, GUIContent.none, true);

			Rect	r3 = r;
			r3.x += 16F;
			r3.width = 16F;
			GUI.DrawTexture(r3, this.icon);

			r.x += 48F;
			r.width -= 48F;
			EditorGUI.LabelField(r, new GUIContent(Utility.NicifyVariableName(this.name)), GeneralStyles.ComponentName);

			if (this.methodNames.Length > 0)
			{
				r.x += r.width;
				r.width = ClientComponent.MethodsWidth - ClientComponent.OpenMethodButtonWidth;

				this.selectedMethod = EditorGUI.Popup(r, this.selectedMethod, this.methodNames);

				r.x += r.width;
				r.width = ClientComponent.OpenMethodButtonWidth;

				if (GUI.Button(r, "@") == true)
					EditorWindow.GetWindow<MethodArgumentsWindow>("Method Invoker Form").Init(this.unityData.Client, this.parent.instanceID, this.instanceID, this.methods[this.selectedMethod]);
			}
		}

		private void	OnComponentToggled(ResponsePacket p)
		{
			if (p.CheckPacketStatus() == true)
			{
				ByteBuffer	buffer = Utility.GetBBuffer((p as ServerUpdateFieldValuePacket).rawValue);

				this.fields[this.enabledFieldIndex].value = this.booleanHandler.Deserialize(buffer, typeof(bool));
				Utility.RestoreBBuffer(buffer);
			}
		}
	}
}