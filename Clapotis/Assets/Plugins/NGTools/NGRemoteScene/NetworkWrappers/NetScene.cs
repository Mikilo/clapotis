namespace NGTools.NGRemoteScene
{
	public sealed class NetScene
	{
		public readonly int				buildIndex;
		public readonly string			name;
		public readonly NetGameObject[]	roots;

		public static void	Serialize(ServerScene scene, ByteBuffer buffer)
		{
			buffer.Append(scene.buildIndex);
			buffer.AppendUnicodeString(scene.name);

			buffer.Append(scene.roots.Count);

			for (int i = 0; i < scene.roots.Count; i++)
				NetGameObject.Serialize(scene.roots[i], buffer);
		}

		public static NetScene	Deserialize(ByteBuffer buffer)
		{
			return new NetScene(buffer);
		}

		private	NetScene(ByteBuffer buffer)
		{
			this.buildIndex = buffer.ReadInt32();
			this.name = buffer.ReadUnicodeString();

			int	length = buffer.ReadInt32();

			this.roots = new NetGameObject[length];
			for (int i = 0; i < length; i++)
				this.roots[i] = NetGameObject.Deserialize(buffer);
		}
	}
}