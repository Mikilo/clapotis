using System;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NGToolsEditor.NGHub
{
	public class NGHubEditorWindow : EditorWindow
	{
		public const string	Title = "NG Hub Editor";

		private NGHubWindow	hub;
		public NGHubWindow	Hub { get { return this.hub; } }

		private ReorderableList		list;
		private Rect				headerRect;
		private HubComponentWindow	componentWindow;
		private bool				skipComponentEditorOpening;
		private Vector2				scrollPosition;
		private HubComponent		lastEditingComponent;

		protected virtual void	OnEnable()
		{
			Undo.undoRedoPerformed += this.Repaint;
		}

		protected virtual void	OnDestroy()
		{
			if (this.componentWindow != null)
				this.componentWindow.Close();
			Undo.undoRedoPerformed -= this.Repaint;
		}

		public void	Init(NGHubWindow hub)
		{
			this.hub = hub;
		}

		protected virtual void	OnGUI()
		{
			if (this.hub == null || this.hub.Initialized == false)
				return;

			if (this.list == null)
			{
				this.list = new ReorderableList(this.hub.components, typeof(HubComponent), true, false, true, true);
				this.list.headerHeight = 24F;
				this.list.drawHeaderCallback = (r) => GUI.Label(r, "Components", GeneralStyles.Title1);
				this.list.showDefaultBackground = false;
				this.list.drawElementCallback = this.DrawComponent;
				this.list.onAddCallback = this.OpenAddComponentWizard;
				this.list.onRemoveCallback = (l) => { l.list.RemoveAt(l.index); this.hub.SaveComponents(); };
				this.list.onReorderCallback = (l) => this.hub.SaveComponents();
				this.list.onChangedCallback = (l) => this.hub.Repaint();
			}

			MethodInfo[]	droppableComponentsMethods = this.hub.DroppableComponents;

			if (droppableComponentsMethods.Length > 0 &&
				DragAndDrop.objectReferences.Length > 0)
			{
				for (int i = 0; i < droppableComponentsMethods.Length; i++)
				{
					if ((bool)droppableComponentsMethods[i].Invoke(null, null) == true)
					{
						Rect	r = GUILayoutUtility.GetRect(GUI.skin.label.CalcSize(new GUIContent(droppableComponentsMethods[i].DeclaringType.Name)).x, this.hub.height, GUI.skin.label);

						if (Event.current.type == EventType.Repaint)
						{
							Utility.DropZone(r, Utility.NicifyVariableName(droppableComponentsMethods[i].DeclaringType.Name));
							this.Repaint();
						}
						else if (Event.current.type == EventType.DragUpdated &&
								 r.Contains(Event.current.mousePosition) == true)
						{
							DragAndDrop.visualMode = DragAndDropVisualMode.Move;
						}
						else if (Event.current.type == EventType.DragPerform &&
								 r.Contains(Event.current.mousePosition) == true)
						{
							DragAndDrop.AcceptDrag();

							HubComponent	component = Activator.CreateInstance(droppableComponentsMethods[i].DeclaringType) as HubComponent;

							if (component != null)
							{
								component.InitDrop(this.hub);
								this.hub.components.Add(component);
								this.hub.SaveComponents();
							}

							DragAndDrop.PrepareStartDrag();
							Event.current.Use();
						}
					}
				}
			}

			Rect	r2 = this.position;
			r2.x = 0F;

			if (this.hub.DockedAsMenu == false)
			{
				r2.y = 24F;

				using (LabelWidthRestorer.Get(50F))
				{
					EditorGUI.BeginChangeCheck();
					this.hub.height = EditorGUILayout.FloatField("Height", this.hub.height);
					if (EditorGUI.EndChangeCheck() == true)
						this.hub.Repaint();
				}
			}
			else
				r2.y = 0F;

			EditorGUI.BeginChangeCheck();
			this.hub.backgroundColor = EditorGUILayout.ColorField("Background Color", this.hub.backgroundColor);
			if (EditorGUI.EndChangeCheck() == true)
				this.hub.Repaint();

			this.headerRect = GUILayoutUtility.GetLastRect();

			this.scrollPosition = EditorGUILayout.BeginScrollView(this.scrollPosition);
			{
				this.list.DoLayoutList();
			}
			EditorGUILayout.EndScrollView();
		}

		protected virtual void	Update()
		{
			if (this.hub == null)
				this.Close();
		}

		protected virtual void	OnFocus()
		{
			if (this.componentWindow != null)
			{
				this.lastEditingComponent = this.componentWindow.component;
				this.componentWindow.Close();
				this.componentWindow = null;
				this.skipComponentEditorOpening = true;
			}
		}

		private void	DrawComponent(Rect rect, int i, bool isActive, bool isFocused)
		{
			if (this.hub.components[i].hasEditorGUI == true)
			{
				Rect	rect2 = rect;

				rect2.width = 40F;
				rect.xMin += rect2.width;

				if (Event.current.type == EventType.MouseDown &&
					rect2.Contains(Event.current.mousePosition) == true)
				{
					// This condition is required to prevent closing and opening in the same frame when toggling the same component.
					if (this.skipComponentEditorOpening == true && this.lastEditingComponent == this.hub.components[i])
					{
						this.skipComponentEditorOpening = false;
						return;
					}

					HubComponentWindow[]	windows = Resources.FindObjectsOfTypeAll< HubComponentWindow >();

					if (windows.Length > 0)
					{
						for (int j = 0; j < windows.Length; j++)
							windows[j].Close();
					}
					else
					{
						this.componentWindow = EditorWindow.CreateInstance<HubComponentWindow>();
						this.componentWindow.titleContent.text = this.hub.components[i].name;
						this.componentWindow.position = new Rect(this.position.x + rect.x, this.position.y + rect.y + rect.height + this.headerRect.yMax - this.scrollPosition.y, Mathf.Max(HubComponentWindow.MinWidth, this.position.width - rect2.xMax), this.componentWindow.position.height);
						this.componentWindow.Init(this, this.hub.components[i]);
						this.componentWindow.ShowPopup();
						this.componentWindow.Focus();
					}
				}

				GUI.Button(rect2, "Edit");
			}

			this.hub.components[i].OnPreviewGUI(rect);
		}

		private void	OpenAddComponentWizard(ReorderableList list)
		{
			if (this.hub.CheckMaxHubComponents(this.hub.components.Count) == true)
				this.hub.OpenAddComponentWizard();
		}

		private void	DeleteComponent(object i)
		{
			this.hub.components.RemoveAt((int)i);
			this.hub.SaveComponents();
			this.hub.Repaint();
		}
	}
}