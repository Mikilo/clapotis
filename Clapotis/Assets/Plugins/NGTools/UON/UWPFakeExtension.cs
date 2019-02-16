using System;
using System.Reflection;
#if NETFX_CORE
using System.Threading.Tasks;
using Windows.Storage;
#endif

namespace NGTools.UON
{
	public static class UWPFakeExtension
	{
#if NETFX_CORE
		public static async Task<Assembly[]>	GetAssemblies()
		{
			StorageFolder	folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
			List<Assembly>	assemblies = new List<Assembly>();

			foreach (StorageFile file in await folder.GetFilesAsync())
			{
				if (file.FileType == ".dll" || file.FileType == ".exe")
				{
					AssemblyName	name = new AssemblyName()
					{
						Name = Path.GetFileNameWithoutExtension(file.Name)
					};
					Assembly	asm = System.Reflection.Assembly.Load(name);
					assemblies.Add(asm);
				}
			}

			return assemblies.ToArray();
		}
#endif

		public static Assembly	Assembly(this Type t)
		{
#if NETFX_CORE
			return t.GetTypeInfo().Assembly;
#else
			return t.Assembly;
#endif
		}

		public static Type	BaseType(this Type t)
		{
#if NETFX_CORE
			return t.GetTypeInfo().BaseType;
#else
			return t.BaseType;
#endif
		}

		public static bool	IsInterface(this Type t)
		{
#if NETFX_CORE
			return t.GetTypeInfo().IsInterface;
#else
			return t.IsInterface;
#endif
		}

		public static bool	IsPrimitive(this Type t)
		{
#if NETFX_CORE
			return t.GetTypeInfo().IsPrimitive;
#else
			return t.IsPrimitive;
#endif
		}

		public static bool	IsGenericType(this Type t)
		{
#if NETFX_CORE
			return t.GetTypeInfo().IsGenericType;
#else
			return t.IsGenericType;
#endif
		}

		public static bool	IsEnum(this Type t)
		{
#if NETFX_CORE
			return t.GetTypeInfo().IsEnum;
#else
			return t.IsEnum;
#endif
		}

		public static bool	IsClass(this Type t)
		{
#if NETFX_CORE
			return t.GetTypeInfo().IsClass;
#else
			return t.IsClass;
#endif
		}

		public static bool	IsValueType(this Type t)
		{
#if NETFX_CORE
			return t.GetTypeInfo().IsValueType;
#else
			return t.IsValueType;
#endif
		}

#if NETFX_CORE
		public static bool	IsSubclassOf(this Type t, Type subType)
		{
			return t.GetTypeInfo().IsSubclassOf(subType);
		}

		public static Type	GetInterface(this Type t, string name)
		{
			return t.GetTypeInfo().ImplementedInterfaces.First(e => e.Name == name);
		}

		public static Type[]	GetGenericArguments(this Type t)
		{
			return t.GetTypeInfo().GenericTypeArguments;
		}

		public static object[]	GetCustomAttributes(this Type t, Type attributeType, bool inherit)
		{
			return t.GetTypeInfo().GetCustomAttributes(attributeType, inherit).ToArray();
		}

		public static object	GetRawConstantValue(this FieldInfo f)
		{
			return f.GetValue(null);
		}
#else
		public static object	GetValue(this PropertyInfo p, object instance)
		{
			return p.GetValue(instance, BindingFlags.Default, null, null, null);
		}

		public static void		SetValue(this PropertyInfo p, object instance, object value)
		{
			p.SetValue(instance, value, BindingFlags.Default, null, null, null);
		}
#endif
		public static Module	Module(this Type t)
		{
#if NETFX_CORE
			return t.GetTypeInfo().Module;
#else
			return t.Module;
#endif
		}

#if UNITY_WSA_8_1 || UNITY_WP_8_1
		public static string	GetString(this System.Text.Encoding e, byte[] input)
		{
			return e.GetString(input, 0, input.Length);
		}

		public static FieldInfo[]	GetFields(this Type type)
		{
			return type.GetRuntimeFields().ToArray();
		}

		public static FieldInfo[]	GetFields(this Type type, BindingFlags bindingFlags)
		{
			return type.GetRuntimeFields().ToArray();
		}

		public static FieldInfo		GetField(this Type type, string name, BindingFlags bindingFlags)
		{
			return type.GetRuntimeField(name);
		}

		public static PropertyInfo[]	GetProperties(this Type type, BindingFlags bindingFlags)
		{
			return type.GetRuntimeProperties().ToArray();
		}

		public static PropertyInfo	GetProperty(this Type type, string name)
		{
			return type.GetTypeInfo().GetDeclaredProperty(name);
		}

		public static PropertyInfo		GetProperty(this Type type, string name, BindingFlags bindingFlags)
		{
			return type.GetRuntimeProperty(name);
		}

		public static MethodInfo		GetGetMethod(this PropertyInfo property, bool nonPublic)
		{
			return property.GetMethod;
		}

		public static MethodInfo		GetSetMethod(this PropertyInfo property, bool nonPublic)
		{
			return property.SetMethod;
		}

		public static MethodInfo[]	GetMethods(this Type type, BindingFlags bindingFlags)
		{
			return type.GetRuntimeMethods().ToArray();
		}

		public static bool	IsAssignableFrom(this Type type, Type subType)
		{
			return type.GetTypeInfo().IsAssignableFrom(subType.GetTypeInfo());
		}

		public static Type[]	GetTypes(this Assembly assembly)
		{
			return assembly.DefinedTypes.Select<TypeInfo, Type>(e => e.AsType()).ToArray();
		}
#endif
	}
}