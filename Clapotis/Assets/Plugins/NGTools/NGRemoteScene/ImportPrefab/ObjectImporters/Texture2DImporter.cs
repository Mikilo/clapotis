using System;
using System.IO;

namespace NGTools.NGRemoteScene
{
	using UnityEngine;

	public sealed class Texture2DImporter : IObjectImporter
	{
		bool	IObjectImporter.CanHandle(Type type)
		{
			return type == typeof(Texture2D);
		}

		byte[]	IObjectImporter.ToBinary(Object asset)
		{
			Texture2D	texture = asset as Texture2D;
			ByteBuffer	buffer = Utility.GetBBuffer();

			//buffer.Append(texture.alphaIsTransparency);
			buffer.Append(texture.anisoLevel);
			buffer.Append((int)texture.filterMode);
			buffer.Append((int)texture.format);
			buffer.Append(texture.width);
			buffer.Append(texture.height);
			buffer.Append(texture.mipMapBias);
			//buffer.Append(texture.mipmapCount);
			buffer.AppendUnicodeString(texture.name);
			//buffer.Append(texture.texelSize.x);
			//buffer.Append(texture.texelSize.y);
			buffer.Append((int)texture.wrapMode);
			byte[]	data = texture.EncodeToPNG();

			if (data == null)
				buffer.Append(-1);
			else
			{
				buffer.Append(data.Length);
				buffer.Append(data);
			}

			return Utility.ReturnBBuffer(buffer);
		}

		ImportAssetResult	IObjectImporter.ToAsset(byte[] data, string path, out Object asset)
		{
			ByteBuffer	buffer = Utility.GetBBuffer(data);

			try
			{
				//bool			alphaIsTransparency = buffer.ReadBoolean();
				int				anisoLevel = buffer.ReadInt32();
				FilterMode		filterMode = (FilterMode)buffer.ReadInt32();
				TextureFormat	format = (TextureFormat)buffer.ReadInt32();
				int				width = buffer.ReadInt32();
				int				height = buffer.ReadInt32();
				float			mipMapBias = buffer.ReadSingle();
				//int				mipMapCount = buffer.ReadInt32();
				string			name = buffer.ReadUnicodeString();
				//Vector2			texel = new Vector2(buffer.ReadSingle(), buffer.ReadSingle());
				TextureWrapMode	wrapMode = (TextureWrapMode)buffer.ReadInt32();
				int				length = buffer.ReadInt32();

				if (length != -1)
				{
					byte[]			data2 = buffer.ReadBytes(length);

					asset = new Texture2D(width, height, format, false);

					Texture2D	texture = asset as Texture2D;
					//texture.alphaIsTransparency = alphaIsTransparency;
					texture.anisoLevel = anisoLevel;
					texture.filterMode = filterMode;
					texture.mipMapBias = mipMapBias;
					//texture.mipmapCount = mimapCount;
					texture.name = name;
					//texture.texelSize = texel;
					texture.wrapMode = wrapMode;
					texture.LoadImage(data2);

					Debug.Log("Texture2D at " + path);
					if (File.Exists(path) == true)
						File.Delete(path);
					Directory.CreateDirectory(Path.GetDirectoryName(path));
					File.WriteAllBytes(path, texture.EncodeToPNG());

					return ImportAssetResult.SavedToDisk;
				}
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException(ex);
			}

			asset = null;

			return ImportAssetResult.ImportFailure;
		}

		string	IObjectImporter.GetExtension()
		{
			return ".png";
		}
	}
}