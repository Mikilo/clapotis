using System;
using System.Text;

namespace NGTools.UON
{
	internal class DecimalUON : UONType
	{
		public override bool	Can(Type t)
		{
			return t == typeof(Decimal);
		}

		public override string	Serialize(UON.SerializationData data, object o)
		{
			return o.ToString();
		}

		public override object	Deserialize(UON.SerializationData data, StringBuilder raw, object instance)
		{
			return Decimal.Parse(raw.ToString());
		}
	}
}