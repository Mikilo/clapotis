using NGTools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEditor;

namespace NGToolsEditor.NGStaticInspector
{
	using UnityEngine;

	public class NGStaticInspectorWindow : EditorWindow, IHasCustomMenu
	{
		public enum ReturnUpdateFieldValue
		{
			Success,
			InternalError,
			TypeNotFound,
			GameObjectNotFound,
			ComponentNotFound,
			PathNotResolved,
			DisableServerForbidden
		}

		public const string	NormalTitle = "NG Static Inspector";
		public const string	ShortTitle = "NG Static Inspe";
		public static Color	TitleColor = Color.gray;
		public const string	TabsKeyPref = "NGStaticInspector.Tabs";
		public const int	ForceMemberEditableTickDuration = 300;
		public const char	ValuePathSeparator = '#';
		public const float	Spacing = 2F;
		public const float	TypeHeight = 32F;
		public const float	TitleSpacing = 5F;
		public const float	SplitterHeight = 5F;
		public const float	MinContentHeight = 32F;
		public const float	MaxContentHeightLeft = 100F;
		public const float	CriticalMinimumContentHeight = 5F;
		public static Color	SelectedTabOutline { get { return Utility.GetSkinColor(0F, 1F, 1F, 1F, 1F, 1F, 0F, 1F); } }
		public static Color	SelectedTypeOutline { get { return Utility.GetSkinColor(0F, 1F, 1F, 1F, 1F, 1F, 0F, 1F); } }
		public static Color	HoveredTypeOutline { get { return Utility.GetSkinColor(.6F, .6F, 1F, 1F, 1F, 1F, 0F, 1F); } }
		private readonly static MemberDrawer[]	Empty = new MemberDrawer[0];

		internal static MemberDrawer	forceMemberEditable;

		private static Type[]	staticTypes;

		[SerializeField]
		private string		searchKeywords;
		private Type		selectedType = null;
		[NonSerialized]
		private string[]	searchPatterns;
		[NonSerialized]
		private List<int>	tabTypes = new List<int>();
		private HorizontalScrollbar	scrollPositionTabs;

		[SerializeField]
		private bool		showTypes = true;
		private List<int>	filteredTypes = new List<int>(1024);
		private Vector2		scrollPositionTypes = new Vector2();
		private Vector2		scrollPositionMembers = new Vector2();
		private Rect		bodyRect = new Rect();
		private Rect		viewRect = new Rect();
		private double		lastClickTime;
		[NonSerialized]
		private GUIStyle	typeNameStyle;
		[NonSerialized]
		private Texture2D	starIcon;

		#region Splitter variables
		[NonSerialized]
		public Vector2	scrollPositionContent;
		/// <summary>Defines the height of the area displaying the members.</summary>
		public float	contentHeight = 70F;
		public bool		draggingSplitterBar = false;
		public float	originPositionY;
		public float	originContentHeight;
		#endregion

		private Dictionary<Type, MemberDrawer[]>	typesMembers = new Dictionary<Type, MemberDrawer[]>();

		private ErrorPopup	errorPopup = new ErrorPopup(NGStaticInspectorWindow.NormalTitle, "An error occurred, try to reopen " + NGStaticInspectorWindow.NormalTitle + ".");

		[MenuItem(Constants.MenuItemPath + NGStaticInspectorWindow.NormalTitle, priority = Constants.MenuItemPriority + 0)]
		public static void	Open()
		{
			Utility.OpenWindow<NGStaticInspectorWindow>(NGStaticInspectorWindow.ShortTitle);
		}

