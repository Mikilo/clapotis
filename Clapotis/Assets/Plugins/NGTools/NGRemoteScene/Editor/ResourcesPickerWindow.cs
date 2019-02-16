using NGTools;
using NGTools.Network;
using NGTools.NGRemoteScene;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	public class ResourcesPickerWindow : EditorWindow
	{
		public const float	HeaderHeight = 44F;
		public const float	HeaderSpace = 2F;

		public static Color	FocusBackgroundColor = new Color(50F / 255F, 76F / 255F, 120F / 255F);
		public static Color	InitialBackgroundColor = new Color(30F / 255F, 46F / 255F, 20F / 255F);

		internal NGRemoteHierarchyWindow	hierarchy;
		internal Type						type;
		internal string						valuePath;
		internal Func<string, byte[], Packet>	packetGenerator;
		internal Action<ResponsePacket>			onPacketComplete;
		internal string						searchString;
		internal int						selectedInstanceID;
		internal Vector2					scrollPosition;
		internal List<string>				filteredResources = new List<string>(64);
		internal List<int>					filteredResourceIDs = new List<int>(64);
		internal int						initialInstanceID;

		internal string[]		resources;
		internal int[]			ids;
		internal TypeHandler	typeHandler;

		private List<TabModule>	tabs = new List<TabModule>();
		private int				currentTab = 0;

		private Rect	viewRect = new Rect();
		private Rect	r = new Rect();
		private int		resourcesCount = 0;

		public static void	Init(NGRemoteHierarchyWindow hierarchy, Type type, string valuePath, Func<string, byte[], Packet> packetGenerator, Action<ResponsePacket> onPacketComplete, int initialInstanceID)
		{
			ResourcesPickerWindow	picker = EditorWindow.GetWindow<ResourcesPickerWindow>(true, "Select " + type.Name);

			picker.hierarchy = hierarchy;
			picker.hierarchy.ResourcesUpdated += picker.RefreshResources;
			picker.type = type;
			picker.valuePath = valuePath;
			picker.packetGenerator = packetGenerator;
			picker.onPacketComplete = onPacketComplete;
			picker.searchString = string.Empty;
			picker.selectedInstanceID = 0;
			picker.filteredResources.Clear();
			picker.filteredResourceIDs.Clear();
			picker.initialInstanceID = initialInstanceID;
			picker.selectedInstanceID = initialInstanceID;
			picker.typeHandler = TypeHandlersManager.GetTypeHandler(type);
			picker.SetTab(0);
			picker.tabs.Clear();

			foreach (Type t in Utility.EachNGTSubClassesOf(typeof(TabModule), (Type t) =>
			{
				TabModuleForTypeAttribute[]	attributes = t.GetCustomAttributes(typeof(TabModuleForTypeAttribute), false) as TabModuleForTypeAttribute[];

				return attributes.Length > 0 && type.IsAssignableFrom(attributes[0].type);
			}))
			{
				picker.tabs.Add(Activator.CreateInstance(t, new object[] { picker }) as TabModule);
			}
		}

		protected virtual void	OnDestroy()
		{
			this.hierarchy.ResourcesUpdated -= this.RefreshResources;

			if (this.currentTab > 0)
				this.tabs[this.currentTab - 1].OnLeave();
		}

		protected virtual void	OnGUI()
		{
			InternalNGDebug.AssertFile(this.hierarchy != null, "ResourcesPicker requires to be created through ResourcesPicker.Init.");

			if (this.resources == null)
			{
				this.hierarchy.GetResources(this.type, out this.resources, out this.ids);

				if (this.resources == null)
				{
					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.FlexibleSpace();
						GUILayout.Label(GeneralStyles.StatusWheel, GUILayoutOptionPool.Width(20F));
						EditorGUILayout.LabelField(LC.G("NGHierarchy_ResourcesNotAvailable"), GeneralStyles.WrapLabel);
						GUILayout.FlexibleSpace();
					}
					EditorGUILayout.EndHorizontal();

					return;
				}
				else
				{
					for (int i = 0; i < this.ids.Length; i++)
					{
						if (this.ids[i] == this.initialInstanceID)
						{
							this.FitFocusedRowInScreen(i);
							break;
						}
					}
				}
			}

			if (Event.current.type == EventType.KeyDown)
			{
				if (Event.current.keyCode == KeyCode.UpArrow)
				{
					this.selectedInstanceID = this.GetInstanceIDNeighbour(this.selectedInstanceID, -1);
					this.SendSelection(this.selectedInstanceID);
					this.Repaint();
				}
				else if (Event.current.keyCode == KeyCode.DownArrow)
				{
					this.selectedInstanceID = this.GetInstanceIDNeighbour(this.selectedInstanceID, 1);
					this.SendSelection(this.selectedInstanceID);
					this.Repaint();
				}
				else if (Event.current.keyCode == KeyCode.PageUp)
				{
					this.selectedInstanceID = this.GetInstanceIDNeighbour(this.selectedInstanceID, -(Mathf.FloorToInt((this.position.height - (ResourcesPickerWindow.HeaderHeight + ResourcesPickerWindow.HeaderSpace)) / 16)));
					this.SendSelection(this.selectedInstanceID);
					this.Repaint();
				}
				else if (Event.current.keyCode == KeyCode.PageDown)
				{
					this.selectedInstanceID = this.GetInstanceIDNeighbour(this.selectedInstanceID, Mathf.FloorToInt((this.position.height - (ResourcesPickerWindow.HeaderHeight + ResourcesPickerWindow.HeaderSpace)) / 16));
					this.SendSelection(this.selectedInstanceID);
					this.Repaint();
				}
				else if (Event.current.keyCode == KeyCode.Home)
				{
					this.selectedInstanceID = 0;
					this.SendSelection(this.selectedInstanceID);
					this.Repaint();
				}
				else if (Event.current.keyCode == KeyCode.End)
				{
					this.selectedInstanceID = this.GetInstanceIDFromIndex(this.GetCountResources() - 1, this.ids);
					this.SendSelection(this.selectedInstanceID);
					this.Repaint();
				}
				else if (Event.current.keyCode == KeyCode.Escape)
					this.SendSelection(this.initialInstanceID);
				else if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
				{
					this.SendSelection(this.selectedInstanceID);
					this.Close();
				}
			}

			GUILayout.BeginHorizontal("ObjectPickerToolbar", GUILayoutOptionPool.Height(ResourcesPickerWindow.HeaderHeight));
			{
				EditorGUI.BeginChangeCheck();
				this.searchString = GUILayout.TextField(this.searchString, "SearchTextField");
				if (EditorGUI.EndChangeCheck() == true)
					this.RefreshFilter();
				
				if (GUILayout.Button("", string.IsNullOrEmpty(this.searchString) ? "SearchCancelButtonEmpty" : "SearchCancelButton") == true)
				{
					this.searchString = string.Empty;
					this.selectedInstanceID = 0;
					GUI.FocusControl(null);
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(-18F);
			GUILayout.BeginHorizontal();
			{
				if (GUILayout.Toggle(this.currentTab == 0, "Assets", "ObjectPickerTab") == true)
					this.SetTab(0);

				for (int i = 0; i < this.tabs.Count; i++)
				{
					if (GUILayout.Toggle(this.currentTab == i + 1, this.tabs[i].name, "ObjectPickerTab") == true)
						this.SetTab(i + 1);
				}

				GUILayout.FlexibleSpace();

				if (this.currentTab == 0)
				{
					if (this.hierarchy.IsChannelBlocked(this.type.GetHashCode()) == true)
					{
						GUILayout.Label(GeneralStyles.StatusWheel, GUILayoutOptionPool.Width(20F));
						this.Repaint();
					}
					else if (GUILayout.Button("Refresh", GUILayoutOptionPool.ExpandWidthFalse) == true)
						this.hierarchy.LoadResources(this.type, true);
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(ResourcesPickerWindow.HeaderSpace);

			this.r.x = 0F;
			this.r.y = GUILayoutUtility.GetLastRect().yMax + ResourcesPickerWindow.HeaderSpace;
			this.r.width = this.position.width;
			this.r.height = this.position.height - this.r.y;

			float	bodyHeight = r.height;

			if (this.currentTab == 0)
			{
				this.resourcesCount = this.GetCountResources() + 1;
				this.viewRect.height = this.resourcesCount * Constants.SingleLineHeight;

				this.scrollPosition = GUI.BeginScrollView(this.r, this.scrollPosition, this.viewRect);
				{
					this.r.x = 0F;
					this.r.y = 0F;
					this.r.height = Constants.SingleLineHeight;

					int	i = 0;

					foreach (string resource in this.ForResources(this.resources))
					{
						if (this.r.y + this.r.height <= this.scrollPosition.y)
						{
							this.r.y += this.r.height;
							++i;
							continue;
						}

						int	instanceID = this.GetInstanceIDFromIndex(i - 1, this.ids);

						if (Event.current.type == EventType.Repaint && instanceID == this.selectedInstanceID)
							EditorGUI.DrawRect(r, ResourcesPickerWindow.FocusBackgroundColor);
						else if (Event.current.type == EventType.Repaint && instanceID == this.initialInstanceID)
							EditorGUI.DrawRect(r, ResourcesPickerWindow.InitialBackgroundColor);

						if (Event.current.type == EventType.MouseDown &&
							this.r.Contains(Event.current.mousePosition) == true)
						{
							if (this.selectedInstanceID == instanceID)
								this.Close();
							else
							{
								this.selectedInstanceID = instanceID;
								this.SendSelection(instanceID);
							}

							this.Repaint();

							Event.current.Use();
						}

						GUI.Label(this.r, resource + " (#" + instanceID + ')');

						this.r.y += this.r.height;
						++i;

						if (this.r.y - this.scrollPosition.y > bodyHeight)
							break;
					}
				}
				GUI.EndScrollView();
			}
			else
				this.tabs[this.currentTab - 1].OnGUI(this.r);
		}

		protected virtual void	Update()
		{
			if (EditorApplication.isCompiling == true)
				this.Close();
		}

		protected virtual void	OnLostFocus()
		{
			if (this.currentTab == 0)
				this.Close();
		}

		private void	SetTab(int index)
		{
			if (this.currentTab != index)
			{
				if (this.currentTab > 0)
					this.tabs[this.currentTab - 1].OnLeave();

				this.currentTab = index;

				if (this.currentTab > 0)
					this.tabs[this.currentTab - 1].OnEnter();
			}
		}

		private void	RefreshFilter()
		{
			this.filteredResources.Clear();
			this.filteredResourceIDs.Clear();

			if (string.IsNullOrEmpty(this.searchString) == false)
			{
				for (int i = 0; i < this.resources.Length; i++)
				{
					if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(this.resources[i], this.searchString, CompareOptions.IgnoreCase) >= 0)
					{
						this.filteredResources.Add(this.resources[i]);
						this.filteredResourceIDs.Add(this.ids[i]);
					}
				}
			}
		}

		private void	SendSelection(int instanceID)
		{
			ByteBuffer	buffer = Utility.GetBBuffer();

			this.typeHandler.Serialize(buffer, this.type, new UnityObject(this.type, instanceID));

			this.hierarchy.AddPacket(this.packetGenerator(this.valuePath, Utility.ReturnBBuffer(buffer)), this.onPacketComplete);

			this.FitFocusedRowInScreen(this.GetIndexFromInstanceID(instanceID, this.ids));
		}

		private int	GetInstanceIDNeighbour(int instanceID, int offset)
		{
			return this.GetInstanceIDFromIndex(Mathf.Clamp(this.GetIndexFromInstanceID(instanceID, this.ids) + offset, -1, this.GetCountResources() - 1), this.ids);
		}

		private int	GetIndexFromInstanceID(int instanceID, int[] IDs)
		{
			if (string.IsNullOrEmpty(this.searchString) == false)
			{
				for (int i = 0; i < this.filteredResourceIDs.Count; i++)
				{
					if (this.filteredResourceIDs[i] == instanceID)
						return i;
				}
			}
			else
			{
				for (int i = 0; i < IDs.Length; i++)
				{
					if (IDs[i] == instanceID)
						return i;
				}
			}

			return -1;
		}

		public void	FitFocusedRowInScreen(int index)
		{
			float	y = (index + 1) * Constants.SingleLineHeight;

			if (this.scrollPosition.y > y)
				this.scrollPosition.y = y;
			else if (this.scrollPosition.y + (this.position.height - (ResourcesPickerWindow.HeaderHeight + ResourcesPickerWindow.HeaderSpace)) < y + Constants.SingleLineHeight)
				this.scrollPosition.y = y - (this.position.height - (ResourcesPickerWindow.HeaderHeight + ResourcesPickerWindow.HeaderSpace)) + Constants.SingleLineHeight;
		}

		private IEnumerable	ForResources(string[] resources)
		{
			yield return "None";

			if (string.IsNullOrEmpty(this.searchString) == false)
			{
				for (int i = 0; i < this.filteredResources.Count; i++)
					yield return this.filteredResources[i];
			}
			else
			{
				for (int i = 0; i < resources.Length; i++)
					yield return resources[i];
			}
		}

		private int	GetInstanceIDFromIndex(int index, int[] IDs)
		{
			if (index <= -1)
				return 0;

			if (string.IsNullOrEmpty(this.searchString) == false)
				return this.filteredResourceIDs[index];
			else
				return IDs[index];
		}

		private int	GetCountResources()
		{
			if (string.IsNullOrEmpty(this.searchString) == false)
				return this.filteredResourceIDs.Count;
			else
				return resources.Length;
		}

		private void	RefreshResources(Type type, string[] resourceNames, int[] instanceIDs)
		{
			if (this.type == type)
			{
				this.resources = resourceNames;
				this.ids = instanceIDs;
				this.RefreshFilter();
			}
		}
	}
}