using NGLicenses;
using NGTools;
using System;
using System.IO;
using System.Text;
using UnityEditor;

namespace NGToolsEditor.NGConsole
{
	using UnityEngine;

	public static class RowUtility
	{
		public const int	LowestRowGoToLineAllowed = 2;

		public static double	LastKeyTime;
		public static double	LastClickTime;
		public static int		LastClickIndex;
		/// <summary>Is setup before OnGUI by NGConsole and ModuleEditorWindow. Use this variable to know which window is drawing.</summary>
		public static EditorWindow	drawingWindow = null;

		// Preview's fields.
		private static EditorWindow	previewEditorWindow;
		private static Rect			previewRect;
		private static Frame		previewFrame;
		private static string[]		previewLines;
		private static RowsDrawer	rowsDrawer;

		public static void	ClearPreview()
		{
			if (RowUtility.previewLines == null)
				return;

			RowUtility.previewEditorWindow = null;
			RowUtility.previewLines = null;
			RowUtility.previewFrame = null;
			RowUtility.rowsDrawer.AfterAllRows -= RowUtility.DrawPreview;
			RowUtility.rowsDrawer = null;
		}

		public static void	PreviewStackFrame(RowsDrawer rowsDrawer, Rect r, Frame frame)
		{
			if (frame.fileExist == true &&
				RowUtility.previewFrame != frame)
			{
				try
				{
					RowUtility.previewLines = ConsoleUtility.files.GetFile(frame.fileName);
					RowUtility.rowsDrawer = rowsDrawer;
					RowUtility.previewFrame = frame;
					RowUtility.previewRect = r;

					FilesWatcher.Watch(frame.fileName);

					RowUtility.previewEditorWindow = RowUtility.drawingWindow;
					RowUtility.rowsDrawer.AfterAllRows -= RowUtility.DrawPreview;
					RowUtility.rowsDrawer.AfterAllRows += RowUtility.DrawPreview;
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException(ex);
					RowUtility.ClearPreview();
				}
			}
		}

		private static void	DrawPreview(Rect bodyRect)
		{
			Rect	r = RowUtility.previewRect;

			r.x += bodyRect.x;
			r.y += bodyRect.y;

			// Out of window.
			if (EditorWindow.mouseOverWindow != RowUtility.previewEditorWindow)
			{
				RowUtility.ClearPreview();
				return;
			}
			// Out of stacktrace.
			//else if (Event.current.type == EventType.MouseMove)
			if (Event.current.type == EventType.MouseMove && r.Contains(Event.current.mousePosition) == false)
			{
				RowUtility.ClearPreview();
				return;
			}

			if (RowUtility.previewFrame.line <= RowUtility.previewLines.Length)
			{
				StackTraceSettings	stackTrace = HQ.Settings.Get<StackTraceSettings>();
				float				maxWidth = float.MinValue;

				r.x = Event.current.mousePosition.x + stackTrace.previewOffset.x;
				r.y = Event.current.mousePosition.y + stackTrace.previewOffset.y;
				r.width = RowUtility.previewRect.width;
				r.height = stackTrace.previewHeight;

				for (int i = RowUtility.previewFrame.line - stackTrace.previewLinesBeforeStackFrame - 1,
					 max = Mathf.Min(RowUtility.previewFrame.line + stackTrace.previewLinesAfterStackFrame, RowUtility.previewLines.Length);
					 i < max; i++)
				{
					Utility.content.text = RowUtility.previewLines[i];
					Vector2	size = stackTrace.PreviewSourceCodeStyle.CalcSize(Utility.content);
					if (size.x > maxWidth)
						maxWidth = size.x;
				}

				r.width = maxWidth;
				for (int i = RowUtility.previewFrame.line - stackTrace.previewLinesBeforeStackFrame - 1,
					 max = Mathf.Min(RowUtility.previewFrame.line + stackTrace.previewLinesAfterStackFrame, RowUtility.previewLines.Length);
					 i < max; i++)
				{
					if (Event.current.type == EventType.Repaint)
					{
						if (i + 1 != RowUtility.previewFrame.line)
							EditorGUI.DrawRect(r, stackTrace.previewSourceCodeBackgroundColor);
						else
							EditorGUI.DrawRect(r, stackTrace.previewSourceCodeMainLineBackgroundColor);
					}

					GUI.Label(r, RowUtility.previewLines[i], stackTrace.PreviewSourceCodeStyle);
					r.y += r.height;
				}

				RowUtility.drawingWindow.Repaint();
			}
		}

