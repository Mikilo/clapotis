using System;
using System.Text;

namespace NGTools.UON
{
	internal class UInt32UON : UONType
	{
		public override bool	Can(Type t)
		{
			return t == typeof(UInt32);
		}

		public override string	Serialize(UON.SerializationData data, object o)
		{
			return o.ToString();
		}

		public override object	Deserialize(UON.SerializationData data, StringBuilder raw, object instance)
		{
			return UInt32.Parse(raw.ToString());
		}
	}
}