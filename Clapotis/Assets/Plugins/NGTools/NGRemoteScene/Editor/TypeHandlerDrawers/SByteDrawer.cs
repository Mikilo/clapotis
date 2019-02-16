using NGTools.NGRemoteScene;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[TypeHandlerDrawerFor(typeof(SByteHandler))]
	internal sealed class SByteDrawer : TypeHandlerDrawer
	{
		private BgColorContentAnimator	anim;
		private ValueMemorizer<SByte>	drag;

		public	SByteDrawer(TypeHandler typeHandler) : base(typeHandler)
		{
			this.drag = new ValueMemorizer<SByte>();
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			if (this.anim == null)
				this.anim = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);

			string	path = data.GetPath();
			if (data.Inspector.Hierarchy.GetUpdateNotification(path) != NotificationPath.None)
			{
				this.drag.NewValue((SByte)data.Value);
				this.anim.Start();
			}

			using (this.anim.Restorer(0F, .8F + this.anim.Value, 0F, 1F))
			{
				try
				{
					EditorGUI.BeginChangeCheck();
					SByte	newValue = Convert.ToSByte(EditorGUI.IntField(r, data.Name, Convert.ToInt32(this.drag.Get((SByte)data.Value))));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path, newValue, typeof(SByte)) == true)
					{
						data.unityData.RecordChange(path, typeof(SByte), this.drag.Get((SByte)data.Value), newValue);
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