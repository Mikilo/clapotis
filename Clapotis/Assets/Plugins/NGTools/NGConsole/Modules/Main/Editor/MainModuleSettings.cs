using NGTools;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	[NGSettings("Main Module", 20)]
	public class MainModuleSettings : ScriptableObject
	{
		[LocaleHeader("NGSettings_MainModule_AlertOnWarning")]
		public bool		alertOnWarning = true;
		[LocaleHeader("NGSettings_MainModule_WarningColor")]
		public Color	warningColor = new Color(90F / 255F, 255F / 255F, 0F / 255F);
		[LocaleHeader("NGSettings_MainModule_ErrorColor")]
		public Color	errorColor = new Color(236F / 255F, 0F / 255F, 0F / 255F);
		[LocaleHeader("NGSettings_MainModule_DisplayClearStreamButton")]
		public bool		displayClearStreamButton = false;
		[LocaleHeader("NGSettings_MainModule_CompactStreamsLimit"), Range(0, 10)]
		public int		compactStreamsLimit = 1;
		[LocaleHeader("NGSettings_MainModule_DefaultFiltersInStream"), SubClassesOf(typeof(ILogFilter))]
		public string[]	defaultFiltersInStream = new string[] { typeof(ContentFilter).FullName };

		public IEnumerable<ILogFilter>	GenerateFilters()
		{
			for (int i = 0; i < this.defaultFiltersInStream.Length; i++)
			{
				Type	t = Type.GetType(this.defaultFiltersInStream[i]);

				if (t != null)
				{
					ILogFilter	filter = Activator.CreateInstance(t) as ILogFilter;

					if (filter != null)
						yield return filter;
				}
			}
		}
	}
}