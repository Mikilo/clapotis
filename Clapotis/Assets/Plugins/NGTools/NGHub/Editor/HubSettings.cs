using UnityEngine;

namespace NGToolsEditor.NGHub
{
	public class HubSettings : ScriptableObject
	{
		[HideInInspector]
		public MultiDataStorage	hubData = new MultiDataStorage();
		public float			NGHubYOffset = 0F;
	}
}