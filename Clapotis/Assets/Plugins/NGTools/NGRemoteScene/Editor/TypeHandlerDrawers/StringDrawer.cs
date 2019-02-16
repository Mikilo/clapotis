using NGTools.NGRemoteScene;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[TypeHandlerDrawerFor(typeof(StringHandler))]
	internal sealed class StringDrawer : TypeHandlerDrawer
	{
		private BgColorContentAnimator	anim;
		private ValueMemorizer<String>	drag;

		public	StringDrawer(TypeHandler typeHandler) : base(typeHandler)
		{
			this.drag = new ValueMemorizer<String>();
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			if (this.anim == null)
				this.anim = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);

			string	path = data.GetPath();
			if (data.Inspector.Hierarchy.GetUpdateNotification(path) != NotificationPath.None)
			{
				this.drag.NewValue((String)data.Value);
				this.anim.Start();
			}

			using (this.anim.Restorer(0F, .8F + this.anim.Value, 0F, 1F))
			{
				EditorGUI.BeginChangeCheck();
				String	newValue = EditorGUI.TextField(r, data.Name, this.drag.Get((String)data.Value));
				if (EditorGUI.EndChangeCheck() == true &&
					this.AsyncUpdateCommand(data.unityData, path, newValue, typeof(String)) == true)
				{
					data.unityData.RecordChange(path, typeof(String), this.drag.Get((String)data.Value), newValue);
					this.drag.Set(newValue);
				}

				this.drag.Draw(r);
			}
		}
	}
}