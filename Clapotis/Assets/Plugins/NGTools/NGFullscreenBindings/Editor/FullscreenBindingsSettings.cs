using System;
using UnityEngine;

namespace NGToolsEditor.NGFullscreenBindings
{
	public class FullscreenBindingsSettings : ScriptableObject
	{
		[Serializable]
		public class Binding
		{
			public string	label;
			public string	type;
			public bool		active;
			public bool		ctrl;
			public bool		shift;
			public bool		alt;

			public	Binding(string input, string type)
			{
				int	n = input.LastIndexOf(" ");
				if (n != -1)
				{
					this.label = input.Substring(0, n);

					for (; n < input.Length; n++)
					{
						if (input[n] == '%')
							this.ctrl = true;
						else if (input[n] == '#')
							this.shift = true;
						else if (input[n] == '&')
							this.alt = true;
					}
				}
				else
					this.label = input;

				this.type = type;
			}

			public bool	Equals(Binding other)
			{
				return this.label == other.label &&
					this.CanBeInMenu() == other.CanBeInMenu() &&
					this.ctrl == other.ctrl &&
					this.shift == other.shift &&
					this.alt == other.alt;
			}

			public bool	CanBeInMenu()
			{
				return this.active == true && string.IsNullOrEmpty(this.label) == false && string.IsNullOrEmpty(this.type) == false && Type.GetType(this.type) != null;
			}
		}

		public Binding[]	bindings = new Binding[12] {
			new Binding("Scene _", "UnityEditor.SceneView,UnityEditor"),
			new Binding("Game _", "UnityEditor.GameView,UnityEditor"),
			new Binding("Inspector _", "UnityEditor.InspectorWindow,UnityEditor"),
			new Binding("Hierarchy _", "UnityEditor.SceneHierarchyWindow,UnityEditor"),
			new Binding("Project _", "UnityEditor.ProjectBrowser,UnityEditor"),
			new Binding("Animation _", "UnityEditor.AnimationWindow,UnityEditor"),
			new Binding("Profiler _", "UnityEditor.ProfilerWindow,UnityEditor"),
			new Binding("Audio Mixer _", "UnityEditor.AudioMixerWindow,UnityEditor"),
			new Binding("Asset Store _", "UnityEditor.AssetStoreWindow,UnityEditor"),
			new Binding(string.Empty, string.Empty),
			new Binding(string.Empty, string.Empty),
			new Binding("NG Console _", "NGToolsEditor.NGConsole.NGConsoleWindow") { active = true },
		};
	}
}