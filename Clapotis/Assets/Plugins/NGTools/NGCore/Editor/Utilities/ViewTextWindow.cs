using UnityEditor;
using UnityEngine;

namespace NGToolsEditor
{
	internal class ViewTextWindow : EditorWindow
	{
		private bool	init;
		private string	text;
		private Vector2	scrollPosition;

		private static GUIStyle	style;

		public static void	Start(string text)
		{
			Utility.OpenWindow<ViewTextWindow>(true, "View Text", true, null, w => {
				w.init = false;
				w.text = text;
			});
		}

		protected virtual void	OnGUI()
		{
			if (ViewTextWindow.style == null)
			{
				ViewTextWindow.style = new GUIStyle(EditorStyles.label);
				ViewTextWindow.style.wordWrap = true;
			}

			if (this.init == false)
			{
				this.init = true;

				Utility.content.text = text;
				Vector2	size = EditorStyles.textArea.CalcSize(Utility.content);
				float	y = this.position.y;

				this.position = new Rect(this.position.x, this.position.y, size.x + 8F, size.y + 4F);

				// In case the window gets too large, reduce it and adapt height.
				if (this.position.width < size.x + 8F)
				{
					// Due to wrap behaviour, we need to shrink it step by step.
					float	width = this.position.width;
					float	height = ViewTextWindow.style.CalcHeight(Utility.content, width) + 8F;

					do
					{
						width -= 10F;
					}
					while (width > 100F && Mathf.Approximately(height, ViewTextWindow.style.CalcHeight(Utility.content, width) + 8F) == true);

					width += 20F;

					this.position = new Rect(this.position.x, y, width, height);
					this.Repaint();
				}

				GUI.FocusControl(null);
			}

			this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition, GUILayoutOptionPool.Width(this.position.width), GUILayoutOptionPool.Height(this.position.height));
			EditorGUILayout.TextArea(this.text, ViewTextWindow.style, GUILayoutOptionPool.ExpandWidthTrue, GUILayoutOptionPool.ExpandHeightTrue);
			GUILayout.EndScrollView();
		}
	}
}