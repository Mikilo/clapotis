using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGStaticInspector
{
	public class AnimationCurveTypeDrawer : TypeDrawer
	{
		public	AnimationCurveTypeDrawer(string path, string label, Type type) : base(path, label, type)
		{
		}

		public override object	OnGUI(Rect r, object instance)
		{
			try
			{
				if (instance == null)
					instance = new AnimationCurve();
				return EditorGUI.CurveField(r, this.label, (AnimationCurve)instance);
			}
			catch (ExitGUIException)
			{
			}

			return instance;
		}
	}
}