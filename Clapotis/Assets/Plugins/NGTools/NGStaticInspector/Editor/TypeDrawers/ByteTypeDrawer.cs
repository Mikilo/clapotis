using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGStaticInspector
{
	public class ByteTypeDrawer : TypeDrawer
	{
		public	ByteTypeDrawer(string path, string label, Type type) : base(path, label, type)
		{
		}

		public override object	OnGUI(Rect r, object instance)
		{
			return EditorGUI.IntField(r, this.label, (Byte)instance);
		}
	}
}