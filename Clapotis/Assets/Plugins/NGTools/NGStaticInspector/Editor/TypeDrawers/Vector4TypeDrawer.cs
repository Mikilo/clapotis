using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGStaticInspector
{
	public class Vector4TypeDrawer : TypeDrawer
	{
		public	Vector4TypeDrawer(string path, string label, Type type) : base(path, label, type)
		{
		}

		public override float	GetHeight(object instance)
		{
			return 16F * 3F;
		}

		public override object	OnGUI(Rect r, object instance)
		{
			return EditorGUI.Vector4Field(r, this.label, (Vector4)instance);
		}
	}
}