using NGTools.UON;
using System;
using UnityEditor;

namespace NGToolsEditor.NGHub
{
	using UnityEngine;

	[Serializable, Category("Misc")]
	internal sealed class AssetShortcutComponent : HubComponent, IUONSerialization
	{
		[Exportable]
		public Object	asset;
		[Exportable]
		public Texture	image;
		[Exportable]
		public string	text;

		[NonSerialized]
		private GUIContent	content;
		[NonSerialized]
		private GUIStyle	button;

		public	AssetShortcutComponent() : base("Asset Shortcut", true)
		{
		}

		public override void	Init(NGHubWindow hub)
		{
			base.Init(hub);

			this.content = new GUIContent(this.text, this.asset != null ? this.asset.name : string.Empty);
		}

		public override void	OnPreviewGUI(Rect r)
		{
			GUI.Label(r, "Asset \"" + (this.asset != null ? this.asset.name : string.Empty) + "\"");
		}

		public override void	OnEditionGUI()
		{
			using (LabelWidthRestorer.Get(60F))
			{
				EditorGUI.BeginChangeCheck();
				this.asset = EditorGUILayout.ObjectField("Asset", this.asset, typeof(Object), false);
				this.image = EditorGUILayout.ObjectField("Image", this.image, typeof(Texture), false) as Texture;
				this.text = EditorGUILayout.TextField("Text", this.text);
				if (EditorGUI.EndChangeCheck() == true)
				{
					this.content.text = this.text;
					this.content.tooltip = this.asset != null ? this.asset.name : string.Empty;
				}
			}
		}

		public override void	OnGUI()
		{
			if (this.button == null)
				this.button = new GUIStyle(GUI.skin.button);

			float	w = this.button.CalcSize(this.content).x;

			if (this.image == null)
				this.button.padding.left = GUI.skin.button.padding.left;
			else
			{
				this.button.padding.left = (int)this.hub.height;
				w += 12F; // Remove texture width, because Button calculates using the whole height.
			}

			Rect	r = GUILayoutUtility.GetRect(w, this.hub.height, GUI.skin.button);

			if (Event.current.type == EventType.MouseDrag &&
				Utility.position2D != Vector2.zero &&
				DragAndDrop.GetGenericData(Utility.DragObjectDataName) != null &&
				(Utility.position2D - Event.current.mousePosition).sqrMagnitude >= Constants.MinStartDragDistance)
			{
				Utility.position2D = Vector2.zero;
				DragAndDrop.StartDrag("Drag Object");
				Event.current.Use();
			}
			else if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition) == true)
			{
				NGEditorGUILayout.PingObject(asset);

				Utility.position2D = Event.current.mousePosition;
				DragAndDrop.PrepareStartDrag();
				DragAndDrop.objectReferences = new Object[] { this.asset };
				DragAndDrop.SetGenericData(Utility.DragObjectDataName, 1);
			}

			GUI.Button(r, this.content, this.button);
			if (this.image != null)
			{
				r = GUILayoutUtility.GetLastRect();
				r.x += 4F;
				r.width = r.height;
				GUI.DrawTexture(r, this.image);
			}
		}

		public override void	InitDrop(NGHubWindow hub)
		{
			base.InitDrop(hub);

			this.asset = DragAndDrop.objectReferences[0];
			this.image = Utility.GetIcon(DragAndDrop.objectReferences[0].GetInstanceID());
			this.text = DragAndDrop.objectReferences[0].name;
			this.content = new GUIContent(this.text, this.asset != null ? this.asset.name : string.Empty);
		}

		private static bool	CanDrop()
		{
			if (DragAndDrop.objectReferences.Length > 0)
				return string.IsNullOrEmpty(AssetDatabase.GetAssetPath(DragAndDrop.objectReferences[0])) == false;
			return false;
		}

		void	IUONSerialization.OnSerializing()
		{
		}

		void	IUONSerialization.OnDeserialized(DeserializationData data)
		{
			if (this.image == null && this.asset != null)
			{
				string	path = data.GetRawValue("image");

				if (string.IsNullOrEmpty(path) == false)
					this.image = Utility.GetIcon(this.asset.GetInstanceID());
			}
		}
	}
}