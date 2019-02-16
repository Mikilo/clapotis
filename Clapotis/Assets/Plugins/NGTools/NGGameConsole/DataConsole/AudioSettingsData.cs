using UnityEngine;

namespace NGTools.NGGameConsole
{
	public class AudioSettingsData : DataConsole
	{
		[SerializeField, HideInInspector]
		private bool	unusedAudioSettings;

		private ClassInspector	inspectedClass;

		protected virtual void	Awake()
		{
			this.inspectedClass = new ClassInspector();
			this.inspectedClass.ExtractTypeProperties(typeof(AudioSettings));
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