using NGTools.Network;
using NGTools.NGRemoteScene;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	public class ComponentsBrowserWindow : PopupWindowContent
	{
		public const string	Title = "Add Component";

		private readonly NGRemoteHierarchyWindow	hierarchy;
		private readonly int						gameObjectInstanceID;

		private string		searchKeywords = string.Empty;
		private string		selectedType = string.Empty;
		private string[]	searchPatterns;

		private Rect		bodyRect = new Rect();
		private Rect		viewRect = new Rect();
		private List<int>	filteredTypes = new List<int>(1024);
		private Vector2		scrollPosition = new Vector2();

		public	ComponentsBrowserWindow(NGRemoteHierarchyWindow hierarchy, int gameObjectInstanceID)
		{
			this.hierarchy = hierarchy;
			this.gameObjectInstanceID = gameObjectInstanceID;
		}

		public override void	OnOpen()
		{
			this.editorWindow.titleContent.text = ComponentsBrowserWindow.Title;

			this.searchPatterns = Utility.SplitKeywords(this.searchKeywords, ' ');
		}

		public override Vector2	GetWindowSize()
		{
			return new Vector2(300F, 400F);
		}

		public override void	OnGUI(Rect r)
		{
			string[]	types = this.hierarchy.GetComponentTypes();

			EditorGUI.BeginDisabledGroup(types == null);
			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				EditorGUI.BeginChangeCheck();
				this.searchKeywords = EditorGUILayout.TextField(this.searchKeywords, GeneralStyles.ToolbarSearchTextField, GUILayoutOptionPool.ExpandWidthTrue);
				if (EditorGUI.EndChangeCheck() == true)
				{
					this.searchPatterns = Utility.SplitKeywords(this.searchKeywords, ' ');
					this.RefreshFilter(types);
				}

				if (GUILayout.Button("", GeneralStyles.ToolbarSearchCancelButton) == true)
				{
					this.searchKeywords = string.Empty;
					this.searchPatterns = Utility.SplitKeywords(this.searchKeywords, ' ');
					GUI.FocusControl(null);
					this.RefreshFilter(types);
				}
			}
			EditorGUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();

			if (types == null)
			{
				EditorGUILayout.BeginHorizontal();
				{
					GUILayout.FlexibleSpace();
					GUILayout.Label(GeneralStyles.StatusWheel, GUILayoutOptionPool.Width(20F));
					GUILayout.Label("Loading types...");
					GUILayout.FlexibleSpace();
				}
				EditorGUILayout.EndHorizontal();

				this.editorWindow.Repaint();

				return;
			}

			this.bodyRect = GUILayoutUtility.GetLastRect();
			this.bodyRect.y += this.bodyRect.height;

			float	maxY = this.bodyRect.yMax;

			r.height = Constants.SingleLineHeight;

			this.bodyRect.height = this.editorWindow.position.height - this.bodyRect.y;
			this.viewRect = new Rect(0F, 0F, 0F, this.CountTypes(types) * (ClientComponent.MemberSpacing + Constants.SingleLineHeight) - ClientComponent.MemberSpacing);

			this.scrollPosition = GUI.BeginScrollView(this.bodyRect, this.scrollPosition, this.viewRect);
			{
				r.width = this.editorWindow.position.width - (viewRect.height > this.bodyRect.height ? 16F : 0F);
				r.height = Constants.SingleLineHeight;

				int i = 0;

				if (this.viewRect.height > this.bodyRect.height)
				{
					i = (int)(this.scrollPosition.y / (ClientComponent.MemberSpacing + r.height));
					r.y = i * (ClientComponent.MemberSpacing + r.height);
				}

				foreach (string label in this.EachType(types, i--))
				{
					++i;

					if (r.y + r.height + ClientComponent.MemberSpacing <= this.scrollPosition.y)
					{
						r.y += r.height + ClientComponent.MemberSpacing;
						continue;
					}

					Color	restore = GeneralStyles.ToolbarLeftButton.normal.textColor;
					if (label == this.selectedType)
						GeneralStyles.ToolbarLeftButton.normal.textColor = GeneralStyles.HighlightActionButton;

					Utility.content.text = label;
					Utility.content.tooltip = label;
					if (GUI.Button(r, Utility.content, GeneralStyles.ToolbarLeftButton) == true)
					{
						if (this.selectedType == label)
							this.hierarchy.AddPacket(new ClientAddComponentPacket(this.gameObjectInstanceID, this.selectedType), this.OnComponentAdded);

						this.selectedType = label;
					}
					GeneralStyles.ToolbarLeftButton.normal.textColor = restore;

					r.y += r.height + ClientComponent.MemberSpacing;
					if (r.y - this.scrollPosition.y > this.bodyRect.height)
						break;
				}
			}
			GUI.EndScrollView();

			this.bodyRect.y = maxY;
		}

		protected virtual void	Update()
		{
			if (this.hierarchy.IsClientConnected() == false || EditorApplication.isCompiling == true)
				this.editorWindow.Close();
		}

		private int	CountTypes(string[] types)
		{
			if (string.IsNullOrEmpty(this.searchKeywords) == true)
				return types.Length;
			else
				return this.filteredTypes.Count;
		}

		private IEnumerable<string>	EachType(string[] types, int offset)
		{
			if (string.IsNullOrEmpty(this.searchKeywords) == true)
			{
				for (int i = offset; i < types.Length; i++)
					yield return types[i];
			}
			else
			{
				for (int i = offset; i < this.filteredTypes.Count; i++)
					yield return types[this.filteredTypes[i]];
			}
		}

		private void	RefreshFilter(string[] types)
		{
			this.filteredTypes.Clear();

			if (types == null)
				return;

			for (int j = 0; j < types.Length; j++)
			{
				int	i = 0;

				for (; i < this.searchPatterns.Length; i++)
				{
					if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(types[j], this.searchPatterns[i], CompareOptions.IgnoreCase) < 0)
						break;
				}

				if (i == this.searchPatterns.Length)
					this.filteredTypes.Add(j);
			}
		}

		private void	OnComponentAdded(ResponsePacket p)
		{
			this.editorWindow.Repaint();

			if (p.CheckPacketStatus() == true)
				this.editorWindow.Close();
			else
				this.editorWindow.ShowNotification(new GUIContent("Component could not be added."));
		}
	}
}