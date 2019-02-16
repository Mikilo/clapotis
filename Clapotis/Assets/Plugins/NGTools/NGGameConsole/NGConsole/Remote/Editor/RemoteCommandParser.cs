using NGTools.NGGameConsole;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGGameConsole
{
	internal sealed class RemoteCommandParser : CommandParser
	{
		public int	scrollOffset = 0;

		private RecycledTextEditorProxy	textEditor = new RecycledTextEditorProxy();

		public override Rect	PostGUI(Rect r, ref string command)
		{
			if (this.matchingCommands != null)
			{
				RemoteModuleSettings	settings = HQ.Settings.Get<RemoteModuleSettings>();

				string[]	commands;
				string[]	arguments;
				this.ParseInput(command, out commands, out arguments);

				r.height = 16F;
				r.x = settings.HighlightedMatchStyle.CalcSize(new GUIContent(this.precachedParentCommand)).x;
				r.width = 200F;

				if (Event.current.type == EventType.ScrollWheel)
				{
					if (Event.current.delta.y < 0F)
					{
						if (this.selectedMatchingCommand < this.matchingCommands.Length - 1)
							++this.selectedMatchingCommand;
					}
					else
					{
						if (this.selectedMatchingCommand >= 0)
							--this.selectedMatchingCommand;
					}

					Event.current.Use();
				}

				if (this.selectedMatchingCommand + 1 >= this.scrollOffset + this.maxCommandsOnScreen)
					this.scrollOffset = this.selectedMatchingCommand + 1 - this.maxCommandsOnScreen;
				else if (this.selectedMatchingCommand < this.scrollOffset)
				{
					if (this.selectedMatchingCommand == -1)
						this.scrollOffset = 0;
					else
						this.scrollOffset = this.selectedMatchingCommand;
				}

				for (int i = this.scrollOffset, j = 0; i < this.matchingCommands.Length && j < this.maxCommandsOnScreen; ++i, ++j)
				{
					r.y -= r.height;

					if (Event.current.type == EventType.Repaint)
					{
						if (r.Contains(Event.current.mousePosition) == false)
							EditorGUI.DrawRect(r, settings.completionBackgroundColor);
						else
							EditorGUI.DrawRect(r, settings.hoverCompletionBackgroundColor);
					}

					string	highlighted = ((i == this.selectedMatchingCommand) ? "<b>" : "") +
						"<color=#" +
						((int)(settings.partialCompletionColor.r * 255)).ToString("X2") +
						((int)(settings.partialCompletionColor.g * 255)).ToString("X2") +
						((int)(settings.partialCompletionColor.b * 255)).ToString("X2") +
						((int)(settings.partialCompletionColor.a * 255)).ToString("X2") + ">" +
						this.matchingCommands[i].Substring(0, commands[commands.Length - 1].Length) +
						"</color>" +
						this.matchingCommands[i].Substring(commands[commands.Length - 1].Length) +
						((i == this.selectedMatchingCommand) ? "</b>" : "");

					if (GUI.Button(r, highlighted, settings.HighlightedMatchStyle) == true)
					{
						int		argumentsPosition = command.IndexOf(NGCLI.CommandsArgumentsSeparator);
						string	rawArguments = string.Empty;

						if (argumentsPosition != -1)
							rawArguments = command.Substring(argumentsPosition + 1);

						commands[commands.Length - 1] = this.matchingCommands[i];

						textEditor.instance = EditorGUIProxy.s_RecycledEditor;

						if (rawArguments != string.Empty)
						{
							command = string.Join(NGCLI.CommandsSeparator.ToString(), commands) +
											NGCLI.CommandsArgumentsSeparator +
											rawArguments;
							this.SetCursor(command, command.Length - rawArguments.Length - 1);
						}
						else
						{
							command = string.Join(NGCLI.CommandsSeparator.ToString(), commands);
							this.SetCursor(command, command.Length);
						}

						if (textEditor.text != command)
						{
							textEditor.text = command;
							textEditor.MoveTextEnd();
						}

						this.matchingCommands = null;
						break;
					}
				}

				if (Event.current.type == EventType.Repaint &&
					this.scrollOffset + this.maxCommandsOnScreen < this.matchingCommands.Length)
				{
					r.height = 10F;
					r.y -= r.height;

					EditorGUI.DrawRect(r, settings.completionBackgroundColor);
					GUI.Label(r, "... (" + (this.matchingCommands.Length - this.scrollOffset - this.maxCommandsOnScreen) + ")", GeneralStyles.SmallLabel);
				}

				r.x = 0F;
			}
			else
				this.scrollOffset = 0;

			return r;
		}
	}
}