using System;

namespace NGToolsEditor.NGMissingScriptRecovery
{
	internal struct PotentialType
	{
		public readonly Type		type;
		public readonly int			matchingFields;
		public readonly string[]	fields;

		public	PotentialType(Type type, int matchingFields, string[] fields)
		{
			this.type = type;
			this.matchingFields = matchingFields;
			this.fields = fields;
		}
	}
}