		public static void	GoToFileLine(string file, int line, bool focus)
		{
			GeneralSettings	general = HQ.Settings.Get<GeneralSettings>();

			if (file.StartsWith("Assets") == true)
				file = Path.Combine(Application.dataPath, file.Substring(7)).Replace('/', '\\');
			else
				file = Application.dataPath + '/' + file;

			if (general.openMode == GeneralSettings.ModeOpen.AssetDatabaseOpenAsset)
			{
				Object	sourceFile = AssetDatabase.LoadAssetAtPath(@"Assets\" + file.Substring(Application.dataPath.Length + 1), typeof(TextAsset));
				if (sourceFile != null)
					AssetDatabase.OpenAsset(sourceFile, line);
				else
					Debug.LogWarning(string.Format(LC.G("Console_AssetNotText"), @"Assets\" + file.Substring(Application.dataPath.Length + 1)));
			}
			else if (general.openMode == GeneralSettings.ModeOpen.NGConsoleOpener)
			{
				GeneralSettings.EditorExtensions	editorExtensions = null;
				string								editorPath = NGEditorPrefs.GetString(ConsoleConstants.ScriptDefaultApp);
				string								fileExtension = Path.GetExtension(file);

				if ((string.IsNullOrEmpty(editorPath) == true ||
					 editorPath == "internal") &&
					string.IsNullOrEmpty(fileExtension) == false)
				{
					fileExtension = fileExtension.Substring(1);
					for (int i = 0; i < general.editorExtensions.Length; i++)
					{
						for (int j = 0; j < general.editorExtensions[i].extensions.Length; j++)
						{
							if (general.editorExtensions[i].extensions[j].Equals(fileExtension, StringComparison.OrdinalIgnoreCase) == true)
							{
								editorExtensions = general.editorExtensions[i];
								editorPath = general.editorExtensions[i].editor;
								goto doubleBreak;
							}
						}
					}
				}
				doubleBreak:

				// It is required to delay the opening, due editor sometimes falling into error state during the same frame.

				// Fallback when it is log without any reachable file. (Happened with a non-usable dll.)
				if (string.IsNullOrEmpty(editorPath) == true || editorExtensions == null)
				{
					Object	sourceFile = AssetDatabase.LoadAssetAtPath(@"Assets\" + file.Substring(Application.dataPath.Length + 1), typeof(Object));

					if (sourceFile != null)
						EditorApplication.delayCall += () => AssetDatabase.OpenAsset(sourceFile, line);
					else
						EditorApplication.delayCall += () => EditorUtility.OpenWithDefaultApp(file);
					return;
				}

				LogSettings	settings = HQ.Settings.Get<LogSettings>();

				foreach (IEditorOpener opener in NGConsoleWindow.Openers)
				{
					if (opener.CanHandleEditor(editorPath) == true)
					{
						EditorApplication.delayCall += () => opener.Open(editorPath, editorExtensions.arguments, file, line);

						// Easy trick to give focus to the application.
						if (settings.giveFocusToEditor == true || focus == true)
							EditorApplication.delayCall += () => EditorUtility.OpenWithDefaultApp(file);
						break;
					}
				}
			}
		}

		public static Rect	DrawStackTrace(ILogContentGetter log, RowsDrawer rowsDrawer, Rect r, int i, Row row)
		{
			StackTraceSettings	settings = HQ.Settings.Get<StackTraceSettings>();
			float				width = r.width;

			// Substract viewRect to avoid scrollbar.
			r.height = settings.height;

			// Display the stack trace.
			int	j = 0;
			foreach (var frame in log.Frames)
			{
				// Hide invisible frames.
				if (r.y - rowsDrawer.currentVars.scrollbar.Offset > rowsDrawer.bodyRect.height)
					break;

				r.x = 0F;
				r.width = width - 16F;
				GUI.SetNextControlName("SF" + i + j);
				if (GUI.Button(r, frame.frameString, settings.Style) == true)
				{
					if (RowUtility.CheckLowestRowGoToLineAllowed(j) == true)
					{
						GUI.FocusControl("SF" + i + j);

						r.y -= rowsDrawer.currentVars.scrollbar.Offset;
						RowUtility.GoToLine(r, frame);
						r.y += rowsDrawer.currentVars.scrollbar.Offset;
					}
				}

				// Handle hover overflow.
				if (r.y - rowsDrawer.currentVars.scrollbar.Offset + r.height > rowsDrawer.bodyRect.height)
					r.height = rowsDrawer.bodyRect.height - r.y + rowsDrawer.currentVars.scrollbar.Offset;

				if (Event.current.type == EventType.MouseMove && r.Contains(Event.current.mousePosition) == true)
				{
					r.y -= rowsDrawer.currentVars.scrollbar.Offset;
					RowUtility.PreviewStackFrame(rowsDrawer, r, frame);
					r.y += rowsDrawer.currentVars.scrollbar.Offset;
				}

				r.x = r.width;
				r.width = 16F;
				if (GUI.Button(r, "+", settings.Style) == true)
				{
					StackTraceSettings	stackTrace = HQ.Settings.Get<StackTraceSettings>();
					GenericMenu			menu = new GenericMenu();

					if (frame.raw == null)
						InternalNGDebug.LogError("The frame stack is invalid." + Environment.NewLine + frame);

					menu.AddItem(new GUIContent("Set as Category"), false, RowUtility.SetAsCategory, frame.raw);

					string	f = RowUtility.GetFilterNamespace(frame.raw);
					if (f != null)
					{
						if (stackTrace.filters.Contains(f) == false)
							menu.AddItem(new GUIContent("Skip Namespace \"" + f + "\""), false, RowUtility.AddFilter, f);
						else
							menu.AddDisabledItem(new GUIContent("Skip Namespace \"" + f + "\""));
					}

					f = RowUtility.GetFilterClass(frame.raw);
					if (f != null)
					{
						if (stackTrace.filters.Contains(f) == false)
							menu.AddItem(new GUIContent("Skip Class \"" + f + "\""), false, RowUtility.AddFilter, f);
						else
							menu.AddDisabledItem(new GUIContent("Skip Class \"" + f + "\""));
					}

					f = RowUtility.GetFilterMethod(frame.raw);
					if (f != null)
					{
						if (stackTrace.filters.Contains(f) == false)
							menu.AddItem(new GUIContent("Skip Method \"" + f + "\""), false, RowUtility.AddFilter, f);
						else
							menu.AddDisabledItem(new GUIContent("Skip Method \"" + f + "\""));
					}

					menu.AddItem(new GUIContent("Manage filters"), false, RowUtility.GoToSettings);
					menu.ShowAsContext();
				}

				++j;
				r.y += r.height;
			}

			return r;
		}

		private static void	GoToSettings()
		{
			EditorWindow.GetWindow<NGSettingsWindow>("NG Settings", true).Focus(NGConsoleWindow.Title);
			ConsoleSettingsEditor.currentTab = ConsoleSettingsEditor.MainTab.General;
			ConsoleSettingsEditor.currentGeneralTab = ConsoleSettingsEditor.GeneralTab.StackTrace;
			ConsoleSettingsEditor.generalStackTraceScrollPosition = Vector2.zero;
		}

		private static void	SetAsCategory(object data)
		{
			StackTraceSettings	stackTrace = HQ.Settings.Get<StackTraceSettings>();
			string				frame = data as string;

			// Fetch "namespace.class[:.]method(".
			int	n = frame.IndexOf("(");
			if (n == -1)
				return;

			string	method = frame.Substring(0, n);
			string	placeholder = "Category Name";

			for (int i = 0; i < stackTrace.categories.Count; i++)
			{
				if (stackTrace.categories[i].method == method)
				{
					placeholder = stackTrace.categories[i].category;
					break;
				}
			}

			PromptWindow.Start(placeholder, RowUtility.SetCategory, method);
		}

		private static void	SetCategory(object data, string category)
		{
			StackTraceSettings	stackTrace = HQ.Settings.Get<StackTraceSettings>();
			string				method = data as string;

			for (int i = 0; i < stackTrace.categories.Count; i++)
			{
				if (stackTrace.categories[i].method == method)
				{
					if (string.IsNullOrEmpty(category) == false)
						stackTrace.categories[i].category = category;
					else
						stackTrace.categories.RemoveAt(i);

					LogConditionParser.cachedFrames.Clear();
					LogConditionParser.cachedFramesArrays.Clear();
					MainModule.methodsCategories.Clear();
					return;
				}
			}

			if (string.IsNullOrEmpty(category) == false)
			{
				stackTrace.categories.Add(new StackTraceSettings.MethodCategory() { category = category, method = method });

				LogConditionParser.cachedFrames.Clear();
				LogConditionParser.cachedFramesArrays.Clear();
				MainModule.methodsCategories.Clear();
			}
		}

		private static void	AddFilter(object data)
		{
			RowUtility.AddFrameFilter(data as string);
		}

		private static string	GetFilterNamespace(object data)
		{
			string	frame = data as string;

			// Fetch "namespace.class[:.]method(".
			int	n = frame.IndexOf("(");
			if (n == -1)
				return null;

			// Reduce to "namespace.class[:.]".
			int	n2 = frame.IndexOf(":", 0, n);
			if (n2 == -1)
			{
				n = frame.LastIndexOf(".", n);
				if (n == -1)
					return null;
			}
			else
				n = n2;

			// Reduce to "namespace.".
			n = frame.IndexOf(".", 0, n);
			if (n == -1)
				return null;

			return frame.Substring(0, n + 1);
		}

		private static string	GetFilterClass(object data)
		{
			string	frame = data as string;

			// Fetch "namespace.class[:.]method(".
			int n = frame.IndexOf("(");
			if (n == -1)
				return null;

			// Reduce to "namespace.class[:.]".
			int	n2 = frame.IndexOf(":", 0, n);
			if (n2 == -1)
			{
				n = frame.LastIndexOf(".", n);
				if (n == -1)
					return null;
			}
			else
				n = n2;

			return frame.Substring(0, n + 1);
		}

		private static string	GetFilterMethod(object data)
		{
			string	frame = data as string;

			// Fetch "namespace.class[:.]method(".
			int	n = frame.IndexOf("(");
			if (n == -1)
				return null;

			return frame.Substring(0, n + 1);
		}

		private static void	AddFrameFilter(string filter)
		{
			StackTraceSettings	stackTrace = HQ.Settings.Get<StackTraceSettings>();

			if (stackTrace.filters.Contains(filter) == false)
			{
				stackTrace.filters.Add(filter);
				HQ.InvalidateSettings();
				EditorUtility.DisplayDialog(Constants.PackageTitle, "\"" + filter + "\" has been added to the filters.", "OK");
			}
			else
				EditorUtility.DisplayDialog(Constants.PackageTitle, "\"" + filter + "\" is already a filter.", "OK");
		}

		private static void	GoToLine(Rect r, Frame frame)
		{
			StackTraceSettings	stackTrace = HQ.Settings.Get<StackTraceSettings>();

			if (Event.current.button == 0 &&
				frame.fileExist == true)
			{
				// Ping folder on click + modifiers.
				if ((Event.current.modifiers & stackTrace.pingFolderOnModifier) != 0)
				{
					int	i = frame.frameString.LastIndexOf('	') + 1;

					Utility.content.text = frame.frameString.Substring(0, i);
					var	v = stackTrace.Style.CalcSize(Utility.content);

					StringBuilder	buffer = Utility.GetBuffer();

					if (Event.current.mousePosition.x >= v.x)
					{
						int	i2 = frame.frameString.IndexOf('/', i + 1);

						// Skip Assets folder.
						string	folder = frame.frameString.Substring(i, i2 - i).Split('>')[1];

						Utility.content.text += folder;
						v = stackTrace.Style.CalcSize(Utility.content);

						if (Event.current.mousePosition.x > v.x)
						{
							i = i2;
							buffer.Append(folder);

							while (Event.current.mousePosition.x >= v.x)
							{
								i2 = frame.frameString.IndexOf('/', i + 1);
								if (i2 == -1)
									break;

								folder = frame.frameString.Substring(i, i2 - i);
								buffer.Append(folder);
								Utility.content.text += folder;
								v = stackTrace.Style.CalcSize(Utility.content);
								i = i2;
							}

							EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(buffer.ToString(), typeof(Object)));
						}
					}

					Utility.RestoreBuffer(buffer);
				}
				// Or go to line.
				else
				{
					bool	focus = (Event.current.modifiers & HQ.Settings.Get<LogSettings>().forceFocusOnModifier) != 0;

					RowUtility.GoToFileLine(frame.fileName, frame.line, focus);

					Event.current.Use();
				}
			}
		}

		public static void	GoToLine(ILogContentGetter log, LogEntry logEntry, bool focus)
		{
			// Prefer using instanceID as much as possible, more reliable.
			// Try to reach the object, it might not be a TextAsset.
			if (logEntry.instanceID != 0)
			{
				string	path = AssetDatabase.GetAssetPath(logEntry.instanceID);
				if (string.IsNullOrEmpty(path) == false)
				{
					Object	sourceFile = AssetDatabase.LoadAssetAtPath(path, typeof(TextAsset));
					if (sourceFile != null)
					{
						RowUtility.GoToFileLine(path,
												logEntry.line,
												focus);
						return;
					}
				}
			}

			// Go to the first reachable frame.
			for (int j = 0; j < log.Frames.Length; j++)
			{
				if (log.Frames[j].fileExist == true)
				{
					RowUtility.GoToFileLine(log.Frames[j].fileName,
											log.Frames[j].line,
											focus);
					break;
				}
			}
		}

		private static bool	CheckLowestRowGoToLineAllowed(int count)
		{
			return NGLicensesManager.Check(count < RowUtility.LowestRowGoToLineAllowed, NGTools.NGConsole.NGAssemblyInfo.Name + " Pro", "Free version does not allow to go to line on frame lower than " + RowUtility.LowestRowGoToLineAllowed + ".\n\nYou just asked for a kill feature... No? You disagree? I am truly sorry, but this is madness! XD");
		}
	}
}