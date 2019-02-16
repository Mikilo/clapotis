using NGTools.NGRemoteScene;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	internal sealed class ClassDrawer : TypeHandlerDrawer
	{
		private BgColorContentAnimator	anim;
		private TypeHandlerDrawer[]		fieldDrawers;
		private bool					fold;

		public	ClassDrawer(TypeHandler typeHandler) : base(typeHandler)
		{
		}

		public override float	GetHeight(object value)
		{
			ClientClass	genericClass = value as ClientClass;

			if (genericClass.fields == null ||
				this.fold == false)
			{
				return Constants.SingleLineHeight;
			}

			float	height = 16F;

			if (this.fieldDrawers == null)
				this.InitDrawers(genericClass);

			for (int i = 0; i < this.fieldDrawers.Length; i++)
				height += this.fieldDrawers[i].GetHeight(genericClass.fields[i].value);

			return height;
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			if (this.anim == null)
				this.anim = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);

			ClientClass	genericClass = data.Value as ClientClass;

			r.height = Constants.SingleLineHeight;

			if (genericClass.fields == null)
			{
				EditorGUI.LabelField(r, data.Name, "Null");
				return;
			}

			string	path = data.GetPath();
			if (data.Inspector.Hierarchy.GetUpdateNotification(path) != NotificationPath.None)
				this.anim.Start();

			using (this.anim.Restorer(0F, .8F + this.anim.Value, 0F, 1F))
			{
				--EditorGUI.indentLevel;
				r.x += 3F;
				this.fold = EditorGUI.Foldout(r, this.fold, data.Name, true);
				r.x -= 3F;
				++EditorGUI.indentLevel;
			}

			if (this.fold == false)
				return;

			r.y += r.height;

			if (this.fieldDrawers == null)
				this.InitDrawers(genericClass);

			using (data.CreateLayerChildScope())
			{
				++EditorGUI.indentLevel;
				for (int i = 0; i < this.fieldDrawers.Length; i++)
				{
					r.height = this.fieldDrawers[i].GetHeight(genericClass.fields[i].value);

					if (r.y + r.height <= data.Inspector.ScrollPosition.y)
					{
						r.y += r.height;
						continue;
					}

					this.fieldDrawers[i].Draw(r, data.DrawChild(genericClass.fields[i].name, genericClass.fields[i].value));

					r.y += r.height;
					if (r.y - data.Inspector.ScrollPosition.y > data.Inspector.BodyRect.height)
						break;
				}
				--EditorGUI.indentLevel;
			}
		}

		private void	InitDrawers(ClientClass genericClass)
		{
			if (genericClass.fields == null)
				this.fieldDrawers = new TypeHandlerDrawer[0];
			else
			{
				this.fieldDrawers = new TypeHandlerDrawer[genericClass.fields.Length];

				for (int i = 0; i < genericClass.fields.Length; i++)
					this.fieldDrawers[i] = TypeHandlerDrawersManager.CreateTypeHandlerDrawer(genericClass.fields[i].handler, genericClass.fields[i].fieldType);
			}
		}
	}
}