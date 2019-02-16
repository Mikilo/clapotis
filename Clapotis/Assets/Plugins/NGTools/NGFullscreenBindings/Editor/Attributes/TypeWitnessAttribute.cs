using System;

namespace NGToolsEditor.NGFullscreenBindings
{
	public sealed class TypeWitnessAttribute : Attribute
	{
		public readonly string	type;

		public	TypeWitnessAttribute(string type)
		{
			this.type = type;
		}
	}
}