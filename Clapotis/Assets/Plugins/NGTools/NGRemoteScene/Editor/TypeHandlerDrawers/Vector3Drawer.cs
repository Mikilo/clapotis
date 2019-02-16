using NGTools.NGRemoteScene;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[TypeHandlerDrawerFor(typeof(Vector3Handler))]
	internal sealed class Vector3Drawer : TypeHandlerDrawer
	{
		private BgColorContentAnimator	animX;
		private BgColorContentAnimator	animY;
		private BgColorContentAnimator	animZ;

		private ValueMemorizer<Single>	dragX;
		private ValueMemorizer<Single>	dragY;
		private ValueMemorizer<Single>	dragZ;

		public	Vector3Drawer(TypeHandler typeHandler) : base(typeHandler)
		{
			this.dragX = new ValueMemorizer<Single>() { labelWidth = 16F };
			this.dragY = new ValueMemorizer<Single>() { labelWidth = 16F };
			this.dragZ = new ValueMemorizer<Single>() { labelWidth = 16F };
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			if (this.animX == null)
			{
				this.animX = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);
				this.animY = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);
				this.animZ = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);
			}

			Vector3		vector = (Vector3)data.Value;
			float		labelWidth;
			float		controlWidth;

			Utility.CalculSubFieldsWidth(r.width, 60F, 3, out labelWidth, out controlWidth);

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

					r.x += r.width;
				}

				if (data.Inspector.Hierarchy.GetUpdateNotification(path + NGServerScene.ValuePathSeparator + 'z') != NotificationPath.None)
				{
					this.dragZ.NewValue(vector.z);
					this.animZ.Start();
				}

				using (this.animZ.Restorer(0F, .8F + this.animZ.Value, 0F, 1F))
				{
					EditorGUI.BeginChangeCheck();
					Single	v = EditorGUI.FloatField(r, "Z", this.dragZ.Get(vector.z));
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path + NGServerScene.ValuePathSeparator + 'z', v, typeof(Single), TypeHandlersManager.GetTypeHandler(typeof(Single))))
					{
						data.unityData.RecordChange(path + NGServerScene.ValuePathSeparator + 'z', typeof(Single), this.dragZ.Get(vector.z), v);
						this.dragZ.Set(v);
					}

					this.dragZ.Draw(r);
				}
			}
		}
	}
}