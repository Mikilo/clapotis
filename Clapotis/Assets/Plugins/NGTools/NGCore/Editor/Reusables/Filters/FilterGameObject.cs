using System;
using UnityEngine;

namespace NGToolsEditor
{
	[Serializable]
	public class FilterGameObject : Filter
	{
		private GameObject	gameObject;
		public GameObject	GameObject
		{
			get
			{
				return this.gameObject;
			}
			set
			{
				this.gameObject = value;

				if (this.gameObject != null)
				{
					this.scenePath = this.gameObject.scene.path;
					this.hierarchyPath = Utility.GetHierarchyStringified(this.gameObject.transform);
				}
				else
				{
					this.scenePath = string.Empty;
					this.hierarchyPath = string.Empty;
				}
			}
		}
		public string		scenePath;
		public string		hierarchyPath;
	}
}