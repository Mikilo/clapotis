using NGTools;
using System;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NGToolsEditor
{
	public sealed class SectionDrawer
	{
		public readonly string	sectionName;
		public readonly Type	typeSetting;

		private SerializedObject	so;
		private FieldInfo			fieldInfo;

		/// <summary>
		/// Initializes a Section.
		/// </summary>
		/// <param name="typeSetting">The Type of your class inside NGSettings.</param>
		/// <param name="priority">The lower the nearest to the top.</param>
		public	SectionDrawer(Type typeSetting, int priority = -1) : this(null, typeSetting, priority)
		{
		}

		/// <summary>
		/// Initializes a Section and adds it into NGSettings.
		/// </summary>
		/// <param name="sectionName">Defines the name of the section.</param>
		/// <param name="typeSetting">The Type of your class inside NGSettings.</param>
		/// <param name="priority">The lower the nearest to the top.</param>
		public	SectionDrawer(string sectionName, Type typeSetting, int priority = -1)
		{
			this.sectionName = sectionName;
			this.typeSetting = typeSetting;

			if (this.typeSetting.IsSubclassOf(typeof(ScriptableObject)) == false)
			{
				FieldInfo[]	fields = typeof(NGSettings).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

				for (int i = 0; i < fields.Length; i++)
				{
					if (fields[i].FieldType == typeSetting)
					{
						this.fieldInfo = fields[i];
						break;
					}
				}

				InternalNGDebug.Assert(this.fieldInfo != null, "Field of type \"" + typeSetting + "\" does not exist in class \"" + typeof(NGSettings) + "\".");
			}

			if (this.sectionName != null)
				NGSettingsWindow.AddSection(this.sectionName, this.OnGUI, priority);
		}

		public void	Uninit()
		{
			if (this.sectionName != null)
				NGSettingsWindow.RemoveSection(this.sectionName);
		}

		public void	OnGUI()
		{
			if (HQ.Settings == null)
			{
				this.so = null;
				GUILayout.Label(LC.G("ConsoleSettings_NullTarget"));
				return;
			}

			try
			{
				if (this.so == null || this.so.targetObject == null || this.so.targetObject != HQ.Settings.Get(this.typeSetting) as ScriptableObject)
					this.so = new SerializedObject(HQ.Settings.Get(this.typeSetting) as ScriptableObject);
				else
					this.so.Update();
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException("Setting " + this.typeSetting + " is failing. (" + HQ.Settings.Get(this.typeSetting)  + ")", ex);
				return;
			}

			GUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Reset", GUILayoutOptionPool.ExpandWidthFalse) == true &&
					((Event.current.modifiers & Constants.ByPassPromptModifier) != 0 || EditorUtility.DisplayDialog(NGSettingsWindow.Title, LC.G("ConsoleSettings_ResetConfirm"), LC.G("Yes"), LC.G("No")) == true))
				{
					if (this.typeSetting.IsSubclassOf(typeof(ScriptableObject)) == true)
					{
						this.so = null;
						// Delete the current settings.
						HQ.Settings.Clear(this.typeSetting);
						// Then regenerate it to ensure it exists and is called from OnGUI context.
						HQ.Settings.Get(this.typeSetting);
					}
					else
					{
						object	settings = Activator.CreateInstance(this.fieldInfo.FieldType);

						this.fieldInfo.SetValue(HQ.Settings, settings);
					}

					HQ.InvalidateSettings();
					InternalEditorUtility.RepaintAllViews();
					return;
				}
			}
			GUILayout.EndHorizontal();

			if (this.typeSetting.IsSubclassOf(typeof(ScriptableObject)) == true)
			{
				SerializedProperty	iterator = this.so.GetIterator();

				iterator.NextVisible(true);

				EditorGUI.BeginChangeCheck();

				while (iterator.NextVisible(false) == true)
					EditorGUILayout.PropertyField(iterator, true);

				if (EditorGUI.EndChangeCheck() == true)
				{
					this.so.ApplyModifiedProperties();
					HQ.InvalidateSettings();
				}
			}
			else
			{
				SerializedProperty	iterator = this.so.FindProperty(this.fieldInfo.Name);
				SerializedProperty	end = iterator.GetEndProperty();
				bool				enterChildren = true;

				EditorGUI.BeginChangeCheck();

				while (iterator.NextVisible(enterChildren) == true && SerializedProperty.EqualContents(iterator, end) == false)
				{
					EditorGUILayout.PropertyField(iterator, true);
					enterChildren = false;
				}

				if (EditorGUI.EndChangeCheck() == true)
				{
					this.so.ApplyModifiedProperties();
					HQ.InvalidateSettings();
				}
			}
		}
	}
}