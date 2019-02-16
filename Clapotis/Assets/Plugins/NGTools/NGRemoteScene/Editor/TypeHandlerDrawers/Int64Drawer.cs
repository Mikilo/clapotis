using NGTools.NGRemoteScene;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[TypeHandlerDrawerFor(typeof(Int64Handler))]
	internal sealed class Int64Drawer : TypeHandlerDrawer
	{
		private BgColorContentAnimator	anim;
		private ValueMemorizer<Int64>	drag;

		public	Int64Drawer(TypeHandler typeHandler) : base(typeHandler)
		{
			this.drag = new ValueMemorizer<Int64>();
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			if (this.anim == null)
				this.anim = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);

			string	path = data.GetPath();
			if (data.Inspector.Hierarchy.GetUpdateNotification(path) != NotificationPath.None)
			{
				this.drag.NewValue((Int64)data.Value);
				this.anim.Start();
			}

			using (this.anim.Restorer(0F, .8F + this.anim.Value, 0F, 1F))
			{
				EditorGUI.BeginChangeCheck();
				Int64	newValue = EditorGUI.LongField(r, data.Name, this.drag.Get((Int64)data.Value));
				if (EditorGUI.EndChangeCheck() == true &&
					this.AsyncUpdateCommand(data.unityData, path, newValue, typeof(Int64)) == true)
				{
					data.unityData.RecordChange(path, typeof(Int64), this.drag.Get((Int64)data.Value), newValue);
					this.drag.Set(newValue);
				}

				this.drag.Draw(r);
			}
		}
	}
}