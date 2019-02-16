using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor
{
	public class GUICallbackWindow : EditorWindow
	{
		private event Action	pendingCallback;
		private event Action	callback;

		public static void	Open(Action callback)
		{
			GUICallbackWindow	w = EditorWindow.GetWindow<GUICallbackWindow>();

			w.maxSize = new Vector2(1F, 1F);
			w.minSize = new Vector2(1F, 1F);
			w.pendingCallback += callback;
		}

		protected virtual void	OnGUI()
		{
			// Callback can be null in the rare case the window is still alive.
			if (this.pendingCallback != null)
			{
				try
				{
					this.callback = this.pendingCallback;
					this.pendingCallback = null;
					this.callback();
				}
				catch
				{
				}
				finally
				{
					this.callback = null;
				}
			}
		}

		protected virtual void	Update()
		{
			if (this.pendingCallback == null)
				this.Close();
		}
	}
}