using System.Collections.Generic;
using System.Text;
using UnityEditor;

namespace NGToolsEditor.NGAssetFinder
{
	using UnityEngine;

	internal sealed class AssetMatches
	{
		public enum Type
		{
			Reference,
			Component
		}

		public const float	PingButtonWidth = 40F;

		private static double			lastClick;
		internal static AssetMatches	workingAssetMatches;

		public Object				origin;
		public List<Match>			matches;
		public List<AssetMatches>	children;
		public Type					type;

		private bool	open;
		public bool		Open
		{
			get
			{
				return this.open;
			}
			set
			{
				this.open = value;
				if (Event.current != null && Event.current.alt == true)
				{
					for (int i = 0; i < this.matches.Count; i++)
						this.matches[i].Open = value;
					for (int i = 0; i < this.children.Count; i++)
						this.children[i].Open = value;
				}
			}
		}

		public bool	allowSceneObject;

		public GUIContent	content;
		public Texture2D	image;

		public	AssetMatches(Object origin)
		{
			this.origin = origin;
			this.matches = new List<Match>();
			this.children = new List<AssetMatches>();

			this.type = Type.Reference;

			this.open = true;
		}

		public void	AggregateMatch(Object instance, string propertyPath)
		{
			List<Match>	current = this.matches;
			string[]	parts = propertyPath.Split('.');

			for (int i = 0; i < parts.Length; i++)
			{
				int	j = 0;

				for (; j < current.Count; j++)
				{
					if (current[j].path == parts[i])
					{
						current = current[j].subMatches;
						break;
					}
				}

				if (j >= current.Count)
				{
					if (i == parts.Length - 1)
						current.Add(new Match(instance, propertyPath) { nicifiedPath = Utility.NicifyVariableName(parts[i]) });
					else
						current.Add(new Match(parts[i]));
					current = current[current.Count - 1].subMatches;
				}
			}
		}

		public void	PrepareResults()
		{
			for (int i = 0; i < this.matches.Count; i++)
				this.matches[i].PreCacheGUI();
			for (int i = 0; i < this.children.Count; i++)
				this.children[i].PrepareResults();

			this.content = new GUIContent();

			if (this.origin is GameObject)
			{
				this.allowSceneObject = (this.origin as GameObject).gameObject.scene.IsValid();

				if (this.matches.Count > 0 || this.children.Count > 0)
					this.content.text = this.origin.name;
				else
					this.content.text = this.origin.name + " (Component)";
			}
			else if (this.origin is Component)
			{
				this.allowSceneObject = (this.origin as Component).gameObject.scene.IsValid();

				if (this.matches.Count > 0 || this.children.Count > 0)
					this.content.text = Utility.NicifyVariableName(this.origin.GetType().Name);
				else
					this.content.text = Utility.NicifyVariableName(this.origin.GetType().Name) + " (Component)";
			}
			else if (this.origin is MonoScript)
			{
				this.content.text = this.origin.name + " (Read-only)";
				this.content.tooltip = "Unfortunately, scripts must be assigned manually.";
				this.allowSceneObject = false;
			}
			else
			{
				this.content.text = this.origin.name;
				this.allowSceneObject = true;
			}

			this.content.text = "   " + this.content.text;

			this.image = Utility.GetIcon(this.origin.GetInstanceID());
		}

		public float	GetHeight()
		{
			if (this.type == AssetMatches.Type.Reference &&
				this.matches.Count == 0 &&
				this.children.Count == 0)
			{
				return 0F;
			}

			float	height = Constants.SingleLineHeight;

			if (this.Open == true)
			{
				for (int i = 0; i < this.matches.Count; i++)
					height += this.matches[i].GetHeight();
				for (int i = 0; i < this.children.Count; i++)
					height += this.children[i].GetHeight();
			}

			return height;
		}

