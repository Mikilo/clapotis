using NGTools;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.Internal
{
	public class VersionsManager : EditorWindow
	{
		public const string	Title = "Versions Manager";

		private List<string>	names = new List<string>();
		private List<string>	versions = new List<string>();
		private List<string>	paths = new List<string>();
		private Vector2			scrollPosition;

		[MenuItem(Constants.PackageTitle + "/Internal/" + VersionsManager.Title)]
		private static void	Open()
		{
			Utility.OpenWindow<VersionsManager>(VersionsManager.Title);
		}

		protected virtual void	OnEnable()
		{
			this.names.Clear();
			this.versions.Clear();
			this.paths.Clear();

			foreach (HQ.ToolAssemblyInfo tool in HQ.EachTool)
			{
				this.names.Add(tool.name);
				this.versions.Add(tool.version);
				this.paths.Add(this.GetFilePath(tool.name));
			}
		}

		protected virtual void	OnGUI()
		{
			bool	hasDiff = false;

			this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition);
			{
				using (LabelWidthRestorer.Get(170F))
				{
					foreach (HQ.ToolAssemblyInfo tool in HQ.EachTool)
					{
						int	i = this.names.IndexOf(tool.name);

						if (i == -1)
							continue;

						if (this.versions[i] != tool.version)
							hasDiff = true;

						EditorGUILayout.BeginHorizontal();
						{
							EditorGUILayout.PrefixLabel(this.names[i]);
							GUILayout.Label(tool.version);
							this.versions[i] = EditorGUILayout.TextField(this.versions[i]);

							if (this.paths[i] != string.Empty)
							{
								if (GUILayout.Button("Open") == true)
									EditorUtility.OpenWithDefaultApp(this.paths[i]);
							}
						}
						EditorGUILayout.EndHorizontal();
					}
				}
			}
			GUILayout.EndScrollView();

			GUI.enabled = hasDiff;
			if (GUILayout.Button("Update") == true)
			{
				StringBuilder	buffer = Utility.GetBuffer();

				foreach (HQ.ToolAssemblyInfo tool in HQ.EachTool)
				{
					int	i = this.names.IndexOf(tool.name);

					if (i == -1 || this.versions[i] == tool.version)
						continue;

					if (this.paths[i] != string.Empty)
					{
						string[]	lines = File.ReadAllLines(this.paths[i]);

						for (int j = 0; j < lines.Length; j++)
						{
							if (lines[j].Contains("public const string	Version") == true)
							{
								lines[j] = lines[j].Replace(tool.version, this.versions[i]);
								break;
							}
						}

						for (int j = 0; j < lines.Length; j++)
						{
							if (j > 0)
								buffer.AppendLine();
							buffer.Append(lines[j]);
						}

						File.WriteAllText(this.paths[i], buffer.ToString());
						buffer.Length = 0;
						InternalNGDebug.Log("Updated \"" + this.paths[i] + "\" with version \"" + this.versions[i] + "\".");
					}
				}

				Utility.RestoreBuffer(buffer);
			}
		}

		private string	GetFilePath(string toolName)
		{
			string	path = Path.Combine(HQ.RootPath, Path.Combine(toolName.Replace(" ", ""), "NGAssemblyInfo.cs"));
			bool	found = File.Exists(path);

			if (found == false)
			{
				path = Path.Combine(HQ.RootPath, Path.Combine(toolName.Replace(" ", ""), "Editor/NGAssemblyInfo.cs"));
				found = File.Exists(path);
			}

			if (found == true)
				return path;
			return string.Empty;
		}
	}
}