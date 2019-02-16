using System;

namespace NGToolsEditor
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public class NGSettingsAttribute : Attribute
	{
		public string	label;
		public int		priority;

		public	NGSettingsAttribute(string label, int priority = -1)
		{
			this.label = label;
			this.priority = priority;
		}
	}
}