using UnityEngine;

namespace NGTools
{
	public class HideIfAttribute : PropertyAttribute
	{
		public readonly string		fieldName;
		public readonly Op			@operator;
		public readonly MultiOp		multiOperator;
		public readonly object[]	values;

		public	HideIfAttribute(string fieldName, Op @operator, object value)
		{
			this.fieldName = fieldName;
			this.@operator = @operator;
			this.multiOperator = MultiOp.None;
			this.values = new object[] { value };
		}

		public	HideIfAttribute(string fieldName, MultiOp multiOperator, params object[] values)
		{
			this.fieldName = fieldName;
			this.@operator = Op.None;
			this.multiOperator = multiOperator;
			this.values = values;
		}
	}
}