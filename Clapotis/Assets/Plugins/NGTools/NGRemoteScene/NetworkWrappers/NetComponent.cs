using System;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	public sealed class NetComponent
	{
		public readonly int			instanceID;
		public readonly bool		togglable;
		public readonly bool		deletable;
		/// <summary>Only used to detect Renderers and display materials.</summary>
		public readonly Type		type;
		public readonly string		name;
		public readonly NetField[]	fields;
		public readonly NetMethod[]	methods;

		public static void	Serialize(ByteBuffer buffer, ServerComponent component)
		{
			using (SafeWrapByteBuffer.Get(buffer))
			{
				Type	componentType = component.component.GetType();

				buffer.Append(component.instanceID);
				buffer.Append(Utility.IsComponentEnableable(component.component),
							  (component.component is Transform) == false); // Deletable
				buffer.AppendUnicodeString(componentType.GetShortAssemblyType());
				buffer.AppendUnicodeString(componentType.Name);
				buffer.Append(component.fields.Length);

				for (int i = 0; i < component.fields.Length; i++)
				{
					try
					{
						NetField.Serialize(buffer, component.component, component.fields[i]);
					}
					catch (Exception ex)
					{
						InternalNGDebug.LogException("Component \"" + componentType.Name + "\" failed on field \"" + component.fields[i].Name + "\" (" + (i + 1) + "/" + component.fields.Length + ").", ex);
						throw;
					}
				}

				buffer.Append(component.methods.Length);

				for (int i = 0; i < component.methods.Length; i++)
				{
					try
					{
						NetMethod.Serialize(buffer, component.methods[i]);
					}
					catch (Exception ex)
					{
						InternalNGDebug.LogException("Component \"" + componentType.Name + "\" failed on method \"" + component.methods[i].methodInfo.Name + "\" (" + (i + 1) + "/" + component.methods.Length + ").", ex);
						throw;
					}
				}
			}
		}

		public static NetComponent	Deserialize(ByteBuffer buffer)
		{
			return new NetComponent(buffer);
		}

		private	NetComponent(ByteBuffer buffer)
		{
			using (SafeUnwrapByteBuffer.Get(buffer, this.GetError))
			{
				this.instanceID = buffer.ReadInt32();
				buffer.ReadBooleans(out this.togglable, out this.deletable);
				this.type = Type.GetType(buffer.ReadUnicodeString());
				this.name = buffer.ReadUnicodeString();

				int	length = buffer.ReadInt32();

				this.fields = new NetField[length];

				for (int i = 0; i < length; i++)
				{
					try
					{
						this.fields[i] = NetField.Deserialize(buffer);
					}
					catch (Exception ex)
					{
						InternalNGDebug.LogException("Component \"" + this.name + "\" failed on field " + (i + 1) + "/" + length + ".", ex);
						throw;
					}
				}

				length = buffer.ReadInt32();

				this.methods = new NetMethod[length];

				for (int i = 0; i < length; i++)
				{
					try
					{
						this.methods[i] = NetMethod.Deserialize(buffer);
					}
					catch (Exception ex)
					{
						InternalNGDebug.LogException("Component \"" + this.name + "\" failed on method " + (i + 1) + "/" + length + ".", ex);
						throw;
					}
				}
			}
		}

		private string	GetError()
		{
			return "Component \"" + this.name + "\" (" + instanceID + ", " + this.type + ")";
		}
	}
}