		protected virtual void	OnEnable()
		{
			Utility.RegisterWindow(this);
			Utility.RestoreIcon(this, NGStaticInspectorWindow.TitleColor);

			Metrics.UseTool(24); // NGStaticInspector

			this.scrollPositionTabs = new HorizontalScrollbar(0F, 18F, 0F, 3F, 0F);
			this.scrollPositionTabs.interceiptEvent = false;
			this.scrollPositionTabs.hasCustomArea = true;

			this.starIcon = EditorGUIUtility.FindTexture("Favorite");
			this.wantsMouseMove = true;

			this.LoadinspectableStaticTypes();

			string[]	rawTypes = (string[])Utility.LoadEditorPref(null, typeof(string[]), NGStaticInspectorWindow.TabsKeyPref);

			if (rawTypes != null)
			{
				this.tabTypes.Clear();
				this.tabTypes.Capacity = rawTypes.Length;

				for (int i = 0; i < rawTypes.Length; i++)
				{
					Type	type = Type.GetType(rawTypes[i]);

					if (type != null)
					{
						for (int j = 0; j < NGStaticInspectorWindow.staticTypes.Length; j++)
						{
							if (NGStaticInspectorWindow.staticTypes[j] == type)
							{
								this.tabTypes.Add(j);
								break;
							}
						}
					}
				}
			}

			if (string.IsNullOrEmpty(this.searchKeywords) == false)
			{
				this.searchPatterns = Utility.SplitKeywords(this.searchKeywords, ' ');
				this.RefreshFilter();
			}
		}

		protected virtual void	OnDisable()
		{
			Utility.UnregisterWindow(this);

			string[]	rawTypes = new string[this.tabTypes.Count];

			for (int i = 0; i < rawTypes.Length; i++)
				rawTypes[i] = NGStaticInspectorWindow.staticTypes[this.tabTypes[i]].GetShortAssemblyType();

			Utility.DirectSaveEditorPref(rawTypes, typeof(string[]), NGStaticInspectorWindow.TabsKeyPref);
		}

		protected virtual void	OnGUI()
		{
			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
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
			}
			EditorGUILayout.EndHorizontal();

			if (this.errorPopup.exception != null)
				this.errorPopup.OnGUILayout();

			if (this.typeNameStyle == null)
			{
				this.typeNameStyle = new GUIStyle(EditorStyles.label);
				this.typeNameStyle.alignment = TextAnchor.MiddleLeft;
				this.typeNameStyle.fontSize = 15;
			}

			this.bodyRect = GUILayoutUtility.GetLastRect();
			this.bodyRect.y += this.bodyRect.height;

			if (this.tabTypes.Count > 0 && NGStaticInspectorWindow.staticTypes != null)
			{
				float	totalWidth = -NGStaticInspectorWindow.Spacing;

				for (int i = 0; i < this.tabTypes.Count; i++)
				{
					Utility.content.text = NGStaticInspectorWindow.staticTypes[this.tabTypes[i]].Name;
					totalWidth += GeneralStyles.ToolbarButton.CalcSize(Utility.content).x + NGStaticInspectorWindow.Spacing;
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
					Type	type = NGStaticInspectorWindow.staticTypes[this.tabTypes[i]];
					string	name = type.Name;
					string	@namespace = type.Namespace;

					Utility.content.text = name;
					if (@namespace != null)
						Utility.content.tooltip = @namespace + '.' + name;
					else
						Utility.content.tooltip = null;
					r3.width = GeneralStyles.ToolbarButton.CalcSize(Utility.content).x;

					if (GUI.Button(r3, name, GeneralStyles.ToolbarButton) == true)
					{
						if (Event.current.button == 2)
							this.tabTypes.RemoveAt(i);

						this.selectedType = type;

						Utility.content.tooltip = null;

						return;
					}

					if (this.selectedType == NGStaticInspectorWindow.staticTypes[this.tabTypes[i]])
						Utility.DrawUnfillRect(r3, NGStaticInspectorWindow.SelectedTabOutline);

					if (Utility.content.tooltip != null)
						TooltipHelper.Label(r3, Utility.content.tooltip);

					r3.x += r3.width + NGStaticInspectorWindow.Spacing;
				}

				Utility.content.tooltip = null;

				this.bodyRect.y += 20F + 2F;
			}

			float	maxY = this.bodyRect.yMax;
			Rect	r = new Rect();

