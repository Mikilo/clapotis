using System;
using System.Reflection;

namespace NGPackageHelpers
{
	internal sealed class EditorPrefString : EditorPrefType
	{
		public override bool	CanHandle(FieldInfo field)
		{
			return field.FieldType == typeof(String);
		}

		public override void	Save(object instance, FieldInfo field, string prefix)
		{
			NGEditorPrefs.SetString(prefix + instance.GetType().FullName + '.' + field.Name, (String)field.GetValue(instance));
		}

		public override void	Load(object instance, FieldInfo field, string prefix)
		{
			field.SetValue(instance, NGEditorPrefs.GetString(prefix + instance.GetType().FullName + '.' + field.Name, (String)(this.GetDefaultValue(field) ?? field.GetValue(instance))));
		}
	}
}