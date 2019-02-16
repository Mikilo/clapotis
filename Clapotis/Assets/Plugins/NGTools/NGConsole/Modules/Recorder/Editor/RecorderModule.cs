using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	[Serializable]
	//[VisibleModule(100)]
	internal sealed class RecorderModule : Module, IStreams
	{
		[Serializable]
		private sealed class Vars
		{
			public int	workingStream;
		}

		public event Action<StreamLog>	StreamAdded;
		public event Action<StreamLog>	StreamDeleted;

		public List<StreamLog>	Streams { get { return new List<StreamLog>(this.streams.ToArray()); } }
		public int				WorkingStream { get { return this.perWindowVars.Get(RowUtility.drawingWindow).workingStream; } }

		[Exportable(ExportableAttribute.ArrayOptions.Immutable)]
		private List<SampleStream>	streams;

		[SerializeField]
		private PerWindowVars<Vars>	perWindowVars;

		[NonSerialized]
		private Vars	currentVars;

		private GUIStyle	requirePlayModeStyle;

		public	RecorderModule()
		{
			this.name = "Recorder";
			this.streams = new List<SampleStream>();
			this.perWindowVars = new PerWindowVars<Vars>();
		}

		public override void	OnEnable(NGConsoleWindow console, int id)
		{
			base.OnEnable(console, id);

			foreach (var stream in this.streams)
				stream.Init(this.console, this);

			this.console.ConsoleCleared += this.Clear;
			this.console.wantsMouseMove = true;

			// Populate with default commands if missing.
			HQ.Settings.Get<ConsoleSettings>().inputsManager.AddCommand("Navigation", ConsoleConstants.SwitchNextStreamCommand, KeyCode.Tab, true);
			HQ.Settings.Get<ConsoleSettings>().inputsManager.AddCommand("Navigation", ConsoleConstants.SwitchPreviousStreamCommand, KeyCode.Tab, true, true);

			if (this.perWindowVars == null)
				this.perWindowVars = new PerWindowVars<Vars>();
		}

		public override void	OnDisable()
		{
			this.console.ConsoleCleared -= this.Clear;
			this.console.wantsMouseMove = false;

			foreach (var stream in this.streams)
				stream.Uninit();
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
			this.console.RemoveNotification();
		}

		public override void	OnGUI(Rect r)
		{
			this.currentVars = this.perWindowVars.Get(RowUtility.drawingWindow);

			float	yOrigin = r.y;
			float	maxHeight = r.height;

			if (EditorApplication.isPlaying == false && this.streams.Count > 0)
			{
				if (this.requirePlayModeStyle == null)
				{
					this.requirePlayModeStyle = new GUIStyle(GUI.skin.label);
					this.requirePlayModeStyle.alignment = TextAnchor.MiddleRight;
					this.requirePlayModeStyle.normal.textColor = Color.green;
				}

				r.height = Constants.SingleLineHeight;
				GUI.Label(r, LC.G("SampleStream_RequirePlayMode"), this.requirePlayModeStyle);
			}

			r.x = 0F;
			r.width = this.console.position.width;
			r = this.DrawSampleTabs(r);

			r.x = 0F;
			r.width = this.console.position.width;
			r.height = maxHeight - (r.y - yOrigin);

			this.currentVars.workingStream = Mathf.Clamp(this.currentVars.workingStream, 0, this.streams.Count - 1);
			if (0 <= this.currentVars.workingStream && this.currentVars.workingStream < this.streams.Count)
				this.streams[this.currentVars.workingStream].OnGUI(r);
			else
				GUI.Label(r, LC.G("RecorderModule_NoSampleCreated"), GeneralStyles.CenterText);
		}

		public void	Clear()
		{
			foreach (var stream in this.streams)
				stream.Clear();
		}

		public void	FocusStream(int i)
		{
			if (i < 0)
				this.currentVars.workingStream = 0;
			else if (i >= this.streams.Count)
				this.currentVars.workingStream = this.streams.Count - 1;
			else
				this.currentVars.workingStream = i;
		}

		public void	DeleteStream(int i)
		{
			StreamLog	stream = this.streams[i];
			stream.Uninit();
			this.streams.RemoveAt(i);

			if (this.StreamDeleted != null)
				this.StreamDeleted(stream);

			foreach (Vars var in this.perWindowVars.Each())
				var.workingStream = Mathf.Clamp(var.workingStream, 0, this.streams.Count - 1);
		}

		private Rect	DrawSampleTabs(Rect r)
		{
			ConsoleSettings	settings = HQ.Settings.Get<ConsoleSettings>();

			r.height = Constants.SingleLineHeight;

			// Switch stream
			if (settings.inputsManager.Check("Navigation", ConsoleConstants.SwitchNextStreamCommand) == true)
			{
				this.currentVars.workingStream += 1;
				if (this.currentVars.workingStream >= this.streams.Count)
					this.currentVars.workingStream = 0;

				Event.current.Use();
			}
			if (settings.inputsManager.Check("Navigation", ConsoleConstants.SwitchPreviousStreamCommand) == true)
			{
				this.currentVars.workingStream -= 1;
				if (this.currentVars.workingStream < 0)
					this.currentVars.workingStream = this.streams.Count - 1;

				Event.current.Use();
			}

			GUILayout.BeginArea(r);
			{
				GUILayout.BeginHorizontal();
				{
					for (int i = 0; i < this.streams.Count; i++)
						r = this.streams[i].OnTabGUI(r, i);

					if (GUILayout.Button("+", HQ.Settings.Get<GeneralSettings>().MenuButtonStyle) == true)
					{
						MainModuleSettings	mainSettings = HQ.Settings.Get<MainModuleSettings>();
						SampleStream		stream = new SampleStream();

						stream.Init(this.console, this);

						foreach (ILogFilter filter in mainSettings.GenerateFilters())
							stream.groupFilters.filters.Add(filter);

						this.streams.Add(stream);

						if (this.streams.Count == 1)
							this.currentVars.workingStream = 0;

						if (this.StreamAdded != null)
							this.StreamAdded(stream);

						stream.RefreshFilteredRows();
					}

					GUILayout.FlexibleSpace();
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndArea();

			r.y += r.height + 2F;

			return r;
		}

		private void	RenameStream(object data, string newName)
		{
			if (string.IsNullOrEmpty(newName) == false)
				this.streams[(int)data].name = newName;
		}

		private Rect	GUIExport(Rect r)
		{
			Vars	vars = this.perWindowVars.Get(RowUtility.drawingWindow);

			EditorGUI.BeginDisabledGroup(this.streams[vars.workingStream].rowsDrawer.Count == 0);
			{
				Utility.content.text = LC.G("RecorderModule_ExportSamples");
				float	x = r.x;
				float	width = HQ.Settings.Get<GeneralSettings>().MenuButtonStyle.CalcSize(Utility.content).x;
				r.x = r.x + r.width - width;
				r.width = width;

				if (GUI.Button(r, Utility.content, HQ.Settings.Get<GeneralSettings>().MenuButtonStyle) == true)
				{
					List<Row>	rows = new List<Row>();

					for (int i = 0; i < this.streams[vars.workingStream].rowsDrawer.Count; i++)
						rows.Add(this.console.rows[this.streams[vars.workingStream].rowsDrawer[i]]);

					ExportLogsWindow.Export(rows);
				}

				r.width = r.x - x;
				r.x = x;
			}
			EditorGUI.EndDisabledGroup();

			return r;
		}
	}
}