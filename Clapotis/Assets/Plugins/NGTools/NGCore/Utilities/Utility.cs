using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
#if UNITY_WSA_8_1 || UNITY_WP_8_1
using WinRTLegacy;
#endif

namespace NGTools
{
	using UnityEngine;

	public static class Utility
	{
		public const BindingFlags	ExposedBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		
		#region Buffer tools
		private static Stack<ByteBuffer>	poolBBuffers = new Stack<ByteBuffer>(2);
		private static Stack<StringBuilder>	poolBuffers = new Stack<StringBuilder>(2);

		public static StringBuilder	GetBuffer(string initialValue)
		{
			StringBuilder	b = GetBuffer();
			b.Append(initialValue);
			return b;
		}

		public static StringBuilder	GetBuffer()
		{
			if (Utility.poolBuffers.Count > 0)
				return Utility.poolBuffers.Pop();
			return new StringBuilder(64);
		}

		public static void			RestoreBuffer(StringBuilder buffer)
		{
			buffer.Length = 0;
			Utility.poolBuffers.Push(buffer);
		}

		public static string		ReturnBuffer(StringBuilder buffer)
		{
			string	result = buffer.ToString();
			buffer.Length = 0;
			Utility.poolBuffers.Push(buffer);
			return result;
		}

		public static ByteBuffer	GetBBuffer(byte[] initialValue)
		{
			ByteBuffer	b = GetBBuffer();
			b.Append(initialValue);
			return b;
		}

		public static ByteBuffer	GetBBuffer()
		{
			if (Utility.poolBBuffers.Count > 0)
				return Utility.poolBBuffers.Pop();
			return new ByteBuffer(64);
		}

		public static void			RestoreBBuffer(ByteBuffer buffer)
		{
			buffer.Clear();
			Utility.poolBBuffers.Push(buffer);
		}

		public static byte[]		ReturnBBuffer(ByteBuffer buffer)
		{
			byte[]	result = buffer.Flush();
			buffer.Clear();
			Utility.poolBBuffers.Push(buffer);
			return result;
		}
		#endregion

		private static Assembly[]	allAssemblies;
		private static Type[][]		allTypes;
		private static Type[]		executingTypes;

		private static void	InitAssemblies()
		{
			// Look into editor assemblies.
#if NETFX_CORE
			var taskAssemblies = UWPFakeExtension.GetAssemblies();
			taskAssemblies.Wait();
			Assembly[]	editorAssemblies = taskAssemblies.Result;
#else
			Assembly[]	editorAssemblies = AppDomain.CurrentDomain.GetAssemblies();
#endif

			List<Assembly>	assemblies = new List<Assembly>(editorAssemblies.Length + 1) { typeof(MonoBehaviour).Assembly() };
			List<Type[]>	types = new List<Type[]>(editorAssemblies.Length + 1) { typeof(MonoBehaviour).Assembly().GetTypes() };
#if NETFX_CORE
			Assembly		executingAssembly = typeof(MonoBehaviour).Assembly();
#else
			Assembly		executingAssembly = Assembly.GetExecutingAssembly();
#endif

			for (int i = 0; i < editorAssemblies.Length; i++)
			{
				if (editorAssemblies[i] == assemblies[0])
					continue;

				try
				{
					Type[]	asmTypes = editorAssemblies[i].GetTypes();

					assemblies.Add(editorAssemblies[i]);
					types.Add(asmTypes);

					if (editorAssemblies[i] == executingAssembly)
						Utility.executingTypes = asmTypes;
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogFileException("Assembly " + editorAssemblies[i] + " failed.", ex);
				}
			}

			Utility.allAssemblies = assemblies.ToArray();
			Utility.allTypes = types.ToArray();
		}

