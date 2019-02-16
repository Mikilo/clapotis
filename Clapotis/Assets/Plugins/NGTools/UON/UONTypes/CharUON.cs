using System;
using System.Text;

namespace NGTools.UON
{
	internal class CharUON : UONType
	{
		public override bool	Can(Type t)
		{
			return t == typeof(Char);
		}

		public override string	Serialize(UON.SerializationData data, object o)
		{
			return o.ToString();
		}

		public override object	Deserialize(UON.SerializationData data, StringBuilder raw, object instance)
		{
			return raw[0];
		}
	}
}