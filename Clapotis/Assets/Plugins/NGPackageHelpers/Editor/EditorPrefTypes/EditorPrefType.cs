using System.Reflection;

namespace NGPackageHelpers
{
	public abstract class EditorPrefType
	{
		public abstract bool	CanHandle(FieldInfo field);
		public abstract void	Save(object instance, FieldInfo field, string prefix);
		public abstract void	Load(object instance, FieldInfo field, string prefix);

		protected object	GetDefaultValue(FieldInfo field)
		{
			foreach (DefaultValueEditorPrefAttribute attribute in field.GetCustomAttributes(typeof(DefaultValueEditorPrefAttribute), true))
			{
				return attribute.defaultValue;
			}

			return null;
		}
	}
}