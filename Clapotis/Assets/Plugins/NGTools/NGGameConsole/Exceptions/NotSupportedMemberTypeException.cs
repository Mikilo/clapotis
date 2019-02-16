using System;

namespace NGTools.NGGameConsole
{
	/// <summary>
	/// Thrown when calling GetSetInvoke on an unhandled property type.
	/// </summary>
	[Serializable]
	internal sealed class NotSupportedMemberTypeException : Exception
	{
		public readonly IFieldModifier	member;

		public override string	Message
		{
			get
			{
				return "Member \"" + this.member.Name + "\" of type \"" + this.member.Type.Name + "\" is not supported.";
			}
		}

		public	NotSupportedMemberTypeException(IFieldModifier type)
		{
			this.member = type;
		}
	}
}