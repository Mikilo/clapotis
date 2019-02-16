using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

#if UON
[assembly: AssemblyTitle("UON")]
[assembly: AssemblyDescription("Unity Object Notation serializer.")]
[assembly: AssemblyProduct("Unity Editor")]
[assembly: AssemblyCompany("Michaël Nguyen")]
[assembly: AssemblyCopyright("Copyright © 2016 - Infinite")]
[assembly: AssemblyFileVersion("1.1")]
#endif

namespace NGTools.UON
{
	/// <summary>
	/// <para>UON transforms any object into Unity Object Notation (UON) and reverses any UON to object.</para>
	/// <para>Author: Michael Nguyen (https://www.assetstore.unity3d.com/en/#!/search/page=1/sortby=popularity/query=publisher:12138)</para>
	/// </summary>
	public static class UON
	{
		internal class SerializationData
		{
			private class RefComparer : IEqualityComparer<object>
			{
				bool	IEqualityComparer<object>.Equals(object x, object y)
				{
					return object.ReferenceEquals(x, y);
				}

				int		IEqualityComparer<object>.GetHashCode(object obj)
				{
					return obj.GetHashCode();
				}
			}

			public Type			workingType;
			public Type			latestType;
			public List<Type>	registeredTypes = new List<Type>();
			public int			nestedLevel;
			public Dictionary<Type, Dictionary<string, FieldInfo>>	fields = new Dictionary<Type, Dictionary<string, FieldInfo>>(16);
			public Dictionary<object, int>	serializedReferences = new Dictionary<object, int>(16, new RefComparer());
			public List<object>				deserializedReferences = new List<object>(16);

			public int	GetTypeIndex(Type t)
			{
				int	n = this.registeredTypes.IndexOf(t);

				if (n == -1)
				{
					this.registeredTypes.Add(t);
					n = this.registeredTypes.Count - 1;
				}

				return n;
			}

			public  bool	GetReferenceIndex(object reference, out int index)
			{
				if (this.serializedReferences.TryGetValue(reference, out index) == false)
				{
					index = this.serializedReferences.Count;
					this.serializedReferences.Add(reference, this.serializedReferences.Count);
					return false;
				}

				return true;
			}

			public string	Generate(string content)
			{
				StringBuilder	buffer = UONUtility.GetBuffer();

				buffer.Append("[[\"");

				for (int i = 0; i < this.registeredTypes.Count; i++)
				{
					if (i > 0)
						buffer.Append("\",\"");

					string	typeStringified = this.registeredTypes[i].GetShortAssemblyType();
					if (Type.GetType(typeStringified) == null)
						Debug.Log("Type \"" + typeStringified + "\" becomes null.");

					buffer.Append(typeStringified);
				}

				buffer.Append("\"],");
				buffer.Append(content);
				buffer.Append(']');

				return UONUtility.ReturnBuffer(buffer);
			}

