using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	[Serializable]
	[Exportable(ExportableAttribute.ArrayOptions.Immutable)]
	public abstract class Module
	{
		public int	Id { get { return this.id; } }

		/// <summary>
		/// The name displayed in the tab on the header bar.
		/// </summary>
		public string	name = "Unnamed";

		[NonSerialized]
		public NGConsoleWindow	console;
		[NonSerialized]
		private int			id = -1;

		/// <summary>Called during OnEnable of NGConsole. Prepares the Module.</summary>
		/// <param name="editor">An instance of NGConsole to work on.</param>
		/// <param name="id">The ID of the current module. Is only available for visible module.</param>
		public virtual void	OnEnable(NGConsoleWindow editor, int id)
		{
			this.console = editor;
			this.id = id;
		}

		/// <summary>Called rightafter EditorWindow's OnDisable.</summary>
		public virtual void	OnDisable()
		{
		}

		/// <summary>Called by EditorWindow when focused. Must have a positive menuPosition.</summary>
		/// <param name="r"></param>
		public virtual void	OnGUI(Rect r)
		{
		}

		/// <summary>Called when EditorWindow focus on this tab.</summary>
		public virtual void	OnEnter()
		{
		}

		/// <summary>Called when EditorWindow leaves this tab. Be aware! ConsoleSettings might me null at this moment.</summary>
		public virtual void	OnLeave()
		{
		}

		/// <summary>
		/// Draws the module in the header. The default behaviour is to draw a simple tab button.
		/// </summary>
		/// <param name="focusedModuleID">ID of the focused module.</param>
		public virtual Rect	DrawMenu(Rect r, int focusedModuleID)
		{
			GeneralSettings	settings = HQ.Settings.Get<GeneralSettings>();
			Utility.content.text = this.name;
			r.width = settings.MenuButtonStyle.CalcSize(Utility.content).x;

			EditorGUI.BeginChangeCheck();
			NGEditorGUILayout.OutlineToggle(r, this.name, this.id == focusedModuleID, settings.MenuButtonStyle);
			if (EditorGUI.EndChangeCheck() == true)
			{
				if (Event.current.button == 1 ||
					Event.current.control == true)
				{
					ConsoleUtility.OpenModuleInWindow(this.console, this, Event.current.control);
				}
				else if (this.id != focusedModuleID)
					this.Focus();
			}

			r.x += r.width;

			return r;
		}

		/// <summary>
		/// Sets the focus to this tab if visible (menuPosition higher than 0 is required).
		/// </summary>
		public void	Focus()
		{
			if (this.id >= 0)
				this.console.SetModule(this.id);
		}
	}
}