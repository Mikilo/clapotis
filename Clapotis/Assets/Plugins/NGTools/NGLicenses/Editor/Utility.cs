using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;

namespace NGLicenses
{
	internal static class Utility
	{
		private static Stack<StringBuilder>	poolBuffers = new Stack<StringBuilder>(2);

		public static StringBuilder	GetBuffer(string initialValue)
		{
			StringBuilder	b = GetBuffer();
			b.Append(initialValue);
			return b;
		}

		public static StringBuilder	GetBuffer()
		{
			if (Utility.poolBuffers.Count > 0)
				return Utility.poolBuffers.Pop();
			return new StringBuilder(64);
		}

		public static string	ReturnBuffer(StringBuilder buffer)
		{
			string	result = buffer.ToString();
			buffer.Length = 0;
			Utility.poolBuffers.Push(buffer);
			return result;
		}

		private class CallbackSchedule
		{
			public Action	action;
			public int		intervalTicks;
			public int		ticksLeft;
			public int		remainingCalls;
		}

		private static List<CallbackSchedule>	schedules = new List<CallbackSchedule>();

		public static void	RegisterIntervalCallback(Action action, int ticks, int count = -1)
		{
			if (Utility.schedules.Count == 0)
				EditorApplication.update += Utility.TickIntervalCallbacks;

			for (int i = 0; i < Utility.schedules.Count; i++)
			{
				if (Utility.schedules[i].action == action)
				{
					Utility.schedules[i].ticksLeft = ticks;
					Utility.schedules[i].intervalTicks = ticks;
					Utility.schedules[i].remainingCalls = count;
					return;
				}
			}

			Utility.schedules.Add(new CallbackSchedule() { action = action, ticksLeft = ticks, intervalTicks = ticks, remainingCalls = count });
		}

		public static void	UnregisterIntervalCallback(Action action)
		{
			for (int i = 0; i < Utility.schedules.Count; i++)
			{
				if (Utility.schedules[i].action == action)
				{
					Utility.schedules.RemoveAt(i);

					if (Utility.schedules.Count == 0)
						EditorApplication.update -= Utility.TickIntervalCallbacks;

					break;
				}
			}
		}

		private static void	TickIntervalCallbacks()
		{
			for (int i = 0; i < Utility.schedules.Count; i++)
			{
				if (--Utility.schedules[i].ticksLeft <= 0)
				{
					CallbackSchedule	callback = Utility.schedules[i];
					callback.ticksLeft = callback.intervalTicks;
					callback.action();

					--callback.remainingCalls;

					if (callback.remainingCalls == 0)
						Utility.schedules.RemoveAt(i);

					if (i < Utility.schedules.Count)
					{
						if (callback != Utility.schedules[i])
							--i;
					}
				}
			}
		}
	}
}