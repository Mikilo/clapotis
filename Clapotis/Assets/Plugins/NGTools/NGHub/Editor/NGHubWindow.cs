using NGLicenses;
using NGTools;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace NGToolsEditor.NGHub
{
	using UnityEngine;

	[InitializeOnLoad, Exportable(ExportableAttribute.ArrayOptions.Overwrite | ExportableAttribute.ArrayOptions.Immutable)]
	public class NGHubWindow : EditorWindow, ISettingExportable, IHasCustomMenu
	{
		private enum DockState
		{
			ProperlyEnabled = -1,
			Closed = 0,
			ProperlyDisabled = 1
		}

		public const string	Title = "NG Hub";
		public static Color	TitleColor = new Color(0F, 0F, 128F / 255F, 1F); // Navy
		public const string	ForceRecreateKeyPref = "NGHub_ForceRecreate";
		public const string	BackgroundColorKeyPref = "NGHub.backgroundColor";
		public const string	DragFromNGHub = "DragFromNGHub";
		public static Color	DockBackgroundColor { get { return Utility.GetSkinColor(41F / 255F, 41F / 255F, 41F / 255F, 1F, 162F / 255F, 162F / 255F, 162F / 255F, 1F); } }

		private const int				MaxHubComponents = 3;
		private static readonly string	FreeAdContent = NGHubWindow.Title + " is restrained to " + NGHubWindow.MaxHubComponents + " components.";

		public float	height = Constants.SingleLineHeight;

		private bool	initialized;
		public bool		Initialized { get { return this.initialized; } }

		private MethodInfo[]	droppableComponents;
		public MethodInfo[]		DroppableComponents
		{
			get
			{
				if (this.droppableComponents == null)
				{
					List<MethodInfo>	methods = new List<MethodInfo>(2);

					foreach (Type type in Utility.EachNGTSubClassesOf(typeof(HubComponent)))
					{
						MethodInfo	method = type.GetMethod(HubComponent.StaticVerifierMethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

						if (method != null)
							methods.Add(method);
					}

					this.droppableComponents = methods.ToArray();
				}

				return this.droppableComponents;
			}
		}

		[Exportable(ExportableAttribute.ArrayOptions.Immutable)]
		public List<HubComponent>	components { get; private set; }

		[SerializeField]
		private bool	dockedAsMenu = false;
		public bool		DockedAsMenu { get { return this.dockedAsMenu; } }

		public Color				backgroundColor;
		public NGHubExtensionWindow	extensionWindow;

		[SerializeField]
		private bool	initOnce;

		private ErrorPopup	errorPopup = new ErrorPopup(NGHubWindow.Title, "Error occurred");
		private float		maxWidth = 0F;

		static	NGHubWindow()
		{
			// In the case of NG Hub as dock, the layout won't load it at the second restart. Certainly due to the window's state as Popup, but it does not explain why it only occurs at the second restart.
			EditorApplication.delayCall += () =>
			{
				int	forceRecreate = NGEditorPrefs.GetInt(NGHubWindow.ForceRecreateKeyPref + "_" + Application.dataPath, 0);
				NGDiagnostic.Log(NGHubWindow.Title, "ForceRecreate", forceRecreate);
				if (forceRecreate == (int)DockState.ProperlyDisabled && Resources.FindObjectsOfTypeAll<NGHubWindow>().Length == 0)
					NGHubWindow.OpenAsDock();
			};
		}

		[NGSettings(NGHubWindow.Title)]
		private static void	OnGUISettings()
		{
			if (HQ.Settings == null)
				return;

			EditorGUILayout.Space();

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.LabelField("Add an offset on NG Hub's window Y-axis.", GeneralStyles.WrapLabel);
			float	offset = EditorGUILayout.FloatField("Y Offset Window", HQ.Settings.Get<HubSettings>().NGHubYOffset);

			EditorGUILayout.HelpBox("This is a workaround. Most of the time NG Hub is correctly placed, but in some cases, it is not.", MessageType.Info);
			EditorGUILayout.HelpBox("If you have the offset bug and you are willing to fix it, contact me through " + Constants.SupportEmail + ".", MessageType.Info);

			if (GUILayout.Button("Contact the author") == true)
				ContactFormWizard.Open(ContactFormWizard.Subject.Support);

			if (EditorGUI.EndChangeCheck() == true)
			{
				HQ.Settings.Get<HubSettings>().NGHubYOffset = offset;
				HQ.InvalidateSettings();
			}
		}

		[MenuItem(Constants.MenuItemPath + NGHubWindow.Title, priority = Constants.MenuItemPriority + 300), Hotkey(NGHubWindow.Title)]
		public static void	Open()
		{
			Utility.OpenWindow<NGHubWindow>(NGHubWindow.Title);
		}

		[MenuItem(Constants.MenuItemPath + NGHubWindow.Title + " as Dock", priority = Constants.MenuItemPriority + 301), Hotkey(NGHubWindow.Title + " as Dock")]
		public static void	OpenAsDock()
		{
			NGHubWindow[]	editors = Resources.FindObjectsOfTypeAll<NGHubWindow>();

			for (int i = 0; i < editors.Length; i++)
			{
				if (editors[i].dockedAsMenu == true)
					return;
				editors[i].Close();
			}

			NGHubWindow	window = ScriptableObject.CreateInstance<NGHubWindow>();
			window.SetDockMode(true);
			window.ShowPopup();
		}

		private void	OnSettingsChanged()
		{
			if (this.initialized == true)
			{
				this.OnDisable();
				this.OnEnable();
			}

			this.Repaint();
		}

		[NGSettingsChanged]
		private static void	OnSettingsGenerated(ScriptableObject settings)
		{
			CustomHotkeysSettings	hotkeys = settings as CustomHotkeysSettings;

			if (hotkeys != null)
				hotkeys.hotkeys.Add(new CustomHotkeysSettings.MethodHotkey() { bind = "%#H", staticMethod = typeof(NGHubWindow).FullName + ".OpenAsDock" });
		}

		protected virtual void	OnEnable()
		{
			Utility.RestoreIcon(this, NGHubWindow.TitleColor);

			Metrics.UseTool(4); // NGHub

			NGChangeLogWindow.CheckLatestVersion(NGAssemblyInfo.Name);

			if (this.initialized == true || HQ.Settings == null)
				return;

			try
			{
				this.titleContent.text = NGHubWindow.Title;
				this.minSize = Vector2.zero;
				this.components = new List<HubComponent>();
				this.RestoreComponents();

				if (this.initOnce == false)
					this.backgroundColor = (Color)Utility.LoadEditorPref(this.backgroundColor, typeof(Color), NGHubWindow.BackgroundColorKeyPref);

				HQ.SettingsChanged += this.OnSettingsChanged;
				Undo.undoRedoPerformed += this.RestoreComponents;

				// Force repaint to hide part.
				EditorApplication.delayCall += EditorApplication.delayCall += this.Repaint;

				NGDiagnostic.DelayDiagnostic(this.Diagnose);

				if (this.dockedAsMenu == true)
					this.SetDockMode(true);

				this.initialized = true;
			}
			catch (Exception ex)
			{
				this.errorPopup.exception = ex;
			}

			this.initOnce = true;
		}

		protected virtual void	OnDisable()
		{
			InternalNGDebug.VerboseLog("NGHub.OnDisable," + this.initialized + "," + this.dockedAsMenu);
			if (this.initialized == false)
				return;

			if (this.dockedAsMenu == true)
				NGEditorPrefs.SetInt(NGHubWindow.ForceRecreateKeyPref + "_" + Application.dataPath, (int)DockState.ProperlyDisabled);

			HQ.SettingsChanged -= this.OnSettingsChanged;
			Undo.undoRedoPerformed -= this.RestoreComponents;

			Utility.DirectSaveEditorPref(this.backgroundColor, typeof(Color), NGHubWindow.BackgroundColorKeyPref);

			this.initialized = false;

			for (int i = 0; i < this.components.Count; i++)
				this.components[i].Uninit();
		}

		protected virtual void	OnGUI()
		{
			if (this.initialized == false)
			{
				// Prevent initialization message after entering play mode.
				if (Time.frameCount <= 2)
					return;

				GUILayout.Label(string.Format(LC.G("RequiringConfigurationFile"), NGHubWindow.Title), GeneralStyles.CenterText, GUILayoutOptionPool.Height(this.height));

				if (this.dockedAsMenu == false)
				{
					GUILayout.BeginHorizontal();
					{
						if (GUILayout.Button(LC.G("ShoWPreferencesWindow")) == true)
							Utility.ShowPreferencesWindowAt(Constants.PreferenceTitle);

						// Especially for NG Hub, we need to add a way to manually close the window when the dock mode is failing.
						if (GUILayout.Button("X", GUILayoutOptionPool.Width(16F)) == true)
							this.Close();
					}
					GUILayout.EndHorizontal();
				}

				return;
			}

			FreeLicenseOverlay.First(this, NGAssemblyInfo.Name + " Pro", NGHubWindow.FreeAdContent);

			if (Event.current.type == EventType.Repaint)
			{
				if (this.backgroundColor.a > 0F)
					EditorGUI.DrawRect(new Rect(0F, 0F, this.position.width, this.position.height), this.backgroundColor);
				else
					EditorGUI.DrawRect(new Rect(0F, 0F, this.position.width, this.position.height), NGHubWindow.DockBackgroundColor);
			}

			EditorGUILayout.BeginHorizontal(GUILayoutOptionPool.Height(this.height));
			{
				if (this.errorPopup.exception != null)
				{
					if (this.dockedAsMenu == true)
					{
						Rect r = GUILayoutUtility.GetRect(0F, 0F, GUILayoutOptionPool.Width(115F), GUILayoutOptionPool.Height(this.height + 3F));
						r.x += 1F;
						r.y += 1F;
						this.errorPopup.OnGUIRect(r);
					}
					else
						this.errorPopup.OnGUILayout();
				}

				bool		isDragging = this.HandleDrop();
				bool		overflow = false;
				EventType	catchedType = EventType.Used;

				if (this.components.Count == 0)
				{
					if (isDragging == false)
					{
						Rect	r = this.position;
						r.x = 1F;
						r.y = 1F;
						r.width -= 1F;
						r.height -= 1F;

						if (this.dockedAsMenu == true && Event.current.type == EventType.Repaint)
							Utility.DrawRectDotted(r, this.position, Color.grey, .02F, 0F);

						GUI.Label(r, "Right-click to add Component" + (this.dockedAsMenu == true && Application.platform == RuntimePlatform.OSXEditor? " (Dock mode is buggy under OSX)" : ""), GeneralStyles.CenterText);
					}
				}
				else
				{
					Rect	miseryRect = default(Rect);
					int		lastMinI = this.extensionWindow != null ? this.extensionWindow.minI : 0;

					if (this.dockedAsMenu == true &&
						this.extensionWindow != null &&
						this.maxWidth > 0F)
					{
						miseryRect = new Rect(this.maxWidth, 0F, this.position.width - this.maxWidth, this.height + 4F);
						GUI.Label(miseryRect, GUIContent.none);

						if (miseryRect.Contains(Event.current.mousePosition) == true)
							GUIUtility.hotControl = 0;
					}

					for (int i = 0; i < this.components.Count; i++)
					{
						// Catch event from the cropped component.
						if (this.dockedAsMenu == true &&
							Event.current.type != EventType.Repaint &&
							Event.current.type != EventType.Layout &&
							this.extensionWindow != null)
						{
							if (this.extensionWindow.minI == i)
							{
								// Simulate context click, because MouseUp is used, therefore ContextClick is not sent.
								if (Event.current.type == EventType.MouseUp &&
									Event.current.button == 1)
								{
									catchedType = EventType.ContextClick;
								}
								else
									catchedType = Event.current.type;
								Event.current.Use();
							}
						}

						EditorGUILayout.BeginHorizontal();
						{
							try
							{
								this.components[i].OnGUI();
							}
							catch (Exception ex)
							{
								this.errorPopup.exception = ex;
							}
						}
						EditorGUILayout.EndHorizontal();

						if (this.dockedAsMenu == true && Event.current.type == EventType.Repaint)
						{
							Rect	r = GUILayoutUtility.GetLastRect();

							if (r.xMax >= this.position.width)
							{
								if (this.extensionWindow == null)
								{
									this.extensionWindow = ScriptableObject.CreateInstance<NGHubExtensionWindow>();
									this.extensionWindow.Init(this);
									this.extensionWindow.ShowPopup();
									this.Repaint();
								}

								this.maxWidth = r.xMin;
								this.extensionWindow.minI = i;
								overflow = true;
								break;
							}
							else if (this.position.width - r.xMax <= 16F && i + 1 < this.components.Count) // Prevent drawing next component if the space is obviously too small.
							{
								if (this.extensionWindow == null)
								{
									this.extensionWindow = ScriptableObject.CreateInstance<NGHubExtensionWindow>();
									this.extensionWindow.Init(this);
									this.extensionWindow.ShowPopup();
									this.Repaint();
								}

								this.maxWidth = r.xMax;
								this.extensionWindow.minI = i + 1;
								overflow = true;
								break;
							}
							else
								this.maxWidth = 0F;
						}
					}

					if (this.dockedAsMenu == true)
					{
						if (this.extensionWindow != null &&
							this.maxWidth > 0F)
						{
							if (lastMinI != this.extensionWindow.minI)
								this.Repaint();

							// Hide the miserable trick...
							if (Event.current.type == EventType.Repaint)
							{
								if (this.backgroundColor.a > 0F)
									EditorGUI.DrawRect(miseryRect, this.backgroundColor);
								else
									EditorGUI.DrawRect(miseryRect, NGHubWindow.DockBackgroundColor);
							}
						}
						else
						{
							Rect	r = GUILayoutUtility.GetLastRect();

							r.xMin = r.xMax + 1F;
							r.width = this.position.width - r.x;
							r.yMin -= 3F;
							r.yMax += 3F;

							if (Event.current.type == EventType.Repaint)
							{
								if (this.backgroundColor.a > 0F)
									EditorGUI.DrawRect(r, this.backgroundColor);
								else
									EditorGUI.DrawRect(r, NGHubWindow.DockBackgroundColor);
							}
						}
					}
				}

				if (this.dockedAsMenu == true &&
					Event.current.type == EventType.Repaint &&
					overflow == false &&
					this.extensionWindow != null)
				{
					this.extensionWindow.Close();
					this.extensionWindow = null;
					base.Repaint();
				}

				if (Event.current.type == EventType.ContextClick ||
					catchedType == EventType.ContextClick)
				{
					this.OpenContextMenu();
				}
			}

			GUILayout.FlexibleSpace();

			EditorGUILayout.EndHorizontal();

			FreeLicenseOverlay.Last(NGAssemblyInfo.Name + " Pro");
		}

		protected virtual void	Update()
		{
			if (this.initialized == false)
			{
				this.OnDisable();
				this.OnEnable();
			}
		}

		public bool	HandleDrop()
		{
			if (DragAndDrop.objectReferences.Length > 0 &&
				1.Equals(DragAndDrop.GetGenericData(Utility.DragObjectDataName)) == false &&
				true.Equals(DragAndDrop.GetGenericData(NGHubWindow.DragFromNGHub)) == false)
			{
				MethodInfo[]	droppableComponents = this.DroppableComponents;

				for (int i = 0; i < droppableComponents.Length; i++)
				{
					if ((bool)droppableComponents[i].Invoke(null, null) == true)
					{
						string	name = droppableComponents[i].DeclaringType.Name;

						if (name.EndsWith("Component") == true)
							name = name.Substring(0, name.Length - "Component".Length);

						Utility.content.text = name;
						Rect	r = GUILayoutUtility.GetRect(GUI.skin.label.CalcSize(Utility.content).x, this.height, GUI.skin.label);

						if (Event.current.type == EventType.Repaint)
						{
							Utility.DropZone(r, Utility.NicifyVariableName(name));
							this.Repaint();
						}
						else if (Event.current.type == EventType.DragUpdated &&
								 r.Contains(Event.current.mousePosition) == true)
						{
							DragAndDrop.visualMode = DragAndDropVisualMode.Move;
						}
						else if (Event.current.type == EventType.DragPerform &&
								 r.Contains(Event.current.mousePosition) == true)
						{
							DragAndDrop.AcceptDrag();

							if (this.CheckMaxHubComponents(this.components.Count) == true)
							{
								HubComponent	component = Activator.CreateInstance(this.droppableComponents[i].DeclaringType) as HubComponent;

								if (component != null)
								{
									component.InitDrop(this);
									this.components.Insert(0, component);
									EditorApplication.delayCall += this.Repaint;
									this.SaveComponents();
								}
							}

							DragAndDrop.PrepareStartDrag();
							Event.current.Use();
						}
					}
				}

				return true;
			}

			return false;
		}

		public new void	Repaint()
		{
			base.Repaint();
			if (this.extensionWindow != null)
				this.extensionWindow.Repaint();
		}

		public void	OpenContextMenu()
		{
			GenericMenu	menu = new GenericMenu();

			menu.AddItem(new GUIContent("Add Component"), false, this.OpenAddComponentWizard);
			menu.AddItem(new GUIContent("Edit"), false, () => EditorWindow.GetWindow<NGHubEditorWindow>(true, NGHubEditorWindow.Title, true).Init(this));
			menu.AddItem(new GUIContent("Dock as menu"), this.dockedAsMenu, (this.dockedAsMenu == true) ? new GenericMenu.MenuFunction(this.ConvertToWindow) : this.ConvertToDock);

			menu.AddSeparator(string.Empty);

			Utility.AddNGMenuItems(menu, this, NGHubWindow.Title, NGAssemblyInfo.WikiURL);

			menu.AddSeparator(string.Empty);

			menu.AddItem(new GUIContent("Close"), false, this.CloseDock);
			menu.ShowAsContext();

			Event.current.Use();
		}

		public void	ConvertToDock()
		{
			this.Close();

			NGHubWindow	window = ScriptableObject.CreateInstance<NGHubWindow>();
			window.SetDockMode(true);
			window.ShowPopup();
		}

		public void	ConvertToWindow()
		{
			this.Close();

			NGHubWindow	 window = EditorWindow.GetWindow<NGHubWindow>();
			window.position = this.position;
			window.Show();
		}

		public void	OpenAddComponentWizard()
		{
			GenericTypesSelectorWizard	wizard = GenericTypesSelectorWizard.Start(NGHubWindow.Title + " - Add Component", typeof(HubComponent), this.OnCreateComponent, true, true);
			wizard.EnableCategories = true;
			wizard.position = new Rect(this.position.x, this.position.y + 60F, wizard.position.width, wizard.position.height);
		}

		public void	SaveComponents()
		{
			EditorApplication.delayCall += this.Repaint;
			HubSettings	settings = HQ.Settings.Get<HubSettings>();
			Undo.RecordObject(settings, "Change HubComponent");
			settings.hubData.Serialize(this.components);
			HQ.InvalidateSettings();
		}

		private void	CloseDock()
		{
			NGEditorPrefs.SetInt(NGHubWindow.ForceRecreateKeyPref + "_" + Application.dataPath, (int)DockState.Closed);
			this.Close();
		}

		private void	SetDockMode(bool mode)
		{
			this.dockedAsMenu = mode;

			if (mode == true)
			{
				NGEditorPrefs.SetInt(NGHubWindow.ForceRecreateKeyPref + "_" + Application.dataPath, (int)DockState.ProperlyEnabled);
				this.UpdateDockPosition();
				Utility.RegisterIntervalCallback(this.UpdateDockPosition, 50);
			}
			else
			{
				NGEditorPrefs.SetInt(NGHubWindow.ForceRecreateKeyPref + "_" + Application.dataPath, (int)DockState.ProperlyDisabled);
				Utility.UnregisterIntervalCallback(this.UpdateDockPosition);
			}
		}

		private void	UpdateDockPosition()
		{
			Rect	r = Utility.GetEditorMainWindowPos();
			float	leftInputsWidth = 330F;
			float	yOffset = HQ.Settings != null ? HQ.Settings.Get<HubSettings>().NGHubYOffset : 0F;
			string	unityVersion = Utility.UnityVersion;

			if (unityVersion[0] == '2' && (unityVersion[5] >= '3' || unityVersion[3] >= '8'))
				leftInputsWidth = 360F;

			r.width = r.width * .5F - leftInputsWidth - 60F; // Half window width - Left inputs width - Half Play/Pause/Next
			this.minSize = new Vector2(r.width, 25F);
			this.maxSize = this.minSize;

			try
			{
				this.position = new Rect(r.x + leftInputsWidth, r.y + 2F + yOffset, this.minSize.x, this.minSize.y);
			}
			catch (NullReferenceException)
			{
			}

			this.height = this.position.height - 4F;

			if (this.extensionWindow != null)
			{
				r.x += r.width + 105F; // Width + Play/Pause/Next

				if (unityVersion[0] == '5' && unityVersion[2] <= '4')
					r.width += 5F;
				else if (unityVersion[0] == '2' && (unityVersion[5] >= '3' || unityVersion[3] >= '8'))
					r.width += -50F;
				else
					r.width += -80F;
				this.extensionWindow.minSize = new Vector2(r.width, this.minSize.y);
				this.extensionWindow.maxSize = this.extensionWindow.minSize;
				this.extensionWindow.position = new Rect(r.x + leftInputsWidth + 10F, this.position.y, this.extensionWindow.minSize.x, this.extensionWindow.minSize.y);
			}
		}

		private void	OnCreateComponent(Type type)
		{
			if (this.CheckMaxHubComponents(this.components.Count) == false)
				return;

			HubComponent	component = Activator.CreateInstance(type) as HubComponent;
			component.Init(this);

			HubComponentWindow[]	editors = Resources.FindObjectsOfTypeAll<HubComponentWindow>();

			for (int i = 0; i < editors.Length; i++)
				editors[i].Close();

			if (component.hasEditorGUI == true)
			{
				HubComponentWindow	editor = EditorWindow.CreateInstance<HubComponentWindow>();

				editor.titleContent.text = component.name;
				editor.position = new Rect(this.position.x, this.position.y + this.height, Mathf.Max(HubComponentWindow.MinWidth, editor.position.width), editor.position.height);
				editor.Init(this, component);
				editor.ShowPopup();
			}

			this.components.Add(component);
			this.SaveComponents();
			this.Repaint();

			NGHubEditorWindow[]	windows = Resources.FindObjectsOfTypeAll<NGHubEditorWindow>();

			for (int i = 0; i < windows.Length; i++)
				windows[i].Repaint();
		}

		protected virtual void	ShowButton(Rect r)
		{
			EditorGUI.BeginChangeCheck();
			GUI.Toggle(r, false, "E", GUI.skin.label);
			if (EditorGUI.EndChangeCheck() == true)
				EditorWindow.GetWindow<NGHubEditorWindow>(true, NGHubEditorWindow.Title, true).Init(this);
		}

		private void	RestoreComponents()
		{
			HQ.Settings.Get<HubSettings>().hubData.Deserialize(this.components);

			for (int i = 0; i < this.components.Count; i++)
			{
				// In case of corrupted data.
				if (this.components[i] != null && this.components[i].GetType().IsSubclassOf(typeof(HubComponent)) == true)
					this.components[i].Init(this);
				else
				{
					this.components.RemoveAt(i);
					--i;
				}
			}

			this.Repaint();
		}

		private void	Diagnose()
		{
			NGDiagnostic.Log(NGHubWindow.Title, "IsDocked", this.dockedAsMenu);
			NGDiagnostic.Log(NGHubWindow.Title, "HasExtension", this.extensionWindow != null);

			if (this.initialized == true)
			{
				NGDiagnostic.Log(NGHubWindow.Title, "Components", this.components.Count);

				for (int i = 0; i < this.components.Count; i++)
					NGDiagnostic.Log(NGHubWindow.Title, "Components[" + i + "]", JsonUtility.ToJson(this.components[i]).Insert(1, "\"type\":\"" + this.components[i].GetType().Name + "\","));
			}
		}

		internal bool	CheckMaxHubComponents(int count)
		{
			return NGLicensesManager.Check(count < NGHubWindow.MaxHubComponents, NGAssemblyInfo.Name + " Pro", "Free version does not allow more than " + NGHubWindow.MaxHubComponents + " components.\n\nToo bad dude... But if you like this feature, you know what to do. ;}\n\nNote that if you have too many components, NG Hub will extend to the other side.");
		}

		void	IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			menu.AddItem(new GUIContent("Add Component"), false, this.OpenAddComponentWizard);
			menu.AddItem(new GUIContent("Edit"), false, () => EditorWindow.GetWindow<NGHubEditorWindow>(true, NGHubEditorWindow.Title, true).Init(this));
			menu.AddItem(new GUIContent("Dock as menu"), this.dockedAsMenu, (this.dockedAsMenu == true) ? new GenericMenu.MenuFunction(this.ConvertToWindow) : this.ConvertToDock);
			menu.AddSeparator("");
			Utility.AddNGMenuItems(menu, this, NGHubWindow.Title, NGAssemblyInfo.WikiURL);
		}

		void	ISettingExportable.PreExport()
		{
		}

		void	ISettingExportable.PreImport()
		{
		}

		void	ISettingExportable.PostImport()
		{
			for (int i = 0; i < this.components.Count; i++)
				this.components[i].hub = this;
		}
	}
}