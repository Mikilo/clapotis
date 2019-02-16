using UnityEngine;

namespace NGTools.NGRemoteScene
{
	public sealed class NetMaterial
	{
		public readonly int						instanceID;
		public readonly string					name;
		public readonly string					shader;
		/// <summary>Is set to null if NG Shader is not available.</summary>
		public readonly NetMaterialProperty[]	properties;

		public static void	Serialize(ByteBuffer buffer, Material mat, NGShader shader)
		{
			buffer.Append(mat.GetInstanceID());
			buffer.AppendUnicodeString(mat.name);
			buffer.AppendUnicodeString(mat.shader.name);

			if (shader == null)
				buffer.Append(-1);
			else
			{
				buffer.Append(shader.properties.Length);

				for (int i = 0; i < shader.properties.Length; i++)
					NetMaterialProperty.Serialize(buffer, mat, shader.properties[i]);
			}
		}

		public static NetMaterial	Deserialize(ByteBuffer buffer)
		{
			return new NetMaterial(buffer);
		}

		private	NetMaterial(ByteBuffer buffer)
		{
			this.instanceID = buffer.ReadInt32();
			this.name = buffer.ReadUnicodeString();
			this.shader = buffer.ReadUnicodeString();

			int	length = buffer.ReadInt32();

			if (length > -1)
			{
				this.properties = new NetMaterialProperty[length];

				for (int i = 0; i < this.properties.Length; i++)
					this.properties[i] = NetMaterialProperty.Deserialize(buffer);
			}
		}
	}
}