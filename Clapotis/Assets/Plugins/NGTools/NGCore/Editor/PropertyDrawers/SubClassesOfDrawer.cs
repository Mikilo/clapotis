using NGTools;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor
{
	[CustomPropertyDrawer(typeof(SubClassesOfAttribute))]
	public class SubClassesOfDrawer : PropertyDrawer
	{
		private List<Type>	subClasses = new List<Type>();
		private string[]	subClassesLabels;
		private int			selected;

		public override void	OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (this.subClassesLabels == null)
			{
				foreach (Type t in Utility.EachAllAssignableFrom((this.attribute as SubClassesOfAttribute).type))
					this.subClasses.Add(t);

				this.subClassesLabels = new string[this.subClasses.Count];
				for (int i = 0; i < this.subClasses.Count; i++)
				{
					this.subClassesLabels[i] = this.subClasses[i].FullName;

					if (this.subClassesLabels[i] == property.stringValue)
						this.selected = i;
				}
			}

			EditorGUI.BeginChangeCheck();
			this.selected = EditorGUI.Popup(position, label.text, this.selected, this.subClassesLabels);
			if (EditorGUI.EndChangeCheck() == true)
				property.stringValue = this.subClasses[this.selected].FullName;
		}
	}
}