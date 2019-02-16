using System;
using UnityEngine;

namespace NGToolsEditor.NGStaticInspector
{
	public abstract class TypeDrawer
	{
		public readonly string	path;
		public readonly string	label;
		public readonly Type	type;

		public	TypeDrawer(string path, string label, Type type)
		{
			this.path = path;
			this.label = Utility.NicifyVariableName(label);
			this.type = type;
		}

		public virtual float	GetHeight(object instance)
		{
			return 16F;
		}

		public abstract object	OnGUI(Rect r, object instance);
	}
}