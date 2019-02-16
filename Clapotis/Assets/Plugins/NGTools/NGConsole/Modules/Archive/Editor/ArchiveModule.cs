using NGTools;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	[Serializable, VisibleModule(150)]
	internal sealed class ArchiveModule : Module
	{
		[Serializable]
		private sealed class Vars
		{
			public int	workingFolder;
		}

		[Serializable, ExcludeFromExport]
		private sealed class UnexportableFolder : Folder
		{
		}

		[Serializable]
		public class Folder : IRows
		{
			[Exportable]
			public string			name;
			public RowsDrawer		rowsDrawer = new RowsDrawer();
			public List<LogNote>	notes = new List<LogNote>();

			[NonSerialized]
			public Rect	noteRect;
			[NonSerialized]
			public LogNote	viewingNote;

			[NonSerialized]
			private NGConsoleWindow console;

			public void	Init(NGConsoleWindow console)
			{
				this.console = console;
				this.rowsDrawer.Init(console, this);
				this.rowsDrawer.RowHovered += this.ShowNote;
				this.rowsDrawer.RowClicked += this.StartDrag;
				this.rowsDrawer.LogContextMenu += this.LogContextMenu;

				for (int i = 0; i < this.notes.Count; i++)
				{
					this.notes[i].row.Init(console, this.notes[i].row.log);
					this.rowsDrawer.Add(i);
				}
			}

			public void	Uninit()
			{
				this.rowsDrawer.Uninit();
				this.rowsDrawer.RowHovered -= this.ShowNote;
				this.rowsDrawer.RowClicked -= this.StartDrag;
				this.rowsDrawer.LogContextMenu -= this.LogContextMenu;

				for (int i = 0; i < this.notes.Count; i++)
					this.notes[i].row.Uninit();
			}

			private void	ShowNote(Rect r, Row row)
			{
				if (Event.current.type == EventType.MouseDrag)
				{
					if (DragAndDrop.GetGenericData("n") != null)
					{
						// Start the actual drag
						DragAndDrop.StartDrag("Dragging Row");

						// Make sure no one uses the event after us
						Event.current.Use();
					}
				}
				else if (Event.current.type == EventType.MouseMove)
				{
					DragAndDrop.PrepareStartDrag();

					this.viewingNote = null;

					for (int i = 0; i < this.notes.Count; i++)
					{
						if (this.notes[i].row == row)
						{
							if (string.IsNullOrEmpty(this.notes[i].note) == false)
							{
								this.noteRect = r;
								this.noteRect.x += this.rowsDrawer.bodyRect.x;
								this.noteRect.y += this.rowsDrawer.bodyRect.y;
								this.viewingNote = this.notes[i];
							}

							break;
						}
					}
				}
			}

			private void	StartDrag(Rect r, Row row)
			{
				DragAndDrop.PrepareStartDrag();

				for (int i = 0; i < this.notes.Count; i++)
				{
					if (this.notes[i].row == row)
					{
						DragAndDrop.SetGenericData("n", this.notes[i]);
						break;
					}
				}
			}

			private void	LogContextMenu(GenericMenu menu, Row row)
			{
				menu.AddSeparator("");
				Utility.content.text = LC.G("ArchiveModule_SetNote");
				menu.AddItem(Utility.content, false, this.PrepareSetNote, row);
			}

			private void	PrepareSetNote(object data)
			{
				Action<object, string>	SetNote = delegate(object data2, string content)
				{
					if (data2 is LogNote)
						((LogNote)data2).note = content;
					// Add a new note.
					else if (data2 is Row)
					{
						LogNote	note = new LogNote() {
							row = (Row)data2,
							note = content
						};
						this.notes.Add(note);
					}

					this.console.SaveModules();
				};

				for (int i = 0; i < this.notes.Count; i++)
				{
					if (this.notes[i].row == data)
					{
						PromptWindow.Start(this.notes[i].note, SetNote, this.notes[i]);
						return;
					}
				}

				PromptWindow.Start(string.Empty, SetNote, data);
			}

			Row	IRows.GetRow(int consoleIndex)
			{
				return this.notes[consoleIndex].row;
			}

			int	IRows.CountRows()
			{
				return this.notes.Count;
			}
		}

		[Serializable]
		public sealed class LogNote
		{
			[Exportable]
			public Row		row;
			[Exportable]
			public string	note;
		}

		[Exportable(ExportableAttribute.ArrayOptions.Immutable)]
		public List<Folder>	folders;

		[SerializeField]
		private PerWindowVars<Vars>	perWindowVars;

		[NonSerialized]
		private Vars	currentVars;

		public	ArchiveModule()
		{
			this.name = "Archive";
			this.folders = new List<Folder>();
			this.folders.Add(new UnexportableFolder() { name = "Common" });
			this.perWindowVars = new PerWindowVars<Vars>();
		}

		public override void	OnEnable(NGConsoleWindow editor, int id)
		{
			base.OnEnable(editor, id);

			// In case the data is corrupted, restart the instance.
			if (this.folders == null || this.folders.Count == 0)
			{
				this.folders = new List<Folder>();
				this.folders.Add(new UnexportableFolder() { name = "Common" });
			}

			for (int i = 0; i < this.folders.Count; i++)
				this.InitFolder(this.folders[i]);

			RowsDrawer.GlobalLogContextMenu += this.ArchiveFromContextMenu;

			if (this.perWindowVars == null)
				this.perWindowVars = new PerWindowVars<Vars>();
			else
			{
				// It is possible to modify a variable from file, therefore we need to guarantee safety.
				foreach (Vars vars in this.perWindowVars.Each())
					vars.workingFolder = Mathf.Clamp(vars.workingFolder, 0, this.folders.Count - 1);
			}
		}

		public override void	OnDisable()
		{
			for (int i = 0; i < this.folders.Count; i++)
				this.UninitFolder(this.folders[i]);

			RowsDrawer.GlobalLogContextMenu -= this.ArchiveFromContextMenu;
		}

		public override void	OnGUI(Rect r)
		{
			this.currentVars = this.perWindowVars.Get(RowUtility.drawingWindow);

			r.y += 2F;
			r = this.DrawFolderTabs(r);

			if (this.folders.Count > 0)
			{
				Folder	folder = this.folders[this.currentVars.workingFolder];
				folder.rowsDrawer.DrawRows(r, false);

				if (folder.viewingNote != null && string.IsNullOrEmpty(folder.viewingNote.note) == false)
				{
					if (Event.current.type == EventType.MouseMove)
					{
						if (folder.noteRect.Contains(Event.current.mousePosition) == false)
						{
							folder.viewingNote = null;
							return;
						}
					}

					StackTraceSettings	stackTrace = HQ.Settings.Get<StackTraceSettings>();

					folder.noteRect.y += Constants.SingleLineHeight;
					EditorGUI.DrawRect(folder.noteRect, stackTrace.previewSourceCodeBackgroundColor);
					GUI.Label(folder.noteRect, folder.viewingNote.note, stackTrace.PreviewSourceCodeStyle);
					folder.noteRect.y -= Constants.SingleLineHeight;

					RowUtility.drawingWindow.Repaint();
				}
			}
		}

		private Rect	DrawFolderTabs(Rect r)
		{
			ConsoleSettings	settings = HQ.Settings.Get<ConsoleSettings>();
			GeneralSettings	general = HQ.Settings.Get<GeneralSettings>();
			float			height = r.height;

			r.height = ConsoleConstants.DefaultSingleLineHeight;

			// Switch stream
			if (settings.inputsManager.Check("Navigation", ConsoleConstants.SwitchNextStreamCommand) == true)
			{
				this.currentVars.workingFolder += 1;
				if (this.currentVars.workingFolder >= this.folders.Count)
					this.currentVars.workingFolder = 0;

				Event.current.Use();
			}
			if (settings.inputsManager.Check("Navigation", ConsoleConstants.SwitchPreviousStreamCommand) == true)
			{
				this.currentVars.workingFolder -= 1;
				if (this.currentVars.workingFolder < 0)
					this.currentVars.workingFolder = this.folders.Count - 1;

				Event.current.Use();
			}

			GUILayout.BeginArea(r);
			{
				GUILayout.BeginHorizontal();
				{
					for (int i = 0; i < this.folders.Count; i++)
					{
						EditorGUI.BeginChangeCheck();
						GUILayout.Toggle(i == this.currentVars.workingFolder, this.folders[i].name + " (" + this.folders[i].rowsDrawer.Count + ")", general.MenuButtonStyle);
						if (EditorGUI.EndChangeCheck() == true)
						{
							if (Event.current.button == 0)
							{
								this.currentVars.workingFolder = i;
								this.console.SaveModules();
							}
							// Forbid to alter the main folder.
							else if (i > 0 || Conf.DebugMode != Conf.DebugState.None)
							{
								// Show context menu on right click.
								if (Event.current.button == 1)
								{
									GenericMenu	menu = new GenericMenu();
									menu.AddItem(new GUIContent(LC.G("ArchiveModule_ChangeName")), false, this.ChangeStreamName, i);
									if (i > 0)
										menu.AddItem(new GUIContent(LC.G("Delete")), false, this.DeleteFolder, i);
									if (Conf.DebugMode != Conf.DebugState.None)
										menu.AddItem(new GUIContent("Clear"), false, this.ClearFolder, i);
									menu.DropDown(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0));
								}
								else if (Event.current.button == 2)
								{
									if (i > 0 || Conf.DebugMode != Conf.DebugState.None)
										this.DeleteFolder(i);
								}
							}
						}

						if ((Event.current.type == EventType.DragPerform ||
							 Event.current.type == EventType.DragUpdated ||
							 Event.current.type == EventType.DragExited ||
							 Event.current.type == EventType.MouseUp) &&
							DragAndDrop.GetGenericData("n") != null)
						{
							Rect	toggleRect = GUILayoutUtility.GetLastRect();

							// Check drop Row.
							if (toggleRect.Contains(Event.current.mousePosition))
							{
								if (Event.current.type == EventType.DragUpdated)
								{
									LogNote	note = DragAndDrop.GetGenericData("n") as LogNote;

									if (this.folders[i].notes.Contains(note) == false)
										DragAndDrop.visualMode = DragAndDropVisualMode.Move;
									else
										DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;

									Event.current.Use();
								}
								else if (Event.current.type == EventType.DragPerform)
								{
									DragAndDrop.AcceptDrag();

									LogNote	note = DragAndDrop.GetGenericData("n") as LogNote;
									Folder	f = this.folders[i];

									EditorApplication.delayCall += () =>
									{
										this.DeleteNote(note);
										f.notes.Add(note);
										f.rowsDrawer.Add(f.notes.Count - 1);
										this.UpdateName();
										this.console.SaveModules();
										this.console.Repaint();
									};

									Event.current.Use();
								}

								RowUtility.drawingWindow.Repaint();
							}

							if (Event.current.type == EventType.DragExited ||
								Event.current.type == EventType.MouseUp)
							{
								DragAndDrop.PrepareStartDrag();
							}
						}
					}

					if (GUILayout.Button("+", general.MenuButtonStyle) == true)
					{
						Folder	folder = new Folder() { name = "Folder " + this.folders.Count };

						this.InitFolder(folder);

						this.folders.Add(folder);
						this.console.SaveModules();

						if (this.folders.Count == 1)
							this.currentVars.workingFolder = 0;
					}

					GUILayout.FlexibleSpace();
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndArea();

			r.y += r.height + 2F;

			r.height = height - r.height - 2F;

			return r;
		}

		public override void	OnEnter()
		{
			base.OnEnter();

			this.console.BeforeGUIHeaderRightMenu += this.GUIExport;
		}

		public override void	OnLeave()
		{
			base.OnLeave();

			this.console.BeforeGUIHeaderRightMenu -= this.GUIExport;
		}

		private void	JustDeleteNote(LogNote log)
		{
			for (int i = 0; i < this.folders.Count; i++)
			{
				int	index = this.folders[i].notes.IndexOf(log);

				if (index != -1)
				{
					this.folders[i].notes.RemoveAt(index);
					break;
				}
			}
		}

		private void	DeleteNote(LogNote log)
		{
			for (int i = 0; i < this.folders.Count; i++)
			{
				int	index = this.folders[i].notes.IndexOf(log);

				if (index != -1)
				{
					this.folders[i].rowsDrawer.RemoveAt(index);

					for (int j = 0; j < this.folders[i].rowsDrawer.Count; j++)
						this.folders[i].rowsDrawer[j] = j;

					//int	extra = this.folders[i].notes.Count - this.folders[i].rowsDrawer.Count;

					//// Clean fallback, in case of error somewhere.
					//if (extra > 0)
					//	this.folders[i].notes.RemoveRange(this.folders[i].notes.Count - extra, extra);
					//else if (extra < 0)
					//{
					//	while (this.folders[i].rowsDrawer.Count < this.folders[i].notes.Count)
					//		this.folders[i].rowsDrawer.RemoveAt(this.folders[i].rowsDrawer.Count - 1);
					//}
					break;
				}
			}
		}

		private void	InitFolder(Folder folder)
		{
			folder.Init(this.console);

			folder.rowsDrawer.RowDeleted += this.ManualDeletedLog;
		}

		private void	UninitFolder(Folder folder)
		{
			folder.Uninit();

			folder.rowsDrawer.RowDeleted -= this.ManualDeletedLog;
		}

		private void	ClearFolder(object d)
		{
			this.folders[(int)d].notes.Clear();
			this.folders[(int)d].rowsDrawer.Clear();

			this.UpdateName();
			this.console.SaveModules();
		}

		private void	DeleteFolder(object data)
		{
			int	i = (int)data;

			this.UninitFolder(this.folders[i]);

			this.folders.RemoveAt(i);

			foreach (Vars vars in this.perWindowVars.Each())
				vars.workingFolder = Mathf.Clamp(vars.workingFolder, 0, this.folders.Count - 1);

			this.UpdateName();
			this.console.SaveModules();
		}

		private void	ChangeStreamName(object data)
		{
			PromptWindow.Start(this.folders[(int)data].name, this.RenameStream, data);
		}

		private void	ManualDeletedLog(Row row)
		{
			LogNote	log = this.GetLogFromRow(row);

			if (log != null)
			{
				this.JustDeleteNote(log);
				this.UpdateName();
				this.console.SaveModules();
			}
		}

		private void	UpdateName()
		{
			int	count = 0;

			for (int i = 0; i < this.folders.Count; i++)
				count += this.folders[i].rowsDrawer.Count;

			if (count == 0)
				this.name = "Archive";
			else
				this.name = "Archive (" + count + ")";
		}

		private void	RenameStream(object data, string newName)
		{
			if (string.IsNullOrEmpty(newName) == false)
			{
				this.folders[(int)data].name = newName;
				this.console.SaveModules();
			}
		}

		private LogNote	GetLogFromRow(Row row)
		{
			for (int i = 0; i < this.folders.Count; i++)
			{
				LogNote	log = this.folders[i].notes.Find(ln => ln.row == row);

				if (log != null)
					return log;
			}

			return null;
		}

		private void	ArchiveFromContextMenu(GenericMenu menu, RowsDrawer rowsDrawer, int streamIndex, Row row)
		{
			LogNote	log = this.GetLogFromRow(row);

			menu.AddItem(new GUIContent("Archive log"), log != null, this.ToggleRowFromArchive, row);
		}

		private void	ToggleRowFromArchive(object data)
		{
			Row		row = data as Row;
			LogNote	log = this.GetLogFromRow(row);

			if (log == null)
			{
				this.folders[0].notes.Add(new LogNote() { row = row });
				this.folders[0].rowsDrawer.Add(this.folders[0].notes.Count - 1);
			}
			else
				this.DeleteNote(log);

			this.UpdateName();
			this.console.SaveModules();
		}

		private Rect	GUIExport(Rect r)
		{
			Vars	vars = this.perWindowVars.Get(RowUtility.drawingWindow);

			EditorGUI.BeginDisabledGroup(this.folders.Count == 0 || this.folders[vars.workingFolder].rowsDrawer.Count == 0);
			{
				Utility.content.text = LC.G("ArchiveModule_ExportArchives");
				GeneralSettings	settings = HQ.Settings.Get<GeneralSettings>();
				float	x = r.x;
				float	width = settings.MenuButtonStyle.CalcSize(Utility.content).x;
				r.x = r.x + r.width - width;
				r.width = width;

				if (GUI.Button(r, Utility.content, settings.MenuButtonStyle) == true)
				{
					List<Row>	rows = new List<Row>();
					Vars		closedVars = vars;

					for (int i = 0; i < this.folders[vars.workingFolder].rowsDrawer.Count; i++)
						rows.Add(this.console.rows[this.folders[vars.workingFolder].rowsDrawer[i]]);

					Action<ILogExporter, Row>	ExportLogNote = delegate(ILogExporter exporter, Row row)
					{
						foreach (LogNote n in this.folders[closedVars.workingFolder].notes)
						{
							if (n.row == row)
							{
								exporter.AddColumn("note", n.note, null);
								break;
							}
						}
					};

					ExportLogsWindow.Export(rows, ExportLogNote);
				}

				r.width = r.x - x;
				r.x = x;
			}
			EditorGUI.EndDisabledGroup();

			return r;
		}
	}
}