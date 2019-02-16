using System;
using System.Collections;
using System.Collections.Generic;
#if NETFX_CORE
using System.Reflection;
#endif

namespace NGTools.NGRemoteScene
{
	using UnityEngine;

	internal static class TypeHandlersManager
	{
		private static readonly TypeHandler[]	typeHandlers;
		private static readonly ClassHandler	classHandler;
		private static readonly ArrayHandler	arrayHandler;

		static	TypeHandlersManager()
		{
			List<TypeHandler>	handlers = new List<TypeHandler>();

			foreach (Type t in Utility.EachAllSubClassesOf(typeof(TypeHandler)))
			{
				if (t == typeof(ClassHandler))
				{
					TypeHandlersManager.classHandler = new ClassHandler();
					handlers.Add(TypeHandlersManager.classHandler);
				}
				else if (t == typeof(ArrayHandler))
				{
					TypeHandlersManager.arrayHandler = new ArrayHandler();
					handlers.Add(TypeHandlersManager.arrayHandler);
				}
				else
					handlers.Add((TypeHandler)Activator.CreateInstance(t));
			}

			handlers.Sort((a, b) => (a.GetType().GetCustomAttributes(typeof(PriorityAttribute), false) as PriorityAttribute[])[0].priority -
									(b.GetType().GetCustomAttributes(typeof(PriorityAttribute), false) as PriorityAttribute[])[0].priority);

			TypeHandlersManager.typeHandlers = handlers.ToArray();

			InternalNGDebug.Assert(TypeHandlersManager.classHandler != null, "Ref-type handler does not exist.");
			InternalNGDebug.Assert(TypeHandlersManager.arrayHandler != null, "Array-type handler does not exist.");
		}

		public static ClassHandler	GetClassHandler()
		{
			return TypeHandlersManager.classHandler;
		}

		public static ArrayHandler	GetArrayHandler()
		{
			return TypeHandlersManager.arrayHandler;
		}

		public static TypeHandler	GetTypeHandler(string assemblyType)
		{
			Type	t = Type.GetType(assemblyType);

			if (t == null)
			{
				int	assemblyMask = 0;

				if (assemblyType.EndsWith(",Assembly-CSharp-firstpass") == true)
					assemblyMask = -1 ^ 1;
				else if (assemblyType.EndsWith(",Assembly-CSharp") == true)
					assemblyMask = -1 ^ 2;
				else if (assemblyType.EndsWith(",NGRemoteScene") == true)
					assemblyMask = -1 ^ 4;

				int	n = assemblyType.IndexOf(',');

				if (n == -1)
					return null;

				if ((assemblyMask & 1) != 0)
					t = Type.GetType(assemblyType.Substring(0, n) + ",Assembly-CSharp-firstpass");
				if ((assemblyMask & 2) != 0 && t == null)
					t = Type.GetType(assemblyType.Substring(0, n) + ",Assembly-CSharp");
				if ((assemblyMask & 4) != 0 && t == null)
					t = Type.GetType(assemblyType.Substring(0, n) + ",NGRemoteScene");

				if (t == null)
					return null;
			}

			for (int i = 0; i < TypeHandlersManager.typeHandlers.Length; i++)
			{
				if (TypeHandlersManager.typeHandlers[i].GetType() == t)
					return TypeHandlersManager.typeHandlers[i];
			}

			return null;
		}

		public static TypeHandler	GetTypeHandler<T>()
		{
			for (int i = 0; i < TypeHandlersManager.typeHandlers.Length; i++)
			{
				if (TypeHandlersManager.typeHandlers[i].CanHandle(typeof(T)))
					return TypeHandlersManager.typeHandlers[i];
			}

			throw new MissingTypeHandlerException(typeof(T));
		}

		public static TypeHandler	GetTypeHandler(Type targetType)
		{
			if (targetType == null)
				return null;

			for (int i = 0; i < TypeHandlersManager.typeHandlers.Length; i++)
			{
				if (TypeHandlersManager.typeHandlers[i].CanHandle(targetType))
					return TypeHandlersManager.typeHandlers[i];
			}

			throw new MissingTypeHandlerException(targetType);
		}

		public static TypeSignature	GetTypeSignature(Type type)
		{
			if (type == null)
				return TypeSignature.Null;

			if (type.IsPrimitive() == true || type == typeof(Decimal) || type == typeof(string))
				return TypeSignature.Primitive;

			if (type.IsEnum() == true || type == typeof(EnumInstance))
				return TypeSignature.Enum;

			if (typeof(Object).IsAssignableFrom(type) == true || type == typeof(UnityObject))
				return TypeSignature.UnityObject;

			if (type.IsArray == true)
				return TypeSignature.Array | TypeHandlersManager.GetTypeSignature(type.GetElementType());

			if (type.GetInterface(typeof(IList<>).Name) != null) // IList<> with Serializable elements.
				return TypeSignature.Array | TypeHandlersManager.GetTypeSignature(type.GetInterface(typeof(IList<>).Name).GetGenericArguments()[0]);

			if (typeof(IList).IsAssignableFrom(type) == true)
				return TypeSignature.Array;

			return TypeSignature.Class;
		}

		public static Type	GetClientType(Type type)
		{
			return TypeHandlersManager.GetClientType(type, TypeHandlersManager.GetTypeSignature(type));
		}

		public static Type	GetClientType(Type type, TypeSignature typeSignature)
		{
			if ((typeSignature & TypeSignature.Primitive) != 0)
				return type;

			if ((typeSignature & TypeSignature.Enum) != 0)
			{
				if ((typeSignature & TypeSignature.Array) != 0)
					return typeof(EnumInstance[]);
				else
					return typeof(EnumInstance);
			}

			if ((typeSignature & TypeSignature.UnityObject) != 0)
			{
				if ((typeSignature & TypeSignature.Array) != 0)
					return typeof(UnityObject[]);
				else
					return typeof(UnityObject);
			}

			if ((typeSignature & TypeSignature.Class) != 0)
			{
				if ((typeSignature & TypeSignature.Array) != 0)
					return typeof(ClientClass[]);
				else
					return typeof(ClientClass);
			}

			throw new Exception("ClientTypes " + typeSignature + " is unknown.");
		}
	}
}