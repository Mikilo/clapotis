using System;
using System.Collections.Generic;

namespace NGTools.NGRemoteScene
{
	using UnityEngine;

	public sealed class MeshImporter : IObjectImporter
	{
		bool	IObjectImporter.CanHandle(Type type)
		{
			return type == typeof(Mesh);
		}

		//struct MeshVertex
		//{
		//	float posX;
		//	float posY;
		//	float posZ;
		//	float normalX;
		//	float normalY;
		//	float normalZ;
		//	byte colorR;
		//	byte colorG;
		//	byte colorB;
		//	byte colorA;
		//	float uvX;
		//	float uvY;
		//	float uv2X;
		//	float uv2Y;
		//	float uv3X;
		//	float uv3Y;
		//	float uv4X;
		//	float uv4Y;
		//	float tangentX;
		//	float tangentY;
		//	float tangentZ;
		//	float tangentW;
		//};

		byte[]	IObjectImporter.ToBinary(Object asset)
		{
			Mesh		mesh = asset as Mesh;
			ByteBuffer	buffer = Utility.GetBBuffer();
			List<int>	intList = new List<int>(256);

			if (mesh.isReadable == false)
			{
				//Debug.Log("vertexBufferCount=" + mesh.vertexBufferCount);
				//Debug.Log("vertexCount=" + mesh.vertexCount);
				//var vertexPtr = mesh.GetNativeVertexBufferPtr(0);
				//var indexPtr = mesh.GetNativeIndexBufferPtr();

				////var mv = new MeshVertex();
				////Marshal.Copy(vertexPtr, mv, 0, sizeof(MeshVertex));

				////ID3D11Buffer

				//byte[] managedArray = new byte[36];
				//Marshal.Copy(vertexPtr, managedArray, 0, managedArray.Length);
				//var p1 = new Vector3(BitConverter.ToSingle(managedArray, 0), BitConverter.ToSingle(managedArray, 4), BitConverter.ToSingle(managedArray, 8));
				//var p2 = new Vector3(BitConverter.ToSingle(managedArray, 12), BitConverter.ToSingle(managedArray, 16), BitConverter.ToSingle(managedArray, 20));
				//var p3 = new Vector3(BitConverter.ToSingle(managedArray, 24), BitConverter.ToSingle(managedArray, 28), BitConverter.ToSingle(managedArray, 32));
				//Debug.Log(p1);
				//Debug.Log(p2);
				//Debug.Log(p3);

				//float[] managedFArray = new float[mesh.vertexCount * 3];
				//Marshal.Copy(vertexPtr, managedFArray, 0, managedFArray.Length);

				//for (int i = 0; i < mesh.vertexCount; i += 3)
				//{
				//	var p21 = new Vector3(managedFArray[i], managedFArray[i + 1], managedFArray[i + 2]);
				//	Debug.Log(p21);
				//}

				//List<Vector3> vert = new List<Vector3>();
				//mesh.GetVertices(vert);
				// Just abort, so the server sends a failure notification.
				throw new NonReadableAssetException(mesh);
			}

			Vector3[]	v3Array = mesh.vertices;
			buffer.Append(v3Array.Length);
			for (int i = 0; i < v3Array.Length; i++)
			{
				buffer.Append(v3Array[i].x);
				buffer.Append(v3Array[i].y);
				buffer.Append(v3Array[i].z);
			}

			buffer.Append(mesh.subMeshCount);
			for (int i = 0; i < mesh.subMeshCount; i++)
			{
				buffer.Append((int)mesh.GetTopology(i));

				mesh.GetIndices(intList, i);
				//RemoteUtility.ObjectToBuffer(intList, buffer); // TODO: Implement DirectObjectToBuffer and not fields! (3rd argument a Type?)
				//buffer.Append(intList.ToArray());// TODO TEMPO: handle list
				buffer.Append(intList.Count);
				for (int j = 0; j < intList.Count; j++)
					buffer.Append(intList[j]);

				mesh.GetTriangles(intList, i);
				buffer.Append(intList.Count);
				for (int j = 0; j < intList.Count; j++)
					buffer.Append(intList[j]);

				//mesh.GetUVs(, i);
			}

			//RemoteUtility.ObjectToBuffer(mesh.bindposes, buffer); // TODO: Implement DirectObjectToBuffer and not fields!
			Matrix4x4[]	bindposes = mesh.bindposes;
			buffer.Append(bindposes.Length);
			for (int i = 0; i < bindposes.Length; i++)
			{
				Matrix4x4	matrix = bindposes[i];

				buffer.Append(matrix.m00);
				buffer.Append(matrix.m01);
				buffer.Append(matrix.m02);
				buffer.Append(matrix.m03);
				buffer.Append(matrix.m10);
				buffer.Append(matrix.m11);
				buffer.Append(matrix.m12);
				buffer.Append(matrix.m13);
				buffer.Append(matrix.m20);
				buffer.Append(matrix.m21);
				buffer.Append(matrix.m22);
				buffer.Append(matrix.m23);
				buffer.Append(matrix.m30);
				buffer.Append(matrix.m31);
				buffer.Append(matrix.m32);
				buffer.Append(matrix.m33);
			}

			////buffer.Append(mesh.blendShapeCount);

			BoneWeight[]	boneWeights = mesh.boneWeights;
			buffer.Append(boneWeights.Length);
			for (int i = 0; i < boneWeights.Length; i++)
			{
				BoneWeight	boneWeight = boneWeights[i];

				buffer.Append(boneWeight.weight0);
				buffer.Append(boneWeight.weight1);
				buffer.Append(boneWeight.weight2);
				buffer.Append(boneWeight.weight3);
				buffer.Append(boneWeight.boneIndex0);
				buffer.Append(boneWeight.boneIndex1);
				buffer.Append(boneWeight.boneIndex2);
				buffer.Append(boneWeight.boneIndex3);
			}

			buffer.Append(mesh.bounds.center.x);
			buffer.Append(mesh.bounds.center.y);
			buffer.Append(mesh.bounds.center.z);
			buffer.Append(mesh.bounds.size.x);
			buffer.Append(mesh.bounds.size.y);
			buffer.Append(mesh.bounds.size.z);

			Color32[]	colors = mesh.colors32;
			buffer.Append(colors.Length);
			for (int i = 0; i < colors.Length; i++)
			{
				buffer.Append(colors[i].r);
				buffer.Append(colors[i].g);
				buffer.Append(colors[i].b);
				buffer.Append(colors[i].a);
			}

			buffer.AppendUnicodeString(mesh.name);

			v3Array = mesh.normals;
			buffer.Append(v3Array.Length);
			for (int i = 0; i < v3Array.Length; i++)
			{
				buffer.Append(v3Array[i].x);
				buffer.Append(v3Array[i].y);
				buffer.Append(v3Array[i].z);
			}

			Vector4[]	v4Array = mesh.tangents;
			buffer.Append(v4Array.Length);
			for (int i = 0; i < v4Array.Length; i++)
			{
				buffer.Append(v4Array[i].x);
				buffer.Append(v4Array[i].y);
				buffer.Append(v4Array[i].z);
				buffer.Append(v4Array[i].w);
			}

			//buffer.Append(mesh.triangles.Length);
			//for (int i = 0; i < mesh.triangles.Length; i++)
			//	buffer.Append(mesh.triangles[i]);

			Vector2[]	v2Array = mesh.uv;
			buffer.Append(v2Array.Length);
			for (int i = 0; i < v2Array.Length; i++)
			{
				buffer.Append(v2Array[i].x);
				buffer.Append(v2Array[i].y);
			}

			v2Array = mesh.uv2;
			buffer.Append(v2Array.Length);
			for (int i = 0; i < v2Array.Length; i++)
			{
				buffer.Append(v2Array[i].x);
				buffer.Append(v2Array[i].y);
			}

			v2Array = mesh.uv3;
			buffer.Append(v2Array.Length);
			for (int i = 0; i < v2Array.Length; i++)
			{
				buffer.Append(v2Array[i].x);
				buffer.Append(v2Array[i].y);
			}

			v2Array = mesh.uv4;
			buffer.Append(v2Array.Length);
			for (int i = 0; i < v2Array.Length; i++)
			{
				buffer.Append(v2Array[i].x);
				buffer.Append(v2Array[i].y);
			}

			return Utility.ReturnBBuffer(buffer);
		}

