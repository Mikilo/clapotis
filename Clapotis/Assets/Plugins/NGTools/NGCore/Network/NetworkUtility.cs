using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace NGTools.Network
{
	public static class ByteBufferExtension
	{
		public static IEnumerable	ProgressiveAppendUnicodeString(this ByteBuffer buffer, string content)
		{
			if (content == null)
			{
				while (buffer.ProgressiveAppend(-1) == false)
					yield return null;
			}
			else if (content.Length == 0)
			{
				while (buffer.ProgressiveAppend(0) == false)
					yield return null;
			}
			else
			{
				foreach (var item in buffer.ProgressiveAppendBytes(Encoding.UTF8.GetBytes(content)))
					yield return null;
			}
		}

		public static IEnumerable	ProgressiveAppendBytes(this ByteBuffer buffer, byte[] src)
		{
			while (buffer.ProgressiveAppend(src.Length) == false)
				yield return null;

			for (int i = 0; i < src.Length;)
			{
				int	length = buffer.Capacity - buffer.Length;

				if (length > 0)
				{
					buffer.Append(src, i, length);
					i += length;
				}

				yield return null;
			}
		}

		public static bool	ProgressiveAppend(this ByteBuffer buffer, Boolean value)
		{
			return buffer.ProgressiveAppend((byte)(value == true ? 1 : 0));
		}

		public static bool	ProgressiveAppend(this ByteBuffer buffer, Byte value)
		{
			if (buffer.Length + sizeof(Byte) > buffer.Capacity)
				return false;

			buffer.Append(value);
			return true;
		}

		public static bool	ProgressiveAppend(this ByteBuffer buffer, SByte value)
		{
			return buffer.ProgressiveAppend((Byte)value);
		}

		public static bool	ProgressiveAppend(this ByteBuffer buffer, Char value)
		{
			return buffer.ProgressiveAppend((Byte)value);
		}

		public static bool	ProgressiveAppend(this ByteBuffer buffer, Int16 value)
		{
			if (buffer.Length + sizeof(Int16) > buffer.Capacity)
				return false;

			buffer.Append(value);
			return true;
		}

		public static bool	ProgressiveAppend(this ByteBuffer buffer, Int32 value)
		{
			if (buffer.Length + sizeof(Int32) > buffer.Capacity)
				return false;

			buffer.Append(value);
			return true;
		}

		public static bool	ProgressiveAppend(this ByteBuffer buffer, Int64 value)
		{
			if (buffer.Length + sizeof(Int64) > buffer.Capacity)
				return false;

			buffer.Append(value);
			return true;
		}

		public static bool	ProgressiveAppend(this ByteBuffer buffer, UInt16 value)
		{
			if (buffer.Length + sizeof(UInt16) > buffer.Capacity)
				return false;

			buffer.Append(value);
			return true;
		}

		public static bool	ProgressiveAppend(this ByteBuffer buffer, UInt32 value)
		{
			if (buffer.Length + sizeof(UInt32) > buffer.Capacity)
				return false;

			buffer.Append(value);
			return true;
		}

		public static bool	ProgressiveAppend(this ByteBuffer buffer, UInt64 value)
		{
			if (buffer.Length + sizeof(UInt64) > buffer.Capacity)
				return false;

			buffer.Append(value);
			return true;
		}

		public static bool	ProgressiveAppend(this ByteBuffer buffer, Single value)
		{
			if (buffer.Length + sizeof(Single) > buffer.Capacity)
				return false;

			buffer.Append(value);
			return true;
		}

		public static bool	ProgressiveAppend(this ByteBuffer buffer, Double value)
		{
			if (buffer.Length + sizeof(Double) > buffer.Capacity)
				return false;

			buffer.Append(value);
			return true;
		}

		public static IEnumerable	ProgressiveUnicodeString(this ByteBuffer buffer, Action<string> callback)
		{
			int	length = 0;

			while (buffer.ProgressiveReadInt32(ref length) == false)
				yield return null;

			if (length == -1)
				callback(null);
			else if (length == 0)
				callback(string.Empty);
			else
			{
				foreach (var item in buffer.ProgressiveReadBytes(array => callback(Encoding.UTF8.GetString(array))))
					yield return null;
			}
		}

		public static IEnumerable	ProgressiveReadBytes(this ByteBuffer buffer, int length, Action<byte[]> callback)
		{
			byte[]	array = new byte[length];

			for (int i = 0; i < array.Length;)
			{
				int	remaining = buffer.Length - buffer.Position;

				if (remaining > 0)
				{
					if (remaining > array.Length - i)
						remaining = array.Length - i;

					Buffer.BlockCopy(buffer.GetRawBuffer(), buffer.Position, array, i, remaining);
					i += remaining;

					if (i == array.Length)
					{
						callback(array);
						yield break;
					}
				}

				yield return null;
			}

			throw new Exception();
		}

		public static bool	ProgressiveReadBoolean(this ByteBuffer buffer, ref Boolean value)
		{
			if (buffer.Position + sizeof(Boolean) > buffer.Length)
				return false;

			value = buffer.ReadBoolean();
			return true;
		}

		public static bool	ProgressiveReadChar(this ByteBuffer buffer, ref Char value)
		{
			if (buffer.Position + sizeof(Char) > buffer.Length)
				return false;

			value = buffer.ReadChar();
			return true;
		}

		public static bool	ProgressiveReadByte(this ByteBuffer buffer, ref Byte value)
		{
			if (buffer.Position + sizeof(Byte) > buffer.Length)
				return false;

			value = buffer.ReadByte();
			return true;
		}

		public static bool	ProgressiveReadSByte(this ByteBuffer buffer, ref SByte value)
		{
			if (buffer.Position + sizeof(SByte) > buffer.Length)
				return false;

			value = buffer.ReadSByte();
			return true;
		}

		public static IEnumerable	ProgressiveReadBytes(this ByteBuffer buffer, Action<byte[]> callback)
		{
			int	length = 0;

			while (buffer.ProgressiveReadInt32(ref length) == false)
				yield return null;

			foreach (var item in buffer.ProgressiveReadBytes(length, callback))
				yield return null;
		}

		public static bool	ProgressiveReadInt16(this ByteBuffer buffer, ref Int16 value)
		{
			if (buffer.Position + sizeof(Int16) > buffer.Length)
				return false;

			value = buffer.ReadInt16();
			return true;
		}

		public static bool	ProgressiveReadInt32(this ByteBuffer buffer, ref Int32 value)
		{
			if (buffer.Position + sizeof(Int32) > buffer.Length)
				return false;

			value = buffer.ReadInt32();
			return true;
		}

		public static bool	ProgressiveReadInt64(this ByteBuffer buffer, ref Int64 value)
		{
			if (buffer.Position + sizeof(Int64) > buffer.Length)
				return false;

			value = buffer.ReadInt64();
			return true;
		}

		public static bool	ProgressiveReadUInt16(this ByteBuffer buffer, ref UInt16 value)
		{
			if (buffer.Position + sizeof(UInt16) > buffer.Length)
				return false;

			value = buffer.ReadUInt16();
			return true;
		}

		public static bool	ProgressiveReadUInt32(this ByteBuffer buffer, ref UInt32 value)
		{
			if (buffer.Position + sizeof(UInt32) > buffer.Length)
				return false;

			value = buffer.ReadUInt32();
			return true;
		}

		public static bool	ProgressiveReadUInt64(this ByteBuffer buffer, ref UInt64 value)
		{
			if (buffer.Position + sizeof(UInt64) > buffer.Length)
				return false;

			value = buffer.ReadUInt64();
			return true;
		}

		public static bool	ProgressiveReadSingle(this ByteBuffer buffer, ref Single value)
		{
			if (buffer.Position + sizeof(Single) > buffer.Length)
				return false;

			value = buffer.ReadSingle();
			return true;
		}

		public static bool	ProgressiveReadDouble(this ByteBuffer buffer, ref Double value)
		{
			if (buffer.Position + sizeof(Double) > buffer.Length)
				return false;

			value = buffer.ReadDouble();
			return true;
		}
	}

	public static class NetworkUtility
	{
		private static Dictionary<Type, FieldInfo[]>	cachedPacketFields = new Dictionary<Type, FieldInfo[]>();

		public static IEnumerator	ProgressiveObjectToBuffer(object instance, ByteBuffer buffer)
		{
			FieldInfo[]	fis = NetworkUtility.GetFields(instance.GetType());

			for (int i = 0; i < fis.Length; i++)
			{
				Debug.Log("Appending " + fis[i].Name);
				if (fis[i].FieldType == typeof(Int32))
				{
					while (buffer.ProgressiveAppend((Int32)fis[i].GetValue(instance)) == false)
						yield return null;
				}
				else if (fis[i].FieldType == typeof(Single))
				{
					while (buffer.ProgressiveAppend((Single)fis[i].GetValue(instance)) == false)
						yield return null;
				}
				else if (fis[i].FieldType == typeof(String))
				{
					foreach (var item in buffer.ProgressiveAppendUnicodeString((String)fis[i].GetValue(instance)))
						yield return null;
				}
				else if (fis[i].FieldType.IsEnum() == true)
				{
					while (buffer.ProgressiveAppend((Int32)fis[i].GetValue(instance)) == false)
						yield return null;
				}
				else if (fis[i].FieldType == typeof(Boolean))
				{
					while (buffer.ProgressiveAppend((Boolean)fis[i].GetValue(instance)) == false)
						yield return null;
				}
				else if (fis[i].FieldType == typeof(Byte))
				{
					while (buffer.ProgressiveAppend((Byte)fis[i].GetValue(instance)) == false)
						yield return null;
				}
				else if (fis[i].FieldType == typeof(SByte))
				{
					while (buffer.ProgressiveAppend((SByte)fis[i].GetValue(instance)) == false)
						yield return null;
				}
				else if (fis[i].FieldType == typeof(Char))
				{
					while (buffer.ProgressiveAppend((Char)fis[i].GetValue(instance)) == false)
						yield return null;
				}
				else if (fis[i].FieldType == typeof(Double))
				{
					while (buffer.ProgressiveAppend((Double)fis[i].GetValue(instance)) == false)
						yield return null;
				}
				else if (fis[i].FieldType == typeof(Int16))
				{
					while (buffer.ProgressiveAppend((Int16)fis[i].GetValue(instance)) == false)
						yield return null;
				}
				else if (fis[i].FieldType == typeof(Int64))
				{
					while (buffer.ProgressiveAppend((Int64)fis[i].GetValue(instance)) == false)
						yield return null;
				}
				else if (fis[i].FieldType == typeof(UInt16))
				{
					while (buffer.ProgressiveAppend((UInt16)fis[i].GetValue(instance)) == false)
						yield return null;
				}
				else if (fis[i].FieldType == typeof(UInt32))
				{
					while (buffer.ProgressiveAppend((UInt32)fis[i].GetValue(instance)) == false)
						yield return null;
				}
				else if (fis[i].FieldType == typeof(UInt64))
				{
					while (buffer.ProgressiveAppend((UInt64)fis[i].GetValue(instance)) == false)
						yield return null;
				}
				else if (fis[i].FieldType == typeof(Vector2))
				{
					Vector2	v = (Vector2)fis[i].GetValue(instance);

					while (buffer.ProgressiveAppend(v.x) == false)
						yield return null;
					while (buffer.ProgressiveAppend(v.y) == false)
						yield return null;
				}
				else if (fis[i].FieldType == typeof(Vector3))
				{
					Vector3	v = (Vector3)fis[i].GetValue(instance);

					while (buffer.ProgressiveAppend(v.x) == false)
						yield return null;
					while (buffer.ProgressiveAppend(v.y) == false)
						yield return null;
					while (buffer.ProgressiveAppend(v.z) == false)
						yield return null;
				}
				else if (fis[i].FieldType == typeof(Vector4))
				{
					Vector4	v = (Vector4)fis[i].GetValue(instance);

					while (buffer.ProgressiveAppend(v.x) == false)
						yield return null;
					while (buffer.ProgressiveAppend(v.y) == false)
						yield return null;
					while (buffer.ProgressiveAppend(v.z) == false)
						yield return null;
					while (buffer.ProgressiveAppend(v.w) == false)
						yield return null;
				}
				else if (fis[i].FieldType == typeof(Rect))
				{
					Rect	r = (Rect)fis[i].GetValue(instance);

					while (buffer.ProgressiveAppend(r.x) == false)
						yield return null;
					while (buffer.ProgressiveAppend(r.y) == false)
						yield return null;
					while (buffer.ProgressiveAppend(r.width) == false)
						yield return null;
					while (buffer.ProgressiveAppend(r.height) == false)
						yield return null;
				}
				else if (fis[i].FieldType == typeof(Color))
				{
					Color	c = (Color)fis[i].GetValue(instance);

					while (buffer.ProgressiveAppend(c.r) == false)
						yield return null;
					while (buffer.ProgressiveAppend(c.g) == false)
						yield return null;
					while (buffer.ProgressiveAppend(c.b) == false)
						yield return null;
					while (buffer.ProgressiveAppend(c.a) == false)
						yield return null;
				}
				else if (fis[i].FieldType == typeof(Quaternion))
				{
					Quaternion q = (Quaternion)fis[i].GetValue(instance);

					while (buffer.ProgressiveAppend(q.x) == false)
						yield return null;
					while (buffer.ProgressiveAppend(q.y) == false)
						yield return null;
					while (buffer.ProgressiveAppend(q.z) == false)
						yield return null;
					while (buffer.ProgressiveAppend(q.w) == false)
						yield return null;
				}
				else if (fis[i].FieldType == typeof(Type))
				{
					foreach (var item in buffer.ProgressiveAppendUnicodeString(((Type)fis[i].GetValue(instance)).GetShortAssemblyType()))
						yield return null;
				}
				else if (fis[i].FieldType == typeof(Byte[]))
				{
					Byte[]	a = (Byte[])fis[i].GetValue(instance);

					if (a != null)
					{
						foreach (var item in buffer.ProgressiveAppendBytes(a))
							yield return null;
					}
					else
					{
						while (buffer.ProgressiveAppend(-1) == false)
							yield return null;
					}
				}
				else if (fis[i].FieldType.IsUnityArray() == true)
				{
					object	rawArray = fis[i].GetValue(instance);

					if (rawArray != null)
					{
						ICollectionModifier	collectionModifier = Utility.GetCollectionModifier(rawArray);

						while (buffer.ProgressiveAppend(collectionModifier.Size) == false)
							yield return null;

						//foreach (var item in NetworkUtility.ProgressiveAppendArrayToBuffer(collectionModifier, Utility.GetArraySubType(fis[i].FieldType), buffer))
						//	yield return null;

						Utility.ReturnCollectionModifier(collectionModifier);
					}
					else
					{
						while (buffer.ProgressiveAppend(-1) == false)
							yield return null;
					}
				}
				else
					throw new NotSupportedException("Type \"" + fis[i].FieldType + "\" is not supported.");
			}
		}
		
		public static IEnumerator	ProgressiveBufferToObject(object instance, ByteBuffer buffer)
		{
			FieldInfo[]	fis = NetworkUtility.GetFields(instance.GetType());

			for (int i = 0; i < fis.Length; i++)
			{
				Debug.Log("Reading " + fis[i].Name);
				if (fis[i].FieldType == typeof(Int32))
				{
					Int32	value = default(Int32);

					while (buffer.ProgressiveReadInt32(ref value) == false)
						yield return null;

					fis[i].SetValue(instance, value);
				}
				else if (fis[i].FieldType == typeof(Single))
				{
					Single	value = default(Single);

					while (buffer.ProgressiveReadSingle(ref value) == false)
						yield return null;

					fis[i].SetValue(instance, value);
				}
				else if (fis[i].FieldType == typeof(String))
				{
					foreach (var item in buffer.ProgressiveUnicodeString(value => fis[i].SetValue(instance, value)))
						yield return null;
				}
				else if (fis[i].FieldType.IsEnum() == true)
				{
					Int32	value = default(Int32);

					while (buffer.ProgressiveReadInt32(ref value) == false)
						yield return null;

					fis[i].SetValue(instance, value);
				}
				else if (fis[i].FieldType == typeof(Boolean))
				{
					Boolean	value = default(Boolean);

					while (buffer.ProgressiveReadBoolean(ref value) == false)
						yield return null;

					fis[i].SetValue(instance, value);
				}
				else if (fis[i].FieldType == typeof(Byte))
				{
					Byte	value = default(Byte);

					while (buffer.ProgressiveReadByte(ref value) == false)
						yield return null;

					fis[i].SetValue(instance, value);
				}
				else if (fis[i].FieldType == typeof(SByte))
				{
					SByte	value = default(SByte);

					while (buffer.ProgressiveReadSByte(ref value) == false)
						yield return null;

					fis[i].SetValue(instance, value);
				}
				else if (fis[i].FieldType == typeof(Char))
				{
					Char	value = default(Char);

					while (buffer.ProgressiveReadChar(ref value) == false)
						yield return null;

					fis[i].SetValue(instance, value);
				}
				else if (fis[i].FieldType == typeof(Double))
				{
					Double	value = default(Double);

					while (buffer.ProgressiveReadDouble(ref value) == false)
						yield return null;

					fis[i].SetValue(instance, value);
				}
				else if (fis[i].FieldType == typeof(Int16))
				{
					Int16	value = default(Int16);

					while (buffer.ProgressiveReadInt16(ref value) == false)
						yield return null;

					fis[i].SetValue(instance, value);
				}
				else if (fis[i].FieldType == typeof(Int64))
				{
					Int64	value = default(Int64);

					while (buffer.ProgressiveReadInt64(ref value) == false)
						yield return null;

					fis[i].SetValue(instance, value);
				}
				else if (fis[i].FieldType == typeof(UInt16))
				{
					UInt16	value = default(UInt16);

					while (buffer.ProgressiveReadUInt16(ref value) == false)
						yield return null;

					fis[i].SetValue(instance, value);
				}
				else if (fis[i].FieldType == typeof(UInt32))
				{
					UInt32	value = default(UInt32);

					while (buffer.ProgressiveReadUInt32(ref value) == false)
						yield return null;

					fis[i].SetValue(instance, value);
				}
				else if (fis[i].FieldType == typeof(UInt64))
				{
					UInt64	value = default(UInt64);

					while (buffer.ProgressiveReadUInt64(ref value) == false)
						yield return null;

					fis[i].SetValue(instance, value);
				}
				else if (fis[i].FieldType == typeof(Vector2))
				{
					Vector2	value = new Vector2();

					while (buffer.ProgressiveReadSingle(ref value.x) == false)
						yield return null;
					while (buffer.ProgressiveReadSingle(ref value.y) == false)
						yield return null;

					fis[i].SetValue(instance, value);
				}
				else if (fis[i].FieldType == typeof(Vector3))
				{
					Vector3	value = new Vector3();

					while (buffer.ProgressiveReadSingle(ref value.x) == false)
						yield return null;
					while (buffer.ProgressiveReadSingle(ref value.y) == false)
						yield return null;
					while (buffer.ProgressiveReadSingle(ref value.z) == false)
						yield return null;

					fis[i].SetValue(instance, value);
				}
				else if (fis[i].FieldType == typeof(Vector4))
				{
					Vector4	value = new Vector4();

					while (buffer.ProgressiveReadSingle(ref value.x) == false)
						yield return null;
					while (buffer.ProgressiveReadSingle(ref value.y) == false)
						yield return null;
					while (buffer.ProgressiveReadSingle(ref value.z) == false)
						yield return null;
					while (buffer.ProgressiveReadSingle(ref value.w) == false)
						yield return null;

					fis[i].SetValue(instance, value);
				}
				else if (fis[i].FieldType == typeof(Rect))
				{
					Single	x = default(Single), y = default(Single), z = default(Single), w = default(Single);

					while (buffer.ProgressiveReadSingle(ref x) == false)
						yield return null;
					while (buffer.ProgressiveReadSingle(ref y) == false)
						yield return null;
					while (buffer.ProgressiveReadSingle(ref z) == false)
						yield return null;
					while (buffer.ProgressiveReadSingle(ref w) == false)
						yield return null;

					fis[i].SetValue(instance, new Rect(x, y, z, w));
				}
				else if (fis[i].FieldType == typeof(Color))
				{
					Color	value = new Color();

					while (buffer.ProgressiveReadSingle(ref value.r) == false)
						yield return null;
					while (buffer.ProgressiveReadSingle(ref value.g) == false)
						yield return null;
					while (buffer.ProgressiveReadSingle(ref value.b) == false)
						yield return null;
					while (buffer.ProgressiveReadSingle(ref value.a) == false)
						yield return null;

					fis[i].SetValue(instance, value);
				}
				else if (fis[i].FieldType == typeof(Quaternion))
				{
					Quaternion	value = new Quaternion();

					while (buffer.ProgressiveReadSingle(ref value.x) == false)
						yield return null;
					while (buffer.ProgressiveReadSingle(ref value.y) == false)
						yield return null;
					while (buffer.ProgressiveReadSingle(ref value.z) == false)
						yield return null;
					while (buffer.ProgressiveReadSingle(ref value.w) == false)
						yield return null;

					fis[i].SetValue(instance, value);
				}
				else if (fis[i].FieldType == typeof(Type))
				{
					foreach (var item in buffer.ProgressiveUnicodeString(value => fis[i].SetValue(instance, Type.GetType(value))))
						yield return null;
				}
				else if (fis[i].FieldType == typeof(Byte[]))
				{
					Int32	length = 0;

					while (buffer.ProgressiveReadInt32(ref length) == false)
						yield return null;

					if (length == -1)
						fis[i].SetValue(instance, null);
					else
					{
						foreach (var item in buffer.ProgressiveReadBytes(length, array => fis[i].SetValue(instance, array)))
							yield return null;
					}
				}
				else if (fis[i].FieldType.IsUnityArray() == true)
				{
					Int32	length = 0;

					while (buffer.ProgressiveReadInt32(ref length) == false)
						yield return null;

					if (length == -1)
						fis[i].SetValue(instance, null);
					else
					{
						object	array;
						Type	subType;

						if (fis[i].FieldType.IsArray == true)
						{
							subType = fis[i].FieldType.GetElementType();
							array = Array.CreateInstance(fis[i].FieldType.GetElementType(), length);
						}
						else
						{
							array = Activator.CreateInstance(fis[i].FieldType, length);
							IList	list = (IList)array;
							object	defaultValue;

							subType = Utility.GetArraySubType(fis[i].FieldType);

							if (subType.IsValueType() == true)
								defaultValue = Activator.CreateInstance(subType);
							else
								defaultValue = null;

							for (int j = 0; j < length; j++)
								list.Add(defaultValue);
						}

						ICollectionModifier	collectionModifier = Utility.GetCollectionModifier(array);

						//foreach (var item in NetworkUtility.ProgressiveReadArrayFromBuffer(collectionModifier, subType, buffer))
						//	yield return null;

						Utility.ReturnCollectionModifier(collectionModifier);

						fis[i].SetValue(instance, array);
					}
				}
				else
					throw new NotSupportedException("Type \"" + fis[i].FieldType + "\" is not supported.");
			}
		}

		public static void	ObjectToBuffer(object instance, ByteBuffer buffer)
		{
			FieldInfo[]	fis = NetworkUtility.GetFields(instance.GetType());

			for (int i = 0; i < fis.Length; i++)
			{
				if (fis[i].FieldType == typeof(Int32))
					buffer.Append((Int32)fis[i].GetValue(instance));
				else if (fis[i].FieldType == typeof(Single))
					buffer.Append((Single)fis[i].GetValue(instance));
				else if (fis[i].FieldType == typeof(String))
					buffer.AppendUnicodeString((String)fis[i].GetValue(instance));
				else if (fis[i].FieldType.IsEnum() == true)
					buffer.Append((Int32)fis[i].GetValue(instance));
				else if (fis[i].FieldType == typeof(Boolean))
					buffer.Append((Boolean)fis[i].GetValue(instance));
				else if (fis[i].FieldType == typeof(Byte))
					buffer.Append((Byte)fis[i].GetValue(instance));
				else if (fis[i].FieldType == typeof(SByte))
					buffer.Append((SByte)fis[i].GetValue(instance));
				else if (fis[i].FieldType == typeof(Char))
					buffer.Append((Char)fis[i].GetValue(instance));
				else if (fis[i].FieldType == typeof(Double))
					buffer.Append((Double)fis[i].GetValue(instance));
				else if (fis[i].FieldType == typeof(Int16))
					buffer.Append((Int16)fis[i].GetValue(instance));
				else if (fis[i].FieldType == typeof(Int64))
					buffer.Append((Int64)fis[i].GetValue(instance));
				else if (fis[i].FieldType == typeof(UInt16))
					buffer.Append((UInt16)fis[i].GetValue(instance));
				else if (fis[i].FieldType == typeof(UInt32))
					buffer.Append((UInt32)fis[i].GetValue(instance));
				else if (fis[i].FieldType == typeof(UInt64))
					buffer.Append((UInt64)fis[i].GetValue(instance));
				else if (fis[i].FieldType == typeof(Vector2))
				{
					Vector2	v = (Vector2)fis[i].GetValue(instance);
					buffer.Append(v.x);
					buffer.Append(v.y);
				}
				else if (fis[i].FieldType == typeof(Vector3))
				{
					Vector3	v = (Vector3)fis[i].GetValue(instance);
					buffer.Append(v.x);
					buffer.Append(v.y);
					buffer.Append(v.z);
				}
				else if (fis[i].FieldType == typeof(Vector4))
				{
					Vector4	v = (Vector4)fis[i].GetValue(instance);
					buffer.Append(v.x);
					buffer.Append(v.y);
					buffer.Append(v.z);
					buffer.Append(v.w);
				}
				else if (fis[i].FieldType == typeof(Rect))
				{
					Rect	r = (Rect)fis[i].GetValue(instance);
					buffer.Append(r.x);
					buffer.Append(r.y);
					buffer.Append(r.width);
					buffer.Append(r.height);
				}
				else if (fis[i].FieldType == typeof(Color))
				{
					Color	c = (Color)fis[i].GetValue(instance);
					buffer.Append(c.r);
					buffer.Append(c.g);
					buffer.Append(c.b);
					buffer.Append(c.a);
				}
				else if (fis[i].FieldType == typeof(Quaternion))
				{
					Quaternion	q = (Quaternion)fis[i].GetValue(instance);
					buffer.Append(q.x);
					buffer.Append(q.y);
					buffer.Append(q.z);
					buffer.Append(q.w);
				}
				else if (fis[i].FieldType == typeof(Type))
					buffer.AppendUnicodeString(((Type)fis[i].GetValue(instance)).GetShortAssemblyType());
				else if (fis[i].FieldType == typeof(Byte[]))
				{
					Byte[]	a = (Byte[])fis[i].GetValue(instance);

					if (a != null)
						buffer.AppendBytes(a);
					else
						buffer.Append(-1);
				}
				else if (fis[i].FieldType.IsUnityArray() == true)
				{
					object	rawArray = fis[i].GetValue(instance);

					if (rawArray != null)
					{
						ICollectionModifier	collectionModifier = Utility.GetCollectionModifier(rawArray);

						buffer.Append(collectionModifier.Size);

						NetworkUtility.AppendArrayToBuffer(collectionModifier, Utility.GetArraySubType(fis[i].FieldType), buffer);
						Utility.ReturnCollectionModifier(collectionModifier);
					}
					else
						buffer.Append(-1);
				}
				else
					throw new NotSupportedException("Type \"" + fis[i].FieldType + "\" is not supported.");
			}
		}

		public static void	BufferToObject(object instance, ByteBuffer buffer)
		{
			FieldInfo[]	fis = NetworkUtility.GetFields(instance.GetType());

			for (int i = 0; i < fis.Length; i++)
			{
				if (fis[i].FieldType == typeof(Int32))
					fis[i].SetValue(instance, buffer.ReadInt32());
				else if (fis[i].FieldType == typeof(Single))
					fis[i].SetValue(instance, buffer.ReadSingle());
				else if (fis[i].FieldType == typeof(String))
					fis[i].SetValue(instance, buffer.ReadUnicodeString());
				else if (fis[i].FieldType.IsEnum() == true)
					fis[i].SetValue(instance, buffer.ReadInt32());
				else if (fis[i].FieldType == typeof(Boolean))
					fis[i].SetValue(instance, buffer.ReadBoolean());
				else if (fis[i].FieldType == typeof(Byte))
					fis[i].SetValue(instance, buffer.ReadByte());
				else if (fis[i].FieldType == typeof(SByte))
					fis[i].SetValue(instance, buffer.ReadSByte());
				else if (fis[i].FieldType == typeof(Char))
					fis[i].SetValue(instance, buffer.ReadChar());
				else if (fis[i].FieldType == typeof(Double))
					fis[i].SetValue(instance, buffer.ReadDouble());
				else if (fis[i].FieldType == typeof(Int16))
					fis[i].SetValue(instance, buffer.ReadInt16());
				else if (fis[i].FieldType == typeof(Int64))
					fis[i].SetValue(instance, buffer.ReadInt64());
				else if (fis[i].FieldType == typeof(UInt16))
					fis[i].SetValue(instance, buffer.ReadUInt16());
				else if (fis[i].FieldType == typeof(UInt32))
					fis[i].SetValue(instance, buffer.ReadUInt32());
				else if (fis[i].FieldType == typeof(UInt64))
					fis[i].SetValue(instance, buffer.ReadUInt64());
				else if (fis[i].FieldType == typeof(Vector2))
					fis[i].SetValue(instance, new Vector2(buffer.ReadSingle(), buffer.ReadSingle()));
				else if (fis[i].FieldType == typeof(Vector3))
					fis[i].SetValue(instance, new Vector3(buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle()));
				else if (fis[i].FieldType == typeof(Vector4))
					fis[i].SetValue(instance, new Vector4(buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle()));
				else if (fis[i].FieldType == typeof(Rect))
					fis[i].SetValue(instance, new Rect(buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle()));
				else if (fis[i].FieldType == typeof(Color))
					fis[i].SetValue(instance, new Color(buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle()));
				else if (fis[i].FieldType == typeof(Quaternion))
					fis[i].SetValue(instance, new Quaternion(buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle()));
				else if (fis[i].FieldType == typeof(Type))
					fis[i].SetValue(instance, Type.GetType(buffer.ReadUnicodeString()));
				else if (fis[i].FieldType == typeof(Byte[]))
				{
					int	length = buffer.ReadInt32();

					if (length == -1)
						fis[i].SetValue(instance, null);
					else
						fis[i].SetValue(instance, buffer.ReadBytes(length));
				}
				else if (fis[i].FieldType.IsUnityArray() == true)
				{
					int	length = buffer.ReadInt32();

					if (length == -1)
						fis[i].SetValue(instance, null);
					else
					{
						object	array;
						Type	subType;

						if (fis[i].FieldType.IsArray == true)
						{
							subType = fis[i].FieldType.GetElementType();
							array = Array.CreateInstance(fis[i].FieldType.GetElementType(), length);
						}
						else
						{
							array = Activator.CreateInstance(fis[i].FieldType, length);
							IList	list = (IList)array;
							object	defaultValue;

							subType = Utility.GetArraySubType(fis[i].FieldType);

							if (subType.IsValueType() == true)
								defaultValue = Activator.CreateInstance(subType);
							else
								defaultValue = null;

							for (int j = 0; j < length; j++)
								list.Add(defaultValue);
						}

						ICollectionModifier	collectionModifier = Utility.GetCollectionModifier(array);

						NetworkUtility.ReadArrayFromBuffer(collectionModifier, subType, buffer);
						Utility.ReturnCollectionModifier(collectionModifier);

						fis[i].SetValue(instance, array);
					}
				}
				else
					throw new NotSupportedException("Type \"" + fis[i].FieldType + "\" is not supported.");
			}
		}

		private static void	AppendArrayToBuffer(ICollectionModifier array, Type type, ByteBuffer buffer)
		{
			if (type == typeof(Int32))
			{
				for (int i = 0; i < array.Size; i++)
					buffer.Append((Int32)array.Get(i));
			}
			else if (type == typeof(Single))
			{
				for (int i = 0; i < array.Size; i++)
					buffer.Append((Single)array.Get(i));
			}
			else if (type == typeof(String))
			{
				for (int i = 0; i < array.Size; i++)
					buffer.AppendUnicodeString((string)array.Get(i));
			}
			else if (type.IsEnum() == true)
			{
				for (int i = 0; i < array.Size; i++)
					buffer.Append((Int32)array.Get(i));
			}
			else if (type == typeof(Boolean))
			{
				for (int i = 0; i < array.Size; i++)
					buffer.Append((Boolean)array.Get(i));
			}
			else if (type == typeof(Byte))
			{
				for (int i = 0; i < array.Size; i++)
					buffer.Append((Byte)array.Get(i));
			}
			else if (type == typeof(SByte))
			{
				for (int i = 0; i < array.Size; i++)
					buffer.Append((SByte)array.Get(i));
			}
			else if (type == typeof(Char))
			{
				for (int i = 0; i < array.Size; i++)
					buffer.Append((Char)array.Get(i));
			}
			else if (type == typeof(Double))
			{
				for (int i = 0; i < array.Size; i++)
					buffer.Append((Double)array.Get(i));
			}
			else if (type == typeof(Int16))
			{
				for (int i = 0; i < array.Size; i++)
					buffer.Append((Int16)array.Get(i));
			}
			else if (type == typeof(Int64))
			{
				for (int i = 0; i < array.Size; i++)
					buffer.Append((Int64)array.Get(i));
			}
			else if (type == typeof(UInt16))
			{
				for (int i = 0; i < array.Size; i++)
					buffer.Append((UInt16)array.Get(i));
			}
			else if (type == typeof(UInt32))
			{
				for (int i = 0; i < array.Size; i++)
					buffer.Append((UInt32)array.Get(i));
			}
			else if (type == typeof(UInt64))
			{
				for (int i = 0; i < array.Size; i++)
					buffer.Append((UInt64)array.Get(i));
			}
			else if (type.IsUnityArray() == true)
			{
				for (int i = 0; i < array.Size; i++)
				{
					object				rawArray = array.Get(i);
					ICollectionModifier	collectionModifier = Utility.GetCollectionModifier(rawArray);

					buffer.Append(collectionModifier.Size);

					NetworkUtility.AppendArrayToBuffer(collectionModifier, Utility.GetArraySubType(type), buffer);
					Utility.ReturnCollectionModifier(collectionModifier);
				}
			}
			else
				throw new NotSupportedException("Type \"" + type + "\" is not supported.");
		}

		private static void	ReadArrayFromBuffer(ICollectionModifier array, Type type, ByteBuffer buffer)
		{
			if (type == typeof(Int32))
			{
				for (int i = 0; i < array.Size; i++)
					array.Set(i, buffer.ReadInt32());
			}
			else if (type == typeof(Single))
			{
				for (int i = 0; i < array.Size; i++)
					array.Set(i, buffer.ReadSingle());
			}
			else if (type == typeof(String))
			{
				for (int i = 0; i < array.Size; i++)
					array.Set(i, buffer.ReadUnicodeString());
			}
			else if (type.IsEnum() == true)
			{
				for (int i = 0; i < array.Size; i++)
					array.Set(i, buffer.ReadInt32());
			}
			else if (type == typeof(Boolean))
			{
				for (int i = 0; i < array.Size; i++)
					array.Set(i, buffer.ReadBoolean());
			}
			else if (type == typeof(Byte))
			{
				for (int i = 0; i < array.Size; i++)
					array.Set(i, buffer.ReadByte());
			}
			else if (type == typeof(SByte))
			{
				for (int i = 0; i < array.Size; i++)
					array.Set(i, buffer.ReadSByte());
			}
			else if (type == typeof(Char))
			{
				for (int i = 0; i < array.Size; i++)
					array.Set(i, buffer.ReadChar());
			}
			else if (type == typeof(Double))
			{
				for (int i = 0; i < array.Size; i++)
					array.Set(i, buffer.ReadDouble());
			}
			else if (type == typeof(Int16))
			{
				for (int i = 0; i < array.Size; i++)
					array.Set(i, buffer.ReadInt16());
			}
			else if (type == typeof(Int64))
			{
				for (int i = 0; i < array.Size; i++)
					array.Set(i, buffer.ReadInt64());
			}
			else if (type == typeof(UInt16))
			{
				for (int i = 0; i < array.Size; i++)
					array.Set(i, buffer.ReadUInt16());
			}
			else if (type == typeof(UInt32))
			{
				for (int i = 0; i < array.Size; i++)
					array.Set(i, buffer.ReadUInt32());
			}
			else if (type == typeof(UInt64))
			{
				for (int i = 0; i < array.Size; i++)
					array.Set(i, buffer.ReadUInt64());
			}
			else if (type.IsUnityArray() == true)
			{
				for (int i = 0; i < array.Size; i++)
				{
					object	subArray;
					Type	subType;

					if (type.IsArray == true)
					{
						subType = type.GetElementType();
						subArray = Array.CreateInstance(type.GetElementType(), buffer.ReadInt32());
					}
					else
					{
						int		count = buffer.ReadInt32();
						subArray = Activator.CreateInstance(type, count);
						IList	list = (IList)subArray;
						object	defaultValue;

						subType = Utility.GetArraySubType(type);

						if (subType.IsValueType() == true)
							defaultValue = Activator.CreateInstance(subType);
						else
							defaultValue = null;

						for (int j = 0; j < count; j++)
							list.Add(defaultValue);
					}

					ICollectionModifier	collectionModifier = Utility.GetCollectionModifier(subArray);

					NetworkUtility.ReadArrayFromBuffer(collectionModifier, subType, buffer);
					Utility.ReturnCollectionModifier(collectionModifier);

					array.Set(i, subArray);
				}
			}
			else
				throw new NotSupportedException("Type \"" + type + "\" is not supported.");
		}

		private static FieldInfo[]	GetFields(Type type)
		{
			FieldInfo[]	result;

			if (NetworkUtility.cachedPacketFields.TryGetValue(type, out result) == false)
			{
				List<FieldInfo>	list = new List<FieldInfo>();

				foreach (FieldInfo f in Utility.EachFieldHierarchyOrdered(type, typeof(object), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
				{
					if (f.IsDefined(typeof(StripFromNetworkAttribute), false) == true)
						continue;

					if (f.IsPublic == true || f.IsDefined(typeof(SerializeField), false) == true)
						list.Add(f);
				}

				result = list.ToArray();
				NetworkUtility.cachedPacketFields.Add(type, result);
			}

			return result;
		}
	}
}