using NGToolsEditor.NGHub;
using System;
using UnityEngine;

namespace NGToolsEditor.NGScenes
{
	[Serializable, Category("Scene")]
	internal sealed class ScenesComponent : HubComponent
	{
		private const float	WindowWidth = 600F;
		private const float	WindowHeight = 400F;
		
		[NonSerialized]
		private GUIContent		content;
		[NonSerialized]
		private GUIStyle		dropDownButton;

		public	ScenesComponent() : base("NG Scenes")
		{
		}

		public override void	Init(NGHubWindow hub)
		{
			base.Init(hub);

			this.content = new GUIContent("Scenes", "Toggle scenes manager.");
		}

		public override void	OnGUI()
		{
			if (this.dropDownButton == null)
				this.dropDownButton = new GUIStyle("DropDownButton");

			this.dropDownButton.fixedHeight = this.hub.height;

			Rect	r = GUILayoutUtility.GetRect(75F, this.hub.height, this.dropDownButton);

			if (GUI.Button(r, this.content, this.dropDownButton) == true)
			{
				NGScenesWindow[]	windows = Resources.FindObjectsOfTypeAll<NGScenesWindow>();

				if (windows.Length > 0)
				{
					for (int i = 0; i < windows.Length; i++)
						windows[i].Close();
				}
				else
				{
					NGScenesWindow	window = ScriptableObject.CreateInstance<NGScenesWindow>();
					window.position = new Rect(r.x + this.hub.position.x, this.hub.position.y + this.hub.height + 4F, ScenesComponent.WindowWidth, ScenesComponent.WindowHeight);
					window.maxSize = new Vector2(window.position.width, window.position.height);
					window.minSize = window.maxSize;
					window.ShowPopup();
				}
			}
		}
	}
}