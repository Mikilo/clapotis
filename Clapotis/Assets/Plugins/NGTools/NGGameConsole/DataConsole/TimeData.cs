using System;
using UnityEngine;

namespace NGTools.NGGameConsole
{
	public class TimeData : DataConsole
	{
		public string	timeFormat = "HH:mm:ss.fff";

		private ClassInspector	inspectedClass;

		protected virtual void	Awake()
		{
			this.inspectedClass = new ClassInspector();
			this.inspectedClass.ExtractTypeProperties(typeof(Time));
			this.inspectedClass.Construct();
		}

		public override bool	HasUpdateData()
		{
			return true;
		}

		public override void	UpdateData()
		{
			this.inspectedClass.UpdatePropertiesValues();
		}

		public override void	ShortGUI()
		{
			this.label.text = DateTime.Now.ToString(this.timeFormat);
			this.DrawSimpleShortGUI();
		}

		public override void	FullGUI()
		{
			this.inspectedClass.OnGUI();
		}

		public override string	Copy()
		{
			return this.inspectedClass.Copy();
		}
	}
}