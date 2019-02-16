using NGTools.NGRemoteScene;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[TypeHandlerDrawerFor(typeof(BooleanHandler))]
	internal sealed class BooleanDrawer : TypeHandlerDrawer
	{
		private BgColorContentAnimator	anim;
		private ValueMemorizer<Boolean>	drag;

		public	BooleanDrawer(TypeHandler typeHandler) : base(typeHandler)
		{
			this.drag = new ValueMemorizer<Boolean>();
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			if (this.anim == null)
				this.anim = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);

			string	path = data.GetPath();
			if (data.Inspector.Hierarchy.GetUpdateNotification(path) != NotificationPath.None)
			{
				this.drag.NewValue((Boolean)data.Value);
				this.anim.Start();
			}

			using (this.anim.Restorer(0F, .8F + this.anim.Value, 0F, 1F))
			{
				EditorGUI.BeginChangeCheck();
				Boolean	newValue = EditorGUI.Toggle(r, data.Name, this.drag.Get((Boolean)data.Value));
				if (EditorGUI.EndChangeCheck() == true &&
					this.AsyncUpdateCommand(data.unityData, path, newValue, typeof(Boolean)) == true)
				{
					data.unityData.RecordChange(path, typeof(Boolean), this.drag.Get((Boolean)data.Value), newValue);
					this.drag.Set(newValue);
				}

				this.drag.Draw(r);
			}
		}
	}
}