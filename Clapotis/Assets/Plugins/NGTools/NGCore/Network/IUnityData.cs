using System;

namespace NGTools.Network
{
	using UnityEngine;

	public enum ImportAssetState
	{
		DoesNotExist,
		Waiting,
		Requesting,
		Ready,
	}

	/// <summary>Provides resources and tools to TypeHandlerDrawer.</summary>
	public interface IUnityData
	{
		Client		Client { get; }
		string[]	Layers { get; }
		void		GetResources(Type type, out string[] resourceNames, out int[] resourceInstanceIds);
		string		GetGameObjectName(int instanceID);
		string		GetBehaviourName(int gameObjectInstanceID, int instanceID);
		/// <summary>From a splitted field path, converts IDs & indexes to their real name.</summary>
		/// <param name="pathComponents">Splitted field path.</param>
		/// <param name="nicifyPath">Set to true to nicify fields' name.</param>
		void		FetchReadablePaths(string[] pathComponents, bool nicifyPath);
		string		GetResourceName(Type type, int instanceID);
		string		GetTypeName(int typeIndex);
		ImportAssetState	GetAssetFromImportParameters(int instanceID, out Object asset, bool autoGenerateImportParameters);
		bool		AddPacket(Packet packet, Action<ResponsePacket> onComplete = null);
		void		RecordChange(string path, Type type, object value, object newValue);
		void		ImportAsset(string path, Type type, int instanceID, int gameObjectInstanceID = 0, int componentInstanceID = 0, Object asset = null);
		void		UpdateFieldValue(string valuePath, byte[] rawValue);
	}
}