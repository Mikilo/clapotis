using NGTools.NGRemoteScene;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[TypeHandlerDrawerFor(typeof(QuaternionHandler))]
	internal sealed class QuaternionDrawer : TypeHandlerDrawer
	{
		private BgColorContentAnimator	animX;
		private BgColorContentAnimator	animY;
		private BgColorContentAnimator	animZ;
		private BgColorContentAnimator	animW;

		private ValueMemorizer<Single>	dragX;
		private ValueMemorizer<Single>	dragY;
		private ValueMemorizer<Single>	dragZ;
		private ValueMemorizer<Single>	dragW;

		public	QuaternionDrawer(TypeHandler typeHandler) : base(typeHandler)
		{
			this.dragX = new ValueMemorizer<Single>();
			this.dragX.labelWidth = 16F;
			this.dragY = new ValueMemorizer<Single>();
			this.dragY.labelWidth = this.dragX.labelWidth;
			this.dragZ = new ValueMemorizer<Single>();
			this.dragZ.labelWidth = this.dragX.labelWidth;
			this.dragW = new ValueMemorizer<Single>();
			this.dragW.labelWidth = this.dragX.labelWidth;
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			if (this.animX == null)
			{
				this.animX = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);
				this.animY = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);
				this.animZ = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);
				this.animW = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);
			}

			Quaternion	quaternion = (Quaternion)data.Value;
			float		labelWidth;
			float		controlWidth;

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
					this.dragX.NewValue(quaternion.x);
					this.animX.Start();
				}

				using (this.animX.Restorer(0F, .8F + this.animX.Value, 0F, 1F))
				{
					EditorGUI.BeginChangeCheck();
					Single	newValue = EditorGUI.FloatField(r, "X", this.dragX.Get(quaternion.x));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path + NGServerScene.ValuePathSeparator + 'x', newValue, typeof(Single), TypeHandlersManager.GetTypeHandler(typeof(Single))))
					{
						data.unityData.RecordChange(path + NGServerScene.ValuePathSeparator + 'x', typeof(Single), this.dragX.Get(quaternion.x), newValue);
						this.dragX.Set(newValue);
					}

					this.dragX.Draw(r);

					r.x += r.width;
				}

				if (data.Inspector.Hierarchy.GetUpdateNotification(path + NGServerScene.ValuePathSeparator + 'y') != NotificationPath.None)
				{
					this.dragY.NewValue(quaternion.y);
					this.animY.Start();
				}

				using (this.animY.Restorer(0F, .8F + this.animY.Value, 0F, 1F))
				{
					EditorGUI.BeginChangeCheck();
					Single	newValue = EditorGUI.FloatField(r, "Y", this.dragY.Get(quaternion.y));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path + NGServerScene.ValuePathSeparator + 'y', newValue, typeof(Single), TypeHandlersManager.GetTypeHandler(typeof(Single))))
					{
						data.unityData.RecordChange(path + NGServerScene.ValuePathSeparator + 'y', typeof(Single), this.dragY.Get(quaternion.y), newValue);
						this.dragY.Set(newValue);
					}

					this.dragY.Draw(r);

					r.x += r.width;
				}

				if (data.Inspector.Hierarchy.GetUpdateNotification(path + NGServerScene.ValuePathSeparator + 'z') != NotificationPath.None)
				{
					this.dragZ.NewValue(quaternion.z);
					this.animZ.Start();
				}

				using (this.animZ.Restorer(0F, .8F + this.animZ.Value, 0F, 1F))
				{
					EditorGUI.BeginChangeCheck();
					Single	newValue = EditorGUI.FloatField(r, "Z", this.dragZ.Get(quaternion.z));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path + NGServerScene.ValuePathSeparator + 'z', newValue, typeof(Single), TypeHandlersManager.GetTypeHandler(typeof(Single))))
					{
						data.unityData.RecordChange(path + NGServerScene.ValuePathSeparator + 'z', typeof(Single), this.dragZ.Get(quaternion.z), newValue);
						this.dragZ.Set(newValue);
					}

					this.dragZ.Draw(r);

					r.x += r.width;
				}

				if (data.Inspector.Hierarchy.GetUpdateNotification(path + NGServerScene.ValuePathSeparator + 'w') != NotificationPath.None)
				{
					this.dragW.NewValue(quaternion.w);
					this.animW.Start();
				}

				using (this.animW.Restorer(0F, .8F + this.animW.Value, 0F, 1F))
				{
					EditorGUI.BeginChangeCheck();
					Single	newValue = EditorGUI.FloatField(r, "W", this.dragW.Get(quaternion.w));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path + NGServerScene.ValuePathSeparator + 'w', newValue, typeof(Single), TypeHandlersManager.GetTypeHandler(typeof(Single))))
					{
						data.unityData.RecordChange(path + NGServerScene.ValuePathSeparator + 'w', typeof(Single), this.dragW.Get(quaternion.w), newValue);
						this.dragW.Set(newValue);
					}

					this.dragW.Draw(r);
				}
			}
		}
	}
}