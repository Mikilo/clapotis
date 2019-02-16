using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	internal sealed class UnsupportedTypeDrawer : TypeHandlerDrawer
	{
		private static GUIContent	unsupportedLabel = new GUIContent("Unsupported type");

		public	UnsupportedTypeDrawer() : base(null)
		{
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			EditorGUI.BeginDisabledGroup(true);
			{
				Utility.content.text = data.Name;
				Utility.content.tooltip = data.Name;
				EditorGUI.LabelField(r, Utility.content, UnsupportedTypeDrawer.unsupportedLabel);
				Utility.content.tooltip = null;
			}
			EditorGUI.EndDisabledGroup();
		}
	}
}