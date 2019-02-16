using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	internal sealed class XMLExporter : ILogExporter
	{
		private StringBuilder	buffer;

		public void		OnEnable()
		{
		}

		public void		OnDestroy()
		{
		}

		public float	GetHeight()
		{
			return 0F;
		}

		public void		OnGUI(Rect r)
		{
		}

		public void		AddColumn(string key, string value, string[] attributes)
		{
			this.buffer.Append('<');
			this.buffer.Append(key);
			this.buffer.Append('>');
			this.buffer.Append(value);
			this.buffer.Append("</");
			this.buffer.Append(key);
			this.buffer.Append(">");
		}

		public string	Generate(IEnumerable<Row> rows, List<ILogExportSource> usedOutputs, LogsExporterDrawer.ExportSettings settings)
		{
			this.buffer = Utility.GetBuffer();

			int	i = 0;

			foreach (Row row in rows)
			{
				if (i > 0)
					this.buffer.Append(Environment.NewLine);

				for (int j = 0; j < usedOutputs.Count; j++)
				{
					string	key = usedOutputs[j].GetKey();

					this.buffer.Append("<" + key + ">");
					this.buffer.Append(usedOutputs[j].GetValue(row, i));
					this.buffer.Append("</" + key + ">");
				}

				if (settings.callbackLog != null)
					settings.callbackLog(this, row);

				++i;
			}

			return Utility.ReturnBuffer(this.buffer);
		}
	}
}