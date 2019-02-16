namespace NGTools.NGRemoteScene
{
	public sealed class NetGameObject
	{
		public readonly bool			active;
		public readonly string			name;
		public readonly int				instanceID;
		public readonly NetGameObject[]	children;

		public static void	Serialize(ServerGameObject node, ByteBuffer buffer)
		{
			using (SafeWrapByteBuffer.Get(buffer))
			{
				buffer.Append(node.gameObject.activeSelf);
				buffer.AppendUnicodeString(node.gameObject.name);
				buffer.Append(node.instanceID);
				buffer.Append(node.children.Count);

				for (int i = 0; i < node.children.Count; i++)
					NetGameObject.Serialize(node.children[i], buffer);
			}
		}

		public static NetGameObject	Deserialize(ByteBuffer buffer)
		{
			return new NetGameObject(buffer);
		}

		private	NetGameObject(ByteBuffer buffer)
		{
			using (SafeUnwrapByteBuffer.Get(buffer, this.GetError))
			{
				this.active = buffer.ReadBoolean();
				this.name = buffer.ReadUnicodeString();
				this.instanceID = buffer.ReadInt32();

				int	length = buffer.ReadInt32();

				this.children = new NetGameObject[length];
				for (int i = 0; i < length; i++)
					this.children[i] = new NetGameObject(buffer);
			}
		}

		private string	GetError()
		{
			return "GameObject " + this.name + " (" + this.active + ", " + this.instanceID + ")";
		}
	}
}