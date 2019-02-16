using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGStaticInspector
{
	public class UnsupportedTypeDrawer : TypeDrawer
	{
		private string	unsupported;

		public	UnsupportedTypeDrawer(string path, string label, Type type) : base(path, label, type)
		{
			this.unsupported = "Unsupported (" + this.type.Name + ")";
		}

		public override object	OnGUI(Rect r, object instance)
		{
			EditorGUI.LabelField(r, this.label, this.unsupported);
			return null;
		}
	}
}