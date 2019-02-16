using System;
using System.Collections.Generic;
using UnityEditor;

namespace NGToolsEditor
{
	public sealed class IndentLevelRestorer : IDisposable
	{
		private static Dictionary<int, IndentLevelRestorer>	cached = new Dictionary<int, IndentLevelRestorer>();

		private int	lastLevel;

		public static IndentLevelRestorer	Get(int level)
		{
			IndentLevelRestorer	restorer;

			if (IndentLevelRestorer.cached.TryGetValue(level, out restorer) == false)
			{
				restorer = new IndentLevelRestorer(level);

				IndentLevelRestorer.cached.Add(level, restorer);
			}
			else
			{
				restorer.lastLevel = EditorGUI.indentLevel;
				EditorGUI.indentLevel = level;
			}

			return restorer;
		}

		private	IndentLevelRestorer(int level)
		{
			this.lastLevel = EditorGUI.indentLevel;
			EditorGUI.indentLevel = level;
		}

		public void	Dispose()
		{
			EditorGUI.indentLevel = this.lastLevel;
		}
	}
}