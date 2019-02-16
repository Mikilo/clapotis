using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGHub
{
	public class NGHubDropdownWindow : EditorWindow
	{
		internal static bool	isDead = true;

		private NGHubWindow	source;
		private int			minI;

		public void	Init(NGHubWindow source, int i)
		{
			this.source = source;
			this.minI = i;
		}

		protected virtual void	OnEnable()
		{
			NGHubDropdownWindow.isDead = false;
		}

		protected virtual void	OnGUI()
		{
			if (Event.current.type == EventType.Repaint)
			{
				Rect	r = this.position;
				r.x = 0F;
				r.y = 0F;
				Utility.DrawUnfillRect(r, Color.grey);
			}

			for (int i = this.minI; i < this.source.components.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				{
					GUILayout.FlexibleSpace();

					this.source.components[i].OnGUI();
				}
				EditorGUILayout.EndHorizontal();
			}

			if (Event.current.type == EventType.ContextClick)
				this.source.OpenContextMenu();
		}

		protected virtual void	OnLostFocus()
		{
			EditorApplication.delayCall += () => NGHubDropdownWindow.isDead = true;
		}
	}
}