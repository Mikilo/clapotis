using System;
using UnityEditor;

namespace NGToolsEditor.NGStaticInspector
{
	using UnityEngine;

	public class ObjectTypeDrawer : TypeDrawer
	{
		public	ObjectTypeDrawer(string path, string label, Type type) : base(path, label, type)
		{
		}

		public override object	OnGUI(Rect r, object instance)
		{
			try
			{
				return EditorGUI.ObjectField(r, this.label, (Object)instance, this.type, true);
			}
			catch (ExitGUIException)
			{
			}

			return instance;
		}
	}
}