		public void	Draw(NGAssetFinderWindow window, Rect r)
		{
			if (this.type == AssetMatches.Type.Reference &&
				this.matches.Count == 0 &&
				this.children.Count == 0)
			{
				return;
			}

			float	w = r.width;
			r.height = Constants.SingleLineHeight;

			bool	isNull = this.origin == null;

			if (Event.current.type == EventType.Repaint)
			{
				if (r.Contains(Event.current.mousePosition) == true)
					EditorGUI.DrawRect(r, NGAssetFinderWindow.HighlightBackground);
				else if (isNull == false && Selection.activeObject == this.origin)
					EditorGUI.DrawRect(r, NGAssetFinderWindow.SelectedAssetBackground);
			}

			if (isNull == false && (this.origin is Component) == false)
				r.width -= AssetMatches.PingButtonWidth;

			EditorGUI.BeginDisabledGroup(isNull);
			{
				if (this.matches.Count > 0 ||
					this.children.Count > 0)
				{
					EditorGUI.BeginChangeCheck();
					bool	open = EditorGUI.Foldout(r, this.Open, this.content, true);
					if (EditorGUI.EndChangeCheck() == true)
						this.Open = open;

					if (string.IsNullOrEmpty(this.content.tooltip) == false)
					{
						if (r.Contains(Event.current.mousePosition) == true)
						{
							Rect	r2 = r;
							r2.y -= r2.height;
							EditorGUI.DrawRect(r2, NGAssetFinderWindow.HighlightBackground);
							GUI.Label(r2, this.content.tooltip);
						}
					}

					r.xMin += ((EditorGUI.indentLevel + 1) * 15F) - 4F;
				}
				else
					r.xMin += (EditorGUI.indentLevel * 15F) - 4F;

				if (this.image != null)
				{
					Rect	r2 = r;

					r2.width = r2.height;
					GUI.DrawTexture(r2, this.image);

					r.xMin += r2.width;
				}

				if (this.matches.Count == 0 && this.children.Count == 0)
				{
					r.xMin -= 16F;
					GUI.Label(r, this.content);
					r.xMin += 16F;
				}

				if (isNull == false && (this.origin is Component) == false)
				{
					r.x += r.width;
					r.width = AssetMatches.PingButtonWidth;

					if (GUI.Button(r, LC.G("Ping")) == true)
					{
						if (Event.current.button != 0 || AssetMatches.lastClick + Constants.DoubleClickTime > EditorApplication.timeSinceStartup)
							Selection.activeObject = this.origin;
						else
							EditorGUIUtility.PingObject(this.origin.GetInstanceID());

						AssetMatches.lastClick = EditorApplication.timeSinceStartup;
					}
				}

				if (this.Open == true)
				{
					r.x = 0F;
					r.y += r.height;
					r.width = w;

					EditorGUI.BeginChangeCheck();

					++EditorGUI.indentLevel;
					AssetMatches.workingAssetMatches = this;
					for (int i = 0; i < this.matches.Count; i++)
					{
						r.height = this.matches[i].GetHeight();
						this.matches[i].Draw(window, r);
						r.y += r.height;
					}

					for (int i = 0; i < this.children.Count; i++)
					{
						r.height = this.children[i].GetHeight();
						this.children[i].Draw(window, r);
						r.y += r.height;
					}
					--EditorGUI.indentLevel;

					if (EditorGUI.EndChangeCheck() == true && this.origin != null)
						EditorUtility.SetDirty(this.origin);
				}
			}
			EditorGUI.EndDisabledGroup();
		}

		public void	Export(StringBuilder buffer, int indentLevel = 0)
		{
			if (this.type == AssetMatches.Type.Reference &&
				this.matches.Count == 0 &&
				this.children.Count == 0)
			{
				return;
			}

			if (this.origin == null)
			{
				buffer.Append(SearchResult.ExportIndent, indentLevel);

				if (this.matches.Count > 0 ||
					this.children.Count > 0)
				{
					buffer.Append(this.content.text, 3, this.content.text.Length - 3);
					buffer.AppendLine(" (NULL)");
				}
				else
				{
					buffer.Append(this.content.text, 3, this.content.text.Length - 3);
					buffer.AppendLine();
				}
			}
			else
			{
				buffer.Append(SearchResult.ExportIndent, indentLevel);

				string	path = indentLevel == 0 ? AssetDatabase.GetAssetPath(origin) : null;

				if (string.IsNullOrEmpty(path) == false)
					buffer.Append(path);
				else
					buffer.Append(this.content.text, 3, this.content.text.Length - 3);
				buffer.AppendLine();
			}

			if (this.Open == true)
			{
				AssetMatches.workingAssetMatches = this;
				for (int j = 0; j < this.matches.Count; j++)
					this.matches[j].Export(buffer, indentLevel + 1);
				for (int i = 0; i < this.children.Count; i++)
					this.children[i].Export(buffer, indentLevel + 1);
			}
		}
	}
}