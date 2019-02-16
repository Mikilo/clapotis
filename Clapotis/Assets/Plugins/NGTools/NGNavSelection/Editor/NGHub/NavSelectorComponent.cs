using NGToolsEditor.NGHub;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGNavSelection
{
	[Serializable, Category("Misc")]
	internal sealed class NavSelectorComponent : HubComponent
	{
		[NonSerialized]
		private GUIContent	leftContent;
		[NonSerialized]
		private GUIContent	rightContent;
		[NonSerialized]
		private GUIStyle	buttonLeft;
		[NonSerialized]
		private GUIStyle	buttonRight;

		public	NavSelectorComponent() : base("Navigation Selector")
		{
		}

		public override void	Init(NGHubWindow hub)
		{
			base.Init(hub);

			this.leftContent = new GUIContent("<", "Select previous selection");
			this.rightContent = new GUIContent(">", "Select next selection");

			NGNavSelectionWindow.SelectionChanged += this.hub.Repaint;
		}

		public override void	Uninit()
		{
			base.Uninit();

			NGNavSelectionWindow.SelectionChanged -= this.hub.Repaint;
		}

		public override void	OnGUI()
		{
			if (this.buttonLeft == null)
			{
				this.buttonLeft = "ButtonLeft";
				this.buttonRight = "ButtonRight";
			}

			EditorGUI.BeginDisabledGroup(!NGNavSelectionWindow.CanSelectPrevious);
			{
				if (GUILayout.Button(this.leftContent, this.buttonLeft, GUILayoutOptionPool.Height(this.hub.height)) == true)
					NGNavSelectionWindow.SelectPreviousSelection();
			}
			EditorGUI.EndDisabledGroup();

			EditorGUI.BeginDisabledGroup(!NGNavSelectionWindow.CanSelectNext);
			{
				if (GUILayout.Button(this.rightContent, this.buttonRight, GUILayoutOptionPool.Height(this.hub.height)) == true)
					NGNavSelectionWindow.SelectNextSelection();
			}
			EditorGUI.EndDisabledGroup();
		}
	}
}