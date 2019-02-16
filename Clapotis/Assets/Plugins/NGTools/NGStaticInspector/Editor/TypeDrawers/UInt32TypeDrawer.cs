using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGStaticInspector
{
	public class UInt32TypeDrawer : TypeDrawer
	{
		public	UInt32TypeDrawer(string path, string label, Type type) : base(path, label, type)
		{
		}

		public override object	OnGUI(Rect r, object instance)
		{
			return EditorGUI.LongField(r, this.label, (UInt32)instance);
		}
	}
}