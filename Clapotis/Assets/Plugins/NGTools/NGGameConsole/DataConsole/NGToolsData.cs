using System;
using UnityEngine;

namespace NGTools.NGGameConsole
{
	public class NGToolsData : DataConsole
	{
		[SerializeField, HideInInspector]
		private bool	unusedNGTools;

		private string	fullString = string.Empty;

		protected virtual void	Awake()
		{
			this.fullString = Constants.PackageTitle + " : " + Constants.Version + Environment.NewLine + "NG Core : " + NGTools.NGAssemblyInfo.Version + Environment.NewLine + "NG Game Console : " + NGAssemblyInfo.Version;
		}

		public override void	FullGUI()
		{
			GUILayout.Label(this.fullString, this.fullStyle);
		}

		public override string	Copy()
		{
			return this.fullString;
		}
	}
}