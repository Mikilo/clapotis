using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;

namespace NGToolsEditor.NGConsole
{
	public static class	ConsoleUtility
	{
		internal static FastFileCache	files = new FastFileCache();
		internal static FastClassCache	classes = new FastClassCache();

		private static Type			ContainerWindow = UnityAssemblyVerifier.TryGetType(typeof(EditorWindow).Assembly, "UnityEditor.ContainerWindow");
		private static Type			View = UnityAssemblyVerifier.TryGetType(typeof(EditorWindow).Assembly, "UnityEditor.View");
		private static Type			DockArea = UnityAssemblyVerifier.TryGetType(typeof(EditorWindow).Assembly, "UnityEditor.DockArea");
		private static PropertyInfo	windows;
		private static PropertyInfo	mainView;
		private static PropertyInfo	allChildren;
		private static FieldInfo	m_Panes;
		private static MethodInfo	AddTab;

		static	ConsoleUtility()
		{
			if (ConsoleUtility.ContainerWindow != null)
			{
				ConsoleUtility.windows = UnityAssemblyVerifier.TryGetProperty(ConsoleUtility.ContainerWindow, "windows", BindingFlags.Static | BindingFlags.Public);
				// After 5.5, they renamed mainView by rootView.
				ConsoleUtility.mainView = ConsoleUtility.ContainerWindow.GetProperty("mainView", BindingFlags.Instance | BindingFlags.Public) ?? UnityAssemblyVerifier.TryGetProperty(ConsoleUtility.ContainerWindow, "rootView", BindingFlags.Instance | BindingFlags.Public);
			}

			if (ConsoleUtility.View != null)
				ConsoleUtility.allChildren = UnityAssemblyVerifier.TryGetProperty(ConsoleUtility.View, "allChildren", BindingFlags.Instance | BindingFlags.Public);

			if (ConsoleUtility.DockArea != null)
			{
				ConsoleUtility.m_Panes = UnityAssemblyVerifier.TryGetField(ConsoleUtility.DockArea, "m_Panes", BindingFlags.Instance | BindingFlags.NonPublic);
				ConsoleUtility.AddTab = ConsoleUtility.DockArea.GetMethod("AddTab", new Type[] { typeof(EditorWindow) }) ?? UnityAssemblyVerifier.TryGetMethod(ConsoleUtility.DockArea, "AddTab", new Type[] { typeof(EditorWindow), typeof(Boolean) });
			}
		}

		public static void	OpenModuleInWindow(NGConsoleWindow console, Module module, bool focus = false)
		{
			ModuleWindow	moduleWindow = EditorWindow.CreateInstance<ModuleWindow>();

			moduleWindow.Init(console, module);

			if (ConsoleUtility.windows != null && ConsoleUtility.mainView != null && ConsoleUtility.allChildren != null && ConsoleUtility.m_Panes != null && ConsoleUtility.AddTab != null)
			{
				Array	array = ConsoleUtility.windows.GetValue(null, null) as Array;

				foreach (object w in array)
				{
					Array	children = ConsoleUtility.allChildren.GetValue(ConsoleUtility.mainView.GetValue(w, null), null) as Array;

					foreach (object c in children)
					{
						try
						{
							List<EditorWindow>	panesCasted = ConsoleUtility.m_Panes.GetValue(c) as List<EditorWindow>;

							if (panesCasted.Exists((EditorWindow pane) => pane.GetType() == console.GetType()))
							{
								try
								{
									ConsoleUtility.AddTab.Invoke(c, new object[] { moduleWindow });
								}
								catch
								{
									// Changed since Unity 2018.3.
									ConsoleUtility.AddTab.Invoke(c, new object[] { moduleWindow, true });
								}

								moduleWindow.Show();
								if (focus == false)
									console.Focus();
								return;
							}
						}
						catch
						{
						}
					}
				}
			}

			moduleWindow.Show();
			if (focus == false)
				console.Focus();
		}

		/// <summary>
		/// Colors keywords given by StackSettings.
		/// </summary>
		/// <param name="line"></param>
		/// <returns></returns>
		public static string	ColorLine(string line)
		{
			StackTraceSettings	settings = HQ.Settings.Get<StackTraceSettings>();
			StringBuilder		buffer = Utility.GetBuffer();

			buffer.Append(' ');
			buffer.AppendStartColor(settings.previewTextColor);
			for (int i = 0; i < line.Length; i++)
			{
				int	previousKeywordColor = -1;

				// Convert tab to spaces.
				if (line[i] == '	' &&
					settings.displayTabAsSpaces > 0)
				{
					buffer.Append(' ', settings.displayTabAsSpaces);
					continue;
				}
				// Color only visible char.
				else if (line[i] != ' ')
				{
					for (int j = 0; j < settings.keywords.Length; ++j)
					{
						for (int k = 0; k < settings.keywords[j].keywords.Length; k++)
						{
							if (Utility.Compare(line, settings.keywords[j].keywords[k], i) == true)
							{
								// Save some color tags.
								if (previousKeywordColor != -1 &&
									settings.keywords[previousKeywordColor].color != settings.keywords[j].color)
								{
									previousKeywordColor = j;
									buffer.AppendEndColor();
									buffer.AppendStartColor(settings.keywords[j].color);
								}
								else if (previousKeywordColor == -1)
								{
									previousKeywordColor = j;
									buffer.AppendStartColor(settings.keywords[j].color);
								}

								buffer.Append(settings.keywords[j].keywords[k]);
								i += settings.keywords[j].keywords[k].Length - 1;
								goto skip;
							}
						}
					}
				}

				buffer.Append(line[i]);
				skip:

				if (previousKeywordColor != -1)
					buffer.AppendEndColor();

				continue;
			}

			buffer.AppendEndColor();

			return Utility.ReturnBuffer(buffer);
		}
	}
}