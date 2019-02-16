using System.Collections.Generic;
using UnityEngine;

namespace NGToolsEditor
{
	public static class GUILayoutOptionPool
	{
		private static GUILayoutOption	expandWidthTrue;
		public static GUILayoutOption	ExpandWidthTrue { get { return expandWidthTrue ?? (expandWidthTrue = GUILayout.ExpandWidth(true)); } }
		private static GUILayoutOption	expandWidthFalse;
		public static GUILayoutOption	ExpandWidthFalse { get { return expandWidthFalse ?? (expandWidthFalse = GUILayout.ExpandWidth(false)); } }
		
		private static GUILayoutOption	expandHeightTrue;
		public static GUILayoutOption	ExpandHeightTrue { get { return expandHeightTrue ?? (expandHeightTrue = GUILayout.ExpandHeight(true)); } }
		private static GUILayoutOption	expandHeightFalse;
		public static GUILayoutOption	ExpandHeightFalse { get { return expandHeightFalse ?? (expandHeightFalse = GUILayout.ExpandHeight(false)); } }

		private static Dictionary<float, GUILayoutOption>	cachedWidth = new Dictionary<float, GUILayoutOption>();

		public static GUILayoutOption	Width(float width)
		{
			GUILayoutOption	restorer;

			if (GUILayoutOptionPool.cachedWidth.TryGetValue(width, out restorer) == false)
			{
				restorer = GUILayout.Width(width);

				GUILayoutOptionPool.cachedWidth.Add(width, restorer);
			}

			return restorer;
		}
		private static Dictionary<float, GUILayoutOption>	cachedHeight = new Dictionary<float, GUILayoutOption>();

		public static GUILayoutOption	Height(float height)
		{
			GUILayoutOption	restorer;

			if (GUILayoutOptionPool.cachedHeight.TryGetValue(height, out restorer) == false)
			{
				restorer = GUILayout.Height(height);

				GUILayoutOptionPool.cachedHeight.Add(height, restorer);
			}

			return restorer;
		}

		private static Dictionary<float, GUILayoutOption>	cachedMinWidth = new Dictionary<float, GUILayoutOption>();

		public static GUILayoutOption	MinWidth(float width)
		{
			GUILayoutOption	restorer;

			if (GUILayoutOptionPool.cachedMinWidth.TryGetValue(width, out restorer) == false)
			{
				restorer = GUILayout.MinWidth(width);

				GUILayoutOptionPool.cachedMinWidth.Add(width, restorer);
			}

			return restorer;
		}

		private static Dictionary<float, GUILayoutOption>	cachedMaxWidth = new Dictionary<float, GUILayoutOption>();

		public static GUILayoutOption	MaxWidth(float width)
		{
			GUILayoutOption	restorer;

			if (GUILayoutOptionPool.cachedMaxWidth.TryGetValue(width, out restorer) == false)
			{
				restorer = GUILayout.MaxWidth(width);

				GUILayoutOptionPool.cachedMaxWidth.Add(width, restorer);
			}

			return restorer;
		}

		private static Dictionary<float, GUILayoutOption>	cachedMaxHeight = new Dictionary<float, GUILayoutOption>();

		public static GUILayoutOption	MaxHeight(float height)
		{
			GUILayoutOption	restorer;

			if (GUILayoutOptionPool.cachedMaxHeight.TryGetValue(height, out restorer) == false)
			{
				restorer = GUILayout.MaxHeight(height);

				GUILayoutOptionPool.cachedMaxHeight.Add(height, restorer);
			}

			return restorer;
		}
	}
}