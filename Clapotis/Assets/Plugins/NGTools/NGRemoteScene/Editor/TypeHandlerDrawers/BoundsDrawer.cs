using NGTools.NGRemoteScene;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[TypeHandlerDrawerFor(typeof(BoundsHandler))]
	internal sealed class BoundsDrawer : TypeHandlerDrawer
	{
		private BgColorContentAnimator	animCenterX;
		private BgColorContentAnimator	animCenterY;
		private BgColorContentAnimator	animCenterZ;
		private BgColorContentAnimator	animExtentsX;
		private BgColorContentAnimator	animExtentsY;
		private BgColorContentAnimator	animExtentsZ;

		private ValueMemorizer<Single>	dragCenterX;
		private ValueMemorizer<Single>	dragCenterY;
		private ValueMemorizer<Single>	dragCenterZ;
		private ValueMemorizer<Single>	dragExtentsX;
		private ValueMemorizer<Single>	dragExtentsY;
		private ValueMemorizer<Single>	dragExtentsZ;

		public	BoundsDrawer(TypeHandler typeHandler) : base(typeHandler)
		{
			this.dragCenterX = new ValueMemorizer<Single>();
			this.dragCenterX.labelWidth = 16F;
			this.dragCenterY = new ValueMemorizer<Single>();
			this.dragCenterY.labelWidth = this.dragCenterX.labelWidth;
			this.dragCenterZ = new ValueMemorizer<Single>();
			this.dragCenterZ.labelWidth = this.dragCenterX.labelWidth;

			this.dragExtentsX = new ValueMemorizer<Single>();
			this.dragExtentsX.labelWidth = this.dragCenterX.labelWidth;
			this.dragExtentsY = new ValueMemorizer<Single>();
			this.dragExtentsY.labelWidth = this.dragCenterX.labelWidth;
			this.dragExtentsZ = new ValueMemorizer<Single>();
			this.dragExtentsZ.labelWidth = this.dragCenterX.labelWidth;
		}

		public override float	GetHeight(object value)
		{
			return Constants.SingleLineHeight + 2F + Constants.SingleLineHeight;
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			if (this.animCenterX == null)
			{
				this.animCenterX = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);
				this.animCenterY = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);
				this.animCenterZ = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);
				this.animExtentsX = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);
				this.animExtentsY = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);
				this.animExtentsZ = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);
			}

			Bounds	vector = (Bounds)data.Value;
			float	labelWidth;
			float	controlWidth;

			Utility.CalculSubFieldsWidth(r.width, 44F, 4, out labelWidth, out controlWidth);

			r.width = labelWidth;
			EditorGUI.LabelField(r, data.Name);
			r.x += r.width;

			using (IndentLevelRestorer.Get(0))
			using (LabelWidthRestorer.Get(14F))
			{
				float	x = r.x;
				r.width = controlWidth;
				r.height = Constants.SingleLineHeight;

				GUI.Label(r, "Center:");
				r.x += r.width;

				string	path = data.GetPath();
				if (data.Inspector.Hierarchy.GetUpdateNotification(path + NGServerScene.ValuePathSeparator + "center" + NGServerScene.ValuePathSeparator + 'x') != NotificationPath.None)
				{
					this.dragCenterX.NewValue(vector.center.x);
					this.animCenterX.Start();
				}

				using (this.animCenterX.Restorer(0F, .8F + this.animCenterX.Value, 0F, 1F))
				{
					EditorGUI.BeginChangeCheck();
					Single	newValue = EditorGUI.FloatField(r, "x", this.dragCenterX.Get(vector.center.x));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path + NGServerScene.ValuePathSeparator + "center" + NGServerScene.ValuePathSeparator + 'x', newValue, typeof(Single), TypeHandlersManager.GetTypeHandler(typeof(Single))))
					{
						data.unityData.RecordChange(path + NGServerScene.ValuePathSeparator + "center" + NGServerScene.ValuePathSeparator + 'x', typeof(Single), this.dragCenterX.Get(vector.center.x), newValue);
						this.dragCenterX.Set(newValue);
					}

					this.dragCenterX.Draw(r);

					r.x += r.width;
				}

				if (data.Inspector.Hierarchy.GetUpdateNotification(path + NGServerScene.ValuePathSeparator + "center" + NGServerScene.ValuePathSeparator + 'y') != NotificationPath.None)
				{
					this.dragCenterY.NewValue(vector.center.y);
					this.animCenterY.Start();
				}

				using (this.animCenterY.Restorer(0F, .8F + this.animCenterY.Value, 0F, 1F))
				{
					EditorGUI.BeginChangeCheck();
					Single	newValue = EditorGUI.FloatField(r, "Y", this.dragCenterY.Get(vector.center.y));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path + NGServerScene.ValuePathSeparator + "center" + NGServerScene.ValuePathSeparator + 'y', newValue, typeof(Single), TypeHandlersManager.GetTypeHandler(typeof(Single))))
					{
						data.unityData.RecordChange(path + NGServerScene.ValuePathSeparator + "center" + NGServerScene.ValuePathSeparator + 'y', typeof(Single), this.dragCenterY.Get(vector.center.y), newValue);
						this.dragCenterY.Set(newValue);
					}

					this.dragCenterY.Draw(r);

					r.x += r.width;
				}

				if (data.Inspector.Hierarchy.GetUpdateNotification(path + NGServerScene.ValuePathSeparator + "center" + NGServerScene.ValuePathSeparator + 'z') != NotificationPath.None)
				{
					this.dragCenterZ.NewValue(vector.center.z);
					this.animCenterZ.Start();
				}

				using (this.animCenterZ.Restorer(0F, .8F + this.animCenterZ.Value, 0F, 1F))
				{
					EditorGUI.BeginChangeCheck();
					Single	newValue = EditorGUI.FloatField(r, "Z", this.dragCenterZ.Get(vector.center.z));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path + NGServerScene.ValuePathSeparator + "center" + NGServerScene.ValuePathSeparator + 'z', newValue, typeof(Single), TypeHandlersManager.GetTypeHandler(typeof(Single))))
					{
						data.unityData.RecordChange(path + NGServerScene.ValuePathSeparator + "center" + NGServerScene.ValuePathSeparator + 'z', typeof(Single), this.dragCenterZ.Get(vector.center.z), newValue);
						this.dragCenterZ.Set(newValue);
					}

					this.dragCenterZ.Draw(r);

					r.x += r.width;
				}

				r.y += r.height + 2F;
				r.x = x;

				GUI.Label(r, "Extents:");
				r.x += r.width;

				if (data.Inspector.Hierarchy.GetUpdateNotification(path + NGServerScene.ValuePathSeparator + "extents" + NGServerScene.ValuePathSeparator + 'x') != NotificationPath.None)
				{
					this.dragExtentsX.NewValue(vector.extents.x);
					this.animExtentsX.Start();
				}

				using (this.animExtentsX.Restorer(0F, .8F + this.animExtentsX.Value, 0F, 1F))
				{
					EditorGUI.BeginChangeCheck();
					Single	newValue = EditorGUI.FloatField(r, "X", this.dragExtentsX.Get(vector.extents.x));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path + NGServerScene.ValuePathSeparator + "extents" + NGServerScene.ValuePathSeparator + 'x', newValue, typeof(Single), TypeHandlersManager.GetTypeHandler(typeof(Single))))
					{
						data.unityData.RecordChange(path + NGServerScene.ValuePathSeparator + "extents" + NGServerScene.ValuePathSeparator + 'x', typeof(Single), this.dragExtentsX.Get(vector.extents.x), newValue);
						this.dragExtentsX.Set(newValue);
					}

					this.dragExtentsX.Draw(r);

					r.x += r.width;
				}

				if (data.Inspector.Hierarchy.GetUpdateNotification(path + NGServerScene.ValuePathSeparator + "extents" + NGServerScene.ValuePathSeparator + 'y') != NotificationPath.None)
				{
					this.dragExtentsY.NewValue(vector.extents.y);
					this.animExtentsY.Start();
				}

				using (this.animExtentsY.Restorer(0F, .8F + this.animExtentsY.Value, 0F, 1F))
				{
					EditorGUI.BeginChangeCheck();
					Single	newValue = EditorGUI.FloatField(r, "Y", this.dragExtentsY.Get(vector.extents.y));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path + NGServerScene.ValuePathSeparator + "extents" + NGServerScene.ValuePathSeparator + 'y', newValue, typeof(Single), TypeHandlersManager.GetTypeHandler(typeof(Single))))
					{
						data.unityData.RecordChange(path + NGServerScene.ValuePathSeparator + "extents" + NGServerScene.ValuePathSeparator + 'y', typeof(Single), this.dragExtentsY.Get(vector.extents.y), newValue);
						this.dragExtentsY.Set(newValue);
					}

					this.dragExtentsY.Draw(r);

					r.x += r.width;
				}

				if (data.Inspector.Hierarchy.GetUpdateNotification(path + NGServerScene.ValuePathSeparator + "extents" + NGServerScene.ValuePathSeparator + 'z') != NotificationPath.None)
				{
					this.dragExtentsZ.NewValue(vector.extents.z);
					this.animExtentsZ.Start();
				}

				using (this.animExtentsZ.Restorer(0F, .8F + this.animExtentsZ.Value, 0F, 1F))
				{
					EditorGUI.BeginChangeCheck();
					Single	newValue = EditorGUI.FloatField(r, "Z", this.dragExtentsZ.Get(vector.extents.z));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path + NGServerScene.ValuePathSeparator + "extents" + NGServerScene.ValuePathSeparator + 'z', newValue, typeof(Single), TypeHandlersManager.GetTypeHandler(typeof(Single))))
					{
						data.unityData.RecordChange(path + NGServerScene.ValuePathSeparator + "extents" + NGServerScene.ValuePathSeparator + 'z', typeof(Single), this.dragExtentsZ.Get(vector.extents.z), newValue);
						this.dragExtentsZ.Set(newValue);
					}

					this.dragExtentsZ.Draw(r);

					r.x += r.width;
				}
			}
		}
	}
}