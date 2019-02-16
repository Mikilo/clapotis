using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGStaticInspector
{
	public class DoubleTypeDrawer : TypeDrawer
	{
		public	DoubleTypeDrawer(string path, string label, Type type) : base(path, label, type)
		{
		}

		public override object	OnGUI(Rect r, object instance)
		{
			return EditorGUI.DoubleField(r, this.label, (Double)instance);
		}
	}
}