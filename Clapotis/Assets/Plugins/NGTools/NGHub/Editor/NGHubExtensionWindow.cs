using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGHub
{
	public class NGHubExtensionWindow : EditorWindow
	{
		public int	minI;
		public int	hiddenI;

		private NGHubWindow	source;
		private bool		overflowing;

		public void	Init(NGHubWindow source)
		{
			this.source = source;
			this.minI = 0;
			this.hiddenI = 0;
		}

		protected virtual void	OnEnable()
		{
			Undo.undoRedoPerformed += this.Repaint;
		}

		protected virtual void	OnDestroy()
		{
			Undo.undoRedoPerformed -= this.Repaint;
		}

		protected virtual void	OnGUI()
		{
			if (this.source == null || this.source.Initialized == false)
				return;

			if (Event.current.type == EventType.Repaint)
			{
				if (this.source.backgroundColor.a > 0F)
					EditorGUI.DrawRect(new Rect(0F, 0F, this.position.width, this.position.height), this.source.backgroundColor);
				else
					EditorGUI.DrawRect(new Rect(0F, 0F, this.position.width, this.position.height), NGHubWindow.DockBackgroundColor);
			}

			if (this.overflowing == true)
			{
				Rect	r = this.position;
				r.y = 5F;
				r.x = r.width - 20F;
				r.width = 20F;

				if (Event.current.type == EventType.MouseDown &&
					Event.current.button == 0 &&
					r.Contains(Event.current.mousePosition) == true)
				{
					NGHubDropdownWindow[]	windows = Resources.FindObjectsOfTypeAll<NGHubDropdownWindow>();

					if (windows.Length > 0)
					{
						for (int i = 0; i < windows.Length; i++)
							windows[i].Close();
					}
					else if (NGHubDropdownWindow.isDead == true)
					{
						NGHubDropdownWindow	window = EditorWindow.CreateInstance<NGHubDropdownWindow>();

						window.Init(this.source, this.hiddenI);

						r.x = this.position.x + this.position.width - 150F;
						r.y += this.position.y - 5F;
						window.ShowAsDropDown(r, new Vector2(150F, 4F + 24F * (this.source.components.Count - this.hiddenI)));
					}
				}
			}

			EditorGUILayout.BeginHorizontal(GUILayoutOptionPool.Height(this.source.height));
			{
				this.source.HandleDrop();

				EventType	catchedType = EventType.Used;

				if (Event.current.type == EventType.Repaint)
					this.overflowing = false;

				for (int i = this.minI; i < this.source.components.Count; i++)
				{
					// Catch event from the cropped component.
					if (Event.current.type != EventType.Repaint &&
						Event.current.type != EventType.Layout)
					{
						if (this.hiddenI == i)
						{
							// Simulate context click, because MouseUp is used, therefore ContextClick is not sent.
							if (Event.current.type == EventType.MouseUp &&
								Event.current.button == 1)
							{
								catchedType = EventType.ContextClick;
							}
							else
								catchedType = Event.current.type;
							Event.current.Use();
						}
					}

					EditorGUILayout.BeginHorizontal();
					{
						this.source.components[i].OnGUI();
					}
					EditorGUILayout.EndHorizontal();

					if (Event.current.type == EventType.Repaint)
					{
						Rect	r = GUILayoutUtility.GetLastRect();

						if (r.xMax >= this.position.width)
						{
							// Hide the miserable trick...
							r.xMin -= 2F;
							r.yMin -= 3F;
							r.yMax += 2F;
							r.xMax += 2F;
							EditorGUI.DrawRect(r, NGHubWindow.DockBackgroundColor);
							this.hiddenI = i;
							this.overflowing = true;
							break;
						}
						else if (this.hiddenI == i)
							--this.hiddenI;
					}
				}

				if (Event.current.type == EventType.ContextClick ||
					catchedType == EventType.ContextClick)
				{
					this.source.OpenContextMenu();
				}
			}

			GUILayout.FlexibleSpace();

			if (this.overflowing == true)
			{
				Rect	r = this.position;
				r.y = 5F;
				r.x = r.width - 20F;
				r.width = 20F;

				GUI.Button(r, "", "Dropdown");
			}

			EditorGUILayout.EndHorizontal();
		}

		protected virtual void	Update()
		{
			if (this.source == null || EditorApplication.isCompiling == true)
				this.Close();
		}
	}
}