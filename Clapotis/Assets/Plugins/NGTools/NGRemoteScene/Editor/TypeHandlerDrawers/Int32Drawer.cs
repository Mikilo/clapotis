using NGTools.NGRemoteScene;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[TypeHandlerDrawerFor(typeof(Int32Handler))]
	internal sealed class Int32Drawer : TypeHandlerDrawer
	{
		private BgColorContentAnimator	anim;
		private ValueMemorizer<Int32>	drag;

		public	Int32Drawer(TypeHandler typeHandler) : base(typeHandler)
		{
			this.drag = new ValueMemorizer<Int32>();
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			if (this.anim == null)
				this.anim = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);

			string	path = data.GetPath();
			if (data.Inspector.Hierarchy.GetUpdateNotification(path) != NotificationPath.None)
			{
				this.drag.NewValue((Int32)data.Value);
				this.anim.Start();
			}

			using (this.anim.Restorer(0F, .8F + this.anim.Value, 0F, 1F))
			{
				EditorGUI.BeginChangeCheck();
				Int32	newValue = EditorGUI.IntField(r, data.Name, this.drag.Get((Int32)data.Value));
				if (EditorGUI.EndChangeCheck() == true &&
					this.AsyncUpdateCommand(data.unityData, path, newValue, typeof(Int32)) == true)
				{
					data.unityData.RecordChange(path, typeof(Int32), this.drag.Get((Int32)data.Value), newValue);
					this.drag.Set(newValue);
				}

				this.drag.Draw(r);
			}
		}
	}
}