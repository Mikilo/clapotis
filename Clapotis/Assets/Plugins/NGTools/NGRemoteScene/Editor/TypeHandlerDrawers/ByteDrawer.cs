using NGTools.NGRemoteScene;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[TypeHandlerDrawerFor(typeof(ByteHandler))]
	internal sealed class ByteDrawer : TypeHandlerDrawer
	{
		private BgColorContentAnimator	anim;
		private ValueMemorizer<Byte>	drag;

		public	ByteDrawer(TypeHandler typeHandler) : base(typeHandler)
		{
			this.drag = new ValueMemorizer<Byte>();
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			if (this.anim == null)
				this.anim = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);

			string	path = data.GetPath();
			if (data.Inspector.Hierarchy.GetUpdateNotification(path) != NotificationPath.None)
			{
				this.drag.NewValue((Byte)data.Value);
				this.anim.Start();
			}

			using (this.anim.Restorer(0F, .8F + this.anim.Value, 0F, 1F))
			{
				try
				{
					EditorGUI.BeginChangeCheck();
					Byte	newValue = (Byte)EditorGUI.IntField(r, data.Name, this.drag.Get((Byte)data.Value));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path, newValue, typeof(Byte)) == true)
					{
						data.unityData.RecordChange(path, typeof(Byte), this.drag.Get((Byte)data.Value), newValue);
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