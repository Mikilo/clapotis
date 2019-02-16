using System;

namespace NGTools.NGRemoteScene
{
	using UnityEngine;

	/// <summary>
	/// Thrown when trying to import a non-readable asset.
	/// </summary>
	[Serializable]
	internal sealed class NonReadableAssetException : Exception
	{
		public readonly	Object	asset;

		public override string	Message
		{
			get
			{
				return "Asset \"" + this.asset.name + "\" of type \"" + this.asset.GetType().FullName + "\" is non-readable.";
			}
		}

		/// <summary>
		/// Thrown when trying to fetch a non-implemented TypeHandler.
		/// </summary>
		public	NonReadableAssetException(Object asset)
		{
			this.asset = asset;
		}

		public override string	ToString()
		{
			return this.Message;
		}
	}
}