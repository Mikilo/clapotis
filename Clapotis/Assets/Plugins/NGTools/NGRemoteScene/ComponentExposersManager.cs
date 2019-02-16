using System;
using System.Collections.Generic;

namespace NGTools.NGRemoteScene
{
	public static class ComponentExposersManager
	{
		private static readonly Dictionary<Type, ComponentExposer>	types = new Dictionary<Type, ComponentExposer>();
		private static readonly ComponentExposer[]					EmptyArray = { };
		private static readonly List<ComponentExposer>				list = new List<ComponentExposer>();

		static	ComponentExposersManager()
		{
			foreach (Type t in Utility.EachAllSubClassesOf(typeof(ComponentExposer)))
			{
				ComponentExposer	exposer = Activator.CreateInstance(t, null) as ComponentExposer;
				ComponentExposersManager.types.Add(exposer.type, exposer);
			}
		}

		public static ComponentExposer[]	GetComponentExposers(Type targetType)
		{
			list.Clear();

			foreach (var item in ComponentExposersManager.types)
			{
				if (item.Key == targetType || targetType.IsSubclassOf(item.Key) == true)
					list.Add(item.Value);
			}

			if (list.Count > 0)
				return list.ToArray();
			return ComponentExposersManager.EmptyArray;
		}
	}
}