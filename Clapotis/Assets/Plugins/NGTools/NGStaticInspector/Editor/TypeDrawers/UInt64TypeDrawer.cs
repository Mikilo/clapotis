using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGStaticInspector
{
	public class UInt64TypeDrawer : TypeDrawer
	{
		public	UInt64TypeDrawer(string path, string label, Type type) : base(path, label, type)
		{
		}

		public override object	OnGUI(Rect r, object instance)
		{
			return EditorGUI.LongField(r, this.label, (long)(UInt64)instance);
		}
	}
}