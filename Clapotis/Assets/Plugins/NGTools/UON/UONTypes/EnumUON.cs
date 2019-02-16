using System;
using System.Text;

namespace NGTools.UON
{
	internal class EnumUON : UONType
	{
		public override bool	Can(Type t)
		{
			return t.IsEnum() == true;
		}

		public override string	Serialize(UON.SerializationData data, object o)
		{
			return ((int)o).ToString();
		}

		public override object	Deserialize(UON.SerializationData data, StringBuilder raw, object instance)
		{
			return Enum.ToObject(data.latestType, int.Parse(raw.ToString()));
		}
	}
}