using NGTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditorInternal;
using InnerUtility = NGTools.Utility;

namespace NGToolsEditor
{
	using UnityEngine;

	public static class	Utility
	{
		private static string	unityVersion;
		public static string	UnityVersion { get { return unityVersion ?? (unityVersion = Application.unityVersion); } }

		static	Utility()
		{
			ServicePointManager.ServerCertificateValidationCallback = Utility.Validator;
		}

		private static bool	Validator(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}

		public static Color	GetSkinColor(float r, float g, float b, float a, float r2, float g2, float b2, float a2)
		{
			try
			{
				return EditorGUIUtility.isProSkin == true ? new Color(r, g, b, a) : new Color(r2, g2, b2, a2);
			}
			catch
			{
			}

			return new Color(r2, g2, b2, a2);
		}

		private static string[]	GenerateCachedNumbers(int max)
		{
			string[]	array = new string[max];

			for (int i = 0; i < max; i++)
				array[i] = i.ToString();

			return array;
		}

		#region Shared variables
		public const string			DragObjectDataName = "i";
		public static Vector2		position2D = new Vector2();
		public static GUIContent	content = new GUIContent();
		public static string[]		cachedNumbers = Utility.GenerateCachedNumbers(256);

		public static string		ToCachedString(this int i)
		{
			if (0 <= i && i < Utility.cachedNumbers.Length)
				return Utility.cachedNumbers[i];
			return i.ToString();
		}
		#endregion

		#region Buffer tools
		public static StringBuilder	GetBuffer(string initialValue)
		{
			return InnerUtility.GetBuffer(initialValue);
		}

		public static StringBuilder	GetBuffer()
		{
			return InnerUtility.GetBuffer();
		}

		public static void			RestoreBuffer(StringBuilder buffer)
		{
			InnerUtility.RestoreBuffer(buffer);
		}

		public static string		ReturnBuffer(StringBuilder buffer)
		{
			return InnerUtility.ReturnBuffer(buffer);
		}

		public static ByteBuffer	GetBBuffer(byte[] initialValue)
		{
			return InnerUtility.GetBBuffer(initialValue);
		}

		public static ByteBuffer	GetBBuffer()
		{
			return InnerUtility.GetBBuffer();
		}

		public static void			RestoreBBuffer(ByteBuffer buffer)
		{
			InnerUtility.RestoreBBuffer(buffer);
		}

		public static byte[]		ReturnBBuffer(ByteBuffer buffer)
		{
			return InnerUtility.ReturnBBuffer(buffer);
		}
		#endregion

		public static Type	GetArraySubType(Type arrayType)
		{
			return InnerUtility.GetArraySubType(arrayType);
		}

		private static List<object>	tempList;

		public static T[]				CreateInstancesOf<T>(params object[] args) where T : class
		{
			return Utility.CreateInstancesFromEnumerable<T>(Utility.EachAssignableFrom(typeof(T)), args);
		}

		public static T[]				CreateNGTInstancesOf<T>(params object[] args) where T : class
		{
			return Utility.CreateInstancesFromEnumerable<T>(Utility.EachNGTAssignableFrom(typeof(T)), args);
		}

		public static T[]				CreateAllInstancesOf<T>(params object[] args) where T : class
		{
			return Utility.CreateInstancesFromEnumerable<T>(Utility.EachAllAssignableFrom(typeof(T)), args);
		}

		public static T[]				CreateInstancesFromEnumerable<T>(IEnumerable<Type> en, params object[] args) where T : class
		{
			if (Utility.tempList == null)
				Utility.tempList = new List<object>();

			Utility.tempList.Clear();

			foreach (Type type in en)
			{
				try
				{
					Utility.tempList.Add(Activator.CreateInstance(type, args));
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException(ex);
				}
			}

			T[]	result = new T[Utility.tempList.Count];

			for (int i = 0; i < Utility.tempList.Count; i++)
				result[i] = Utility.tempList[i] as T;

			return result;
		}

		public static Type[]			GetSubClassesOf(Type baseType)
		{
			if (Utility.allAssemblies == null)
				Utility.InitializeAssemblies();

			List<Type>	result = new List<Type>();

			try
			{

				if (Utility.allTypes[Utility.executingTypes] == null)
					Utility.allTypes[Utility.executingTypes] = Utility.allAssemblies[Utility.executingTypes].GetTypes();
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException("GetSubClassesOf failed on Assembly " + Utility.allAssemblies[Utility.executingTypes], ex);
				Utility.allTypes[Utility.executingTypes] = new Type[0];
			}

			Type[]	types = Utility.allTypes[Utility.executingTypes];

			for (int i = 0; i < types.Length; i++)
			{
				if (types[i].IsSubclassOf(baseType) == true)
					result.Add(types[i]);
			}

			return result.ToArray();
		}

		public static Type[]			GetAllSubClassesOf(Type baseType)
		{
			if (Utility.allAssemblies == null)
				Utility.InitializeAssemblies();

			List<Type>	result = new List<Type>();

			for (int i = 0; i < Utility.allAssemblies.Length; i++)
			{
				try
				{
					if (Utility.allTypes[i] == null)
						Utility.allTypes[i] = Utility.allAssemblies[i].GetTypes();
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException("GetAllSubClassesOf failed on Assembly " + Utility.allAssemblies[i], ex);
					Utility.allTypes[i] = new Type[0];
					continue;
				}

				foreach (Type t in Utility.allTypes[i])
				{
					if (t.IsSubclassOf(baseType) == true && result.Contains(t) == false)
						result.Add(t);
				}
			}

			return result.ToArray();
		}

		private static readonly Dictionary<Type, Type[]>	cachedEachAssignableFrom = new Dictionary<Type, Type[]>();

		public static IEnumerable<Type>	EachAssignableFrom(Type baseType, Func<Type, bool> match = null)
		{
			if (Utility.allAssemblies == null)
				Utility.InitializeAssemblies();

			Type[]	types;

			if (match == null && Utility.cachedEachAssignableFrom.TryGetValue(baseType, out types) == true)
			{
				for (int i = 0; i < types.Length; i++)
					yield return types[i];

				yield break;
			}

			List<Type>	list;

			if (Utility.poolTypes.Count > 0)
			{
				list = Utility.poolTypes.Pop();
				list.Clear();
			}
			else
				list = new List<Type>(32);

			try
			{
				if (Utility.allTypes[Utility.executingTypes] == null)
					Utility.allTypes[Utility.executingTypes] = Utility.allAssemblies[Utility.executingTypes].GetTypes();
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException("EachAssignableFrom failed on Assembly " + Utility.allAssemblies[Utility.executingTypes], ex);
				Utility.allTypes[Utility.executingTypes] = new Type[0];
			}

			types = Utility.allTypes[Utility.executingTypes];

			for (int i = 0; i < types.Length; i++)
			{
				if ((baseType == typeof(object) || baseType.IsAssignableFrom(types[i]) == true) &&
					types[i].UnderlyingSystemType != baseType &&
					(match == null || match(types[i]) == true))
				{
					list.Add(types[i]);
					yield return types[i];
				}
			}

			if (match == null)
			{
				if (Utility.cachedEachAssignableFrom.ContainsKey(baseType) == false)
					Utility.cachedEachAssignableFrom.Add(baseType, list.ToArray());
			}

			Utility.poolTypes.Push(list);
		}

		private static readonly Dictionary<Type, Type[]>	cachedEachAllAssignableFrom = new Dictionary<Type, Type[]>();

		public static IEnumerable<Type>	EachAllAssignableFrom(Type baseType, Func<Type, bool> match = null)
		{
			if (Utility.allAssemblies == null)
				Utility.InitializeAssemblies();

			Type[]	types;

			if (match == null && Utility.cachedEachAllAssignableFrom.TryGetValue(baseType, out types) == true)
			{
				for (int i = 0; i < types.Length; i++)
					yield return types[i];

				yield break;
			}

			List<Type>	list;

			if (Utility.poolTypes.Count > 0)
			{
				list = Utility.poolTypes.Pop();
				list.Clear();
			}
			else
				list = new List<Type>(32);

			for (int i = 0; i < Utility.allAssemblies.Length; i++)
			{
				try
				{
					if (Utility.allTypes[i] == null)
						Utility.allTypes[i] = Utility.allAssemblies[i].GetTypes();
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException("EachAllAssignableFrom failed on Assembly " + Utility.allAssemblies[i], ex);
					Utility.allTypes[i] = new Type[0];
					continue;
				}

				for (int j = 0; j < Utility.allTypes[i].Length; j++)
				{
					if ((baseType == typeof(object) || baseType.IsAssignableFrom(Utility.allTypes[i][j]) == true) &&
						Utility.allTypes[i][j].UnderlyingSystemType != baseType &&
						(match == null || match(Utility.allTypes[i][j]) == true))
					{
						list.Add(Utility.allTypes[i][j]);
						yield return Utility.allTypes[i][j];
					}
				}
			}

			if (match == null)
			{
				if (Utility.cachedEachAllAssignableFrom.ContainsKey(baseType) == false)
					Utility.cachedEachAllAssignableFrom.Add(baseType, list.ToArray());
			}

			Utility.poolTypes.Push(list);
		}
		
		private static readonly Dictionary<Type, Type[]>	cachedEachUnityAssignableFrom = new Dictionary<Type, Type[]>();

		public static IEnumerable<Type>	EachUnityAssignableFrom(Type baseType, Func<Type, bool> match = null)
		{
			if (Utility.allAssemblies == null)
				Utility.InitializeAssemblies();

			Type[]	types;

			if (match == null && Utility.cachedEachUnityAssignableFrom.TryGetValue(baseType, out types) == true)
			{
				for (int i = 0; i < types.Length; i++)
					yield return types[i];

				yield break;
			}

			List<Type>	list;

			if (Utility.poolTypes.Count > 0)
			{
				list = Utility.poolTypes.Pop();
				list.Clear();
			}
			else
				list = new List<Type>(32);

			for (int i = 0; i < Utility.unityAssemblies.Length; i++)
			{
				try
				{
					if (Utility.allTypes[Utility.unityAssemblies[i]] == null)
						Utility.allTypes[Utility.unityAssemblies[i]] = Utility.allAssemblies[Utility.unityAssemblies[i]].GetTypes();
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException("EachUnityAssignableFrom failed on Assembly " + Utility.allAssemblies[Utility.unityAssemblies[i]], ex);
					Utility.allTypes[Utility.unityAssemblies[i]] = new Type[0];
					continue;
				}

				types = Utility.allTypes[Utility.unityAssemblies[i]];

				for (int j = 0; j < types.Length; j++)
				{
					if ((baseType == typeof(object) || baseType.IsAssignableFrom(types[j]) == true) &&
						types[j].UnderlyingSystemType != baseType &&
						(match == null || match(types[j]) == true))
					{
						list.Add(types[j]);
						yield return types[j];
					}
				}
			}

			if (match == null)
			{
				if (Utility.cachedEachUnityAssignableFrom.ContainsKey(baseType) == false)
					Utility.cachedEachUnityAssignableFrom.Add(baseType, list.ToArray());
			}

			Utility.poolTypes.Push(list);
		}
		
		private static readonly Dictionary<Type, Type[]>	cachedEachNGTAssignableFrom = new Dictionary<Type, Type[]>();

		public static IEnumerable<Type>	EachNGTAssignableFrom(Type baseType, Func<Type, bool> match = null)
		{
			if (Utility.allAssemblies == null)
				Utility.InitializeAssemblies();

			Type[]	types;

			if (match == null && Utility.cachedEachNGTAssignableFrom.TryGetValue(baseType, out types) == true)
			{
				for (int i = 0; i < types.Length; i++)
					yield return types[i];

				yield break;
			}

			List<Type>	list;

			if (Utility.poolTypes.Count > 0)
			{
				list = Utility.poolTypes.Pop();
				list.Clear();
			}
			else
				list = new List<Type>(32);

			for (int i = 0; i < Utility.ngToolsAssemblies.Length; i++)
			{
				try
				{
					if (Utility.allTypes[Utility.ngToolsAssemblies[i]] == null)
						Utility.allTypes[Utility.ngToolsAssemblies[i]] = Utility.allAssemblies[Utility.ngToolsAssemblies[i]].GetTypes();
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException("EachNGTAssignableFrom failed on Assembly " + Utility.allAssemblies[Utility.ngToolsAssemblies[i]], ex);
					Utility.allTypes[Utility.ngToolsAssemblies[i]] = new Type[0];
					continue;
				}

				types = Utility.allTypes[Utility.ngToolsAssemblies[i]];

				for (int j = 0; j < types.Length; j++)
				{
					if ((baseType == typeof(object) || baseType.IsAssignableFrom(types[j]) == true) &&
						types[j].UnderlyingSystemType != baseType &&
						(match == null || match(types[j]) == true))
					{
						list.Add(types[j]);
						yield return types[j];
					}
				}
			}

			if (match == null)
			{
				if (Utility.cachedEachNGTAssignableFrom.ContainsKey(baseType) == false)
					Utility.cachedEachNGTAssignableFrom.Add(baseType, list.ToArray());
			}

			Utility.poolTypes.Push(list);
		}

