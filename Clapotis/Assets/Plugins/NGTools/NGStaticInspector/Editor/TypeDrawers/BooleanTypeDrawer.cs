using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGStaticInspector
{
	public class BooleanTypeDrawer : TypeDrawer
	{
		public	BooleanTypeDrawer(string path, string label, Type type) : base(path, label, type)
		{
		}

		public override object	OnGUI(Rect r, object instance)
		{
			return EditorGUI.Toggle(r, this.label, (Boolean)instance);
		}
	}
}