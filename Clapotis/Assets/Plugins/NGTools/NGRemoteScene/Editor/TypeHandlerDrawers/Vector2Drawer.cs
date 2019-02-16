using NGTools.NGRemoteScene;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[TypeHandlerDrawerFor(typeof(Vector2Handler))]
	internal sealed class Vector2Drawer : TypeHandlerDrawer
	{
		private BgColorContentAnimator	animX;
		private BgColorContentAnimator	animY;

		private ValueMemorizer<Single>	dragX;
		private ValueMemorizer<Single>	dragY;

		public	Vector2Drawer(TypeHandler typeHandler) : base(typeHandler)
		{
			this.dragX = new ValueMemorizer<Single>() { labelWidth = 16F };
			this.dragY = new ValueMemorizer<Single>() { labelWidth = 16F };
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			if (this.animX == null)
			{
				this.animX = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);
				this.animY = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);
			}

			Vector2	vector = (Vector2)data.Value;
			float	labelWidth;
			float	controlWidth;

			Utility.CalculSubFieldsWidth(r.width, 90F, 2, out labelWidth, out controlWidth);

			r.width = labelWidth;
			EditorGUI.LabelField(r, data.Name);
			r.x += r.width;

			using (IndentLevelRestorer.Get(0))
			using (LabelWidthRestorer.Get(12F))
			{
				r.width = controlWidth;

				string	path = data.GetPath();
				if (data.Inspector.Hierarchy.GetUpdateNotification(path + NGServerScene.ValuePathSeparator + 'x') != NotificationPath.None)
				{
					this.dragX.NewValue(vector.x);
					this.animX.Start();
				}

				using (this.animX.Restorer(0F, .8F + this.animX.Value, 0F, 1F))
				{
					EditorGUI.BeginChangeCheck();
					Single	v = EditorGUI.FloatField(r, "X", this.dragX.Get(vector.x));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path + NGServerScene.ValuePathSeparator + 'x', v, typeof(Single), TypeHandlersManager.GetTypeHandler(typeof(Single))))
					{
						data.unityData.RecordChange(path + NGServerScene.ValuePathSeparator + 'x', typeof(Single), this.dragX.Get(vector.x), v);
						this.dragX.Set(v);
					}

					this.dragX.Draw(r);

					r.x += r.width;
				}

				if (data.Inspector.Hierarchy.GetUpdateNotification(path + NGServerScene.ValuePathSeparator + 'y') != NotificationPath.None)
				{
					this.dragY.NewValue(vector.y);
					this.animY.Start();
				}

				using (this.animY.Restorer(0F, .8F + this.animY.Value, 0F, 1F))
				{
					EditorGUI.BeginChangeCheck();
					Single	v = EditorGUI.FloatField(r, "Y", this.dragY.Get(vector.y));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path + NGServerScene.ValuePathSeparator + 'y', v, typeof(Single), TypeHandlersManager.GetTypeHandler(typeof(Single))))
					{
						data.unityData.RecordChange(path + NGServerScene.ValuePathSeparator + 'y', typeof(Single), this.dragY.Get(vector.y), v);
						this.dragY.Set(v);
					}

					this.dragY.Draw(r);
				}
			}
		}
	}
}