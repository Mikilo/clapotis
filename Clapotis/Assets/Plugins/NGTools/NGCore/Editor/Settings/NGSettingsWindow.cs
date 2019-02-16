using NGTools;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor
{
	[PrewarmEditorWindow]
	public class NGSettingsWindow : EditorWindow, IHasCustomMenu
	{
		private class Section
		{
			public string	title;
			public Action	onGUI;
			public int		priority;
			public int		optimalFontSize;

			public	Section(string title, Action OnGUI, int priority)
			{
				this.title = title;
				this.onGUI = OnGUI;
				this.priority = priority;
			}

			public void	OptimizeFontSize(GUIStyle style, float sectionWidth)
			{
				if (this.optimalFontSize != 0)
					return;

				int	fontSize = style.fontSize;

				style.fontSize = NGSettingsWindow.SectionDefaultFontSize;

				// Shrink title to fit the space.
				Utility.content.text = this.title;
				while (style.CalcSize(Utility.content).x >= sectionWidth &&
					   style.fontSize > NGSettingsWindow.SectionMinFontSize)
				{
					--style.fontSize;
				}

				this.optimalFontSize = style.fontSize;

				style.fontSize = fontSize;
			}
		}

		public const string	Title = "NG Settings";
		public static Color	TitleColor = Color.white;
		public const string	LastSectionPrefKey = "NGSettings_lastSection";
		public const float	SectionWidth = 140F;
		public const int	SectionDefaultFontSize = 14;
		public const int	SectionMinFontSize = 6;

		private static List<Section>	sections = new List<Section>();

		private Section	workingSection;
		private Vector2	sectionsScrollPosition;
		private Vector2	sectionScrollPosition;

		[NonSerialized]
		private GUIStyle	sectionScrollView;
		[NonSerialized]
		private GUIStyle	sectionElement;
		[NonSerialized]
		private GUIStyle	selected;

		[NonSerialized]
		private bool	viewOverflow;
		[NonSerialized]
		private Rect	r;
		[NonSerialized]
		private Rect	body;
		[NonSerialized]
		private Rect	viewRect;

		static	NGSettingsWindow()
		{
			foreach (Type type in Utility.EachNGTSubClassesOf(typeof(object)))
			{
				if (typeof(ScriptableObject).IsAssignableFrom(type) == true)
				{
					NGSettingsAttribute[]	attributes = type.GetCustomAttributes(typeof(NGSettingsAttribute), false) as NGSettingsAttribute[];

					if (attributes.Length > 0)
						new SectionDrawer(attributes[0].label, type, attributes[0].priority);
				}

				MethodInfo[]	methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static);

				for (int i = 0; i < methods.Length; i++)
				{
					if (methods[i].IsDefined(typeof(NGSettingsAttribute), false) == true)
					{
						NGSettingsAttribute[]	attributes = methods[i].GetCustomAttributes(typeof(NGSettingsAttribute), false) as NGSettingsAttribute[];

						NGSettingsWindow.AddSection(attributes[0].label, Delegate.CreateDelegate(typeof(Action), methods[i]) as Action, attributes[0].priority);
					}
				}
			}
		}

		[MenuItem(Constants.MenuItemPath + NGSettingsWindow.Title, priority = Constants.MenuItemPriority + 1), Hotkey(NGSettingsWindow.Title)]
		public static void	Open()
		{
			Utility.OpenWindow<NGSettingsWindow>(false, NGSettingsWindow.Title, true);
		}

		/// <summary></summary>
		/// <param name="title">Defines the name of the Section.</param>
		/// <param name="callback">Method to draw GUI.</param>
		/// <param name="priority">The lower the nearest to the top.</param>
		public static void	AddSection(string title, Action callback, int priority = -1)
		{
			for (int i = 0; i < NGSettingsWindow.sections.Count; i++)
			{
				// Overwrite when found.
				if (NGSettingsWindow.sections[i].title == title)
				{
					NGSettingsWindow.sections[i].onGUI = callback;
					return;
				}
			}

			if (priority >= 0)
			{
				for (int i = 0; i < NGSettingsWindow.sections.Count; i++)
				{
					if (priority <= NGSettingsWindow.sections[i].priority ||
						NGSettingsWindow.sections[i].priority == -1)
					{
						NGSettingsWindow.sections.Insert(i, new Section(title, callback, priority));
						return;
					}
				}
			}

			NGSettingsWindow.sections.Add(new Section(title, callback, priority));
		}

		/// <summary></summary>
		/// <param name="title">Defines the name of the Section.</param>
		public static void	RemoveSection(string title)
		{
			for (int i = 0; i < NGSettingsWindow.sections.Count; i++)
			{
				if (NGSettingsWindow.sections[i].title == title)
				{
					NGSettingsWindow.sections.RemoveAt(i);
					break;
				}
			}
		}

		protected virtual void	OnEnable()
		{
			Utility.RegisterWindow(this);
			Utility.RestoreIcon(this, NGSettingsWindow.TitleColor);

			if (NGSettingsWindow.sections.Count > 0)
				this.workingSection = NGSettingsWindow.sections[0];

			this.r = new Rect(0F, 40F, NGSettingsWindow.SectionWidth, 0F);
			this.body = new Rect(0F, 0F, NGSettingsWindow.SectionWidth, 0F);
			this.viewRect = new Rect();

			HQ.SettingsChanged += this.Repaint;
			Undo.undoRedoPerformed += this.Repaint;

			EditorApplication.delayCall += () =>
			{
				// As crazy as it seems, we need 3 nested delayed calls. Because we need to ensure everybody is in the room to start the party.
				this.Focus(NGEditorPrefs.GetString(NGSettingsWindow.LastSectionPrefKey));
				this.Repaint();
			};

			this.wantsMouseMove = true;
		}

		protected virtual void	OnDisable()
		{
			Utility.UnregisterWindow(this);
			HQ.SettingsChanged -= this.Repaint;
			Undo.undoRedoPerformed -= this.Repaint;

			if (this.workingSection != null)
				NGEditorPrefs.SetString(NGSettingsWindow.LastSectionPrefKey, this.workingSection.title);
		}

		public void	OnGUI()
		{
			this.InitGUIStyles();

			EditorGUIUtility.labelWidth = 200f;
			GUILayout.BeginHorizontal();
			{
				this.r.y = 40F;
				this.body.height = this.position.height;
				this.viewRect.height = 40F + NGSettingsWindow.sections.Count * r.height;

				if ((this.viewOverflow == false && viewRect.height > body.height) ||
					(this.viewOverflow == true && viewRect.height <= body.height))
				{
					this.viewOverflow = !this.viewOverflow;

					if (this.viewOverflow == true)
					{
						this.sectionElement.padding.left = 1;
						this.sectionElement.padding.right = 1;
					}
					else
					{
						this.sectionElement.padding.left = 5;
						this.sectionElement.padding.right = 5;
					}

					this.r.width = NGSettingsWindow.SectionWidth - (this.viewOverflow == true ? 16F : 0F);

					for (int i = 0; i < NGSettingsWindow.sections.Count; i++)
						NGSettingsWindow.sections[i].optimalFontSize = 0;
				}
				else
					this.r.width = NGSettingsWindow.SectionWidth - (this.viewOverflow == true ? 16F : 0F);

				GUI.Box(body, string.Empty, sectionScrollView);

				this.sectionsScrollPosition = GUI.BeginScrollView(body, this.sectionsScrollPosition, viewRect);
				{
					for (int i = 0; i < NGSettingsWindow.sections.Count; i++)
					{
						NGSettingsWindow.sections[i].OptimizeFontSize(this.sectionElement, this.r.width);
						this.sectionElement.fontSize = NGSettingsWindow.sections[i].optimalFontSize;

						if (NGSettingsWindow.sections[i] == this.workingSection && Event.current.type == EventType.Repaint)
						{
							this.r.xMin += 1F;
							this.selected.Draw(this.r, false, false, false, false);
							this.r.xMin -= 1F;
						}

						EditorGUI.BeginChangeCheck();
						if (GUI.Toggle(this.r, this.workingSection == NGSettingsWindow.sections[i], NGSettingsWindow.sections[i].title, this.sectionElement))
							this.workingSection = NGSettingsWindow.sections[i];
						this.r.y += this.r.height;
						if (EditorGUI.EndChangeCheck())
							GUIUtility.keyboardControl = 0;
					}

					if (Conf.DebugMode != Conf.DebugState.None && HQ.Settings != null)
						GUI.Label(r, "Version " + HQ.Settings.version, this.sectionElement);
				}
				GUI.EndScrollView();

				GUILayout.Space(body.width);

				if (NGSettingsWindow.sections.Contains(this.workingSection) == true)
				{
					this.sectionScrollPosition = GUILayout.BeginScrollView(this.sectionScrollPosition);
					{
						GUILayout.BeginHorizontal();
						GUILayout.Space(1F);
						GUILayout.BeginVertical();
						{
							GUILayout.Label(this.workingSection.title, GeneralStyles.MainTitle);
							this.workingSection.onGUI();
						}
						GUILayout.EndVertical();
						GUILayout.EndHorizontal();
					}
					GUILayout.EndScrollView();
				}
				else if (NGSettingsWindow.sections.Count > 0 && Event.current.type == EventType.Repaint)
				{
					this.workingSection = NGSettingsWindow.sections[0];
					this.Repaint();
				}
			}
			GUILayout.EndHorizontal();
		}

		public void	Focus(string title)
		{
			for (int i = 0; i < NGSettingsWindow.sections.Count; i++)
			{
				if (NGSettingsWindow.sections[i].title == title)
				{
					this.workingSection = NGSettingsWindow.sections[i];
					NGEditorPrefs.SetString(NGSettingsWindow.LastSectionPrefKey, this.workingSection.title);
				}
			}
		}

		private void	InitGUIStyles()
		{
			if (this.sectionScrollView != null)
				return;

			this.sectionScrollView = new GUIStyle("PreferencesSectionBox");
			this.sectionScrollView.overflow.bottom++;
			this.sectionScrollView.onNormal = this.sectionScrollView.onHover;

			this.sectionElement = new GUIStyle("PreferencesSection");
			this.sectionElement.padding.left = 5;
			this.sectionElement.padding.right = 5;

			this.r.height = this.sectionElement.fixedHeight;

			if (Utility.UnityVersion[0] == '5')
				this.selected = "ServerUpdateChangesetOn";
			else
				this.selected = "OL SelectedRow";
		}

		void	IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			Utility.AddNGMenuItems(menu, this, NGSettingsWindow.Title, Constants.WikiBaseURL + "#markdown-header-120-ng-settings", true);
		}
	}
}