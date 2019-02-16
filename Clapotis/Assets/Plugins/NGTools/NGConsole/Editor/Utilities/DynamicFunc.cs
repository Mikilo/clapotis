using System;
using UnityEditor;

namespace NGToolsEditor.NGConsole
{
	public class DynamicFunc<T1>
	{
		private Func<T1, T1>[]	callbacks = new Func<T1, T1>[0];

		public T1	Invoke(T1 value)
		{
			for (int i = 0; i < this.callbacks.Length; i++)
				value = this.callbacks[i](value);

			return value;
		}

		public static DynamicFunc<T1> operator	+(DynamicFunc<T1> a, Func<T1, T1> callback)
		{
			if (a == null)
				a = new DynamicFunc<T1>();

			ArrayUtility.Add(ref a.callbacks, callback);
			return a;
		}

		public static DynamicFunc<T1> operator	-(DynamicFunc<T1> a, Func<T1, T1> callback)
		{
			if (a != null)
				ArrayUtility.Remove(ref a.callbacks, callback);
			return a;
		}

		public static bool operator	!=(DynamicFunc<T1> a, object callback)
		{
			return !object.Equals(a, null) && a.callbacks.Length > 0;
		}

		public static bool operator	==(DynamicFunc<T1> a, object callback)
		{
			return object.Equals(a, null) || a.callbacks.Length == 0;
		}

		public override bool	Equals(object obj)
		{
			return base.Equals(obj);
		}

		public override int	GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}