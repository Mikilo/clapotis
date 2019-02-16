using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NGTools.UON
{
	internal static class UONUtility
	{
		public static readonly string[]	cachedHexaStrings = new string[256];

		public static string	ToHex(this byte input)
		{
			if (UONUtility.cachedHexaStrings[input] == null)
			{
				if (input <= 16)
					UONUtility.cachedHexaStrings[input] = ((char)(input > 9 ? input + 0x37 + 0x20 : input + 0x30)).ToString();
				else
				{
					char[]	c = new char[2];
					byte	b;
			
					b = ((byte)(input >> 4));
					c[0] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);

					b = ((byte)(input & 0x0F));
					c[1] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);
					UONUtility.cachedHexaStrings[input] = new string(c);
				}
			}

			return UONUtility.cachedHexaStrings[input];
		}

		public static byte		HexToByte(this string str)
		{
			if (str.Length == 0 || str.Length > 2)
				return 0;

			byte	buffer = 0;
			char	c;

			// Convert first half of byte
			c = str[0];
			buffer = (byte)(c > '9' ? (c > 'Z' ? (c - 'a' + 10) : (c - 'A' + 10)) : (c - '0'));

			if (str.Length > 1)
			{
				buffer <<= 4;

				// Convert second half of byte
				c = str[1];
				buffer |= (byte)(c > '9' ? (c > 'Z' ? (c - 'a' + 10) : (c - 'A' + 10)) : (c - '0'));
			}

			return buffer;
		}

		public static string	ToHex(this sbyte input)
		{
			return UONUtility.ToHex((byte)input);
		}

		public static sbyte		HexToSByte(this string str)
		{
			return (sbyte)UONUtility.HexToByte(str);
		}

		public static int		IndexOf(this StringBuilder buffer, string needle)
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				if (buffer[i] == needle[0])
				{
					int	j = 1;

					++i;

					for (; j < needle.Length && i < buffer.Length; j++, ++i)
					{
						if (needle[j] != buffer[i])
							break;
					}

					if (j == needle.Length)
						return i - j;
				}
			}

			return -1;
		}

		public static bool IsStruct(this Type t)
		{
			return t.IsValueType() == true && t.IsPrimitive() == false && t.IsEnum() == false && t != typeof(Decimal);
		}

		private static Dictionary<Type, string>	cachedShortAssemblyTypes;

		public static string	GetShortAssemblyType(this Type t)
		{
			if (UONUtility.cachedShortAssemblyTypes == null)
				UONUtility.cachedShortAssemblyTypes = new Dictionary<Type, string>(32);

			string	shortAssemblyType;

			if (UONUtility.cachedShortAssemblyTypes.TryGetValue(t, out shortAssemblyType) == true)
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

		#region Buffer tools
		private static Stack<StringBuilder>	poolBuffers = new Stack<StringBuilder>(2);

		public static StringBuilder	GetBuffer(string initialValue)
		{
			StringBuilder	b = GetBuffer();
			b.Append(initialValue);
			return b;
		}

		public static StringBuilder	GetBuffer()
		{
			if (UONUtility.poolBuffers.Count > 0)
				return UONUtility.poolBuffers.Pop();
			return new StringBuilder(64);
		}

		public static void			RestoreBuffer(StringBuilder buffer)
		{
			buffer.Length = 0;
			UONUtility.poolBuffers.Push(buffer);
		}

		public static string		ReturnBuffer(StringBuilder buffer)
		{
			string	result = buffer.ToString();
			buffer.Length = 0;
			UONUtility.poolBuffers.Push(buffer);
			return result;
		}
		#endregion
#if NETFX_CORE
		private static Type[]		executingTypes = typeof(UnityEngine.MonoBehaviour).Assembly().GetTypes();
#else
		private static Type[]		executingTypes = Assembly.GetExecutingAssembly().GetTypes();
#endif
		private static List<object>	tempList = UONUtility.tempList = new List<object>();

		public static T[]				CreateInstancesOf<T>(params object[] args) where T : class
		{
			UONUtility.tempList.Clear();

			foreach (Type type in UONUtility.EachSubClassesOf(typeof(T)))
				UONUtility.tempList.Add(Activator.CreateInstance(type, args));

			T[]	result = new T[UONUtility.tempList.Count];

			for (int i = 0; i < UONUtility.tempList.Count; i++)
				result[i] = UONUtility.tempList[i] as T;

			return result;
		}

		public static IEnumerable<Type>	EachSubClassesOf(Type baseType, Func<Type, bool> match = null)
		{
			for (int i = 0; i < UONUtility.executingTypes.Length; i++)
			{
				if (UONUtility.executingTypes[i].IsSubclassOf(baseType) == true &&
					(match == null || match(UONUtility.executingTypes[i]) == true))
				{
					yield return UONUtility.executingTypes[i];
				}
			}
		}

		public static List<FieldInfo>			GetFieldsHierarchyOrdered(Type t, Type stopType, BindingFlags flags)
		{
			var	inheritances = new Stack<Type>();
			var	fields = new List<FieldInfo>();

			inheritances.Push(t);
			if (t.BaseType() != null)
			{
				while (t.BaseType() != stopType)
				{
					inheritances.Push(t.BaseType());
					t = t.BaseType();
				}
			}

			foreach (var i in inheritances)
				fields.AddRange(i.GetFields(flags | BindingFlags.DeclaredOnly));

			return fields;
		}

		public static IEnumerable<FieldInfo>	EachFieldHierarchyOrdered(Type t, Type stopType, BindingFlags flags)
		{
			var	inheritances = new Stack<Type>();

			inheritances.Push(t);

			if (t.BaseType() != null)
			{
				while (t.BaseType() != stopType)
				{
					inheritances.Push(t.BaseType());
					t = t.BaseType();
				}
			}

			foreach (var type in inheritances)
			{
				FieldInfo[]	fields = type.GetFields(flags | BindingFlags.DeclaredOnly);

				for (int i = 0; i < fields.Length; i++)
					yield return fields[i];
			}
		}

		public static Type	GetArraySubType(Type arrayType)
		{
			if (arrayType.IsArray == true)
				return arrayType.GetElementType();

			Type	@interface = arrayType.GetInterface(typeof(IList<>).Name);
			if (@interface != null) // IList<> with Serializable elements.
				return @interface.GetGenericArguments()[0];

			return null;
		}
	}
}