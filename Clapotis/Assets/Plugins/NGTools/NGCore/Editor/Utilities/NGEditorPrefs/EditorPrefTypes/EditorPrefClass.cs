using System;
using System.Reflection;
using UnityEngine;

namespace NGToolsEditor
{
	internal sealed class EditorPrefClass : EditorPrefType
	{
		public override bool	CanHandle(Type type)
		{
			return type.IsClass == true;
		}

		public override void	DirectSave(object instance, Type type, string path)
		{
			if (instance == null)
				return;

			foreach (FieldInfo field in Utility.EachFieldHierarchyOrdered(type, typeof(object), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				if (field.IsDefined(typeof(NonSerializedAttribute), true) == true ||
					(field.IsPublic == false && field.IsDefined(typeof(SerializeField), true) == false))
				{
					continue;
				}

				object	value = field.GetValue(instance);

				if (value == null || object.Equals(value, 0) == true)
					value = this.GetDefaultValue(field) ?? value;

				Utility.DirectSaveEditorPref(value, field.FieldType, path + '@' + instance.GetType().Name + '.' + field.Name);
			}
		}

		public override void	Save(object instance, Type type, string path)
		{
			if (instance == null)
				return;

			this.DirectSave(instance, type, path);
		}

		public override void	Load(object instance, Type type, string path)
		{
			foreach (FieldInfo field in Utility.EachFieldHierarchyOrdered(type, typeof(object), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				if (field.IsDefined(typeof(NonSerializedAttribute), true) == true ||
					(field.IsPublic == false && field.IsDefined(typeof(SerializeField), true) == false))
				{
					continue;
				}

				field.SetValue(instance, Utility.LoadEditorPref(field.GetValue(instance), field.FieldType, path + '@' + instance.GetType().Name + '.' + field.Name));
			}
		}

		public override object	Fetch(object instance, Type type, string path)
		{
			if (instance == null)
				return null;

			foreach (FieldInfo field in Utility.EachFieldHierarchyOrdered(type, typeof(object), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				if (field.IsDefined(typeof(NonSerializedAttribute), true) == true ||
					(field.IsPublic == false && field.IsDefined(typeof(SerializeField), true) == false))
				{
					continue;
				}

				field.SetValue(instance, Utility.LoadEditorPref(field.GetValue(instance), field.FieldType, path + '@' + instance.GetType().Name + '.' + field.Name));
			}

			return instance;
		}
	}
}