using UnityEngine;

namespace NGToolsEditor.NGNavSelection
{
	public class NavSettings : ScriptableObject
	{
		public bool	enable = true;
		public int	maxHistoric = 100;
		public int	maxDisplayHierarchy = 0;
	}
}