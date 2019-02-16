using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	public class PrefabField
	{
		public string					name;
		public AssetImportParameters	importParameters;
		public List<PrefabField>		children;
		public bool						isSupported;

		public bool	open = false;

		public	PrefabField(string name, AssetImportParameters importParameters)
		{
			this.name = Utility.NicifyVariableName(name);
			this.importParameters = importParameters;
			this.isSupported = this.importParameters.isSupported;
		}

		public	PrefabField(string name)
		{
			this.name = Utility.NicifyVariableName(name);
		}

		public void	UpdateIsSupported()
		{
			if (this.importParameters != null && this.importParameters.isSupported == true)
			{
				this.isSupported = true;
				return;
			}

			if (this.children != null)
			{
				for (int i = 0; i < this.children.Count; i++)
				{
					if (this.children[i].isSupported == true)
					{
						this.isSupported = true;
						break;
					}

					this.children[i].UpdateIsSupported();
					if (this.children[i].isSupported == true)
					{
						this.isSupported = true;
						break;
					}
				}
			}
		}

		public void	AddChild(PrefabField child)
		{
			this.open = true;

			if (this.children == null)
				this.children = new List<PrefabField>();
			this.children.Add(child);
		}

		public float	GetHeight(ImportAssetsWindow importAssetsWindow, NGRemoteHierarchyWindow hierarchy)
		{
			if (hierarchy.displayNonSuppported == false && this.importParameters.isSupported == false)
				return 0F;

			float	height = Constants.SingleLineHeight;

			if (importAssetsWindow.selectedField == this)
				height += this.importParameters.GetHeight();

			if (this.children != null)
			{
				for (int i = 0; i < this.children.Count; i++)
					height += this.children[i].GetHeight(importAssetsWindow, hierarchy);
			}

			return height;
		}

		public void	DrawField(Rect r, ImportAssetsWindow importAssetsWindow, NGRemoteHierarchyWindow hierarchy)
		{
			if (hierarchy.displayNonSuppported == false && this.importParameters.isSupported == false)
				return;

			//Rect	r = GUILayoutUtility.GetRect(0F, Constants.SingleLineHeight);
			float	x = r.x;
			float	w = r.width;

			r.width = EditorGUIUtility.labelWidth;
			r.height = Constants.SingleLineHeight;
			if (this.children != null)
				this.open = EditorGUI.Foldout(r, this.open, this.name, false);
			else
				EditorGUI.LabelField(r, this.name);
			r.x += r.width;

			r.width = w - r.width;
			if (Event.current.type == EventType.MouseDown &&
				r.Contains(Event.current.mousePosition) == true)
			{
				importAssetsWindow.selectedField = this;
				importAssetsWindow.Repaint();
			}

			if (this.importParameters.finalized == true)
			{
				if (this.importParameters.finalObject != null)
					EditorGUI.ObjectField(r, this.importParameters.finalObject, this.importParameters.realType, false);
				else
					EditorGUI.LabelField(r, "Null");
			}
			else
			{
				if (this.importParameters.isSupported == false)
					EditorGUI.LabelField(r, "Not supported.");
				else if (this.importParameters.importErrorMessage != null)
					EditorGUI.HelpBox(r, "Import failed : " + this.importParameters.importErrorMessage, MessageType.Error);
				else if (this.importParameters.ParametersConfirmed == true)
				{
					if (this.importParameters.totalBytes > 0)
					{
						if (importAssetsWindow.selectedField == this)
							EditorGUI.LabelField(r, "Downloading");
						else
							EditorGUI.LabelField(r, "Downloading (" + ((float)this.importParameters.bytesReceived / (float)this.importParameters.totalBytes * 100F).ToString("0.0") + "%)");

						importAssetsWindow.Repaint();
					}
					else
						EditorGUI.LabelField(r, "Waiting");
				}
				else
					EditorGUI.LabelField(r, new GUIContent("Need attention /!\\", UtilityResources.WarningIcon));
			}

			if (importAssetsWindow.selectedField == this)
			{
				r.x = x + 3F;
				r.width = w - 3F;

				Rect	r2 = r;
				r2.x -= 1F;
				r2.width = 5F;
				r2.height = 1F;
				EditorGUI.DrawRect(r2, Color.green);

				r.height += this.importParameters.GetHeight();

				r2.height = r.height;
				r2.width = 1F;
				EditorGUI.DrawRect(r2, Color.green);

				r2.width = 5F;
				r2.y += r2.height;
				r2.height = 1F;
				EditorGUI.DrawRect(r2, Color.green);

				//r.height = r2.height - Constants.SingleLineHeight;
				r.y += Constants.SingleLineHeight;

				this.importParameters.DrawAssetImportParams(r, importAssetsWindow);
			}

			if (this.open == true)
			{
				r.xMin += 15F;
				r.y += r.height;

				//++EditorGUI.indentLevel;
				for (int i = 0; i < this.children.Count; i++)
				{
					r.height = this.children[i].GetHeight(importAssetsWindow, hierarchy);
					this.children[i].DrawField(r, importAssetsWindow, hierarchy);
					r.y += r.height;
				}
				//--EditorGUI.indentLevel;
			}
		}
	}
}