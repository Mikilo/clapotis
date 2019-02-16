using NGTools.NGRemoteScene;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[TypeHandlerDrawerFor(typeof(UInt64Handler))]
	internal sealed class UInt64Drawer : TypeHandlerDrawer
	{
		private BgColorContentAnimator	anim;
		private ValueMemorizer<UInt64>	drag;

		public	UInt64Drawer(TypeHandler typeHandler) : base(typeHandler)
		{
			this.drag = new ValueMemorizer<UInt64>();
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			if (this.anim == null)
				this.anim = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);

			string	path = data.GetPath();
			if (data.Inspector.Hierarchy.GetUpdateNotification(path) != NotificationPath.None)
			{
				this.drag.NewValue((UInt64)data.Value);
				this.anim.Start();
			}

			using (this.anim.Restorer(0F, .8F + this.anim.Value, 0F, 1F))
			{
				try
				{
					EditorGUI.BeginChangeCheck();
					UInt64	newValue = Convert.ToUInt64(EditorGUI.LongField(r, data.Name, Convert.ToInt64(this.drag.Get((UInt64)data.Value))));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path, newValue, typeof(UInt64)) == true)
					{
						data.unityData.RecordChange(path, typeof(UInt64), this.drag.Get((UInt64)data.Value), newValue);
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