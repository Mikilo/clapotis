using UnityEditor;

namespace NGToolsEditor
{
	internal static class Metrics
	{
		private const string	UsedToolsPrefKey = "NGT_UsedTools";

		internal static uint	GetUsedTools()
		{
			return (uint)EditorPrefs.GetInt(Metrics.UsedToolsPrefKey, 0);
		}

		internal static void	UseTool(int toolID)
		{
			EditorPrefs.SetInt(Metrics.UsedToolsPrefKey, EditorPrefs.GetInt(Metrics.UsedToolsPrefKey) | 1 << (toolID - 1));
		}

		internal static void	ResetUsedTools()
		{
			EditorPrefs.SetInt(Metrics.UsedToolsPrefKey, 0);
		}
	}
}