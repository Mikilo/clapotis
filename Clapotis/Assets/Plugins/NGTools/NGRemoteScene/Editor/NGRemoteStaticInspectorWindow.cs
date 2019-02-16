using NGTools;
using NGTools.Network;
using NGTools.NGRemoteScene;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	public class NGRemoteStaticInspectorWindow : NGRemoteWindow, IHasCustomMenu, IDataDrawerTool
	{
		private sealed class OptionPopup : PopupWindowContent
		{
			private readonly NGRemoteStaticInspectorWindow	window;

			public	OptionPopup(NGRemoteStaticInspectorWindow window)
			{
				this.window = window;
			}

			public override Vector2	GetWindowSize()
			{
				return new Vector2(Mathf.Max(this.window.position.width * .5F, 175F), 19F);
			}

			public override void	OnGUI(Rect r)
			{
				Utility.content.text = LC.G("NGProject_AutoLoad");

				EditorGUI.BeginChangeCheck();
				this.window.autoLoad = EditorGUILayout.Toggle(Utility.content, this.window.autoLoad);
				if (EditorGUI.EndChangeCheck() == true)
					this.window.Repaint();
			}
		}

		public const string	NormalTitle = "NG Remote Static Inspector";
		public const string	ShortTitle = "NG R Static Ins";
		public const string	TabsKeyPref = "NGRemoteStaticInspector.Tabs";
		public const int	ForceRepaintRefreshTick = 10;
		public const float	TitleSpacing = 5F;
		public const float	ContentSplitterHeight = 5F;
		public const float	MinContentHeight = 32F;
		public const float	MaxContentHeightLeft = 100F;
		public const float	CriticalMinimumContentHeight = 5F;
		public const float	TypeHeight = 32F;
		public const float	Spacing = 2F;
		public const int	ForceMemberEditableTickDuration = 300;
		public static Color	SelectedTabOutline { get { return Utility.GetSkinColor(0F, 1F, 1F, 1F, 1F, 1F, 0F, 1F); } }
		public static Color	SelectedTypeOutline { get { return Utility.GetSkinColor(0F, 1F, 1F, 1F, 1F, 1F, 0F, 1F); } }
		public static Color	HoveredTypeOutline { get { return Utility.GetSkinColor(0F, 1F, 1F, 1F, 1F, 1F, 0F, 1F); } }

		#region Row Content Variables
		[NonSerialized]
		public Vector2	scrollPositionRowContent;
		/// <summary>Defines the height of the area displaying log's content.</summary>
		public float	contentHeight = 70F;
		public bool		draggingSplitterBar = false;
		public float	originPositionY;
		public float	originContentHeight;
		#endregion

		public Vector2	ScrollPosition { get { return this.scrollPositionMembers; } }
		public Rect		BodyRect { get { return this.bodyRect; } }

		public bool	autoLoad;

		private string		searchKeywords = string.Empty;
		private ClientType	selectedType = null;
		private string[]	searchPatterns;
		private List<int>		tabTypes = new List<int>();
		private HorizontalScrollbar	scrollPositionTabs;

		private bool		showTypes = true;
		private List<int>	filteredTypes = new List<int>(1024);
		private Vector2		scrollPositionTypes = new Vector2();
		private Vector2		scrollPositionMembers = new Vector2();
		private Rect		bodyRect = new Rect();
		private Rect		viewRect = new Rect();
		[NonSerialized]
		private GUIStyle	style;
		[NonSerialized]
		private Texture2D	starIcon;

		private ClientStaticMember	forceMemberEditable;
		private double				lastClickTime;

		private ErrorPopup	errorPopup = new ErrorPopup(NGRemoteStaticInspectorWindow.NormalTitle, "An error occurred, try to reopen " + NGRemoteStaticInspectorWindow.NormalTitle + ".");

		[MenuItem(Constants.MenuItemPath + NGRemoteStaticInspectorWindow.NormalTitle, priority = Constants.MenuItemPriority + 217)]
		public static void	Open()
		{
			Utility.OpenWindow<NGRemoteStaticInspectorWindow>(NGRemoteStaticInspectorWindow.ShortTitle);
		}

		protected override void	OnEnable()
		{
			base.OnEnable();

			this.scrollPositionTabs = new HorizontalScrollbar(0F, 18F, 0F, 3F, 0F);
			this.scrollPositionTabs.interceiptEvent = false;
			this.scrollPositionTabs.hasCustomArea = true;

			this.starIcon = EditorGUIUtility.FindTexture("Favorite");
			this.wantsMouseMove = true;

			if (string.IsNullOrEmpty(this.searchKeywords) == false)
			{
				this.searchPatterns = Utility.SplitKeywords(this.searchKeywords, ' ');
				this.RefreshFilter();
			}
		}

		protected override  void	OnDisable()
		{
			base.OnDisable();

			if (this.Hierarchy != null && this.Hierarchy.inspectableTypes != null)
			{
				string[]	rawTypes = new string[this.tabTypes.Count];

				for (int i = 0; i < rawTypes.Length; i++)
					rawTypes[i] = this.Hierarchy.inspectableTypes[this.tabTypes[i]].@namespace + '.' + this.Hierarchy.inspectableTypes[this.tabTypes[i]].name;

				Utility.DirectSaveEditorPref(rawTypes, typeof(string[]), NGRemoteStaticInspectorWindow.TabsKeyPref);
			}
		}

		protected override void	OnHierarchyConnected()
		{
			base.OnHierarchyConnected();

			Utility.RegisterIntervalCallback(this.Repaint, NGRemoteStaticInspectorWindow.ForceRepaintRefreshTick);

			this.selectedType = null;
			this.showTypes = true;
			this.filteredTypes.Clear();
			GUI.FocusControl(null);

			if (this.autoLoad == true)
				this.Hierarchy.LoadInspectableTypes(this.OnStaticTypesReceived);
		}

		protected override void	OnHierarchyDisconnected()
		{
			base.OnHierarchyDisconnected();

			Utility.UnregisterIntervalCallback(this.Repaint);
		}

		protected override void	OnGUIHeader()
		{
			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				if (GUILayout.Button("☰", "GV Gizmo DropDown", GUILayoutOptionPool.Width(30F)) == true)
					PopupWindow.Show(new Rect(0F, 16F, 0F, 0F), new OptionPopup(this));

				this.showTypes = GUILayout.Toggle(this.showTypes, "", GeneralStyles.ToolbarDropDown, GUILayoutOptionPool.Width(20F));

				EditorGUI.BeginChangeCheck();
				this.searchKeywords = EditorGUILayout.TextField(this.searchKeywords, GeneralStyles.ToolbarSearchTextField, GUILayoutOptionPool.ExpandWidthTrue);
				if (EditorGUI.EndChangeCheck() == true)
				{
					this.showTypes = true;
					this.searchPatterns = Utility.SplitKeywords(this.searchKeywords, ' ');
					this.RefreshFilter();
				}

				if (GUILayout.Button("", GeneralStyles.ToolbarSearchCancelButton) == true)
				{
					this.searchKeywords = string.Empty;
					this.searchPatterns = Utility.SplitKeywords(this.searchKeywords, ' ');
					GUI.FocusControl(null);
					this.RefreshFilter();
				}

				if (this.autoLoad == false)
				{
					if (this.Hierarchy.inspectableTypes == null)
					{
						bool	isConnected = this.Hierarchy.IsClientConnected();
						EditorGUI.BeginDisabledGroup(!isConnected);
						if (GUILayout.Button(LC.G("NGProject_Load"), GeneralStyles.ToolbarButton, GUILayoutOptionPool.Width(40F)) == true)
							this.Hierarchy.LoadInspectableTypes(this.OnStaticTypesReceived);
						XGUIHighlightManager.DrawHighlightLayout(NGRemoteStaticInspectorWindow.Title + ".Load", this);
						EditorGUI.EndDisabledGroup();
					}
				}
			}
			EditorGUILayout.EndHorizontal();

			if (this.errorPopup.exception != null)
				this.errorPopup.OnGUILayout();
		}

		protected override void	OnGUIConnected()
		{
			if (this.Hierarchy.inspectableTypes == null)
			{
				if (this.Hierarchy.IsChannelBlocked(this.GetHashCode()) == true)
				{
					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.FlexibleSpace();

						GUILayout.Label(GeneralStyles.StatusWheel, GUILayoutOptionPool.Width(20F));
						GUILayout.Label("Loading types...");

						GUILayout.FlexibleSpace();
					}
					EditorGUILayout.EndHorizontal();
				}
				else
				{
					GUILayout.Label("Types not loaded.", GeneralStyles.BigCenterText);
					Rect	r2 = GUILayoutUtility.GetLastRect();
					EditorGUIUtility.AddCursorRect(r2, MouseCursor.Link);

					if (Event.current.type == EventType.MouseDown && r2.Contains(Event.current.mousePosition) == true)
						XGUIHighlightManager.Highlight(NGRemoteStaticInspectorWindow.Title + ".Load");
				}

				return;
			}

			if (this.style == null)
			{
				this.style = new GUIStyle(EditorStyles.label);
				this.style.alignment = TextAnchor.MiddleLeft;
				this.style.fontSize = 15;
			}

			this.bodyRect = GUILayoutUtility.GetLastRect();
			this.bodyRect.y += this.bodyRect.height;

			if (this.tabTypes.Count > 0 && this.Hierarchy.inspectableTypes != null)
			{
				float	totalWidth = -NGRemoteStaticInspectorWindow.Spacing;

				for (int i = 0; i < this.tabTypes.Count; i++)
				{
					Utility.content.text = this.Hierarchy.inspectableTypes[this.tabTypes[i]].name;
					totalWidth += GeneralStyles.ToolbarButton.CalcSize(Utility.content).x + NGRemoteStaticInspectorWindow.Spacing;
				}

				this.scrollPositionTabs.allowedMouseArea = new Rect(0F, this.bodyRect.y, this.position.width, 22F);
				this.scrollPositionTabs.SetPosition(0F, this.bodyRect.y);
				this.scrollPositionTabs.SetSize(this.position.width);
				this.scrollPositionTabs.RealWidth = totalWidth;
				this.scrollPositionTabs.OnGUI();

				this.bodyRect.y += this.scrollPositionTabs.MaxHeight;

				Rect	r3 = bodyRect;
				r3.height = 18F;
				r3.y += 2F;
				r3.x = -this.scrollPositionTabs.Offset;

				for (int i = 0; i < this.tabTypes.Count; i++)
				{
					ClientType	type = this.Hierarchy.inspectableTypes[this.tabTypes[i]];

					Utility.content.text = type.name;
					if (type.@namespace != null)
						Utility.content.tooltip = type.@namespace + '.' + type.name;
					else
						Utility.content.tooltip = null;
					r3.width = GeneralStyles.ToolbarButton.CalcSize(Utility.content).x;

					if (GUI.Button(r3, type.name, GeneralStyles.ToolbarButton) == true)
					{
						if (Event.current.button == 2)
							this.tabTypes.RemoveAt(i);

						this.Hierarchy.WatchTypes(this, type.typeIndex);
						this.selectedType = type;
						this.selectedType.LoadInspectableTypeStaticMembers(this.Hierarchy.Client, this.Hierarchy);

						Utility.content.tooltip = null;

						return;
					}

					if (this.selectedType == this.Hierarchy.inspectableTypes[this.tabTypes[i]])
						Utility.DrawUnfillRect(r3, NGRemoteStaticInspectorWindow.SelectedTabOutline);

					if (Utility.content.tooltip != null)
						TooltipHelper.Label(r3, Utility.content.tooltip);

					r3.x += r3.width + NGRemoteStaticInspectorWindow.Spacing;
				}

				Utility.content.tooltip = null;

				this.bodyRect.y += 20F + 2F;
			}

			float	maxY = this.bodyRect.yMax;
			Rect	r = new Rect();

			if (this.showTypes == true && this.Hierarchy.inspectableTypes != null)
			{
				if (Event.current.type == EventType.MouseMove)
					this.Repaint();

				this.bodyRect.height = this.position.height - this.bodyRect.y;
				this.viewRect = new Rect(0F, 0F, 0F, this.CountTypes() * (NGRemoteStaticInspectorWindow.Spacing + NGRemoteStaticInspectorWindow.TypeHeight) - NGRemoteStaticInspectorWindow.Spacing);

				this.bodyRect.height -= this.contentHeight;

				if (this.viewRect.height < 100F)
				{
					if (this.bodyRect.height > this.viewRect.height)
						this.bodyRect.height = this.viewRect.height;
				}

				maxY = this.bodyRect.yMax;

				this.scrollPositionTypes = GUI.BeginScrollView(this.bodyRect, this.scrollPositionTypes, this.viewRect);
				{
					r.width = this.position.width - (viewRect.height > this.bodyRect.height ? 16F : 0F);
					r.height = NGRemoteStaticInspectorWindow.TypeHeight;

					int	i = 0;

					if (this.viewRect.height > this.bodyRect.height)
					{
						i = (int)(this.scrollPositionTypes.y / (NGRemoteStaticInspectorWindow.Spacing + r.height));
						r.y = i * (NGRemoteStaticInspectorWindow.Spacing + r.height);
					}

					r.xMin += 5F;

					foreach (ClientType type in this.EachType(i--))
					{
						r.height = NGRemoteStaticInspectorWindow.TypeHeight;
						++i;

						if (r.y + r.height + NGRemoteStaticInspectorWindow.Spacing <= this.scrollPositionTypes.y)
						{
							r.y += r.height + NGRemoteStaticInspectorWindow.Spacing;
							continue;
						}

						GUI.Box(r, "");

						if (Event.current.type == EventType.Repaint)
						{
							if (this.selectedType == type)
								Utility.DrawUnfillRect(r, NGRemoteStaticInspectorWindow.SelectedTypeOutline);

							if (r.Contains(Event.current.mousePosition) == true)
							{
								r.x -= 2F;
								r.y -= 2F;
								r.width += 4F;
								r.height += 4F;
								Utility.DrawUnfillRect(r, NGRemoteStaticInspectorWindow.HoveredTypeOutline);
								r.x += 2F;
								r.y += 2F;
								r.width -= 4F;
								r.height -= 4F;
							}
						}
						else if (Event.current.type == EventType.MouseDown &&
								 r.Contains(Event.current.mousePosition) == true)
						{
							this.Hierarchy.WatchTypes(this, type.typeIndex);
							this.selectedType = type;
							this.selectedType.LoadInspectableTypeStaticMembers(this.Hierarchy.Client, this.Hierarchy);

							if (this.tabTypes.Contains(type.typeIndex) == false)
								this.tabTypes.Insert(0, type.typeIndex);
						}

						r.height = 22F;
						GUI.Label(r, type.name, this.style);
						r.y += 18F;

						r.height = 15F;
						GUI.Label(r, type.@namespace, GeneralStyles.SmallLabel);
						r.y += 14F + NGRemoteStaticInspectorWindow.Spacing;

						if (r.y - this.scrollPositionTypes.y > this.bodyRect.height)
							break;
					}
				}
				GUI.EndScrollView();

				this.bodyRect.y = maxY;
			}

			bool	doubleClickResize = false;

			if (this.showTypes == true)
			{
				float	minHeight = GUI.skin.label.CalcHeight(Utility.content, this.bodyRect.width);

				// Handle splitter bar.
				this.bodyRect.height = NGRemoteStaticInspectorWindow.ContentSplitterHeight;
				GUI.Box(this.bodyRect, "");
				EditorGUIUtility.AddCursorRect(this.bodyRect, MouseCursor.ResizeVertical);

				if (this.draggingSplitterBar == true &&
					Event.current.type == EventType.MouseDrag)
				{
					this.contentHeight = Mathf.Clamp(this.originContentHeight + this.originPositionY - Event.current.mousePosition.y,
														NGRemoteStaticInspectorWindow.MinContentHeight, this.position.height - NGRemoteStaticInspectorWindow.MaxContentHeightLeft);
					Event.current.Use();
				}
				else if (Event.current.type == EventType.MouseDown &&
							this.bodyRect.Contains(Event.current.mousePosition) == true)
				{
					this.originPositionY = Event.current.mousePosition.y;
					this.originContentHeight = this.contentHeight;
					this.draggingSplitterBar = true;
					Event.current.Use();
				}
				else if (this.draggingSplitterBar == true &&
							Event.current.type == EventType.MouseUp)
				{
					// Auto adjust height on left click or double click.
					if (this.bodyRect.Contains(Event.current.mousePosition) == true &&
						(Event.current.button == 1 ||
							(this.lastClickTime + Constants.DoubleClickTime > EditorApplication.timeSinceStartup &&
							Mathf.Abs(this.originPositionY - Event.current.mousePosition.y) < 5F)))
					{
						// 7F of margin, dont know why it is required. CalcHeight seems to give bad result.
						this.contentHeight = Mathf.Clamp(minHeight + 7F,
															NGRemoteStaticInspectorWindow.MinContentHeight, this.position.height - NGRemoteStaticInspectorWindow.MaxContentHeightLeft);
						doubleClickResize = true;
					}

					this.lastClickTime = EditorApplication.timeSinceStartup;
					this.draggingSplitterBar = false;
					Event.current.Use();
				}

				this.bodyRect.height = this.position.height - this.bodyRect.y;

				if (this.bodyRect.height > this.position.height - NGRemoteStaticInspectorWindow.MaxContentHeightLeft)
					this.contentHeight = this.position.height - NGRemoteStaticInspectorWindow.MaxContentHeightLeft;

				// Smoothly stay at the minimum if not critical under the critical threshold.
				if (this.contentHeight < NGRemoteStaticInspectorWindow.MinContentHeight)
					this.contentHeight = NGRemoteStaticInspectorWindow.MinContentHeight;

				this.bodyRect.y += NGRemoteStaticInspectorWindow.TitleSpacing;
			}

			if (this.selectedType != null)
			{
				Utility.content.text = this.selectedType.name;
				this.bodyRect.height = GeneralStyles.Title1.CalcHeight(Utility.content, this.position.width);

				bool	isPinned = false;
				Rect	starRect = this.bodyRect;

				starRect.x = 5F;
				starRect.y += 3F;
				starRect.width = 12F;
				starRect.height = 14F;

				EditorGUIUtility.AddCursorRect(starRect, MouseCursor.Link);

				for (int i = 0; i < this.tabTypes.Count; i++)
				{
					if (this.Hierarchy.inspectableTypes[this.tabTypes[i]] == this.selectedType)
					{
						isPinned = true;
						break;
					}
				}

				if (Event.current.type == EventType.MouseDown &&
					starRect.Contains(Event.current.mousePosition) == true)
				{
					if (isPinned == true)
					{
						for (int i = 0; i < this.tabTypes.Count; i++)
						{
							if (this.Hierarchy.inspectableTypes[this.tabTypes[i]] == this.selectedType)
							{
								this.tabTypes.RemoveAt(i);
								break;
							}
						}
					}
					else
					{
						for (int i = 0; i < this.Hierarchy.inspectableTypes.Length; i++)
						{
							if (this.Hierarchy.inspectableTypes[i] == this.selectedType)
							{
								this.tabTypes.Insert(0, i);
								break;
							}
						}
					}

					Event.current.Use();
					return;
				}

				if (isPinned == true)
				{
					Color	c = GUI.color;
					GUI.color = Color.yellow;
					GUI.DrawTexture(starRect, this.starIcon);
					GUI.color = c;
				}
				else
					GUI.DrawTexture(starRect, this.starIcon);

				this.bodyRect.xMin += 20F;
				if (string.IsNullOrEmpty(this.selectedType.@namespace) == false)
					GUI.Label(this.bodyRect, this.selectedType.name + " (" + this.selectedType.@namespace + ")", GeneralStyles.Title1);
				else
					GUI.Label(this.bodyRect, this.selectedType.name, GeneralStyles.Title1);
				this.bodyRect.xMin -= 20F;

				this.bodyRect.y += this.bodyRect.height + NGRemoteStaticInspectorWindow.TitleSpacing;

				ClientStaticMember[]	members = this.selectedType.members;

				if (members != null)
				{
					viewRect.height = 0F;

					try
					{
						for (int i = 0; i < members.Length; i++)
							viewRect.height += ClientComponent.MemberSpacing + members[i].GetHeight(this);

						// Remove last spacing.
						if (viewRect.height > ClientComponent.MemberSpacing)
							viewRect.height -= ClientComponent.MemberSpacing;

						if (doubleClickResize == true)
						{
							this.contentHeight = Mathf.Clamp(viewRect.height + this.bodyRect.height + NGRemoteStaticInspectorWindow.TitleSpacing + NGRemoteStaticInspectorWindow.TitleSpacing,
																NGRemoteStaticInspectorWindow.MinContentHeight, this.position.height - NGRemoteStaticInspectorWindow.MaxContentHeightLeft);
						}

						this.bodyRect.height = this.position.height - this.bodyRect.y;

						this.scrollPositionMembers = GUI.BeginScrollView(this.bodyRect, this.scrollPositionMembers, viewRect);
						{
							r.x = 0F;
							r.y = 0F;
							r.width = this.position.width - (viewRect.height > this.bodyRect.height ? 16F : 0F);

							++EditorGUI.indentLevel;
							for (int i = 0; i < members.Length; i++)
							{
								float	height = members[i].GetHeight(this);

								if (r.y + height + ClientComponent.MemberSpacing <= this.scrollPositionMembers.y)
								{
									r.y += height + ClientComponent.MemberSpacing;
									continue;
								}

								r.height = height;

								if (Event.current.type == EventType.MouseDown &&
									r.Contains(Event.current.mousePosition) == true &&
									Event.current.button == 1)
								{
									this.forceMemberEditable = members[i];
									this.Hierarchy.PacketInterceptor += this.CatchFieldUpdatePacket;

									Utility.RegisterIntervalCallback(() =>
									{
										this.forceMemberEditable = null;
										this.Hierarchy.PacketInterceptor -= this.CatchFieldUpdatePacket;
									}, NGRemoteStaticInspectorWindow.ForceMemberEditableTickDuration, 1);
								}

								EditorGUI.BeginDisabledGroup(!members[i].isEditable && (this.forceMemberEditable != members[i]));
								members[i].Draw(r, this);
								EditorGUI.EndDisabledGroup();

								r.y += height + ClientComponent.MemberSpacing;
								if (r.y - this.scrollPositionMembers.y > this.bodyRect.height)
									break;
							}
							--EditorGUI.indentLevel;
						}
						GUI.EndScrollView();
					}
					catch (Exception ex)
					{
						this.errorPopup.exception = ex;
					}
				}
				else
				{
					this.bodyRect.height = 32F;
					EditorGUI.HelpBox(this.bodyRect, "Data not available yet.", MessageType.Info);

					this.Repaint();
				}
			}

			TooltipHelper.PostOnGUI();
		}

		private bool	CatchFieldUpdatePacket(Packet p)
		{
			ClientUpdateFieldValuePacket	packet = p as ClientUpdateFieldValuePacket;

			return packet == null || packet.fieldPath != this.forceMemberEditable.declaringTypeIndex.ToCachedString() + NGServerScene.ValuePathSeparator + this.forceMemberEditable.name;
		}

		private IEnumerable<ClientType>	EachType(int offset)
		{
			if (string.IsNullOrEmpty(this.searchKeywords) == true)
			{
				for (int i = offset; i < this.Hierarchy.inspectableTypes.Length; i++)
					yield return this.Hierarchy.inspectableTypes[i];
			}
			else
			{
				for (int i = offset; i < this.filteredTypes.Count; i++)
					yield return this.Hierarchy.inspectableTypes[this.filteredTypes[i]];
			}
		}

		private int	CountTypes()
		{
			if (string.IsNullOrEmpty(this.searchKeywords) == true)
				return this.Hierarchy.inspectableTypes.Length;
			else
				return this.filteredTypes.Count;
		}

		private void	OnStaticTypesReceived()
		{
			for (int i = 0; i < this.tabTypes.Count; i++)
			{
				if (this.tabTypes[i] >= this.Hierarchy.inspectableTypes.Length)
					this.tabTypes.RemoveAt(i--);
			}

			string[]	rawTypes = (string[])Utility.LoadEditorPref(null, typeof(string[]), NGRemoteStaticInspectorWindow.TabsKeyPref);

			if (rawTypes != null)
			{
				this.tabTypes.Clear();
				this.tabTypes.Capacity = rawTypes.Length;

				for (int i = 0; i < rawTypes.Length; i++)
				{
					for (int j = 0; j < this.Hierarchy.inspectableTypes.Length; j++)
					{
						if (this.Hierarchy.inspectableTypes[j].@namespace + '.' + this.Hierarchy.inspectableTypes[j].name == rawTypes[i])
						{
							this.tabTypes.Add(j);
							break;
						}
					}
				}
			}

			this.RefreshFilter();
		}

		private void	RefreshFilter()
		{
			this.filteredTypes.Clear();

			if (this.Hierarchy.inspectableTypes == null)
				return;

			for (int j = 0; j < this.Hierarchy.inspectableTypes.Length; j++)
			{
				int	i = 0;

				for (; i < this.searchPatterns.Length; i++)
				{
					if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(this.Hierarchy.inspectableTypes[j].name, this.searchPatterns[i], CompareOptions.IgnoreCase) < 0)
						break;
				}

				if (i == this.searchPatterns.Length)
					this.filteredTypes.Add(j);
			}

			this.Repaint();
		}

		void	IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			if (this.Hierarchy != null)
				this.Hierarchy.AddTabMenus(menu, this);
			Utility.AddNGMenuItems(menu, this, NGRemoteInspectorWindow.NormalTitle, Constants.WikiBaseURL + "#markdown-header-132-ng-remote-inspector");

			if (Conf.DebugMode != Conf.DebugState.None)
			{
				menu.AddItem(new GUIContent("Output static types"), false, () =>
				{
					if (this.Hierarchy.inspectableTypes != null)
					{
						for (int i = 0; i < this.Hierarchy.inspectableTypes.Length; i++)
						{
							if (this.Hierarchy.inspectableTypes[i].@namespace != null)
								Debug.Log(this.Hierarchy.inspectableTypes[i].typeIndex + " " + this.Hierarchy.inspectableTypes[i].@namespace + '.' + this.Hierarchy.inspectableTypes[i].name);
							else
								Debug.Log(this.Hierarchy.inspectableTypes[i].typeIndex + " " + this.Hierarchy.inspectableTypes[i].name);
						}
					}
					else
						InternalNGDebug.Log("Static types not available, load them first.");
				});
			}
		}
	}
}