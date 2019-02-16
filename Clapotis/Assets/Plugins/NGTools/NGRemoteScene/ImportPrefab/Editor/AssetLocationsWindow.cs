using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	public class AssetLocationsWindow : PopupWindowContent
	{
		public const string	Title = "Asset Locations";

		public NGRemoteHierarchyWindow				hierarchy;
		public List<AssetImportParameters.MyClass>	paths;

		public	AssetLocationsWindow(NGRemoteHierarchyWindow hierarchy, List<AssetImportParameters.MyClass> paths)
		{
			this.hierarchy = hierarchy;
			this.paths = paths;
		}

		public override void	OnOpen()
		{
			this.editorWindow.titleContent.text = AssetLocationsWindow.Title;
		}

		public override Vector2	GetWindowSize()
		{
			return new Vector2(400F, (Constants.SingleLineHeight + 2F) * (1F + this.paths.Count));
		}

		public override void	OnGUI(Rect r)
		{
			r.height = Constants.SingleLineHeight;
			GUI.Label(r, "Locations :");
			r.y += r.height + 2F;

			for (int i = 0; i < this.paths.Count; i++)
			{
				GUI.Label(r, this.hierarchy.GetGameObjectName(this.paths[i].gameObjectInstanceID) + " > " + this.hierarchy.GetBehaviourName(this.paths[i].gameObjectInstanceID, this.paths[i].componentInstanceID) + " > " + this.paths[i].path);
				r.y += r.height + 2F;
			}
		}
	}
}