			if (this.showTypes == true && NGStaticInspectorWindow.staticTypes != null)
			{
				if (Event.current.type == EventType.MouseMove)
					this.Repaint();

				this.bodyRect.height = this.position.height - this.bodyRect.y;
				this.viewRect = new Rect(0F, 0F, 0F, this.CountTypes() * (NGStaticInspectorWindow.Spacing + NGStaticInspectorWindow.TypeHeight) - NGStaticInspectorWindow.Spacing);

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
					r.height = NGStaticInspectorWindow.TypeHeight;

					int	i = 0;

					if (this.viewRect.height > this.bodyRect.height)
					{
						i = (int)(this.scrollPositionTypes.y / (NGStaticInspectorWindow.Spacing + r.height));
						r.y = i * (NGStaticInspectorWindow.Spacing + r.height);
					}

					r.xMin += 5F;

					foreach (Type type in this.EachType(i--))
					{
						r.height = NGStaticInspectorWindow.TypeHeight;
						++i;

						if (r.y + r.height + NGStaticInspectorWindow.Spacing <= this.scrollPositionTypes.y)
						{
							r.y += r.height + NGStaticInspectorWindow.Spacing;
							continue;
						}

						GUI.Box(r, "");

						if (Event.current.type == EventType.Repaint)
						{
							if (this.selectedType == type)
								Utility.DrawUnfillRect(r, NGStaticInspectorWindow.SelectedTypeOutline);

							if (r.Contains(Event.current.mousePosition) == true)
							{
								r.x -= 2F;
								r.y -= 2F;
								r.width += 4F;
								r.height += 4F;
								Utility.DrawUnfillRect(r, NGStaticInspectorWindow.HoveredTypeOutline);
								r.x += 2F;
								r.y += 2F;
								r.width -= 4F;
								r.height -= 4F;
							}
						}
						else if (Event.current.type == EventType.MouseDown &&
								 r.Contains(Event.current.mousePosition) == true)
						{
							this.Repaint();

							if (Event.current.button != 2)
								this.selectedType = type;

							if (string.IsNullOrEmpty(this.searchKeywords) == false)
								i = this.filteredTypes[i];

							if (Event.current.button != 0 && this.tabTypes.Contains(i) == false)
								this.tabTypes.Insert(0, i);
							break;
						}

						r.height = 22F;
						GUI.Label(r, type.Name, this.typeNameStyle);
						r.y += 18F;

						r.height = 15F;
						GUI.Label(r, type.Namespace, GeneralStyles.SmallLabel);
						r.y += 14F + NGStaticInspectorWindow.Spacing;

						if (r.y - this.scrollPositionTypes.y > this.bodyRect.height)
							break;
					}

					r.xMin -= 5F;
				}
				GUI.EndScrollView();

				this.bodyRect.y = maxY;
			}

			bool	doubleClickResize = false;

			if (this.showTypes == true)
			{
				float	minHeight = GUI.skin.label.CalcHeight(Utility.content, this.bodyRect.width);

				// Handle splitter bar.
				this.bodyRect.height = NGStaticInspectorWindow.SplitterHeight;
				GUI.Box(this.bodyRect, "");
				EditorGUIUtility.AddCursorRect(this.bodyRect, MouseCursor.ResizeVertical);

				if (this.draggingSplitterBar == true &&
					Event.current.type == EventType.MouseDrag)
				{
					this.contentHeight = Mathf.Clamp(this.originContentHeight + this.originPositionY - Event.current.mousePosition.y,
													 NGStaticInspectorWindow.MinContentHeight, this.position.height - NGStaticInspectorWindow.MaxContentHeightLeft);
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
														 NGStaticInspectorWindow.MinContentHeight, this.position.height - NGStaticInspectorWindow.MaxContentHeightLeft);
						doubleClickResize = true;
					}

