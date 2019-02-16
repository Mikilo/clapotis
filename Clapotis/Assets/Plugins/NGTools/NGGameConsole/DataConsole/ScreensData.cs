using System;
using System.Text;
using UnityEngine;

namespace NGTools.NGGameConsole
{
	public class ScreensData : DataConsole
	{
		[SerializeField, HideInInspector]
		private bool	unusedScreens;

		private ClassInspector	inspectedClass;

		protected virtual void	Awake()
		{
			this.inspectedClass = new ClassInspector();
			this.inspectedClass.ExtractTypeProperties(typeof(Screen));
			this.inspectedClass.ExtractTypeProperties(typeof(Cursor));
			this.inspectedClass.ExtractTypeFields(typeof(Display));
			this.inspectedClass.AddDrawer(typeof(Display[]), this.DrawDisplay, this.CopyDisplay);
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

		private void	DrawDisplay(object instance, string prefix)
		{
			Display[]	displays = instance as Display[];

			for (int i = 0; i < displays.Length; i++)
			{
				GUILayout.Label("Display " + i + (Display.main == displays[i] ? " (main)" : string.Empty), this.inspectedClass.textStyle, GUILayout.ExpandWidth(false));
				GUILayout.Label("  Rendering: " + displays[i].renderingWidth + " x " + displays[i].renderingHeight, this.inspectedClass.textStyle, GUILayout.ExpandWidth(false));
				GUILayout.Label("  System: " + displays[i].systemWidth + " x " + displays[i].systemHeight, this.inspectedClass.textStyle, GUILayout.ExpandWidth(false));
			}
		}

		private string	CopyDisplay(object instance, string prefix)
		{
			StringBuilder	buffer = Utility.GetBuffer();
			Display[]		displays = instance as Display[];

			for (int i = 0; i < displays.Length; i++)
			{
				buffer.AppendLine("Display " + i + (Display.main == displays[i] ? " (main)" : string.Empty));
				buffer.AppendLine("  Rendering: " + displays[i].renderingWidth + " x " + displays[i].renderingHeight);
				buffer.AppendLine("  System: " + displays[i].systemWidth + " x " + displays[i].systemHeight);
			}

			if (buffer.Length >= Environment.NewLine.Length)
				buffer.Length -= Environment.NewLine.Length;

			return Utility.ReturnBuffer(buffer);
		}
	}
}