using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEditor;

namespace NGToolsEditor.NGConsole
{
	using UnityEngine;

	[Serializable]
	internal sealed class ContentFilter : ILogFilter
	{
		private enum SearchMode
		{
			Content,
			StackTrace,
			Both,
		}

		public string	Name { get { return "By content"; } }
		[Exportable]
		private bool	enabled = true;
		public bool		Enabled { get { return this.enabled; } set { if (this.enabled != value) { this.enabled = value; if (this.ToggleEnable != null) this.ToggleEnable(); } } }

		public event Action	ToggleEnable;

		[Exportable]
		private string			keyword = string.Empty;
		[Exportable]
		private SearchMode		searchMode;
		[Exportable]
		private CompareOptions	caseSensitive = CompareOptions.IgnoreCase;
		[Exportable]
		private bool			wholeWord;
		[Exportable]
		private bool			useRegex;
		private string			regexSyntaxError;

		[NonSerialized]
		private GUIContent		cs;
		[NonSerialized]
		private GUIContent		whole;
		[NonSerialized]
		private GUIContent		regex;
		[NonSerialized]
		private string[]		searchPatterns;
		[NonSerialized]
		private Dictionary<string, FilterResult>	cachedResults = new Dictionary<string, FilterResult>();

		public FilterResult	CanDisplay(Row row)
		{
			if (string.IsNullOrEmpty(this.keyword) == true)
				return FilterResult.None;

			ILogContentGetter	logContent = row as ILogContentGetter;

			if (logContent == null)
				return FilterResult.None;

			FilterResult	cachedResult;

			if (this.cachedResults.TryGetValue(row.log.condition, out cachedResult) == true)
				return cachedResult;

			if (this.useRegex == true || this.wholeWord == true)
			{
				RegexOptions	options = RegexOptions.Multiline;

				if (this.caseSensitive == CompareOptions.IgnoreCase)
					options |= RegexOptions.IgnoreCase;

				string	keyword = this.keyword;
				if (this.wholeWord == true)
					keyword = "\\b" + keyword + "\\b";

				try
				{
					if (this.searchMode == SearchMode.Content ||
						this.searchMode == SearchMode.Both)
					{
						if (Regex.IsMatch(logContent.FullMessage, keyword, options))
						{
							this.cachedResults.Add(row.log.condition, FilterResult.Accepted);
							return FilterResult.Accepted;
						}
					}

					if (this.searchMode == SearchMode.StackTrace ||
						this.searchMode == SearchMode.Both)
					{
						if (Regex.IsMatch(logContent.StackTrace, keyword, options))
						{
							this.cachedResults.Add(row.log.condition, FilterResult.Accepted);
							return FilterResult.Accepted;
						}
					}
				}
				catch (Exception ex)
				{
					this.regexSyntaxError = ex.Message;
				}
			}
			else
			{
				if (this.searchPatterns == null)
					this.searchPatterns = Utility.SplitKeywords(this.keyword, ' ');

				if (this.searchMode == SearchMode.Content ||
					this.searchMode == SearchMode.Both)
				{
					int	i = 0;

					if (this.caseSensitive == CompareOptions.None)
					{
						for (; i < this.searchPatterns.Length; i++)
						{
							if (logContent.FullMessage.FastContains(this.searchPatterns[i]) == false)
								break;
						}

						if (i == this.searchPatterns.Length)
						{
							this.cachedResults.Add(row.log.condition, FilterResult.Accepted);
							return FilterResult.Accepted;
						}
					}
					else
					{
						for (; i < this.searchPatterns.Length; i++)
						{
							if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(logContent.FullMessage, this.searchPatterns[i], this.caseSensitive) < 0)
								break;
						}

						if (i == this.searchPatterns.Length)
						{
							this.cachedResults.Add(row.log.condition, FilterResult.Accepted);
							return FilterResult.Accepted;
						}
					}
				}

				if (this.searchMode == SearchMode.StackTrace ||
					this.searchMode == SearchMode.Both)
				{
					int	i = 0;

					if (this.caseSensitive == CompareOptions.None)
					{
						for (; i < this.searchPatterns.Length; i++)
						{
							if (logContent.StackTrace.FastContains(this.searchPatterns[i]) == false)
								break;
						}

						if (i == this.searchPatterns.Length)
						{
							this.cachedResults.Add(row.log.condition, FilterResult.Accepted);
							return FilterResult.Accepted;
						}
					}
					else
					{
						for (; i < this.searchPatterns.Length; i++)
						{
							if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(logContent.StackTrace, this.searchPatterns[i], this.caseSensitive) < 0)
								break;
						}
					}

					if (i == this.searchPatterns.Length)
					{
						this.cachedResults.Add(row.log.condition, FilterResult.Accepted);
						return FilterResult.Accepted;
					}
				}
			}