		private static readonly Dictionary<Type, Type[]>	cachedEachSubClassesOf = new Dictionary<Type, Type[]>();

		public static IEnumerable<Type>	EachSubClassesOf(Type baseType, Func<Type, bool> match = null)
		{
			if (Utility.allAssemblies == null)
				Utility.InitializeAssemblies();

			Type[]	types;

			if (match == null && Utility.cachedEachSubClassesOf.TryGetValue(baseType, out types) == true)
			{
				for (int i = 0; i < types.Length; i++)
					yield return types[i];

				yield break;
			}

			List<Type>	list;

			if (Utility.poolTypes.Count > 0)
			{
				list = Utility.poolTypes.Pop();
				list.Clear();
			}
			else
				list = new List<Type>(32);

			try
			{
				if (Utility.allTypes[Utility.executingTypes] == null)
					Utility.allTypes[Utility.executingTypes] = Utility.allAssemblies[Utility.executingTypes].GetTypes();
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException("EachSubClassesOf failed on Assembly " + Utility.allAssemblies[Utility.executingTypes], ex);
				Utility.allTypes[Utility.executingTypes] = new Type[0];
			}

			types = Utility.allTypes[Utility.executingTypes];

			for (int i = 0; i < types.Length; i++)
			{
				if ((baseType == typeof(object) || types[i].IsSubclassOf(baseType) == true) &&
					(match == null || match(types[i]) == true))
				{
					list.Add(types[i]);
					yield return types[i];
				}
			}

			if (match == null)
			{
				if (Utility.cachedEachSubClassesOf.ContainsKey(baseType) == false)
					Utility.cachedEachSubClassesOf.Add(baseType, list.ToArray());
			}

			Utility.poolTypes.Push(list);
		}

		private static readonly Dictionary<Type, Type[]>	cachedEachNGTSubClassesOf = new Dictionary<Type, Type[]>();

		public static IEnumerable<Type>	EachNGTSubClassesOf(Type baseType, Func<Type, bool> match = null)
		{
			if (Utility.allAssemblies == null)
				Utility.InitializeAssemblies();

			Type[]	types;

			if (match == null && Utility.cachedEachNGTSubClassesOf.TryGetValue(baseType, out types) == true)
			{
				for (int i = 0; i < types.Length; i++)
					yield return types[i];

				yield break;
			}

			List<Type>	list;

			if (Utility.poolTypes.Count > 0)
			{
				list = Utility.poolTypes.Pop();
				list.Clear();
			}
			else
				list = new List<Type>(32);

			for (int i = 0; i < Utility.ngToolsAssemblies.Length; i++)
			{
				try
				{
					if (Utility.allTypes[Utility.ngToolsAssemblies[i]] == null)
						Utility.allTypes[Utility.ngToolsAssemblies[i]] = Utility.allAssemblies[Utility.ngToolsAssemblies[i]].GetTypes();
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException("EachNGTSubClassesOf failed on Assembly " + Utility.allAssemblies[Utility.ngToolsAssemblies[i]], ex);
					Utility.allTypes[Utility.ngToolsAssemblies[i]] = new Type[0];
					continue;
				}

				types = Utility.allTypes[Utility.ngToolsAssemblies[i]];

				for (int j = 0; j < types.Length; j++)
				{
					if ((baseType == typeof(object) || types[j].IsSubclassOf(baseType) == true) &&
						(match == null || match(types[j]) == true))
					{
						list.Add(types[j]);
						yield return types[j];
					}
				}
			}

			if (match == null)
			{
				if (Utility.cachedEachNGTSubClassesOf.ContainsKey(baseType) == false)
					Utility.cachedEachNGTSubClassesOf.Add(baseType, list.ToArray());
			}

			Utility.poolTypes.Push(list);
		}

		private static readonly Dictionary<Type, Type[]>	cachedEachAllSubClassesOf = new Dictionary<Type, Type[]>();
		private static readonly Stack<List<Type>>			poolTypes = new Stack<List<Type>>(8);

		public static IEnumerable<Type>	EachAllSubClassesOf(Type baseType, Func<Type, bool> match = null)
		{
			if (Utility.allAssemblies == null)
				Utility.InitializeAssemblies();

			Type[]	types;

			if (match == null && Utility.cachedEachAllSubClassesOf.TryGetValue(baseType, out types) == true)
			{
				for (int i = 0; i < types.Length; i++)
					yield return types[i];

				yield break;
			}

			List<Type>	list;

			if (Utility.poolTypes.Count > 0)
			{
				list = Utility.poolTypes.Pop();
				list.Clear();
			}
			else
				list = new List<Type>(32);

			for (int i = 0; i < Utility.allAssemblies.Length; i++)
			{
				try
				{
					if (Utility.allTypes[i] == null)
						Utility.allTypes[i] = Utility.allAssemblies[i].GetTypes();
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException("EachAllSubClassesOf failed on Assembly " + Utility.allAssemblies[i], ex);
					Utility.allTypes[i] = new Type[0];
					continue;
				}

				for (int j = 0; j < Utility.allTypes[i].Length; j++)
				{
					if ((baseType == typeof(object) || Utility.allTypes[i][j].IsSubclassOf(baseType) == true) &&
						(match == null || match(Utility.allTypes[i][j]) == true))
					{
						list.Add(Utility.allTypes[i][j]);
						yield return Utility.allTypes[i][j];
					}
				}
			}

			if (match == null)
			{
				if (Utility.cachedEachAllSubClassesOf.ContainsKey(baseType) == false)
					Utility.cachedEachAllSubClassesOf.Add(baseType, list.ToArray());
			}

			Utility.poolTypes.Push(list);
		}

		public static IEnumerable<Type>	EachAllSubClassesOf(Type baseType, Func<Assembly, bool> asmMatch, Func<Type, bool> typeMatch = null)
		{
			if (Utility.allAssemblies == null)
				Utility.InitializeAssemblies();

			for (int i = 0; i < Utility.allAssemblies.Length; i++)
			{
				if (asmMatch(Utility.allAssemblies[i]) == false)
					continue;

				try
				{
					if (Utility.allTypes[i] == null)
						Utility.allTypes[i] = Utility.allAssemblies[i].GetTypes();
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException("EachAllSubClassesOf failed on Assembly " + Utility.allAssemblies[i], ex);
					Utility.allTypes[i] = new Type[0];
					continue;
				}

				for (int j = 0; j < Utility.allTypes[i].Length; j++)
				{
					if ((baseType == typeof(object) || Utility.allTypes[i][j].IsSubclassOf(baseType) == true) &&
						(typeMatch == null || typeMatch(Utility.allTypes[i][j]) == true))
					{
						yield return Utility.allTypes[i][j];
					}
				}
			}
		}

		private static int[]					unityAssemblies;
		private static int[]					ngToolsAssemblies;
		private static Assembly[]				allAssemblies;
		private static Type[][]					allTypes;
		private static int						executingTypes;
		private static Dictionary<string, Type>	cachedGetType;

		/// <summary>
		/// Searches for a type looking in all assemblies from Editor and Engine.
		/// </summary>
		/// <param name="className"></param>
		/// <returns></returns>
		public static Type	GetType(string className)
		{
			if (Utility.allAssemblies == null)
				Utility.InitializeAssemblies();

			Type	type;

			if (Utility.cachedGetType == null)
				Utility.cachedGetType = new Dictionary<string, Type>(32);

			if (Utility.cachedGetType.TryGetValue(className, out type) == true)
				return type;

			// Remove generic symbols.
			int	n = className.IndexOf("[");

			if (n != -1)
				className = className.Substring(0, n);

			for (int i = 0; i < Utility.allAssemblies.Length; i++)
			{
				Type	classType = Utility.allAssemblies[i].GetType(className);

				if (classType != null)
				{
					Utility.cachedGetType.Add(className, classType);
					return classType;
				}

				try
				{
					if (Utility.allTypes[i] == null)
						Utility.allTypes[i] = Utility.allAssemblies[i].GetTypes();
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException("EachAllSubClassesOf failed on Assembly " + Utility.allAssemblies[i], ex);
					Utility.allTypes[i] = new Type[0];
					continue;
				}

				foreach (Type t in Utility.allTypes[i])
				{
					if (t.FullName == className ||
						t.Name == className)
					{
						Utility.cachedGetType.Add(className, t);
						return t;
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Searches for a type looking in all assemblies from Editor and Engine.
		/// </summary>
		/// <param name="className"></param>
		/// <returns></returns>
		public static Type	GetType(string @namespace, string className)
		{
			if (Utility.allAssemblies == null)
				Utility.InitializeAssemblies();

			// Remove generic symbols.
			int	n = className.IndexOf("[");

			if (n != -1)
				className = className.Substring(0, n);

			string	fullName = @namespace + '.' + className;

			for (int i = 0; i < Utility.allAssemblies.Length; i++)
			{
				Type	classType = Utility.allAssemblies[i].GetType(className);

				if (classType != null && classType.Name == @namespace)
					return classType;

				try
				{
					if (Utility.allTypes[i] == null)
						Utility.allTypes[i] = Utility.allAssemblies[i].GetTypes();
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException("EachAllSubClassesOf failed on Assembly " + Utility.allAssemblies[i], ex);
					Utility.allTypes[i] = new Type[0];
					continue;
				}

				foreach (Type t in Utility.allTypes[i])
				{
					if ((t.Namespace == @namespace &&
						 t.Name == className) ||
						t.FullName == fullName)
					{
						return t;
					}
				}
			}

			return null;
		}

		private static void	InitializeAssemblies()
		{
			Utility.allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
			Utility.allTypes = new Type[Utility.allAssemblies.Length][];

			List<int>	ngToolsAssemblies = new List<int>(2);
			List<int>	unityAssemblies = new List<int>(8);
			Assembly	executingAssembly = Assembly.GetExecutingAssembly();

			for (int i = 0; i < Utility.allAssemblies.Length; i++)
			{
				string			fullName = Utility.allAssemblies[i].FullName;
				AssemblyName[]	refs = Utility.allAssemblies[i].GetReferencedAssemblies();

				if (
#if FULL_NGTOOLS
					fullName.StartsWith("NGTools,") || fullName.StartsWith("NGToolsEditor,")
#elif NGTOOLS
					fullName.StartsWith("NGCore,") || fullName.StartsWith("NGCoreEditor,")
#else
					fullName.StartsWith("Assembly-CSharp-firstpass") == true || fullName.StartsWith("Assembly-CSharp-Editor-firstpass") == true
#endif
					)
					ngToolsAssemblies.Add(i);
				else
				{
					for (int j = 0; j < refs.Length; j++)
					{
						string	name = refs[j].Name;

						if (
#if FULL_NGTOOLS
							name == "NGTools" || name == "NGToolsEditor"
#elif NGTOOLS
							name == "NGCore" || name == "NGCoreEditor"
#else
							name.StartsWith("Assembly-CSharp") == true
#endif
							)
						{
							ngToolsAssemblies.Add(i);
							break;
						}
					}
				}

				if (fullName.StartsWith("UnityEngine,") == true || fullName.StartsWith("UnityEditor,") == true)
					unityAssemblies.Add(i);
				else
				{
					for (int j = 0; j < refs.Length; j++)
					{
						string	name = refs[j].Name;

						if (name == "UnityEngine" || name == "UnityEditor" || name == "UnityEngine.CoreModule")
						{
							unityAssemblies.Add(i);
							break;
						}
					}
				}

				try
				{
					if (Utility.allAssemblies[i] == executingAssembly)
						Utility.executingTypes = i;
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogFileException("Assembly \"" + Utility.allAssemblies[i] + "\" failed to initiate.", ex);
				}
			}

			Utility.ngToolsAssemblies = ngToolsAssemblies.ToArray();
			Utility.unityAssemblies = unityAssemblies.ToArray();
		}

		public static IEnumerable<MemberInfo>	EachMemberHierarchyOrdered(Type type, Type stopType, BindingFlags flags, Func<MemberInfo, bool> filter = null)
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
				{
					if (filter == null || filter(fields[i]) == true)
						yield return fields[i];
				}

				PropertyInfo[]	properties = t.GetProperties(flags | BindingFlags.DeclaredOnly);

				for (int i = 0; i < properties.Length; i++)
				{
					if (filter == null || filter(properties[i]) == true)
						yield return properties[i];
				}
			}
		}

		public static List<FieldInfo>			GetFieldsHierarchyOrdered(Type type, Type stopType, BindingFlags flags)
		{
			Stack<Type>		inheritances = new Stack<Type>();
			List<FieldInfo>	fields = new List<FieldInfo>();

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
				fields.AddRange(t.GetFields(flags | BindingFlags.DeclaredOnly));

			return fields;
		}

		public static IEnumerable<FieldInfo>	EachFieldHierarchyOrdered(Type type, Type stopType, BindingFlags flags, Func<FieldInfo, bool> filter = null)
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
				{
					if (filter == null || filter(fields[i]) == true)
						yield return fields[i];
				}
			}
		}

