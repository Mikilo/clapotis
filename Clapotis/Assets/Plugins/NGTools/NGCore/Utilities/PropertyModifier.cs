using System;
#if NETFX_CORE
using System.Linq;
#endif
using System.Reflection;

namespace NGTools
{
	public class PropertyModifier : IFieldModifier
	{
		public Type			Type { get { return this.propertyInfo.PropertyType; } }
		public string		Name { get { return this.propertyInfo.Name; } }
		public bool			IsPublic { get { return this.propertyInfo.CanRead; } }
		public MemberInfo	MemberInfo { get { return this.propertyInfo; } }

		public readonly PropertyInfo	propertyInfo;

		public	PropertyModifier(PropertyInfo propertyInfo)
		{
			this.propertyInfo = propertyInfo;
		}

		public void		SetValue(object instance, object value)
		{
			this.propertyInfo.SetValue(instance, value);
		}

		public object	GetValue(object instance)
		{
			return this.propertyInfo.GetValue(instance);
		}

		public T		GetValue<T>(object instance)
		{
			return (T)this.propertyInfo.GetValue(instance);
		}

		public bool		IsDefined(Type type, bool inherit)
		{
			return this.propertyInfo.IsDefined(type, inherit);
		}

		public object[]	GetCustomAttributes(Type type, bool inherit)
		{
			return this.propertyInfo.GetCustomAttributes(type, inherit)
#if NETFX_CORE
				.ToArray()
#endif
			;
		}

		public override string	ToString()
		{
			return this.propertyInfo.ToString();
		}
	}
}