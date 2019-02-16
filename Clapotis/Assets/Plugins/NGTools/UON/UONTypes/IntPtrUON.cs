using System;
using System.Text;

namespace NGTools.UON
{
	internal class IntPtrUON : UONType
	{
		public override bool	Can(Type t)
		{
			return t == typeof(IntPtr) || t == typeof(UIntPtr);
		}

		public override string	Serialize(UON.SerializationData data, object o)
		{
			return null;
		}

		public override object	Deserialize(UON.SerializationData data, StringBuilder raw, object instance)
		{
			return null;
		}
	}
}