using System;

namespace NGToolsEditor.NGRemoteScene
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class TabModuleForTypeAttribute : Attribute
	{
		public readonly Type	type;

		public	TabModuleForTypeAttribute(Type type)
		{
			this.type = type;
		}
	}
}