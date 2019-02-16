using System;
using System.Text;

namespace NGTools.UON
{
	internal class SingleUON : UONType
	{
		public override bool	Can(Type t)
		{
			return t == typeof(Single);
		}

		public override string	Serialize(UON.SerializationData data, object o)
		{
			return o.ToString();
		}

		public override object	Deserialize(UON.SerializationData data, StringBuilder raw, object instance)
		{
			return Single.Parse(raw.ToString());
		}
	}
}