using System;

namespace NGToolsEditor
{
	/// <summary>
	/// Must have this signature:
	/// private static void	MethodName(ScriptableObject asset)
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public class NGSettingsChangedAttribute : Attribute
	{
	}
}