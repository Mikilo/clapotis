using UnityEngine;

namespace NGToolsEditor
{
	public sealed class PointOfInterest
	{
		public float		offset;
		public float		postOffset;
		public Color		color;
		public Texture2D	icon;
		public int			id;

		public override string	ToString()
		{
			return offset + ", " + postOffset + ", " + color + ", " + icon + ", " + id;
		}
	}
}