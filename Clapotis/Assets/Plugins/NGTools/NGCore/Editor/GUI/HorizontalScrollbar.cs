using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor
{
	public sealed class HorizontalScrollbar
	{
		public const float	MinSize = 10F;

		public float	innerMargin;
		public float	speedScroll = 10F;

		private float	realWidth;
		public float	RealWidth
		{
			get
			{
				return this.realWidth;
			}
			set
			{
				if (this.realWidth != value)
				{
					this.realWidth = value;
					this.scrollWidth = this.bgScrollRect.width * this.bgScrollRect.width / this.realWidth;
					if (this.scrollWidth < VerticalScrollbar.MinSize)
						this.scrollWidth = VerticalScrollbar.MinSize;
					this.UpdateOffset();
				}
			}
		}

		private float	offset;
		public float	Offset
		{
			get
			{
				return this.offset;
			}
			set
			{
				if (this.offset != value)
				{
					this.offset = value;
					this.UpdateScrollX();
				}
			}
		}

		public float	MaxHeight
		{
			get
			{
				return (this.bgScrollRect.width < this.RealWidth) ? this.bgScrollRect.height : 0F;
			}
		}
		public float	MaxWidth { get { return this.bgScrollRect.width; } }

		public bool		interceiptEvent = true;

		public bool	hasCustomArea = false;
		public Rect	allowedMouseArea;

		public float	InterestSizeMargin
		{
			get
			{
				return this.interestSizeMargin;
			}
			set
			{
				this.interestSizeMargin = value;
				this.interestHalfSizeMargin = this.interestSizeMargin * .5F;
			}
		}
		private float	interestSizeMargin = 4F;
		private float	interestHalfSizeMargin = 2F;

		private bool	drawBackgroundColor;
		public bool		DrawBackgroundColor { get { return this.drawBackgroundColor; } set { drawBackgroundColor = value; } }

		private float	scrollX;
		private float	scrollWidth;
		private Rect	bgScrollRect;
		private float	onDownXOffset;

		private List<ListPointOfInterest>	pointsOfInterest = new List<ListPointOfInterest>() { new ListPointOfInterest(0) };

		// Cache variable
		private Rect	scrollRect = default(Rect);
		private Rect	cumulativeRect = default(Rect);
		private Color	cumulativeColor = default(Color);
		private Color	background;
		private Color	focused;
		private Color	idle;
		private Color	currentBackgroundColor;

		public	HorizontalScrollbar(float x, float y, float width) : this(x, y, width, 15F, 4F)
		{
		}

		public	HorizontalScrollbar(float x, float y, float width, float height) : this(x, y, width, height, 4F)
		{
		}

		public	HorizontalScrollbar(float x, float y, float width, float height, float innerMargin)
		{
			this.innerMargin = innerMargin;

			this.background = EditorGUIUtility.isProSkin == true ? new Color(45F / 255F, 45F / 255F, 45F / 255F) : new Color(.7F, .7F, .7F);
			this.idle = EditorGUIUtility.isProSkin == true ? new Color(.6F, .6F, .6F) : new Color(.4F, .4F, .4F);
			this.focused = EditorGUIUtility.isProSkin == true ? new Color(.7F, .7F, .7F) : new Color(.3F, .3F, .3F);
			this.currentBackgroundColor = this.idle;

			this.bgScrollRect = new Rect(x, y, width, height);
		}

		/// <summary>
		/// Draws the scrollbar and update offsets.
		/// </summary>
		public void	OnGUI()
		{
			// Toggle scrollbar
			if (Event.current.type == EventType.Repaint)
			{
				if (this.drawBackgroundColor == true)
					EditorGUI.DrawRect(this.bgScrollRect, this.background);

				if (this.bgScrollRect.width >= this.realWidth)
				{
					this.Offset = 0F;
					this.DrawInterest();
					return;
				}
			}

			if (Event.current.type != EventType.Repaint &&
				this.interceiptEvent == false &&
				bgScrollRect.Contains(Event.current.mousePosition) == false &&
				(this.hasCustomArea == false ||
				 this.allowedMouseArea.Contains(Event.current.mousePosition) == false) &&
				 onDownXOffset == -1F)
			{
				return;
			}

			if (this.bgScrollRect.width >= this.realWidth)
				return;

			switch (Event.current.type)
			{
				case EventType.ScrollWheel:
					this.scrollX += Event.current.delta.y * this.speedScroll * this.bgScrollRect.width / this.realWidth;
					this.UpdateOffset();
					HandleUtility.Repaint();
					Event.current.Use();
					break;

				case EventType.MouseDrag:
					if (this.onDownXOffset > 0F)
					{
						this.scrollX = Event.current.mousePosition.x - this.onDownXOffset;
						this.UpdateOffset();
						HandleUtility.Repaint();
					}
					break;

				case EventType.MouseDown:
					if (this.bgScrollRect.Contains(Event.current.mousePosition) == true)
					{
						if (Event.current.mousePosition.x >= this.bgScrollRect.x + this.scrollX &&
							Event.current.mousePosition.x < this.bgScrollRect.x + this.scrollX + this.scrollWidth)
						{
							this.onDownXOffset = Event.current.mousePosition.x - this.scrollX;
						}
						else
						{
							this.onDownXOffset = this.bgScrollRect.x + this.scrollWidth * .5F;
							this.scrollX = Event.current.mousePosition.y - this.onDownXOffset;
							this.UpdateOffset();
						}

						this.currentBackgroundColor = this.focused;
						HandleUtility.Repaint();
						Event.current.Use();
					}
					break;

				case EventType.MouseMove:
				case EventType.MouseUp:
					this.currentBackgroundColor = this.idle;
					this.onDownXOffset = -1;
					HandleUtility.Repaint();
					break;

				default:
					break;
			}

			this.scrollRect.x = this.bgScrollRect.x + this.scrollX;
			this.scrollRect.y = this.bgScrollRect.y + this.innerMargin;
			this.scrollRect.width = this.scrollWidth;
			this.scrollRect.height = this.bgScrollRect.height - this.innerMargin - this.innerMargin;
			EditorGUI.DrawRect(this.scrollRect, this.currentBackgroundColor);

			this.DrawInterest();
		}

		private void	DrawInterest()
		{
			float	min = this.bgScrollRect.x - this.interestHalfSizeMargin;

			this.scrollRect.y = this.bgScrollRect.y + this.bgScrollRect.height * .5F - this.interestHalfSizeMargin;
			this.scrollRect.width = this.InterestSizeMargin;
			this.scrollRect.height = this.InterestSizeMargin;

			if (this.bgScrollRect.height <= this.realWidth)
			{
				float	factor = this.bgScrollRect.width / this.realWidth;
				bool	first = false;

				if (this.bgScrollRect.width * 4F <= this.realWidth)
					this.scrollRect.width = 16F * factor;
				min = this.bgScrollRect.x - factor;

				this.cumulativeRect.height = this.interestSizeMargin;
				this.cumulativeColor.a = 1F;

				for (int i = this.pointsOfInterest.Count - 1; i >= 0; --i)
				{
					ListPointOfInterest	list = this.pointsOfInterest[i];
					double				nextXMin = min;
					double				xMin = 0F;
					double				xMax = 0F;

					first = false;

					if (list.offset != 0F)
						this.cumulativeRect.y = this.scrollRect.y + list.offset;
					else
						this.cumulativeRect.y = this.scrollRect.y;

					this.cumulativeRect.xMax = 0F;

					for (int j = 0, max = list.Count; j < max; j++)
					{
						PointOfInterest	poi = list[j];

						nextXMin += poi.offset * factor;

						if (xMax >= nextXMin &&
							this.cumulativeColor.r == poi.color.r &&
							this.cumulativeColor.g == poi.color.g &&
							this.cumulativeColor.b == poi.color.b &&
							this.cumulativeColor.a == poi.color.a)
						{
							xMax = nextXMin + this.scrollRect.width;
						}
						else
						{
							if (first == true)
							{
								this.cumulativeRect.x = (float)xMin;
								this.cumulativeRect.width = (float)(xMax - xMin);
								if (this.cumulativeRect.width < 3F)
								{
									this.cumulativeRect.x -= 1.5F;
									this.cumulativeRect.width = 3F;
								}

								if (this.cumulativeColor.a > 0F)
								{
									this.cumulativeColor.a = 1F;
									EditorGUI.DrawRect(this.cumulativeRect, this.cumulativeColor);
								}
							}

							first = true;

							xMin = nextXMin;
							xMax = nextXMin + this.scrollRect.height;
							this.cumulativeColor.r = poi.color.r;
							this.cumulativeColor.g = poi.color.g;
							this.cumulativeColor.b = poi.color.b;
							this.cumulativeColor.a = poi.color.a;
						}

						if (Mathf.Approximately(poi.postOffset, 0F) == false)
							nextXMin += poi.postOffset * factor;
					}

					if (first == true)
					{
						this.cumulativeRect.x = (float)xMin;
						this.cumulativeRect.width = (float)(xMax - xMin);
						if (this.cumulativeRect.width < 3F)
						{
							this.cumulativeRect.x -= 1.5F;
							this.cumulativeRect.width = 3F;
						}

						if (this.cumulativeColor.a > 0F)
						{
							this.cumulativeColor.a = 1F;
							EditorGUI.DrawRect(this.cumulativeRect, this.cumulativeColor);
						}
					}
				}
			}
			else
			{
				//for (int i = this.pointsOfInterest.Count - 1; i >= 0; --i)
				//{
				//	this.scrollRect.y += this.pointsOfInterest[i].offset;
				//	for (int j = 0; j < this.pointsOfInterest[i].Count; j++)
				//	{
				//		this.scrollRect.x = min + this.pointsOfInterest[i][j].offset;
				//		EditorGUI.DrawRect(this.scrollRect, this.pointsOfInterest[i][j].color);
				//	}
				//	this.scrollRect.y -= this.pointsOfInterest[i].offset;
				//}
			}
		}

		public void	SetPosition(float x, float y)
		{
			if (Event.current.type != EventType.Layout &&
				(this.bgScrollRect.x != x ||
				 this.bgScrollRect.y != y))
			{
				this.bgScrollRect.x = x;
				this.bgScrollRect.y = y;
			}
		}

		public void	SetSize(float width)
		{
			if (Event.current.type != EventType.Layout &&
				this.bgScrollRect.width != width)
			{
				this.bgScrollRect.width = width;
				// Update width, function of the max content width
				this.scrollWidth = this.bgScrollRect.width * this.bgScrollRect.width / this.realWidth;
				if (this.scrollWidth < VerticalScrollbar.MinSize)
					this.scrollWidth = VerticalScrollbar.MinSize;
				this.UpdateScrollX();
			}
		}

		public IEnumerable<ListPointOfInterest>	EachListInterests()
		{
			for (int i = 0; i < this.pointsOfInterest.Count; i++)
			{
				yield return this.pointsOfInterest[i];
			}
		}

		public void	AddListInterests(ListPointOfInterest list)
		{
			for (int i = 1; i < this.pointsOfInterest.Count; i++)
			{
				if (list.priority <= this.pointsOfInterest[i].priority)
				{
					this.pointsOfInterest.Insert(i, list);
					return;
				}
			}

			this.pointsOfInterest.Add(list);
		}

		public void	DeleteListInterests(ListPointOfInterest list)
		{
			for (int i = 0; i < this.pointsOfInterest.Count; i++)
			{
				if (list == this.pointsOfInterest[i])
				{
					this.pointsOfInterest.RemoveAt(i);
					break;
				}
			}
		}

		public void	AddInterest(float absoluteOffset, Color color, int id = -1)
		{
			this.pointsOfInterest[0].Add(absoluteOffset, color, id);
		}

		public void	ClearInterests()
		{
			this.pointsOfInterest[0].Clear();
		}

		public void	ClearAllInterests()
		{
			for (int i = 0; i < this.pointsOfInterest.Count; i++)
				this.pointsOfInterest[i].Clear();
		}

		private void	UpdateOffset()
		{
			if (this.scrollX < 0F)
				this.scrollX = 0F;
			else if (this.scrollX + this.scrollWidth > this.bgScrollRect.width)
				this.scrollX = this.bgScrollRect.width - this.scrollWidth;

			if (this.scrollX <= 0F)
				this.offset = 0F;
			else
				this.offset = (this.scrollX / (this.bgScrollRect.width - this.scrollWidth)) *
							  (this.realWidth - this.bgScrollRect.width);
		}

		private void	UpdateScrollX()
		{
			this.scrollX = (this.offset * (this.bgScrollRect.height - this.scrollWidth)) /
						   (this.realWidth - this.bgScrollRect.height);
		}
	}
}