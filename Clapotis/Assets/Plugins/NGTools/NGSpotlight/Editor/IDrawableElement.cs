using UnityEngine;

namespace NGToolsEditor.NGSpotlight
{
	public interface IDrawableElement
	{
		string	RawContent { get; }
		string	LowerStringContent { get; }
		void	OnGUI(Rect r, NGSpotlightWindow window, EntryRef key, int i);
		void	Select(NGSpotlightWindow window, EntryRef key);
		void	Execute(NGSpotlightWindow window, EntryRef key);
	}
}