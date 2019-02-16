using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.Internal
{
	public class EditorWindowsDestroyer : EditorWindow
	{
		public const string	Title = "Windows Destroyer";

		private bool			showRawWindows;
		private EditorWindow[]	windows;

		[MenuItem(Constants.PackageTitle + "/Internal/" + EditorWindowsDestroyer.Title)]
		private static void	Open()
		{
			Utility.OpenWindow<EditorWindowsDestroyer>(EditorWindowsDestroyer.Title);
		}

		protected virtual void	OnEnable()
		{
			this.windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
		}

		protected virtual void	OnGUI()
		{
			if (GUI.Button(new Rect(this.position.width - 30F, 0F, 30F, 30F), "W") == true)
				this.showRawWindows = !this.showRawWindows;
			if (GUI.Button(new Rect(this.position.width - 60F, 0F, 30F, 30F), "R") == true)
				this.windows = Resources.FindObjectsOfTypeAll<EditorWindow>();

			if (this.showRawWindows == false)
			{
				float	totalWidth = 1920F * 2F;
				float	totalHeight = 1080F;
				float	offsetX = 1920F;
				float	offsetY = 75F;
				int		bringToFront = -1;

				for (int i = 0; i < this.windows.Length; i++)
				{
					Rect	r = new Rect();

					r.x = (this.windows[i].position.x + offsetX) * this.position.width / totalWidth;
					r.y = (this.windows[i].position.y + offsetY) * this.position.height / totalHeight;
					r.width = this.windows[i].position.width * this.position.width / totalWidth;
					r.height = this.windows[i].position.height * this.position.height / totalHeight;

					if (Event.current.type == EventType.MouseDown &&
						r.Contains(Event.current.mousePosition) == true)
					{
						bringToFront = i;
					}

					int	index = this.IndexOnSpot(this.windows[i]);
					if (index == 0)
						EditorGUI.DrawRect(r, new Color((i * 20F) / 255F, 0F, 0F, .85F));

					if (index == 0)
						GUI.Label(r, "\n" + this.windows[i].position.x + ", " + this.windows[i].position.y + ", " + this.windows[i].position.width + ", " + this.windows[i].position.height);
					else
					{
						r.xMin += 10F;
						r.y += 14F + index * 14F;
					}

					r.height = 14F;
					if (GUI.Button(r, i + " " + this.windows[i].GetType().Name, EditorStyles.textField) == true)
						this.windows[i].Close();
				}

				if (bringToFront >= 0)
				{
					var	tmp = this.windows[bringToFront];

					for (int j = bringToFront; j + 1 < this.windows.Length; j++)
						this.windows[j] = this.windows[j + 1];
					this.windows[this.windows.Length - 1] = tmp;
					return;
				}
			}
			else
			{
				for (int i = 0; i < this.windows.Length; i++)
				{
					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.Label(i + " " + this.windows[i].GetType().Name + "\n" + this.windows[i].position.x + ", " + this.windows[i].position.y + ", " + this.windows[i].position.width + ", " + this.windows[i].position.height, GUILayoutOptionPool.ExpandWidthFalse);
						if (GUILayout.Button("Close", GUILayoutOptionPool.ExpandWidthFalse) == true)
						{
							try
							{
								this.windows[i].Close();
								this.windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
							}
							catch
							{
								Object.DestroyImmediate(this.windows[i]);
							}
						}
					}
					EditorGUILayout.EndHorizontal();
				}
			}

			this.Repaint();
		}

		private	int	IndexOnSpot(EditorWindow window)
		{
			Rect	position = window.position;
			int		index = 0;

			for (int i = 0; i < this.windows.Length; i++)
			{
				if (this.windows[i].position == position)
				{
					if (this.windows[i] == window)
						break;
					++index;
				}
			}

			return index;
		}
	}
}