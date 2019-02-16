using NGTools;
using System.Collections.Generic;
using UnityEngine;

namespace NGToolsEditor.NGFav
{
	[NGSettings(NGFavWindow.Title)]
	public class FavSettings : ScriptableObject
	{
		public enum ChangeSelection
		{
			SimpleClick,
			DoubleClick,
			Modifier,
			ModifierOrDoubleClick,
		}

		[LocaleHeader("NGFav_ChangeSelectionDescription")]
		public ChangeSelection	changeSelection = ChangeSelection.ModifierOrDoubleClick;
		[EnumMask, LocaleHeader("NGFav_SelectModifiersDescription")]
		public EventModifiers	selectModifiers = (EventModifiers)((int)EventModifiers.Shift << 1);
		[EnumMask, LocaleHeader("NGFav_DeleteModifiersDescription")]
		public EventModifiers	deleteModifiers = (EventModifiers)((int)(EventModifiers.Control | EventModifiers.Shift) << 1);

		[HideInInspector]
		public List<Favorites>	favorites = new List<Favorites>();
	}
}