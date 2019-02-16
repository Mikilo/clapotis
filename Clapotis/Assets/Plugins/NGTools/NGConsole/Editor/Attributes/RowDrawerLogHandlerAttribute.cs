using System;

namespace NGToolsEditor.NGConsole
{
	/// <summary>
	/// <para>Any class having this attribute must implement:</para>
	/// <para>private static bool {<see cref="RowLogHandleAttribute.StaticMethodName"/>}(UnityLogEntry).</para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class RowLogHandlerAttribute : Attribute
	{
		public const string	StaticVerifierMethodName = "CanDealWithIt";

		public readonly int	priority;

		public Func<UnityLogEntry, bool>	handler;

		/// <param name="priority">The higher the bigger priority.</param>
		public	RowLogHandlerAttribute(int priority)
		{
			this.priority = priority;
		}
	}
}