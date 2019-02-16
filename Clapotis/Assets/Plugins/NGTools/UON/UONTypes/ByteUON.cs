using System;
using System.Text;

namespace NGTools.UON
{
	internal class ByteUON : UONType
	{
		public override bool	Can(Type t)
		{
			return t == typeof(Byte);
		}

		public override string	Serialize(UON.SerializationData data, object o)
		{
			return ((byte)o).ToHex();
		}

		public override object	Deserialize(UON.SerializationData data, StringBuilder raw, object instance)
		{
			return raw.ToString().HexToByte();
		}
	}
}