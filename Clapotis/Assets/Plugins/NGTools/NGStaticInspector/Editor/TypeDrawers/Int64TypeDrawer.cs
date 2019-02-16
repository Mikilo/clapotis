using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGStaticInspector
{
	public class Int64TypeDrawer : TypeDrawer
	{
		public	Int64TypeDrawer(string path, string label, Type type) : base(path, label, type)
		{
		}

		public override object	OnGUI(Rect r, object instance)
		{
			return EditorGUI.LongField(r, this.label, (Int64)instance);
		}
	}
}