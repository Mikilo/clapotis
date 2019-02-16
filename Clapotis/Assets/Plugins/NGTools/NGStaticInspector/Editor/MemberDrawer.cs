using NGTools;
using System;

namespace NGToolsEditor.NGStaticInspector
{
	public class MemberDrawer
	{
		public readonly TypeDrawer		typeDrawer;
		public readonly IFieldModifier	fieldModifier;
		public readonly bool			isEditable;
		public Exception				exception;

		public	MemberDrawer(TypeDrawer typeDrawer, IFieldModifier fieldModifier)
		{
			this.typeDrawer = typeDrawer;
			this.fieldModifier = fieldModifier;

			if (this.fieldModifier is FieldModifier)
			{
				FieldModifier	modifier = (this.fieldModifier as FieldModifier);

				this.isEditable = modifier.fieldInfo.IsLiteral == false && modifier.fieldInfo.IsInitOnly == false;
			}
			else
				this.isEditable = (this.fieldModifier as PropertyModifier).propertyInfo.GetSetMethod() != null;
		}
	}
}