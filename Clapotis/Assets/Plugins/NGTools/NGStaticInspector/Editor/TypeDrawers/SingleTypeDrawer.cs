using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGStaticInspector
{
	public class SingleTypeDrawer : TypeDrawer
	{
		public	SingleTypeDrawer(string path, string label, Type type) : base(path, label, type)
		{
		}

		public override object	OnGUI(Rect r, object instance)
		{
			return EditorGUI.FloatField(r, this.label, (Single)instance);
		}
	}
}