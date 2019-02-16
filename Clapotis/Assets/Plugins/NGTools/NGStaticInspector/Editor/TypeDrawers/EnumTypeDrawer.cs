using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGStaticInspector
{
	public class EnumTypeDrawer : TypeDrawer
	{
		public	EnumTypeDrawer(string path, string label, Type type) : base(path, label, type)
		{
		}

		public override object	OnGUI(Rect r, object instance)
		{
			return EditorGUI.EnumPopup(r, this.label, (Enum)instance);
		}
	}
}