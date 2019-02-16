using System;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	[Serializable]
	public sealed class InputCommand
	{
		public const byte	Control = 1 << 0;
		public const byte	Shift = 1 << 1;
		public const byte	Alt = 1 << 2;

		/// <summary>
		/// String appended to the name to fetch the locale.
		/// </summary>
		public const string	DescriptionLocalizationSuffix = "_Description";

		public string	name;
		public KeyCode	keyCode;
		public byte		modifiers;

		/// <summary></summary>
		/// <param name="name">Name of the command. Used to call this command. Is also used as a Localization Key.</param>
		/// <param name="keyCode">KeyCode required by this command.</param>
		/// <param name="control">Whether modifier control is required.</param>
		/// <param name="shift">Whether modifier shift is required.</param>
		/// <param name="alt">Whether modifier alt is required.</param>
		public	InputCommand(string name, KeyCode keyCode, bool control, bool shift, bool alt)
		{
			this.name = name;
			this.keyCode = keyCode;
			this.modifiers = (byte)((control == true ? InputCommand.Control : (byte)0) | (shift == true ? InputCommand.Shift : (byte)0) | (alt == true ? InputCommand.Alt : (byte)0));
		}

		public bool	Check()
		{
			EventType	targetKey = EventType.KeyDown;

			// Exception! Because this event is not passed as KeyDown but only at KeyUp.
			if (this.keyCode == KeyCode.Tab && (this.modifiers & InputCommand.Control) != 0)
				targetKey = EventType.KeyUp;

			return Event.current.type == targetKey &&
				   this.keyCode != KeyCode.None &&
				   Event.current.keyCode == this.keyCode &&
				   Event.current.control == ((this.modifiers & InputCommand.Control) != 0) &&
				   Event.current.shift == ((this.modifiers & InputCommand.Shift) != 0) &&
				   Event.current.alt == ((this.modifiers & InputCommand.Alt) != 0);
		}
	}
}