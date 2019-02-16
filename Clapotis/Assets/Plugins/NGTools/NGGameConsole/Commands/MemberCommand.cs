using System;

namespace NGTools.NGGameConsole
{
	public class MemberCommand : CommandNode
	{
		public override bool	IsLeaf { get { return true; } }

		private IFieldModifier					member;
		private PropertyConstraintAttribute[]	constraints;

		public	MemberCommand(CommandAttribute attribute, IFieldModifier member, object instance) : base(instance, attribute.name, attribute.description)
		{
			this.member = member;
			this.constraints = member.GetCustomAttributes(typeof(PropertyConstraintAttribute), true) as PropertyConstraintAttribute[];

			if (this.member.Type.IsClass() == true &&
				this.member.Type != typeof(string))
			{
				throw new NotSupportedMemberTypeException(this.member);
			}
		}

		public	MemberCommand(IFieldModifier member, object instance) : base(instance, member.Name, string.Empty)
		{
			this.member = member;
			this.constraints = member.GetCustomAttributes(typeof(PropertyConstraintAttribute), true) as PropertyConstraintAttribute[];

			if (this.member.Type.IsClass() == true &&
				this.member.Type != typeof(string))
			{
				throw new NotSupportedMemberTypeException(this.member);
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// <exception cref="NGTools.NotSupportedPropertyTypeException">Thrown when a property is of an unsupported type.</exception>
		public override string	GetSetInvoke(params string[] args)
		{
			if (args.Length == 0)
			{
				object	value = this.member.GetValue(this.instance);

				if (value != null)
					return value.ToString();
				return string.Empty;
			}
			else if (args.Length != 1)
				return "Too many arguments.";

			if (this.member.Type == typeof(Int32))
			{
				Int32	n;
				if (Int32.TryParse(args[0], out n) == true)
					return this.SetValue(n);
			}
			else if (this.member.Type == typeof(Single))
			{
				Single	n;
				if (Single.TryParse(args[0], out n) == true)
					return this.SetValue(n);
			}
			else if (this.member.Type == typeof(String))
				return this.SetValue(args[0]);
			else if (this.member.Type == typeof(Boolean))
			{
				// No constraints possible on boolean.
				if (args[0].Equals("true", StringComparison.OrdinalIgnoreCase) == true)
					this.member.SetValue(this.instance, true);
				else if (args[0].Equals("false", StringComparison.OrdinalIgnoreCase) == true)
					this.member.SetValue(this.instance, false);
				return this.member.GetValue(this.instance).ToString();
			}
			else if (this.member.Type.IsEnum() == true)
			{
				try
				{
					return this.SetValue(Enum.Parse(this.member.Type, args[0], true));
				}
				catch
				{
					return "Wrong enum. Values available: " + string.Join(", ", Enum.GetNames(this.member.Type)) + ".";
				}
			}
			else if (this.member.Type == typeof(Char))
			{
				Char	n;
				if (Char.TryParse(args[0], out n) == true)
					return this.SetValue(n);
			}
			else if (this.member.Type == typeof(UInt32))
			{
				UInt32	n;
				if (UInt32.TryParse(args[0], out n) == true)
					return this.SetValue(n);
			}
			else if (this.member.Type == typeof(Int16))
			{
				Int16	n;
				if (Int16.TryParse(args[0], out n) == true)
					return this.SetValue(n);
			}
			else if (this.member.Type == typeof(UInt16))
			{
				UInt16	n;
				if (UInt16.TryParse(args[0], out n) == true)
					return this.SetValue(n);
			}
			else if (this.member.Type == typeof(Int64))
			{
				Int64	n;
				if (Int64.TryParse(args[0], out n) == true)
					return this.SetValue(n);
			}
			else if (this.member.Type == typeof(UInt64))
			{
				UInt64	n;
				if (UInt64.TryParse(args[0], out n) == true)
					return this.SetValue(n);
			}
			else if (this.member.Type == typeof(Double))
			{
				Double	n;
				if (Double.TryParse(args[0], out n) == true)
					return this.SetValue(n);
			}
			else if (this.member.Type == typeof(Decimal))
			{
				Decimal	n;
				if (Decimal.TryParse(args[0], out n) == true)
					return this.SetValue(n);
			}
			else if (this.member.Type == typeof(Byte))
			{
				Byte	n;
				if (Byte.TryParse(args[0], out n) == true)
					return this.SetValue(n);
			}
			else if (this.member.Type == typeof(SByte))
			{
				SByte	n;
				if (SByte.TryParse(args[0], out n) == true)
					return this.SetValue(n);
			}
			else
				throw new NotSupportedMemberTypeException(this.member);

			return "Invalid value.";
		}

		private string	SetValue(object n)
		{
			string	error = this.CheckValueConstraints(n);

			if (error != null)
				return error;
			this.member.SetValue(this.instance, n);
			return this.member.GetValue(this.instance).ToString();
		}

		private string	CheckValueConstraints(object value)
		{
			for (int i = 0; i < this.constraints.Length; i++)
			{
				if (this.constraints[i].Check(value) == false)
					return this.constraints[i].GetDescription();
			}

			return null;
		}
	}
}