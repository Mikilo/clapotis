using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGHub
{
	[Serializable, Category("Misc")]
	internal sealed class MenuCallerComponent : HubComponent
	{
		[Exportable]
		public string	menuItem;
		[Exportable]
		public Texture	image;
		[Exportable]
		public string	alias;

		[NonSerialized]
		private GUIContent	content = new GUIContent();

		public	MenuCallerComponent() : base("Call MenuItem", true)
		{
		}

		public override void	OnPreviewGUI(Rect r)
		{
			GUI.Label(r, "Call MenuItem \"" + this.menuItem + "\"");
		}

		public override void	OnEditionGUI()
		{
			using (LabelWidthRestorer.Get(100F))
			{
				if (GUILayout.Button("Pick") == true)
				{
					GenericMenu	menu = new GenericMenu();
					string[]	menuItems = Utility.GetAllMenuItems();

					for (int i = 0; i < menuItems.Length; i++)
						menu.AddItem(new GUIContent(menuItems[i]), false, this.PickMenuItem, menuItems[i]);

					menu.ShowAsContext();
				}

				this.menuItem = EditorGUILayout.TextField("Menu Item Path", this.menuItem);
				this.image = EditorGUILayout.ObjectField("Image", this.image, typeof(Texture), false) as Texture;
				this.alias = EditorGUILayout.TextField("Alias", this.alias);
			}
		}

		public override void	OnGUI()
		{
			this.content.text = (string.IsNullOrEmpty(this.alias) == true ? this.menuItem : this.alias);
			this.content.image = this.image;
			if (GUILayout.Button(this.content, GUILayoutOptionPool.Height(this.hub.height)) == true)
				EditorApplication.ExecuteMenuItem(this.menuItem);
		}

		private void	PickMenuItem(object data)
		{
			string	path = (string)data;

			this.menuItem = path;
			this.alias = path.Substring(path.LastIndexOf("/") + 1);
			this.hub.SaveComponents();
		}
	}
}