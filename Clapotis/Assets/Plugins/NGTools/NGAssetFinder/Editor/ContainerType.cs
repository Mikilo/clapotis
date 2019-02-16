using System;
using System.Collections.Generic;
using System.Reflection;

namespace NGToolsEditor.NGAssetFinder
{
	internal enum ParseResult
	{
		HasObject,
		MustDiscard,
		Unsure,
	}

	internal sealed class ContainerType
	{
		public Type					type;
		public List<FieldInfo>		fields;
		public List<PropertyInfo>	properties;
		public List<MemberInfo>		unsureMembers;
		public ParseResult			containObject;

		public	ContainerType(Type type, ParseResult containObject)
		{
			this.type = type;
			this.fields = new List<FieldInfo>();
			this.properties = new List<PropertyInfo>();
			this.unsureMembers = new List<MemberInfo>();
			this.containObject = containObject;
		}
	}
}