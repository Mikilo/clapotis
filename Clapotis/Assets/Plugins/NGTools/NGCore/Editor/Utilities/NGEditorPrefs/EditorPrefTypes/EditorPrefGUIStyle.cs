using NGTools;
using System;
using System.Reflection;
using UnityEngine;

namespace NGToolsEditor
{
	internal sealed class EditorPrefGUIStyle : EditorPrefType
	{
		public PropertyModifier[]	fields;

		public	EditorPrefGUIStyle()
		{
			this.fields = new PropertyModifier[] {
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "normal", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "hover", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "active", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "focused", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "onNormal", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "onHover", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "onActive", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "onFocused", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "border", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "margin", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "padding", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "overflow", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "font", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "fontSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "fontStyle", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "alignment", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "wordWrap", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "richText", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "clipping", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "imagePosition", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "contentOffset", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "fixedWidth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "fixedHeight", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "stretchWidth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
				new PropertyModifier(UnityAssemblyVerifier.TryGetProperty(typeof(GUIStyle), "stretchHeight", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)),
			};
		}

		public override bool	CanHandle(Type type)
		{
			return type == typeof(GUIStyle);
		}

		public override void	DirectSave(object instance, Type type, string path)
		{
			if (instance == null)
				return;

			for (int i = 0; i < this.fields.Length; i++)
			{
				object	value = this.fields[i].GetValue(instance);

				Utility.DirectSaveEditorPref(value, this.fields[i].Type, path + '.' + this.fields[i].Name);
			}
		}

		public override void	Load(object instance, Type type, string path)
		{
			for (int i = 0; i < this.fields.Length; i++)
				this.fields[i].SetValue(instance, Utility.LoadEditorPref(this.fields[i].GetValue(instance), this.fields[i].Type, path + '.' + this.fields[i].Name));
		}

		public override object	Fetch(object instance, Type type, string path)
		{
			if (instance == null)
				return null;

			this.Load(instance, type, path);

			return instance;
		}
	}
}