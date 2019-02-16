using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace NGTools.NGGameConsole
{
	public class ClassInspector
	{
		private struct ClassDefinition
		{
			public readonly IFieldModifier[]	members;
			public readonly string[]			nicifiedNames;

			public	ClassDefinition(IFieldModifier[] members)
			{
				this.members = members;
				this.nicifiedNames = new string[this.members.Length];

				for (int i = 0; i < this.members.Length; i++)
					this.nicifiedNames[i] = Utility.NicifyVariableName(this.members[i].Name);
			}
		}

		public const float	Height = 11F;

		public GUIStyle	textStyle;
		public GUIStyle	buttonStyle;
		public GUIStyle	inputStyle;

		private List<IFieldModifier>	members = new List<IFieldModifier>();
		private string[]				membersNicifiedNames;
		private bool[]					membersSetMethods;
		private object[]				membersValues;

		private object	target = null;
		private int		editingProperty = -1;
		private object	editingPropertyValue = null;

		private Dictionary<Type, object[]>			typeDrawers = new Dictionary<Type, object[]>();
		private Dictionary<Type, ClassDefinition>	refMembers = new Dictionary<Type, ClassDefinition>();

		private List<IFieldModifier>	classDefinitionMembers = new List<IFieldModifier>();

		public	ClassInspector()
		{
			this.AddDrawer(typeof(Resolution), this.ToStringDrawer, this.CopyToString);
			this.AddDrawer(typeof(Vector2), this.ToStringDrawer, this.CopyToString);
			this.AddDrawer(typeof(Vector3), this.ToStringDrawer, this.CopyToString);
			this.AddDrawer(typeof(Vector4), this.ToStringDrawer, this.CopyToString);
			this.AddDrawer(typeof(Quaternion), this.ToStringDrawer, this.CopyToString);
			this.AddDrawer(typeof(Rect), this.ToStringDrawer, this.CopyToString);
			this.AddDrawer(typeof(Bounds), this.ToStringDrawer, this.CopyToString);
			this.AddDrawer(typeof(RectOffset), this.ToStringDrawer, this.CopyToString);
			this.AddDrawer(typeof(Color), this.ToStringDrawer, this.CopyToString);
			this.AddDrawer(typeof(Color32), this.ToStringDrawer, this.CopyToString);
			this.AddDrawer(typeof(Matrix4x4), this.ToStringDrawer, this.CopyToString);
		}

		private void	ToStringDrawer(object instance, string prefix)
		{
			GUILayout.Label(prefix + instance.ToString(), this.textStyle);
		}

		private string	CopyToString(object instance, string prefix)
		{
			return prefix + instance.ToString();
		}

		public void	AddDrawer(Type type, Action<object, string> drawer, Func<object, string, string> copy)
		{
			this.typeDrawers.Add(type, new object[] { drawer, copy });
		}

		public void	Construct()
		{
//			members.Sort((IFieldModifier a, IFieldModifier b) => a.Name.CompareTo(b.Name));

			this.membersNicifiedNames = new string[this.members.Count];
			this.membersSetMethods = new bool[this.members.Count];
			this.membersValues = new object[this.members.Count];

			for (int i = 0; i < this.members.Count; i++)
			{
				if (this.members[i].IsDefined(typeof(ObsoleteAttribute), false) == true)
					this.members[i] = null;
				else
				{
					this.membersNicifiedNames[i] = Utility.NicifyVariableName(this.members[i].Name) + " : ";

					if (this.members[i].Type.IsSubclassOf(typeof(UnityEngine.Object)) == false)
					{
						if (this.members[i] is PropertyModifier)
							this.membersSetMethods[i] = (this.members[i] as PropertyModifier).propertyInfo.GetSetMethod() != null;
						else // Is FieldModifier.
							this.membersSetMethods[i] = true;
					}
				}
			}

			this.UpdatePropertiesValues();
		}

		public string	Copy()
		{
			StringBuilder	buffer = Utility.GetBuffer();

			for (int i = 0; i < this.members.Count; i++)
			{
				if (this.members[i] == null)
					continue;

				bool	complexType = this.members[i].Type.IsArray == true ||
									  (this.members[i].Type != typeof(string) &&
									   (this.members[i].Type.IsClass == true || this.members[i].Type.IsStruct() == true));

				buffer.Append(this.membersNicifiedNames[i]);

				if (complexType == false)
					this.CopyObject(buffer, this.membersValues[i], string.Empty);
				else
				{
					buffer.AppendLine();
					this.CopyObject(buffer, this.membersValues[i], "  ");
				}
			}

			return Utility.ReturnBuffer(buffer);
		}

		private void	CopyObject(StringBuilder buffer, object instance, string prefix)
		{
			if (instance != null)
			{
				Type		type = instance.GetType();
				object[]	actions;

				if (this.typeDrawers.TryGetValue(type, out actions) == true)
					buffer.AppendLine((actions[1] as Func<object, string, string>)(instance, prefix));
				else
				{
					if (instance.GetType().IsArray == true)
					{
						IEnumerable	array = instance as IEnumerable;

						foreach (object element in array)
							this.CopyObject(buffer, element, "  ");
					}
					else if (type != typeof(string) &&
							 (type.IsClass == true || type.IsStruct() == true))
					{
						ClassDefinition	classDefinition;

						if (this.refMembers.TryGetValue(type, out classDefinition) == false)
						{
							int					j = 0;
							FieldInfo[]			fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
							PropertyInfo[]		properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
							IFieldModifier[]	members = new IFieldModifier[fields.Length + properties.Length];

							for (int i = 0; i < fields.Length; i++, ++j)
								members[j] = new FieldModifier(fields[i]);

							for (int i = 0; i < properties.Length; i++, ++j)
								members[j] = new PropertyModifier(properties[i]);

							classDefinition = new ClassDefinition(members);

							this.refMembers.Add(type, classDefinition);
						}

						for (int i = 0; i < classDefinition.members.Length; i++)
							this.CopyObject(buffer, classDefinition.members[i].GetValue(instance), "  " + classDefinition.nicifiedNames[i] + " : ");
					}
					else
						buffer.AppendLine(prefix + instance.ToString());
				}
			}
			else
				buffer.AppendLine(prefix + "NULL");
		}

		public void	ExtractTypeProperties(Type type, BindingFlags flags = BindingFlags.Public | BindingFlags.Static)
		{
			List<PropertyInfo>	properties = new List<PropertyInfo>(type.GetProperties(flags));
			properties.Sort((PropertyInfo a, PropertyInfo b) => a.Name.CompareTo(b.Name));

			for (int i = 0; i < properties.Count; i++)
				this.members.Add(new PropertyModifier(properties[i]));
		}

		public void	ExtractTypeFields(Type type, BindingFlags flags = BindingFlags.Public | BindingFlags.Static)
		{
			List<FieldInfo>	fields = new List<FieldInfo>(type.GetFields(flags));
			fields.Sort((FieldInfo a, FieldInfo b) => a.Name.CompareTo(b.Name));

			for (int i = 0; i < fields.Count; i++)
				this.members.Add(new FieldModifier(fields[i]));
		}

		public void	UpdatePropertiesValues()
		{
			for (int i = 0; i < this.members.Count; i++)
			{
				if (this.members[i] == null)
					continue;

				try
				{
					this.membersValues[i] = this.members[i].GetValue(this.target);
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException("Updating property \"" + this.members[i].Name + "\" (" + this.members[i].Type.DeclaringType + " " + this.members[i].Type.Name + ") failed.", ex);
					this.members[i] = null;
				}
			}
		}

		public void	OnGUI()
		{
			if (this.textStyle == null)
			{
				this.textStyle = new GUIStyle(GUI.skin.label);
				this.textStyle.padding = new RectOffset(this.textStyle.padding.left, 0, 0, 0);
				this.textStyle.margin = new RectOffset(0, 0, 4, 4);
				this.textStyle.fixedHeight = ClassInspector.Height;
				this.textStyle.clipping = TextClipping.Overflow;

				this.buttonStyle = new GUIStyle(GUI.skin.button);
				this.buttonStyle.padding = new RectOffset(this.buttonStyle.padding.left, this.buttonStyle.padding.right, 0, 0);
				this.buttonStyle.fixedHeight = ClassInspector.Height;
				this.buttonStyle.fontSize = 13;
				this.buttonStyle.clipping = TextClipping.Overflow;
				this.buttonStyle.contentOffset = new Vector2(0F, 2F);
				this.buttonStyle.overflow = new RectOffset(0, 0, 0, 4);

				this.inputStyle = new GUIStyle(GUI.skin.textField);
				this.inputStyle.alignment = TextAnchor.MiddleLeft;
				this.inputStyle.clipping = TextClipping.Overflow;
				this.inputStyle.fixedHeight = ClassInspector.Height;
				this.inputStyle.contentOffset = new Vector2(0F, 2F);
				this.inputStyle.overflow = new RectOffset(0, 0, 0, 4);
			}

			for (int i = 0; i < this.members.Count; i++)
			{
				if (this.members[i] == null)
					continue;

				bool	complexType = this.members[i].Type.IsSubclassOf(typeof(UnityEngine.Object)) == false &&
									  (this.members[i].Type.IsArray == true ||
									   (this.members[i].Type != typeof(string) &&
									    (this.members[i].Type.IsClass == true || this.members[i].Type.IsStruct() == true)));

				GUILayout.BeginHorizontal();
				{
					GUILayout.Label(this.membersNicifiedNames[i], this.textStyle, GUILayout.ExpandWidth(false));

					if (complexType == false)
					{
						if (this.editingProperty != i)
							this.DrawObject(this.membersValues[i], string.Empty);

						if (this.membersSetMethods[i] == true)
						{
							if (this.editingProperty != i)
							{
								if (GUILayout.Button("Edit", this.buttonStyle, GUILayout.ExpandWidth(false)) == true)
								{
									this.editingProperty = i;
									this.editingPropertyValue = this.membersValues[i];
								}
							}
							else
							{
								object	result;

								if (this.DrawPropertyEditor(out result, this.members[i].Type, this.membersValues[i]) == true)
								{
									this.members[i].SetValue(this.target, result);
									this.membersValues[i] = this.members[i].GetValue(this.target);
								}
							}
						}
					}
				}
				GUILayout.EndHorizontal();

				if (complexType == true)
					this.DrawObject(this.membersValues[i], "  ");
			}
		}

		private void	DrawObject(object instance, string prefix)
		{
			if (instance != null)
			{
				Type		type = instance.GetType();
				object[]	actions;

				if (type.IsSubclassOf(typeof(UnityEngine.Object)) == true)
					GUILayout.Label(prefix + (instance as UnityEngine.Object).name, this.textStyle, GUILayout.ExpandWidth(false));
				else if (this.typeDrawers.TryGetValue(type, out actions) == true)
					(actions[0] as Action<object, string>)(instance, prefix);
				else
				{
					if (instance.GetType().IsArray == true)
					{
						IEnumerable	array = instance as IEnumerable;

						foreach (object element in array)
							this.DrawObject(element, "  ");
					}
					else if (type != typeof(string) &&
							 (type.IsClass == true || type.IsStruct() == true))
					{
						ClassDefinition	classDefinition;

						if (this.refMembers.TryGetValue(type, out classDefinition) == false)
						{
							int				j = 0;
							FieldInfo[]		fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
							PropertyInfo[]	properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

							this.classDefinitionMembers.Clear();

							for (int i = 0; i < fields.Length; i++, ++j)
								this.classDefinitionMembers.Add(new FieldModifier(fields[i]));

							for (int i = 0; i < properties.Length; i++, ++j)
							{
								if (properties[i].GetIndexParameters().Length == 0)
									this.classDefinitionMembers.Add(new PropertyModifier(properties[i]));
							}

							classDefinition = new ClassDefinition(this.classDefinitionMembers.ToArray());
							this.refMembers.Add(type, classDefinition);
						}

						for (int i = 0; i < classDefinition.members.Length; i++)
						{
							if (classDefinition.members[i] != null)
								this.DrawObject(classDefinition.members[i].GetValue(instance), "  " + classDefinition.nicifiedNames[i] + " : ");
						}
					}
					else
						GUILayout.Label(prefix + instance.ToString(), this.textStyle, GUILayout.ExpandWidth(false));
				}
			}
			else
				GUILayout.Label(prefix + "NULL", this.textStyle, GUILayout.ExpandWidth(false));
		}

		private bool	DrawPropertyEditor(out object result, Type type, object instance)
		{
			if (type.IsArray == true)
			{
				IEnumerable	array = instance as IEnumerable;
				Type		subType = type.GetElementType();

				foreach (object element in array)
				{
					object	subResult;

					if (this.DrawPropertyEditor(out subResult, subType, element) == true)
					{
						result = instance;
						return true;
					}
				}
			}
			else if (type == typeof(bool))
			{
				if (Event.current.type == EventType.Repaint)
				{
					this.editingProperty = -1;
					result = !(bool)this.editingPropertyValue;
					return true;
				}
			}
			else if (type == typeof(int))
			{
				string	v = ((int)this.editingPropertyValue).ToString();
				string	newValue = GUILayout.TextField(v, this.inputStyle, GUILayout.ExpandWidth(false));
				int		n;

				if (int.TryParse(newValue, out n) == false)
					GUILayout.Label("Must be an integer.");
				else
				{
					this.editingPropertyValue = n;

					if (GUILayout.Button("Set", this.buttonStyle, GUILayout.ExpandWidth(false)) == true)
					{
						this.editingProperty = -1;
						result = n;
						return true;
					}
				}
			}
			else if (type == typeof(float))
			{
				string	v = ((float)this.editingPropertyValue).ToString();
				string	newValue = GUILayout.TextField(v, this.inputStyle, GUILayout.ExpandWidth(false));
				float	f;

				if (float.TryParse(newValue, out f) == false)
					GUILayout.Label("Must be a float.");
				else
				{
					this.editingPropertyValue = f;

					if (GUILayout.Button("Set", this.buttonStyle, GUILayout.ExpandWidth(false)) == true)
					{
						this.editingProperty = -1;
						result = f;
						return true;
					}
				}
			}
			else if (type == typeof(string))
			{
				this.editingPropertyValue = GUILayout.TextField((string)this.editingPropertyValue, this.inputStyle, GUILayout.ExpandWidth(false));

				if (GUILayout.Button("Set", this.buttonStyle, GUILayout.ExpandWidth(false)) == true)
				{
					this.editingProperty = -1;
					result = this.editingPropertyValue;
					return true;
				}
			}
			else if (type.IsEnum == true)
			{
				string[]	names = Enum.GetNames(type);
				string		v = this.editingPropertyValue.ToString();
				GUILayout.Label(v, GUILayout.ExpandWidth(false));

				if (GUILayout.Button("Set", this.buttonStyle, GUILayout.ExpandWidth(false)) == true)
				{
					this.editingProperty = -1;
					result = Enum.Parse(type, v);
					return true;
				}

				if (GUILayout.Button("Cancel", this.buttonStyle, GUILayout.ExpandWidth(false)) == true)
					this.editingProperty = -1;

				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();

				for (int j = 0; j < names.Length; j++)
				{
					if (GUILayout.Button(names[j]) == true)
					{
						if (v != names[j])
							this.editingPropertyValue = names[j];
						else
						{
							this.editingProperty = -1;
							result = Enum.Parse(type, v);
							return true;
						}
					}
				}
			}
			else
				GUILayout.Label("Unsupported type.", this.textStyle, GUILayout.ExpandWidth(false));

			if (GUILayout.Button("Cancel", this.buttonStyle, GUILayout.ExpandWidth(false)) == true)
				this.editingProperty = -1;

			result = null;
			return false;
		}
	}
}