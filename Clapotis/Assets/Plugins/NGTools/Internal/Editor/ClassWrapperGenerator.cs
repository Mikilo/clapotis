using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.Internal
{
	public class ClassWrapperGenerator : EditorWindow
	{
		public const string	Title = "Class Wrapper Generator";

		private string		result;
		private string		input = string.Empty;
		private string		outputFilePath;
		private List<Type>	matchingTypes = new List<Type>(128);

		[NonSerialized]
		private Type			type;
		[NonSerialized]
		private FieldInfo[]		fields;
		[NonSerialized]
		private PropertyInfo[]	properties;
		[NonSerialized]
		private MethodInfo[]	methods;

		private List<MemberInfo>	membersEmbedded = new List<MemberInfo>();

		private Vector2	scrollPositionAQN;
		private Vector2	scrollPositionMembers;

		[MenuItem(Constants.PackageTitle + "/Internal/" + ClassWrapperGenerator.Title)]
		private static void	Open()
		{
			Utility.OpenWindow<ClassWrapperGenerator>(ClassWrapperGenerator.Title);
		}

		protected virtual void	OnGUI()
		{
			EditorGUI.BeginChangeCheck();
			this.input = EditorGUILayout.TextField("Type", this.input);
			if (EditorGUI.EndChangeCheck() == true && this.input.Length > 2)
				Utility.RegisterIntervalCallback(this.RefreshMatchingTypes, 100, 1);

			Type	t = Type.GetType(this.input);

			if (t != null)
			{
				if (GUILayout.Button("Analyze " + t.FullName))
				{
					this.membersEmbedded.Clear();
					this.type = t;
					this.fields = t.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
					this.properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
					this.methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

					for (int i = 0; i < this.fields.Length; i++)
					{
						if (this.SkipMember(this.fields[i]) == false)
							this.membersEmbedded.Add(this.fields[i]);
					}
					for (int i = 0; i < this.properties.Length; i++)
					{
						if (this.SkipMember(this.properties[i]) == false)
							this.membersEmbedded.Add(this.properties[i]);
					}
					for (int i = 0; i < this.methods.Length; i++)
					{
						if (this.methods[i].Name == "Finalize" ||
							this.methods[i].Name == "GetHashCode" ||
							this.methods[i].Name == "GetType" ||
							this.methods[i].Name == "MemberwiseClone" ||
							this.methods[i].Name == "ToString")
						{
							continue;
						}

						if (this.SkipMember(this.methods[i]) == false)
							this.membersEmbedded.Add(this.methods[i]);
					}
				}
			}

			this.scrollPositionAQN = EditorGUILayout.BeginScrollView(this.scrollPositionAQN, GUILayoutOptionPool.Height(Mathf.Min(this.matchingTypes.Count * 18F, 200F)));
			{
				for (int i = 0; i < this.matchingTypes.Count; i++)
				{
					if (GUILayout.Button(this.matchingTypes[i].FullName) == true)
						this.input = this.matchingTypes[i].AssemblyQualifiedName;
				}
			}
			EditorGUILayout.EndScrollView();

			this.scrollPositionMembers = EditorGUILayout.BeginScrollView(this.scrollPositionMembers);
			{
				if (this.type != null)
				{
					if (string.IsNullOrEmpty(this.result) == false)
					{
						this.outputFilePath = NGEditorGUILayout.SaveFileField("File", this.outputFilePath);

						if (GUILayout.Button("Write to file") == true)
							File.WriteAllText(this.outputFilePath, this.result);

						if (GUILayout.Button("Copy to clipboard") == true)
							EditorGUIUtility.systemCopyBuffer = this.result;

						if (this.result.Length < short.MaxValue / 2)
							EditorGUILayout.TextArea(this.result, GUILayout.MaxHeight(150F));
						else
							EditorGUILayout.HelpBox("Result is too big to display.", MessageType.Warning);
					}

					if (GUILayout.Button("Generate") == true)
						this.Generate();

					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("Fields (" + this.fields.Length + ")");
					if (GUILayout.Button("Toggle") == true)
						this.ToggleMembers(this.fields);
					GUILayout.FlexibleSpace();
					EditorGUILayout.EndHorizontal();

					++EditorGUI.indentLevel;
					this.DrawMembers(this.fields);
					--EditorGUI.indentLevel;

					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("Properties (" + this.properties.Length + ")");
					if (GUILayout.Button("Toggle") == true)
						this.ToggleMembers(this.properties);
					GUILayout.FlexibleSpace();
					EditorGUILayout.EndHorizontal();

					++EditorGUI.indentLevel;
					this.DrawMembers(this.properties);
					--EditorGUI.indentLevel;


					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("Methods (" + this.methods.Length + ")");
					if (GUILayout.Button("Toggle") == true)
						this.ToggleMembers(this.methods);
					GUILayout.FlexibleSpace();
					EditorGUILayout.EndHorizontal();

					++EditorGUI.indentLevel;
					this.DrawMembers(this.methods);
					--EditorGUI.indentLevel;
				}
			}
			EditorGUILayout.EndScrollView();
		}

		private void	ToggleMembers(MemberInfo[] members)
		{
			for (int i = 0; i < members.Length; i++)
			{
				if (membersEmbedded.Contains(members[i]) == true)
					membersEmbedded.Remove(members[i]);
				else if (this.SkipMember(members[i]) == false)
					membersEmbedded.Add(members[i]);
			}
		}

		private string	ConvertTypeToString(Type type)
		{
			if (type == typeof(void))
				return "void";

			if (type.IsVisible == false)
				return "object";

			if (type.IsGenericType == true)
			{
				Type[]	arguments = type.GetGenericArguments();

				StringBuilder	buffer = Utility.GetBuffer();
				buffer.Append(type.Name.Substring(0, type.Name.IndexOf('`')));
				buffer.Append('<');
				for (int i = 0; i < arguments.Length; i++)
				{
					if (arguments[i].IsVisible == false)
						return "object";

					if (i > 0)
						buffer.Append(", ");
					buffer.Append(arguments[i].Name);
				}
				buffer.Append('>');

				return Utility.ReturnBuffer(buffer);
			}

			if (type.IsByRef == true)
				return "ref " + type.Name.Replace("&", string.Empty);
			else if (type.IsNested == true)
				return ConvertTypeToString(type.DeclaringType) + "." + type.Name;
			else
				return type.Name;
		}

		private void	Generate()
		{
			StringBuilder	buffer = Utility.GetBuffer();
			List<string>	usings = new List<string>() { "System.Reflection"};

			buffer.AppendLine();
			if (this.type.IsAbstract && this.type.IsSealed)
			{
				buffer.AppendLine("public static class " + this.type.Name);
				buffer.AppendLine("{");
			}
			else
			{
				buffer.AppendLine("public class " + this.type.Name);
				buffer.AppendLine("{");
				buffer.AppendLine("	public object instance;");
			}
			buffer.AppendLine("	private static Type	type = Type.GetType(\"" + this.type.AssemblyQualifiedName + "\");");

			for (int i = 0; i < this.fields.Length; i++)
			{
				if (this.membersEmbedded.Contains(this.fields[i]) == true)
				{
					buffer.AppendLine();
					buffer.AppendLine("	private static FieldInfo	" + this.fields[i].Name + "Field = " + this.type.Name + ".type.GetField(\"" + this.fields[i].Name + "\", " + this.GetBindingFlags(this.fields[i]) + ");");
					this.AddType(usings, this.fields[i].FieldType);

					if (this.fields[i].IsStatic == true)
					{
						buffer.AppendLine("	public static " + this.ConvertTypeToString(this.fields[i].FieldType) + "	" + this.fields[i].Name);
						buffer.AppendLine("	{");
						buffer.AppendLine("		get { return (" + this.ConvertTypeToString(this.fields[i].FieldType) + ")" + this.fields[i].Name + "Field.GetValue(null); }");
						buffer.AppendLine("		set { " + this.fields[i].Name + "Field.SetValue(null, value); }");
						buffer.AppendLine("	}");
					}
					else
					{
						buffer.AppendLine("	public " + this.ConvertTypeToString(this.fields[i].FieldType) + "	" + this.fields[i].Name);
						buffer.AppendLine("	{");
						buffer.AppendLine("		get { return (" + this.ConvertTypeToString(this.fields[i].FieldType) + ")" + this.fields[i].Name + "Field.GetValue(this.instance); }");
						buffer.AppendLine("		set { " + this.fields[i].Name + "Field.SetValue(this.instance, value); }");
						buffer.AppendLine("	}");
					}
				}
			}

			for (int i = 0; i < this.properties.Length; i++)
			{
				if (this.membersEmbedded.Contains(this.properties[i]) == true)
				{
					buffer.AppendLine();
					buffer.AppendLine("	private static PropertyInfo	" + this.properties[i].Name + "Property = " + this.type.Name + ".type.GetProperty(\"" + this.properties[i].Name + "\", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);");
					this.AddType(usings, this.properties[i].PropertyType);

					if ((this.properties[i].GetGetMethod(true) != null && this.properties[i].GetGetMethod(true).IsStatic == true) ||
						(this.properties[i].GetSetMethod(true) != null && this.properties[i].GetSetMethod(true).IsStatic == true))
					{
						buffer.AppendLine("	public static " + this.ConvertTypeToString(this.properties[i].PropertyType) + "	" + this.properties[i].Name);
						buffer.AppendLine("	{");
						if (this.properties[i].CanRead == true)
							buffer.AppendLine("		get { return (" + this.ConvertTypeToString(this.properties[i].PropertyType) + ")" + this.properties[i].Name + "Property.GetValue(null, null); }");
						if (this.properties[i].CanWrite == true)
							buffer.AppendLine("		set { " + this.properties[i].Name + "Property.SetValue(null, value, null); }");
						buffer.AppendLine("	}");
					}
					else
					{
						buffer.AppendLine("	public " + this.ConvertTypeToString(this.properties[i].PropertyType) + "	" + this.properties[i].Name);
						buffer.AppendLine("	{");
						if (this.properties[i].CanRead == true)
							buffer.AppendLine("		get { return (" + this.ConvertTypeToString(this.properties[i].PropertyType) + ")" + this.properties[i].Name + "Property.GetValue(this.instance, null); }");
						if (this.properties[i].CanWrite == true)
							buffer.AppendLine("		set { " + this.properties[i].Name + "Property.SetValue(this.instance, value, null); }");
						buffer.AppendLine("	}");
					}
				}
			}

			for (int i = 0; i < this.methods.Length; i++)
			{
				if (this.membersEmbedded.Contains(this.methods[i]) == true)
				{
					buffer.AppendLine();
					buffer.AppendLine("	private static MethodInfo	" + this.methods[i].Name + "Method = " + this.type.Name + ".type.GetMethod(\"" + this.methods[i].Name + "\", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);");

					this.AddType(usings, this.methods[i].ReturnType);

					if (this.methods[i].IsStatic == true)
						buffer.Append("	public static " + this.ConvertTypeToString(this.methods[i].ReturnType) + "	" + this.methods[i].Name + "(");
					else
						buffer.Append("	public " + this.ConvertTypeToString(this.methods[i].ReturnType) + "	" + this.methods[i].Name + "(");

					ParameterInfo[]	parameters = this.methods[i].GetParameters();

					for (int j = 0; j < parameters.Length; j++)
					{
						if (j > 0)
							buffer.Append(", ");

						this.AddType(usings, parameters[j].ParameterType);
						buffer.Append(this.ConvertTypeToString(parameters[j].ParameterType));

						buffer.Append(' ');
						buffer.Append(parameters[j].Name);
					}

					buffer.AppendLine(")");

					buffer.AppendLine("	{");
					buffer.Append("		");
					if (this.methods[i].ReturnType != typeof(void))
						buffer.Append("return (" + this.ConvertTypeToString(this.methods[i].ReturnType) + ")");

					if (this.methods[i].IsStatic == true)
						buffer.Append(this.type.Name + "." + this.methods[i].Name + "Method.Invoke(null, new object[] {");
					else
						buffer.Append(this.type.Name + "." + this.methods[i].Name + "Method.Invoke(this.instance, new object[] {");
					for (int j = 0; j < parameters.Length; j++)
					{
						if (j > 0)
							buffer.Append(", ");
						buffer.Append(parameters[j].Name);
					}

					buffer.AppendLine("});");
					buffer.AppendLine("	}");
				}
			}

			buffer.Append("}");

			usings.Sort();
			for (int i = 0; i < usings.Count; i++)
				buffer.Insert(0, "using " + usings[i] + ";" + Environment.NewLine);

			this.result = Utility.ReturnBuffer(buffer);
		}

		private void	AddType(List<string> usings, Type type)
		{
			if (type.IsVisible == false)
				return;

			if (string.IsNullOrEmpty(type.Namespace) == false && usings.Contains(type.Namespace) == false)
				usings.Add(type.Namespace);
		}

		private string	GetBindingFlags(FieldInfo field)
		{
			StringBuilder	buffer = Utility.GetBuffer();

			if (field.IsStatic == true)
				buffer.Append("BindingFlags.Static");
			else
				buffer.Append("BindingFlags.Instance");

			if (field.IsPublic == true)
				buffer.Append(" | BindingFlags.Public");
			else
				buffer.Append(" | BindingFlags.NonPublic");

			return Utility.ReturnBuffer(buffer);
		}

		private void	DrawMembers(MemberInfo[] members)
		{
			for (int i = 0; i < members.Length; i++)
			{
				if (this.SkipMember(members[i]) == true)
					continue;

				EditorGUI.BeginChangeCheck();
				bool	v = EditorGUILayout.ToggleLeft(members[i].ToString(), this.membersEmbedded.Contains(members[i]));
				if (EditorGUI.EndChangeCheck() == true)
				{
					if (v == true)
						this.membersEmbedded.Add(members[i]);
					else
						this.membersEmbedded.Remove(members[i]);
				}
			}
		}

		private bool	SkipMember(MemberInfo member)
		{
			return member.Name[0] == '<' || (member is MethodInfo && (member as MethodInfo).IsSpecialName == true);
		}

		private void	RefreshMatchingTypes()
		{
			if (string.IsNullOrEmpty(this.input) == true)
				return;

			string[]	subParts = this.input.Split(' ');

			this.matchingTypes.Clear();

			foreach (Type type in Utility.EachAllAssignableFrom(typeof(object)))
			{
				if (type.IsClass == false && type.IsInterface == false)
					continue;

				if (type.FullName.Contains(this.input) == true)
				{
					this.matchingTypes.Add(type);
					continue;
				}

				int	i = 0;

				for (; i < subParts.Length; i++)
				{
					if (type.FullName.Contains(subParts[i]) == false)
						break;
				}

				if (i < subParts.Length)
					continue;

				this.matchingTypes.Add(type);
				if (this.matchingTypes.Count >= 100)
					break;
			}

			this.Repaint();
		}
	}
}