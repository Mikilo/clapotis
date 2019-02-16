using NGTools;
using System;
using System.Collections.Generic;
using UnityEditor;

namespace NGToolsEditor
{
	using UnityEngine;

	public class ExportSettingsWizard : ScriptableWizard
	{
		[File(FileAttribute.Mode.Save, "")]
		public string	exportFile;

		private SettingsExporter.Node	root;
		private List<object>			instances;
		private Vector2					scrollPosition;
		private GUIStyle				richTextField;

		protected virtual void	OnEnable()
		{
			this.instances = new List<object>();

			foreach (Type type in Utility.EachAllSubClassesOf(typeof(EditorWindow), (Type t) => t.IsDefined(typeof(ExportableAttribute), false)))
			{
				Object[]	instances = Resources.FindObjectsOfTypeAll(type);
				if (instances.Length > 0)
					this.instances.Add(instances[0]);
			}

			this.root = SettingsExporter.Collect(this.instances.ToArray());
			//this.OutputNode(this.root);
			Utility.LoadEditorPref(this);
		}

		protected virtual void	OnDestroy()
		{
			Utility.SaveEditorPref(this);
		}

		protected virtual void	OnGUI()
		{
			if (this.richTextField == null)
			{
				this.richTextField = new GUIStyle(GUI.skin.textField);
				this.richTextField.richText = true;
			}

			this.DrawWizardGUI();

			using (BgColorContentRestorer.Get(GeneralStyles.HighlightActionButton))
			{
				if (GUILayout.Button(LC.G("ExportSettings_Export")) == true)
				{
					if (SettingsExporter.Export(this.instances, this.root, exportFile) == true)
						InternalNGDebug.Log(LC.G("ExportSettings_ExportSuccess"));
					else
						InternalNGDebug.LogError(LC.G("ExportSettings_ExportFailed"));
				}
			}

			this.scrollPosition = EditorGUILayout.BeginScrollView(this.scrollPosition);
			this.DrawNode(this.root);

			EditorGUILayout.EndScrollView();
		}

		private void	DrawNode(SettingsExporter.Node node)
		{
			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Space((GUI.depth - 1) * 16F);

				node.include = GUILayout.Toggle(node.include, Utility.NicifyVariableName(node.name));
				if (node.value != null)
					GUILayout.Label("<color=cyan>" + node.value + "</color>", this.richTextField);

				GUILayout.FlexibleSpace();
			}
			EditorGUILayout.EndHorizontal();

			++GUI.depth;
			for (int i = 0; i < node.children.Count; i++)
			{
				if (node.children[i].options == SettingsExporter.Node.Option.Normal)
				{
					EditorGUI.BeginDisabledGroup(GUI.enabled == false || node.include == false);
					{
						this.DrawNode(node.children[i]);
					}
					EditorGUI.EndDisabledGroup();
				}
			}
			--GUI.depth;
		}

		private void	OutputNode(SettingsExporter.Node node, int depth = 0)
		{
			Debug.Log(new string('	', depth) + node.name + "=" + node.value);

			for (int i = 0; i < node.children.Count; i++)
				this.OutputNode(node.children[i], depth + 1);
		}
	}
}