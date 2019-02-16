using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	public abstract class TabModule
	{
		public readonly string					name;
		public readonly ResourcesPickerWindow	window;

		protected	TabModule(string name, ResourcesPickerWindow window)
		{
			this.name = name;
			this.window = window;
		}

		public virtual void		OnEnter()
		{
		}

		public virtual void		OnLeave()
		{
		}

		public abstract void	OnGUI(Rect r);
	}
}