using System;
using System.Reflection;

namespace NGToolsEditor.NGGameConsole
{
	public class EditorGUIProxy
	{
		public object	instance;
		private static Type	type = Type.GetType("UnityEditor.EditorGUI, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

		private static FieldInfo	s_RecycledEditorField = EditorGUIProxy.type.GetField("s_RecycledEditor", BindingFlags.Static | BindingFlags.NonPublic);
		public static object	s_RecycledEditor
		{
			get { return (object)s_RecycledEditorField.GetValue(null); }
			set { s_RecycledEditorField.SetValue(null, value); }
		}
	}
}