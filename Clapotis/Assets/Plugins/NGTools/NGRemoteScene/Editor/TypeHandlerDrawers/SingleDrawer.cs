using NGTools.NGRemoteScene;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[TypeHandlerDrawerFor(typeof(SingleHandler))]
	internal sealed class SingleDrawer : TypeHandlerDrawer
	{
		private BgColorContentAnimator	anim;
		private ValueMemorizer<Single>	drag;

		public	SingleDrawer(TypeHandler typeHandler) : base(typeHandler)
		{
			this.drag = new ValueMemorizer<Single>();
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			if (this.anim == null)
				this.anim = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);

			string	path = data.GetPath();
			if (data.Inspector.Hierarchy.GetUpdateNotification(path) != NotificationPath.None)
			{
				this.drag.NewValue((Single)data.Value);
				this.anim.Start();
			}

			using (this.anim.Restorer(0F, .8F + this.anim.Value, 0F, 1F))
			{
				EditorGUI.BeginChangeCheck();
				Single	newValue = EditorGUI.FloatField(r, data.Name, this.drag.Get((Single)data.Value));
				if (EditorGUI.EndChangeCheck() == true &&
					this.AsyncUpdateCommand(data.unityData, path, newValue, typeof(Single)) == true)
				{
					data.unityData.RecordChange(path, typeof(Single), this.drag.Get((Single)data.Value), newValue);
					this.drag.Set(newValue);
				}

				this.drag.Draw(r);
			}
		}
	}
}