		public static IEnumerable<Type>	EachAssignableFrom(Type baseType, Func<Type, bool> match = null)
		{
			if (Utility.executingTypes == null)
				Utility.InitAssemblies();

			for (int i = 0; i < Utility.executingTypes.Length; i++)
			{
				if (baseType.IsAssignableFrom(Utility.executingTypes[i]) == true &&
#if NETFX_CORE
					Utility.executingTypes[i] != baseType &&
#else
					Utility.executingTypes[i].UnderlyingSystemType != baseType &&
#endif
					(match == null || match(Utility.executingTypes[i]) == true))
				{
					yield return Utility.executingTypes[i];
				}
			}
		}

		public static IEnumerable<Type>	EachAllAssignableFrom(Type baseType)
		{
			if (Utility.allAssemblies == null)
				Utility.InitAssemblies();

			for (int i = 0; i < Utility.allAssemblies.Length; i++)
			{
				for (int j = 0; j < Utility.allTypes[i].Length; j++)
				{
					if (baseType.IsAssignableFrom(Utility.allTypes[i][j]) == true &&
#if NETFX_CORE
						Utility.allTypes[i][j] != baseType
#else
						Utility.allTypes[i][j].UnderlyingSystemType != baseType
#endif
						)
					{
						yield return Utility.allTypes[i][j];
					}
				}
			}
		}

		public static IEnumerable<Type>	EachSubClassesOf(Type baseType, Func<Type, bool> match = null)
		{
			if (Utility.executingTypes == null)
				Utility.InitAssemblies();

			for (int i = 0; i < Utility.executingTypes.Length; i++)
			{
				if (Utility.executingTypes[i].IsSubclassOf(baseType) == true &&
					(match == null || match(Utility.executingTypes[i]) == true))
				{
					yield return Utility.executingTypes[i];
				}
			}
		}

		public static IEnumerable<Type>	EachAllSubClassesOf(Type baseType)
		{
			if (Utility.allAssemblies == null)
				Utility.InitAssemblies();

			for (int i = 0; i < Utility.allAssemblies.Length; i++)
			{
				foreach (Type t in Utility.allTypes[i])
				{
					if (t.IsSubclassOf(baseType) == true)
						yield return t;
				}
			}
		}

		public static bool	CanExposeFieldInInspector(FieldInfo field)
		{
			if ((field.IsPublic == false && field.IsDefined(typeof(SerializeField), false) == false) ||
				field.IsDefined(typeof(NonSerializedAttribute), false) == true ||
				field.IsDefined(typeof(HideInInspector), false) == true ||
				Utility.CanExposeTypeInInspector(field.FieldType) == false)
			{
				return false;
			}

			return true;
		}

		public static bool	CanExposePropertyInInspector(PropertyInfo property)
		{
			if (property.IsDefined(typeof(NonSerializedAttribute), false) == true ||
				property.IsDefined(typeof(HideInInspector), false) == true ||
				property.GetIndexParameters().Length != 0 || // Skip indexer.
				property.CanRead == false || // Skip prop without both get/set.
				property.CanWrite == false ||
				Utility.CanExposeTypeInInspector(property.PropertyType) == false)
			{
				return false;
			}

			return true;
		}

