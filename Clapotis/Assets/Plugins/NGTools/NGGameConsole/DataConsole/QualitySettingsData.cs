using UnityEngine;

namespace NGTools.NGGameConsole
{
	public class QualitySettingsData : DataConsole
	{
		[SerializeField, HideInInspector]
		private bool	unusedQualitySettings;

		private ClassInspector	inspectedClass;
		private string			qualityLevel;
		private bool			applyExpensiveChanges = false;
		private bool			editing = false;

		protected virtual void	Awake()
		{
			this.inspectedClass = new ClassInspector();
			this.inspectedClass.ExtractTypeProperties(typeof(QualitySettings));
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

			GUILayout.BeginHorizontal();
			{
				this.applyExpensiveChanges = GUILayout.Toggle(this.applyExpensiveChanges, "Apply Expensive Changes", GUILayout.ExpandWidth(false));

				if (GUILayout.Button("Increase Level", GUILayout.ExpandWidth(false)) == true)
					QualitySettings.IncreaseLevel(this.applyExpensiveChanges);

				if (GUILayout.Button("Decrease Level", GUILayout.ExpandWidth(false)) == true)
					QualitySettings.DecreaseLevel(this.applyExpensiveChanges);
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Set Quality Level : " + QualitySettings.GetQualityLevel(), this.inspectedClass.textStyle, GUILayout.ExpandWidth(false));
				if (this.editing == false)
				{
					if (GUILayout.Button("Edit", this.inspectedClass.buttonStyle, GUILayout.ExpandWidth(false)) == true)
					{
						this.editing = true;
						this.qualityLevel = QualitySettings.GetQualityLevel().ToString();
					}
				}
				else
				{
					this.qualityLevel = GUILayout.TextField(this.qualityLevel, this.inspectedClass.inputStyle, GUILayout.ExpandWidth(false));

					int	n;

					if (int.TryParse(this.qualityLevel, out n) == false)
						GUILayout.Label("Must be an integer.");
					else
					{
						if (GUILayout.Button("Set", this.inspectedClass.buttonStyle, GUILayout.ExpandWidth(false)) == true)
						{
							QualitySettings.SetQualityLevel(n, this.applyExpensiveChanges);
							this.editing = false;
						}
					}
				}
			}
			GUILayout.EndHorizontal();
		}

		public override string	Copy()
		{
			return this.inspectedClass.Copy();
		}
	}
}