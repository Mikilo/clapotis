using NGTools;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGStaticInspector
{
	public class CollectionTypeDrawer : TypeDrawer
	{
		private List<TypeDrawer>	drawers = new List<TypeDrawer>();
		private bool				isExpanded;

		public	CollectionTypeDrawer(string path, string label, Type type) : base(path, label, type)
		{
			this.isExpanded = NGEditorPrefs.GetBool(path, this.isExpanded);
		}

		public override float	GetHeight(object instance)
		{
			float	h = Constants.SingleLineHeight;

			if (this.isExpanded == true)
			{
				ICollectionModifier	collection = NGTools.Utility.GetCollectionModifier(instance);

				if (collection != null)
				{
					h += 2F + Constants.SingleLineHeight;

					try
					{
						Type	subType = collection.SubType;

						while (this.drawers.Count < collection.Size)
							this.drawers.Add(TypeDrawerManager.GetDrawer(this.path + '.' + this.drawers.Count.ToCachedString(), "Element " + this.drawers.Count.ToCachedString(), subType));

						for (int i = 0; i < collection.Size; i++)
							h += 2F + this.drawers[i].GetHeight(collection.Get(i));

					}
					finally
					{
						NGTools.Utility.ReturnCollectionModifier(collection);
					}
				}
			}

			return h;
		}

		public override object	OnGUI(Rect r, object instance)
		{
			r.height = Constants.SingleLineHeight;

			--EditorGUI.indentLevel;
			r.x += 3F;
			if (instance == null)
			{
				EditorGUI.BeginChangeCheck();
				EditorGUI.Foldout(r, false, this.label + " (Null)", false);
				if (EditorGUI.EndChangeCheck() == true)
					GUI.changed = false;
			}
			else
			{
				EditorGUI.BeginChangeCheck();
				this.isExpanded = EditorGUI.Foldout(r, this.isExpanded, this.label, true);
				if (EditorGUI.EndChangeCheck() == true)
				{
					NGEditorPrefs.SetBool(path, this.isExpanded);
					GUI.changed = false;
				}
			}
			r.x -= 3F;
			++EditorGUI.indentLevel;

			if (this.isExpanded == true)
			{
				r.y += r.height + 2F;
				++EditorGUI.indentLevel;

				ICollectionModifier	collection = NGTools.Utility.GetCollectionModifier(instance);

				if (collection != null)
				{
					try
					{
						Type	subType = collection.SubType;

						EditorGUI.BeginChangeCheck();
						int	size = EditorGUI.DelayedIntField(r, "Size", collection.Size);
						if (EditorGUI.EndChangeCheck() == true)
						{
							size = Mathf.Max(0, size);

							if (collection.Size != size)
							{
								--EditorGUI.indentLevel;

								if (instance is Array)
								{
									Array	newArray = Array.CreateInstance(subType, size);

									Array.Copy(instance as Array, newArray, Mathf.Min(collection.Size, size));

									if (collection.Size > 0)
									{
										object	lastValue = collection.Get(collection.Size - 1);

										for (int i = collection.Size; i < size; i++)
											newArray.SetValue(lastValue, i);
									}

									instance = newArray;
								}
								else if (instance is IList)
								{
									IList	list = instance as IList;

									if (list.Count < size)
									{
										object	lastValue = list.Count > 0 ? list[list.Count - 1] : (subType.IsValueType == true ? Activator.CreateInstance(subType) : null);

										while (list.Count < size)
											list.Add(lastValue);
									}
									else
									{
										while (list.Count > size)
											list.RemoveAt(list.Count - 1);
									}

									instance = list;
								}
								else
									throw new NotImplementedException("Collection of type \"" + instance.GetType() + "\" is not supported.");
							}

							return instance;
						}

						while (this.drawers.Count < collection.Size)
							this.drawers.Add(TypeDrawerManager.GetDrawer(this.path + '.' + this.drawers.Count.ToCachedString(), "Element " + this.drawers.Count.ToCachedString(), subType));

						for (int i = 0; i < collection.Size; i++)
						{
							r.y += r.height + 2F;

							object	element = collection.Get(i);
							r.height = this.drawers[i].GetHeight(element);

							EditorGUI.BeginChangeCheck();
							object	value = this.drawers[i].OnGUI(r, element);
							if (EditorGUI.EndChangeCheck() == true)
								collection.Set(i, value);
						}
					}
					finally
					{
						NGTools.Utility.ReturnCollectionModifier(collection);
					}
				}

				--EditorGUI.indentLevel;
			}

			return instance;
		}
	}
}