using System;
using System.Collections;
#if NETFX_CORE
using System.Reflection;
#endif

namespace NGTools.NGRemoteScene
{
	internal sealed class ArrayData
	{
		public const int	BigArrayThreshold = 256;

		public readonly	Type	serverType;
		public readonly	bool	isBigArray;
		public Array			array;
		public bool				isNull;

		/// <summary>
		/// Forces the handler to serialize big array. It is a use-once variable.
		/// </summary>
		internal static bool	forceBigArray = false;

		private int	originLength;

		public static void	Serializer(ByteBuffer buffer, Type fieldType, object instance)
		{
			using (SafeWrapByteBuffer.Get(buffer))
			{
				IEnumerable	array = instance as IEnumerable;

				if (array == null)
				{
					buffer.Append(-1); // -1 = null array
					return;
				}

				bool	isBigArray = false;
				int		count = 0;

				if (fieldType.IsArray == true)
				{
					Array	a = array as Array;

					count = a.Length;
					isBigArray = a.Length > ArrayData.BigArrayThreshold;
				}
				else if (typeof(IList).IsAssignableFrom(fieldType) == true)
				{
					IList	a = array as IList;

					count = a.Count;
					isBigArray = a.Count > ArrayData.BigArrayThreshold;
				}
				else
					throw new InvalidCastException("Array of type \"" + fieldType + "\" is not supported.");

				buffer.Append(count);

				if (ArrayData.forceBigArray == true)
				{
					ArrayData.forceBigArray = false;
					isBigArray = false;
				}

				buffer.Append(isBigArray);

				if (isBigArray == false)
				{
					Type		subType = Utility.GetArraySubType(fieldType);
					TypeHandler	subHandler;

					if (subType != null)
						subHandler = TypeHandlersManager.GetTypeHandler(subType);
					else
					{
						subType = typeof(object);
						subHandler = TypeHandlersManager.GetTypeHandler(subType);
					}

					if (subHandler != null)
					{
						buffer.AppendUnicodeString(subHandler.GetType().GetShortAssemblyType());
						buffer.Append((byte)TypeHandlersManager.GetTypeSignature(subType));

						foreach (object item in array)
							subHandler.Serialize(buffer, subType, item);
					}
					else
						buffer.Append(0);
				}
			}
		}

		public static object	Deserialize(ByteBuffer buffer, Type fieldType)
		{
			return new ArrayData(buffer, fieldType);
		}

		private	ArrayData(ByteBuffer buffer, Type fieldType)
		{
			using (SafeUnwrapByteBuffer unwrap = SafeUnwrapByteBuffer.Get(buffer, this.GetError))
			{
				this.serverType = fieldType;
				this.originLength = buffer.ReadInt32();

				if (this.originLength == -1)
					this.isNull = true;
				else
				{
					this.isBigArray = buffer.ReadBoolean();

					if (this.isBigArray == false)
					{
						string	typeHandlerType = buffer.ReadUnicodeString();

						if (string.IsNullOrEmpty(typeHandlerType) == false)
						{
							TypeHandler	subHandler = TypeHandlersManager.GetTypeHandler(typeHandlerType);

							if (subHandler != null)
							{
								TypeSignature	typeSignature = (TypeSignature)buffer.ReadByte();

								if (typeSignature != TypeSignature.Null)
								{
									Type	subType = TypeHandlersManager.GetClientType(this.serverType, typeSignature);

									this.array = Array.CreateInstance(subHandler.type, this.originLength);

									for (int i = 0; i < this.originLength; i++)
									{
										try
										{
											this.array.SetValue(subHandler.Deserialize(buffer, subType), i);
										}
										catch (Exception ex)
										{
											InternalNGDebug.LogException("Array of type " + fieldType.Name + " (" + subType + ") at " + i + " failed.", ex);
											throw;
										}
									}
								}
							}
							else // Client does not know how to deserialize the element.
								unwrap.ForceFallback();
						}
					}
				}
			}
		}

		private string	GetError()
		{
			return "Array[" + this.originLength + "] (" + this.serverType + ")";
		}
	}

	[Priority(100)]
	internal sealed class ArrayHandler : TypeHandler
	{
		public	ArrayHandler() : base(typeof(ArrayData))
		{
		}

		public override bool	CanHandle(Type type)
		{
			return type.IsUnityArray() || type == typeof(ArrayData) || type == typeof(ArrayList);
		}

		public override void	Serialize(ByteBuffer buffer, Type fieldType, object instance)
		{
			ArrayData.Serializer(buffer, fieldType, instance);
		}

		public override object	Deserialize(ByteBuffer buffer, Type fieldType)
		{
			return ArrayData.Deserialize(buffer, fieldType);
		}
	}
}