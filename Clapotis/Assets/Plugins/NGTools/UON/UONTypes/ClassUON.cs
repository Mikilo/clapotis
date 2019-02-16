using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;

namespace NGTools.UON
{
	internal class ClassUON : UONType
	{
		private enum Step
		{
			OpenQuoteKey,
			Key,
			Colon,
			Value,
			Comma
		}

		private readonly DeserializationData			fieldsData = new DeserializationData();
		private readonly Dictionary<Type, FieldInfo[]>	typesFields = new Dictionary<Type, FieldInfo[]>();

		public override bool	Can(Type t)
		{
			return t.IsClass() == true || t.IsStruct() == true;
		}

		public override string	Serialize(UON.SerializationData data, object o)
		{
			int	refIndex;

			if (data.GetReferenceIndex(o, out refIndex) == true)
				return "#" + refIndex;

			StringBuilder		buffer = UONUtility.GetBuffer();
			FieldInfo[]			fields;
			IUONSerialization	serializationInterface = o as IUONSerialization;

			if (serializationInterface != null)
				serializationInterface.OnSerializing();

			if (this.typesFields.TryGetValue(data.workingType, out fields) == false)
				fields = UONUtility.GetFieldsHierarchyOrdered(data.workingType, typeof(object), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToArray();

			buffer.Append('{');

			foreach (FieldInfo field in fields)
			{
#if NETFX_CORE
				if (field.IsDefined(typeof(NonSerializedAttribute)) == false)
#else
				if (field.IsNotSerialized == false)
#endif
				{
					string	raw = data.ToUON(field.GetValue(o));

					if (string.IsNullOrEmpty(raw) == false)
					{
						if (buffer.Length > 1)
							buffer.Append(',');

						buffer.Append('"');
						buffer.Append(field.Name);
						buffer.Append("\":");
						buffer.Append(raw);
					}
				}
			}

			buffer.Append('}');

			return UONUtility.ReturnBuffer(buffer);
		}

		public override object	Deserialize(UON.SerializationData data, StringBuilder raw, object instance)
		{
			if (raw[0] == '#')
				return data.deserializedReferences[int.Parse(raw.ToString(1, raw.Length - 1))];

			if (raw[0] != '{')
				throw new FormatException("No opening char '{' found in \"" + raw + "\".");

			StringBuilder	currentType = UONUtility.GetBuffer();
			string			key = null;
			int				deep = 0;
			Step			step = Step.OpenQuoteKey;

			if (instance == null)
			{
				if (typeof(ScriptableObject).IsAssignableFrom(data.latestType) == true)
					instance = ScriptableObject.CreateInstance(data.latestType);
				else
				{
#if !NETFX_CORE
					if (data.latestType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) == null)
						instance = FormatterServices.GetUninitializedObject(data.latestType);
					else
#else
					if (data.latestType.GetConstructor(Type.EmptyTypes) == null)
#endif
						instance = Activator.CreateInstance(data.latestType);
				}
			}

			data.deserializedReferences.Add(instance);

			IUONSerialization	deserializationInterface = instance as IUONSerialization;

			if (deserializationInterface != null)
				this.fieldsData.entries.Clear();

			if (raw[1] != '}')
			{
				for (int i = 1; i < raw.Length; i++)
				{
					if (step == Step.OpenQuoteKey)
					{
						if (raw[i] != '"')
							throw new FormatException("Expected '\"' instead of \"" + raw[i] + "\" at position " + i + ".");

						++step;
					}
					else if (step == Step.Key)
					{
						for (; i < raw.Length; i++)
						{
							if (raw[i] == '"')
							{
								key = currentType.ToString();
								currentType.Length = 0;
								++step;
								break;
							}

							currentType.Append(raw[i]);
						}
					}
					else if (step == Step.Colon)
					{
						if (raw[i] != ':')
							throw new FormatException("Expected ':' instead of \"" + raw[i] + "\" at position " + i + ".");

						++step;
					}
					else if (step == Step.Value)
					{
						bool	inText = false;

						for (; i < raw.Length; i++)
						{
							if (raw[i] == '"' && (inText == false || this.IsSpecialCharCancelled(raw, i) == false))
								inText = !inText;
							else if ((raw[i] == '{' || raw[i] == '[') && inText == false)
								++deep;
							else if ((raw[i] == '}' || raw[i] == ']' || raw[i] == ',') && inText == false)
							{
								if (deep == 0 && currentType.Length > 0)
								{
									if (UON.VerboseLevel > 0)
									{
										Debug.Log(key);
										Debug.Log(currentType.ToString());
									}

									if (deserializationInterface != null)
										this.fieldsData.entries.Add(key, currentType.ToString());

									data.AssignField(key, currentType, instance);

									currentType.Length = 0;
									step = Step.Comma;
									--i;
									break;
								}

								if (raw[i] == '}' || raw[i] == ']')
									--deep;
							}

							currentType.Append(raw[i]);
						}
					}
					else if (step == Step.Comma)
					{
						if (raw[i] == '}')
							break;

						if (raw[i] != ',')
							throw new FormatException("Expected ',' instead of \"" + raw[i] + "\" at position " + i + ".");

						step = Step.OpenQuoteKey;
					}
				}
			}

			UONUtility.RestoreBuffer(currentType);

			if (deserializationInterface != null)
				deserializationInterface.OnDeserialized(this.fieldsData);

			return instance;
		}

		private bool	IsSpecialCharCancelled(StringBuilder buffer, int i)
		{
			bool	isCancelled = false;

			for (i = i - 1; i >= 0; --i)
			{
				if (buffer[i] == '\\')
					isCancelled = !isCancelled;
				else
					break;
			}

			return isCancelled;
		}
	}
}