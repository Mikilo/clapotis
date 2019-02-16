using UnityEngine;

namespace NGTools.NGGameConsole
{
	public class RenderSettingsData : DataConsole
	{
		[SerializeField, HideInInspector]
		private bool	unusedRenderSettings;

		private ClassInspector	inspectedClass;

		protected virtual void	Awake()
		{
			this.inspectedClass = new ClassInspector();
			this.inspectedClass.ExtractTypeProperties(typeof(RenderSettings));
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