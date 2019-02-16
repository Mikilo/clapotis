using NGTools;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	/// <summary>A set of tools to serialize a Shader and its properties into a ByteBuffer.</summary>
	/// <see cref="NGTools.NGRemoteScene.NGShader"/>
	/// <see cref="NGTools.NGRemoteScene.NGShaderProperty"/>
	public static class NGShaderUtility
	{
		public static void	SerializeShader(Shader shader, ByteBuffer buffer)
		{
			buffer.AppendUnicodeString(shader.name);

			int	total = ShaderUtil.GetPropertyCount(shader);

			buffer.Append(total);

			for (int i = 0; i < total; i++)
				NGShaderUtility.SerializeShaderProperty(shader, i, buffer);
		}

		public static void	SerializeShaderProperty(Shader shader, int i, ByteBuffer buffer)
		{
			string							description = ShaderUtil.GetPropertyDescription(shader, i);
			string							name = ShaderUtil.GetPropertyName(shader, i);
			ShaderUtil.ShaderPropertyType	propertyType = ShaderUtil.GetPropertyType(shader, i);
			bool							hidden = ShaderUtil.IsShaderPropertyHidden(shader, i);

			buffer.AppendUnicodeString(description);
			buffer.AppendUnicodeString(name);
			buffer.Append((int)propertyType);
			buffer.Append(hidden);

			buffer.Append(ShaderUtil.GetRangeLimits(shader, i, 1));
			buffer.Append(ShaderUtil.GetRangeLimits(shader, i, 2));
		}
	}
}