using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGStaticInspector
{
	public class RectTypeDrawer : TypeDrawer
	{
		public	RectTypeDrawer(string path, string label, Type type) : base(path, label, type)
		{
		}

		public override float	GetHeight(object instance)
		{
			return 16F * 3F;
		}

		public override object	OnGUI(Rect r, object instance)
		{
			return EditorGUI.RectField(r, this.label, (Rect)instance);
		}
	}
}