using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	[CustomPropertyDrawer(typeof(GeneralSettings.EditorExtensions))]
	internal sealed class EditorExtensionsDrawer : PropertyDrawer
	{
		private string	newExt = string.Empty;

		public override float	GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (property.propertyType == SerializedPropertyType.Generic)
				return base.GetPropertyHeight(property, label) * 3;
			else
				return base.GetPropertyHeight(property, label);
		}

		public override void	OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			SerializedProperty	editor = property.FindPropertyRelative("editor");
			SerializedProperty	arguments = property.FindPropertyRelative("arguments");
			SerializedProperty	extensions = property.FindPropertyRelative("extensions");

			position.height = Constants.SingleLineHeight;

			if (editor != null)
			{
				EditorGUI.BeginChangeCheck();
				EditorGUI.PropertyField(position, editor);
				if (EditorGUI.EndChangeCheck() == true && arguments != null)
				{
					if (string.IsNullOrEmpty(arguments.stringValue) == true)
					{
						foreach (IEditorOpener opener in NGConsoleWindow.Openers)
						{
							if (opener.CanHandleEditor(editor.stringValue) == true)
							{
								arguments.stringValue = opener.defaultArguments;
								break;
							}
						}
					}
				}
			}
			else
				EditorGUI.LabelField(position, "Editor", "Field \"editor\" is missing.");

			position.y += position.height;

			if (arguments != null)
				EditorGUI.PropertyField(position, arguments);
			else
				EditorGUI.LabelField(position, "Arguments", "Field \"arguments\" is missing.");

			position.y += position.height;

			if (extensions != null)
			{
				EditorGUI.LabelField(position, "Extensions");

				position.x = EditorGUIUtility.labelWidth + GUI.depth * 16F;
				for (int i = 0; i < extensions.arraySize; i++)
				{
					SerializedProperty	ext = extensions.GetArrayElementAtIndex(i);

					Utility.content.text = ext.stringValue;
					position.width = GUI.skin.button.CalcSize(Utility.content).x;
					if (GUI.Button(position, Utility.content) == true)
					{
						extensions.DeleteArrayElementAtIndex(i);
						break;
					}
					position.x += position.width;
				}

				Utility.content.text = this.newExt;
				position.width = GUI.skin.button.CalcSize(Utility.content).x + 10F;
				this.newExt = GUI.TextField(position, this.newExt);
				position.x += position.width;

				EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(this.newExt));
				{
					Utility.content.text = "+";
					position.width = GUI.skin.button.CalcSize(Utility.content).x;
					if (GUI.Button(position, Utility.content) == true)
					{
						++extensions.arraySize;
						extensions.GetArrayElementAtIndex(extensions.arraySize - 1).stringValue = this.newExt;
						this.newExt = string.Empty;
					}
				}
			}
			else
				EditorGUI.LabelField(position, "Extensions", "Field \"extensions\" is missing.");
		}
	}
}