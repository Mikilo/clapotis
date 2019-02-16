using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
    public interface IDataDrawerTool
	{
		NGRemoteHierarchyWindow	Hierarchy { get; }
		Vector2					ScrollPosition { get; }
		Rect					BodyRect { get; }

		void	Repaint();
	}
}