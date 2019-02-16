namespace NGToolsEditor.NGConsole
{
	internal sealed class UselessFrameFilter : IStackFrameFilter
	{
		public bool	Filter(string frame)
		{
			StackTraceSettings	settings = HQ.Settings.Get<StackTraceSettings>();

			for (int i = 0; i < settings.filters.Count; i++)
			{
				if (frame.StartsWith(settings.filters[i]) == true)
					return true;
			}

			return frame.StartsWith("NGDebug:") == true ||
				   frame.Contains(".InternalNGDebug:") == true ||
				   frame.StartsWith("UnityEngine.Debug:") == true ||
				   frame.StartsWith("UnityEditor.DockArea:OnGUI") == true;
		}
	}
}