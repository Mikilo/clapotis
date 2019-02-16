using System;

namespace NGToolsEditor
{
	/// <summary>
	/// Prewarms an EditorWindow type, to prevent RepaintEditorWindow() from using Resources.FindObjectsOfTypeAll().
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public class PrewarmEditorWindowAttribute : Attribute
	{
	}
}