		ImportAssetResult	IObjectImporter.ToAsset(byte[] data, string path, out Object asset)
		{
			ByteBuffer	buffer = Utility.GetBBuffer(data);

			try
			{
				asset = new Mesh();
				//asset.hideFlags = HideFlags.HideAndDontSave;

				Mesh		mesh = asset as Mesh;
				Vector3[]	vertices = new Vector3[buffer.ReadInt32()];

				for (int i = 0; i < vertices.Length; i++)
					vertices[i] = new Vector3(buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle());

				mesh.vertices = vertices;

				mesh.subMeshCount = buffer.ReadInt32();

				for (int i = 0; i < mesh.subMeshCount; i++)
				{
					MeshTopology	meshTopology = (MeshTopology)buffer.ReadInt32();
					int[]			array = new int[buffer.ReadInt32()];

					for (int j = 0; j < array.Length; j++)
						array[j] = buffer.ReadInt32();

					mesh.SetIndices(array, meshTopology, i);
					//array = RemoteUtility.BufferToObject(buffer); // TODO: Implement DirectObjectToBuffer and not fields!
					//TODO Implement buffer.ReadArray?
					//asset.SetIndices(array, meshTopology, i);

					array = new int[buffer.ReadInt32()];
					for (int j = 0; j < array.Length; j++)
						array[j] = buffer.ReadInt32();

					mesh.SetTriangles(array, i);
				}

				Matrix4x4[]	matrixes = new Matrix4x4[buffer.ReadInt32()];

				for (int i = 0; i < mesh.bindposes.Length; i++)
				{
					matrixes[i] = new Matrix4x4()
					{
						m00 = buffer.ReadSingle(),
						m01 = buffer.ReadSingle(),
						m02 = buffer.ReadSingle(),
						m03 = buffer.ReadSingle(),
						m10 = buffer.ReadSingle(),
						m11 = buffer.ReadSingle(),
						m12 = buffer.ReadSingle(),
						m13 = buffer.ReadSingle(),
						m20 = buffer.ReadSingle(),
						m21 = buffer.ReadSingle(),
						m22 = buffer.ReadSingle(),
						m23 = buffer.ReadSingle(),
						m30 = buffer.ReadSingle(),
						m31 = buffer.ReadSingle(),
						m32 = buffer.ReadSingle(),
						m33 = buffer.ReadSingle()
					};
				}

				mesh.bindposes = matrixes;

				//mesh.blendShapeCount

				BoneWeight[]	boneWeights = new BoneWeight[buffer.ReadInt32()];

				for (int i = 0; i < mesh.boneWeights.Length; i++)
				{
					boneWeights[i] = new BoneWeight()
					{
						weight0 = buffer.ReadSingle(),
						weight1 = buffer.ReadSingle(),
						weight2 = buffer.ReadSingle(),
						weight3 = buffer.ReadSingle(),
						boneIndex0 = buffer.ReadInt32(),
						boneIndex1 = buffer.ReadInt32(),
						boneIndex2 = buffer.ReadInt32(),
						boneIndex3 = buffer.ReadInt32()
					};
				}

				mesh.boneWeights = boneWeights;

				mesh.bounds = new Bounds(new Vector3(buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle()), new Vector3(buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle()));

				Color32[]	colors = new Color32[buffer.ReadInt32()];

				for (int i = 0; i < colors.Length; i++)
					colors[i] = new Color32(buffer.ReadByte(), buffer.ReadByte(), buffer.ReadByte(), buffer.ReadByte());

				mesh.colors32 = colors;

				mesh.name = buffer.ReadUnicodeString();

				Vector3[]	normals = new Vector3[buffer.ReadInt32()];

				for (int i = 0; i < normals.Length; i++)
					normals[i] = new Vector3(buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle());

				mesh.normals = normals;

				Vector4[]	tangents = new Vector4[buffer.ReadInt32()];

				for (int i = 0; i < tangents.Length; i++)
					tangents[i] = new Vector4(buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle(), buffer.ReadSingle());

				mesh.tangents = tangents;

				//int[]	triangles = new int[buffer.ReadInt32()];

				//for (int i = 0; i < triangles.Length; i++)
				//	triangles[i] = buffer.ReadInt32();

				//mesh.triangles = triangles;

				Vector2[]	uv = new Vector2[buffer.ReadInt32()];

				for (int i = 0; i < uv.Length; i++)
					uv[i] = new Vector2(buffer.ReadSingle(), buffer.ReadSingle());

				mesh.uv = uv;

				Vector2[]	uv2 = new Vector2[buffer.ReadInt32()];

				for (int i = 0; i < uv2.Length; i++)
					uv2[i] = new Vector2(buffer.ReadSingle(), buffer.ReadSingle());

				mesh.uv2 = uv2;

				Vector2[]	uv3 = new Vector2[buffer.ReadInt32()];

				for (int i = 0; i < uv3.Length; i++)
					uv3[i] = new Vector2(buffer.ReadSingle(), buffer.ReadSingle());

				mesh.uv3 = uv3;

				Vector2[]	uv4 = new Vector2[buffer.ReadInt32()];

				for (int i = 0; i < uv4.Length; i++)
					uv4[i] = new Vector2(buffer.ReadSingle(), buffer.ReadSingle());

				mesh.uv4 = uv4;

				//mesh.RecalculateBounds();?
				//mesh.RecalculateNormals();?
				//mesh.RecalculateTangents();?

				return ImportAssetResult.NeedCreateViaAssetDatabase;
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
			return ".asset";
		}
	}
}