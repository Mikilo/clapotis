using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	internal sealed class BrowseLogExportSourcesPopup : PopupWindowContent
	{
		public const float	Spacing = 2F;

		private List<ILogExportSource>		availableOutputs = new List<ILogExportSource>();
		private Action<ILogExportSource>	onCreate;
		private Vector2						size;

		public	BrowseLogExportSourcesPopup(List<ILogExportSource> usedOutputs, Action<ILogExportSource> onCreate)
		{
			this.onCreate = onCreate;

			float	maxWidth = 0F;

			foreach (Type type in Utility.EachNGTAssignableFrom(typeof(ILogExportSource)))
			{
				if (usedOutputs.Exists(s => s.GetType() == type) == false)
				{
					ILogExportSource	exportOutput = Activator.CreateInstance(type) as ILogExportSource;
					Utility.content.text = exportOutput.GetName();
					float	w = GUI.skin.button.CalcSize(Utility.content).x;

					if (maxWidth < w)
						maxWidth = w;

					this.availableOutputs.Add(exportOutput);
				}
			}

			if (this.availableOutputs.Count == 0)
				this.size = new Vector2(200F, 100F);
			else
				this.size = new Vector2(maxWidth, availableOutputs.Count * (Constants.SingleLineHeight + BrowseLogExportSourcesPopup.Spacing) - BrowseLogExportSourcesPopup.Spacing);
		}

		public override Vector2	GetWindowSize()
		{
			return this.size;
		}

		public override void	OnGUI(Rect rect)
		{
			if (this.availableOutputs.Count == 0)
			{
				GUI.Label(rect, "No more sources available.", GeneralStyles.BigCenterText);
				return;
			}

			rect.height = Constants.SingleLineHeight;

			for (int i = 0; i < this.availableOutputs.Count; i++)
			{
				if (GUI.Button(rect, this.availableOutputs[i].GetName(), GeneralStyles.ToolbarButton) == true)
				{
					this.onCreate(this.availableOutputs[i]);
					this.availableOutputs.RemoveAt(i);

					if (this.availableOutputs.Count == 0)
						this.editorWindow.Close();
					return;
				}

				rect.y += rect.height + BrowseLogExportSourcesPopup.Spacing;
			}
		}
	}
}