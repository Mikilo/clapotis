using System;
using System.Text;
using UnityEngine;

namespace NGTools.NGGameConsole
{
	public class FPSCountersData : DataConsole
	{
		public const float	RefreshPeriod = 0.5f;

		public NGGameConsole	gameConsole;
		public bool	displayUpdate = true;
		public bool	displayFixedUpdate = true;
		public bool	displayOnGUI = true;

		private float	nextTime;
		private int		updateCount;
		private int		fixedUpdateCount;
		private int		onGUICount;
		private int		updatePerSecond;
		private int		fixedUpdatePerSecond;
		private int		onGUIPerSecond;

		private bool	hasNewData = false;
		private string	fullString = string.Empty;

		protected virtual void	Awake()
		{
			Debug.Assert(this.gameConsole != null, "FPSCounterData requires field \"Game Console\".", this);
			this.gameConsole.GameConsoleEnableChanged += this.OnGameConsoleEnableChanged;
		}

		protected virtual void	Start()
		{
			this.nextTime = Time.realtimeSinceStartup + FPSCountersData.RefreshPeriod;
		}

		protected virtual void	OnDestroy()
		{
			this.gameConsole.GameConsoleEnableChanged -= this.OnGameConsoleEnableChanged;
		}

		protected virtual void	OnGUI()
		{
			++this.onGUICount;
		}

		protected virtual void	Update()
		{
			if (this.enabled != this.gameConsole.enabled)
				this.enabled = this.gameConsole.enabled;

			++this.updateCount;

			if (this.nextTime <= Time.realtimeSinceStartup)
			{
				this.nextTime += FPSCountersData.RefreshPeriod;

				this.updatePerSecond = (int)(this.updateCount / FPSCountersData.RefreshPeriod);
				this.fixedUpdatePerSecond = (int)(this.fixedUpdateCount / FPSCountersData.RefreshPeriod);
				this.onGUIPerSecond = (int)(this.onGUICount / FPSCountersData.RefreshPeriod);
				this.updateCount = 0;
				this.fixedUpdateCount = 0;
				this.onGUICount = 0;
				this.hasNewData = true;
			}
		}

		protected virtual void	FixedUpdate()
		{
			++this.fixedUpdateCount;
		}

		public override bool	HasUpdateData()
		{
			return true;
		}

		public override void	UpdateData()
		{
			if (this.hasNewData == true)
			{
				this.hasNewData = false;

				StringBuilder	buffer = Utility.GetBuffer();

				if (this.displayUpdate == true)
					buffer.Append(this.updatePerSecond + " Update/s ");
				if (this.displayFixedUpdate == true)
					buffer.Append(this.fixedUpdatePerSecond + " FixedUpdate/s ");
				if (this.displayOnGUI == true)
					buffer.Append(this.onGUIPerSecond + " OnGUI/s ");
				if (buffer.Length > 0)
					buffer.Length -= 1;
				this.label.text = buffer.ToString();

				buffer.Length = 0;
				if (this.displayUpdate == true)
					buffer.AppendLine("Update : " + this.updatePerSecond + " per second");
				if (this.displayFixedUpdate == true)
					buffer.AppendLine("Fixed Update : " + this.fixedUpdatePerSecond + " per second");
				if (this.displayOnGUI == true)
					buffer.AppendLine("OnGUI : " + this.onGUIPerSecond + " per second");
				if (buffer.Length > 0)
					buffer.Length -= Environment.NewLine.Length;
				this.fullString = Utility.ReturnBuffer(buffer);
			}
		}

		public override void	ShortGUI()
		{
			this.DrawSimpleShortGUI();
		}

		public override void	FullGUI()
		{
			GUILayout.Label(this.fullString, this.fullStyle);
		}

		public override string	Copy()
		{
			return this.fullString;
		}

		private void	OnGameConsoleEnableChanged(bool enable)
		{
			this.enabled = enable;
		}
	}
}