		public static List<PropertyInfo>		GetPropertiesHierarchyOrdered(Type type, Type stopType, BindingFlags flags)
		{
			Stack<Type>			inheritances = new Stack<Type>();
			List<PropertyInfo>	properties = new List<PropertyInfo>();

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
				properties.AddRange(t.GetProperties(flags | BindingFlags.DeclaredOnly));

			return properties;
		}

		public static IEnumerable<PropertyInfo>	EachPropertyHierarchyOrdered(Type type, Type stopType, BindingFlags flags)
		{
			var	inheritances = new Stack<Type>();

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
				PropertyInfo[]	properties = t.GetProperties(flags | BindingFlags.DeclaredOnly);

				for (int i = 0; i < properties.Length; i++)
					yield return properties[i];
			}
		}

		private static DynamicOrderedArray<EditorPrefType>	editorPrefInstances;

		/// <summary>
		/// <para>Saves the given <paramref name="instance"/> in EditorPrefs.</para>
		/// <para>Only works on integers, unsigned integers, float, double, decimal, bool, char, byte, sbyte, byte, sbyte, string, Vector2, Vector3, Vector4, Rect, Quaternion, Color, GUIStyle, GUIStyleState, enum, Array, IList<>, Object, struct and class.</para>
		/// <para>Use NonSerializedAttribute to prevent serializing.</para>
		/// <para>Use SerializeField to serialize protected or private fields.</para>
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="type"></param>
		/// <param name="path"></param>
		public static void		DirectSaveEditorPref(object instance, Type type, string path = "")
		{
			if (Utility.editorPrefInstances == null)
				Utility.CreateEditorPrefInstances();

			EditorPrefType	pref = null;

			for (int j = 0; j < Utility.editorPrefInstances.array.Length; j++)
			{
				if (Utility.editorPrefInstances.array[j].CanHandle(type) == true)
				{
					pref = Utility.editorPrefInstances.array[j];
					Utility.editorPrefInstances.BringToTop(j);
					break;
				}
			}

			if (pref != null)
				pref.DirectSave(instance, type, path);
		}

		/// <summary>
		/// <para>Saves all public non-static fields of the given <paramref name="instance"/> or the <paramref name="instance"/> itself if primitive in EditorPrefs.</para>
		/// <para>Only works on integers, unsigned integers, float, double, decimal, bool, char, byte, sbyte, string, Vector2, Vector3, Vector4, Rect, Quaternion, Color, GUIStyle, GUIStyleState, enum, Array, IList<>, Object, struct and class.</para>
		/// <para>Use NonSerializedAttribute to prevent serializing.</para>
		/// <para>Use SerializeField to serialize protected or private fields.</para>
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="prefix"></param>
		public static void		SaveEditorPref(object instance, string prefix = "")
		{
			if (Utility.editorPrefInstances == null)
				Utility.CreateEditorPrefInstances();

			EditorPrefType	pref = null;
			Type			type = instance.GetType();

			for (int j = 0; j < Utility.editorPrefInstances.array.Length; j++)
			{
				if (Utility.editorPrefInstances.array[j].CanHandle(type) == true)
				{
					pref = Utility.editorPrefInstances.array[j];
					Utility.editorPrefInstances.BringToTop(j);
					break;
				}
			}

			if (pref != null)
				pref.Save(instance, type, prefix);
		}

		/// <summary>
		/// <para>Fetches the value from EditorPrefs.</para>
		/// <para>Only works on integers, unsigned integers, float, double, decimal, bool, char, byte, sbyte, string, Vector2, Vector3, Vector4, Rect, Quaternion, Color, GUIStyle, GUIStyleState, enum, Array, IList<>, Object, struct and class.</para>
		/// <para>Use NonSerializedAttribute to prevent serializing.</para>
		/// <para>Use SerializeField to serialize protected or private fields.</para>
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="type"></param>
		/// <param name="prefix"></param>
		/// <returns></returns>
		public static object	LoadEditorPref(object instance, Type type, string prefix = "")
		{
			if (Utility.editorPrefInstances == null)
				Utility.CreateEditorPrefInstances();

			EditorPrefType	pref = null;

			for (int j = 0; j < Utility.editorPrefInstances.array.Length; j++)
			{
				if (Utility.editorPrefInstances.array[j].CanHandle(type) == true)
				{
					pref = Utility.editorPrefInstances.array[j];
					Utility.editorPrefInstances.BringToTop(j);
					break;
				}
			}

			if (pref != null)
				instance = pref.Fetch(instance, type, prefix);

			return instance;
		}

		/// <summary>
		/// <para>Restores values from EditorPrefs to all public non-static fields.</para>
		/// <para>Only works on integers, unsigned integers, float, double, decimal, bool, char, byte, sbyte, string, Vector2, Vector3, Vector4, Rect, Quaternion, Color, GUIStyle, GUIStyleState, enum, Array, IList<>, Object, struct and class.</para>
		/// <para>Use NonSerializedAttribute to prevent serializing.</para>
		/// <para>Use SerializeField to serialize protected or private fields.</para>
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="prefix"></param>
		public static void		LoadEditorPref(object instance, string prefix = "")
		{
			if (Utility.editorPrefInstances == null)
				Utility.CreateEditorPrefInstances();

			EditorPrefType	pref = null;
			Type			type = instance.GetType();

			for (int j = 0; j < Utility.editorPrefInstances.array.Length; j++)
			{
				if (Utility.editorPrefInstances.array[j].CanHandle(type) == true)
				{
					pref = Utility.editorPrefInstances.array[j];
					Utility.editorPrefInstances.BringToTop(j);
					break;
				}
			}

			if (pref != null)
				pref.Load(instance, type, prefix);
		}

