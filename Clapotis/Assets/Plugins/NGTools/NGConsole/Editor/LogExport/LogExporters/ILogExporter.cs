using System.Collections.Generic;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	public interface ILogExporter
	{
		/// <summary>Called when the wizard select this exporter.</summary>
		void	OnEnable();
		/// <summary>Called when the wizard focus another exporter.</summary>
		void	OnDestroy();

		float	GetHeight();
		void	OnGUI(Rect r);

		/// <summary>
		/// Can be called to add an extra column to the actual row.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <param name="attributes"></param>
		void	AddColumn(string key, string value, string[] attributes);

		/// <summary>
		/// Generates the export with <paramref name="rows"/> using the given <paramref name="settings"/>.
		/// </summary>
		/// <param name="rows"></param>
		/// <param name="usedOutputs"></param>
		/// <param name="settings"></param>
		/// <returns></returns>
		string	Generate(IEnumerable<Row> rows, List<ILogExportSource> usedOutputs, LogsExporterDrawer.ExportSettings settings);
	}
}