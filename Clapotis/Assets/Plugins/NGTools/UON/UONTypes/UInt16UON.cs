using System;
using System.Text;

namespace NGTools.UON
{
	internal class UInt16UON : UONType
	{
		public override bool	Can(Type t)
		{
			return t == typeof(UInt16);
		}

		public override string	Serialize(UON.SerializationData data, object o)
		{
			return o.ToString();
		}

		public override object	Deserialize(UON.SerializationData data, StringBuilder raw, object instance)
		{
			return UInt16.Parse(raw.ToString());
		}
	}
}