using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGStaticInspector
{
	public class Vector2TypeDrawer : TypeDrawer
	{
		public	Vector2TypeDrawer(string path, string label, Type type) : base(path, label, type)
		{
		}

		public override float	GetHeight(object instance)
		{
			return 16F * 2F;
		}

		public override object	OnGUI(Rect r, object instance)
		{
			return EditorGUI.Vector2Field(r, this.label, (Vector2)instance);
		}
	}
}