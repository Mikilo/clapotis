using NGTools.Network;
using System;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.StaticClass_ServerSendTypeStaticMembers)]
	internal sealed class ServerSendTypeStaticMembersPacket : ResponsePacket
	{
		public IFieldModifier[]	members;
		public int				typeIndex;
		public bool[]			editableMembers;
		public NetField[]		netMembers;

		public	ServerSendTypeStaticMembersPacket(int networkId) : base(networkId)
		{
		}

		private	ServerSendTypeStaticMembersPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public override void	Out(ByteBuffer buffer)
		{
			if (this.OutResponseStatus(buffer))
			{
				buffer.Append(this.typeIndex);
				buffer.Append(this.members.Length);

				for (int i = 0; i < this.members.Length; i++)
				{
					using (SafeWrapByteBuffer wrap = SafeWrapByteBuffer.Get(buffer))
					{
						try
						{
							if (this.members[i] is FieldModifier)
							{
								FieldModifier	modifier = (this.members[i] as FieldModifier);

								buffer.Append(modifier.fieldInfo.IsLiteral == false || modifier.fieldInfo.IsInitOnly == true);
							}
							else
								buffer.Append((this.members[i] as PropertyModifier).propertyInfo.GetSetMethod() != null);

							NetField.Serialize(buffer, null, this.members[i]);
						}
						catch (Exception ex)
						{
							InternalNGDebug.LogException("Inspectable type index \"" + this.typeIndex + "\" at static member \"" + this.members[i].Name + "\" failed.", ex);
							wrap.Erase();
						}
					}
				}
			}
		}

		public override void	In(ByteBuffer buffer)
		{
			if (this.InResponseStatus(buffer) == true)
			{
				this.typeIndex = buffer.ReadInt32();
				this.editableMembers = new bool[buffer.ReadInt32()];
				this.netMembers = new NetField[this.editableMembers.Length];

				for (int i = 0; i < this.netMembers.Length; i++)
				{
					using (SafeUnwrapByteBuffer unwrap = SafeUnwrapByteBuffer.Get(buffer, this.GetError))
					{
						if (unwrap.IsValid() == true)
						{
							try
							{
								this.editableMembers[i] = buffer.ReadBoolean();
								this.netMembers[i] = NetField.Deserialize(buffer);
							}
							catch (Exception ex)
							{
								InternalNGDebug.LogException("Inspectable type index \"" + this.typeIndex + "\" at static member #" + i + " failed.", ex);
							}
						}
					}
				}
			}
		}

		private string	GetError()
		{
			return "Inspectable type \"" + this.typeIndex + "\"";
		}
	}
}