			this.cachedResults.Add(row.log.condition, FilterResult.Refused);
			return FilterResult.Refused;
		}

		public Rect	OnGUI(Rect r, bool compact)
		{
			if (this.cs == null)
			{
				this.cs = new GUIContent("Ab", LC.G("CaseSensitive"));
				this.regex = new GUIContent("R*", LC.G("RegularExpressions"));
				this.whole = new GUIContent("|abc|", LC.G("WholeMatch"));
			}

			float		xMax = r.xMax;
			SearchMode	mode;

			GUI.Box(r, string.Empty, GeneralStyles.Toolbar);

			if (Event.current.type == EventType.ValidateCommand)
			{
				if (Event.current.commandName == "Find")
				{
					GUI.FocusControl("keyword");
					EditorGUIUtility.editingTextField = true;
					Event.current.Use();
				}
			}
			else if (Event.current.type == EventType.KeyDown)
			{
				if (Event.current.keyCode == KeyCode.F)
				{
					if (this.keyword.Length > 0)
					{
						GUI.changed = true;
						this.keyword = string.Empty;
						this.searchPatterns = null;
						this.cachedResults.Clear();
						this.CheckRegex();
					}
				}
			}

			if (compact == false)
			{
				r.width = 200F;
				using (LabelWidthRestorer.Get(120F))
				{
					EditorGUI.BeginChangeCheck();
					mode = (SearchMode)EditorGUI.EnumPopup(r, LC.G("SearchMode"), this.searchMode, GeneralStyles.ToolbarDropDown);
				}
				if (EditorGUI.EndChangeCheck() == true)
				{
					//Undo.RecordObject(HQ.Settings, "Alter content filter");
					this.searchMode = mode;
					this.cachedResults.Clear();
				}
				r.x += r.width;

				r.width = 30F;
				EditorGUI.BeginChangeCheck();
				GUI.Toggle(r, this.caseSensitive == CompareOptions.None, this.cs, GeneralStyles.ToolbarToggle);
				if (EditorGUI.EndChangeCheck() == true)
				{
					//Undo.RecordObject(HQ.Settings, "Alter content filter");
					if (this.caseSensitive == CompareOptions.IgnoreCase)
						this.caseSensitive = CompareOptions.None;
					else
						this.caseSensitive = CompareOptions.IgnoreCase;
					this.cachedResults.Clear();
				}
				r.x += r.width;

				r.width = 40F;
				EditorGUI.BeginChangeCheck();
				GUI.Toggle(r, this.wholeWord, this.whole, GeneralStyles.ToolbarToggle);
				if (EditorGUI.EndChangeCheck() == true)
				{
					//Undo.RecordObject(HQ.Settings, "Alter content filter");
					this.wholeWord = !this.wholeWord;
					this.cachedResults.Clear();
				}
				r.x += r.width;

				r.width = 30F;
				EditorGUI.BeginChangeCheck();
				GUI.Toggle(r, this.useRegex, this.regex, GeneralStyles.ToolbarToggle);
				if (EditorGUI.EndChangeCheck() == true)
				{
					//Undo.RecordObject(HQ.Settings, "Alter content filter");
					this.useRegex = !this.useRegex;
					this.cachedResults.Clear();
					this.CheckRegex();
				}
				r.x += r.width + 2F;
			}

			using (BgColorContentRestorer.Get(string.IsNullOrEmpty(this.regexSyntaxError) == false, Color.red))
			{
				bool	unfocused = false;

				if (Event.current.type == EventType.KeyDown)
				{
					if (Event.current.keyCode == KeyCode.DownArrow || Event.current.keyCode == KeyCode.UpArrow)
						unfocused = true;
				}

				r.y += 2F;
				r.width = xMax - r.x - 16F;
				EditorGUI.BeginChangeCheck();
				GUI.SetNextControlName("keyword");
				string	keyword = EditorGUI.TextField(r, this.keyword, GeneralStyles.ToolbarSearchTextField);
				if (EditorGUI.EndChangeCheck() == true)
				{
					//Undo.RecordObject(HQ.Settings, "Alter content filter");
					this.keyword = keyword;
					this.searchPatterns = null;
					this.cachedResults.Clear();
					this.CheckRegex();
				}
				r.x += r.width;

				if (unfocused == true)
				{
					EditorGUIUtility.editingTextField = false;
					Event.current.type = EventType.KeyDown;
				}

				r.width = 16F;
				if (GUI.Button(r, GUIContent.none, GeneralStyles.ToolbarSearchCancelButton) == true)
				{
					//Undo.RecordObject(HQ.Settings, "Alter content filter");
					this.keyword = string.Empty;
					this.searchPatterns = null;
					this.cachedResults.Clear();
					this.regexSyntaxError = null;
					GUI.FocusControl(null);
				}
			}
			r.y += r.height;

			return r;
		}

		private void	CheckRegex()
		{
			this.regexSyntaxError = null;

			if (this.useRegex == true)
			{
				try
				{
					Regex.IsMatch("", this.keyword);
				}
				catch (Exception ex)
				{
					this.regexSyntaxError = ex.Message;
				}
			}
		}

		public void	ContextMenu(GenericMenu menu, Row row, int i)
		{
			if (row is ILogContentGetter)
				menu.AddItem(new GUIContent("#" + i + " " + LC.G("FilterByThisContent")), false, this.ActiveFilter, row);
		}

		private void	ActiveFilter(object data)
		{
			ILogContentGetter	logContent = data as ILogContentGetter;

			this.keyword = logContent.HeadMessage;
			this.Enabled = true;
		}
	}
}