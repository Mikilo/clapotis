using NGToolsEditor.NGHub;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGNavSelection
{
	[Serializable, Category("Misc")]
	internal sealed class NavPickerComponent : HubComponent
	{
		private int		maxElements = 3;
		private float	maxWidth = 80F;

		[NonSerialized]
		private double	lastClick;
		[NonSerialized]
		private GUIStyle	unwrapLabel;
		[NonSerialized]
		private GUIStyle	buttonLeft;
		[NonSerialized]
		private GUIStyle	buttonMid;
		[NonSerialized]
		private GUIStyle	buttonRight;

		public	NavPickerComponent() : base("Navigation Picker", true, true)
		{
		}

		public override void	Init(NGHubWindow hub)
		{
			base.Init(hub);

			NGNavSelectionWindow.SelectionChanged += this.hub.Repaint;
		}

		public override void	OnEditionGUI()
		{
			using (LabelWidthRestorer.Get(80F))
			{
				EditorGUI.BeginChangeCheck();
				this.maxElements = EditorGUILayout.IntField("Max Element", this.maxElements);
				if (EditorGUI.EndChangeCheck() == true)
				{
					if (this.maxElements < 1)
						this.maxElements = 1;

					this.hub.Repaint();
				}
				EditorGUI.BeginChangeCheck();
				this.maxWidth = EditorGUILayout.FloatField("Max Width", this.maxWidth);
				if (EditorGUI.EndChangeCheck() == true)
				{
					if (this.maxWidth < 30F)
						this.maxWidth = 30F;

					this.hub.Repaint();
				}
			}
		}

		public override void	Uninit()
		{
			base.Uninit();

			NGNavSelectionWindow.SelectionChanged -= this.hub.Repaint;
		}

		public override void	OnGUI()
		{
			if (this.unwrapLabel == null)
			{
				this.unwrapLabel = new GUIStyle(GUI.skin.label);
				this.unwrapLabel.wordWrap = false;
				this.buttonLeft = new GUIStyle("ButtonLeft");
				this.buttonRight = new GUIStyle("ButtonRight");
				this.buttonMid = new GUIStyle("ButtonMid");
			}

			for (int i = NGNavSelectionWindow.historic.Count - 1; i >= 0 && i >= NGNavSelectionWindow.historic.Count - this.maxElements; i--)
			{
				AssetsSelection	selection = NGNavSelectionWindow.historic[i];
				Texture2D		image = null;
				string			label = string.Empty;

				if (selection.refs.Count > 0)
				{
					if (selection.refs[0].@object == null)
					{
						if (selection.refs[0].hierarchy.Count > 0)
							label = (string.IsNullOrEmpty(selection.refs[0].resolverAssemblyQualifiedName) == false ? "(R)" : "") + selection.refs[0].hierarchy[selection.refs[0].hierarchy.Count - 1];
						else
							label = "Unknown";
					}
					else
						label = selection.refs[0].@object.name;
				}

				if (selection.refs.Count > 1)
					label = "(" + selection.refs.Count + ") " + label;

				image = Utility.GetIcon(selection.refs[0].@object.GetInstanceID());

				float	width = this.maxWidth;
				GUIStyle	style;

				if (i == NGNavSelectionWindow.historic.Count - 1)
					style = this.buttonLeft;
				else if (i == 0 || i == NGNavSelectionWindow.historic.Count - this.maxElements)
					style = this.buttonRight;
				else
					style = this.buttonMid;

				Rect	r = GUILayoutUtility.GetRect(GUIContent.none, style, GUILayoutOptionPool.Height(this.hub.height), GUILayoutOptionPool.Width(width));

				if (Event.current.type == EventType.MouseDrag &&
					(this.dragOriginPosition - Event.current.mousePosition).sqrMagnitude >= Constants.MinStartDragDistance &&
					i.Equals(DragAndDrop.GetGenericData(Utility.DragObjectDataName)) == true)
				{
					DragAndDrop.StartDrag("Drag Object");
					Event.current.Use();
				}
				else if (Event.current.type == EventType.MouseDown &&
						 r.Contains(Event.current.mousePosition) == true)
				{
					this.dragOriginPosition = Event.current.mousePosition;

					if (Event.current.button == 0)
					{
						DragAndDrop.PrepareStartDrag();
						DragAndDrop.objectReferences = new UnityEngine.Object[] { selection.refs[0].@object };
						DragAndDrop.SetGenericData(Utility.DragObjectDataName, i);
						DragAndDrop.SetGenericData(NGHubWindow.DragFromNGHub, true);
					}
				}

				if (GUI.Button(r, string.Empty, style) == true)
				{
					if (Event.current.button == 1 || this.lastClick + Constants.DoubleClickTime > EditorApplication.timeSinceStartup)
					{
						selection.Select();
						NGNavSelectionWindow.lastFocusedHistoric = i;
						Utility.RepaintEditorWindow(typeof(NGNavSelectionWindow));
					}
					else if (Event.current.button == 0)
						EditorGUIUtility.PingObject(selection.refs[0].@object);

					this.lastClick = EditorApplication.timeSinceStartup;
				}

				if (image != null)
				{
					width = r.width;

					r.xMax = r.xMin + 16F;
					r.x += 2F;
					GUI.DrawTexture(r, image, ScaleMode.ScaleToFit);

					r.xMin += r.width;
					r.width = width - r.width - 20F;
				}

				r.y += 3F;
				Utility.content.text = label;
				Utility.content.tooltip = label;
				GUI.Label(r, Utility.content, this.unwrapLabel);
			}

			Utility.content.tooltip = null;
		}

		private Vector2	dragOriginPosition;
	}
}