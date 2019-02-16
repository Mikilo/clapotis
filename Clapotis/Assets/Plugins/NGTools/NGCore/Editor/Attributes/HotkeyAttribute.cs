using System;

namespace NGToolsEditor
{
	[AttributeUsage(AttributeTargets.Method)]
	public class HotkeyAttribute : Attribute
	{
		public string	label;

		public	HotkeyAttribute(string label)
		{
			this.label = label;
		}
	}
}