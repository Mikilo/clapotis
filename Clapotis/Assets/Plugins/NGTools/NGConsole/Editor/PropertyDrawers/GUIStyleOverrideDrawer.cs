using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	[CustomPropertyDrawer(typeof(GUIStyleOverride))]
	internal sealed class GUIStyleOverrideDrawer : PropertyDrawer
	{
		public const float	StyleNotFoundHeight = 24F;
		public const float	ToggleWidth = 20F;

		private string[]	fields;
		private bool		invalidate;
		private bool?		styleFound;
		private FieldInfo	styleField;

		public override float	GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			this.Init();

			float	height = EditorGUI.GetPropertyHeight(property, label, false);

			if (property.isExpanded == true)
			{
				if (this.styleFound == false)
					height += GUIStyleOverrideDrawer.StyleNotFoundHeight;

				height += 16F + 16F; // BaseStyleName + ContentOffset's half height. Vector2 seems to return 16 as height...

				for (int i = 0; i < this.fields.Length; ++i)
					height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(fields[i]), GUIContent.none, true);
			}

			return height;
		}

		public override void	OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			position.height = Constants.SingleLineHeight;
			if (EditorGUI.PropertyField(position, property, label, false) == true)
			{
				SerializedProperty	overrideMask = property.FindPropertyRelative("overrideMask");
				SerializedProperty	baseStyleName = property.FindPropertyRelative("baseStyleName");

				if (this.styleFound.HasValue == false)
					this.styleFound = string.IsNullOrEmpty(baseStyleName.stringValue) == true || GUI.skin.FindStyle(baseStyleName.stringValue) != null;

				this.invalidate = false;

				position.y += position.height;
				position.xMin += GUIStyleOverrideDrawer.ToggleWidth;
				EditorGUI.BeginChangeCheck();
				EditorGUI.PropertyField(position, baseStyleName);
				if (EditorGUI.EndChangeCheck() == true)
				{
					this.invalidate = true;
					this.styleFound = string.IsNullOrEmpty(baseStyleName.stringValue) == true || GUI.skin.FindStyle(baseStyleName.stringValue) != null;
				}
				position.y += position.height;

				if (this.styleFound == false)
				{
					position.height = GUIStyleOverrideDrawer.StyleNotFoundHeight;
					EditorGUI.HelpBox(position, "Style not found.", MessageType.Warning);
					position.y += GUIStyleOverrideDrawer.StyleNotFoundHeight;
				}

				position.xMin -= GUIStyleOverrideDrawer.ToggleWidth;

				for (int i = 0; i < this.fields.Length; ++i)
					position = this.DrawOverride(position, i, overrideMask, property.FindPropertyRelative(this.fields[i]));

				TooltipHelper.PostOnGUI();

				if (this.invalidate == true)
				{
					if (this.styleField == null)
						this.styleField = this.fieldInfo.FieldType.GetField("style", BindingFlags.NonPublic | BindingFlags.Instance);
					this.styleField.SetValue(this.fieldInfo.GetValue(property.serializedObject.targetObject), null);
					InternalEditorUtility.RepaintAllViews();
				}
			}
		}

		private void	Init()
		{
			if (this.fields == null)
			{
				List<string>	activeFields = new List<string>(16);
				FieldInfo[]		fields = this.fieldInfo.FieldType.GetFields();

				for (int i = 0; i < fields.Length; i++)
				{
					if (fields[i].Name != "overrideMask" &&
						fields[i].Name != "baseStyleName")
					{
						activeFields.Add(fields[i].Name);
					}
				}

				this.fields = activeFields.ToArray();
			}
		}

		private Rect	DrawOverride(Rect position, int i, SerializedProperty boolean, SerializedProperty value)
		{
			if (boolean != null && value != null)
			{
				position.height = Constants.SingleLineHeight;
				Rect	rBool = position;
				rBool.width = GUIStyleOverrideDrawer.ToggleWidth;

				EditorGUI.BeginChangeCheck();
				EditorGUI.Toggle(rBool, (boolean.intValue & (2 << i)) != 0);
				if (EditorGUI.EndChangeCheck() == true)
				{
					boolean.intValue = boolean.intValue ^ (2 << i);
					this.invalidate = true;
				}

				position.xMin += GUIStyleOverrideDrawer.ToggleWidth;

				position.height = EditorGUI.GetPropertyHeight(value, GUIContent.none, true);

				EditorGUI.BeginDisabledGroup((boolean.intValue & (2 << i)) == 0);
				EditorGUI.BeginChangeCheck();
				bool	isExpanded = value.isExpanded;
				EditorGUI.PropertyField(position, value, true);
				if (EditorGUI.EndChangeCheck() == true)
				{
					if (isExpanded == value.isExpanded)
						this.invalidate = true;
				}
				EditorGUI.EndDisabledGroup();

				position.xMin -= GUIStyleOverrideDrawer.ToggleWidth;
				position.y += position.height;
			}

			return position;
		}
	}
}