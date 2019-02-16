using System;
using System.Reflection;

namespace NGPackageHelpers
{
	internal sealed class EditorPrefBoolean : EditorPrefType
	{
		public override bool	CanHandle(FieldInfo field)
		{
			return field.FieldType == typeof(Boolean);
		}

		public override void	Save(object instance, FieldInfo field, string prefix)
		{
			NGEditorPrefs.SetBool(prefix + instance.GetType().FullName + '.' + field.Name, (Boolean)field.GetValue(instance));
		}

		public override void	Load(object instance, FieldInfo field, string prefix)
		{
			field.SetValue(instance, NGEditorPrefs.GetBool(prefix + instance.GetType().FullName + '.' + field.Name, (Boolean)(this.GetDefaultValue(field) ?? field.GetValue(instance))));
		}
	}
}