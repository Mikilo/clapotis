using System.IO;
using System.Text;
using UnityEditor;

namespace NGToolsEditor.NGAssetFinder
{
	using UnityEngine;

	internal sealed class SceneMatch
	{
		public const float	ReplaceButtonWidth = 80F;

		public readonly Object	scene;
		public readonly string	scenePath;
		public int				count;
		public int				prefabCount;
		public int				prefabModificationCount;

		private string	label;
		private double	lastClick;

		public	SceneMatch(Object scene, string scenePath)
		{
			this.scene = scene;
			this.scenePath = scenePath;
		}

		public void	PrepareResults()
		{
			if (this.count == 0 && this.prefabCount == 0)
				this.label = this.scene.name + ": No reference";
			else
				this.label = this.scene.name + ": " + this.GetLabel();
		}

		public void	Draw(SearchResult result, NGAssetFinderWindow window, Rect r)
		{
			float	width = r.width;

			r.width = 24F;
			if (GUI.Button(r, GUIContent.none) == true)
			{
				if (Event.current.button == 1 || this.lastClick + Constants.DoubleClickTime > EditorApplication.timeSinceStartup)
				{
					Selection.activeObject = scene;
				}
				else
				{
					EditorGUIUtility.PingObject(scene);
					this.lastClick = EditorApplication.timeSinceStartup;
				}
			}
			GUI.DrawTexture(r, UtilityResources.UnityIcon, ScaleMode.ScaleToFit);
			r.x += r.width;

			r.width = width - r.x;
			if (window.canReplace == true)
				r.width -= SceneMatch.ReplaceButtonWidth;

			GUI.Label(r, this.label);
			r.x += r.width;

			if (window.canReplace == true)
			{
				EditorGUI.BeginDisabledGroup(this.count == 0 && this.prefabCount == 0 && this.prefabModificationCount == 0);
				{
					Utility.content.text = "Replace";
					r.width = SceneMatch.ReplaceButtonWidth;
					if (GUI.Button(r, Utility.content) == true &&
						((Event.current.modifiers & Constants.ByPassPromptModifier) != 0 ||
						 EditorUtility.DisplayDialog(NGAssetFinderWindow.Title, "Replacing references in scene is undoable.\nAre you sure you want to replace " + this.GetLabel() + " from " + this.scene.name + "?", "Replace", "Cancel") == true))
					{
						this.ReplaceReferencesInScene(result, window);
					}
					r.x += r.width;
				}
				EditorGUI.EndDisabledGroup();
			}
		}

		public void	Export(StringBuilder buffer)
		{
			buffer.Append(this.scene.name);
			buffer.Append(": ");

			if (this.count == 0 && this.prefabCount == 0)
				buffer.AppendLine("No reference");
			else
				buffer.AppendLine(this.GetLabel());
		}

		public void	ReplaceReferencesInScene(SearchResult result, NGAssetFinderWindow window)
		{
			string	guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(window.targetAsset));
			string	id = Utility.GetLocalIdentifierFromObject(window.targetAsset) + ", guid: " + guid;
			if (string.IsNullOrEmpty(id) == true)
				return;

			string	newGuid = string.Empty;
			string	newID = null;
			if (window.replaceAsset != null)
			{
				newGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(window.replaceAsset));
				newID = Utility.GetLocalIdentifierFromObject(window.replaceAsset) + ", guid: " + newGuid;
			}

			if (this.prefabCount > 0 && newID == null)
			{
				if (EditorUtility.DisplayDialog(NGAssetFinderWindow.Title, "You are replacing prefabs with nothing.\nPrefabs will be destroyed and they will be replace with \"Missing Prefab\" in the scene.", "Continue", "Cancel") == false)
					return;
			}

			string[]	lines = File.ReadAllLines(this.scenePath);
			bool		searchingComponentType = (window.replaceAsset is Component || window.replaceAsset is MonoScript);

			for (int i = 0; i < lines.Length; i++)
			{
				string	line = lines[i];

				if (line.Length < 11 + 8 + 32 + 1) // {fileID: , guid: }
					continue;

				int	position = line.IndexOf(" {fileID: ");
				if (position == -1)
					continue;

				// References of scripts.
				if (line.StartsWith("  m_Script: {fileID: ") == true)
				{
					if (newID != null && searchingComponentType == true && line.IndexOf(id, "  m_Script: {fileID: ".Length) != -1)
					{
						lines[i] = line.Replace(id, newID);
						--this.count;
						++result.updatedReferencesCount;
						continue;
					}
				}
				// References of prefabs.
				else if (line.StartsWith("  m_ParentPrefab: {fileID: ") == true)
				{
					if (line.IndexOf(guid) != -1)
					{
						if (newID == null)
							lines[i] = line.Substring(0, line.IndexOf("{fileID: ")) + "{fileID: 0}";
						else
							lines[i] = line.Replace(guid, newGuid);
						--this.prefabCount;
						++result.updatedReferencesCount;
						continue;
					}
				}
				// Modifications of prefabs.
				else if (line.StartsWith("    - target: {fileID: ") == false)
				{
					if (newID == null)
						lines[i] = line.Substring(0, line.IndexOf("{fileID: ")) + "{fileID: 0}";
					else
						lines[i] = line.Replace(guid, newGuid);
					--this.prefabModificationCount;
					++result.updatedReferencesCount;
					continue;
				}
				// References in script.
				else if (line.IndexOf(id, 11) != -1)
				{
					if (newID == null)
						lines[i] = line.Substring(0, line.IndexOf("{fileID: ")) + "{fileID: 0}";
					else
						lines[i] = line.Replace(id, newID);
					--this.count;
					++result.updatedReferencesCount;
					continue;
				}
			}

			this.PrepareResults();

			AssetFinderCache.UpdateFile(this.scenePath);
			File.WriteAllLines(this.scenePath, lines);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
		}

		private string	GetLabel()
		{
			StringBuilder	buffer = Utility.GetBuffer();

			if (this.count > 0)
			{
				buffer.Append(this.count);
				buffer.Append(" reference");

				if (this.count > 1)
					buffer.Append('s');
			}

			if (this.prefabCount > 0)
			{
				if (buffer.Length > 0)
					buffer.Append(", ");

				buffer.Append(this.prefabCount);
				buffer.Append(" prefab");

				if (this.prefabCount > 1)
					buffer.Append('s');
			}

			if (this.prefabModificationCount > 0)
			{
				if (buffer.Length > 0)
					buffer.Append(", ");

				buffer.Append(this.prefabModificationCount);
				buffer.Append(" prefab change");

				if (this.prefabModificationCount > 1)
					buffer.Append('s');
			}

			return Utility.ReturnBuffer(buffer);
		}
	}
}