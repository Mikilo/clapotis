using UnityEngine;

namespace NGTools.NGGameConsole
{
	public class ApplicationData : DataConsole
	{
		[SerializeField, HideInInspector]
		private bool	unusedApplication;

		private ClassInspector	inspectedClass;

		protected virtual void	Awake()
		{
			this.inspectedClass = new ClassInspector();
			this.inspectedClass.ExtractTypeProperties(typeof(Application));
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