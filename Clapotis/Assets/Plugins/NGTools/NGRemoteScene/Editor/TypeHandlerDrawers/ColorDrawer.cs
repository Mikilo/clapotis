using NGTools.NGRemoteScene;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[TypeHandlerDrawerFor(typeof(ColorHandler))]
	internal sealed class ColorDrawer : TypeHandlerDrawer
	{
		private ColorContentAnimator	anim;

		public	ColorDrawer(TypeHandler typeHandler) : base(typeHandler)
		{
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			if (this.anim == null)
				this.anim = new ColorContentAnimator(data.Inspector.Repaint, 1F, 0F);

			string	path = data.GetPath();
			if (data.Inspector.Hierarchy.GetUpdateNotification(path) != NotificationPath.None)
				this.anim.Start();

			using (this.anim.Restorer(0F, .8F + this.anim.Value, 0F, 1F))
			{
				try
				{
					EditorGUI.BeginChangeCheck();
					Color	newValue = EditorGUI.ColorField(r, data.Name, (Color)data.Value);
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path, newValue, typeof(Color)) == true)
					{
						data.unityData.RecordChange(path, typeof(Color), data.Value, newValue);
					}
				}
				// EditorGUI.ColorField throws this on click.
				catch (ExitGUIException)
				{
				}
			}
		}
	}
}