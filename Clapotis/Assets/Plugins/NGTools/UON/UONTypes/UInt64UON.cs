using System;
using System.Text;

namespace NGTools.UON
{
	internal class UInt64UON : UONType
	{
		public override bool	Can(Type t)
		{
			return t == typeof(UInt64);
		}

		public override string	Serialize(UON.SerializationData data, object o)
		{
			return o.ToString();
		}

		public override object	Deserialize(UON.SerializationData data, StringBuilder raw, object instance)
		{
			return UInt64.Parse(raw.ToString());
		}
	}
}