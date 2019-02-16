using NGTools.NGRemoteScene;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	internal sealed class ArrayDrawer : TypeHandlerDrawer
	{
		private BgColorContentAnimator	anim;
		private List<TypeHandlerDrawer>	subDrawers;
		private bool					fold;
		private Type					subType;
		private TypeHandler				subHandler;

		public	ArrayDrawer(TypeHandler typeHandler, Type type) : base(typeHandler)
		{
			this.subType = Utility.GetArraySubType(type);
			this.subHandler = TypeHandlersManager.GetTypeHandler(this.subType);
			this.subDrawers = new List<TypeHandlerDrawer>();
		}

		public override float	GetHeight(object value)
		{
			if (this.fold == false)
				return Constants.SingleLineHeight;

			ArrayData	array = value as ArrayData;

			if (array.array == null)
			{
				if (array.isNull == true)
					return Constants.SingleLineHeight + Constants.SingleLineHeight;
				return Constants.SingleLineHeight + Constants.SingleLineHeight + Constants.SingleLineHeight;
			}

			float	height = 32F; // First line + Size
			int		i = 0;

			foreach (object item in array.array)
			{
				if (item != null)
				{
					// Add new drawer for new element.
					if (this.subDrawers.Count <= i)
						this.subDrawers.Add(TypeHandlerDrawersManager.CreateTypeHandlerDrawer(this.subHandler, this.subType));

					height += this.subDrawers[i].GetHeight(item);
				}
				else
					height += Constants.SingleLineHeight;

				++i;
			}

			return height;
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			if (this.anim == null)
				this.anim = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);

			string		path = data.GetPath();
			ArrayData	array = data.Value as ArrayData;

			if (array.array == null)
			{
				--EditorGUI.indentLevel;
				r.height = Constants.SingleLineHeight;
				r.x += 3F;
				this.fold = EditorGUI.Foldout(r, this.fold, data.Name + (array.isNull == true ? " (Null)" : " (Unloaded)"), true);
				r.x -= 3F;
				++EditorGUI.indentLevel;

				if (this.fold == false)
					return;

				r.y += r.height;
				if (array.isNull == true)
				{
					++EditorGUI.indentLevel;
					EditorGUI.BeginChangeCheck();
					int	forceSize = EditorGUI.DelayedIntField(r, "Size", 0);
					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path, forceSize, typeof(int), TypeHandlersManager.GetTypeHandler(typeof(int))))
					{
						data.unityData.RecordChange(path, typeof(int), 0, forceSize);
					}
					--EditorGUI.indentLevel;
				}
				else
				{
					GUI.Label(r, "Array was not loaded because it has more than " + ArrayData.BigArrayThreshold + " elements.");
					r.y += r.height;

					if (GUI.Button(r, "Load") == true)
						data.Inspector.Hierarchy.LoadBigArray(path);
				}

				return;
			}

			r.height = Constants.SingleLineHeight;

			if (data.Inspector.Hierarchy.GetUpdateNotification(path) != NotificationPath.None)
				this.anim.Start();

			using (this.anim.Restorer(0F, .8F + this.anim.Value, 0F, 1F))
			{
				--EditorGUI.indentLevel;
				r.x += 3F;
				this.fold = EditorGUI.Foldout(r, this.fold, data.Name + " [" + array.array.Length.ToCachedString() + "]", true);
				r.x -= 3F;
				++EditorGUI.indentLevel;
			}

			if (this.fold == false)
				return;

			r.y += r.height;

			++EditorGUI.indentLevel;
			EditorGUI.BeginChangeCheck();
			int	newSize = EditorGUI.DelayedIntField(r, "Size", array.array.Length);
			if (EditorGUI.EndChangeCheck() == true &&
				this.AsyncUpdateCommand(data.unityData, path, newSize, typeof(int), TypeHandlersManager.GetTypeHandler(typeof(int))))
			{
				data.unityData.RecordChange(path, typeof(int), array.array.Length, newSize);
			}

			r.y += r.height;

			using (data.CreateLayerChildScope())
			{
				int	i = 0;

				foreach (object item in array.array)
				{
					// Add new drawer for new element.
					if (this.subDrawers.Count <= i)
						this.subDrawers.Add(TypeHandlerDrawersManager.CreateTypeHandlerDrawer(this.subHandler, this.subType));

					float	height = this.subDrawers[i].GetHeight(item);

					if (r.y + height <= data.Inspector.ScrollPosition.y)
					{
						r.y += height;
						++i;
						continue;
					}

					if (item != null)
						this.subDrawers[i].Draw(r, data.DrawChild(i.ToCachedString(), "Element " + i, item));
					else
						EditorGUI.LabelField(r, i.ToCachedString(), "Null");

					r.y += height;
					if (r.y - data.Inspector.ScrollPosition.y > data.Inspector.BodyRect.height)
					{
						// Override i to prevent removing unwanted subDrawers.
						i = int.MaxValue;
						break;
					}

					++i;
				}

				// Drawer are linked to their item, therefore they must be removed as their item is removed.
				if (i < this.subDrawers.Count)
					this.subDrawers.RemoveRange(i, this.subDrawers.Count - i);
			}

			--EditorGUI.indentLevel;
		}
	}
}