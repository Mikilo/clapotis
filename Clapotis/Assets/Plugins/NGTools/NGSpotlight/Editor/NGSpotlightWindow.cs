using NGTools;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGSpotlight
{
	[PrewarmEditorWindow]
	public class NGSpotlightWindow : EditorWindow
	{
		private class EntryHardRef
		{
			public string	key;
			public string	content;
		}

		public const string	Title = "NG Spotlight";
		public const string	KeywordsPlaceholder = "Enter keywords or ':'";
		public const float	WindowWidth = 500F;
		public const float	WindowHeight = 400F;
		public const int	MaxResult = 50;
		public const float	DropdownWidth = 16F;
		public const float	RowHeight = 32F;
		public const float	Spacing = 2F;
		public const int	MaxLastUsed = 32;

		public readonly static Color	HighlightedEntryColor = Color.cyan;
		public readonly static Color	SelectedEntryColor = Color.green;

		private static Dictionary<string, List<IDrawableElement>>	entries = new Dictionary<string, List<IDrawableElement>>(4096);
		private static List<EntryRef>								lastUsed = new List<EntryRef>(NGSpotlightWindow.MaxLastUsed);
		private static List<AssetFilter>							filters = new List<AssetFilter>(Utility.CreateInstancesFromEnumerable<AssetFilter>(Utility.EachAllAssignableFrom(typeof(AssetFilter), t => t != typeof(DefaultFilter)), null));

		public static Action	UpdatingResult;

		public string	keywords = string.Empty;
		public string	cleanLowerKeywords = string.Empty;

		public int	changeCount = 0;
		public int	selectedEntry = -1;
		private int	selectedFilter = -1;

		internal List<string>	error = new List<string>();

		internal List<AssetFilter>		availableFilters = new List<AssetFilter>();
		internal List<IFilterInstance>	filterInstances = new List<IFilterInstance>();

		private VerticalScrollbar	scrollbar;
		private List<EntryRef>		results = new List<EntryRef>();
		private List<int>			weight = new List<int>();
		private bool				focusTextfieldOnce;
		private bool				consummedKeydown;
		private bool				displayHotkeyOnControlPressed;

		static	NGSpotlightWindow()
		{
			foreach (Type type in Utility.EachNGTSubClassesOf(typeof(object)))
			{
				MethodInfo[]	methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static);

				for (int i = 0; i < methods.Length; i++)
				{
					if (methods[i].IsDefined(typeof(SpotlightUpdatingResultAttribute), false) == true)
					{
						NGSpotlightWindow.UpdatingResult += Delegate.CreateDelegate(typeof(Action), null, methods[i]) as Action;
						break;
					}
				}
			}
		}

		[MenuItem(Constants.MenuItemPath + NGSpotlightWindow.Title, priority = Constants.MenuItemPriority + 290), Hotkey(NGSpotlightWindow.Title)]
		public static void	Open()
		{
			Utility.CloseAllEditorWindows(typeof(NGSpotlightWindow));

			POINT	pos = new POINT();

			NativeMethods.GetCursorPos(out pos);

			Rect	r = Utility.GetEditorMainWindowPos();
			NGSpotlightWindow	window = EditorWindow.CreateInstance<NGSpotlightWindow>();
			window.titleContent.text = NGSpotlightWindow.Title;
			window.position = new Rect(Mathf.Clamp(pos.x - NGSpotlightWindow.WindowWidth * .5F, r.x + 5F, r.xMax - NGSpotlightWindow.WindowWidth - 5F), r.y + r.height * .5F - NGSpotlightWindow.WindowHeight * .5F, NGSpotlightWindow.WindowWidth, NGSpotlightWindow.WindowHeight);
			window.minSize = new Vector2(NGSpotlightWindow.WindowWidth, NGSpotlightWindow.WindowHeight);
			window.maxSize = window.minSize;
			window.ShowPopup();
			window.Focus();
		}

		public static void	AddFilter(AssetFilter filter)
		{
			for (int i = 0; i < NGSpotlightWindow.filters.Count; i++)
			{
				if (NGSpotlightWindow.filters[i].key == filter.key)
				{
					InternalNGDebug.LogError("Filter \"" + filter.GetType().FullName + "\" shares the same key with \"" + NGSpotlightWindow.filters[i].GetType().FullName + "\". One of them must change its key.");
					return;
				}
			}

			NGSpotlightWindow.filters.Add(filter);
		}

		public static void	DeleteKey(string key)
		{
			NGSpotlightWindow.entries.Remove(key);
		}

		public static void	AddEntry(string key, IDrawableElement element)
		{
			List<IDrawableElement>	list;

			if (NGSpotlightWindow.entries.TryGetValue(key, out list) == false)
			{
				list = new List<IDrawableElement>();
				NGSpotlightWindow.entries.Add(key, list);
			}

			list.Add(element);
		}

		public static void	DeleteEntry(string key, int i)
		{
			List<IDrawableElement>	list;

			if (NGSpotlightWindow.entries.TryGetValue(key, out list) == true)
			{
				list.RemoveAt(i);

				if (list.Count == 0)
					NGSpotlightWindow.DeleteKey(key);
			}

			Utility.RepaintEditorWindow(typeof(NGSpotlightWindow));
		}

		public static void	UseEntry(EntryRef m)
		{
			for (int i = 0; i < NGSpotlightWindow.lastUsed.Count; i++)
			{
				if (NGSpotlightWindow.lastUsed[i].key == m.key &&
					NGSpotlightWindow.lastUsed[i].i == m.i)
				{
					NGSpotlightWindow.lastUsed.RemoveAt(i);
					break;
				}
			}

			NGSpotlightWindow.lastUsed.Insert(0, m);

			if (NGSpotlightWindow.lastUsed.Count > NGSpotlightWindow.MaxLastUsed)
				NGSpotlightWindow.lastUsed.RemoveAt(NGSpotlightWindow.lastUsed.Count - 1);
		}

		[NGSettingsChanged]
		private static void	OnSettingsGenerated(ScriptableObject settings)
		{
			CustomHotkeysSettings	hotkeys = settings as CustomHotkeysSettings;

			if (hotkeys != null)
				hotkeys.hotkeys.Add(new CustomHotkeysSettings.MethodHotkey() { bind = "%Q", staticMethod = typeof(NGSpotlightWindow).FullName + ".Open" });
		}

		protected virtual void	OnEnable()
		{
			Utility.RegisterWindow(this);
			Utility.RestoreIcon(this);

			Metrics.UseTool(23); // NGSpolight

			this.scrollbar = new VerticalScrollbar(0F, 0F, this.position.height);
			this.scrollbar.interceiptEvent = true;

			this.wantsMouseMove = true;

			this.RefreshResult();
			this.UpdateAvailableFilters();

			List<EntryHardRef>	refs = (List<EntryHardRef>)Utility.LoadEditorPref(null, typeof(List<EntryHardRef>), NGSpotlightWindow.Title + ".lastUsed");

			if (refs != null)
			{
				NGSpotlightWindow.lastUsed.Clear();

				for (int i = 0; i < refs.Count; i++)
				{
					List<IDrawableElement>	list;

					if (NGSpotlightWindow.entries.TryGetValue(refs[i].key, out list) == true)
					{
						for (int j = 0; j < list.Count; j++)
						{
							if (list[j].RawContent == refs[i].content)
							{
								NGSpotlightWindow.lastUsed.Add(new EntryRef() { key = refs[i].key, i = j });
								break;
							}
						}
					}
				}
			}
		}

		protected virtual void	OnDisable()
		{
			Utility.UnregisterWindow(this);

			List<EntryHardRef>	refs = new List<EntryHardRef>(NGSpotlightWindow.lastUsed.Count);

			for (int i = 0; i < NGSpotlightWindow.lastUsed.Count; i++)
			{
				EntryRef	k = NGSpotlightWindow.lastUsed[i];

				refs.Add(new EntryHardRef() { key = k.key, content = NGSpotlightWindow.entries[k.key][k.i].RawContent });
			}

			Utility.DirectSaveEditorPref(refs, typeof(List<EntryHardRef>), NGSpotlightWindow.Title + ".lastUsed");
		}

		protected virtual void	OnGUI()
		{
			List<EntryRef>	list;

			if (string.IsNullOrEmpty(this.keywords) == true && this.filterInstances.Count == 0)
				list = NGSpotlightWindow.lastUsed;
			else
				list = this.results;

			Rect	r = new Rect(0F, 0F, this.position.width, 18F);

			if (this.focusTextfieldOnce == false && Event.current.type == EventType.Repaint)
			{
				this.focusTextfieldOnce = true;
				GUI.FocusControl("content");
				EditorGUIUtility.editingTextField = true;
			}

			if (Event.current.type == EventType.MouseMove)
				this.Repaint();
			else if (Event.current.type == EventType.ValidateCommand)
			{
				if (Event.current.commandName == "SelectAll" ||
					Event.current.commandName == "Copy" ||
					Event.current.commandName == "Paste" ||
					Event.current.commandName == "Cut")
				{
					this.displayHotkeyOnControlPressed = false;
				}
			}
			else if (Event.current.type == EventType.KeyDown)
			{
				if (Event.current.keyCode == KeyCode.LeftControl || Event.current.keyCode == KeyCode.RightControl)
				{
					if (this.consummedKeydown == false)
					{
						this.consummedKeydown = true;
						this.displayHotkeyOnControlPressed = !this.displayHotkeyOnControlPressed;
						this.Repaint();
					}
				}
				else if (this.displayHotkeyOnControlPressed == true &&
						 ((Event.current.keyCode >= KeyCode.Alpha1 && Event.current.keyCode <= KeyCode.Alpha9) ||
						  (Event.current.keyCode >= KeyCode.Keypad1 && Event.current.keyCode <= KeyCode.Keypad9)))
				{
					int	n;

					if (Event.current.keyCode >= KeyCode.Alpha1 && Event.current.keyCode <= KeyCode.Alpha9)
						n = (int)(Event.current.keyCode - KeyCode.Alpha1);
					else
						n = (int)(Event.current.keyCode - KeyCode.Keypad1);

					EntryRef				er = list[n];
					List<IDrawableElement>	registry = NGSpotlightWindow.entries[er.key];

					if (er.i < registry.Count)
					{
						registry[er.i].Execute(this, er);
						this.Close();
						Event.current.Use();
					}
				}
				else if (Event.current.character == ':' || Event.current.keyCode == KeyCode.Space)
				{
					if (this.selectedFilter >= 0 || this.selectedEntry >= 0)
					{
						this.selectedFilter = -1;
						this.selectedEntry = -1;
						this.focusTextfieldOnce = false;
					}
				}
				else if (Event.current.keyCode == KeyCode.Escape)
				{
					this.Close();
					Event.current.Use();
				}
				else if (Event.current.keyCode == KeyCode.Delete)
				{
					if (this.selectedFilter >= 0)
					{
						this.RemoveFilterInstance(this.filterInstances[this.selectedFilter]);

						while (this.selectedFilter >= this.filterInstances.Count)
							--this.selectedFilter;

						if (this.filterInstances.Count == 0)
							this.focusTextfieldOnce = false;
					}
				}
				else if (Event.current.keyCode == KeyCode.Backspace)
				{
					if (Event.current.control == true)
						this.displayHotkeyOnControlPressed = false;

					if (this.selectedEntry == -1 && this.keywords.Length == 0 && this.filterInstances.Count > 0)
					{
						if (this.selectedFilter == -1)
							this.selectedFilter = this.filterInstances.Count - 1;
						else if (this.selectedFilter >= 0)
						{
							this.RemoveFilterInstance(this.filterInstances[this.selectedFilter]);

							while (this.selectedFilter >= this.filterInstances.Count)
								--this.selectedFilter;

							if (this.filterInstances.Count == 0)
								this.focusTextfieldOnce = false;
						}
						GUI.FocusControl(null);
					}
				}
				else if (Event.current.keyCode == KeyCode.LeftArrow)
				{
					if (Event.current.control == true)
						this.displayHotkeyOnControlPressed = false;

					if (this.selectedEntry == -1 && this.keywords.Length == 0 && this.filterInstances.Count > 0)
					{
						if (this.selectedFilter == -1)
							this.selectedFilter = this.filterInstances.Count - 1;
						else if (this.selectedFilter > 0)
							--this.selectedFilter;
						GUI.FocusControl(null);
					}
				}
				else if (Event.current.keyCode == KeyCode.RightArrow)
				{
					if (Event.current.control == true)
						this.displayHotkeyOnControlPressed = false;

					if (this.selectedEntry == -1 && this.keywords.Length == 0 && this.filterInstances.Count > 0)
					{
						if (this.selectedFilter < this.filterInstances.Count - 1)
							++this.selectedFilter;
						else if (this.selectedFilter == this.filterInstances.Count - 1)
						{
							this.selectedFilter = -1;
							this.focusTextfieldOnce = false;
						}
					}
				}
				else if (Event.current.keyCode == KeyCode.Return ||
						 Event.current.keyCode == KeyCode.KeypadEnter)
				{
					if (list.Count > 0)
					{
						int	n = this.selectedEntry;

						if (n == -1)
						{
							if (EditorGUIUtility.editingTextField == false)
								return;

							n = 0;
						}

						EntryRef				er = list[n];
						List<IDrawableElement>	registry = NGSpotlightWindow.entries[er.key];

						if (er.i < registry.Count)
						{
							registry[er.i].Execute(this, er);
							this.Close();
							Event.current.Use();
						}
					}
				}
				else if (Event.current.keyCode == KeyCode.DownArrow)
				{
					if (this.selectedEntry + 1 < list.Count)
					{
						if (GUI.GetNameOfFocusedControl() == "content")
							GUI.FocusControl(null);
						this.selectedFilter = -1;

						this.SelectEntry(this.selectedEntry + 1);
						this.ClampScrollbarOffsetToSelectedEntry();

						Event.current.Use();
					}
				}
				else if (Event.current.keyCode == KeyCode.UpArrow)
				{
					if (this.selectedEntry >= 0)
					{
						this.selectedFilter = -1;
						this.SelectEntry(this.selectedEntry - 1);
						this.ClampScrollbarOffsetToSelectedEntry();

						Event.current.Use();
					}
				}
				else if (Event.current.keyCode == KeyCode.PageUp)
				{
					if (this.selectedEntry >= 0)
					{
						this.selectedFilter = -1;

						if (this.selectedEntry > 1)
							this.SelectEntry(Mathf.Max(0, this.selectedEntry - Mathf.FloorToInt((this.position.height - 18F) / NGSpotlightWindow.RowHeight)));
						else
							this.SelectEntry(-1);

						this.ClampScrollbarOffsetToSelectedEntry();

						Event.current.Use();
					}
				}
				else if (Event.current.keyCode == KeyCode.PageDown)
				{
					if (this.selectedEntry + 1 < list.Count)
					{
						this.selectedFilter = -1;

						if (GUI.GetNameOfFocusedControl() == "content")
							GUI.FocusControl(null);

						this.SelectEntry(Mathf.Min(list.Count - 1, this.selectedEntry + Mathf.CeilToInt((this.position.height - 18F) / NGSpotlightWindow.RowHeight)));
						this.ClampScrollbarOffsetToSelectedEntry();

						Event.current.Use();
					}
				}
				else if (Event.current.keyCode == KeyCode.Home && GUI.GetNameOfFocusedControl() != "content")
				{
					this.selectedFilter = -1;
					this.SelectEntry(-1);
					this.scrollbar.Offset = 0F;
					Event.current.Use();
				}
				else if (Event.current.keyCode == KeyCode.End && GUI.GetNameOfFocusedControl() != "content")
				{
					this.selectedFilter = -1;
					this.SelectEntry(list.Count - 1);
					this.scrollbar.Offset = -this.scrollbar.MaxHeight + (this.selectedEntry + 1) * (NGSpotlightWindow.RowHeight + NGSpotlightWindow.Spacing) - NGSpotlightWindow.Spacing;
					Event.current.Use();
				}
			}
			else if (Event.current.type == EventType.KeyUp)
			{
				this.consummedKeydown = false;
				//this.displayHotkeyOnControlPressed = Event.current.control;
				this.Repaint();
			}

			if (this.filterInstances.Count > 0)
			{
				for (int i = 0; i < this.filterInstances.Count; i++)
				{
					using (BgColorContentRestorer.Get(this.selectedFilter == i, Color.green))
					{
						r.width = this.filterInstances[i].GetWidth();
						this.filterInstances[i].OnGUI(r, this);
						r.x += r.width + 2F;
					}
				}

				r.width = this.position.width - r.x;
			}

			r.xMax -= NGSpotlightWindow.DropdownWidth;

			EditorGUI.BeginChangeCheck();
			GUI.SetNextControlName("content");
			this.keywords = EditorGUI.TextField(r, this.keywords);
			if (EditorGUI.EndChangeCheck() == true)
				this.RefreshResult();

			if (string.IsNullOrEmpty(this.keywords) == true)
			{
				GUI.enabled = false;
				r.x += 4F;
				GUI.Label(r, NGSpotlightWindow.KeywordsPlaceholder);
				r.x -= 4F;
				GUI.enabled = true;
			}

			r.xMin += r.width;
			r.width = NGSpotlightWindow.DropdownWidth;
			if (GUI.Button(r, "", GeneralStyles.ToolbarDropDown) == true)
			{
				GenericMenu					menu = new GenericMenu();
				GenericMenu.MenuFunction	rawExport = (this.results.Count > 0) ? new GenericMenu.MenuFunction(this.ExportRawResults) : null;
				GenericMenu.MenuFunction	exportWithContext = (this.results.Count > 0) ? new GenericMenu.MenuFunction(this.ExportResultsWithContext) : null;

				menu.AddItem(new GUIContent("Raw export"), false, rawExport);
				menu.AddItem(new GUIContent("Export with context"), false, exportWithContext);
				menu.AddItem(new GUIContent("Settings"), false, this.OpenSettings);

				menu.DropDown(r);
			}

			if (GUI.GetNameOfFocusedControl() == "content")
			{
				this.selectedFilter = -1;
				this.selectedEntry = -1;
			}

			r.x = 0F;
			r.y += r.height;
			r.width = this.position.width;

			if (this.error.Count > 0)
			{
				for (int i = 0; i < this.error.Count; i++)
				{
					Utility.content.text = this.error[i];
					// Helpbox style does not calculate the height correctly.
					r.height = Mathf.Max(NGSpotlightWindow.RowHeight, EditorStyles.helpBox.CalcHeight(Utility.content, r.width - 50F)); // Reduce the width to ensure we display everything with a bit of luck.
					EditorGUI.HelpBox(r, this.error[i], MessageType.Warning);
					r.y += r.height;
				}
			}

			if (this.keywords.Length > 0 && this.keywords[0] == ':' && this.IsValidFilter() == false)
			{
				r.height = 16F;

				using (LabelWidthRestorer.Get(60F))
				{
					for (int i = 0; i < this.availableFilters.Count; i++)
					{
						EditorGUIUtility.AddCursorRect(r, MouseCursor.Link);

						if (Event.current.type == EventType.MouseUp && r.Contains(Event.current.mousePosition) == true)
						{
							if (this.availableFilters[i].key.Contains("=") == false && this.availableFilters[i].key.Contains("{") == false)
								this.keywords = this.availableFilters[i].key + ' ';
							else
								this.keywords = this.availableFilters[i].key;
							this.RefreshResult();
							GUI.FocusControl(null);
							this.focusTextfieldOnce = false;
							this.Repaint();
							Event.current.Use();
						}

						if (this.availableFilters[i].icon != null)
						{
							using (LabelWidthRestorer.Get(80F - 16F))
							{
								Rect	iconR = r;

								r.xMin += 16F;
								EditorGUI.LabelField(r, this.availableFilters[i].key, this.availableFilters[i].description);

								iconR.width = iconR.height;
								GUI.DrawTexture(iconR, this.availableFilters[i].icon, ScaleMode.ScaleToFit);
								r.xMin -= 16F;
							}
						}
						else
						{
							using (LabelWidthRestorer.Get(80F))
							{
								EditorGUI.LabelField(r, this.availableFilters[i].key, this.availableFilters[i].description);
							}
						}
						r.y += r.height + 2F;
					}
				}

				EditorGUI.HelpBox(new Rect(0F, this.position.height - NGSpotlightWindow.RowHeight, this.position.width, NGSpotlightWindow.RowHeight), "Add a space to validate the filter.", MessageType.Info);
			}
			else
			{
				r.height = NGSpotlightWindow.RowHeight;

				this.scrollbar.ClearInterests();
				if (this.selectedEntry != -1)
					this.scrollbar.AddInterest(this.selectedEntry * (NGSpotlightWindow.RowHeight + NGSpotlightWindow.Spacing) + NGSpotlightWindow.RowHeight * .5F, Color.yellow);

				this.scrollbar.RealHeight = list.Count * (NGSpotlightWindow.RowHeight + NGSpotlightWindow.Spacing) - NGSpotlightWindow.Spacing;
				this.scrollbar.SetPosition(this.position.width - 15F, r.y);
				this.scrollbar.SetSize(this.position.height - r.y);
				this.scrollbar.OnGUI();

				GUI.BeginClip(new Rect(0F, r.y, this.position.width, this.position.height - r.y));
				{
					r.y = -this.scrollbar.Offset;
					r.width -= this.scrollbar.MaxWidth;

					for (int i = 0; i < list.Count; i++)
					{
						if (r.y + r.height <= 0)
						{
							r.y += r.height + NGSpotlightWindow.Spacing;
							continue;
						}

						List<IDrawableElement>	registry;

						if (NGSpotlightWindow.entries.TryGetValue(list[i].key, out registry) == true)
						{
							if (list[i].i < registry.Count)
							{
								NGSpotlightWindow.entries[list[i].key][list[i].i].OnGUI(r, this, list[i], i);

								if (this.displayHotkeyOnControlPressed == true && i < 9)
								{
									Rect	numberR = r;
									numberR.width = numberR.height;

									EditorGUI.DrawRect(numberR, Color.cyan * .6F);

									using (ColorContentRestorer.Get(Color.black))
									{
										GUI.Label(numberR, (i + 1).ToCachedString(), GeneralStyles.MainTitle);
									}
								}
							}
							else
								list.RemoveAt(i--);
						}
						else
							list.RemoveAt(i--);

						r.y += r.height + NGSpotlightWindow.Spacing;

						if (r.y > this.scrollbar.MaxHeight)
							break;
					}
				}
				GUI.EndClip();
			}
		}

		protected virtual void	Update()
		{
			if (EditorApplication.isCompiling == true)
				this.Close();
		}

		protected virtual void	OnLostFocus()
		{
			EditorApplication.delayCall += this.Close;
		}

		public void		SelectEntry(int i)
		{
			this.selectedEntry = i;

			if (this.selectedEntry == -1)
				GUI.FocusControl("content");
			else
			{
				GUI.FocusControl(null);

				List<EntryRef>	list;

				if (string.IsNullOrEmpty(this.keywords) == true && this.filterInstances.Count == 0)
					list = NGSpotlightWindow.lastUsed;
				else
					list = this.results;

				EntryRef	k = list[this.selectedEntry];
				NGSpotlightWindow.entries[k.key][k.i].Select(this, k);
			}
		}

		public void		RemoveFilterInstance(IFilterInstance instance)
		{
			this.filterInstances.Remove(instance);

			for (int i = 0; i < this.filterInstances.Count; i++)
			{
				if (this.filterInstances[i].FamilyMask == instance.FamilyMask && 
					this.filterInstances[i].FilterLevel > instance.FilterLevel && 
					this.filterInstances[i].CheckFilterRequirements(this) == false)
				{
					this.filterInstances.RemoveAt(i);
					--i;
				}
			}

			this.RefreshResult();
		}

		public string	HighlightWeightContent(string lowerInput, string input, string lowerKeywords)
		{
			if (lowerKeywords.Length == 0)
				return input;

			SpotlightSettings	settings = HQ.Settings != null ? HQ.Settings.Get<SpotlightSettings>() : null;
			StringBuilder		buffer = Utility.GetBuffer();

			//int	longest = 100;
			Color	color = settings != null ? settings.highlightLetterColor : Color.cyan;
			int		j = 0;
			int		streak = 0;
			int		k = 0;

			for (; k < lowerInput.Length; k++)
			{
				if (input[k] == ' ')
				{
					buffer.Append(' ');
					continue;
				}
				
				if (lowerInput[k] == lowerKeywords[j])
				{
					if (streak == 0)
						buffer.AppendStartColor(color);

					buffer.Append(input[k]);

					++j;

					while (j < lowerKeywords.Length && lowerKeywords[j] == ' ')
						++j;

					//weight += /*-(streak * longest) + */(k) - (j * longest);
					streak++;

					if (j == lowerKeywords.Length)
						break;
				}
				else
				{
					if (streak > 0)
						buffer.AppendEndColor();

					buffer.Append(input[k]);
					streak = 0;
				}
			}

			if (streak > 0)
				buffer.AppendEndColor();

			++k;

			while (k < input.Length)
			{
				buffer.Append(input[k]);
				++k;
			}

			return Utility.ReturnBuffer(buffer);
		}

		private string	ExtractFilterInKeywords()
		{
			if (this.keywords.Length > 2 && this.keywords[0] == ':')
			{
				StringBuilder	buffer = Utility.GetBuffer();
				bool			inText = false;
				bool			backslashed = false;

				for (int i = 0; i < this.keywords.Length; i++)
				{
					if (this.keywords[i] == ' ' && inText == false)
						return Utility.ReturnBuffer(buffer);

					if (this.keywords[i] == '\\' && backslashed == false)
						backslashed = !backslashed;
					else if (this.keywords[i] == '"' && backslashed == false)
						inText = !inText;
					else
					{
						backslashed = false;
						buffer.Append(this.keywords[i]);
					}
				}
			}

			return null;
		}

		private bool	IsValidFilter()
		{
			bool	inText = false;
			bool	backslashed = false;

			for (int i = 0; i < this.keywords.Length; i++)
			{
				if (this.keywords[i] == ' ' && inText == false)
					return true;

				if (this.keywords[i] == '\\' && backslashed == false)
					backslashed = !backslashed;
				else if (this.keywords[i] == '"' && backslashed == false)
					inText = !inText;
				else
					backslashed = false;
			}

			return false;
		}

		private void	RefreshResult(bool limitResults = true)
		{
			if (NGSpotlightWindow.UpdatingResult != null)
				NGSpotlightWindow.UpdatingResult();

			++this.changeCount;
			this.results.Clear();
			this.weight.Clear();

			this.error.Clear();
			this.cleanLowerKeywords = string.Empty;

			string	filter = this.ExtractFilterInKeywords();

			if (this.keywords.Length > 0 && this.keywords[0] == ':')
			{
				this.UpdateAvailableFilters();

				if (filter != null)
				{
					if (this.IdentifyFilter(filter) == true)
					{
						this.keywords = string.Empty;
						this.RefreshResult();
						GUI.FocusControl(null);
						this.focusTextfieldOnce = false;
						this.Repaint();
					}
					else
						this.error.Add("Filter not recognized with \"" + filter + "\".");
				}

				return;
			}

			SpotlightSettings	settings = HQ.Settings != null ? HQ.Settings.Get<SpotlightSettings>() : null;
			int					maxResult = limitResults == true && settings != null ? settings.maxResult : int.MaxValue;
			bool				keepResultsWithPartialMatch = settings != null ? settings.keepResultsWithPartialMatch : true;

			if (this.keywords.Length == 0 && this.filterInstances.Count > 0)
			{
				foreach (var registry in NGSpotlightWindow.entries)
				{
					for (int i = 0; i < registry.Value.Count; i++)
					{
						if (registry.Value[i] == null)
							continue;

						int	j = 0;
						int	family = 0;

						// At least one level 0 filter must filter it in.
						for (; j < this.filterInstances.Count; j++)
						{
							if (this.filterInstances[j].FilterLevel == 0 && this.filterInstances[j].CheckFilterIn(this, registry.Value[i]) == true)
								family |= this.filterInstances[j].FamilyMask;
						}

						if (family == 0)
							continue;

						j = 0;

						for (; j < this.filterInstances.Count; j++)
						{
							if ((family & this.filterInstances[j].FamilyMask) != 0)
							{
								if (this.filterInstances[j].FilterLevel > 0 && this.filterInstances[j].CheckFilterIn(this, registry.Value[i]) == false)
									break;
							}
						}

						if (j < this.filterInstances.Count)
							continue;

						if (this.results.Count < maxResult)
							this.results.Add(new EntryRef() { key = registry.Key, i = i });
						else
							return;
					}
				}
			}
			else if (this.keywords.Length > 0)
			{
				string	lowerKeywords = this.keywords.ToLower();
				this.cleanLowerKeywords = lowerKeywords;

				if (filter != null)
					this.cleanLowerKeywords = lowerKeywords.Substring(filter.Length);

				// Custom implementation of fuzzy search.
				foreach (var registry in NGSpotlightWindow.entries)
				{
					for (int i = 0; i < registry.Value.Count; i++)
					{
						if (registry.Value[i] == null)
							continue;

						if (this.filterInstances.Count > 0)
						{
							int	j = 0;
							int	family = 0;

							// At least one level 0 filter must filter it in.
							for (; j < this.filterInstances.Count; j++)
							{
								if (this.filterInstances[j].FilterLevel == 0 && this.filterInstances[j].CheckFilterIn(this, registry.Value[i]) == true)
									family |= this.filterInstances[j].FamilyMask;
							}

							if (family == 0)
								continue;

							j = 0;

							for (; j < this.filterInstances.Count; j++)
							{
								if ((family & this.filterInstances[j].FamilyMask) != 0)
								{
									if (this.filterInstances[j].FilterLevel > 0 && this.filterInstances[j].CheckFilterIn(this, registry.Value[i]) == false)
										break;
								}
							}

							if (j < this.filterInstances.Count)
								continue;
						}

						int	weight;

						if (this.WeightContent(keepResultsWithPartialMatch, registry.Value[i].LowerStringContent, this.cleanLowerKeywords, out weight) == true)
						{
							int	k = 0;

							for (; k < this.weight.Count; k++)
							{
								if (weight < this.weight[k])
								{
									this.weight.Insert(k, weight);
									this.results.Insert(k, new EntryRef() { key = registry.Key, i = i });
									break;
								}
							}

							if (k == this.weight.Count && this.results.Count < maxResult)
							{
								this.results.Add(new EntryRef() { key = registry.Key, i = i });
								this.weight.Add(weight);
							}
							else if (this.results.Count > maxResult)
							{
								this.results.RemoveAt(this.results.Count - 1);
								this.weight.RemoveAt(this.weight.Count - 1);
							}
						}
					}
				}
			}
		}

		private void	UpdateAvailableFilters()
		{
			this.availableFilters.Clear();

			for (int i = 0; i < NGSpotlightWindow.filters.Count; i++)
			{
				if (NGSpotlightWindow.filters[i].CheckFilterRequirements(this) == true)
					this.availableFilters.Add(NGSpotlightWindow.filters[i]);
			}
		}

		private void	ClampScrollbarOffsetToSelectedEntry()
		{
			if (this.selectedEntry * (NGSpotlightWindow.RowHeight + NGSpotlightWindow.Spacing) < this.scrollbar.Offset)
				this.scrollbar.Offset = this.selectedEntry * (NGSpotlightWindow.RowHeight + NGSpotlightWindow.Spacing) - NGSpotlightWindow.Spacing;
			else if ((this.selectedEntry + 1) * (NGSpotlightWindow.RowHeight + NGSpotlightWindow.Spacing) - NGSpotlightWindow.Spacing - this.scrollbar.Offset > this.scrollbar.MaxHeight)
				this.scrollbar.Offset = -this.scrollbar.MaxHeight + (this.selectedEntry + 1) * (NGSpotlightWindow.RowHeight + NGSpotlightWindow.Spacing) - NGSpotlightWindow.Spacing;
		}

		private bool	IdentifyFilter(string keywords)
		{
			string	lowerKeywords = keywords.ToLower();

			for (int i = 0; i < this.availableFilters.Count; i++)
			{
				IFilterInstance	instance = this.availableFilters[i].Identify(this, keywords, lowerKeywords);

				if (instance != null)
				{
					this.filterInstances.Add(instance);
					return true;
				}
			}

			return false;
		}

		private bool	WeightContent(bool keepResultsWithPartialMatch, string input, string lowerKeywords, out int weight)
		{
			int	longest = 100;
			int	j = 0;
			int	streak = 0;

			weight = 0;

			for (int k = 0; k < input.Length; k++)
			{
				if (input[k] == ' ')
					continue;

				if (input[k] == lowerKeywords[j])
				{
					++j;

					while (j < lowerKeywords.Length && lowerKeywords[j] == ' ')
						++j;

					weight += /*-(streak * longest) + */(k) - (j * longest);
					streak++;

					if (j == lowerKeywords.Length)
						break;
				}
				else
					streak = 0;
			}

			if (keepResultsWithPartialMatch == true)
				return j > 0;
			return j == lowerKeywords.Length;
		}

		private void	ExportResultsWithContext()
		{
			StringBuilder	buffer = Utility.GetBuffer();
			EntryRef[]		dump = this.results.ToArray();

			this.RefreshResult(false);

			buffer.AppendLine(DateTime.Now.ToString());

			for (int i = 0; i < this.filterInstances.Count; i++)
				buffer.AppendLine(this.filterInstances[i].ToString());

			buffer.AppendLine(this.keywords);
			buffer.AppendLine();

			this.CopyResultsToBuffer(buffer);

			EditorGUIUtility.systemCopyBuffer = Utility.ReturnBuffer(buffer);

			this.ShowNotification(new GUIContent("Results with context (" + this.results.Count + ") exported to clipboard."));

			this.results.Clear();
			this.results.AddRange(dump);
		}

		private void	ExportRawResults()
		{
			StringBuilder	buffer = Utility.GetBuffer();
			EntryRef[]		dump = this.results.ToArray();

			this.RefreshResult(false);

			this.CopyResultsToBuffer(buffer);

			EditorGUIUtility.systemCopyBuffer = Utility.ReturnBuffer(buffer);

			this.ShowNotification(new GUIContent("Results (" + this.results.Count + ") exported to clipboard."));

			this.results.Clear();
			this.results.AddRange(dump);
		}

		private void	CopyResultsToBuffer(StringBuilder buffer)
		{
			for (int i = 0; i < this.results.Count; i++)
				buffer.AppendLine(NGSpotlightWindow.entries[this.results[i].key][this.results[i].i].RawContent);

			buffer.Length -= Environment.NewLine.Length;
		}

		private void	OpenSettings()
		{
			EditorWindow.GetWindow<NGSettingsWindow>("Settings", true).Focus(NGSpotlightWindow.Title);
		}
	}
}