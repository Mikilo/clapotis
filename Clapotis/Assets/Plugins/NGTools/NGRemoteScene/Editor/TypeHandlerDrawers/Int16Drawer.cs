using NGTools.NGRemoteScene;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[TypeHandlerDrawerFor(typeof(Int16Handler))]
	internal sealed class Int16Drawer : TypeHandlerDrawer
	{
		private BgColorContentAnimator	anim;
		private ValueMemorizer<Int16>	drag;

		public	Int16Drawer(TypeHandler typeHandler) : base(typeHandler)
		{
			this.drag = new ValueMemorizer<Int16>();
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			if (this.anim == null)
				this.anim = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);

			string	path = data.GetPath();
			if (data.Inspector.Hierarchy.GetUpdateNotification(path) != NotificationPath.None)
			{
				this.drag.NewValue((Int16)data.Value);
				this.anim.Start();
			}

			using (this.anim.Restorer(0F, .8F + this.anim.Value, 0F, 1F))
			{
				try
				{
					EditorGUI.BeginChangeCheck();
					Int16	newValue = Convert.ToInt16(EditorGUI.IntField(r, data.Name, this.drag.Get((Int16)data.Value)));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path, newValue, typeof(Int16)) == true)
					{
						data.unityData.RecordChange(path, typeof(Int16), this.drag.Get((Int16)data.Value), newValue);
						this.drag.Set(newValue);
					}

					this.drag.Draw(r);
				}
				catch (OverflowException)
				{
				}
			}
		}
	}
}