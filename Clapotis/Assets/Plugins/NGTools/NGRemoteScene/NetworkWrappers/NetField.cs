using System;
#if NETFX_CORE
using System.Reflection;
#endif

namespace NGTools.NGRemoteScene
{
	public sealed class NetField
	{
		public readonly Type			fieldType;
		public readonly string			name;
		public readonly bool			isPublic;
		public readonly TypeHandler		handler;
		public readonly TypeSignature	typeSignature;
		public readonly object			value;

		public static void	Serialize(ByteBuffer buffer, object instance, IFieldModifier field)
		{
			using (SafeWrapByteBuffer.Get(buffer))
			{
				buffer.AppendUnicodeString(field.Type.GetShortAssemblyType());
				buffer.AppendUnicodeString(field.Name);
				buffer.Append(field.IsPublic);

				TypeHandler	handler = TypeHandlersManager.GetTypeHandler(field.Type);

				if (handler != null)
				{
					if (field.MemberInfo.DeclaringType.IsGenericTypeDefinition == false || ((field is FieldModifier) == true && (field as FieldModifier).fieldInfo.IsLiteral == true))
					{
						buffer.AppendUnicodeString(handler.GetType().GetShortAssemblyType());

						try
						{
							buffer.Append((byte)TypeHandlersManager.GetTypeSignature(field.Type));

							ByteBuffer	handlerBuffer = Utility.GetBBuffer();
							handler.Serialize(handlerBuffer, field.Type, field.GetValue(instance));
							buffer.Append(Utility.ReturnBBuffer(handlerBuffer));
						}
						catch (Exception ex)
						{
							buffer.Append((byte)TypeSignature.Null);
							InternalNGDebug.LogException("Member \"" + field.Name + "\" failed.", ex);
							throw;
						}
					}
					else // Leave it unsupported.
						buffer.Append(0);
				}
				else
					buffer.Append(0);
			}
		}

		public static NetField	Deserialize(ByteBuffer buffer)
		{
			return new NetField(buffer);
		}

		private	NetField(ByteBuffer buffer)
		{
			using (SafeUnwrapByteBuffer unwrap = SafeUnwrapByteBuffer.Get(buffer, this.GetError))
			{
				this.fieldType = Type.GetType(buffer.ReadUnicodeString());
				this.name = buffer.ReadUnicodeString();
				this.isPublic = buffer.ReadBoolean();

				string	typeHandlerType = buffer.ReadUnicodeString();

				if (string.IsNullOrEmpty(typeHandlerType) == false)
				{
					this.handler = TypeHandlersManager.GetTypeHandler(typeHandlerType);

					if (this.handler != null)
					{
						this.typeSignature = (TypeSignature)buffer.ReadByte();
						this.fieldType = this.fieldType ?? TypeHandlersManager.GetClientType(this.handler.type, this.typeSignature);

						if (this.typeSignature != TypeSignature.Null)
						{
							try
							{
								this.value = this.handler.Deserialize(buffer, this.fieldType ?? TypeHandlersManager.GetClientType(this.handler.type, this.typeSignature));
							}
							catch (Exception ex)
							{
								InternalNGDebug.LogException("Member \"" + this.name + "\" of type \"" + this.fieldType + "\" failed.", ex);
								throw;
							}
						}
					}
					else // Client does not know how to deserialize this field.
						unwrap.ForceFallback();
				}
			}
		}

		private string	GetError()
		{
			return "Member \"" + this.name + "\" (" + this.fieldType + ", " + this.typeSignature + ")";
		}
	}
}