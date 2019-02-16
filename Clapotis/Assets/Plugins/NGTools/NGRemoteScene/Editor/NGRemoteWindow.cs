using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[PrewarmEditorWindow]
	public abstract class NGRemoteWindow : EditorWindow
	{
		private NGRemoteHierarchyWindow	hierarchy;
		public NGRemoteHierarchyWindow	Hierarchy { get { return this.hierarchy; } }

		public const string	Title = "NG Remote Scene";

		private int	hierarchyIndex;

		protected virtual void	OnEnable()
		{
			Utility.RegisterWindow(this, typeof(NGRemoteWindow));
			Utility.RestoreIcon(this, NGRemoteHierarchyWindow.TitleColor);
			
			if (this.hierarchy != null)
				EditorApplication.delayCall += this.OnHierarchyInit;
		}

		protected virtual void	OnDisable()
		{
			Utility.UnregisterWindow(this, typeof(NGRemoteWindow));
		}

		protected void	OnGUI()
		{
			FreeLicenseOverlay.First(this, NGTools.NGRemoteScene.NGAssemblyInfo.Name + " Pro", NGRemoteWindow.Title + " is exclusive to NG Tools Pro or NG Remote Scene Pro.\n\nFree version is restrained to read-only.");

			if (this.Hierarchy == null)
			{
				NGRemoteHierarchyWindow[]	hierarchies = Resources.FindObjectsOfTypeAll<NGRemoteHierarchyWindow>();

				if (hierarchies.Length == 0)
				{
					if (GUILayout.Button(LC.G("NGRemote_NoHierarchyAvailable"), GeneralStyles.BigCenterText, GUILayoutOptionPool.ExpandHeightTrue) == true)
						NGRemoteHierarchyWindow.Open();
				}
				else if (hierarchies.Length == 1)
				{
					// Prevents GUI layout warning.
					EditorApplication.delayCall += () =>
					{
						this.SetHierarchy(hierarchies[0]);
						this.Repaint();
					};
				}
				else
				{
					EditorGUILayout.LabelField(LC.G("NGRemote_RequireHierarchy"));

					string[]	hierarchyNames = new string[hierarchies.Length];

					for (int i = 0; i < hierarchies.Length; i++)
						hierarchyNames[i] = hierarchies[i].titleContent.text + ' ' + hierarchies[i].address + ':' + hierarchies[i].port;

					EditorGUILayout.BeginHorizontal();
					{
						this.hierarchyIndex = EditorGUILayout.Popup(this.hierarchyIndex, hierarchyNames);

						if (GUILayout.Button(LC.G("Set")) == true)
							this.SetHierarchy(hierarchies[this.hierarchyIndex]);
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			else
			{
				this.OnGUIHeader();

				if (this.Hierarchy.IsClientConnected() == false)
					this.OnGUIDisconnected();
				else
					this.OnGUIConnected();
			}

			FreeLicenseOverlay.Last(NGTools.NGRemoteScene.NGAssemblyInfo.Name + " Pro");
		}

		public void	SetHierarchy(NGRemoteHierarchyWindow hierarchy)
		{
			if (this.hierarchy == hierarchy)
				return;

			if (this.hierarchy != null)
			{
				this.hierarchy.RemoveRemoteWindow(this);
				this.OnHierarchyUninit();
			}

			this.hierarchy = hierarchy;

			if (this.hierarchy != null)
			{
				this.hierarchy.AddRemoteWindow(this);
				this.OnHierarchyInit();
			}
		}

		protected virtual void	OnHierarchyInit()
		{
			this.hierarchy.HierarchyConnected += this.OnHierarchyConnected;
			this.hierarchy.HierarchyDisconnected += this.OnHierarchyDisconnected;

			if (this.hierarchy.IsClientConnected() == true)
				this.OnHierarchyConnected();
		}

		protected virtual void	OnHierarchyUninit()
		{
			this.hierarchy.HierarchyConnected -= this.OnHierarchyConnected;
			this.hierarchy.HierarchyDisconnected -= this.OnHierarchyDisconnected;
		}

		protected virtual void	OnHierarchyConnected()
		{
			this.Repaint();
		}

		protected virtual void	OnHierarchyDisconnected()
		{
			this.Repaint();
		}

		protected virtual void	OnGUIHeader()
		{
		}

		protected abstract void	OnGUIConnected();

		protected virtual void	OnGUIDisconnected()
		{
			GUILayout.FlexibleSpace();

			if (GUILayout.Button(LC.G("NGRemote_NotConnected") + Environment.NewLine + LC.G("NGRemote_NotConnectedTooltip"), GeneralStyles.BigCenterText, GUILayoutOptionPool.ExpandHeightTrue))
			{
				XGUIHighlightManager.Highlight(NGRemoteHierarchyWindow.NormalTitle + ".Connect");
				NGRemoteHierarchyWindow.Open();
			}

			EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

			GUILayout.FlexibleSpace();
		}
	}
}