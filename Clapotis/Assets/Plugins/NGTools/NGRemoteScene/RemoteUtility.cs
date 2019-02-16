using System;
using System.Collections.Generic;
using System.Reflection;

namespace NGTools.NGRemoteScene
{
	using UnityEngine;

	public static class RemoteUtility
	{
		/// <summary>
		/// Gets all fields exposable in Inspector.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="exposers"></param>
		/// <returns></returns>
		public static FieldInfo[]		GetExposedFields(Type type, ComponentExposer[] exposers)
		{
			Stack<Type>		inheritances = new Stack<Type>();
			List<FieldInfo>	fields = new List<FieldInfo>();

			inheritances.Push(type);
			type = type.BaseType();

			while (type != null && type != typeof(Component) && type != typeof(object))
			{
				inheritances.Push(type);
				type = type.BaseType();
			}

			foreach (Type ty in inheritances)
			{
				int	j = 0;

				for (; j < exposers.Length; j++)
				{
					if (exposers[j].type == ty)
					{
						fields.AddRange(exposers[j].GetFieldInfos());
						break;
					}
				}

				if (j < exposers.Length)
					continue;

				FieldInfo[]	fis = ty.GetFields(Utility.ExposedBindingFlags | BindingFlags.DeclaredOnly);

				for (int i = 0; i < fis.Length; i++)
				{
					if (Utility.CanExposeFieldInInspector(fis[i]) == false)
						continue;

					fields.Add(fis[i]);
				}
			}

			return fields.ToArray();
		}

		/// <summary>
		/// Gets all properties exposable in Inspector.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="exposers"></param>
		/// <returns></returns>
		public static PropertyInfo[]	GetExposedProperties(Type type, ComponentExposer[] exposers)
		{
			Stack<Type>			inheritances = new Stack<Type>();
			List<PropertyInfo>	properties = new List<PropertyInfo>();

			inheritances.Push(type);
			type = type.BaseType();

			while (type != null && type != typeof(Component) && type != typeof(object))
			{
				inheritances.Push(type);
				type = type.BaseType();
			}

			foreach (Type ty in inheritances)
			{
				int	j = 0;

				for (; j < exposers.Length; j++)
				{
					if (exposers[j].type == ty)
					{
						properties.AddRange(exposers[j].GetPropertyInfos());
						break;
					}
				}

				if (j < exposers.Length)
					continue;

				PropertyInfo[]	pis = ty.GetProperties(Utility.ExposedBindingFlags | BindingFlags.DeclaredOnly);

				for (int i = 0; i < pis.Length; i++)
				{
					if (Utility.CanExposePropertyInInspector(pis[i]) == false)
					{
						continue;
					}

					// GetGetMethod can return null ns a weird case even if a real getter is present.
					MethodInfo	getMethod = pis[i].GetGetMethod();
					if (getMethod != null && RemoteUtility.CheckPropertyIsOverriding(getMethod.GetBaseDefinition(), properties) == true) // Skip overriden properties.
						continue;

					properties.Add(pis[i]);
				}
			}

			return properties.ToArray();
		}

		private static bool	CheckPropertyIsOverriding(MethodInfo getter, List<PropertyInfo> properties)
		{
			for (int i = 0; i < properties.Count; i++)
			{
				if (properties[i].GetGetMethod() == getter)
					return true;
			}

			return false;
		}

		private static IObjectImporter[]	objectImporters;

		public static IObjectImporter	GetImportAssetTypeSupported(Type type)
		{
			if (RemoteUtility.objectImporters == null)
			{
				List<IObjectImporter> list = new List<IObjectImporter>();

				foreach (Type subType in Utility.EachAllAssignableFrom(typeof(IObjectImporter)))
					list.Add(Activator.CreateInstance(subType) as IObjectImporter);

				RemoteUtility.objectImporters = list.ToArray();
			}

			for (int i = 0; i < RemoteUtility.objectImporters.Length; i++)
			{
				if (RemoteUtility.objectImporters[i].CanHandle(type) == true)
					return RemoteUtility.objectImporters[i];
			}

			return null;
		}
	}
}