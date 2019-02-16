using NGTools.NGRemoteScene;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[TypeHandlerDrawerFor(typeof(RectHandler))]
	internal sealed class RectDrawer : TypeHandlerDrawer
	{
		private BgColorContentAnimator	animX;
		private BgColorContentAnimator	animY;
		private BgColorContentAnimator	animW;
		private BgColorContentAnimator	animH;

		private ValueMemorizer<Single>	dragX;
		private ValueMemorizer<Single>	dragY;
		private ValueMemorizer<Single>	dragW;
		private ValueMemorizer<Single>	dragH;

		public	RectDrawer(TypeHandler typeHandler) : base(typeHandler)
		{
			this.dragX = new ValueMemorizer<Single>();
			this.dragX.labelWidth = 16F;
			this.dragY = new ValueMemorizer<Single>();
			this.dragY.labelWidth = this.dragX.labelWidth;
			this.dragW = new ValueMemorizer<Single>();
			this.dragW.labelWidth = this.dragX.labelWidth;
			this.dragH = new ValueMemorizer<Single>();
			this.dragH.labelWidth = this.dragX.labelWidth;
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			if (this.animX == null)
			{
				this.animX = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);
				this.animY = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);
				this.animW = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);
				this.animH = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);
			}

			Rect	vector = (Rect)data.Value;
			float	labelWidth;
			float	controlWidth;

			Utility.CalculSubFieldsWidth(r.width, 44F, 4, out labelWidth, out controlWidth);

			r.width = labelWidth;
			EditorGUI.LabelField(r, data.Name);
			r.x += r.width;

			using (IndentLevelRestorer.Get(0))
			using (LabelWidthRestorer.Get(14F))
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
					Single	newValue = EditorGUI.FloatField(r, "X", this.dragX.Get(vector.x));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path + NGServerScene.ValuePathSeparator + 'x', newValue, typeof(Single), TypeHandlersManager.GetTypeHandler(typeof(Single))))
					{
						data.unityData.RecordChange(path + NGServerScene.ValuePathSeparator + 'x', typeof(Single), this.dragX.Get(vector.x), newValue);
						this.dragX.Set(newValue);
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
					Single	newValue = EditorGUI.FloatField(r, "Y", this.dragY.Get(vector.y));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path + NGServerScene.ValuePathSeparator + 'y', newValue, typeof(Single), TypeHandlersManager.GetTypeHandler(typeof(Single))))
					{
						data.unityData.RecordChange(path + NGServerScene.ValuePathSeparator + 'y', typeof(Single), this.dragY.Get(vector.y), newValue);
						this.dragY.Set(newValue);
					}

					this.dragY.Draw(r);

					r.x += r.width;
				}

				if (data.Inspector.Hierarchy.GetUpdateNotification(path + NGServerScene.ValuePathSeparator + "width") != NotificationPath.None)
				{
					this.dragW.NewValue(vector.width);
					this.animW.Start();
				}

				using (this.animW.Restorer(0F, .8F + this.animW.Value, 0F, 1F))
				{
					EditorGUI.BeginChangeCheck();
					Single	newValue = EditorGUI.FloatField(r, "W", this.dragW.Get(vector.width));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path + NGServerScene.ValuePathSeparator + "width", newValue, typeof(Single), TypeHandlersManager.GetTypeHandler(typeof(Single))))
					{
						data.unityData.RecordChange(path + NGServerScene.ValuePathSeparator + "width", typeof(Single), this.dragW.Get(vector.width), newValue);
						this.dragW.Set(newValue);
					}

					this.dragW.Draw(r);

					r.x += r.width;
				}

				if (data.Inspector.Hierarchy.GetUpdateNotification(path + NGServerScene.ValuePathSeparator + "height") != NotificationPath.None)
				{
					this.dragH.NewValue(vector.height);
					this.animH.Start();
				}

				using (this.animH.Restorer(0F, .8F + this.animH.Value, 0F, 1F))
				{
					EditorGUI.BeginChangeCheck();
					Single	newValue = EditorGUI.FloatField(r, "H", this.dragH.Get(vector.height));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path + NGServerScene.ValuePathSeparator + "height", newValue, typeof(Single), TypeHandlersManager.GetTypeHandler(typeof(Single))))
					{
						data.unityData.RecordChange(path + NGServerScene.ValuePathSeparator + "height", typeof(Single), this.dragH.Get(vector.height), newValue);
						this.dragH.Set(newValue);
					}

					this.dragH.Draw(r);
				}
			}
		}
	}
}