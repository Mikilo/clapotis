using System;
using System.Text;

namespace NGTools.UON
{
	internal class StringUON : UONType
	{
		public override bool	Can(Type t)
		{
			return t == typeof(String);
		}

		public override string	Serialize(UON.SerializationData data, object o)
		{
			return "\"" + (o as string).Replace("\"", "\\\"") + '"';
		}

		public override object	Deserialize(UON.SerializationData data, StringBuilder raw, object instance)
		{
			return raw.Replace("\\\"", "\"").ToString(1, raw.Length - 2);
		}
	}
}