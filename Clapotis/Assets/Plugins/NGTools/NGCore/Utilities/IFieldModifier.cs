using System;
using System.Reflection;

namespace NGTools
{
	/// <summary>Provides properties and methods to alter/display a field/property of a type.</summary>
	public interface IFieldModifier : IValueGetter
	{
		string		Name { get; }
		bool		IsPublic { get; }
		MemberInfo	MemberInfo { get; }

		void	SetValue(object instance, object value);
		object	GetValue(object instance);
	}
}