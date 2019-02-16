using System;

namespace NGToolsEditor.NGInspectorGadget
{
	public static class NGInspectorGadget
	{
		public static event Action	GUISettings;

		[NGSettings("NG Inspector Gadget")]
		private static void	OnGUISettings()
		{
			if (HQ.Settings == null)
				return;

			if (NGInspectorGadget.GUISettings != null)
				NGInspectorGadget.GUISettings();
		}
	}
}