using System;

namespace NGToolsEditor.NGMissingScriptRecovery
{
	[Serializable]
	internal sealed class CachedLineFix
	{
		public string	brokenLine;
		public string	fixedLine;
		public Type		type;
	}
}