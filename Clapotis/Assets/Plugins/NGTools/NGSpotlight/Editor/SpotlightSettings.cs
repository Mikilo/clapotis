using UnityEngine;

namespace NGToolsEditor.NGSpotlight
{
	[NGSettings(NGSpotlightWindow.Title)]
	public class SpotlightSettings : ScriptableObject
	{
		public Color	hoverSelectionColor = Color.cyan;
		public Color	outlineSelectionColor = new Color(42F / 255F, 139 / 255F, 55F / 255F, 1F);
		public Color	highlightLetterColor = new Color(42F / 255F, 139 / 255F, 55F / 255F, 1F);

		[Header("Maximum displayed results."), Range(1, 1000)]
		public int		maxResult = 50;

		[Header("If the entry partially contains the pattern, it will be added.")]
		[Header("Set to false if the entry must contain the whole pattern.")]
		public bool		keepResultsWithPartialMatch = true;
	}
}