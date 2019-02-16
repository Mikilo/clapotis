using NGToolsEditor.NGHub;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[Serializable, Category("Misc")]
	internal sealed class SwitchRemoteWindowsComponent : HubComponent
	{
		[Exportable]
		public bool	displayOnlyIcon;

		[NonSerialized]
		private GUIContent	unityMode;
		[NonSerialized]
		private GUIContent	remoteMode;
		[NonSerialized]
		private GUIContent	unityModeIcon;
		[NonSerialized]
		private GUIContent	remoteModeIcon;
		[NonSerialized]
		private GUIStyle	buttonLeft;
		[NonSerialized]
		private GUIStyle	buttonRight;

		public	SwitchRemoteWindowsComponent() : base("Switch Remote Windows", true, true)
		{
		}

		public override void	Init(NGHubWindow hub)
		{
			base.Init(hub);

			this.unityMode = new GUIContent("Unity", "Bring all Unity windows on top.");
			this.remoteMode = new GUIContent("NG Remote", "Bring all NG Remote windows on top.");
			this.unityModeIcon = new GUIContent(string.Empty, UtilityResources.UnityIcon, this.unityMode.tooltip);
			this.remoteModeIcon = new GUIContent(string.Empty, UtilityResources.NGIcon, this.remoteMode.tooltip);
		}

		public override void	OnEditionGUI()
		{
			this.displayOnlyIcon = EditorGUILayout.Toggle("Display Only Icon", this.displayOnlyIcon);
		}

		public override void	OnGUI()
		{
			if (this.buttonLeft == null)
			{
				this.buttonLeft = "ButtonLeft";
				this.buttonRight = "ButtonRight";
			}

			if (this.displayOnlyIcon == true)
			{
				if (GUILayout.Button(this.unityModeIcon, this.buttonLeft, GUILayoutOptionPool.Height(this.hub.height), GUILayoutOptionPool.Width(24F)) == true)
					NGRemoteHierarchyWindow.FocusUnityWindows();
				Rect	r = GUILayoutUtility.GetLastRect();

				GUI.DrawTexture(r, this.unityModeIcon.image, ScaleMode.ScaleToFit);

				if (GUILayout.Button(this.remoteModeIcon, this.buttonRight, GUILayoutOptionPool.Height(this.hub.height), GUILayoutOptionPool.Width(24F)) == true)
				{
					foreach (NGRemoteHierarchyWindow window in Utility.EachEditorWindows(typeof(NGRemoteHierarchyWindow)))
						window.Focus();

					foreach (NGRemoteWindow window in Utility.EachEditorWindows(typeof(NGRemoteWindow)))
						window.Focus();
				}

				r = GUILayoutUtility.GetLastRect();

				GUI.DrawTexture(r, this.remoteModeIcon.image, ScaleMode.ScaleToFit);
			}
			else
			{
				if (GUILayout.Button(this.unityMode, this.buttonLeft, GUILayoutOptionPool.Height(this.hub.height)) == true)
					NGRemoteHierarchyWindow.FocusUnityWindows();

				if (GUILayout.Button(this.remoteMode, this.buttonRight, GUILayoutOptionPool.Height(this.hub.height)) == true)
				{
					foreach (NGRemoteHierarchyWindow window in Utility.EachEditorWindows(typeof(NGRemoteHierarchyWindow)))
						window.Focus();

					foreach (NGRemoteWindow window in Utility.EachEditorWindows(typeof(NGRemoteWindow)))
						window.Focus();
				}
			}
		}
	}
}