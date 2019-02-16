using System;
using System.Text;

namespace NGTools.UON
{
	internal class Int32UON : UONType
	{
		public override bool	Can(Type t)
		{
			return t == typeof(Int32);
		}

		public override string	Serialize(UON.SerializationData data, object o)
		{
			return o.ToString();
		}

		public override object	Deserialize(UON.SerializationData data, StringBuilder raw, object instance)
		{
			return Int32.Parse(raw.ToString());
		}
	}
}