using System;
using System.Text;

namespace NGTools.UON
{
	internal class SByteUON : UONType
	{
		public override bool	Can(Type t)
		{
			return t == typeof(SByte);
		}

		public override string	Serialize(UON.SerializationData data, object o)
		{
			return ((sbyte)o).ToHex();
		}

		public override object	Deserialize(UON.SerializationData data, StringBuilder raw, object instance)
		{
			return raw.ToString().HexToSByte();
		}
	}
}