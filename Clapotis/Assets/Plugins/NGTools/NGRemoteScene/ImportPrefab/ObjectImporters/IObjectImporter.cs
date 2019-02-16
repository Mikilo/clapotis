using System;

namespace NGTools.NGRemoteScene
{
	using UnityEngine;

	public enum ImportAssetResult
	{
		ImportFailure = -1,
		SavedToDisk,
		NeedCreateViaAssetDatabase
	}

	public interface IObjectImporter
	{
		bool				CanHandle(Type type);
		byte[]				ToBinary(Object asset);
		ImportAssetResult	ToAsset(byte[] data, string path, out Object asset);
		string				GetExtension();
	}
}