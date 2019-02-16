using NGTools;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	public class LogsExporterDrawer
	{
		public sealed class ExportSettings
		{
			public Action<ILogExporter, Row>	callbackLog;
		}

		private const float				AddButtonWidth = 20F;
		private const float				ExportMargin = 1F;
		private const float				DoubleExportMargin = LogsExporterDrawer.ExportMargin + LogsExporterDrawer.ExportMargin;
		private const int				MaxChars = (UInt16.MaxValue >> 2) - 1;
		private readonly static Type[]	DefaultExportSources = new Type[] { typeof(LogTypeSource), typeof(FullMessageSource), typeof(StackTraceSource) };

		public int	previewCount = 3;
		public int	selectedExporter;

		private ExportSettings	settings = new ExportSettings();

		private EditorWindow		window;
		private List<Row>			rows;
		private ILogExporter[]		exporters;
		private string[]			names;
		private List<ILogExportSource>	usedOutputs = new List<ILogExportSource>();
		private string				previewLabel;
		private string				preview;
		private Vector2				scrollPosition;
		private HorizontalScrollbar	horizontalScrollbar;
		private GUIStyle			previewStyle;

		public	LogsExporterDrawer(EditorWindow window, List<Row> rows)
		{
			this.window = window;
			this.rows = rows;
			this.exporters = Utility.CreateNGTInstancesOf<ILogExporter>();
			this.previewLabel = "Preview              (" + this.rows.Count + " element" + (this.rows.Count > 1 ? "s" : string.Empty) + ")";

			this.names = new string[this.exporters.Length];
			for (int i = 0; i < this.exporters.Length; i++)
				this.names[i] = Utility.NicifyVariableName(this.exporters[i].GetType().Name);

			string	rawUsedOutputs = NGEditorPrefs.GetString(ExportLogsWindow.UsedOutputsKeyPref, null);

			if (string.IsNullOrEmpty(rawUsedOutputs) == false)
			{
				string[]	splittedTypes = rawUsedOutputs.Split(ExportLogsWindow.UsedOutputsSeparator);

				for (int i = 0; i < splittedTypes.Length; i++)
				{
					Type	t = Type.GetType(splittedTypes[i]);

					if (t != null)
						this.usedOutputs.Add(Activator.CreateInstance(t) as ILogExportSource);
				}
			}

			if (this.usedOutputs.Count == 0)
			{
				for (int i = 0; i < LogsExporterDrawer.DefaultExportSources.Length; i++)
					this.usedOutputs.Add(Activator.CreateInstance(LogsExporterDrawer.DefaultExportSources[i]) as ILogExportSource);
			}

			Utility.LoadEditorPref(this, NGEditorPrefs.GetPerProjectPrefix());

			for (int i = 0; i < this.exporters.Length; i++)
				this.exporters[i].OnEnable();

			this.horizontalScrollbar = new HorizontalScrollbar(0F, 18F, this.window.position.width, 4F);

			this.UpdatePreview();
		}

		public void	Save()
		{
			StringBuilder	buffer = Utility.GetBuffer();

			for (int i = 0; i < this.usedOutputs.Count; i++)
			{
				if (i > 0)
					buffer.Append(ExportLogsWindow.UsedOutputsSeparator);
				buffer.Append(this.usedOutputs[i].GetType().GetShortAssemblyType());
			}

			NGEditorPrefs.SetString(ExportLogsWindow.UsedOutputsKeyPref, Utility.ReturnBuffer(buffer));

			Utility.SaveEditorPref(this, NGEditorPrefs.GetPerProjectPrefix());

			for (int i = 0; i < this.exporters.Length; i++)
				this.exporters[i].OnDestroy();
		}

		public void	OnGUI(Rect position)
		{
			if (this.previewStyle == null)
			{
				this.previewStyle = new GUIStyle(GUI.skin.textArea);
				this.previewStyle.wordWrap = false;
				this.previewStyle.fontSize = 8;
			}

			Rect	viewRect = default(Rect);
			float	previewHeight = 100F;
			float	titleSourcesHeight;

			Utility.content.text = "Sources (Drag to reorder; click to delete)";
			titleSourcesHeight = GeneralStyles.Title1.CalcSize(Utility.content).y;

			viewRect.height += Constants.SingleLineHeight; // Exporter selector
			viewRect.height += titleSourcesHeight + 5F; // Sources title + Horizontal Scrollbar

			if (0 <= this.selectedExporter && this.selectedExporter < this.exporters.Length)
			{
				viewRect.height += Constants.SingleLineHeight + 6F + 4F; // Sources
				viewRect.height += this.exporters[this.selectedExporter].GetHeight() + 2F; // Exporter GUI
				viewRect.height += Constants.SingleLineHeight; // Preview label

				if (previewHeight < position.height - viewRect.height)
					previewHeight = position.height - viewRect.height;
				viewRect.height += previewHeight; // Preview
			}

			this.scrollPosition = GUI.BeginScrollView(position, this.scrollPosition, viewRect);
			{
				if (viewRect.height > position.height)
					position.width -= 16F;

				Rect	r = position;

				r.x = 0F;
				r.y = 0F;
				r.height = Constants.SingleLineHeight;

				using (LabelWidthRestorer.Get(70F))
				{
					EditorGUI.BeginChangeCheck();
					int	e = EditorGUI.Popup(r, LC.G("Exporter"), this.selectedExporter, this.names);
					if (EditorGUI.EndChangeCheck() == true)
					{
						this.selectedExporter = e;
						this.UpdatePreview();
					}
					r.y += r.height;
				}

				r.height = titleSourcesHeight;
				GUI.Label(r, Utility.content, GeneralStyles.Title1);
				r.y += titleSourcesHeight + 5F;

				GUIStyle	style = GeneralStyles.ToolbarButton;
				float		totalWidth = LogsExporterDrawer.ExportMargin;

				for (int i = 0; i < this.usedOutputs.Count; i++)
				{
					Utility.content.text = this.usedOutputs[i].GetName();
					totalWidth += style.CalcSize(Utility.content).x + LogsExporterDrawer.DoubleExportMargin; // Drag margin
				}

				r.width = position.width - LogsExporterDrawer.AddButtonWidth;
				r.height = Constants.SingleLineHeight + 6F;

				this.horizontalScrollbar.SetPosition(0F, r.y - 6F);
				this.horizontalScrollbar.RealWidth = totalWidth + LogsExporterDrawer.DoubleExportMargin;
				this.horizontalScrollbar.SetSize(r.width);
				this.horizontalScrollbar.interceiptEvent = false;
				this.horizontalScrollbar.hasCustomArea = true;
				this.horizontalScrollbar.allowedMouseArea = r;
				this.horizontalScrollbar.OnGUI();
				Utility.DrawUnfillRect(r, Color.gray * .8F);

				using (ColorContentRestorer.Get(GeneralStyles.HighlightActionButton))
				{
					Utility.content.text = "+";
					r.x += LogsExporterDrawer.AddButtonWidth;
					r.y += 2F;
					r.xMin = r.xMax - LogsExporterDrawer.AddButtonWidth;
					if (GUI.Button(r, Utility.content, GeneralStyles.ToolbarButton) == true)
						PopupWindow.Show(new Rect(r.x, r.yMax, 0F, 0F), new BrowseLogExportSourcesPopup(this.usedOutputs, this.OnCreate));
					r.y -= 2F;
				}

				r.x = 1F;
				r.width = position.width - LogsExporterDrawer.AddButtonWidth - 2F;

				GUI.BeginGroup(r);
				{
					Rect	r2 = r;
					r2.x = -this.horizontalScrollbar.Offset;
					r2.y = 2F;
					r2.height -= 4F;

					for (int i = 0; i < this.usedOutputs.Count; i++)
					{
						Utility.content.text = this.usedOutputs[i].GetName();
						r2.width = style.CalcSize(Utility.content).x + LogsExporterDrawer.DoubleExportMargin;

						if (Event.current.type == EventType.MouseDrag &&
							Utility.position2D != Vector2.zero &&
							DragAndDrop.GetGenericData(Utility.DragObjectDataName) != null &&
							(Utility.position2D - Event.current.mousePosition).sqrMagnitude >= Constants.MinStartDragDistance)
						{
							Utility.position2D = Vector2.zero;
							DragAndDrop.StartDrag("Drag Object");
							Event.current.Use();
						}
						else if (Event.current.type == EventType.MouseDown &&
								 r2.Contains(Event.current.mousePosition) == true)
						{
							Utility.position2D = Event.current.mousePosition;
							DragAndDrop.PrepareStartDrag();
							DragAndDrop.SetGenericData(Utility.DragObjectDataName, i);
						}
						else if (Event.current.type == EventType.Repaint && DragAndDrop.GetGenericData(Utility.DragObjectDataName) != null)
						{
							int	dragIndex = (int)DragAndDrop.GetGenericData(Utility.DragObjectDataName);

							if (dragIndex == i)
							{
								EditorGUI.DrawRect(r2, Color.red * .5F);
							}
							else if (r2.Contains(Event.current.mousePosition) == true &&
									 DragAndDrop.visualMode == DragAndDropVisualMode.Copy)
							{
								bool	isPointingBefore = Event.current.mousePosition.x < r2.x + r2.width * .5F;

								if (i + 1 == dragIndex || (isPointingBefore == true && i - 1 != dragIndex))
								{
									Rect	r3 = r2;
									r3.width = 1F;
									EditorGUI.DrawRect(r3, Color.yellow);
								}
								else if (i - 1 == dragIndex || isPointingBefore == false)
								{
									Rect	r3 = r2;
									r3.x += r3.width;
									r3.width = 1F;
									EditorGUI.DrawRect(r3, Color.yellow);
								}
							}
						}
						else if ((Event.current.type == EventType.DragUpdated ||
								  Event.current.type == EventType.DragPerform) &&
								 r2.Contains(Event.current.mousePosition) == true)
						{
							int	dragIndex = (int)DragAndDrop.GetGenericData(Utility.DragObjectDataName);

							if (i.Equals(dragIndex) == false)
								DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
							else
								DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;

							if (Event.current.type == EventType.DragPerform)
							{
								DragAndDrop.AcceptDrag();

								bool	isPointingBefore = Event.current.mousePosition.x < r2.x + r2.width * .5F;

								if (i + 1 == dragIndex || (isPointingBefore == true && i - 1 != dragIndex))
									this.usedOutputs.Insert(i, this.usedOutputs[dragIndex]);
								else if (i - 1 == dragIndex || isPointingBefore == false)
									this.usedOutputs.Insert(i + 1, this.usedOutputs[dragIndex]);

								if (i < dragIndex)
									this.usedOutputs.RemoveAt(dragIndex + 1);
								else
									this.usedOutputs.RemoveAt(dragIndex);

								this.UpdatePreview();

								DragAndDrop.PrepareStartDrag();
							}

							Event.current.Use();
						}

						r2.x += LogsExporterDrawer.ExportMargin;
						r2.width -= LogsExporterDrawer.DoubleExportMargin;
						if (GUI.Button(r2, Utility.content.text, style) == true)
						{
							this.usedOutputs.RemoveAt(i);
							this.UpdatePreview();
							return;
						}
						r2.width += LogsExporterDrawer.DoubleExportMargin;
						r2.x += r2.width - LogsExporterDrawer.ExportMargin;
					}
				}
				GUI.EndGroup();

				if (0 <= this.selectedExporter && this.selectedExporter < this.exporters.Length)
				{
					r.x = 0F;
					r.width = position.width;
					r.y += r.height + 4F;

					EditorGUI.BeginChangeCheck();
					r.height = this.exporters[this.selectedExporter].GetHeight();
					this.exporters[this.selectedExporter].OnGUI(r);
					r.y += r.height + 2F;
					if (EditorGUI.EndChangeCheck() == true ||
						string.IsNullOrEmpty(this.preview) == true)
					{
						this.UpdatePreview();
					}

					r.height = Constants.SingleLineHeight;

					GUI.Label(r, this.previewLabel);

					r.x += 50F;
					r.width = 50F;
					EditorGUI.BeginChangeCheck();
					this.previewCount = EditorGUI.IntField(r, this.previewCount);
					if (EditorGUI.EndChangeCheck() == true)
					{
						if (this.previewCount < 1)
							this.previewCount = 1;
						this.UpdatePreview();
					}
					r.y += r.height;

					r.x = 0F;
					r.width = position.width;
					r.height = previewHeight;
					GUI.TextArea(r, this.preview, this.previewStyle);
				}
			}
			GUI.EndScrollView();
		}

		public string	Export()
		{
			return this.exporters[this.selectedExporter].Generate(this.rows, this.usedOutputs, this.settings);
		}

		private void	UpdatePreview()
		{
			Row[]	rows = new Row[Mathf.Min(this.previewCount, this.rows.Count)];

			for (int i = 0; i < rows.Length; i++)
				rows[i] = this.rows[i];

			this.preview = this.exporters[this.selectedExporter].Generate(rows, this.usedOutputs, this.settings);

			if (this.preview.Length >= LogsExporterDrawer.MaxChars)
				this.preview = this.preview.Substring(0, LogsExporterDrawer.MaxChars);
		}

		private void	OnCreate(ILogExportSource exportOutput)
		{
			this.usedOutputs.Add(exportOutput);
			this.UpdatePreview();
			this.window.Repaint();
		}
	}
}