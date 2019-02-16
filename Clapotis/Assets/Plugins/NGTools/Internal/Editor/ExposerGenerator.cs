using NGPackageHelpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using UnityEditor;

namespace NGToolsEditor.Internal
{
	using UnityEngine;

	public class ExposerGenerator : EditorWindow
	{
		public class ExposedClass
		{
			public Type			type;
			public List<string>	included = new List<string>();
			public List<string>	excluded = new List<string>();

			public Dictionary<string, List<string>>	properties = new Dictionary<string, List<string>>();
			public Dictionary<string, List<string>>	fields = new Dictionary<string, List<string>>();
		}

		public const string	Title = "Exposer Generator";

		public string	exposersFolder;

		[MenuItem(Constants.PackageTitle + "/Internal/" + ExposerGenerator.Title)]
		private static void	Open()
		{
			Utility.OpenWindow<ExposerGenerator>(ExposerGenerator.Title);
		}

		protected virtual void	OnEnable()
		{
			Utility.LoadEditorPref(this);
		}

		protected virtual void	OnDisable()
		{
			Utility.SaveEditorPref(this);
		}

		protected virtual void	OnGUI()
		{
			this.exposersFolder = EditorGUILayout.TextField("Exposers Folder", this.exposersFolder);
			EditorGUILayout.HelpBox("After generating, this current instance of Unity Editor will be unstable and might crash anytime.", MessageType.Warning);

			if (GUILayout.Button("Generate") == true)
			{
				List<ExposedClass>	classes = new List<ExposerGenerator.ExposedClass>();

				// Manual init
				classes.Add(new ExposedClass()
				{
					type = typeof(Renderer),
					excluded = new List<string>() { "material", "materials", "sharedMaterial" }
				});
				classes.Add(new ExposedClass()
				{
					type = typeof(MeshFilter),
					excluded = new List<string>() { "mesh" }
				});
				classes.Add(new ExposedClass()
				{
					type = typeof(Collider),
					excluded = new List<string>() { "material" }
				});
				classes.Add(new ExposedClass()
				{
					type = typeof(Transform),
					included = new List<string>() { "localPosition", "localEulerAngles", "localScale" }
				});

				Dictionary<string, string>	installs = new Dictionary<string, string>();

				NGUnityDetectorWindow.GetInstalls(installs);

				foreach (var pair in installs)
				{
					string			dllPath = Path.Combine(pair.Value, @"Editor\Data\Managed\UnityEngine.dll");
					AppDomainSetup	domaininfo = new AppDomainSetup();
					domaininfo.ApplicationBase = @"Library\ScriptAssemblies";
					Evidence		adevidence = AppDomain.CurrentDomain.Evidence;
					AppDomain		domain = AppDomain.CreateDomain("MyDomain", adevidence, domaininfo);

					Type	type2 = typeof(ProxyDomain);
					var		value = (ProxyDomain)domain.CreateInstanceAndUnwrap(type2.Assembly.FullName, type2.FullName);

					value.GetAssembly(dllPath);

					Assembly	unityEngine = Assembly.LoadFile(dllPath);
					Debug.Log("Assembly found at " + dllPath);

					for (int i = 0; i < classes.Count; i++)
					{
						Type			type = unityEngine.GetType(classes[i].type.FullName, true);
						PropertyInfo[]	pis = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

						for (int j = 0; j < pis.Length; j++)
						{
							if (classes[i].included.Count > 0 && classes[i].included.Contains(pis[j].Name) == false)
								continue;

							if (classes[i].excluded.Contains(pis[j].Name) == true ||
								pis[j].IsDefined(typeof(ObsoleteAttribute), false) == true ||
								pis[j].IsDefined(typeof(HideInInspector), false) == true ||
								pis[j].GetIndexParameters().Length != 0 || // Skip indexer.
								pis[j].CanRead == false || // Skip prop without both get/set.
								pis[j].CanWrite == false ||
								this.CanExposeTypeInInspector(pis[j].PropertyType) == false)
							{
								continue;
							}

							List<string>	versions;

							if (classes[i].properties.TryGetValue(pis[j].Name, out versions) == false)
							{
								versions = new List<string>();
								classes[i].properties.Add(pis[j].Name, versions);
							}

							versions.Add(pair.Key);
						}

						FieldInfo[]		fis = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

						for (int j = 0; j < fis.Length; j++)
						{
							if (classes[i].included.Count > 0 && classes[i].included.Contains(fis[j].Name) == false)
								continue;

							if (classes[i].excluded.Contains(fis[j].Name) == true ||
								fis[j].IsDefined(typeof(ObsoleteAttribute), false) == true ||
								fis[j].IsDefined(typeof(HideInInspector), false) == true ||
								this.CanExposeTypeInInspector(fis[j].FieldType) == false)
							{
								continue;
							}

							List<string>	versions;

							if (classes[i].fields.TryGetValue(fis[j].Name, out versions) == false)
							{
								versions = new List<string>();
								classes[i].fields.Add(fis[j].Name, versions);
							}

							versions.Add(pair.Key);
						}
					}

					AppDomain.Unload(domain);
				}

				string	latestUnityVersion = "0";

				foreach (var pair in installs)
				{
					if ((pair.Key[0] == '2' && (latestUnityVersion[0] == '4' || latestUnityVersion[0] == '5')) ||
						(pair.Key[0] == latestUnityVersion[0] && latestUnityVersion.CompareTo(pair.Key) < 0) ||
						latestUnityVersion[0] == '0')
					{
						latestUnityVersion = pair.Key;
					}
				}

				latestUnityVersion = latestUnityVersion.Substring(0, latestUnityVersion.LastIndexOf('.'));

				StringBuilder	linkBuffer = Utility.GetBuffer();

				linkBuffer.AppendLine("<linker>");
				linkBuffer.AppendLine("	<assembly fullname=\"UnityEngine\">");
				linkBuffer.AppendLine("		<type fullname=\"" + typeof(GameObject).FullName + "\" preserve=\"all\"/>");

				Directory.CreateDirectory(this.exposersFolder);

				string	path;

				for (int i = 0; i < classes.Count; i++)
				{
					StringBuilder	buffer = Utility.GetBuffer();

					linkBuffer.AppendLine("		<type fullname=\"" + classes[i].type.FullName + "\" preserve=\"all\"/>");

					buffer.Append(@"// File auto-generated by ExposerGenerator.
using System.Reflection;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	internal sealed class " + classes[i].type.Name + @"Exposer : ComponentExposer
	{
		public	" + classes[i].type.Name + @"Exposer() : base(typeof(" + classes[i].type.Name + @"))
		{
		}");

					this.AddMembers(buffer, "Property", classes[i].properties, latestUnityVersion);
					this.AddMembers(buffer, "Field", classes[i].fields, latestUnityVersion);

					buffer.Append(@"
	}
}");

					Debug.Log("Type " + classes[i].type.FullName);
					foreach (var item in classes[i].properties)
						Debug.Log("Property " + item.Key + " in " + string.Join(", ", item.Value.ToArray()));
					foreach (var item in classes[i].fields)
						Debug.Log("Field " + item.Key + " in " + string.Join(", ", item.Value.ToArray()));

					path = Path.Combine(this.exposersFolder, classes[i].type.Name + "Exposer.cs");

					File.WriteAllText(path, Utility.ReturnBuffer(buffer));
					Debug.Log("Exposer generated at \"" + path + "\".");
				}

				linkBuffer.AppendLine("	</assembly>");
				linkBuffer.AppendLine("</linker>");

				path = Path.Combine(this.exposersFolder, "link.xml");
				Debug.Log("Links generated at \"" + path + "\".");
				File.WriteAllText(Path.Combine(this.exposersFolder, "link.xml"), Utility.ReturnBuffer(linkBuffer));
			}
		}

		private void	AddMembers(StringBuilder buffer, string memberName, Dictionary<string, List<string>> members, string latestUnityVersion)
		{
			if (members.Count == 0)
				return;

			List<string>	versions = new List<string>();

			buffer.AppendLine(@"

		private " + memberName + "Info[]	cached" + memberName + @";

		public override " + memberName + "Info[]	Get" + memberName + @"Infos()
		{
			if (this.cached" + memberName + @" == null)
			{
				ComponentExposer." + memberName + @".Clear();

				string	unityVersion = Utility.UnityVersion;
");

			foreach (var item in members)
			{
				versions.Clear();

				item.Value.Sort((string a, string b) =>
				{
					var aS = a.Split('.');
					var bS = b.Split('.');
					if (aS[0] != bS[0])
						return int.Parse(bS[0]) - int.Parse(aS[0]);
					return int.Parse(bS[1]) - int.Parse(aS[1]);
				});

				for (int k = 0; k < item.Value.Count; k++)
				{
					string	version = item.Value[k].Substring(0, item.Value[k].LastIndexOf('.'));

					if (versions.Contains(version) == false)
						versions.Add(version);
				}

				int	i = 0;

				// Use first element to compare, because 2017 should be on top.
				if (versions[0] != latestUnityVersion)
					buffer.Append("				if (");
				else
				{
					i = 1;
					buffer.Append("				if ((unityVersion[0] == '" + latestUnityVersion[0] + "' && \"" + latestUnityVersion + @""".CompareTo(unityVersion) <= 0)");
				}

				for (; i < versions.Count; i++)
				{
					if (i > 0)
					{
						buffer.AppendLine(" ||");
						buffer.Append("					");
					}

					buffer.Append(@"unityVersion.StartsWith(""" + versions[i] + "\")");
				}

				buffer.AppendLine(@")
				{
					ComponentExposer." + memberName + ".Add(this.type.Get" + memberName + "(\"" + item.Key + @"""));
				}
");
			}

			buffer.Append(@"				if (ComponentExposer." + memberName + @".Count == 0)
					this.cached" + memberName + " = ComponentExposer.EmptyArray" + memberName + @";
				else
					this.cached" + memberName + " = ComponentExposer." + memberName + @".ToArray();
			}

			return this.cached" + memberName + @";
		}");
		}

		// A copy of Utility.CanExposeTypeInInspector, but uses AreSame instead of == which does not work for different assemblies.
		public bool	CanExposeTypeInInspector(Type type)
		{
			if (type.IsPrimitive == true || // Any primitive types (int, float, byte, etc... not struct or decimal).
				AreSame(type, typeof(string)) || // Built-in types.
				type.IsEnum == true ||
				AreSame(type, typeof(Rect)) ||
				AreSame(type, typeof(Vector3)) ||
				AreSame(type, typeof(Color)) ||
				this.CheckParenting(typeof(Object), type) == true ||
				//typeof(Object).IsAssignableFrom(type) == true || // Unity Object.
				AreSame(type, typeof(Object)) ||
				AreSame(type, typeof(Vector2)) ||
				AreSame(type, typeof(Vector4)) ||
				AreSame(type, typeof(Quaternion)) ||
				AreSame(type, typeof(AnimationCurve)))
			{
				return true;
			}

			if (type.IsInterface == true)
				return false;

			if (typeof(Delegate).IsAssignableFrom(type) == true)
				return false;

			if (type.GetInterface(typeof(IList<>).Name) != null) // IList<> or Array with Serializable elements.
			{
				Type	subType = type.GetInterface(typeof(IList<>).Name).GetGenericArguments()[0];

				// Nested list or array is not supported.
				if (subType.GetInterface(typeof(IList<>).Name) == null)
					return this.CanExposeTypeInInspector(subType);
				return false;
			}

			if (typeof(IList).IsAssignableFrom(type) == true) // IList with Serializable elements.
			{
				return false;
				//PropertyInfo prop = type.GetProperty("Item", new Type[] { typeof(int) });
				//return (prop != null && Utility.CanExposeInInspector(prop.PropertyType) == true);
			}

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) // Especially, we prevent Dictionary to be exposed.
				return false;

			// We need to check if type is a class or struct even after all the checks before. Because types like Decimal have Serializable and are not primitive. Thank you CLR and 64bits implementation limitation.
			if (AreSame(type, typeof(Decimal)) == false &&
				(type.IsClass == true || // A class.
				 type.IsStruct() == true) && // Or a struct.
				type.IsSerializable == true) // With SerializableAttribute.
			{
				return true;
			}

			return false;
		}

		private bool	CheckParenting(Type parent, Type b)
		{
			string	aqn = null;

			while (b != null && aqn != b.AssemblyQualifiedName)
			{
				aqn = b.AssemblyQualifiedName;
				b = b.BaseType;

				if (aqn == parent.AssemblyQualifiedName)
					return true;
			}

			return false;
		}

		private bool	AreSame(Type a, Type b)
		{
			if (a == b)
				return true; // Either both are null or they are the same type

			if (a == null || b == null)
				return false;

			if (a.IsSubclassOf(b) || b.IsSubclassOf(a))
				return true; // One inherits from the other

			return a.BaseType == b.BaseType; // They have the same immediate parent
		}

		public class ProxyDomain : MarshalByRefObject
		{
			public void	GetAssembly(string dllPath)
			{
				try
				{
					AppDomainSetup	domaininfo = new AppDomainSetup();
					Evidence		adevidence = AppDomain.CurrentDomain.Evidence;
					domaininfo.ApplicationBase = Path.GetDirectoryName(dllPath);
					domaininfo.DisallowApplicationBaseProbing = true;
					AppDomain		domain = AppDomain.CreateDomain("NGTools", adevidence, domaininfo);

					// After this loading in this temporary AppDomain, the next LoadFile from the executing AppDomain will successfuly load the right assembly.
					// Don't ask me how, I don't even know...
					domain.Load("UnityEngine");
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException(ex.Message);
				}
			}
		}
	}
}