using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGStaticInspector
{
	public class BoundsTypeDrawer : TypeDrawer
	{
		public	BoundsTypeDrawer(string path, string label, Type type) : base(path, label, type)
		{
		}

		public override float	GetHeight(object instance)
		{
			return 16F * 3F;
		}

		public override object	OnGUI(Rect r, object instance)
		{
			return EditorGUI.BoundsField(r, this.label, (Bounds)instance);
		}
	}
}