using System;
using System.Collections;
using System.Reflection;

namespace NGPackageHelpers
{
	/// <summary>
	/// Saves IList with any class, but the class must be public and implement a default constructor.
	/// </summary>
	internal sealed class EditorPrefList : EditorPrefType
	{
		public override bool	CanHandle(FieldInfo field)
		{
			return typeof(IList).IsAssignableFrom(field.FieldType);
		}

		public override void	Save(object instance, FieldInfo field, string prefix)
		{
			IList	list = field.GetValue(instance) as IList;

			prefix += instance.GetType().FullName + '.' + field.Name;
			NGEditorPrefs.SetInt(prefix, list.Count);
			prefix += '.';

			for (int i = 0; i < list.Count; i++)
				NGEditorPrefs.SetString(prefix + i, Convert.ToBase64String(Utility.SerializeField(list[i])));
		}

		public override void	Load(object instance, FieldInfo field, string prefix)
		{
			IList	list = field.GetValue(instance) as IList;
			int		count = NGEditorPrefs.GetInt(prefix + instance.GetType().FullName + '.' + field.Name, -1);
			Type	subType = Utility.GetArraySubType(field.FieldType);

			if (count != -1)
			{
				list.Clear();

				prefix += instance.GetType().FullName + '.' + field.Name + '.';

				try
				{
					for (int i = 0; i < count; i++)
					{
						object	element = null;
						string	v = NGEditorPrefs.GetString(prefix + i, null);

						if (v != null)
							element = Utility.DeserializeField<object>(Convert.FromBase64String(v));
						else if (subType.IsValueType == true)
							element = Activator.CreateInstance(subType);

						list.Add(element);
					}

					field.SetValue(instance, list);
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException(prefix, ex);
				}
			}
		}
	}
}