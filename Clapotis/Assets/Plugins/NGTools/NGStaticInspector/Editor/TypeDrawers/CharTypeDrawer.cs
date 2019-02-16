using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGStaticInspector
{
	public class CharTypeDrawer : TypeDrawer
	{
		public	CharTypeDrawer(string path, string label, Type type) : base(path, label, type)
		{
		}

		public override object	OnGUI(Rect r, object instance)
		{
			EditorGUI.BeginChangeCheck();
			string	value = EditorGUI.TextField(r, this.label, ((Char)instance).ToString());
			if (EditorGUI.EndChangeCheck() == true)
			{
				if (string.IsNullOrEmpty(value) == true)
					return (Char)0;
				else
					return value[0];
			}
			return instance;
		}
	}
}