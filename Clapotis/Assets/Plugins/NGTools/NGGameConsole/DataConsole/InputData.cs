using System.Text;

namespace NGTools.NGGameConsole
{
	using UnityEngine;

	public class InputData : DataConsole
	{
		public NGGameConsole	gameConsole;
		public int	maxMouseButtons = 3;

		private bool	requestJoystickNamesUpdate = true;
		
		private string	latestJoystickNames;
		private string	latestMouseButtons;
		
		private string	displayedJoystickNames;
		private string	displayedMouseButtons;

		private ClassInspector	inspectedClass;

		protected virtual void	Awake()
		{
			Debug.Assert(this.gameConsole != null, "FPSCounterData requires field \"Game Console\".", this);

			this.inspectedClass = new ClassInspector();
			this.inspectedClass.ExtractTypeProperties(typeof(Input));
			this.inspectedClass.Construct();

			this.gameConsole.GameConsoleEnableChanged += this.OnGameConsoleEnableChanged;
		}

		protected virtual void	OnDestroy()
		{
			this.gameConsole.GameConsoleEnableChanged -= this.OnGameConsoleEnableChanged;
		}

		public override bool	HasUpdateData()
		{
			return true;
		}

		public override void	UpdateData()
		{
			this.displayedJoystickNames = this.latestJoystickNames;
			this.displayedMouseButtons = this.latestMouseButtons;
		}

		public override void	FullGUI()
		{
			this.inspectedClass.OnGUI();

			GUILayout.Label(this.displayedJoystickNames, this.fullStyle);

			GUILayout.Space(10F);
			GUILayout.Label("Mouse Buttons", this.subTitleStyle);
			GUILayout.Label(this.displayedMouseButtons, this.fullStyle);
		}

		public override string	Copy()
		{
			StringBuilder	buffer = Utility.GetBuffer(this.inspectedClass.Copy());

			buffer.AppendLine(this.latestJoystickNames);
			buffer.AppendLine();
			buffer.AppendLine("Mouse Buttons");
			buffer.Append(this.latestMouseButtons);

			return Utility.ReturnBuffer(buffer);
		}

		protected virtual void	Update()
		{
			StringBuilder	buffer = Utility.GetBuffer();

			if (this.requestJoystickNamesUpdate == true)
			{
				this.requestJoystickNamesUpdate = false;
				this.latestJoystickNames = "Joysticks: " + string.Join(", ", Input.GetJoystickNames());
			}

			for (int i = 0; i < this.maxMouseButtons; i++)
			{
				if (i > 0)
					buffer.AppendLine();

				buffer.Append("MouseButton ");
				buffer.Append(i);
				buffer.Append(": Drag ");
				buffer.Append(Input.GetMouseButton(i));
				buffer.Append(", Down ");
				buffer.Append(Input.GetMouseButtonDown(i));
				buffer.Append(", Up ");
				buffer.Append(Input.GetMouseButtonUp(i));
			}

			this.latestMouseButtons = Utility.ReturnBuffer(buffer);
		}

		private void	OnGameConsoleEnableChanged(bool enable)
		{
			this.enabled = enable;
		}
	}
}