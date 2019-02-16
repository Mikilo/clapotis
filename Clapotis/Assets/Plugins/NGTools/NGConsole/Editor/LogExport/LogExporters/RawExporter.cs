using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	internal sealed class RawExporter : ILogExporter
	{
		public string	dataSeparator = "\\n";
		public string	logSeparator = "\\n\\n";

		private StringBuilder	buffer;

		public void	OnEnable()
		{
			Utility.LoadEditorPref(this);
		}

		public void	OnDestroy()
		{
			Utility.SaveEditorPref(this);
		}

		public float	GetHeight()
		{
			return Constants.SingleLineHeight + 2F;
		}

		public void	OnGUI(Rect r)
		{
			GUILayout.BeginArea(r);
			{
				EditorGUILayout.BeginHorizontal();
				{
					using (LabelWidthRestorer.Get(92F))
					{
						this.dataSeparator = EditorGUILayout.TextField(LC.G("DataSeparator"), this.dataSeparator);
					}

					if (GUILayout.Button("Tab", GeneralStyles.ToolbarButton) == true)
					{
						GUI.FocusControl(null);
						this.dataSeparator += "\\t";
					}

					if (GUILayout.Button("CR", GeneralStyles.ToolbarButton) == true)
					{
						GUI.FocusControl(null);
						this.dataSeparator += "\\r";
					}

					if (GUILayout.Button("LF", GeneralStyles.ToolbarButton) == true)
					{
						GUI.FocusControl(null);
						this.dataSeparator += "\\n";
					}

					//GUILayout.Space(10F);
					GUILayout.FlexibleSpace();

					using (LabelWidthRestorer.Get(85F))
					{
						this.logSeparator = EditorGUILayout.TextField(LC.G("LogSeparator"), this.logSeparator);
					}

					if (GUILayout.Button("Tab", GeneralStyles.ToolbarButton) == true)
					{
						GUI.FocusControl(null);
						this.logSeparator += "\\t";
					}

					if (GUILayout.Button("CR", GeneralStyles.ToolbarButton) == true)
					{
						GUI.FocusControl(null);
						this.logSeparator += "\\r";
					}

					if (GUILayout.Button("LF", GeneralStyles.ToolbarButton) == true)
					{
						GUI.FocusControl(null);
						this.logSeparator += "\\n";
					}
				}
				EditorGUILayout.EndHorizontal();
			}
			GUILayout.EndArea();
		}

		public void	AddColumn(string key, string value, string[] attributes)
		{
			this.buffer.Append(value);
			this.buffer.Append(this.dataSeparator.Replace("\\t", "\t").Replace("\\n", "\n").Replace("\\r", "\r"));
		}

		public string	Generate(IEnumerable<Row> rows, List<ILogExportSource> usedOutputs, LogsExporterDrawer.ExportSettings settings)
		{
			this.buffer = Utility.GetBuffer();

			string	dataSeparator = this.dataSeparator.Replace("\\t", "\t").Replace("\\n", "\n").Replace("\\r", "\r");
			string	logSeparator = this.logSeparator.Replace("\\t", "\t").Replace("\\n", "\n").Replace("\\r", "\r");
			int		i = 0;

			foreach (Row row in rows)
			{
				if (i > 0)
					this.buffer.Append(logSeparator);

				for (int j = 0; j < usedOutputs.Count; j++)
				{
					if (j > 0)
						this.buffer.Append(dataSeparator);

					this.buffer.Append(usedOutputs[j].GetValue(row, i));
				}

				if (settings.callbackLog != null)
					settings.callbackLog(this, row);

				++i;
			}

			return Utility.ReturnBuffer(buffer);
		}
	}
}