			public FieldInfo	GetField(Type t, string fieldName)
			{
				Dictionary<string, FieldInfo>	fields;
				FieldInfo						field = null;

				if (this.fields.TryGetValue(t, out fields) == false)
				{
					fields = new Dictionary<string, FieldInfo>();
					this.fields.Add(t, fields);
				}

				if (fields.TryGetValue(fieldName, out field) == false)
				{
					foreach (FieldInfo f in UONUtility.EachFieldHierarchyOrdered(t, typeof(object), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
					{
						if (f.Name == fieldName)
						{
							field = f;
							break;
						}
					}

					if (field == null && UON.VerboseLevel > 1)
						Debug.LogWarning("Field \"" + fieldName + "\" was fot found in \"" + t.Name + "\".");

					fields.Add(fieldName, field);
				}

				return field;
			}

			public string	ToUON(object instance)
			{
				return UON.ToUON(this, instance);
			}

			public object	FromUON(Type type, StringBuilder raw)
			{
				return UON.FromUON(this, type, raw);
			}

			public void		AssignField(string key, StringBuilder raw, object instance)
			{
				UON.AssignField(key, raw, instance, this);
			}
		}

		public static int	VerboseLevel = 0;

		private static readonly UONType[]	types;

		static	UON()
		{
			UON.types = UONUtility.CreateInstancesOf<UONType>();

			for (int i = 0; i < UON.types.Length; i++)
			{
				if (UON.types[i] is ClassUON)
				{
					if (i != UON.types.Length - 1)
					{
						UONType	tmp = UON.types[UON.types.Length - 1];
						UON.types[UON.types.Length - 1] = UON.types[i];
						UON.types[i] = tmp;
					}
				}
			}

			//UnityEditor.EditorApplication.delayCall += () =>
			//{
			//	try
			//	{
			//		//UON.Verbose = true;

			//		UON.Test(BindingFlags.DeclaredOnly | BindingFlags.CreateInstance);
			//		UON.Test(null);
			//		UON.Test(1);
			//		UON.Test(0);
			//		UON.Test(-1);
			//		UON.Test(1.234F);
			//		UON.Test(0F);
			//		UON.Test(-1.234F);
			//		UON.Test(true);
			//		UON.Test(false);
			//		UON.Test(string.Empty);
			//		UON.Test("Abcdefg");
			//		UON.Test(new int[] { 1, 0, -1 });
			//		UON.Test(new byte[] { 1, 0, 255 });
			//		UON.Test(new sbyte[] { -123, 1, 0, 124 });
			//		UON.Test(new float[] { 1.234567F, 0.1234567F, -1.234567F });
			//		UON.Test(new double[] { 1.23456789D, 0.123456789D, -1.23456789D });
			//		UON.Test(new decimal[] { 1.234568790M, 0.1234567890M, -1.234567890M });
			//		UON.Test(new string[] { "aa", "bb", "cc", string.Empty, null });
			//		UON.Test(new string[] { });
			//		UON.Test(new List<int> { 1, 0, -1 });
			//		UON.Test(new List<byte> { 1, 0, 255 });
			//		UON.Test(new List<sbyte> { -123, 1, 0, 124 });
			//		UON.Test(new List<float> { 1F, 0F, -1F });
			//		UON.Test(new List<double> { 1D, 0D, -1D });
			//		UON.Test(new List<decimal> { 1M, 0M, -1M });
			//		UON.Test(new List<string> { "aa", "bb", "cc", string.Empty, null });
			//		UON.Test(new List<string> { });
			//		UON.Test(new Action(() => { }));
			//		UON.Test(new Action<int>((a) => { }));
			//		UON.Test(new Action<int, string>((a, b) => { }));
			//		UON.Test(new Func<string>(() => null));
			//		UON.Test(new Dictionary<int, byte>());
			//		UON.Test(new Dictionary<Action, Func<string>>());
			//		UON.Test(new List<Action<int, Action<string, Dictionary<int, LocalVariableInfo>>>>());
			//		var t1 = typeof(int);
			//		var t2 = typeof(bool);
			//		UON.Test(new List<object>() { t1, t1, t2, null });
			//		UON.Test(new object[] { t1, t1, t2, null });
			//		UON.Test(t1);
			//		UON.Test(t2);
			//	}
			//	catch (Exception ex)
			//	{
			//		Debug.LogException(ex);
			//	}
			//};
		}

		//public static void Test(object o)
		//{
		//	try
		//	{
		//		string a = UON.ToUON(o);
		//		Debug.Log(a);
		//		object c = UON.FromUON(a);
		//		string b = UON.ToUON(c);
		//		Debug.Log(b);
		//		Debug.Log(b.Equals(a) == true ? "<color=green>VALID</color>" : "<color=red>FAILED</color>");
		//	}
		//	catch (Exception ex)
		//	{
		//		Debug.LogException(ex);
		//	}
		//}

		/// <summary>Converts <paramref name="instance"/> into UON.</summary>
		/// <param name="instance">Can be of any types, Unity Object, array, list, struct or class.</param>
		/// <returns>A UON of the given <paramref name="instance"/>.</returns>
		public static string	ToUON(object instance)
		{
			if (instance == null)
				return "[]";

			SerializationData	data = new SerializationData() { workingType = instance.GetType() };

			data.registeredTypes.Add(instance.GetType());

			for (int i = 0; i < UON.types.Length; i++)
			{
				if (UON.types[i] is UnityObjectUON)
					continue;

				if (UON.types[i].Can(data.workingType) == true)
					return data.Generate(UON.types[i].Serialize(data, instance));
			}

			throw new UnhandledTypeException("Object of type " + instance.GetType().FullName + " not handled.");
		}

		/// <summary>Reverses UON into object.</summary>
		/// <param name="rawUON">Any UON.</param>
		/// <param name="instance">The <paramref name="instance"/> to overwrite instead of a complete new object.</param>
		/// <returns>Returns a new object or the given <paramref name="instance"/>.</returns>
		public static object	FromUON(string rawUON, object instance = null)
		{
			if (UON.VerboseLevel > 0)
				Debug.Log(rawUON);

			if (string.IsNullOrEmpty(rawUON) == true || rawUON.Length <= 4)
				return null;

			if (rawUON[0] != '[')
				throw new FormatException("Expected '[' instead of \"" + rawUON[0] + "\" at position 0.");

			SerializationData	data = new SerializationData();
			int					i = 0;

			if (rawUON[1] != '[')
				throw new FormatException("Expected '[' instead of \"" + rawUON[1] + "\" at position 1.");

			StringBuilder	currentType = UONUtility.GetBuffer();
			bool			isOpen = false;
			int				deep = 0;

			i = 1;

			for (; i < rawUON.Length; i++)
			{
				if (rawUON[i] == '[')
					++deep;
				else if (rawUON[i] == ']')
				{
					--deep;
					if (deep == 0)
						break;
				}

				if (rawUON[i] == '"')
				{
					isOpen = !isOpen;

					if (isOpen == false)
					{
						Type	t = Type.GetType(currentType.ToString());

						if (t == null)
							throw new FormatException("A registered type is null using \"" + currentType.ToString() + "\".");

						data.registeredTypes.Add(t);
						currentType.Length = 0;
					}
				}
				else if (isOpen == true)
					currentType.Append(rawUON[i]);
			}

			if (data.registeredTypes.Count == 0)
				return null;

			UONUtility.RestoreBuffer(currentType);

			data.workingType = data.registeredTypes[0];
			data.latestType = data.registeredTypes[0];

			for (int j = 0; j < UON.types.Length; j++)
			{
				if (UON.types[j] is UnityObjectUON)
					continue;

				if (UON.types[j].Can(data.workingType) == true)
				{
					StringBuilder	buffer = UONUtility.GetBuffer(rawUON);

					try
					{
						buffer.Length -= 1;
						buffer.Remove(0, i + 2);
						return UON.types[j].Deserialize(data, buffer, instance);
					}
					catch (Exception ex)
					{
						if (UON.VerboseLevel > 1)
						{
							Debug.LogException(ex);
							Debug.LogError(buffer);
						}
					}
					finally
					{
						UONUtility.RestoreBuffer(buffer);
					}

					break;
				}
			}

			return null;
		}

		private static string	ToUON(SerializationData data, object instance)
		{
			if (instance == null)
				return "NULL";

			data.workingType = instance.GetType();

			try
			{
				++data.nestedLevel;

				for (int i = 0; i < UON.types.Length; i++)
				{
					if (UON.types[i].Can(data.workingType) == true)
					{
						try
						{
							string	typeChange = UON.types[i].AppendTypeIfNecessary(data, instance.GetType());
							string	content = UON.types[i].Serialize(data, instance);

							if (content == null)
								return null;

							return typeChange + content;
						}
						catch (Exception ex)
						{
							Debug.LogException(ex);
							return string.Empty;
						}
					}
				}
			}
			finally
			{
				--data.nestedLevel;
			}

			throw new UnhandledTypeException("Object of type " + instance.GetType().FullName + " not handled.");
		}

		private static object	FromUON(SerializationData data, Type type, StringBuilder raw)
		{
			if (raw.Length == 4 && raw[0] == 'N' && raw[1] == 'U' && raw[2] == 'L' && raw[3] == 'L')
				return null;

			for (int i = 0; i < UON.types.Length; i++)
			{
				if (UON.types[i].Can(type) == true)
					return UON.types[i].Deserialize(data, raw, null);
			}

			return null;
		}

		private static void		AssignField(string key, StringBuilder raw, object o, SerializationData data)
		{
			FieldInfo	f = data.GetField(o.GetType(), key);

			if (f == null)
				return;

			if (raw.Length == 4 && raw[0] == 'N' && raw[1] == 'U' && raw[2] == 'L' && raw[3] == 'L')
			{
				f.SetValue(o, null);
				return;
			}

			if (raw[0] == '(')
			{
				int	n = raw.IndexOf(")");
				data.latestType = data.registeredTypes[int.Parse(raw.ToString(1, n - 1))];
				raw = raw.Remove(0, n + 1);
			}

			if (f.FieldType.IsAssignableFrom(data.latestType) == false)
			{
				if (UON.VerboseLevel > 1)
					Debug.Log("Type \"" + data.latestType.Name + "\" can not be assigned on \"" + f + "\"." + raw);
				return;
			}

			for (int i = 0; i < UON.types.Length; i++)
			{
				if (UON.types[i].Can(data.latestType) == true)
				{
					try
					{
						f.SetValue(o, UON.types[i].Deserialize(data, raw, null));
					}
					catch (Exception ex)
					{
						if (UON.VerboseLevel > 1)
							Debug.LogError(ex.GetType().Name + ": " + f + " from " + f.DeclaringType.Name + "with \"" + raw + "\" failed." + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace);
					}
					break;
				}
			}
		}
	}
}