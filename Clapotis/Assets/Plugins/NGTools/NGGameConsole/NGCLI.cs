using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NGTools.NGGameConsole
{
	using UnityEngine;

	/// <summary>
	/// Gives the possibility to assign a class a description.
	/// </summary>
	public interface ICommandHelper
	{
		string	Helper { get; }
	}

	public class NGCLI : MonoBehaviour
	{
		[Serializable]
		public class AliasBehaviour
		{
			public string		alias;
			public Behaviour	behaviour;
		}

		public const string		ReceivedCommandColor = "green";
		public const string		PendingCommandColor = "yellow";
		public const string		ErrorCommandColor = "red";
		public const char		CommandsSeparator = '.';
		public static char[]	ArgumentsSeparator = new char[] { ' ', '	' };
		public const char		CommandsArgumentsSeparator = ' ';
		public static char[]	ForbiddenChars = new char[] { ' ' };
		public static string[]	EmptyArray = {};

		private static List<NGCLI>	instances = new List<NGCLI>();

		[Header("Keep NG CLI alive between scenes.")]
		public bool	dontDestroyOnLoad = true;

		[Header("[Required] Available commands to execute.")]
		public AliasBehaviour[]	rootCommands;

		[Header("Automatically handles static classes as root commands.")]
		public bool	includeStaticTypes = true;

		[Header("[Optional] Link to a GameConsole to stick.")]
		public NGGameConsole	gameConsole;
		[Header("Height is used in both cases."),
		Header("[Optional] If no GameConsole, defines the position of the input on the screen.")]
		public Rect				inputArea;

		public Color	completionBgColor = Color.cyan;
		public Color	hoverCompletionBgColor = Color.grey;
		public Color	partialCompletionColor;
		public GUIStyle	highlightedMatchStyle;
		public GUIStyle	commandInputStyle;
		public float	execButtonWidth = 70F;
		public GUIStyle	execButtonStyle;

		internal LocalCommandParser	parser;

		private string	command = string.Empty;

		protected virtual void	Reset()
		{
			if (this.highlightedMatchStyle == null)
				this.highlightedMatchStyle = new GUIStyle();
			this.highlightedMatchStyle.richText = true;
		}

		protected virtual void	Awake()
		{
			if (this.parser != null)
				return;

			for (int i = 0; i < NGCLI.instances.Count; i++)
			{
				if (NGCLI.instances[i].GetType() == this.GetType())
				{
					Object.Destroy(this.gameObject);
					return;
				}
			}

			this.parser = new LocalCommandParser(this);
			this.parser.LazyInit += this.InitializeCommands;
			this.parser.CallExec += this.Exec;

			this.gameConsole.GameConsoleEnableChanged += this.OnGameConsoleEnableChanged;

			if (this.dontDestroyOnLoad == true)
			{
				NGCLI.instances.Add(this);
				Object.DontDestroyOnLoad(this.transform.root.gameObject);
			}
		}

		protected virtual void	OnEnable()
		{
			if (this.gameConsole != null)
			{
				this.gameConsole.ReserveFootSpace(this.inputArea.height);
				//this.gameConsole.AddSetting("NG CLI", this.GUISettings);
			}
		}

		protected virtual void	OnDisable()
		{
			if (this.gameConsole != null)
			{
				this.gameConsole.ReserveFootSpace(-this.inputArea.height);
				//this.gameConsole.RemoveSetting("NG CLI", this.GUISettings);
			}
		}

		protected virtual void	OnDestroy()
		{
			this.gameConsole.GameConsoleEnableChanged -= this.OnGameConsoleEnableChanged;

			if (this.parser != null)
			{
				this.parser.LazyInit -= this.InitializeCommands;
				this.parser.CallExec -= this.Exec;
			}
		}

		protected virtual void	OnGUI()
		{
			if (this.gameConsole != null &&
				((this.gameConsole.enabled == false || this.gameConsole.enabled == false) ||
				 this.gameConsole.tab != NGGameConsole.Tab.Logs))
			{
				return;
			}

			GUI.depth = -1;

			Rect	r = this.inputArea;
			if (this.gameConsole != null)
			{
				r = new Rect(this.gameConsole.windowSize.x,
							 this.gameConsole.windowSize.y + this.gameConsole.windowSize.height - this.inputArea.height,
							 this.gameConsole.windowSize.width,
							 r.height);

				if (this.gameConsole.resizable == true)
					r.width -= 25F;
			}

			float	width = r.width;

			this.parser.HandleKeyboard(ref this.command);

			r.width -= this.execButtonWidth;
			GUI.SetNextControlName(CommandParser.CommandTextFieldName);
			this.command = GUI.TextField(r, this.command, this.commandInputStyle);
			if (GUI.changed == true)
				this.parser.UpdateMatchesAvailable(this.command);

			r.x += r.width;
			r.width = this.execButtonWidth;
			if (GUI.Button(r, "Exec", this.execButtonStyle) == true)
				this.Exec();

			r.x -= width - this.execButtonWidth;
			r.width = width;
			this.parser.PostGUI(r, ref this.command);
		}

		protected virtual void	OnValidate()
		{
			if (this.execButtonWidth < 10F)
				this.execButtonWidth = 10F;
		}

		private void	Exec()
		{
			GameLog		log;
			string		result = string.Empty;
			ExecResult	returnValue = this.parser.Exec(this.command, ref result);

			if (returnValue == ExecResult.Success)
			{
				log = new GameLog(this.command,
								  "<color=" + NGCLI.ReceivedCommandColor + ">></color> " + result,
								  LogType.Log,
								  this.gameConsole != null ? this.gameConsole.timeFormat : string.Empty);
			}
			else
			{
				log = new GameLog(this.command,
								  "<color=" + NGCLI.ErrorCommandColor + ">></color> " + result,
								  LogType.Error,
								  this.gameConsole != null ? this.gameConsole.timeFormat : string.Empty);
			}

			log.opened = true;

			if (this.gameConsole != null)
				this.gameConsole.AddGameLog(log);

			this.command = string.Empty;
			this.parser.Clear();
		}

		private void	GUISettings()
		{
			GUILayout.Button("Test");
		}

		private void	OnGameConsoleEnableChanged(bool enable)
		{
			this.enabled = enable;
		}

		private void	InitializeCommands()
		{
			if (this.rootCommands != null && this.rootCommands.Length > 0)
			{
				for (int i = 0; i < this.rootCommands.Length; i++)
				{
					if (string.IsNullOrEmpty(this.rootCommands[i].alias) == true)
					{
						InternalNGDebug.Log(Errors.CLI_RootCommandEmptyAlias, "Root command #" + i + " as an empty alias.");
						continue;
					}
					if (this.rootCommands[i].behaviour == null)
					{
						InternalNGDebug.Log(Errors.CLI_RootCommandNullBehaviour, "Root command \"" + this.rootCommands[i].alias + "\" has no behaviour.");
						continue;
					}

					ICommandHelper	helper = this.rootCommands[i].behaviour as ICommandHelper;
					string			description = string.Empty;

					if (helper != null)
						description = helper.Helper;

					this.parser.AddRootCommand(new BehaviourCommand(this.rootCommands[i].alias,
																	description,
																	this.rootCommands[i].behaviour));
				}
			}

			if (this.includeStaticTypes == true)
			{
				StringBuilder	buffer = Utility.GetBuffer();

				foreach (Type type in Utility.EachAllSubClassesOf(typeof(object)))
				{
					if (type.IsEnum == true ||
						type.Name.StartsWith("<Private") == true ||
						(type.Name == "Consts" && type.Namespace == null)) // This particular hidden type is redundant as fuck, and it has no namespace!
					{
						continue;
					}

					FieldInfo[]		fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
					PropertyInfo[]	properties = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

					if (fields.Length > 0 || properties.Length > 0)
					{
						CommandNode	node = this.CreateOrGetNodeFromClass(type);

						for (int i = 0; i < fields.Length; i++)
						{
							try
							{
								this.SortedAdd(node, new MemberCommand(new FieldModifier(fields[i]), null));
							}
							catch (NotSupportedMemberTypeException ex)
							{
								if (buffer.Length == 0)
									buffer.Append("#" + Errors.CLI_UnsupportedPropertyType + " - ");
								buffer.AppendLine("Field \"" + ex.member.Name + "\" of type \"" + ex.member.Type.Name + "\" from \"" + ex.member.MemberInfo.DeclaringType.Name + "\" is unsupported.");
							}
						}

						for (int i = 0; i < properties.Length; i++)
						{
							try
							{
								this.SortedAdd(node, new MemberCommand(new PropertyModifier(properties[i]), null));
							}
							catch (NotSupportedMemberTypeException ex)
							{
								if (buffer.Length == 0)
									buffer.Append("#" + Errors.CLI_UnsupportedPropertyType + " - ");
								buffer.AppendLine("Property \"" + ex.member.Name + "\" of type \"" + ex.member.Type.Name + "\" from \"" + ex.member.MemberInfo.DeclaringType.Name + "\" is unsupported.");
							}
						}
					}
				}

				if (buffer.Length > 0)
				{
					buffer.Length -= Environment.NewLine.Length;
					InternalNGDebug.VerboseLog(Utility.ReturnBuffer(buffer));
				}
				else
					Utility.RestoreBuffer(buffer);
			}

			this.CleanEmptyNodes(this.parser.Root);

			if (this.parser.Root.children.Count == 0)
				InternalNGDebug.Log(Errors.CLI_EmptyRootCommand, "There is not root command in your CLI.");
		}

		/// <summary>Removes nodes that does not contain any leaf commands.</summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private bool	CleanEmptyNodes(CommandNode node)
		{
			bool	empty = true;

			for (int i = 0; i < node.children.Count; i++)
			{
				if (node.children[i].IsLeaf == false && this.CleanEmptyNodes(node.children[i]) == true)
				{
					node.children.RemoveAt(i);
					--i;
					continue;
				}
				else
					empty = false;
			}

			return empty;
		}

		private void	SortedAdd(CommandNode parent, CommandNode child)
		{
			for (int i = 0; i < parent.children.Count; i++)
			{
				//if (string.Compare(parent.children[i].name, child.name) >= 0)
				if (parent.children[i].name.CompareTo(child.name) >= 0)
				{
					parent.children.Insert(i, child);
					return;
				}
			}

			parent.children.Add(child);
		}

		private CommandNode	CreateOrGetNodeFromClass(Type type)
		{
			string[]	namespaces = type.FullName.Split('.');
			int			matchingIndex = 0;
			CommandNode	node = this.parser.Root;

			for (int i = 0; i < node.children.Count; i++)
			{
				if (node.children[i].name == namespaces[matchingIndex])
				{
					++matchingIndex;

					if (namespaces.Length == matchingIndex)
						return node.children[i];

					node = node.children[i];
					i = -1;
				}
			}

			while (matchingIndex < namespaces.Length)
			{
				CommandNode	subNode = new CommandNode(null, namespaces[matchingIndex], string.Empty);
				this.SortedAdd(node, subNode);
				node = subNode;
				++matchingIndex;
			}

			return node;
		}
	}
}