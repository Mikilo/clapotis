using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	[Serializable]
	internal sealed class ColorMarkersModule : Module
	{
		private class ScrollbarInterests
		{
			public ListPointOfInterest	filterInterests = new ListPointOfInterest(1) { offset = 3F };
			public ListPointOfInterest	logTypeInterests = new ListPointOfInterest(2) { offset = -5F };
			public ListPointOfInterest	warningLogsInterests = new ListPointOfInterest(2) { offset = -3F };
			public ListPointOfInterest	errorLogsInterests = new ListPointOfInterest(2) { offset = -1F };
		}

		private class ColoredRow
		{
			public Row	row;
			public int	i;
		}

		[NonSerialized]
		private List<ColoredRow>	coloredRows;
		[NonSerialized]
		private Dictionary<RowsDrawer, ScrollbarInterests>	rowsDrawerInterests = new Dictionary<RowsDrawer, ScrollbarInterests>();

		public	ColorMarkersModule()
		{
		}

		public override void	OnEnable(NGConsoleWindow console, int id)
		{
			base.OnEnable(console, id);

			this.coloredRows = new List<ColoredRow>();

			MainModule	main = this.console.GetModule("Main") as MainModule;

			main.StreamAdded += this.OnMainStreamAdded;
			main.StreamDeleted += this.OnMainStreamDeleted;

			for (int i = 0; i < main.Streams.Count; i++)
				main.Streams[i].RowAdded += this.OnRowAdded;

			RowsDrawer.GlobalBeforeFoldout += this.RowsDrawer_GlobalBeforeFoldout;
			RowsDrawer.GlobalLogContextMenu += this.AppendColorsMenuItem;
			this.console.BeforeGUIHeaderRightMenu += this.HeaderButton;
		}

		public override void	OnDisable()
		{
			MainModule	main = this.console.GetModule("Main") as MainModule;

			main.StreamAdded -= this.OnMainStreamAdded;
			main.StreamDeleted -= this.OnMainStreamDeleted;

			for (int i = 0; i < main.Streams.Count; i++)
				main.Streams[i].RowAdded -= this.OnRowAdded;

			RowsDrawer.GlobalBeforeFoldout -= this.RowsDrawer_GlobalBeforeFoldout;
			RowsDrawer.GlobalLogContextMenu -= this.AppendColorsMenuItem;
			this.console.BeforeGUIHeaderRightMenu -= this.HeaderButton;
		}

		private void	OnMainStreamAdded(StreamLog stream)
		{
			stream.RowAdded += this.OnRowAdded;
			stream.rowsDrawer.perWindowVars.VarsAdded += this.ClosureVars(stream);
		}

		private void	OnMainStreamDeleted(StreamLog stream)
		{
			stream.RowAdded -= this.OnRowAdded;
			// It is added as a closure, removing 
			//stream.rowsDrawer.perWindowVars.VarsAdded -= this.OnVarsAdded;
		}

		private void	OnRowAdded(StreamLog stream, Row row, int consoleIndex)
		{
			ColorMarkersModuleSettings	settings = HQ.Settings.Get<ColorMarkersModuleSettings>();
			ScrollbarInterests			interests;

			if (this.rowsDrawerInterests.TryGetValue(stream.rowsDrawer, out interests) == false)
			{
				interests = new ScrollbarInterests();

				foreach (var vars in stream.rowsDrawer.perWindowVars.Each())
				{
					vars.scrollbar.AddListInterests(interests.filterInterests);
					vars.scrollbar.AddListInterests(interests.logTypeInterests);
					vars.scrollbar.AddListInterests(interests.warningLogsInterests);
					vars.scrollbar.AddListInterests(interests.errorLogsInterests);
				}

				this.rowsDrawerInterests.Add(stream.rowsDrawer, interests);
			}

			if (settings.dotInScrollbarRowByLogType == true)
			{
				if ((row.log.mode & Mode.ScriptingException) != 0)
					interests.errorLogsInterests.Add(8F + stream.rowsDrawer.GetOffsetAtIndex(consoleIndex), HQ.Settings.Get<GeneralSettings>().differentiateException == false ? ConsoleConstants.ErrorFoldoutColor : ConsoleConstants.ExceptionFoldoutColor, consoleIndex);
				else if ((row.log.mode & (Mode.ScriptCompileError | Mode.ScriptingError | Mode.Fatal | Mode.Error | Mode.Assert | Mode.AssetImportError | Mode.ScriptingAssertion)) != 0)
					interests.errorLogsInterests.Add(8F + stream.rowsDrawer.GetOffsetAtIndex(consoleIndex), ConsoleConstants.ErrorFoldoutColor, consoleIndex);
				else if ((row.log.mode & (Mode.ScriptCompileWarning | Mode.ScriptingWarning | Mode.AssetImportWarning)) != 0)
					interests.warningLogsInterests.Add(8F + stream.rowsDrawer.GetOffsetAtIndex(consoleIndex), ConsoleConstants.WarningFoldoutColor, consoleIndex);
				else
					goto skip;
			}

			skip:

			List<ColorBackground>	stamps = settings.colorBackgrounds;

			for (int j = 0; j < this.coloredRows.Count;  j++)
			{
				if (this.coloredRows[j].row == row)
				{
					if (this.coloredRows[j].i < stamps.Count)
					{
						interests.filterInterests.Add(8F + stream.rowsDrawer.GetOffsetAtIndex(consoleIndex), stamps[this.coloredRows[j].i].color, consoleIndex);
						return;
					}
				}
			}

			List<ColorMarker>	markers = settings.colorMarkers;

			for (int j = 0; j < markers.Count; j++)
			{
				if (markers[j].groupFilters.filters.Count > 0 &&
					markers[j].groupFilters.Filter(row) == true)
				{
					interests.filterInterests.Add(8F + stream.rowsDrawer.GetOffsetAtIndex(consoleIndex), markers[j].backgroundColor, consoleIndex);
					return;
				}
			}

			IRowDotColored	dotColored = row as IRowDotColored;

			if (dotColored != null)
				interests.logTypeInterests.Add(8F + stream.rowsDrawer.GetOffsetAtIndex(consoleIndex), dotColored.GetColor(), consoleIndex);
		}

		private void	AppendColorsMenuItem(GenericMenu menu, RowsDrawer rowsDrawer, int streamIndex, Row row)
		{
			if (HQ.Settings == null)
				return;

			ColorMarkersModuleSettings	settings = HQ.Settings.Get<ColorMarkersModuleSettings>();

			for (int i = 0; i < settings.colorBackgrounds.Count; i++)
			{
				if (settings.colorBackgrounds[i].name != string.Empty)
					menu.AddItem(new GUIContent(settings.nestedMenu == true ? "Colors/" + settings.colorBackgrounds[i].name : settings.colorBackgrounds[i].name), this.coloredRows.Exists((e) => e.i == i && e.row == row), this.ToggleColor, new object[] { rowsDrawer, streamIndex, row, i });
			}
		}

		private void	ToggleColor(object data)
		{
			object[]	array = data as object[];
			RowsDrawer	rowsDrawer = array[0] as RowsDrawer;
			int			consoleIndex = rowsDrawer[(int)array[1]];
			Row			row = array[2] as Row;
			int			colorIndex = (int)array[3];
			

			ColorMarkersModuleSettings	settings = HQ.Settings.Get<ColorMarkersModuleSettings>();
			List<ColorBackground>		markers = settings.colorBackgrounds;
			ScrollbarInterests			interests;

			if (this.rowsDrawerInterests.TryGetValue(rowsDrawer, out interests) == false)
			{
				interests = new ScrollbarInterests();

				this.rowsDrawerInterests.Add(rowsDrawer, interests);
			}

			for (int j = 0; j < this.coloredRows.Count;  j++)
			{
				if (this.coloredRows[j].row == row)
				{
					if (this.coloredRows[j].i == colorIndex)
					{
						interests.filterInterests.RemoveId(consoleIndex, 0F);
						this.coloredRows.RemoveAt(j);
					}
					else
					{
						interests.filterInterests.Add(8F + rowsDrawer.GetOffsetAtIndex(consoleIndex), markers[colorIndex].color, consoleIndex);
						this.coloredRows[j].i = colorIndex;
					}

					return;
				}
			}

			interests.filterInterests.Add(8F + rowsDrawer.GetOffsetAtIndex(consoleIndex), markers[colorIndex].color, consoleIndex);
			this.coloredRows.Add(new ColoredRow() { row = row, i = colorIndex });
		}

		private Rect	RowsDrawer_GlobalBeforeFoldout(RowsDrawer rowsDrawer, Rect r, int i, Row row)
		{
			if (HQ.Settings == null)
				return r;

			ColorMarkersModuleSettings	settings = HQ.Settings.Get<ColorMarkersModuleSettings>();
			List<ColorMarker>			markers = settings.colorMarkers;
			List<ColorBackground>		stamps = settings.colorBackgrounds;

			for (int j = 0; j < this.coloredRows.Count;  j++)
			{
				if (this.coloredRows[j].row == row)
				{
					if (this.coloredRows[j].i < stamps.Count)
						EditorGUI.DrawRect(r, stamps[this.coloredRows[j].i].color);
					return r;
				}
			}

			for (int j = 0; j < markers.Count; j++)
			{
				if (markers[j].groupFilters.filters.Count > 0 &&
					markers[j].groupFilters.Filter(row) == true)
				{
					EditorGUI.DrawRect(r, markers[j].backgroundColor);
					break;
				}
			}

			return r;
		}

		private Rect	HeaderButton(Rect r)
		{
			Utility.content.text = LC.G("ColorMarkers");
			GeneralSettings	settings = HQ.Settings.Get<GeneralSettings>();
			float			x = r.x;
			float			width = settings.MenuButtonStyle.CalcSize(Utility.content).x;

			r.x = r.x + r.width - width;
			r.width = width;
			if (GUI.Button(r, Utility.content.text, settings.MenuButtonStyle) == true)
				Utility.OpenWindow<ColorMarkersWizard>(true, ColorMarkersWizard.Title, true);

			r.width = r.x - x;
			r.x = x;
			return r;
		}

		private Action<RowsDrawer.Vars>	ClosureVars(StreamLog stream)
		{
			return (vars) =>
			{
				ScrollbarInterests	interests;

				if (this.rowsDrawerInterests.TryGetValue(stream.rowsDrawer, out interests) == false)
				{
					interests = new ScrollbarInterests();

					this.rowsDrawerInterests.Add(stream.rowsDrawer, interests);
				}

				vars.scrollbar.AddListInterests(interests.filterInterests);
				vars.scrollbar.AddListInterests(interests.logTypeInterests);
				vars.scrollbar.AddListInterests(interests.warningLogsInterests);
				vars.scrollbar.AddListInterests(interests.errorLogsInterests);
			};
		}
	}
}