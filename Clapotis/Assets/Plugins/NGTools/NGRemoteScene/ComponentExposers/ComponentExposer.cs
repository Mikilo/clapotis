using System;
using System.Collections.Generic;
using System.Reflection;

namespace NGTools.NGRemoteScene
{
	/// <summary>
	/// <para>Explicitly defines which fields or properties the class must expose.</para>
	/// <para>Used by both the network and the value monitoring system.</para>
	/// </summary>
	public abstract class ComponentExposer
	{
		protected static readonly List<FieldInfo>		Field = new List<FieldInfo>();
		protected static readonly List<PropertyInfo>	Property = new List<PropertyInfo>();
		protected static readonly PropertyInfo[]		EmptyArrayProperty = {};
		protected static readonly FieldInfo[]			EmptyArrayField	= {};

		public readonly Type	type;

		protected	ComponentExposer(Type type)
		{
			this.type = type;
		}

		public virtual PropertyInfo[]	GetPropertyInfos()
		{
			return ComponentExposer.EmptyArrayProperty;
		}

		public virtual FieldInfo[]	GetFieldInfos()
		{
			return ComponentExposer.EmptyArrayField;
		}

		public IFieldModifier[]	GetFields()
		{
			FieldInfo[]			fields = this.GetFieldInfos();
			PropertyInfo[]		properties = this.GetPropertyInfos();
			IFieldModifier[]	result = new IFieldModifier[fields.Length + properties.Length];
			int					i = 0;

			for (; i < fields.Length; ++i)
				result[i] = new FieldModifier(fields[i]);

			for (int j = 0; j < properties.Length; ++j, ++i)
				result[i] = new PropertyModifier(properties[j]);

			return result;
		}
	}
}