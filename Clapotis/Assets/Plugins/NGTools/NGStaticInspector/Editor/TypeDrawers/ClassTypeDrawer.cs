using NGTools;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGStaticInspector
{
	public class ClassTypeDrawer : TypeDrawer
	{
		public const float	ExceptionHeight = 40F;

		private MemberDrawer[]	members;
		private bool			isExpanded;

		public	ClassTypeDrawer(string path, string label, Type type) : base(path, label, type)
		{
			this.isExpanded = NGEditorPrefs.GetBool(path, this.isExpanded);
		}

		public override float	GetHeight(object instance)
		{
			float	h = Constants.SingleLineHeight;

			if (this.isExpanded == true)
			{
				this.InitMembers();

				if (instance != null)
				{
					for (int i = 0; i < this.members.Length; i++)
					{
						try
						{
							if (this.members[i].exception != null)
								h += 2F + 16F;
							else
								h += 2F + this.members[i].typeDrawer.GetHeight(this.members[i].fieldModifier.GetValue(instance));
						}
						catch (Exception ex)
						{
							this.members[i].exception = ex;
						}
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
				if (instance != null)
				{
					this.InitMembers();

					try
					{
						++EditorGUI.indentLevel;

						for (int i = 0; i < this.members.Length; i++)
						{
							r.y += r.height + 2F;

							if (this.members[i].exception != null)
							{
								r.height = 16F;
								using (ColorContentRestorer.Get(Color.red))
									EditorGUI.LabelField(r, this.members[i].fieldModifier.Name, "Property raised an exception");
								continue;
							}

							try
							{
								r.height = this.members[i].typeDrawer.GetHeight(this.members[i].fieldModifier.GetValue(instance));

								if (Event.current.type == EventType.MouseDown &&
									r.Contains(Event.current.mousePosition) == true &&
									Event.current.button == 1)
								{
									NGStaticInspectorWindow.forceMemberEditable = this.members[i];

									EditorWindow	window = EditorWindow.mouseOverWindow;
									if (window != null)
										window.Repaint();

									Utility.RegisterIntervalCallback(() =>
									{
										NGStaticInspectorWindow.forceMemberEditable = null;
									}, NGStaticInspectorWindow.ForceMemberEditableTickDuration, 1);
								}

								bool	enabled = GUI.enabled;
								GUI.enabled = !(!this.members[i].isEditable && NGStaticInspectorWindow.forceMemberEditable != this.members[i]);
								EditorGUI.BeginChangeCheck();
								object	value = this.members[i].typeDrawer.OnGUI(r, this.members[i].fieldModifier.GetValue(instance));
								if (EditorGUI.EndChangeCheck() == true)
								{
									if (this.members[i].isEditable == true)
										this.members[i].fieldModifier.SetValue(instance, value);
									else
										GUI.changed = false;
								}
								GUI.enabled = enabled;
							}
							catch (Exception ex)
							{
								this.members[i].exception = ex;
							}
						}
					}
					finally
					{
						--EditorGUI.indentLevel;
					}
				}
			}

			return instance;
		}

		private void	InitMembers()
		{
			if (this.members != null)
				return;

			List<MemberDrawer>		list = new List<MemberDrawer>();
			FieldInfo[]		fields = this.type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			PropertyInfo[]	properties = this.type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			for (int i = 0; i < fields.Length; i++)
				list.Add(new MemberDrawer(TypeDrawerManager.GetDrawer(this.path + '.' + fields[i].Name, fields[i].Name, fields[i].FieldType), new FieldModifier(fields[i])));

			for (int i = 0; i < properties.Length; i++)
			{
				if (properties[i].GetGetMethod() != null && properties[i].GetIndexParameters().Length == 0)
				{
					string	niceName = Utility.NicifyVariableName(properties[i].Name);
					int		j = 0;

					for (; j < list.Count; j++)
					{
						if (list[j].typeDrawer.label == niceName)
							break;
					}

					if (j == list.Count)
						list.Add(new MemberDrawer(TypeDrawerManager.GetDrawer(this.path + '.' + properties[i].Name, properties[i].Name, properties[i].PropertyType), new PropertyModifier(properties[i])));
				}
			}

			this.members = list.ToArray();
		}
	}
}