		private static void		CreateEditorPrefInstances()
		{
			EditorPrefType[]	array = Utility.CreateNGTInstancesOf<EditorPrefType>();

			Utility.editorPrefInstances = new DynamicOrderedArray<EditorPrefType>(array) { fixedIndexes = new int[] { array.Length - 1, array.Length - 2 } };

			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] is EditorPrefClass)
				{
					if (i != array.Length - 1)
					{
						EditorPrefType	tmp = array[array.Length - 1];
						array[array.Length - 1] = array[i];
						array[i] = tmp;
					}
				}
				if (array[i] is EditorPrefStruct)
				{
					if (i != array.Length - 2)
					{
						EditorPrefType	tmp = array[array.Length - 2];
						array[array.Length - 2] = array[i];
						array[i] = tmp;
					}
				}
			}
		}

		/// <summary>Checks if a symbol exists in the active build target.</summary>
		/// <param name="symbol"></param>
		/// <returns></returns>
		public static bool	ExistSymbol(string symbol)
		{
			string	rawSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(Utility.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
			int		i = rawSymbols.IndexOf(symbol);

			// Symbol not found, add it.
			if (i == -1)
				return false;

			// Check if symbol is starting.
			if (i > 0 && rawSymbols[i - 1] != ';')
				return false;

			// Check if symbol is ending.
			if (i + symbol.Length < rawSymbols.Length &&
				rawSymbols[i + symbol.Length] != ';')
				return false;

			return true;
		}

		/// <summary>Appends a symbol to the active build target.</summary>
		/// <param name="symbol"></param>
		public static void	AppendSymbol(string symbol)
		{
			BuildTargetGroup	target = Utility.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
			string				rawSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
			PlayerSettings.SetScriptingDefineSymbolsForGroup(target, rawSymbols + ";" + symbol);
		}

		/// <summary>Removes a symbol from the active build target.</summary>
		/// <param name="symbol"></param>
		public static void	RemoveSymbol(string symbol)
		{
			BuildTargetGroup	target = Utility.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
			string				rawSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
			int					i = rawSymbols.IndexOf(symbol);

			// Symbol not found, add it.
			if (i != -1)
				PlayerSettings.SetScriptingDefineSymbolsForGroup(target, rawSymbols.Substring(0, i) + rawSymbols.Substring(i + symbol.Length));
		}

		/// <summary>Toggles a symbol in the active build target.</summary>
		/// <param name="symbol"></param>
		public static void	ToggleSymbol(string symbol)
		{
			BuildTargetGroup	target = Utility.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
			string				rawSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
			int					i = rawSymbols.IndexOf(symbol);

			// Symbol not found, add it.
			if (i == -1)
				PlayerSettings.SetScriptingDefineSymbolsForGroup(target, rawSymbols + ";" + symbol);
			else
				PlayerSettings.SetScriptingDefineSymbolsForGroup(target, rawSymbols.Substring(0, i) + rawSymbols.Substring(i + symbol.Length));
		}

		public static BuildTargetGroup	GetBuildTargetGroup(BuildTarget buildTarget)
		{
			if (buildTarget == (BuildTarget)13) // Android
				return (BuildTargetGroup)7; // Android
			if (buildTarget == (BuildTarget)28) // BlackBerry
				return (BuildTargetGroup)16; // BlackBerry
			if (buildTarget == (BuildTarget)18) // FlashPlayer
				return (BuildTargetGroup)12; // FlashPlayer
			if (buildTarget == (BuildTarget)9) // iOS & iPhone
				return (BuildTargetGroup)4; // iOS & iPhone
			if (buildTarget == (BuildTarget)21) // MetroPlayer & WSAPlayer
				return (BuildTargetGroup)14; // Metro & WSA
			if (buildTarget == (BuildTarget)16) // NaCl
				return (BuildTargetGroup)11; // NaCl
			if (buildTarget == (BuildTarget)35) // Nintendo3DS
				return (BuildTargetGroup)23; // Nintendo3DS
			if (buildTarget == (BuildTarget)10) // PS3
				return (BuildTargetGroup)5; // PS3
			if (buildTarget == (BuildTarget)31) // PS4
				return (BuildTargetGroup)19; // PS4
			if (buildTarget == (BuildTarget)32) // PSM
				return (BuildTargetGroup)20; // PSM
			if (buildTarget == (BuildTarget)30) // PSP2
				return (BuildTargetGroup)18; // PSP2
			if (buildTarget == (BuildTarget)34) // SamsungTV
				return (BuildTargetGroup)22; // SamsungTV
			if (buildTarget == (BuildTarget)14 || // StandaloneGLESEmu
				buildTarget == (BuildTarget)17 || // StandaloneLinux
				buildTarget == (BuildTarget)24 || // StandaloneLinux64
				buildTarget == (BuildTarget)25 || // StandaloneLinuxUniversal
				buildTarget == (BuildTarget)4 || // StandaloneOSXIntel
				buildTarget == (BuildTarget)27 || // StandaloneOSXIntel64
				buildTarget == (BuildTarget)2 || // StandaloneOSXUniversal
				buildTarget == (BuildTarget)5 || // StandaloneWindows
				buildTarget == (BuildTarget)19) // StandaloneWindows64
			{
				return (BuildTargetGroup)1; // Standalone
			}
			if (buildTarget == (BuildTarget)29) // Tizen
				return (BuildTargetGroup)17; // Tizen
			if (buildTarget == (BuildTarget)37) // tvOS
				return (BuildTargetGroup)25; // tvOS
			if (buildTarget == (BuildTarget)20) // WebGL
				return (BuildTargetGroup)13; // WebGL
			if (buildTarget == (BuildTarget)6 || // WebPlayer
				buildTarget == (BuildTarget)7) // WebPlayerStreamed
			{
				return (BuildTargetGroup)2; // WebPlayer
			}
			if (buildTarget == (BuildTarget)36) // WiiU
				return (BuildTargetGroup)24; // WiiU
			if (buildTarget == (BuildTarget)26) // WP8Player
				return (BuildTargetGroup)15; // WP8
			if (buildTarget == (BuildTarget)11) // XBOX360
				return (BuildTargetGroup)6; // XBOX360
			if (buildTarget == (BuildTarget)33) // XboxOne
				return (BuildTargetGroup)21; // XboxOne
			return 0; // Unknown
		}

		public static void	Append(this StringBuilder sb, string content, int repeatCount)
		{
			for (int i = 0; i < repeatCount; i++)
				sb.Append(content);
		}

		public static void	Append(this StringBuilder sb, string content, Color color)
		{
			sb.AppendStartColor(color);
			sb.Append(content);
			sb.AppendEndColor();
		}

		public static void	AppendStartColor(this StringBuilder sb, Color color)
		{
			sb.Append("<color=#");
			sb.Append(((int)(color.r * 255)).ToString("X2"));
			sb.Append(((int)(color.g * 255)).ToString("X2"));
			sb.Append(((int)(color.b * 255)).ToString("X2"));
			sb.Append(((int)(color.a * 255)).ToString("X2"));
			sb.Append('>');
		}

		public static void	AppendEndColor(this StringBuilder sb)
		{
			sb.Append("</color>");
		}

		public static string	Color(string content, Color color)
		{
			return "<color=#" + ((int)(color.r * 255)).ToString("X2") + ((int)(color.g * 255)).ToString("X2") + ((int)(color.b * 255)).ToString("X2") + ((int)(color.a * 255)).ToString("X2") + ">" + content + "</color>";
		}

		private static string	cachedConsolePath = string.Empty;

		/// <summary>
		/// Gets the relative path of NG Tools folder from the project.
		/// </summary>
		/// <returns></returns>
		public static string	GetPackagePath()
		{
			if (string.IsNullOrEmpty(Utility.cachedConsolePath) == true)
			{
				string[]	dirs = Directory.GetDirectories("Assets", Constants.RootFolderName, SearchOption.AllDirectories);

				for (int i = 0; i < dirs.Length; i++)
				{
					int	chances = 0;

					if (Directory.Exists(Path.Combine(dirs[i], Constants.RelativeLocaleFolder)) == true)
						++chances;
					if (Directory.Exists(Path.Combine(dirs[i], "NGCore")) == true)
						++chances;
					if (chances < 2 && Directory.Exists(Path.Combine(dirs[i], "NGGameConsole")) == true)
						++chances;
					if (chances < 2 && Directory.Exists(Path.Combine(dirs[i], "Test")) == true)
						++chances;

					// Set the path anyway.
					Utility.cachedConsolePath = dirs[i].Replace('\\', '/');

					// But break on the highest potential.
					if (chances >= 2)
						break;
				}
			}

			return Utility.cachedConsolePath;
		}

		/// <summary>
		/// <para>Returns the given <paramref name="texture"/> if not null.</para>
		/// <para>Otherwise gets a Texture asset from AssetDatabase using <paramref name="settings"/>, <paramref name="folder"/> and <paramref name="fileName"/>.</para>
		/// <para>If none is found, it calls the callback <paramref name="defaultTexture"/> to generate a new texture and saves it to AssetDatabse and returns it.</para>
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="texture">A texture.</param>
		/// <param name="folder">Path of the texture to save on disk. Relative to the given ConsoleSetting.</param>
		/// <param name="fileName">Filename of the texture without extension. ".asset" will be appended.</param>
		/// <param name="defaultTexture">A function generating a Texture if the requesting asset is not found.</param>
		/// <returns></returns>
		public static Texture2D	GetRefFromAssetDatabase(NGSettings settings, Texture2D texture, string folder, string fileName, Func<Texture2D> defaultTexture)
		{
			if (texture == null)
			{
				try
				{
					string	assetPath = AssetDatabase.GetAssetPath(settings);

					if (string.IsNullOrEmpty(assetPath) == true)
						throw new Exception("The given instance of NGSettings is not an asset in the project.");

					string	texturePath = Directory.GetParent(assetPath).FullName.Substring(Application.dataPath.Length - 6) +
						Path.DirectorySeparatorChar +
						folder +
						Path.DirectorySeparatorChar;

					if (Directory.Exists(texturePath) == false)
						Directory.CreateDirectory(texturePath);

					texturePath = Path.Combine(texturePath, fileName + ".asset");

					// Find texture if exists.
					texture = AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D)) as Texture2D;
					if (texture == null)
					{
						// Generates an asset with the default texture.
						texture = defaultTexture();

						AssetDatabase.CreateAsset(texture, texturePath);

						var	asset = AssetDatabase.LoadMainAssetAtPath(texturePath);

						texture = EditorUtility.InstanceIDToObject(asset.GetInstanceID()) as Texture2D;
					}
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException(ex);
					texture = new Texture2D(1, 1)
					{
						hideFlags = HideFlags.HideAndDontSave
					};
				}
			}
			return texture;
		}

		public static Texture2D	CreateDotTexture(float r, float g, float b, float a)
		{
			Texture2D	texture = new Texture2D(1, 1);
			texture.SetPixel(0, 0, new Color(r, g, b, a));
			texture.Apply();
			return texture;
		}

		private static readonly MethodInfo	LoadIcon = UnityAssemblyVerifier.TryGetMethod(typeof(EditorGUIUtility), "LoadIcon", BindingFlags.Static | BindingFlags.NonPublic);

		/// <summary>
		/// Returns a Texture2D from the given <paramref name="name"/> using method LoadIcon from EditorGUIUtility.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static Texture2D	GetConsoleIcon(string name)
		{
			if (Utility.LoadIcon != null)
				return Utility.LoadIcon.Invoke(null, new object[] { name }) as Texture2D;
			return null;
		}

		/// <summary>Launches an executable with arguments.</summary>
		/// <param name="editorPath"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		public static bool	OpenFileLine(string editorPath, string arguments, ProcessWindowStyle windowStyle = ProcessWindowStyle.Hidden, bool createNoWindow = true)
		{
			try
			{
				Process	myProcess = new Process();

				myProcess.StartInfo.WindowStyle = windowStyle;
				myProcess.StartInfo.CreateNoWindow = createNoWindow;
				myProcess.StartInfo.UseShellExecute = false;
				myProcess.StartInfo.FileName = editorPath;
				myProcess.StartInfo.Arguments = arguments;
				myProcess.Start();

				return true;
			}
			catch (Exception e)
			{
				InternalNGDebug.LogException(e);
			}
			return false;
		}

		private static readonly Type	consoleWindowType = UnityAssemblyVerifier.TryGetType(typeof(InternalEditorUtility).Assembly, "UnityEditor.ConsoleWindow");

		/// <summary>Calls Repaint from Unity's console if available.</summary>
		public static void	RepaintConsoleWindow()
		{
			if (Utility.consoleWindowType != null)
				Utility.RepaintEditorWindow(Utility.consoleWindowType);
		}

		private static bool												lazyPrewarmEditorWindowsLoaded = false;
		private static readonly Dictionary<Type, List<EditorWindow>>	runningWindows = new Dictionary<Type, List<EditorWindow>>();

		/// <summary>
		/// Prewarms an EditorWindow type, to prevent RepaintEditorWindow() from using Resources.FindObjectsOfTypeAll().
		/// Use this on windows that are manually manage through RegisterWindow() and UnregisterWindow().
		/// </summary>
		/// <param name="windowType"></param>
		public static void	PrewarmWindowType(Type windowType)
		{
			if (Utility.lazyPrewarmEditorWindowsLoaded == false)
			{
				Utility.lazyPrewarmEditorWindowsLoaded = true;
				Utility.LazyPrewarmEditorWindows();
			}

			if (Utility.runningWindows.ContainsKey(windowType) == false)
				Utility.runningWindows.Add(windowType, new List<EditorWindow>());
		}

		public static void	RegisterWindow(EditorWindow window, Type type = null)
		{
			if (Utility.lazyPrewarmEditorWindowsLoaded == false)
			{
				Utility.lazyPrewarmEditorWindowsLoaded = true;
				Utility.LazyPrewarmEditorWindows();
			}

			List<EditorWindow>	list;
			type = type ?? window.GetType();

			if (Utility.runningWindows.TryGetValue(type, out list) == false)
			{
				list = new List<EditorWindow>();
				Utility.runningWindows.Add(type, list);
			}

			if (list.Contains(window) == false)
				list.Add(window);
		}

		public static void	UnregisterWindow(EditorWindow window, Type type = null)
		{
			if (Utility.lazyPrewarmEditorWindowsLoaded == false)
			{
				Utility.lazyPrewarmEditorWindowsLoaded = true;
				Utility.LazyPrewarmEditorWindows();
			}

			List<EditorWindow>	list;
			type = type ?? window.GetType();

			if (Utility.runningWindows.TryGetValue(type, out list) == true)
				list.Remove(window);
		}

		public static void	CloseAllEditorWindows(Type windowType)
		{
			if (Utility.lazyPrewarmEditorWindowsLoaded == false)
			{
				Utility.lazyPrewarmEditorWindowsLoaded = true;
				Utility.LazyPrewarmEditorWindows();
			}

			List<EditorWindow>	list;

			if (Utility.runningWindows.TryGetValue(windowType, out list) == true)
			{
				for (int i = 0; i < list.Count; i++)
					list[i].Close();
			}
			else
			{
				Object[]	windows = Resources.FindObjectsOfTypeAll(windowType);

				for (int i = 0; i < windows.Length; i++)
					(windows[i] as EditorWindow).Close();
			}
		}

		public static void	RepaintEditorWindow(Type windowType)
		{
			if (Utility.lazyPrewarmEditorWindowsLoaded == false)
			{
				Utility.lazyPrewarmEditorWindowsLoaded = true;
				Utility.LazyPrewarmEditorWindows();
			}

			List<EditorWindow>	list;

			if (Utility.runningWindows.TryGetValue(windowType, out list) == true)
			{
				for (int i = 0; i < list.Count; i++)
					list[i].Repaint();
			}
			else
			{
				Object[]	windows = Resources.FindObjectsOfTypeAll(windowType);

				for (int i = 0; i < windows.Length; i++)
					(windows[i] as EditorWindow).Repaint();
			}
		}

		public static IEnumerable<EditorWindow>	EachEditorWindows(Type windowType)
		{
			if (Utility.lazyPrewarmEditorWindowsLoaded == false)
			{
				Utility.lazyPrewarmEditorWindowsLoaded = true;
				Utility.LazyPrewarmEditorWindows();
			}

			List<EditorWindow>	list;

			if (Utility.runningWindows.TryGetValue(windowType, out list) == true)
			{
				for (int i = 0; i < list.Count; i++)
					yield return list[i];
			}
		}

		private static void	LazyPrewarmEditorWindows()
		{
			foreach (Type type in Utility.EachNGTSubClassesOf(typeof(EditorWindow)))
			{
				if (type.IsDefined(typeof(PrewarmEditorWindowAttribute), false) == true)
					Utility.runningWindows.Add(type, new List<EditorWindow>());
			}
		}

		private static bool			initializedPreferencesMetadata;
		internal static Type		settingsWindowType = typeof(Editor).Assembly.GetType("UnityEditor.PreferencesWindow") ?? typeof(Editor).Assembly.GetType("UnityEditor.SettingsWindow");
		private static Type			preferencesWindowType;
		private static FieldInfo	m_RefreshCustomPreferences;
		private static MethodInfo	AddCustomSections;
		private static FieldInfo	m_Sections;
		private static FieldInfo	m_SelectedSectionIndex;

		private static void	LazyInitializePreferencesMetadata()
		{
			Utility.preferencesWindowType = UnityAssemblyVerifier.TryGetType(typeof(InternalEditorUtility).Assembly, "UnityEditor.PreferencesWindow");

			if (Utility.preferencesWindowType != null)
			{
				Utility.m_RefreshCustomPreferences = UnityAssemblyVerifier.TryGetField(Utility.preferencesWindowType, "m_RefreshCustomPreferences", BindingFlags.Instance | BindingFlags.NonPublic);
				Utility.AddCustomSections = UnityAssemblyVerifier.TryGetMethod(Utility.preferencesWindowType, "AddCustomSections", BindingFlags.Instance | BindingFlags.NonPublic);
				Utility.m_Sections = UnityAssemblyVerifier.TryGetField(Utility.preferencesWindowType, "m_Sections", BindingFlags.Instance | BindingFlags.NonPublic);
				Utility.m_SelectedSectionIndex = UnityAssemblyVerifier.TryGetField(Utility.preferencesWindowType, "m_SelectedSectionIndex", BindingFlags.Instance | BindingFlags.NonPublic);

				if (Utility.m_RefreshCustomPreferences == null || Utility.AddCustomSections == null || Utility.m_Sections == null || Utility.m_SelectedSectionIndex == null)
					Utility.preferencesWindowType = null;
			}
		}

		/// <summary>Opens Preferences Window at the given <paramref name="preferenceItem"/>.</summary>
		/// <param name="preferenceItem">The title of the Preferences Window used in attribute PreferenceItem.</param>
		public static void	ShowPreferencesWindowAt(string preferenceItem, string subPath = null)
		{
			string	unityVersion = Utility.UnityVersion;

			if (unityVersion.StartsWith("20") == true &&
				unityVersion.CompareTo("2018.3") >= 0)
			{
				//SettingsService.OpenProjectSettings("Project/Graphics");
				Type		SettingsWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SettingsWindow");
				MethodInfo	ShowMethod = SettingsWindowType.GetMethod("Show", BindingFlags.NonPublic | BindingFlags.Static);

				ShowMethod.Invoke(null, new object[] { 0/*SettingsScope.User*/, "Preferences/" + preferenceItem + (subPath != null ? '/' + subPath : null)});
				return;
			}

			if (Utility.initializedPreferencesMetadata == false)
			{
				Utility.initializedPreferencesMetadata = true;
				Utility.LazyInitializePreferencesMetadata();
			}

			if (Utility.preferencesWindowType != null)
			{
				EditorWindow.GetWindow(Utility.preferencesWindowType, true, "Unity Preferences");
				Object[]	windows = Resources.FindObjectsOfTypeAll(Utility.preferencesWindowType);

				if (windows.Length > 0)
				{
					if (Utility.m_RefreshCustomPreferences != null)
					{
						// Force PreferencesWindow to load custom sections before setting m_SelectedSectionIndex.
						if ((bool)Utility.m_RefreshCustomPreferences.GetValue(windows[0]) == true)
						{
							if (Utility.AddCustomSections != null)
							{
								Utility.AddCustomSections.Invoke(windows[0], new object[] { });
								Utility.m_RefreshCustomPreferences.SetValue(windows[0], false);
							}
							else
								return;
						}
					}

					if (Utility.m_Sections != null)
					{
						IEnumerable	sections = Utility.m_Sections.GetValue(windows[0]) as IEnumerable;
						if (sections != null)
						{
							int	i = 0;
							foreach (object element in sections)
							{
								FieldInfo	contentField = element.GetType().GetField("content", BindingFlags.Instance | BindingFlags.Public);
								GUIContent	content = contentField.GetValue(element) as GUIContent;

								if (content.text == preferenceItem)
								{

									if (Utility.m_SelectedSectionIndex != null)
										Utility.m_SelectedSectionIndex.SetValue(windows[0], i);
									break;
								}
								++i;
							}
						}
					}
				}
			}
		}

		public static void	ShowSettingsWindowAt(string path)
		{
//#if UNITY_2018_3_OR_NEWER
//			Type		SettingsWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SettingsWindow");
//			MethodInfo	ShowMethod = SettingsWindowType.GetMethod("Show", BindingFlags.NonPublic | BindingFlags.Static);

//			ShowMethod.Invoke(null, new object[] { SettingsScopes.Project, "Project/" + Constants.PackageTitle });
//#else
			Utility.OpenWindow<NGSettingsWindow>(NGSettingsWindow.Title, true, null, e => e.Focus(path));
//#endif
		}

		public static IEnumerable<T>	EachCustomAttributesIncludingBaseInterfaces<T>(this Type type)
		{
			Type	attributeType = typeof(T);
			T[]		attributes = type.GetCustomAttributes(attributeType, true) as T[];

			for (int i = 0; i < attributes.Length; i++)
				yield return attributes[i];

			Type[]	interfaces = type.GetInterfaces();

			for (int i = 0; i < interfaces.Length; i++)
			{
				attributes = interfaces[i].GetCustomAttributes(attributeType, true) as T[];

				for (int j = 0; j < attributes.Length; j++)
					yield return attributes[j];
			}
		}

		public static byte[]	SerializeField(object field)
		{
			using (MemoryStream	ms = new MemoryStream())
			{
				BinaryFormatter	bin = new BinaryFormatter();
				bin.Serialize(ms, field);
				return ms.ToArray();
			}
		}

		public static T			DeserializeField<T>(byte[] raw)
		{
			using (MemoryStream	ms = new MemoryStream(raw))
			{
				BinaryFormatter	bin = new BinaryFormatter();
				return (T)bin.Deserialize(ms);
			}
		}

		public static object	DeserializeField(byte[] raw)
		{
			using (MemoryStream	ms = new MemoryStream(raw))
			{
				BinaryFormatter	bin = new BinaryFormatter();
				return bin.Deserialize(ms);
			}
		}

		private static readonly MethodInfo	GetIconForObject = UnityAssemblyVerifier.TryGetMethod(typeof(EditorGUIUtility), "GetIconForObject", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod);

		/// <summary>
		/// Gets the cached icon or tries to find it. The returned icon might be null.
		/// </summary>
		/// <returns></returns>
		public static Texture2D	GetIcon(Object asset)
		{
			if (asset != null)
				return Utility.GetIcon(asset.GetInstanceID());
			return null;
		}

		/// <summary>
		/// Gets the cached icon or tries to find it. The returned icon might be null.
		/// </summary>
		/// <returns></returns>
		public static Texture2D	GetIcon(int instanceID)
		{
			if (instanceID != 0)
			{
				Object	asset = EditorUtility.InstanceIDToObject(instanceID);

				if (asset != null)
				{
					Texture2D	icon;

					// Unfortunately, GameObject does not have an proper icon.
					if (asset is GameObject)
					{
						return UtilityResources.PrefabIcon;
						//Debug.Log("GetIconForFile(prefab)");
					}

					icon = AssetPreview.GetMiniThumbnail(asset);
					//Debug.Log("AssetPreview.GetMiniThumbnail");
					if (icon != null && icon.name != "DefaultAsset Icon")
						return icon;

					if (Utility.GetIconForObject != null)
					{
						object	obj = Utility.GetIconForObject.Invoke(null, new object[] { asset });
						//Debug.Log("Method" + " " + obj);
						icon = (Texture2D)obj;
						if (icon != null)
							return icon;
					}

					string	path = AssetDatabase.GetAssetPath(asset);

					if (string.IsNullOrEmpty(path) == false)
					{
						icon = InternalEditorUtility.GetIconForFile(path);
						//Debug.Log("GetIconForFile(path)");
						if (icon != null)
							return icon;
					}

					icon = EditorGUIUtility.ObjectContent(asset, asset.GetType()).image as Texture2D;
					//Debug.Log("EditorGUIUtility.ObjectContent");
					if (icon != null)
						return icon;
				}
			}

			return null;
		}

		private static Color	DropZoneOutline { get { return Utility.GetSkinColor(.2F, .9F, .2F, .95F, .9F, .9F, .9F, .95F); } }
		private static Color	DropZoneBackground { get { return Utility.GetSkinColor(.2F, .2F, .2F, .95F, .7F, .7F, .7F, .95F); } }
		private static GUIStyle	dynamicFontSizeCenterText;

		/// <summary>
		/// Draws a drop area where drag&drop should be handled.
		/// </summary>
		/// <param name="r"></param>
		/// <param name="message"></param>
		public static void	DropZone(Rect r, string message)
		{
			Utility.DropZone(r, message, Utility.DropZoneOutline, Utility.DropZoneBackground);
		}

		/// <summary>
		/// Draws a drop area where drag&drop should be handled.
		/// </summary>
		/// <param name="r"></param>
		/// <param name="message"></param>
		/// <param name="inline"></param>
		public static void	DropZone(Rect r, string message, Color outline)
		{
			Utility.DropZone(r, message, outline, Utility.DropZoneBackground);
		}

		/// <summary>
		/// Draws a drop area where drag&drop should be handled.
		/// </summary>
		/// <param name="r"></param>
		/// <param name="message"></param>
		/// <param name="inline"></param>
		/// <param name="outline"></param>
		public static void		DropZone(Rect r, string message, Color outline, Color inline)
		{
			if (Utility.dynamicFontSizeCenterText == null)
			{
				Utility.dynamicFontSizeCenterText = new GUIStyle(GUI.skin.label)
				{
					wordWrap = true,
					alignment = TextAnchor.MiddleCenter
				};
			}

			EditorGUI.DrawRect(r, inline);

			Rect	r2 = r;
			r2.xMin += 2F;
			r2.xMax -= 2F;
			r2.yMin += 2F;
			r2.yMax -= 2F;

			Utility.DrawUnfillRect(r2, outline);

			Utility.dynamicFontSizeCenterText.fontSize = 15;

			// Shrink title to fit the space.
			Utility.content.text = message;
			while (Utility.dynamicFontSizeCenterText.CalcSize(Utility.content).x >= r.width &&
				   Utility.dynamicFontSizeCenterText.fontSize > 9)
			{
				--Utility.dynamicFontSizeCenterText.fontSize;
			}

			using (ColorContentRestorer.Get(UnityEngine.Color.cyan))
			{
				GUI.Label(r, message, Utility.dynamicFontSizeCenterText);
			}
		}

		/// <summary>Processes width of one element in a field with many, relying on EditorGUIUtility.labelWidth.</summary>
		/// <param name="totalWidth">Width of the whole field in inspector.</param>
		/// <param name="elementMinWidth">Minimum width an element can have.</param>
		/// <param name="elementCount">Number of sub-elements.</param>
		/// <param name="labelWidth">Output label width.</param>
		/// <param name="subElementsWidth">Output total sub-elements width.</param>
		public static void	CalculSubFieldsWidth(float totalWidth, float elementMinWidth, int elementCount, out float labelWidth, out float subElementsWidth)
		{
			float	totalElementsWidth = elementMinWidth * elementCount;

			if (totalWidth < EditorGUIUtility.labelWidth + totalElementsWidth)
			{
				labelWidth = totalWidth - totalElementsWidth;
				subElementsWidth = elementMinWidth;
			}
			else
			{
				labelWidth = EditorGUIUtility.labelWidth;
				subElementsWidth = (totalWidth - labelWidth) / elementCount;
			}
		}

		/// <summary>
		/// Creates a new inspector window instance and locks it to inspect the specified target
		/// </summary>
		/// <remarks>Thank to vexe at: http://answers.unity3d.com/questions/36131/editor-multiple-inspectors.html</remarks>
		public static EditorWindow	InspectTarget(Object target)
		{
			// Get a reference to the `InspectorWindow` type object
			var	inspectorType = UnityAssemblyVerifier.TryGetType(typeof(Editor).Assembly, "UnityEditor.InspectorWindow");
			// Create an InspectorWindow instance
			var	inspectorInstance = ScriptableObject.CreateInstance(inspectorType) as EditorWindow;
			// We display it - currently, it will inspect whatever gameObject is currently selected
			// So we need to find a way to let it inspect/aim at our target GO that we passed
			// For that we do a simple trick:
			// 1- Cache the current selected gameObject
			// 2- Set the current selection to our target GO (so now all inspectors are targeting it)
			// 3- Lock our created inspector to that target
			// 4- Fallback to our previous selection
			//inspectorInstance.Show(false);
			// Cache previous selected gameObject
			var	prevSelection = Selection.activeGameObject;
			// Set the selection to GO we want to inspect
			Selection.activeObject = target;
			// Get a ref to the "locked" property, which will lock the state of the inspector to the current inspected target
			var	isLocked = inspectorType.GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public);
			// Invoke `isLocked` setter method passing 'true' to lock the inspector
			isLocked.GetSetMethod().Invoke(inspectorInstance, new object[] { true });
			// Finally revert back to the previous selection so that other inspectors continue to inspect whatever they were inspecting...
			Selection.activeGameObject = prevSelection;

			return inspectorInstance;
		}

		private static readonly Stack<string>	hierarchy = new Stack<string>(4);

		public static string	GetHierarchyStringified(Transform transform)
		{
			StringBuilder	buffer = Utility.GetBuffer();

			Utility.hierarchy.Clear();

			while (transform != null)
			{
				Utility.hierarchy.Push(transform.name);
				transform = transform.parent;
			}

			while (Utility.hierarchy.Count > 0)
			{
				buffer.Append(Utility.hierarchy.Pop());
				buffer.Append('/');
			}

			if (buffer.Length > 0)
				buffer.Length -= 1;

			buffer.Append(Environment.NewLine);

			if (buffer.Length > Environment.NewLine.Length)
				buffer.Length -= Environment.NewLine.Length;

			return Utility.ReturnBuffer(buffer);
		}

		public static void		StartBackgroundTask(IEnumerator update, Action end = null)
		{
			EditorApplication.CallbackFunction	closureCallback = null;

			closureCallback = () =>
			{
				try
				{
					if (update.MoveNext() == false)
					{
						if (end != null)
							end();
						EditorApplication.update -= closureCallback;
					}
				}
				catch (Exception ex)
				{
					if (end != null)
						end();
					InternalNGDebug.LogException(ex);
					EditorApplication.update -= closureCallback;
				}
			};

			EditorApplication.update += closureCallback;
		}

		private class ProgressBarTask
		{
			public Action<object>	action;
			public string			content;
			public double			endTime;
			public float			lifetime;
			public string			abortMessage;
			public Thread			thread;
		}

		private static readonly List<ProgressBarTask>	progressBarTasks = new List<ProgressBarTask>();

		public static void	StartAsyncBackgroundTask(Action<object> callback, string progressBarString, float lifetime, string abortMessage = null)
		{
			lock (Utility.progressBarTasks)
			{
				if (Utility.progressBarTasks.Count == 0)
					EditorApplication.update += Utility.UpdateProgressBars;

				ProgressBarTask	task = new ProgressBarTask() {
					action = callback,
					content = progressBarString,
					endTime = EditorApplication.timeSinceStartup + lifetime,
					lifetime = lifetime,
					abortMessage = abortMessage,
					thread = new Thread(new ParameterizedThreadStart(callback))
				};

				Utility.progressBarTasks.Add(task);

				task.thread.Start(task);
			}
		}

		private static void	UpdateProgressBars()
		{
			StringBuilder	buffer = Utility.GetBuffer();
			float			globalProgress = 0F;
			int				total = 0;

			// Around 32 chars max, smartly share the amount between tasks.

			lock (Utility.progressBarTasks)
			{
				for (int i = 0; i < Utility.progressBarTasks.Count; i++)
				{
					var	task = Utility.progressBarTasks[i];

					Utility.AsyncProgressBarDisplay(task.content, 0F);

					if (task.thread.IsAlive == true && task.endTime > EditorApplication.timeSinceStartup)
					{
						if (i > 0)
							buffer.Append(" | ");

						buffer.Append(task.content);

						float	rate = 1F - ((float)(task.endTime - EditorApplication.timeSinceStartup) / task.lifetime);

						if (Utility.progressBarTasks.Count >= 2)
							buffer.Append((rate * 100F).ToString(" ###\\%"));

						globalProgress += rate;
						++total;
					}
					else
					{
						Utility.progressBarTasks.RemoveAt(i);

						if (task.thread.IsAlive == true)
						{
							if (string.IsNullOrEmpty(task.abortMessage) == false)
								InternalNGDebug.LogWarning(task.abortMessage);

							task.thread.Abort();
							task.thread.Join();
						}

						if (Utility.progressBarTasks.Count == 0)
						{
							Utility.AsyncProgressBarClear();
							EditorApplication.update -= Utility.UpdateProgressBars;
							return;
						}
					}
				}
			}

			if (total > 0)
				Utility.AsyncProgressBarDisplay(Utility.ReturnBuffer(buffer), globalProgress / total);
		}

		/// <summary>
		/// Makes sure a callback is invoked in the next frame or near future.
		/// Thanks to you Allegorithmic for doing shitty code.
		/// </summary>
		/// <param name="callback"></param>
		public static void	SafeDelayCall(EditorApplication.CallbackFunction callback)
		{
			bool								done = false;
			EditorApplication.CallbackFunction	once = null;
			once = () =>
			{
				if (done == false)
				{
					done = true;
					callback();
				}

				EditorApplication.delayCall -= once;
				EditorApplication.update -= once;
			};

			// If delay, update or thread fails, we might have bigger problems. :)
			EditorApplication.delayCall += once;
			EditorApplication.update += once;

			new Thread(new ThreadStart(() =>
			{
				Thread.Sleep(1000);

				while (done == false)
				{
					EditorApplication.delayCall -= once;
					EditorApplication.delayCall += once;
					Thread.Sleep(1000);
				}
			})).Start();
		}

		private class CallbackSchedule
		{
			public Action	action;
			public int		intervalTicks;
			public int		ticksLeft;
			public int		remainingCalls;
		}

		private static readonly List<CallbackSchedule>	schedules = new List<CallbackSchedule>();

		public static void	RegisterIntervalCallback(Action action, int ticks, int count = -1)
		{
			if (Utility.schedules.Count == 0)
				EditorApplication.update += Utility.TickIntervalCallbacks;

			for (int i = 0; i < Utility.schedules.Count; i++)
			{
				if (Utility.schedules[i].action == action)
				{
					Utility.schedules[i].ticksLeft = ticks;
					Utility.schedules[i].intervalTicks = ticks;
					Utility.schedules[i].remainingCalls = count;
					return;
				}
			}

			Utility.schedules.Add(new CallbackSchedule() { action = action, ticksLeft = ticks, intervalTicks = ticks, remainingCalls = count });
		}

		public static void	UnregisterIntervalCallback(Action action)
		{
			for (int i = 0; i < Utility.schedules.Count; i++)
			{
				if (Utility.schedules[i].action == action)
				{
					Utility.schedules.RemoveAt(i);

					if (Utility.schedules.Count == 0)
						EditorApplication.update -= Utility.TickIntervalCallbacks;

					break;
				}
			}
		}

		private static void	TickIntervalCallbacks()
		{
			for (int i = 0; i < Utility.schedules.Count; i++)
			{
				if (--Utility.schedules[i].ticksLeft <= 0)
				{
					CallbackSchedule	callback = Utility.schedules[i];
					callback.ticksLeft = callback.intervalTicks;
					callback.action();

					--callback.remainingCalls;

					if (callback.remainingCalls == 0)
						Utility.schedules.RemoveAt(i);

					if (i < Utility.schedules.Count)
					{
						if (callback != Utility.schedules[i])
							--i;
					}
				}
			}
		}

		private static string[]		cachedMenuItems;

		public static string[]	GetAllMenuItems()
		{
			if (Utility.cachedMenuItems == null)
			{
				List<string>	parent = new List<string>(4);
				List<string>	menuItems = new List<string>(256);
				string[]		rawMenuItems = EditorGUIUtility.SerializeMainMenuToString().Split('\n');
				string			mi;

				if (rawMenuItems[0][0] == '&')
					parent.Add(rawMenuItems[0].Substring(1).Replace("&&", "&"));
				else
					parent.Add(rawMenuItems[0].Replace("&&", "&"));

				for (int i = 1; i < rawMenuItems.Length; i++)
				{
					if (rawMenuItems[i].Contains("UNUSED") == true ||
						rawMenuItems[i].Contains("TEMPORARY") == true)
					{
						continue;
					}

					int	indentation = 0;

					while (indentation < rawMenuItems[i].Length && rawMenuItems[i][indentation] == ' ')
						++indentation;

					// Skip empty entry.
					if (indentation == rawMenuItems[i].Length)
						continue;

					int	level = indentation / 4;

					if (level >= parent.Count)
					{
						if (rawMenuItems[i][indentation] == '&')
							parent.Add(rawMenuItems[i].Substring(indentation + 1).Replace("&&", "&"));
						else
							parent.Add(rawMenuItems[i].Substring(indentation).Replace("&&", "&"));
					}
					else
					{
						if (level < parent.Count - 1)
						{
							mi = string.Join("/", parent.ToArray());

							if (menuItems.Contains(mi) == false)
								menuItems.Add(mi);

							parent.RemoveRange(level + 1, parent.Count - level - 1);

							if (rawMenuItems[i][indentation] == '&')
								parent[level] = rawMenuItems[i].Substring(indentation + 1).Replace("&&", "&");
							else
								parent[level] = rawMenuItems[i].Substring(indentation).Replace("&&", "&");
						}
						else if (level == parent.Count - 1)
						{
							mi = string.Join("/", parent.ToArray());

							if (menuItems.Contains(mi) == false)
								menuItems.Add(mi);

							if (rawMenuItems[i][indentation] == '&')
								parent[level] = rawMenuItems[i].Substring(indentation + 1).Replace("&&", "&");
							else
								parent[level] = rawMenuItems[i].Substring(indentation).Replace("&&", "&");
						}
					}
				}

				mi = string.Join("/", parent.ToArray());

				if (menuItems.Contains(mi) == false)
					menuItems.Add(mi);

				//Debug.Log(EditorGUIUtility.SerializeMainMenuToString());
				//for (int i = 0; i < menuItems.Count; i++)
				//	Debug.Log(menuItems[i]);

				//foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
				//{
				//	foreach (var type in asm.GetTypes())
				//	{
				//		foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
				//		{
				//			MenuItem[]	attributes = method.GetCustomAttributes(typeof(MenuItem), false) as MenuItem[];

				//			if (attributes.Length > 0 && attributes[0].validate == false && attributes[0].menuItem.StartsWith("CONTEXT/") == false)
				//			{
				//				int	n = attributes[0].menuItem.LastIndexOf(" ");

				//				if (n != -1)
				//				{
				//					if (attributes[0].menuItem[n + 1] == '%' ||
				//						attributes[0].menuItem[n + 1] == '#' ||
				//						attributes[0].menuItem[n + 1] == '&' ||
				//						attributes[0].menuItem[n + 1] == '_')
				//					{
				//						menuItems.Add(attributes[0].menuItem.Substring(0, n));
				//					}
				//					else
				//						menuItems.Add(attributes[0].menuItem);
				//				}
				//				else
				//					menuItems.Add(attributes[0].menuItem);
				//			}
				//		}
				//	}
				//}

				//EditorApplication.ExecuteMenuItem(item);

				Utility.cachedMenuItems = menuItems.ToArray();
			}

			return Utility.cachedMenuItems;
		}

		public static void	DrawLine(Vector2 a, Vector2 b, Color c)
		{
			using (HandlesColorRestorer.Get(c))
			{
				Handles.BeginGUI();
				Handles.DrawLine(a, b);
				Handles.EndGUI();
			}
		}

		public static void	DrawCircle(Vector2 a, Color c, float radius)
		{
			using (HandlesColorRestorer.Get(c))
			{
				Handles.BeginGUI();
				Handles.DrawWireDisc(new Vector3(a.x, a.y, 0F), new Vector3(0F, 0F, 1F), radius);
				Handles.EndGUI();
			}
		}

		public static void	DrawUnfillRect(Rect r, Color c)
		{
			float	x = r.x;
			float	xMax = r.xMax;
			float	w = r.width;
			float	h = r.height;

			// Top border
			r.height = 1F;
			EditorGUI.DrawRect(r, c);

			// Left border
			r.width = 1F;
			r.height = h;
			EditorGUI.DrawRect(r, c);

			// Right border
			r.x = xMax - 1F;
			EditorGUI.DrawRect(r, c);

			// Bottom border
			r.x = x;
			r.y += r.height - 1F;
			r.width = w;
			r.height = 1F;
			EditorGUI.DrawRect(r, c);
		}

		public const float					HeaderHeight = 43F; // Double of the real height, don't know why.
		public const float					Space = .02F;
		private static readonly Vector3[]	positions = new Vector3[] { new Vector3(), new Vector3(), new Vector3(), new Vector3() };
		private static readonly int[]		positionIndexes = new int[] { 0, 1, 1, 2, 2, 3, 3, 0 };

		public static void	DrawRectDotted(Rect r, Rect position, Color c, float space = Utility.Space, float headerHeight = Utility.HeaderHeight)
		{
			Handles.BeginGUI();
			{
				float	heightRate = (position.height - headerHeight) / position.height;
				float	YMax = 1F + heightRate;

				float	x = -1 + (r.x * 2 / position.width);
				float	y = heightRate - (r.y * YMax / position.height);
				float	w = -1 + ((r.x + r.width) * 2 / position.width);
				float	h = heightRate - ((r.y + r.height) * YMax / position.height);

				using (HandlesColorRestorer.Get(c))
				using (HandlesMatrix4x4Restorer.Get(Matrix4x4.TRS(Vector3.one, Quaternion.identity, Vector3.one)))
				{
					Utility.positions[0].x = x;
					Utility.positions[0].y = y;
					Utility.positions[1].x = x;
					Utility.positions[1].y = h;
					Utility.positions[2].x = w;
					Utility.positions[2].y = h;
					Utility.positions[3].x = w;
					Utility.positions[3].y = y;

					Handles.DrawDottedLines(Utility.positions, Utility.positionIndexes, space);
				}
			}
			Handles.EndGUI();
		}

		private static bool			initializedMainViewMetadata;
		private static Type			containerWinType;
		private static FieldInfo	showModeField;
		private static PropertyInfo	positionProperty;

		private static object	mainWindow;

		private static void	LazyInitializeMainViewMetadata()
		{
			Utility.containerWinType = UnityAssemblyVerifier.TryGetType(typeof(Editor).Assembly, "UnityEditor.ContainerWindow");
			if (Utility.containerWinType != null)
			{
				Utility.showModeField = UnityAssemblyVerifier.TryGetField(Utility.containerWinType, "m_ShowMode", BindingFlags.NonPublic | BindingFlags.Instance);
				Utility.positionProperty = UnityAssemblyVerifier.TryGetProperty(Utility.containerWinType, "position", BindingFlags.Public | BindingFlags.Instance);

				if (Utility.showModeField == null || Utility.positionProperty == null)
					Utility.containerWinType = null;
			}
		}

		public static Rect	GetEditorMainWindowPos()
		{
			if (Utility.initializedMainViewMetadata == false)
			{
				Utility.initializedMainViewMetadata = true;
				Utility.LazyInitializeMainViewMetadata();
			}

			if (Utility.containerWinType == null)
				return default(Rect);

			if (Utility.mainWindow == null || Utility.mainWindow.Equals(null) == true || Object.Equals(Utility.mainWindow, "null") == true)
			{
				Object[]	windows = Resources.FindObjectsOfTypeAll(containerWinType);
				foreach (Object win in windows)
				{
					int	showmode = (int)Utility.showModeField.GetValue(win);
					if (showmode == 4) // main window
					{
						Utility.mainWindow = win;
						break;
					}
				}
			}

			if (Utility.mainWindow.Equals(null) == false)
				return (Rect)Utility.positionProperty.GetValue(Utility.mainWindow, null);

			throw new NotSupportedException("Can't find internal main window. Maybe something has changed inside Unity.");
		}

		public static void	CenterOnMainWin(this EditorWindow window)
		{
			Rect	main = Utility.GetEditorMainWindowPos();
			Rect	pos = window.position;
			float	w = (main.width - pos.width) * 0.5f;
			float	h = (main.height - pos.height) * 0.5f;

			pos.x = main.x + w;
			pos.y = main.y + h;
			window.position = pos;
		}

		public static void	AddNGMenuItems(GenericMenu menu, EditorWindow window, string helpLabel, string helpURL, bool isNGSettings = false)
		{
			menu.AddItem(new GUIContent(Preferences.Title), false, () => Utility.ShowPreferencesWindowAt(Constants.PackageTitle));
			if (isNGSettings == false)
			{
				menu.AddItem(new GUIContent(NGSettingsWindow.Title), false, () =>
				{
					Utility.ShowSettingsWindowAt(window.titleContent.text);
				});
			}

			if (string.IsNullOrEmpty(helpURL) == false)
				menu.AddItem(new GUIContent(helpLabel + "/Help"), false, () => Application.OpenURL(helpURL));

			menu.AddItem(new GUIContent(helpLabel + "/Send feedback"), false, () => ContactFormWizard.Open(ContactFormWizard.Subject.Feedback, helpLabel, null));

			if (NGChangeLogWindow.HasChangeLog(helpLabel) == true)
				menu.AddItem(new GUIContent(helpLabel + "/Change log"), false, () => NGChangeLogWindow.Open(helpLabel));

			if (Conf.DebugMode != Conf.DebugState.None)
				menu.AddItem(new GUIContent("Open NGRealTimeEditorDebug"), false, () => Utility.OpenWindow<NGRealTimeEditorDebug>(NGRealTimeEditorDebug.Title));
		}

		/// <summary>
		/// Simply searches a keyword into the line starting at i.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="keyword"></param>
		/// <param name="i"></param>
		/// <returns></returns>
		public static bool	Compare(string line, string keyword, int i)
		{
			int	j = 0;

			for (; i < line.Length && j < keyword.Length; i++, j++)
			{
				if (line[i] != keyword[j])
					return false;
			}

			// Check if keyword matches.
			if (j == keyword.Length)
			{
				// Check if keyword is a whole word.
				if (('a' <= keyword[0] && keyword[0] <= 'z') ||
					('A' <= keyword[0] && keyword[0] <= 'Z'))
				{
					// Check char after keyword's end.
					if (i < line.Length)
					{
						if ((line[i] < 'a' || line[i] > 'z') &&
							(line[i] < 'A' || line[i] > 'Z') &&
							(line[i] < '0' || line[i] > '9') &&
							line[i] != '_')
						{
							// Then check char before.
							if (i - j - 1 >= 0)
							{
								i -= j + 1;
								return ((line[i] < 'a' || line[i] > 'z') &&
										(line[i] < 'A' || line[i] > 'Z') &&
										(line[i] < '0' || line[i] > '9') &&
										line[i] != '_');
							}
						}
						else
							return false;
					}
					// Otherwise only check before.
					else if (i - j - 1 >= 0)
					{
						i -= j + 1;
						return ((line[i] < 'a' || line[i] > 'z') &&
								(line[i] < 'A' || line[i] > 'Z') &&
								(line[i] < '0' || line[i] > '9') &&
								line[i] != '_');
					}
				}

				return true;
			}

			return false;
		}

		// Thanks to Dave Swersky at http://stackoverflow.com/questions/3354893/how-can-i-convert-a-datetime-to-the-number-of-seconds-since-1970
		public static DateTime	ConvertFromUnixTimestamp(double timestamp)
		{
			DateTime	origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			return origin.AddSeconds(timestamp);
		}

		public static double	ConvertToUnixTimestamp(DateTime date)
		{
			DateTime	origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			TimeSpan	diff = date.ToUniversalTime() - origin;
			return Math.Floor(diff.TotalSeconds);
		}

		public static bool	IsStruct(this Type t)
		{
			return InnerUtility.IsStruct(t);
		}

		private static bool			initializeAsyncProgressBarMetadata;
		private static PropertyInfo	AsyncProgressBarProgressInfo;
		private static MethodInfo	AsyncProgressBarDisplayMethod;
		private static MethodInfo	AsyncProgressBarClearMethod;

		private static void	LazyInitializeAsyncProgressBarMetadata()
		{
			Type	t = UnityAssemblyVerifier.TryGetType(typeof(EditorWindow).Assembly, "UnityEditor.AsyncProgressBar");

			if (t != null)
			{
				Utility.AsyncProgressBarProgressInfo = UnityAssemblyVerifier.TryGetProperty(t, "progressInfo", BindingFlags.Static | BindingFlags.Public);
				Utility.AsyncProgressBarDisplayMethod = UnityAssemblyVerifier.TryGetMethod(t, "Display", BindingFlags.Static | BindingFlags.Public);
				Utility.AsyncProgressBarClearMethod = UnityAssemblyVerifier.TryGetMethod(t, "Clear", BindingFlags.Static | BindingFlags.Public);
			}
		}

		public static string	GetAsyncProgressBarInfo()
		{
			if (Utility.initializeAsyncProgressBarMetadata == false)
			{
				Utility.initializeAsyncProgressBarMetadata = true;
				Utility.LazyInitializeAsyncProgressBarMetadata();
			}

			if (Utility.AsyncProgressBarProgressInfo != null)
				return Utility.AsyncProgressBarProgressInfo.GetValue(null, null) as string;
			return string.Empty;
		}

		public static void	AsyncProgressBarDisplay(string progressInfo, float progress)
		{
			if (Utility.initializeAsyncProgressBarMetadata == false)
			{
				Utility.initializeAsyncProgressBarMetadata = true;
				Utility.LazyInitializeAsyncProgressBarMetadata();
			}

			if (Utility.AsyncProgressBarDisplayMethod != null)
				Utility.AsyncProgressBarDisplayMethod.Invoke(null, new object[] { progressInfo, progress });
		}

		public static void	AsyncProgressBarClear()
		{
			if (Utility.initializeAsyncProgressBarMetadata == false)
			{
				Utility.initializeAsyncProgressBarMetadata = true;
				Utility.LazyInitializeAsyncProgressBarMetadata();
			}

			if (Utility.AsyncProgressBarClearMethod != null)
				Utility.AsyncProgressBarClearMethod.Invoke(null, null);
		}

		public static string	NicifyVariableName(string name)
		{
			return InnerUtility.NicifyVariableName(name);
		}

		private static readonly Dictionary<int, Texture2D>	cachedColoredIcons = new Dictionary<int, Texture2D>();

		public static void	RestoreIcon(EditorWindow editor)
		{
			editor.titleContent.image = UtilityResources.NGIcon;
		}

		public static void	RestoreIcon(EditorWindow editor, Color lineColor)
		{
			Texture2D	newIcon;
			int			hashCode = lineColor.GetHashCode();

			if (Utility.cachedColoredIcons.TryGetValue(hashCode, out newIcon) == false)
			{
				Texture2D	icon = UtilityResources.NGIcon as Texture2D;

				newIcon = new Texture2D(icon.width, icon.height, icon.format, false, true)
				{
					hideFlags = icon.hideFlags
				};
				newIcon.LoadRawTextureData(icon.GetRawTextureData());
				for (int x = 0; x < icon.width - 0; x++)
					newIcon.SetPixel(x, 1, lineColor);
				for (int x = 2; x < icon.height - 12; x++)
					newIcon.SetPixel(0, x, lineColor);
				newIcon.Apply();

				Utility.cachedColoredIcons.Add(hashCode, newIcon);
			}

			editor.titleContent.image = newIcon;
		}

		public static void	OpenWindow<T>(string title, bool focus, Type nextTo, Action<T> callback = null) where T : EditorWindow
		{
			Utility.OpenWindow<T>(false, title, focus, nextTo, callback);
		}

		public static void	OpenWindow<T>(bool defaultIsUtility, string title, bool focus = true, Type nextTo = null, Action<T> callback = null) where T : EditorWindow
		{
			if (Application.platform == RuntimePlatform.WindowsEditor)
			{
				GUICallbackWindow.Open(() =>
				{
					T	instance;

					if (Event.current.control == true)
					{
						instance = EditorWindow.CreateInstance<T>();
						instance.titleContent.text = Event.current.shift == defaultIsUtility && title.StartsWith("NG ") == true ? title.Substring(3) : title;

						if (focus == true)
							instance.Focus();

						if (Event.current.shift != defaultIsUtility)
							instance.ShowUtility();
						else
							instance.Show();
					}
					else
					{
						if (Event.current.shift != defaultIsUtility)
							instance = EditorWindow.GetWindow<T>(true, title, focus);
						else
							instance = EditorWindow.GetWindow<T>(null, focus, nextTo);
					}

					instance.titleContent.text = title.StartsWith("NG ") == true ? title.Substring(3) : title;
					if (instance.titleContent.image == null)
						instance.titleContent.image = UtilityResources.NGIcon;

					if (callback != null)
						callback(instance);
				});
			}
			else
			{
				T	instance = EditorWindow.GetWindow<T>(title.StartsWith("NG ") == true ? title.Substring(3) : title, focus);

				if (instance.titleContent.image == null)
					instance.titleContent.image = UtilityResources.NGIcon;

				if (callback != null)
					callback(instance);
			}
		}

		public static void	OpenWindow<T>(string title, bool focus = true, Action<T> callback = null) where T : EditorWindow
		{
			Utility.OpenWindow<T>(false, title, focus, null, callback);
		}

		private static bool	isCustomEditorCompatible;
		public static bool	IsCustomEditorCompatible
		{
			get
			{
				if (Utility.initializeCustomEditorTypes == false)
				{
					Utility.initializeCustomEditorTypes = true;
					Utility.LazyInitializeCustomEditorTypes();
				}
				return Utility.isCustomEditorCompatible;
			}
		}

		private static bool			initializeCustomEditorTypes;
		private static Type			CustomEditorAttributes;
		private static FieldInfo	kSCustomEditors;
		private static MethodInfo	FindCustomEditorTypeByType;

		private static Type			MonoEditorType;
		private static Type			ListMonoEditorType;
		private static FieldInfo	m_InspectedType;
		private static FieldInfo	m_InspectorType;

		/// <summary>Adds a CustomEditor into Unity Editor if it does not exist.</summary>
		/// <param name="inspected"></param>
		/// <param name="inspector"></param>
		/// <returns>True if the inspector is added.</returns>
		public static bool	AddCustomEditor(Type inspected, Type inspector)
		{
			if (Utility.initializeCustomEditorTypes == false)
			{
				Utility.initializeCustomEditorTypes = true;
				Utility.LazyInitializeCustomEditorTypes();
			}

			if (Utility.isCustomEditorCompatible == false)
				return false;

			// Force CustomEditorAttributes to rebuild at least once its cache.
			if (Utility.FindCustomEditorTypeByType != null)
				Utility.FindCustomEditorTypeByType.Invoke(null, new object[] { null, false });

			if (Utility.UnityVersion.StartsWith("5.") == true || Utility.UnityVersion.CompareTo("2017.3") <= 0)
			{
				IList	list = Utility.kSCustomEditors.GetValue(null) as IList;
				bool	found = false;

				foreach (object item in list)
				{
					if ((Type)Utility.m_InspectorType.GetValue(item) == inspector)
					{
						found = true;
						break;
					}
				}

				if (found == false)
				{
					object	instance = Activator.CreateInstance(Utility.MonoEditorType);

					Utility.m_InspectedType.SetValue(instance, inspected);
					Utility.m_InspectorType.SetValue(instance, inspector);
					list.Insert(0, instance);
					return true;
				}
			}
			else
			{
				IDictionary	dict = Utility.kSCustomEditors.GetValue(null) as IDictionary;
				object		monoEditorType = Activator.CreateInstance(Utility.MonoEditorType);

				Utility.m_InspectedType.SetValue(monoEditorType, inspected);
				Utility.m_InspectorType.SetValue(monoEditorType, inspector);

				if (dict.Contains(inspected) == false)
				{
					IList	instance = Activator.CreateInstance(Utility.ListMonoEditorType) as IList;
					instance.Insert(0, monoEditorType);

					dict.Add(inspected, instance);
					return true;
				}
				else
				{
					IList	list = dict[inspected] as IList;

					for (int i = 0; i < list.Count; i++)
					{
						if ((Type)Utility.m_InspectorType.GetValue(list[i]) == inspector)
						{
							list.RemoveAt(i);
							return false;
						}
					}

					(dict[inspected] as IList).Insert(0, monoEditorType);
					return true;
				}
			}

			return false;
		}

		/// <summary>Removes a CustomEditor from Unity Editor if it does exist.</summary>
		/// <param name="inspector"></param>
		/// <returns>Returns True if the inspector is removed.</returns>
		public static bool	RemoveCustomEditor(Type inspector)
		{
			if (Utility.initializeCustomEditorTypes == false)
			{
				Utility.initializeCustomEditorTypes = true;
				Utility.LazyInitializeCustomEditorTypes();
			}

			if (Utility.isCustomEditorCompatible == false)
				return false;

			// Force CustomEditorAttributes to rebuild at least once its cache.
			if (Utility.FindCustomEditorTypeByType != null)
				Utility.FindCustomEditorTypeByType.Invoke(null, new object[] { null, false });

			if (Utility.UnityVersion.StartsWith("5.") == true || Utility.UnityVersion.CompareTo("2017.3") <= 0)
			{
				IList	list = Utility.kSCustomEditors.GetValue(null) as IList;

				foreach (object item in list)
				{
					if ((Type)Utility.m_InspectorType.GetValue(item) == inspector)
					{
						list.Remove(item);
						return true;
					}
				}
			}
			else
			{
				IDictionary	dict = Utility.kSCustomEditors.GetValue(null) as IDictionary;

				foreach (IList list in dict.Values)
				{
					for (int i = 0; i < list.Count; i++)
					{
						if ((Type)Utility.m_InspectorType.GetValue(list[i]) == inspector)
						{
							list.RemoveAt(i);
							return true;
						}
					}
				}
			}

			return false;
		}

		private static void	LazyInitializeCustomEditorTypes()
		{
			Utility.CustomEditorAttributes = UnityAssemblyVerifier.TryGetType(typeof(Editor).Assembly, "UnityEditor.CustomEditorAttributes");

			if (Utility.CustomEditorAttributes != null)
			{
				Utility.kSCustomEditors = UnityAssemblyVerifier.TryGetField(Utility.CustomEditorAttributes, "kSCustomEditors", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
				Utility.FindCustomEditorTypeByType = UnityAssemblyVerifier.TryGetMethod(Utility.CustomEditorAttributes, "FindCustomEditorTypeByType", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
				Utility.MonoEditorType = UnityAssemblyVerifier.TryGetNestedType(Utility.CustomEditorAttributes, "MonoEditorType", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

				if (Utility.MonoEditorType != null)
				{
					Utility.ListMonoEditorType = typeof(List<>).MakeGenericType(Utility.MonoEditorType);
					Utility.m_InspectedType = UnityAssemblyVerifier.TryGetField(Utility.MonoEditorType, "m_InspectedType", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
					Utility.m_InspectorType = UnityAssemblyVerifier.TryGetField(Utility.MonoEditorType, "m_InspectorType", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				}

				Utility.isCustomEditorCompatible = Utility.kSCustomEditors != null &&
					Utility.FindCustomEditorTypeByType != null &&
					Utility.MonoEditorType != null &&
					Utility.ListMonoEditorType != null &&
					Utility.m_InspectedType != null &&
					Utility.m_InspectorType != null;
			}
		}

		public static void	RecompileUnityEditor()
		{
			BuildTargetGroup	target = Utility.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
			string				rawSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
			// Force Unity Editor to recompile.
			// Thanks to darbotron @ http://answers.unity3d.com/questions/416711/force-unity-to-recompile-scripts.html
			PlayerSettings.SetScriptingDefineSymbolsForGroup(target, rawSymbols + "a");
			PlayerSettings.SetScriptingDefineSymbolsForGroup(target, rawSymbols);
		}

		public enum RequestStatus
		{
			None,
			PreException,
			Completed,
			PostException
		}

		public static void	RequestURL(string url, Action<RequestStatus, object> onComplete)
		{
			string	unityVersion = Utility.UnityVersion;
			Action	asyncWebRequestInvoke = () =>
			{
				try
				{
					// Due to a random issue where the request is never ending, the timer will fallback.
					System.Timers.Timer	autoRequestKill = new System.Timers.Timer(15000)
					{
						Enabled = true
					};

					HttpWebRequest	request = (HttpWebRequest)WebRequest.Create(url);

					autoRequestKill.Elapsed += (sender, e) =>
					{
						lock (unityVersion)
						{
							autoRequestKill.Stop();

							// Little trick to avoid a double call to onComplete.
							if (onComplete == null)
								return;

							request.Abort();

							Action<RequestStatus, object>	local = onComplete;

							onComplete = null;

							EditorApplication.delayCall += () => local(RequestStatus.PostException, new TimeoutException("Request has expired."));
						}
					};
					autoRequestKill.Start();

					request.UserAgent = "Unity/" + unityVersion + " NG Tools/" + Constants.Version;
					request.Timeout = 5000;
					request.ReadWriteTimeout = 15000;

					using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
					using (StreamReader readStream = new StreamReader(response.GetResponseStream(), Encoding.ASCII))
					{
						lock (unityVersion)
						{
							// Little trick to avoid a double call to onComplete.
							if (onComplete == null)
								return;

							Action<RequestStatus, object>	localOnComplete = onComplete;

							onComplete = null;
							autoRequestKill.Stop();

							RequestStatus	status = RequestStatus.None;
							object			arg = null;

							try
							{
								string	result = readStream.ReadToEnd();

								arg = result;
								status = RequestStatus.Completed;
							}
							catch (Exception ex)
							{
								arg = ex;
								status = RequestStatus.PostException;
							}
							finally
							{
								EditorApplication.delayCall += () => localOnComplete(status, arg);
							}
						}
					}
				}
				catch (Exception ex)
				{
					lock (unityVersion)
					{
						// Little trick to avoid a double call to onComplete.
						if (onComplete == null)
							return;

						Action<RequestStatus, object>	localOnComplete = onComplete;

						onComplete = null;
						EditorApplication.delayCall += () => localOnComplete(RequestStatus.PreException, ex);
					}
				}
			};

			asyncWebRequestInvoke.BeginInvoke(iar => ((Action)iar.AsyncState).EndInvoke(iar), asyncWebRequestInvoke);
		}

		/// <summary>
		/// Loads all asset at path and make sure the result contains no null entry.
		/// </summary>
		/// <param name="assetPath"></param>
		/// <returns></returns>
		public static Object[]	SafeLoadAllAssetsAtPath(string assetPath)
		{
			Object[]	result = AssetDatabase.LoadAllAssetsAtPath(assetPath);
			int			nullCount = 0;

			for (int i = 0; i < result.Length; i++)
			{
				if (result[i] == null)
					++nullCount;
			}

			if (nullCount == 0)
				return result;

			Object[]	shrinkedResult = new Object[result.Length - nullCount];

			for (int i = 0, j = 0; i < result.Length; ++i, ++j)
			{
				while (i < result.Length && result[i] == null)
					++i;

				if (i < result.Length)
					shrinkedResult[j] = result[i];
			}

			return shrinkedResult;
		}

		private static readonly MethodInfo	Internal_GetGUIDepth = Utility.GetInternal_GetGUIDepth();

		private static MethodInfo	GetInternal_GetGUIDepth()
		{
			PropertyInfo	guiDepth = typeof(GUIUtility).GetProperty("guiDepth", BindingFlags.Static | BindingFlags.NonPublic);

			if (guiDepth != null)
			{
				MethodInfo	method = guiDepth.GetGetMethod(true);
				if (method != null)
					return method;
			}

			return UnityAssemblyVerifier.TryGetMethod(typeof(GUIUtility), "Internal_GetGUIDepth", BindingFlags.Static | BindingFlags.NonPublic);
		}

		public static bool	CheckOnGUI()
		{
			return (int)Utility.Internal_GetGUIDepth.Invoke(null, null) > 0;
		}

		private static readonly int[]	containsBadChar = new int[256];

		// Thanks @ https://www.programmingalgorithms.com/algorithm/boyer%E2%80%93moore-string-search-algorithm
		// Boyer-Moore implementation.
		public static bool	FastContains(this string str, string pattern)
		{
			int	m = pattern.Length;
			int	n = str.Length;
			int	s = 0;
			int	i;

			for (i = 0; i < 256; i++)
				Utility.containsBadChar[i] = -1;

			for (i = 0; i < m; i++)
				Utility.containsBadChar[(int)pattern[i]] = i;

			while (s <= (n - m))
			{
				int	j = m - 1;

				while (j >= 0 && pattern[j] == str[s + j])
					--j;

				if (j < 0)
					return true;
				else
					s += Math.Max(1, j - Utility.containsBadChar[(byte)str[s + j]]); // The byte cast fixes a very rare and weird bug where str[s + j] was being an int and not a char, therefore being > 255.
			}

			return false;
		}

		private static readonly string[]		emptyStringArray = new string[0];
		private static readonly List<string>	keywordsPatterns = new List<string>();

		public static string[]	SplitKeywords(string keyword, char separator)
		{
			int	j = 0;

			Utility.keywordsPatterns.Clear();

			// Handle spaces at start and end.
			for (int i = 0; i < keyword.Length; i++)
			{
				if (keyword[i] == separator)
				{
					if (i - j >= 1)
					{
						string	v = keyword.Substring(j, i - j);

						if (v != string.Empty)
							Utility.keywordsPatterns.Add(v);
					}

					j = i + 1;
				}
			}

			if (j < keyword.Length)
			{
				string	v = keyword.Substring(j);

				if (v != string.Empty)
					Utility.keywordsPatterns.Add(v);
			}

			if (Utility.keywordsPatterns.Count == 0)
				return Utility.emptyStringArray;

			return Utility.keywordsPatterns.ToArray();
		}

		public static string	GetLocalIdentifierFromObject(Object asset)
		{
			int		localIdentifier = Unsupported.GetLocalIdentifierInFile(asset.GetInstanceID());
			string	localIdentifierStringified = localIdentifier.ToCachedString();
			string	assetPath = AssetDatabase.GetAssetPath(asset);

			if (string.IsNullOrEmpty(assetPath) == true)
				return string.Empty;

			using (FileStream stream = File.OpenRead(assetPath))
			{
				byte[]	array = new byte[5];
				int		n = stream.Read(array, 0, 5);

				if (n == 5 &&
					array[0] == '%' &&
					array[1] == 'Y' &&
					array[2] == 'A' &&
					array[3] == 'M' &&
					array[4] == 'L')
				{
					return localIdentifierStringified;
				}
			}
			
			using (FileStream fs = File.Open(AssetDatabase.GetAssetPath(asset), FileMode.Open, FileAccess.Read, FileShare.Read))
			using (BufferedStream bs = new BufferedStream(fs))
			using (StreamReader sr = new StreamReader(bs))
			{
				string	line;

				while ((line = sr.ReadLine()) != null)
				{
					if (line.StartsWith("--- !u!") == true)
					{
						if (line.EndsWith(localIdentifierStringified) == true)
							return localIdentifierStringified;

						string	realIdentifier = line.Substring(line.IndexOf('&') + 1);
						long	id = long.Parse(realIdentifier);

						if ((int)id == localIdentifier)
							return realIdentifier;
					}
				}
			}

			return localIdentifierStringified;
		}
	}
}