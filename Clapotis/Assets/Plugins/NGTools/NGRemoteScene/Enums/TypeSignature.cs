using System;

namespace NGTools.NGRemoteScene
{
	[Flags]
	public enum TypeSignature : byte
	{
		Null = 0,
		/// <summary>Includes primitives, decimal and string.</summary>
		Primitive = 1,
		Enum = 2,
		UnityObject = 4,
		Class = 8,
		Array = 16,
	}
}