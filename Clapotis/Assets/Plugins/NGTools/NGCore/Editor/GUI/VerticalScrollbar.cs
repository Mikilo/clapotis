using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor
{
	public sealed class VerticalScrollbar
	{
		public const float	MinSize = 10F;

		public float	innerMargin;
		public float	speedScroll = 10F;

		private float	realHeight;
		public float	RealHeight
		{
			get
			{
				return this.realHeight;
			}
			set
			{
				if (this.realHeight != value)
				{
					this.realHeight = value;

					if (this.realHeight > 0F)
						this.scrollHeight = this.bgScrollRect.height * this.bgScrollRect.height / this.realHeight;
					if (this.scrollHeight < VerticalScrollbar.MinSize)
						this.scrollHeight = VerticalScrollbar.MinSize;

					this.UpdateScrollY();
					this.UpdateOffset();
				}
			}
		}

		public event Action<float>	OffsetChanged;

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
					this.UpdateScrollY();
					this.UpdateOffset();
				}
			}
		}

		public float	MaxWidth
		{
			get
			{
				return (this.bgScrollRect.height < this.RealHeight) ? this.bgScrollRect.width : 0F;
			}
		}
		public float	MaxHeight { get { return this.bgScrollRect.height; } }

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

		private float	scrollY;
		private float	scrollHeight;
		private Rect	bgScrollRect;
		private float	onDownYOffset;

		private List<ListPointOfInterest>	pointsOfInterest = new List<ListPointOfInterest>() { new ListPointOfInterest(0) };

		// Cache variable
		private Rect	scrollRect = default(Rect);
		private Rect	cumulativeRect = default(Rect);
		private Color	cumulativeColor = default(Color);
		private Color	background;
		private Color	focused;
		private Color	idle;
		private Color	currentBackgroundColor;

		public	VerticalScrollbar(float x, float y, float height) : this(x, y, height, 15F, 4F)
		{
		}

		public	VerticalScrollbar(float x, float y, float height, float width) : this(x, y, height, width, 4F)
		{
		}

		public	VerticalScrollbar(float x, float y, float height, float width, float innerMargin)
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

				if (this.bgScrollRect.height > this.realHeight)
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
				 onDownYOffset == -1F)
			{
				return;
			}

			if (this.bgScrollRect.height >= this.realHeight)
				return;

			switch (Event.current.type)
			{
				case EventType.ScrollWheel:
					this.scrollY += Event.current.delta.y * this.speedScroll * this.bgScrollRect.height / this.realHeight;
					this.UpdateOffset();
					HandleUtility.Repaint();
					Event.current.Use();
					break;

				case EventType.MouseDrag:
					if (this.onDownYOffset > 0F)
					{
						this.scrollY = Event.current.mousePosition.y - this.onDownYOffset;
						this.UpdateOffset();
						HandleUtility.Repaint();
					}
					break;

				case EventType.MouseDown:
					if (this.bgScrollRect.Contains(Event.current.mousePosition) == true)
					{
						if (Event.current.mousePosition.y >= this.bgScrollRect.y + this.scrollY &&
							Event.current.mousePosition.y < this.bgScrollRect.y + this.scrollY + this.scrollHeight)
						{
							this.onDownYOffset = Event.current.mousePosition.y - this.scrollY;
						}
						else
						{
							this.onDownYOffset = this.bgScrollRect.y + this.scrollHeight * .5F;
							this.scrollY = Event.current.mousePosition.y - this.onDownYOffset;
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
					this.onDownYOffset = -1F;
					HandleUtility.Repaint();
					break;

				default:
					break;
			}

			this.scrollRect.x = this.bgScrollRect.x + this.innerMargin;
			this.scrollRect.y = this.bgScrollRect.y + this.scrollY;
			this.scrollRect.width = this.bgScrollRect.width - this.innerMargin - this.innerMargin;
			this.scrollRect.height = this.scrollHeight;
			EditorGUI.DrawRect(this.scrollRect, this.currentBackgroundColor);

			this.DrawInterest();
		}
 
		private void	DrawInterest()
		{
			float	min = this.bgScrollRect.y - this.interestHalfSizeMargin;

			this.scrollRect.x = this.bgScrollRect.x + this.bgScrollRect.width * .5F - this.interestHalfSizeMargin;
			this.scrollRect.width = this.interestSizeMargin;
			this.scrollRect.height = this.interestSizeMargin;

			if (this.bgScrollRect.height <= this.realHeight)
			{
				float	factor = this.bgScrollRect.height / this.realHeight;
				bool	first = false;

				if (this.bgScrollRect.height * 4F <= this.realHeight)
					this.scrollRect.height = 16F * factor;
				min = this.bgScrollRect.y - factor;

				this.cumulativeRect.width = this.interestSizeMargin;
				this.cumulativeColor.a = 1F;

				for (int i = this.pointsOfInterest.Count - 1; i >= 0; --i)
				{
					ListPointOfInterest	list = this.pointsOfInterest[i];
					double				nextYMin = min;
					double				yMin = 0F;
					double				yMax = 0F;

					first = false;

					if (list.offset != 0F)
						this.cumulativeRect.x = this.scrollRect.x + list.offset;
					else
						this.cumulativeRect.x = this.scrollRect.x;

					this.cumulativeRect.yMax = 0F;

					for (int j = 0, max = list.Count; j < max; j++)
					{
						PointOfInterest	poi = list[j];

						nextYMin += poi.offset * factor;

						if (poi.icon != null)
						{
							this.cumulativeRect.y = (float)nextYMin;
							this.cumulativeRect.height = this.cumulativeRect.width;
							GUI.DrawTexture(this.cumulativeRect, poi.icon, ScaleMode.ScaleToFit);
							continue;
						}

						if (yMax >= nextYMin &&
							this.cumulativeColor.r == poi.color.r &&
							this.cumulativeColor.g == poi.color.g &&
							this.cumulativeColor.b == poi.color.b &&
							this.cumulativeColor.a == poi.color.a)
						{
							yMax = nextYMin + this.scrollRect.height;
						}
						else
						{
							if (first == true)
							{
								this.cumulativeRect.y = (float)yMin;
								this.cumulativeRect.height = (float)(yMax - yMin);
								if (this.cumulativeRect.height < 3F)
								{
									this.cumulativeRect.y -= 1.5F;
									this.cumulativeRect.height = 3F;
								}

								if (this.cumulativeColor.a > 0F)
								{
									this.cumulativeColor.a = 1F;
									EditorGUI.DrawRect(this.cumulativeRect, this.cumulativeColor);
								}
							}

							first = true;

							yMin = nextYMin;
							yMax = nextYMin + this.scrollRect.height;
							this.cumulativeColor.r = poi.color.r;
							this.cumulativeColor.g = poi.color.g;
							this.cumulativeColor.b = poi.color.b;
							this.cumulativeColor.a = poi.color.a;
						}

						if (Mathf.Approximately(poi.postOffset, 0F) == false)
							nextYMin += poi.postOffset * factor;
					}

					if (first == true)
					{
						this.cumulativeRect.y = (float)yMin;
						this.cumulativeRect.height = (float)(yMax - yMin);
						if (this.cumulativeRect.height < 3F)
						{
							this.cumulativeRect.y -= 1.5F;
							this.cumulativeRect.height = 3F;
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
				//	this.scrollRect.x += this.pointsOfInterest[i].offset;
				//	for (int j = 0; j < this.pointsOfInterest[i].Count; j++)
				//	{
				//		this.scrollRect.y = min + this.pointsOfInterest[i][j].offset;
				//		EditorGUI.DrawRect(this.scrollRect, this.pointsOfInterest[i][j].color);
				//	}
				//	this.scrollRect.x -= this.pointsOfInterest[i].offset;
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

		public void	SetSize(float height)
		{
			if (Event.current.type != EventType.Layout &&
				Mathf.Approximately(this.bgScrollRect.height, height) == false)
			{
				this.bgScrollRect.height = height;
				// Update height, function of the max content 
				if (this.realHeight > 0F)
					this.scrollHeight = this.bgScrollRect.height * this.bgScrollRect.height / this.realHeight;
				else
					this.scrollHeight = height;
				if (this.scrollHeight < VerticalScrollbar.MinSize)
					this.scrollHeight = VerticalScrollbar.MinSize;
				this.UpdateScrollY();
				this.UpdateOffset();
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
			if (this.scrollY < 0F)
				this.scrollY = 0F;
			else if (this.scrollY + this.scrollHeight > this.bgScrollRect.height)
				this.scrollY = this.bgScrollRect.height - this.scrollHeight;

			float	lastOffset = this.offset;

			if (this.scrollY <= 0F)
				this.offset = 0F;
			else
				this.offset = (this.scrollY / (this.bgScrollRect.height - this.scrollHeight)) *
							  (this.realHeight - this.bgScrollRect.height);

			if (Mathf.Approximately(lastOffset, this.offset) == false && this.OffsetChanged != null)
				this.OffsetChanged(this.offset);
		}

		private void	UpdateScrollY()
		{
			float	diffHeight = (this.realHeight - this.bgScrollRect.height);

			if (Mathf.Approximately(0F, diffHeight) == false)
				this.scrollY = (this.offset * (this.bgScrollRect.height - this.scrollHeight)) / diffHeight;
			else
				this.scrollY = 0F;
		}
	}
}