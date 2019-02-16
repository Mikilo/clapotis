using System;
using System.Collections.Generic;
using UnityEngine;

namespace NGToolsEditor
{
	public class CustomHotkeysSettings : ScriptableObject
	{
		[Serializable]
		public class MethodHotkey
		{
			public string	staticMethod;
			public string	bind;
		}

		public List<MethodHotkey>	hotkeys = new List<MethodHotkey>();
	}
}