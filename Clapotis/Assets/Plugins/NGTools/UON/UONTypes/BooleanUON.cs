using System;
using System.Text;

namespace NGTools.UON
{
	internal class BooleanUON : UONType
	{
		public override bool	Can(Type t)
		{
			return t == typeof(Boolean);
		}

		public override string	Serialize(UON.SerializationData data, object o)
		{
			return (Boolean)o == true ? "T" : "F";
		}

		public override object	Deserialize(UON.SerializationData data, StringBuilder raw, object instance)
		{
			return raw[0] == 'T' ? true : false;
		}
	}
}