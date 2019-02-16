using NGTools.NGRemoteScene;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[TypeHandlerDrawerFor(typeof(CharHandler))]
	internal sealed class CharDrawer : TypeHandlerDrawer
	{
		private BgColorContentAnimator	anim;
		private ValueMemorizer<Char>	drag;

		public	CharDrawer(TypeHandler typeHandler) : base(typeHandler)
		{
			this.drag = new ValueMemorizer<Char>();
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			if (this.anim == null)
				this.anim = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);

			string	path = data.GetPath();
			if (data.Inspector.Hierarchy.GetUpdateNotification(path) != NotificationPath.None)
			{
				this.drag.NewValue((Char)data.Value);
				this.anim.Start();
			}

			using (this.anim.Restorer(0F, .8F + this.anim.Value, 0F, 1F))
			{
				EditorGUI.BeginChangeCheck();
				string	newValue = EditorGUI.TextField(r, data.Name, this.drag.Get((Char)data.Value).ToString());
				if (EditorGUI.EndChangeCheck() == true &&
					string.IsNullOrEmpty(newValue) == false &&
					this.AsyncUpdateCommand(data.unityData, path, newValue[0], typeof(Char)) == true)
				{
					data.unityData.RecordChange(path, typeof(Char), this.drag.Get((Char)data.Value), newValue);
					this.drag.Set(newValue[0]);
				}

				this.drag.Draw(r);
			}
		}
	}
}