using System;
using UnityEngine;

namespace NGTools.NGGameConsole
{
	public class MemoryData : DataConsole
	{
		public enum ByteSize
		{
			B,
			kB,
			MB,
			GB
		}

		public ByteSize	byteSize = ByteSize.MB;

		private string	fullContent;
		private double	divide;

		protected virtual void	OnEnable()
		{
			if (byteSize == ByteSize.B)
				this.divide = 1D;
			else if (byteSize == ByteSize.kB)
				this.divide = 1000D;
			else if (byteSize == ByteSize.MB)
				this.divide = 1000D * 1000D;
			else if (byteSize == ByteSize.GB)
				this.divide = 1000D * 1000D * 1000D;
		}

		protected virtual void	OnValidate()
		{
			this.OnEnable();
		}

		public override bool	HasUpdateData()
		{
			return true;
		}

		public override void	UpdateData()
		{
			long	totalMemory = GC.GetTotalMemory(false);
			this.fullContent = "Memory : " + totalMemory + " B (" + ((double)totalMemory / this.divide).ToString("#.#") + " " + this.byteSize + ")";
		}

		public override void	ShortGUI()
		{
			this.label.text = ((double)GC.GetTotalMemory(false) / this.divide).ToString("#.#") + " " + this.byteSize;
			this.DrawSimpleShortGUI();
		}

		public override void	FullGUI()
		{
			this.label.text = this.fullContent;
			GUILayout.Label(this.label.text, this.fullStyle);
		}

		public override string	Copy()
		{
			return this.label.text;
		}
	}
}