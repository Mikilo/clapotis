using System;
using System.Text;

namespace NGTools.UON
{
	internal abstract class UONType
	{
		public abstract bool	Can(Type t);
		public abstract string	Serialize(UON.SerializationData data, object instance);
		public abstract object	Deserialize(UON.SerializationData data, StringBuilder raw, object instance);

		public string	AppendTypeIfNecessary(UON.SerializationData data, Type targetType)
		{
			if (data.nestedLevel > 0 && (data.latestType == null || data.latestType != targetType))
			{
				data.latestType = targetType;
				return "(" + data.GetTypeIndex(targetType) + ')';
			}

			return string.Empty;
		}
	}
}