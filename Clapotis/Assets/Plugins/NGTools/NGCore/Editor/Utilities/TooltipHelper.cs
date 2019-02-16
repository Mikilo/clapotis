using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor
{
	public static class TooltipHelper
	{
		private enum DrawType
		{
			Label,
			Helpbox,
			Custom
		}

		public const float	XOffset = 2F;
		public const float	YOffset = 21F;
		public const float	DelayFadeIn = .2F;
		public const float	FadeInDuration = .15F;

		private static Color	helpboxBackgroundColor = new Color(.4F, .4F, .4F, 1F);
		private static Color	transparentWhite = new Color(1F, 1F, 1F, 0F);
		private static GUIStyle	style;

		private static double	startTime;
		private static bool		fadeCompleted;

		private static DrawType		drawType;
		private static MessageType	messageType;
		private static Action<Rect>	gui;
		private static Rect			uiRect;
		private static Rect			tooltipRect;
		private static int			hashCode;

		private static GUIContent	content = new GUIContent();
		private static Texture2D	infoIcon;
		private static Texture2D	warningIcon;
		private static Texture2D	errorIcon;

		public static void	LabelOnPrefixField(string tooltip)
		{
			Rect	r = GUILayoutUtility.GetLastRect();
			r.width = EditorGUIUtility.labelWidth;
			TooltipHelper.InternalLabel(r, tooltip, DrawType.Label, MessageType.None);
		}

		public static void	Label(string tooltip)
		{
			TooltipHelper.InternalLabel(GUILayoutUtility.GetLastRect(), tooltip, DrawType.Label, MessageType.None);
		}

		public static void	Label(Rect r, string tooltip)
		{
			TooltipHelper.InternalLabel(r, tooltip, DrawType.Label, MessageType.None);
		}

		public static void	HelpBox(string tooltip, MessageType messageType)
		{
			TooltipHelper.InternalLabel(GUILayoutUtility.GetLastRect(), tooltip, DrawType.Helpbox, messageType);
		}

		public static void	HelpBox(Rect r, string tooltip, MessageType messageType)
		{
			TooltipHelper.InternalLabel(r, tooltip, DrawType.Helpbox, messageType);
		}

		private static void	InternalLabel(Rect r, string tooltip, DrawType drawType, MessageType messageType)
		{
			if (Event.current.rawType == EventType.Repaint)
			{
				EditorGUIUtility.AddCursorRect(r, MouseCursor.Link);

				Color	shapeColor;

				if (messageType == MessageType.Warning)
					shapeColor = Color.yellow;
				else if (messageType == MessageType.Error)
					shapeColor = Color.red;
				else
					shapeColor = Color.cyan;

				TooltipHelper.DrawWitness(r, shapeColor);
			}
			else if (Event.current.rawType == EventType.MouseMove)
			{
				if (r.Contains(Event.current.mousePosition) == true)
				{
					if (TooltipHelper.hashCode != tooltip.GetHashCode())
					{
						Rect	tooltipR = r;
						tooltipR.x += TooltipHelper.XOffset;
						tooltipR.y += TooltipHelper.YOffset;
						TooltipHelper.messageType = messageType;
						TooltipHelper.Set(r, tooltipR, tooltip, drawType);

						if (EditorWindow.mouseOverWindow != null)
							EditorWindow.mouseOverWindow.Repaint();
					}
				}
				else if (TooltipHelper.hashCode != 0 && TooltipHelper.hashCode == tooltip.GetHashCode())
				{
					TooltipHelper.hashCode = 0;

					if (EditorWindow.mouseOverWindow != null)
						EditorWindow.mouseOverWindow.Repaint();
				}
			}
		}

		private static void	DrawWitness(Rect r, Color shapeColor)
		{
			r.x += 1F;
			r.width = 1F;
			r.height = 4F;
			EditorGUI.DrawRect(r, shapeColor);

			r.x += 1F;
			r.height = 3F;
			EditorGUI.DrawRect(r, shapeColor);

			r.x += 1F;
			r.height = 2F;
			EditorGUI.DrawRect(r, shapeColor);

			r.x += 1F;
			r.height = 1F;
			EditorGUI.DrawRect(r, shapeColor);

			//r.x -= 3F;
			//r.y -= 1F;
			//r.width = 5F;
			//r.height = 1F;
			//EditorGUI.DrawRect(r, new Color(shapeColor.r * .8F, shapeColor.g * .8F, shapeColor.b * .8F, 1F));
		}

		public static void	Custom(Action<Rect> gui, float width, float height)
		{
			TooltipHelper.Custom(GUILayoutUtility.GetLastRect(), gui, width, height);
		}

		public static void	Custom(Rect r, Action<Rect> gui, float width, float height)
		{
			if (Event.current.rawType == EventType.Repaint)
				EditorGUIUtility.AddCursorRect(r, MouseCursor.Zoom);
			else if (Event.current.rawType == EventType.MouseMove)
			{
				if (r.Contains(Event.current.mousePosition) == true)
				{
					if (TooltipHelper.hashCode != gui.GetHashCode())
					{
						Rect	tooltipR = r;
						tooltipR.x += TooltipHelper.XOffset;
						tooltipR.y += TooltipHelper.YOffset;
						tooltipR.width = width;
						tooltipR.height = height;
						TooltipHelper.Set(r, tooltipR, gui);
						if (EditorWindow.mouseOverWindow != null)
							EditorWindow.mouseOverWindow.Repaint();
					}
				}
				else if (TooltipHelper.hashCode != 0 && TooltipHelper.hashCode == gui.GetHashCode())
				{
					TooltipHelper.hashCode = 0;

					if (EditorWindow.mouseOverWindow != null)
						EditorWindow.mouseOverWindow.Repaint();
				}
			}
		}

		public static void	PostOnGUI()
		{
			if (TooltipHelper.hashCode != 0)
			{
				if (EditorWindow.mouseOverWindow != null)
				{
					if (TooltipHelper.tooltipRect.x + TooltipHelper.tooltipRect.width > EditorWindow.mouseOverWindow.position.width)
						TooltipHelper.tooltipRect.x = EditorWindow.mouseOverWindow.position.width - TooltipHelper.tooltipRect.width;
					if (TooltipHelper.tooltipRect.y + TooltipHelper.tooltipRect.height > EditorWindow.mouseOverWindow.position.height)
						TooltipHelper.tooltipRect.y = EditorWindow.mouseOverWindow.position.height - TooltipHelper.tooltipRect.height;
				}

				if (Event.current.rawType == EventType.MouseMove)
				{
					if (TooltipHelper.uiRect.Contains(Event.current.mousePosition) == false)
					{
						TooltipHelper.hashCode = 0;

						if (EditorWindow.mouseOverWindow != null)
							EditorWindow.mouseOverWindow.Repaint();
					}
				}

				TooltipHelper.DrawFadeTooltip();
			}
		}

		private static void	DrawFadeTooltip()
		{
			if (TooltipHelper.fadeCompleted == false)
			{
				float	t = (float)(EditorApplication.timeSinceStartup - TooltipHelper.startTime) - TooltipHelper.DelayFadeIn;

				if (t < TooltipHelper.FadeInDuration)
				{
					if (EditorWindow.mouseOverWindow != null)
						EditorWindow.mouseOverWindow.Repaint();
				}
				else
					TooltipHelper.fadeCompleted = true;

				if (t < 0F)
					return;

				using (ColorContentRestorer.Get(t < TooltipHelper.FadeInDuration, Color.Lerp(TooltipHelper.transparentWhite, Color.white, t / TooltipHelper.FadeInDuration)))
					TooltipHelper.DrawTooltip();
			}
			else
				TooltipHelper.DrawTooltip();
		}

		private static void	DrawTooltip()
		{
			if (TooltipHelper.drawType == DrawType.Label)
			{
				if (Event.current.type == EventType.Repaint)
				{
					if (TooltipHelper.style != null)
					{
						tooltipRect.x += 1F;
						tooltipRect.width -= 2F;
						tooltipRect.y += 1F;
						tooltipRect.height -= 2F;
						EditorGUI.DrawRect(tooltipRect, TooltipHelper.helpboxBackgroundColor);
						tooltipRect.x -= 1F;
						tooltipRect.width += 2F;
						tooltipRect.y -= 1F;
						tooltipRect.height += 2F;
					}

					GUI.Label(tooltipRect, TooltipHelper.content, TooltipHelper.style);
				}
			}
			//else if (TooltipHelper.drawType == DrawType.Helpbox)
			//	EditorGUI.HelpBox(r, TooltipHelper.tooltip, TooltipHelper.messageType);
			else if (TooltipHelper.drawType == DrawType.Custom)
				TooltipHelper.gui(tooltipRect);
			else
				throw new Exception("Not implemented DrawType " + TooltipHelper.drawType + ".");
		}

		private static void	Set(Rect r, Rect tooltipR, string tooltip, DrawType drawType)
		{
			TooltipHelper.startTime = EditorApplication.timeSinceStartup;
			TooltipHelper.fadeCompleted = false;

			TooltipHelper.drawType = DrawType.Label;

			TooltipHelper.content.text = tooltip;
			if (drawType == DrawType.Helpbox)
				TooltipHelper.content.image = TooltipHelper.GetIconFromMessageType();
			else
				TooltipHelper.content.image = null;

			if (TooltipHelper.style == null)
				TooltipHelper.style = new GUIStyle(EditorStyles.helpBox);

			Vector2	size = TooltipHelper.style.CalcSize(TooltipHelper.content);
			tooltipR.width = size.x;
			tooltipR.height = size.y;

			TooltipHelper.uiRect = r;
			TooltipHelper.tooltipRect = tooltipR;
			TooltipHelper.hashCode = tooltip.GetHashCode();
		}

		private static void	Set(Rect r, Rect tooltipR, Action<Rect> gui)
		{
			TooltipHelper.startTime = EditorApplication.timeSinceStartup;
			TooltipHelper.fadeCompleted = false;

			TooltipHelper.drawType = DrawType.Custom;
			TooltipHelper.gui = gui;
			TooltipHelper.uiRect = r;
			TooltipHelper.tooltipRect = tooltipR;
			TooltipHelper.hashCode = gui.GetHashCode();
		}

		private static Texture2D	GetIconFromMessageType()
		{
			if (TooltipHelper.messageType == MessageType.Info)
				return UtilityResources.InfoIcon;
			if (TooltipHelper.messageType == MessageType.Warning)
				return UtilityResources.WarningIcon;
			if (TooltipHelper.messageType == MessageType.Error)
				return UtilityResources.ErrorIcon;
			return null;
		}
	}
}