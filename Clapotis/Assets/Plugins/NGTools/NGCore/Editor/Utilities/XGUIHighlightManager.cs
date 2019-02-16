using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NGToolsEditor
{
	public static class XGUIHighlightManager
	{
		[Flags]
		public enum Highlights
		{
			Glow = 1 << 0,
			Wave = 1 << 1,
		}

		private class HighlightInstance
		{
			public string		id;
			public double		endTime = -1F;
			public EditorWindow	window;
		}

		private const float				Duration = 10F;
		private const float				RectGlowSpeed = 2.5F;
		private const float				MaxWaveRange = 500F;
		private const float				MaxWaveSpeed = 1.9F;
		private const float				WaveInterval = 1.8F;
		private static	Color			WaveColor { get { return Utility.GetSkinColor(1F, .92F, .016F, 1F, 60F / 255F, 31F / 255F, 224F / 255F, 1F); } }
		private static readonly Color	GlowStartColor = Color.white;
		private static readonly Color	GlowEndColor = Color.black;

		private static List<HighlightInstance>	pendingHighlights = new List<HighlightInstance>();
		private static List<HighlightInstance>	runningHighlights = new List<HighlightInstance>();

		public static void	Highlight(string id)
		{
			int	i = 0;

			for (; i < XGUIHighlightManager.pendingHighlights.Count; i++)
			{
				if (XGUIHighlightManager.pendingHighlights[i].id == id)
				{
					XGUIHighlightManager.pendingHighlights[i].endTime = -1F;
					break;
				}
			}

			if (i == XGUIHighlightManager.pendingHighlights.Count)
				XGUIHighlightManager.pendingHighlights.Add(new HighlightInstance() { id = id });

			InternalEditorUtility.RepaintAllViews();
		}

		public static void	Highlight(string id1, string id2)
		{
			int	i = 0;

			for (; i < XGUIHighlightManager.pendingHighlights.Count; i++)
			{
				if (XGUIHighlightManager.pendingHighlights[i].id == id1)
				{
					XGUIHighlightManager.pendingHighlights[i].endTime = -1F;
					break;
				}
			}

			if (i == XGUIHighlightManager.pendingHighlights.Count)
				XGUIHighlightManager.pendingHighlights.Add(new HighlightInstance() { id = id1 });

			for (i = 0; i < XGUIHighlightManager.pendingHighlights.Count; i++)
			{
				if (XGUIHighlightManager.pendingHighlights[i].id == id2)
				{
					XGUIHighlightManager.pendingHighlights[i].endTime = -1F;
					break;
				}
			}

			if (i == XGUIHighlightManager.pendingHighlights.Count)
				XGUIHighlightManager.pendingHighlights.Add(new HighlightInstance() { id = id2 });

			InternalEditorUtility.RepaintAllViews();
		}

		public static void	Highlight(string id1, string id2, string id3)
		{
			int	i = 0;

			for (; i < XGUIHighlightManager.pendingHighlights.Count; i++)
			{
				if (XGUIHighlightManager.pendingHighlights[i].id == id1)
				{
					XGUIHighlightManager.pendingHighlights[i].endTime = -1F;
					break;
				}
			}

			if (i == XGUIHighlightManager.pendingHighlights.Count)
				XGUIHighlightManager.pendingHighlights.Add(new HighlightInstance() { id = id1 });

			for (i = 0; i < XGUIHighlightManager.pendingHighlights.Count; i++)
			{
				if (XGUIHighlightManager.pendingHighlights[i].id == id2)
				{
					XGUIHighlightManager.pendingHighlights[i].endTime = -1F;
					break;
				}
			}

			if (i == XGUIHighlightManager.pendingHighlights.Count)
				XGUIHighlightManager.pendingHighlights.Add(new HighlightInstance() { id = id2 });

			for (i = 0; i < XGUIHighlightManager.pendingHighlights.Count; i++)
			{
				if (XGUIHighlightManager.pendingHighlights[i].id == id3)
				{
					XGUIHighlightManager.pendingHighlights[i].endTime = -1F;
					break;
				}
			}

			if (i == XGUIHighlightManager.pendingHighlights.Count)
				XGUIHighlightManager.pendingHighlights.Add(new HighlightInstance() { id = id3 });

			InternalEditorUtility.RepaintAllViews();
		}

		public static bool	IsHighlightRunning(string id)
		{
			for (int i = 0; i < XGUIHighlightManager.runningHighlights.Count; i++)
			{
				if (XGUIHighlightManager.runningHighlights[i].id == id)
					return true;
			}

			return false;
		}

		public static void	DrawHighlightLayout(string id, EditorWindow window, Highlights highlights = XGUIHighlightManager.Highlights.Glow | Highlights.Wave)
		{
			if (Event.current.type != EventType.Repaint)
				return;

			XGUIHighlightManager.DrawHighlight(id, window, GUILayoutUtility.GetLastRect(), true, highlights);
		}

		public static void	DrawHighlightLayout(string id, EditorWindow window, bool canCancel, Highlights highlights = XGUIHighlightManager.Highlights.Glow | Highlights.Wave)
		{
			if (Event.current.type != EventType.Repaint)
				return;

			XGUIHighlightManager.DrawHighlight(id, window, GUILayoutUtility.GetLastRect(), canCancel);
		}

		public static void	DrawHighlight(string id, EditorWindow window, Rect r, Highlights highlights)
		{
			if (Event.current.type != EventType.Repaint)
				return;

			XGUIHighlightManager.DrawHighlight(id, window, r, true, highlights);
		}

		public static void	DrawHighlight(string id, EditorWindow window, Rect r, bool canCancel = true, Highlights highlights = XGUIHighlightManager.Highlights.Glow | Highlights.Wave)
		{
			if (Event.current.type != EventType.Repaint)
				return;

			for (int i = 0; i < XGUIHighlightManager.pendingHighlights.Count; i++)
			{
				if (XGUIHighlightManager.pendingHighlights[i].id == id)
				{
					XGUIHighlightManager.runningHighlights.Add(XGUIHighlightManager.pendingHighlights[i]);
					XGUIHighlightManager.pendingHighlights.RemoveAt(i);
					window.Repaint();
					break;
				}
			}

			for (int i = 0; i < XGUIHighlightManager.runningHighlights.Count; i++)
			{
				if (XGUIHighlightManager.runningHighlights[i].id == id)
				{
					if (XGUIHighlightManager.runningHighlights[i].endTime < 0F)
					{
						XGUIHighlightManager.runningHighlights[i].endTime = EditorApplication.timeSinceStartup + XGUIHighlightManager.Duration;
						XGUIHighlightManager.runningHighlights[i].window = window;
					}

					if (EditorApplication.timeSinceStartup < XGUIHighlightManager.runningHighlights[i].endTime &&
						(canCancel == false ||
						 (XGUIHighlightManager.runningHighlights[i].endTime - EditorApplication.timeSinceStartup > 9F ||
						  r.Contains(Event.current.mousePosition) == false)))
					{
						float	time = (float)(EditorApplication.timeSinceStartup - XGUIHighlightManager.runningHighlights[i].endTime);

						if ((highlights & Highlights.Wave) != 0)
						{
							Utility.DrawCircle(r.center,
											   XGUIHighlightManager.WaveColor,
											   Mathf.Lerp(0F, XGUIHighlightManager.MaxWaveRange, 1F - Mathf.Min(1F, Mathf.Repeat(time * XGUIHighlightManager.MaxWaveSpeed, XGUIHighlightManager.WaveInterval))));
						}

						if ((highlights & Highlights.Glow) != 0)
							Utility.DrawUnfillRect(r, Color.Lerp(XGUIHighlightManager.GlowStartColor, XGUIHighlightManager.GlowEndColor, Mathf.PingPong(time * XGUIHighlightManager.RectGlowSpeed, 1F)));

						XGUIHighlightManager.runningHighlights[i].window.Repaint();
					}
					else
						XGUIHighlightManager.runningHighlights.RemoveAt(i--);
				}
			}
		}

		//public static void	OnGUI(string prefix, EditorWindow window)
		//{
		//	for (int i = 0; i < XGUIHighlightManager.runningHighlights.Count; i++)
		//	{
		//		if (XGUIHighlightManager.runningHighlights[i].id.StartsWith(prefix) == false)
		//			continue;

		//		if (XGUIHighlightManager.runningHighlights[i].endTime < 0F)
		//		{
		//			XGUIHighlightManager.runningHighlights[i].endTime = EditorApplication.timeSinceStartup + XGUIHighlightManager.Duration;
		//			XGUIHighlightManager.runningHighlights[i].window = window;
		//		}

		//		if (EditorApplication.timeSinceStartup < XGUIHighlightManager.runningHighlights[i].endTime &&
		//			(XGUIHighlightManager.runningHighlights[i].endTime - EditorApplication.timeSinceStartup > 9F ||
		//			 XGUIHighlightManager.runningHighlights[i].position.Contains(Event.current.mousePosition) == false))
		//		{
		//			Utility.DrawCircle(XGUIHighlightManager.runningHighlights[i].position.center, XGUIHighlightManager.WaveColor, Mathf.Lerp(0F, XGUIHighlightManager.MaxWaveRange, 1F - Mathf.Min(1F, Mathf.Repeat((float)EditorApplication.timeSinceStartup * XGUIHighlightManager.MaxWaveSpeed, XGUIHighlightManager.WaveInterval))));
		//			Utility.DrawUnfillRect(XGUIHighlightManager.runningHighlights[i].position, Color.Lerp(XGUIHighlightManager.GlowStartColor, XGUIHighlightManager.GlowEndColor, Mathf.PingPong((float)EditorApplication.timeSinceStartup * XGUIHighlightManager.RectGlowSpeed, 1F)));
		//			XGUIHighlightManager.runningHighlights[i].window.Repaint();
		//		}
		//		else
		//			XGUIHighlightManager.runningHighlights.RemoveAt(i--);
		//	}
		//}
	}
}