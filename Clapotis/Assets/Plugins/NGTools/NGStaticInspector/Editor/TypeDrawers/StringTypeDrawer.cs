using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGStaticInspector
{
	public class StringTypeDrawer : TypeDrawer
	{
		public	StringTypeDrawer(string path, string label, Type type) : base(path, label, type)
		{
		}

		public override object	OnGUI(Rect r, object instance)
		{
			return EditorGUI.TextField(r, this.label, (String)instance);
		}
	}
}