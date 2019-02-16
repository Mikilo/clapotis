using System;
using System.Collections;
#if NETFX_CORE
using System.Reflection;
#endif
using System.Text;
using UnityEngine;

namespace NGTools.UON
{
	internal class ListUON : UONType
	{
		public override bool	Can(Type t)
		{
			return typeof(IList).IsAssignableFrom(t);
		}

		public override string	Serialize(UON.SerializationData data, object o)
		{
			int	refIndex;

			if (data.GetReferenceIndex(o, out refIndex) == true)
				return "#" + refIndex;

			StringBuilder	buffer = UONUtility.GetBuffer();
			IList			array = o as IList;

			data.workingType = UONUtility.GetArraySubType(data.workingType);

			buffer.Append('[');

			foreach (object element in array)
			{
				string	raw = data.ToUON(element);

				if (string.IsNullOrEmpty(raw) == false)
				{
					if (buffer.Length > 1)
						buffer.Append(',');

					buffer.Append(raw);
				}
			}

			buffer.Append(']');

			return UONUtility.ReturnBuffer(buffer);
		}

		public override object	Deserialize(UON.SerializationData data, StringBuilder raw, object instance)
		{
			if (raw[0] == '#')
				return data.deserializedReferences[int.Parse(raw.ToString(1, raw.Length - 1))];

			if (raw[0] != '[')
				throw new FormatException("No opening char '[' found in \"" + raw + "\".");

			StringBuilder	currentType = UONUtility.GetBuffer();
			int				deep = 0;
			IList			list;

			if (instance == null)
				instance = Activator.CreateInstance(data.latestType);

			data.deserializedReferences.Add(instance);

			list = instance as IList;

			if (list == null)
				throw new InvalidCastException("The given object of type \"" + instance.GetType() + "\" does not implement interface IList.");
			else
				list.Clear();

			for (int i = 1; i < raw.Length; i++)
			{
				if (raw[i] == '{' || raw[i] == '[')
					++deep;
				else if (raw[i] == '}' || raw[i] == ']' || raw[i] == ',')
				{
					if (deep == 0 && currentType.Length > 0)
					{
						if (UON.VerboseLevel > 0)
							Debug.Log(currentType.ToString());

						if (currentType[0] == '(')
						{
							int	n = currentType.IndexOf(")");
							data.latestType = data.registeredTypes[int.Parse(currentType.ToString(1, n - 1))];
							currentType = currentType.Remove(0, n + 1);
						}

						list.Add(data.FromUON(data.latestType, currentType));

						currentType.Length = 0;
						continue;
					}

					if (raw[i] == '}' || raw[i] == ']')
						--deep;
				}

				currentType.Append(raw[i]);
			}

			UONUtility.RestoreBuffer(currentType);

			return instance;
		}
	}
}