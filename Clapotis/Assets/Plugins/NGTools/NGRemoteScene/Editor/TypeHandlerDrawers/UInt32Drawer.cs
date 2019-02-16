using NGTools.NGRemoteScene;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[TypeHandlerDrawerFor(typeof(UInt32Handler))]
	internal sealed class UInt32Drawer : TypeHandlerDrawer
	{
		private BgColorContentAnimator	anim;
		private ValueMemorizer<UInt32>	drag;

		public	UInt32Drawer(TypeHandler typeHandler) : base(typeHandler)
		{
			this.drag = new ValueMemorizer<UInt32>();
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			if (this.anim == null)
				this.anim = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);

			string	path = data.GetPath();
			if (data.Inspector.Hierarchy.GetUpdateNotification(path) != NotificationPath.None)
			{
				this.drag.NewValue((UInt32)data.Value);
				this.anim.Start();
			}

			using (this.anim.Restorer(0F, .8F + this.anim.Value, 0F, 1F))
			{
				try
				{
					EditorGUI.BeginChangeCheck();
					UInt32	newValue = Convert.ToUInt32(EditorGUI.LongField(r, data.Name, Convert.ToInt64(this.drag.Get((UInt32)data.Value))));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path, newValue, typeof(UInt32)) == true)
					{
						data.unityData.RecordChange(path, typeof(UInt32), this.drag.Get((UInt32)data.Value), newValue);
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