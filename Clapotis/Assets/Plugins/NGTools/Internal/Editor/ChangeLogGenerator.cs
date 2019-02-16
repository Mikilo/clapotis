using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

namespace NGToolsEditor.Internal
{
	using UnityEngine;

	public class ChangeLogGenerator : EditorWindow
	{
		public const string	Title = "Change Log Generator";

		public string	path;

		[MenuItem(Constants.PackageTitle + "/Internal/" + ChangeLogGenerator.Title)]
		private static void	Open()
		{
			Utility.OpenWindow<ChangeLogGenerator>(ChangeLogGenerator.Title);
		}

		protected virtual void	OnEnable()
		{
			Utility.LoadEditorPref(this);
		}

		protected virtual void	OnDisable()
		{
			Utility.SaveEditorPref(this);
		}

		protected virtual void	OnGUI()
		{
			this.path = NGEditorGUILayout.OpenFolderField("Path", this.path);
			if (GUILayout.Button("Generate") == true)
			{
				this.Aaa();
			}
		}

		private void	Aaa()
		{
			Dictionary<string, string>	versionsDates = new Dictionary<string, string>();
			Dictionary<string, Dictionary<string, List<string>>>	versions = new Dictionary<string, Dictionary<string, List<string>>>();
			string[]	files = Directory.GetFiles(this.path, "changelog_*");

			for (int i = 0; i < files.Length; i++)
			{
				Dictionary<string, List<string>>	modules = new Dictionary<string, List<string>>();
				string[]	lines = File.ReadAllLines(files[i]);
				string		module = null;
				string		version = lines[0].Substring("Version ".Length, lines[0].Length - "Version ".Length - 1);

				versions.Add(version, modules);
				DateTime	date = File.GetLastWriteTime(files[i]);
				versionsDates[version] = date.Year + "/" + date.Month + "/" + date.Day;

				for (int j = 2; j < lines.Length; j++)
				{
					if (lines[j].Length < 3)
						continue;

					if (lines[j][0] == '[')
						module = lines[j].Substring(1, lines[j].Length - 2);
					else if (lines[j][0] == '-')
					{
						List<string>	commits;

						if (modules.TryGetValue(module, out commits) == false)
						{
							commits = new List<string>();
							modules.Add(module, commits);
						}

						commits.Add(lines[j]);
					}
				}
			}

			foreach (HQ.ToolAssemblyInfo tool in HQ.EachTool)
			{
				if (tool.name == "NG Licenses")
					continue;

				bool			hasEditor = File.Exists(HQ.RootPath + "/" + tool.name.Replace(" ", "") + "/Editor/NGAssemblyInfo.cs");
				StringBuilder	buffer = Utility.GetBuffer();

				if (tool.name == "NG Core")
					buffer.Append("namespace NGTools");
				else
				{
					buffer.Append(@"namespace NGTools");
					if (hasEditor == true)
						buffer.Append("Editor");
					buffer.Append('.');
					buffer.Append(tool.name.Replace(" ", ""));
				}

				buffer.Append(@"
{
	partial class NGAssemblyInfo
	{
");
				buffer.Append(@"		public static readonly string[]	ChangeLog = { ");

				int	j = 0;

				foreach (KeyValuePair<string, Dictionary<string, List<string>>> version in versions)
				{
					if (version.Value.ContainsKey(tool.name) == true)
					{
						List<string>	commits = version.Value[tool.name];

						if (commits != null)
						{
							if (j++ > 0)
								buffer.Append(", ");

							buffer.Append("\r\n\t\t\t\"");
							buffer.Append(version.Key);
							buffer.Append("\", \"");
							buffer.Append(versionsDates[version.Key]);
							buffer.Append("\", @\"");

							int	i = 0;

							foreach (string commit in commits)
							{
								if (i++ > 0)
									buffer.AppendLine();
								buffer.Append(commit.Substring(2).Replace("\"", "\"\""));
							}

							buffer.Append("\"");
						}
					}
				}

				buffer.Append(@"
		};");

				/*buffer.Append(@"
		static	NGChangeLog()
		{
");
				foreach (KeyValuePair<string, Dictionary<string, List<string>>> version in versions)
				{
					if (version.Value.ContainsKey(tool.name) == true)
					{
						List<string>	commits = version.Value[tool.name];

						if (commits != null)
						{
							buffer.Append(@"			NGChangeLogWindow.AddChangeLog(NGAssemblyInfo.Name, """);
							buffer.Append(version.Key);
							buffer.Append(@""", """);
							buffer.Append(versionsDates[version.Key]);
							buffer.Append(@""", @""");

							int	i = 0;

							foreach (string commit in commits)
							{
								if (i++ > 0)
									buffer.AppendLine();
								buffer.Append(commit.Substring(2).Replace("\"", "\"\""));
							}

							buffer.AppendLine("\");");
						}
					}
				}*/

				buffer.Append(@"
	}
}");

				if (buffer.Length > 0)
				{
					Directory.CreateDirectory(HQ.RootPath + "/" + tool.name.Replace(" ", "") + "/Editor");

					if (hasEditor == false)
					{
						File.WriteAllText(HQ.RootPath + "/" + tool.name.Replace(" ", "") + "/NGAssemblyInfo_ChangeLog.cs", Utility.ReturnBuffer(buffer));
						Debug.Log("Generated " + HQ.RootPath + "/" + tool.name.Replace(" ", "") + "/NGAssemblyInfo_ChangeLog.cs");
					}
					else
					{
						File.WriteAllText(HQ.RootPath + "/" + tool.name.Replace(" ", "") + "/Editor/NGAssemblyInfo_ChangeLog.cs", Utility.ReturnBuffer(buffer));
						Debug.Log("Generated " + HQ.RootPath + "/" + tool.name.Replace(" ", "") + "/Editor/NGAssemblyInfo_ChangeLog.cs");
					}
				}
			}
		}
	}
}