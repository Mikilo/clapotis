using System;
using System.Text;
using UnityEngine;

namespace NGTools.UON
{
	internal class ArrayUON : UONType
	{
		public override bool Can(Type t)
		{
			return t.IsArray;
		}

		public override string	Serialize(UON.SerializationData data, object o)
		{
			int	refIndex;

			if (data.GetReferenceIndex(o, out refIndex) == true)
				return "#" + refIndex;

			StringBuilder	buffer = UONUtility.GetBuffer();
			Array			array = o as Array;

			data.workingType = data.workingType.GetElementType();

			buffer.Append('[');
			buffer.Append(array.Length);

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

			if (instance != null && (instance is Array) == false)
				throw new InvalidCastException("The given object of type \"" + instance.GetType() + "\" is not an Array.");

			StringBuilder	currentType = UONUtility.GetBuffer();
			int				deep = 0;
			Type			arrayType = data.latestType;
			Array			array = null;

			for (int i = 1, j = 0; i < raw.Length; i++)
			{
				if (raw[i] == '{' || raw[i] == '[')
					++deep;
				else if (raw[i] == '}' || raw[i] == ']' || raw[i] == ',')
				{
					if (instance == null)
					{
						instance = Array.CreateInstance(arrayType.GetElementType(), int.Parse(currentType.ToString()));
						currentType.Length = 0;
						continue;
					}

					if (array == null)
					{
						array = instance as Array;
						data.deserializedReferences.Add(array);
					}

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

						if (j < array.Length)
							array.SetValue(data.FromUON(data.latestType, currentType), j++);

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