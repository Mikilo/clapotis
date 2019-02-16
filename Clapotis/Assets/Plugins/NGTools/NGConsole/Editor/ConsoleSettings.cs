using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	public class ConsoleSettings : ScriptableObject
	{
		[LocaleHeader("ConsoleSettings_InputsManager")]
		public InputsManager	inputsManager = new InputsManager();

		[HideInInspector]
		public MultiDataStorage	serializedModules = new MultiDataStorage();
	}
}