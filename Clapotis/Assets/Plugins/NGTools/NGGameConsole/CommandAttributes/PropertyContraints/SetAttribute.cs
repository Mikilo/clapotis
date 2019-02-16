using System;
using System.Text;

namespace NGTools.NGGameConsole
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class SetAttribute : PropertyConstraintAttribute
	{
		public readonly int[]	values;

		private string	valuesStringified = null;

		public	SetAttribute(params int[] values)
		{
			this.values = values;
		}

		public override bool	Check(object value)
		{
			return value is int && this.AllowValue((int)value) == true;
		}

		public override string	GetDescription()
		{
			if (this.valuesStringified == null)
			{
				StringBuilder	buffer = Utility.GetBuffer();

				for (int i = 0; i < this.values.Length; i++)
				{
					buffer.Append(this.values[i]);
					buffer.Append(',');
				}

				buffer.Length -= 1;

				this.valuesStringified = Utility.ReturnBuffer(buffer);
			}

			return "Value must be one of the following: " + this.valuesStringified + ".";
		}

		private bool	AllowValue(int value)
		{
			for (int i = 0; i < this.values.Length; i++)
			{
				if (this.values[i] == value)
					return true;
			}

			return false;
		}
	}
}