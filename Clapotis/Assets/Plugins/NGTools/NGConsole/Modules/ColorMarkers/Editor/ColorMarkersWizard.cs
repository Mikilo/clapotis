using NGLicenses;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	public class ColorMarkersWizard : ScriptableWizard
	{
		public const string	Title = "Color Markers Wizard";
		public const int	MaxColorMarkers = 2;
		public const string	AssetFolderEditorPref = "CreateAssetWizardAssetFolder";
		public const float	Spacing = 2F;
		public const float	MarkerSpacing = 6F;

		private List<bool>	folds = new List<bool>();

		private ColorMarker	alteredColorMarker;
		private Color		newColor;
		private Rect		r = new Rect();
		private Rect		viewRect = new Rect();
		private Vector2		scrollPosition = new Vector2();

		protected virtual void	OnEnable()
		{
			Undo.undoRedoPerformed += this.Repaint;

			this.minSize = new Vector2(430F, 100F);
		}

		protected virtual void	OnDisable()
		{
			Undo.undoRedoPerformed -= this.Repaint;
		}

		protected virtual void	Update()
		{
			if (EditorApplication.isCompiling == true)
			{
				this.Close();
				return;
			}
		}

		protected virtual void	OnGUI()
		{
			if (HQ.Settings == null)
			{
				GUILayout.Label(string.Format(LC.G("RequiringConfigurationFile"), ColorMarkersWizard.Title));
				if (GUILayout.Button(LC.G("ShowPreferencesWindow")) == true)
					Utility.ShowPreferencesWindowAt(Constants.PreferenceTitle);
				return;
			}

			ColorMarkersModuleSettings	settings = HQ.Settings.Get<ColorMarkersModuleSettings>();

			this.r.x = 0F;
			this.r.y = 0F;
			this.r.width = this.position.width;
			this.r.height = Constants.SingleLineHeight;

			if (GUI.Button(r, LC.G("AddMarker")) == true)
			{
				if (this.CheckMaxColorMarkers(settings.colorMarkers.Count) == true)
				{
					Undo.RecordObject(settings, "Add color marker");

					ColorMarker			marker = new ColorMarker();
					MainModuleSettings	mainSettings = HQ.Settings.Get<MainModuleSettings>();

					foreach (ILogFilter filter in mainSettings.GenerateFilters())
						marker.groupFilters.filters.Add(filter);

					settings.colorMarkers.Add(marker);

					this.RefreshAllStreams();
				}
			}

			this.r.y += this.r.height + ColorMarkersWizard.MarkerSpacing;

			float	totalHeight = 0f;

			for (int i = 0; i < settings.colorMarkers.Count; i++)
			{
				while (this.folds.Count < settings.colorMarkers.Count)
					this.folds.Add(true);

				totalHeight += this.r.height + ColorMarkersWizard.Spacing + ColorMarkersWizard.MarkerSpacing;
				if (this.folds[i] == true)
				{
					totalHeight += this.r.height + ColorMarkersWizard.Spacing;

					for (int j = 0; j < settings.colorMarkers[i].groupFilters.filters.Count; j++)
					{
						if (settings.colorMarkers[i].groupFilters.filters[j].Enabled == true)
						{
							totalHeight += this.r.height + 2F;
						}
					}
				}
			}

			totalHeight -= ColorMarkersWizard.MarkerSpacing;
			this.viewRect.height = totalHeight;

			Rect	body = this.r;
			body.height = this.position.height - this.r.y;

			this.scrollPosition = GUI.BeginScrollView(body, this.scrollPosition, this.viewRect);
			{
				float	width = body.width;

				if (totalHeight > body.height)
					width -= 16F;

				this.r.y = 0F;
				for (int i = 0; i < settings.colorMarkers.Count; i++)
				{
					this.r.x = 0F;
					this.r.width = width;

					GUI.Box(r, GUIContent.none, GeneralStyles.Toolbar);

					this.r.width = width - 325F;
					this.folds[i] = EditorGUI.Foldout(this.r, this.folds[i], LC.G("Marker") + " #" + (i + 1));
					this.r.x += this.r.width;

					this.r.width = 200F;

					EditorGUI.BeginChangeCheck();
					Color	color = EditorGUI.ColorField(r, settings.colorMarkers[i].backgroundColor);
					if (EditorGUI.EndChangeCheck() == true)
					{
						this.alteredColorMarker = settings.colorMarkers[i];
						this.newColor = color;

						settings.colorMarkers[i].backgroundColor = color;
						Utility.RepaintEditorWindow(typeof(NGConsoleWindow));
						Utility.RegisterIntervalCallback(this.DelayedSetBackgroundcolor, 100, 1);
					}
					this.r.x += this.r.width;

					this.r.width = 65F;
					EditorGUI.DrawRect(this.r, settings.colorMarkers[i].backgroundColor);
					EditorGUI.LabelField(this.r, "# # # # #");
					this.r.x += this.r.width;

					this.r.width = 20F;

					EditorGUI.BeginDisabledGroup(i == 0);
					{
						if (GUI.Button(r, "↑", GeneralStyles.ToolbarButton) == true)
						{
							Undo.RecordObject(settings, "Reorder color marker");
							settings.colorMarkers.Reverse(i - 1, 2);
							this.RefreshAllStreams();
							break;
						}
						this.r.x += this.r.width;

						GUI.enabled = i < settings.colorMarkers.Count - 1;
						if (GUI.Button(r, "↓", GeneralStyles.ToolbarButton) == true)
						{
							Undo.RecordObject(settings, "Reorder color marker");
							settings.colorMarkers.Reverse(i, 2);
							this.RefreshAllStreams();
							break;
						}
						this.r.x += this.r.width;

						GUI.enabled = true;
						if (GUI.Button(r, "X", GeneralStyles.ToolbarCloseButton) == true)
						{
							Undo.RecordObject(settings, "Delete color marker");
							settings.colorMarkers.RemoveAt(i);
							this.RefreshAllStreams();
							break;
						}
					}
					EditorGUI.EndDisabledGroup();

					this.r.y += this.r.height + ColorMarkersWizard.Spacing;

					if (this.folds[i] == true)
					{
						this.r.x = 0F;

						EditorGUI.BeginChangeCheck();
						{
							settings.colorMarkers[i].groupFilters.OnGUI(r);

							this.r.y += this.r.height + ColorMarkersWizard.Spacing;

							for (int j = 0; j < settings.colorMarkers[i].groupFilters.filters.Count; j++)
							{
								if (settings.colorMarkers[i].groupFilters.filters[j].Enabled == true)
								{
									this.r.x = 0F;
									this.r.width = width;
									this.r = settings.colorMarkers[i].groupFilters.filters[j].OnGUI(this.r, false);
								}
							}
						}
						if (EditorGUI.EndChangeCheck() == true)
							this.RefreshAllStreams();
					}

					this.r.y += ColorMarkersWizard.MarkerSpacing;
				}
			}
			GUI.EndScrollView();
		}

		private void	RefreshAllStreams()
		{
			Utility.RegisterIntervalCallback(this.DelayedRefreshStreams, 100, 1);
			HQ.InvalidateSettings();
			Utility.RepaintEditorWindow(typeof(NGConsoleWindow));
		}

		private void	DelayedSetBackgroundcolor()
		{
			Undo.RecordObject(HQ.Settings.Get<ColorMarkersModuleSettings>(), "Change color marker color");
			this.alteredColorMarker.backgroundColor = this.newColor;
			this.RefreshAllStreams();
		}

		private void	DelayedRefreshStreams()
		{
			NGConsoleWindow[]	consoles = Resources.FindObjectsOfTypeAll<NGConsoleWindow>();

			if (consoles.Length == 0)
				return;

			MainModule	main = consoles[0].GetModule("Main") as MainModule;

			if (main != null)
			{
				for (int i = 0; i < main.Streams.Count; i++)
					main.Streams[i].RefreshFilteredRows();

				HQ.InvalidateSettings();
				Utility.RepaintEditorWindow(typeof(NGConsoleWindow));
			}
		}

		private bool	CheckMaxColorMarkers(int count)
		{
			return NGLicensesManager.Check(count < ColorMarkersWizard.MaxColorMarkers, NGTools.NGConsole.NGAssemblyInfo.Name + " Pro", "Free version does not allow more than " + ColorMarkersWizard.MaxColorMarkers + " color markers.\n\nMarkers are truly awesome to pinpoint words or specific logs in a glance. :]");
		}
	}
}