		public static bool	CanExposeTypeInInspector(Type type)
		{
			if (type.IsPrimitive() == true || // Any primitive types (int, float, byte, etc... not struct or decimal).
				type == typeof(string) || // Built-in types.
				type.IsEnum() == true ||
				type == typeof(Rect) ||
				type == typeof(Vector3) ||
				type == typeof(Color) ||
				type == typeof(Bounds) ||
				typeof(Object).IsAssignableFrom(type) == true || // Unity Object.
				type == typeof(Object) ||
				type == typeof(Vector2) ||
				type == typeof(Vector4) ||
				type == typeof(Quaternion) ||
				type == typeof(AnimationCurve))
			{
				return true;
			}

			if (type.IsInterface() == true)
				return false;

			if (typeof(Delegate).IsAssignableFrom(type) == true)
				return false;

			if (type.GetInterface(typeof(IList<>).Name) != null) // IList<> or Array with Serializable elements.
			{
				Type	subType = type.GetInterface(typeof(IList<>).Name).GetGenericArguments()[0];

				// Nested list or array is not supported.
				if (subType.GetInterface(typeof(IList<>).Name) == null)
					return Utility.CanExposeTypeInInspector(subType);
				return false;
			}

			if (typeof(IList).IsAssignableFrom(type) == true) // IList with Serializable elements.
			{
				return false;
				//PropertyInfo prop = type.GetProperty("Item", new Type[] { typeof(int) });
				//return (prop != null && Utility.CanExposeInInspector(prop.PropertyType) == true);
			}

			if (type.IsGenericType() && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) // Especially, we prevent Dictionary to be exposed.
				return false;

			// We need to check if type is a class or struct even after all the checks before. Because types like Decimal have Serializable and are not primitive. Thank you CLR and 64bits implementation limitation.
			if (type != typeof(Decimal) &&
				(type.IsClass() == true || // A class.
				 type.IsStruct() == true) && // Or a struct.
				type.IsSerializable == true) // With SerializableAttribute.
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Gets element's type of array supported by Unity Inspector.
		/// </summary>
		/// <param name="arrayType"></param>
		/// <returns></returns>
		/// <seealso cref="TypeExtension.IsUnityArray"/>
		public static Type	GetArraySubType(Type arrayType)
		{
			if (arrayType.IsArray == true)
				return arrayType.GetElementType();

			Type	@interface = arrayType.GetInterface(typeof(IList<>).Name);
			if (@interface != null) // IList<> with Serializable elements.
				return @interface.GetGenericArguments()[0];

			return null;
		}

		public static IEnumerable<FieldInfo>	EachFieldHierarchyOrdered(Type type, Type stopType, BindingFlags flags)
		{
			Stack<Type>	inheritances = new Stack<Type>();

			inheritances.Push(type);

			if (type.BaseType != null)
			{
				while (type.BaseType != stopType)
				{
					inheritances.Push(type.BaseType);
					type = type.BaseType;
				}
			}

			foreach (Type t in inheritances)
			{
				FieldInfo[]	fields = t.GetFields(flags | BindingFlags.DeclaredOnly);

				for (int i = 0; i < fields.Length; i++)
					yield return fields[i];
			}
		}

		public static List<FieldInfo>	GetFieldsHierarchyOrdered(Type t, Type stopType, BindingFlags flags)
		{
			Stack<Type>		inheritances = new Stack<Type>();
			List<FieldInfo>	fields = new List<FieldInfo>();

			inheritances.Push(t);
			while (t.BaseType() != stopType)
			{
				inheritances.Push(t.BaseType());
				t = t.BaseType();
			}

			foreach (Type type in inheritances)
				fields.AddRange(type.GetFields(flags | BindingFlags.DeclaredOnly));

			return fields;
		}

		public static float		RelativeAngle(Vector3 fwd, Vector3 targetDir, Vector3 upDir)
		{
			float	angle = Vector3.Angle(fwd, targetDir);

			if (Mathf.Approximately(Utility.AngleDirection(fwd, targetDir, upDir), -1F) == true)
				return -angle;
			else
				return angle;
		}
		
		public static float		RelativeAngle(Vector2 fwd, Vector2 targetDir, Vector3 upDir)
		{
			float	angle = Vector2.Angle(fwd, targetDir);

			if (Mathf.Approximately(Utility.AngleDirection(fwd, targetDir, upDir), -1F) == true)
				return -angle;
			else
				return angle;
		}

		public static float		AngleDirection(Vector3 fwd, Vector3 targetDir, Vector3 up)
		{
			Vector3	perp = Vector3.Cross(fwd, targetDir);
			float	dir = Vector3.Dot(perp, up);

			if (dir > 0F)
				return 1F;
			else if (dir < 0F)
				return -1F;
			else
				return 0F;
		}

		public static float		AngleDirection(Vector2 fwd, Vector2 targetDir, Vector3 up)
		{
			Vector3	perp = Vector3.Cross(new Vector3(fwd.x, 0f, fwd.y),
										 new Vector3(targetDir.x, 0f, targetDir.y));
			float	dir = Vector3.Dot(perp, up);

			if (dir > 0F)
				return 1F;
			else if (dir < 0F)
				return -1F;
			else
				return 0F;
		}

		/// <summary>
		/// Defines if a type is an array supported by Unity inspector.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public static bool	IsUnityArray(this Type t)
		{
			return t.IsArray == true || typeof(IList).IsAssignableFrom(t) == true || t.GetInterface(typeof(IList<>).Name) != null;
		}

		public static bool	IsStruct(this Type t)
		{
			return t.IsValueType() == true && t.IsPrimitive() == false && t.IsEnum() == false && t != typeof(Decimal);
		}

		private static Dictionary<Type, string>	cachedShortAssemblyTypes;

		public static string	GetShortAssemblyType(this Type t)
		{
			if (Utility.cachedShortAssemblyTypes == null)
				Utility.cachedShortAssemblyTypes = new Dictionary<Type, string>(32);

			string	shortAssemblyType;

			if (Utility.cachedShortAssemblyTypes.TryGetValue(t, out shortAssemblyType) == true)
				return shortAssemblyType;

			if (t.IsGenericType() == true)
			{
				StringBuilder	buffer = new StringBuilder();
				Type			declaringType = t.DeclaringType;
				Type[]			types = t.GetGenericArguments();

				buffer.Append(t.Namespace);
				buffer.Append('.');

				while (declaringType != null)
				{
					buffer.Append(declaringType.Name);

					declaringType = declaringType.DeclaringType;
					buffer.Append('+');
				}

				buffer.Append(t.Name);
				buffer.Append("[");

				for (int i = 0; i < types.Length; i++)
				{
					if (i > 0)
						buffer.Append(',');

					buffer.Append("[");
					buffer.Append(types[i].GetShortAssemblyType());
					buffer.Append("]");
				}

				buffer.Append("]");

				if (t.Module().Name.StartsWith("mscorlib.dll") == false)
				{
					buffer.Append(',');
					buffer.Append(t.Module().Name.Substring(0, t.Module().Name.Length - ".dll".Length));
				}

				return buffer.ToString();
			}

			if (t.Module().Name.StartsWith("mscorlib.dll") == true)
				return t.ToString();
			return t.FullName + "," + t.Module().Name.Substring(0, t.Module().Name.Length - ".dll".Length);
		}

		/// <summary>
		/// Looks for a field or a property using <paramref name="name"/> in <paramref name="type"/>.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="name">The name of a field or a property.</param>
		/// <returns></returns>
		/// <exception cref="System.MissingFieldException"></exception>
		public static IFieldModifier	GetFieldInfo(Type type, string name)
		{
			FieldInfo	fieldInfo = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			if (fieldInfo != null)
				return new FieldModifier(fieldInfo);

			PropertyInfo	propertyInfo = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			if (propertyInfo != null)
				return new PropertyModifier(propertyInfo);

#if UNITY_WSA_8_1 || UNITY_WP_8_1
			throw new MissingMemberException("Field or property \"" + name + "\" was not found in type \"" + type.Name + "\".");
#else
			throw new MissingFieldException("Field or property \"" + name + "\" was not found in type \"" + type.Name + "\".");
#endif
		}

		private static List<ICollectionModifier>	cachedCollectionModifiers = new List<ICollectionModifier>();

		public static ICollectionModifier	GetCollectionModifier(object rawArray)
		{
			if (rawArray == null)
				return null;

			Array	array = rawArray as Array;

			if (array != null)
			{
				for (int i = 0; i < Utility.cachedCollectionModifiers.Count; i++)
				{
					ArrayModifier	instance = Utility.cachedCollectionModifiers[i] as ArrayModifier;

					if (instance != null)
					{
						instance.subType = null;
						instance.array = array;
						Utility.cachedCollectionModifiers.RemoveAt(i);
						return instance;
					}
				}

				return new ArrayModifier(array);
			}

			IList	list = rawArray as IList;

			if (list != null)
			{
				for (int i = 0; i < Utility.cachedCollectionModifiers.Count; i++)
				{
					ListModifier	instance = Utility.cachedCollectionModifiers[i] as ListModifier;

					if (instance != null)
					{
						instance.subType = null;
						instance.list = list;
						Utility.cachedCollectionModifiers.RemoveAt(i);
						return instance;
					}
				}

				return new ListModifier(list);
			}

			throw new Exception("Collection of type \"" + rawArray.GetType() + "\" is not supported.");
		}

		public static void	ReturnCollectionModifier(ICollectionModifier modifier)
		{
			Utility.cachedCollectionModifiers.Add(modifier);
		}

		public static bool	IsComponentEnableable(Component component)
		{
			if (component is Transform)
				return false;

			if (component is Behaviour)
			{
				Type	componentType = component.GetType();

				if (componentType.GetMethod("OnEnable", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null ||
					componentType.GetMethod("OnDisable", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null ||
					componentType.GetMethod("OnGUI", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null ||
					componentType.GetMethod("Start", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null ||
					componentType.GetMethod("Update", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null ||
					componentType.GetMethod("FixedUpdate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null ||
					componentType.GetMethod("LateUpdate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null)
				{
					return true;
				}
				else
					return false;
			}
			else
			{
				// UI types does not follow the same pattern...
				//if (component is Component)
					return true;

				//Type	componentType = component.GetType();

				//if (componentType.GetProperty("enabled", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null)
				//	return true;
				//else
				//	return false;
			}
		}

		public static bool	Contains<T>(this T[] a, T b)
		{
			for (int i = 0; i < a.Length; i++)
			{
				if (EqualityComparer<T>.Default.Equals(a[i], b) == true)
					return true;
			}

			return false;
		}

		public static string	NicifyVariableName(string name)
		{
			if (string.IsNullOrEmpty(name) == true)
				return name;

			StringBuilder	buffer = Utility.GetBuffer();
			int				i = 0;

			if (name.Length > 2 && ((name[0] >= 'a' && name[0] <= 'z') || (name[0] >= 'A' && name[0] <= 'Z')) && name[1] == '_')
				i = 2;

			while ((name[i] < 'A' || name[i] > 'Z') &&
				   (name[i] < 'a' || name[i] > 'z') &&
				   (name[i] < '0' || name[i] > '9'))
			{
				++i;
			}

			if (name[i] >= 'a' && name[i] <= 'z')
			{
				buffer.Append((char)(name[i] + 'A' - 'a'));
				++i;
			}

			for (; i < name.Length; i++)
			{
				if (name[i] >= '1' && name[i] <= '9')
				{
					if (i > 0 && name[i - 1] >= 'a' && name[i - 1] <= 'z')
						buffer.Append(' ');
				}
				else if (name[i] >= 'A' && name[i] <= 'Z')
				{
					if (i + 1 == name.Length) // Last letter
					{
						if (i > 0 && name[i - 1] >= 'a' && name[i - 1] <= 'z')
							buffer.Append(' ');
					}
					else if (i > 1)
					{
						if (name[i + 1] >= 'a' && name[i + 1] <= 'z')
						{
							if (((name[i - 1] >= 'a' && name[i - 1] <= 'z') || (name[i - 1] >= 'A' && name[i - 1] <= 'Z') || (name[i - 1] >= '0' && name[i - 1] <= '9')))
								buffer.Append(' ');
						}
						else if (name[i - 1] >= 'a' && name[i - 1] <= 'z')
							buffer.Append(' ');
					}
				}

				buffer.Append(name[i]);
			}

			return Utility.ReturnBuffer(buffer);
		}
	}
}