					this.lastClickTime = EditorApplication.timeSinceStartup;
					this.draggingSplitterBar = false;
					Event.current.Use();
				}

				this.bodyRect.height = this.position.height - this.bodyRect.y;

				if (this.bodyRect.height > this.position.height - NGStaticInspectorWindow.MaxContentHeightLeft)
					this.contentHeight = this.position.height - NGStaticInspectorWindow.MaxContentHeightLeft;

				// Smoothly stay at the minimum if not critical under the critical threshold.
				if (this.contentHeight < NGStaticInspectorWindow.MinContentHeight)
					this.contentHeight = NGStaticInspectorWindow.MinContentHeight;

				this.bodyRect.y += NGStaticInspectorWindow.TitleSpacing;
			}

			if (this.selectedType != null)
			{
				Utility.content.text = this.selectedType.Name;
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
					if (NGStaticInspectorWindow.staticTypes[this.tabTypes[i]] == this.selectedType)
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
							if (NGStaticInspectorWindow.staticTypes[this.tabTypes[i]] == this.selectedType)
							{
								this.tabTypes.RemoveAt(i);
								break;
							}
						}
					}
					else
					{
						for (int i = 0; i < NGStaticInspectorWindow.staticTypes.Length; i++)
						{
							if (NGStaticInspectorWindow.staticTypes[i] == this.selectedType)
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
				if (string.IsNullOrEmpty(this.selectedType.Namespace) == false)
					GUI.Label(this.bodyRect, this.selectedType.Name + " (" + this.selectedType.Namespace + ")", GeneralStyles.Title1);
				else
					GUI.Label(this.bodyRect, this.selectedType.Name, GeneralStyles.Title1);
				this.bodyRect.xMin -= 20F;

				this.bodyRect.y += this.bodyRect.height + NGStaticInspectorWindow.TitleSpacing;

				MemberDrawer[]	members = this.GetMembers(this.selectedType);

				viewRect.height = 0F;

				try
				{
					for (int i = 0; i < members.Length; i++)
					{
						try
						{
							if (members[i].exception != null)
								viewRect.height += NGStaticInspectorWindow.Spacing + 16F;
							else
								viewRect.height += NGStaticInspectorWindow.Spacing + members[i].typeDrawer.GetHeight(members[i].fieldModifier.GetValue(null));
						}
						catch (Exception ex)
						{
							members[i].exception = ex;
						}
					}

					// Remove last spacing.
					if (viewRect.height > NGStaticInspectorWindow.Spacing)
						viewRect.height -= NGStaticInspectorWindow.Spacing;

					if (doubleClickResize == true)
					{
						this.contentHeight = Mathf.Clamp(viewRect.height + this.bodyRect.height + NGStaticInspectorWindow.TitleSpacing + NGStaticInspectorWindow.TitleSpacing,
														 NGStaticInspectorWindow.MinContentHeight, this.position.height - NGStaticInspectorWindow.MaxContentHeightLeft);
					}

					this.bodyRect.height = this.position.height - this.bodyRect.y;

					this.scrollPositionMembers = GUI.BeginScrollView(this.bodyRect, this.scrollPositionMembers, viewRect);
					{
						r.width = this.position.width - (viewRect.height > this.bodyRect.height ? 16F : 0F) - 8F;
						r.y = 0F;

						++EditorGUI.indentLevel;
						for (int i = 0; i < members.Length; i++)
						{
							if (members[i].exception != null)
							{
								r.height = 16F;
								using (ColorContentRestorer.Get(Color.red))
									EditorGUI.LabelField(r, members[i].fieldModifier.Name, "Property raised an exception");
								r.y += r.height + NGStaticInspectorWindow.Spacing;
								continue;
							}

							object	instance = members[i].fieldModifier.GetValue(null);
							float	height = members[i].typeDrawer.GetHeight(instance);

							if (r.y + height + NGStaticInspectorWindow.Spacing <= this.scrollPositionMembers.y)
							{
								r.y += height + NGStaticInspectorWindow.Spacing;
								continue;
							}

							r.height = height;

							if (Event.current.type == EventType.MouseDown &&
								r.Contains(Event.current.mousePosition) == true &&
								Event.current.button == 1)
							{
								NGStaticInspectorWindow.forceMemberEditable = members[i];
								this.Repaint();

								Utility.RegisterIntervalCallback(() =>
								{
									NGStaticInspectorWindow.forceMemberEditable = null;
								}, NGStaticInspectorWindow.ForceMemberEditableTickDuration, 1);
							}

							EditorGUI.BeginDisabledGroup(!members[i].isEditable && NGStaticInspectorWindow.forceMemberEditable != members[i]);
							EditorGUI.BeginChangeCheck();
							object	value = members[i].typeDrawer.OnGUI(r, instance);
							if (EditorGUI.EndChangeCheck() == true)
							{
								if (members[i].isEditable == true)
									members[i].fieldModifier.SetValue(null, value);
							}
							EditorGUI.EndDisabledGroup();

							r.y += height + NGStaticInspectorWindow.Spacing;
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

			TooltipHelper.PostOnGUI();
		}

		private void				LoadinspectableStaticTypes()
		{
			List<Type>	staticTypes = new List<Type>(2048);

			foreach (Type type in Utility.EachAllSubClassesOf(typeof(object)))
			{
				if (type.IsEnum == true || type.Name.StartsWith("<Private") == true || typeof(Delegate).IsAssignableFrom(type) == true)
					continue;

				FieldInfo[]	fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

				if (fields.Length > 0)
				{
					if (staticTypes.Contains(type) == false)
						staticTypes.Add(type);
				}
				else
				{
					PropertyInfo[]	properties = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

					if (properties.Length > 0)
					{
						if (staticTypes.Contains(type) == false)
							staticTypes.Add(type);
					}
				}
			}

			staticTypes.Sort((a, b) => a.Name.CompareTo(b.Name));

			NGStaticInspectorWindow.staticTypes = staticTypes.ToArray();
		}

		private MemberDrawer[]		GetMembers(Type type)
		{
			MemberDrawer[]	drawers;

			if (type.IsGenericTypeDefinition == true)
				return NGStaticInspectorWindow.Empty;

			if (this.typesMembers.TryGetValue(type, out drawers) == false)
			{
				List<MemberDrawer>	list = new List<MemberDrawer>();
				FieldInfo[]			fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				PropertyInfo[]		properties = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

				drawers = new MemberDrawer[fields.Length + properties.Length];

				for (int i = 0; i < fields.Length; i++)
					list.Add(new MemberDrawer(TypeDrawerManager.GetDrawer(type.GetShortAssemblyType() + '.' + fields[i].Name, fields[i].Name, fields[i].FieldType), new FieldModifier(fields[i])));

				for (int i = 0; i < properties.Length; i++)
				{
					if (properties[i].GetGetMethod() != null)
					{
						string	niceName = Utility.NicifyVariableName(properties[i].Name);
						int		j = 0;

						for (; j < list.Count; j++)
						{
							if (list[j].typeDrawer.label == niceName)
								break;
						}

						if (j == list.Count)
							list.Add(new MemberDrawer(TypeDrawerManager.GetDrawer(type.GetShortAssemblyType() + '.' + properties[i].Name, properties[i].Name, properties[i].PropertyType), new PropertyModifier(properties[i])));
					}
				}

				drawers = list.ToArray();

				this.typesMembers.Add(type, drawers);
			}

			return drawers;
		}

		private IEnumerable<Type>	EachType(int offset)
		{
			if (string.IsNullOrEmpty(this.searchKeywords) == true)
			{
				for (int i = offset; i < NGStaticInspectorWindow.staticTypes.Length; i++)
					yield return NGStaticInspectorWindow.staticTypes[i];
			}
			else
			{
				for (int i = offset; i < this.filteredTypes.Count; i++)
					yield return NGStaticInspectorWindow.staticTypes[this.filteredTypes[i]];
			}
		}

		private int					CountTypes()
		{
			if (string.IsNullOrEmpty(this.searchKeywords) == true)
				return NGStaticInspectorWindow.staticTypes.Length;
			else
				return this.filteredTypes.Count;
		}

		private void				RefreshFilter()
		{
			this.filteredTypes.Clear();

			for (int j = 0; j < NGStaticInspectorWindow.staticTypes.Length; j++)
			{
				int	i = 0;

				for (; i < this.searchPatterns.Length; i++)
				{
					if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(NGStaticInspectorWindow.staticTypes[j].Name, this.searchPatterns[i], CompareOptions.IgnoreCase) < 0)
						break;
				}

				if (i == this.searchPatterns.Length)
					this.filteredTypes.Add(j);
			}

			this.Repaint();
		}

		void	IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			Utility.AddNGMenuItems(menu, this, NGStaticInspectorWindow.NormalTitle, NGAssemblyInfo.WikiURL);
		}
	}
}