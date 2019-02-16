using System;
#if NETFX_CORE
using System.Linq;
#endif
using System.Reflection;

namespace NGTools
{
	public class FieldModifier : IFieldModifier
	{
		public Type			Type { get { return this.fieldInfo.FieldType; } }
		public string		Name { get { return this.fieldInfo.Name; } }
		public bool			IsPublic { get { return this.fieldInfo.IsPublic; } }
		public MemberInfo	MemberInfo { get { return this.fieldInfo; } }

		public readonly FieldInfo	fieldInfo;

		protected	FieldModifier()
		{
		}

		public	FieldModifier(FieldInfo fieldInfo)
		{
			this.fieldInfo = fieldInfo;
		}

		public void		SetValue(object instance, object value)
		{
			this.fieldInfo.SetValue(instance, value);
		}

		public object	GetValue(object instance)
		{
			return this.fieldInfo.GetValue(instance);
		}

		public T		GetValue<T>(object instance)
		{
			return (T)this.fieldInfo.GetValue(instance);
		}

		public bool		IsDefined(Type type, bool inherit)
		{
			return this.fieldInfo.IsDefined(type, inherit);
		}

		public object[]	GetCustomAttributes(Type type, bool inherit)
		{
			return this.fieldInfo.GetCustomAttributes(type, inherit)
#if NETFX_CORE
				.ToArray()
#endif
			;
		}

		public override string	ToString()
		{
			return this.fieldInfo.ToString();
		}
	}
}