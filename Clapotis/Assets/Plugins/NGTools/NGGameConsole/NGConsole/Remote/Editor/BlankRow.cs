﻿using NGToolsEditor.NGConsole;
using System;
using UnityEngine;

namespace NGToolsEditor.NGGameConsole
{
	[Serializable]
	internal sealed class BlankRow : Row
	{
		private float	height;

		public	BlankRow(float height)
		{
			this.height = height;
		}

		public override float	GetWidth()
		{
			return 0F;
		}

		public override float	GetHeight(RowsDrawer rowsDrawer)
		{
			return this.height;
		}

		public override void	DrawRow(RowsDrawer rowsDrawer, Rect r, int i, bool? collapse)
		{
		}
	}
}