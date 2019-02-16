using NGTools;
using System;
using System.Reflection;
using UnityEngine;

namespace NGToolsEditor
{
	[Serializable]
	public class GUIStyleOverride
	{
		[Serializable]
		public class GUIStyleStateProxy
		{
			public Texture2D	background;
			public Texture2D[]	scaledBackgrounds;
			public Color		textColor;
		}

		[Flags]
		public enum Overrides
		{
			Normal = 2 << 0,
			Hover = 2 << 1,
			Active = 2 << 2,
			Focused = 2 << 3,
			OnNormal = 2 << 4,
			OnHover = 2 << 5,
			OnActive = 2 << 6,
			OnFocused = 2 << 7,
			Border = 2 << 8,
			Margin = 2 << 9,
			Padding = 2 << 10,
			Overflow = 2 << 11,
			Font = 2 << 12,
			FontSize = 2 << 13,
			FontStyle = 2 << 14,
			Alignment = 2 << 15,
			WordWrap = 2 << 16,
			RichText = 2 << 17,
			Clipping = 2 << 18,
			ImagePosition = 2 << 19,
			FixedWidth = 2 << 20,
			FixedHeight = 2 << 21,
			StretchWidth = 2 << 22,
			StretchHeight = 2 << 23,
			ContentOffset = 2 << 24
		}

		public int					overrideMask;
		public string				baseStyleName;
		public GUIStyleStateProxy	normal;
		public GUIStyleStateProxy	hover;
		public GUIStyleStateProxy	active;
		public GUIStyleStateProxy	focused;
		public GUIStyleStateProxy	onNormal;
		public GUIStyleStateProxy	onHover;
		public GUIStyleStateProxy	onActive;
		public GUIStyleStateProxy	onFocused;
		public RectOffset			border;
		public RectOffset			margin;
		public RectOffset			padding;
		public RectOffset			overflow;
		public Font					font;
		public int					fontSize;
		public FontStyle			fontStyle;
		public TextAnchor			alignment;
		public bool					wordWrap;
		public bool					richText;
		public TextClipping			clipping;
		public ImagePosition		imagePosition;
		public float				fixedWidth;
		public float				fixedHeight;
		public bool					stretchWidth;
		public bool					stretchHeight;
		public Vector2				contentOffset;

		private GUIStyle	style;

		public void	ResetStyle()
		{
			this.style = null;
		}

		public GUIStyle	GetStyle()
		{
			if (this.style == null)
			{
				Type		type = this.GetType();
				FieldInfo[]	fields = type.GetFields();

				this.style = GUI.skin.FindStyle(this.baseStyleName);

				if (this.style != null)
					this.style = new GUIStyle(this.style);
				else
					this.style = new GUIStyle();

				for (int i = 0; i < fields.Length; i++)
				{
					if (fields[i].Name != "overrideMask" &&
						fields[i].Name != "baseStyleName")
					{
						if ((this.overrideMask & (2 << (i - 2))) != 0)
						{
							PropertyInfo	property = typeof(GUIStyle).GetProperty(fields[i].Name);

							if (property != null)
							{
								if (fields[i].FieldType == typeof(GUIStyleStateProxy))
								{
									GUIStyleStateProxy	a = fields[i].GetValue(this) as GUIStyleStateProxy;
									GUIStyleState		b = property.GetValue(this.style, null) as GUIStyleState;

									b.background = a.background;
									b.scaledBackgrounds = a.scaledBackgrounds;
									b.textColor = a.textColor;
								}
								else
									property.SetValue(this.style, fields[i].GetValue(this) ?? Activator.CreateInstance(fields[i].FieldType));
							}
						}
					}
				}
			}

			return this.style;
		}

		public void	CopyTo(GUIStyleOverride style)
		{
			style.style = this.style;
			style.overrideMask = this.overrideMask;
			style.baseStyleName = this.baseStyleName;
			style.normal = this.normal;
			style.hover = this.hover;
			style.active = this.active;
			style.focused = this.focused;
			style.onNormal = this.onNormal;
			style.onHover = this.onHover;
			style.onActive = this.onActive;
			style.onFocused = this.onFocused;
			style.border = this.border;
			style.margin = this.margin;
			style.padding = this.padding;
			style.overflow = this.overflow;
			style.font = this.font;
			style.fontSize = this.fontSize;
			style.fontStyle = this.fontStyle;
			style.alignment = this.alignment;
			style.wordWrap = this.wordWrap;
			style.richText = this.richText;
			style.clipping = this.clipping;
			style.imagePosition = this.imagePosition;
			style.fixedWidth = this.fixedWidth;
			style.fixedHeight = this.fixedHeight;
			style.stretchWidth = this.stretchWidth;
			style.stretchHeight = this.stretchHeight;
			style.contentOffset = this.contentOffset;
		}
	}
}