using NGTools;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	public class ExportLogsWindow : EditorWindow
	{
		public enum OutputContent
		{
			FirstLine,
			FullLog
		}

		public const string	UsedOutputsKeyPref = "NGExportRows.UsedOutputs";
		public const char	UsedOutputsSeparator = '!';

		[File(FileAttribute.Mode.Save, "log")]
		public string		exportFile;

		private LogsExporterDrawer.ExportSettings	settings = new LogsExporterDrawer.ExportSettings();

		private LogsExporterDrawer	drawer;

		public static void	Export(List<Row> rows, Action<ILogExporter, Row> callbackLog = null)
		{
			Utility.OpenWindow<ExportLogsWindow>(true, "Export logs", true, null, w =>
			{
				w.settings.callbackLog = callbackLog;
				w.drawer = new LogsExporterDrawer(w, rows);
			});
		}

		protected virtual void	OnEnable()
		{
			Utility.LoadEditorPref(this);

			this.minSize = new Vector2(450F, 300F);
		}

		protected virtual void	OnDestroy()
		{
			this.drawer.Save();

			Utility.SaveEditorPref(this);
		}

		protected virtual void	OnGUI()
		{
			Rect	r = this.position;

			r.x = 0F;
			r.y = 0F;
			r.yMax -= 32F;
			this.drawer.OnGUI(r);

			r.y += r.height;
			r.height = 32F;

			GUILayout.BeginArea(r);
			{
				EditorGUILayout.BeginHorizontal();
				{
					EditorGUILayout.BeginVertical();
					{
						GUILayout.Space(10F);
						using (LabelWidthRestorer.Get(95F))
						{
							this.exportFile = NGEditorGUILayout.SaveFileField(LC.G("ExportFilePath"), this.exportFile, string.Empty, string.Empty);
						}
					}
					EditorGUILayout.EndVertical();

					GUILayout.Space(10F);

					EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(this.exportFile));
					{
						using (BgColorContentRestorer.Get(GeneralStyles.HighlightActionButton))
						{
							if (GUILayout.Button(LC.G("Export"), GUILayoutOptionPool.Height(30F)) == true)
								this.ExportLogs();
						}
					}
					EditorGUI.EndDisabledGroup();
				}
				EditorGUILayout.EndHorizontal();
			}
			GUILayout.EndArea();
		}

		protected virtual void	Update()
		{
			if (EditorApplication.isCompiling == true || this.drawer == null)
				this.Close();
		}

		private void	ExportLogs()
		{
			try
			{
				File.WriteAllText(this.exportFile, this.drawer.Export());
				InternalNGDebug.Log(LC.G("LogsExportedTo") + "\"" + this.exportFile + "\".");
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException(ex);
			}
		}
	}
}