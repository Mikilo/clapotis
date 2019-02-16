using System.Collections.Generic;
using UnityEngine;

namespace NGToolsEditor
{
	public sealed class ListPointOfInterest
	{
		public int	Count { get { return this.list.Count; } }

		public PointOfInterest	this[int index]
		{
			get
			{
				return this.list[index];
			}
		}

		public int		priority;
		public float	offset = 0F;

		private	float					lastOffset = 0F;
		private List<PointOfInterest>	list = new List<PointOfInterest>();

		public	ListPointOfInterest(int priority)
		{
			this.priority = priority;
		}

		public void	Add(float absoluteOffset, Texture2D icon, int id)
		{
			float	deltaOffset = absoluteOffset;

			if (this.list.Count > 0)
				deltaOffset = deltaOffset - lastOffset;

			lastOffset += deltaOffset;
			this.list.Add(new PointOfInterest() { offset = deltaOffset, icon = icon, id = id });
		}

		public void	Add(float absoluteOffset, Color color, int id)
		{
			float	deltaOffset = absoluteOffset;

			if (this.list.Count > 0)
				deltaOffset = deltaOffset - lastOffset;

			lastOffset += deltaOffset;
			this.list.Add(new PointOfInterest() { offset = deltaOffset, color = color, id = id });
		}

		public void	InsertOffset(float yOffset, float deltaOffset, int id = -1)
		{
			if (id != -1)
			{
				for (int i = 0; i < this.list.Count; i++)
				{
					if (this.list[i].id == id)
					{
						this.list[i].postOffset += deltaOffset;
						if (Mathf.Approximately(this.list[i].offset + this.list[i].postOffset, 0F) == true)
							this.list.RemoveAt(i);

						lastOffset += deltaOffset;
						return;
					}
				}
			}

			float	interestOffset = 0F;

			lastOffset += deltaOffset;

			if (this.list.Count > 0)
			{
				if (this.list[0].offset + this.list[0].postOffset > yOffset)
				{
					if (Mathf.Approximately(this.list[0].postOffset, -deltaOffset) == true)
						this.list.RemoveAt(0);
					else
						this.list.Insert(0, new PointOfInterest() { postOffset = deltaOffset, id = id });
					return;
				}
				else
				{
					for (int j = 0; j < this.list.Count; j++)
					{
						interestOffset += this.list[j].offset + this.list[j].postOffset;
						if (interestOffset > yOffset)
						{
							if (this.list[j].id == -1)
							{
								if (Mathf.Approximately(this.list[j].postOffset, -deltaOffset) == true)
									this.list.RemoveAt(j);
								else if (j + 1 < this.list.Count && Mathf.Approximately(this.list[j + 1].postOffset, -deltaOffset) == true)
									this.list.RemoveAt(j + 1);
							}
							else
								this.list.Insert(j, new PointOfInterest() { postOffset = deltaOffset, id = id });
							return;
						}
					}
				}
			}

			this.list.Add(new PointOfInterest() { postOffset = deltaOffset, id = id });
		}

		public void	Clear()
		{
			this.list.Clear();
			this.lastOffset = 0F;
		}

		public bool	RemoveId(int id, float height)
		{
			for (int i = 0; i < this.list.Count; i++)
			{
				if (this.list[i].id == id)
				{
					lastOffset -= this.list[i].offset + this.list[i].postOffset;

					// Adjust the offset regarding the height.
					this.list[i].postOffset = this.list[i].offset + this.list[i].postOffset - height;

					// Delete it if it was just a "shifter".
					if (Mathf.Approximately(this.list[i].postOffset, 0F) == true)
						this.list.RemoveAt(i);
					else
					{
						this.list[i].offset = 0F;
						this.list[i].color = default(Color);
						this.list[i].id = -1;
					}
					return true;
				}
			}

			return false;
		}
	}
}