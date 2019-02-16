using NGTools.NGRemoteScene;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[CustomPropertyDrawer(typeof(ListingAssets))]
	internal sealed class ListingAssetsDrawer : PropertyDrawer
	{
		private SerializedProperty	assets;

		public override float	GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return Constants.SingleLineHeight;
		}

		public override void	OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (this.assets == null)
				this.assets = property.FindPropertyRelative("assets");
			if (this.assets == null)
			{
				EditorGUI.HelpBox(position, "Field \"assets\" was not found in " + this.fieldInfo.Name + ".", MessageType.Error);
				return;
			}

			position.xMin += 60F;
			EditorGUI.LabelField(position, "NG Remote Project Assets (" + this.assets.arraySize + ")");
			position.xMin -= 60F;

			position.width = 60F;
			if (GUI.Button(position, "Edit") == true)
			{
				Utility.OpenWindow<EmbedAssetsBrowserWindow>(true, EmbedAssetsBrowserWindow.Title, true, null, w => {
					w.serializedObject = property.serializedObject;
					w.origin = (ListingAssets)this.fieldInfo.GetValue(property.serializedObject.targetObject);
				});
			}
		}
	}
}