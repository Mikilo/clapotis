using System;
using System.Reflection;

namespace NGToolsEditor.NGGameConsole
{
	public class RecycledTextEditorProxy
	{
		public object	instance;
		private static Type	type = Type.GetType("UnityEditor.EditorGUI+RecycledTextEditor, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

		private static PropertyInfo	textProperty = RecycledTextEditorProxy.type.GetProperty("text", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		public String	text
		{
			get { return (String)textProperty.GetValue(this.instance, null); }
			set { textProperty.SetValue(this.instance, value, null); }
		}

		private static MethodInfo	MoveTextEndMethod = RecycledTextEditorProxy.type.GetMethod("MoveTextEnd", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		public void	MoveTextEnd()
		{
			RecycledTextEditorProxy.MoveTextEndMethod.Invoke(this.instance, new object[] { });
		}
	}
}