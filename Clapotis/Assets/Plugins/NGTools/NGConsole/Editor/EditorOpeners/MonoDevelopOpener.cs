using System.IO;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	internal sealed class MonoDevelopOpener : IEditorOpener
	{
		private const string	MonoDevelopExePath = @"C:\Program Files\Unity\MonoDevelop\bin\MonoDevelop.exe";

		public string	defaultArguments { get { return "--nologo \"$(File);$(Line)\""; } }

		public bool	CanHandleEditor(string editor)
		{
			return string.IsNullOrEmpty(editor) || editor.Contains("MonoDevelop");
		}

		public void	Open(string editorPath, string arguments, string file, int line)
		{
			if (string.IsNullOrEmpty(editorPath) == true)
			{
				if (Application.platform == RuntimePlatform.WindowsEditor)
				{
					// Try to use the default executable.
					if (File.Exists(MonoDevelopOpener.MonoDevelopExePath) == true)
						Utility.OpenFileLine(MonoDevelopOpener.MonoDevelopExePath, arguments.Replace("$(File)", file).Replace("$(Line)", line.ToString()));
					else
					{
						TextAsset	fileAsset = AssetDatabase.LoadAssetAtPath(file, typeof(TextAsset)) as TextAsset;
						AssetDatabase.OpenAsset(fileAsset, line);
					}
				}
				else
				{
					TextAsset	fileAsset = AssetDatabase.LoadAssetAtPath(file, typeof(TextAsset)) as TextAsset;
					AssetDatabase.OpenAsset(fileAsset, line);
				}
			}
			else
				Utility.OpenFileLine(editorPath, arguments.Replace("$(File)", file).Replace("$(Line)", line.ToString()));
		}
	}
}