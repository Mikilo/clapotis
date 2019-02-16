using System;
#if NETFX_CORE
using System.Reflection;
#endif
using System.Text;

namespace NGTools.UON
{
	internal class DelegateUON : UONType
	{
		public override bool	Can(Type t)
		{
			return typeof(Delegate).IsAssignableFrom(t);
		}

		public override string	Serialize(UON.SerializationData data, object o)
		{
			return null;
		}

		public override object	Deserialize(UON.SerializationData data, StringBuilder raw, object instance)
		{
			return null;
		}
	}
}