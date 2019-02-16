using NGTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

namespace NGToolsEditor.NGMissingScriptRecovery
{
	using UnityEngine;

	internal sealed class RawComponent
	{
		private sealed class HighlightMatchesPopup : PopupWindowContent
		{
			public const float	Spacing = 2F;
			public const float	FieldsExtraSpacing = 10F;
			public static Color	MatchingColor { get { return Utility.GetSkinColor(0F, 1F, 0F, 1F, 0F, 1F, 0F, 1F); } }
			public static Color	UnmatchingColor { get { return Utility.GetSkinColor(1F, 0F, 0F, 1F, 1F, 0F, 0F, 1F); } }

			public readonly RawComponent	component;
			public readonly PotentialType	potentialType;

			private bool[]			matchingFields;
			private List<string>	extraFields = new List<string>();
			private Vector2			size;
			private GUIStyle		style;

			public	HighlightMatchesPopup(RawComponent component, PotentialType potentialType)
			{
				this.component = component;
				this.potentialType = potentialType;
				this.matchingFields = new bool[component.fields.Count];

				for (int i = 0; i < component.fields.Count; i++)
				{
					for (int j = 0; j < potentialType.fields.Length; j++)
					{
						if (component.fields[i] == potentialType.fields[j])
						{
							this.matchingFields[i] = true;
							break;
						}
					}
				}

				for (int i = 0; i < potentialType.fields.Length; i++)
				{
					int	j = 0;

					for (; j < component.fields.Count; j++)
					{
						if (component.fields[j] == potentialType.fields[i])
							break;
					}

					if (j >= component.fields.Count)
						this.extraFields.Add(potentialType.fields[i]);
				}

				float	maxWidth = 0F;

				for (int i = 0; i < component.fields.Count; i++)
				{
					Utility.content.text = component.fields[i];
					float	width = GUI.skin.label.CalcSize(Utility.content).x;

					if (maxWidth < width)
						maxWidth = width;
				}

				for (int i = 0; i < this.extraFields.Count; i++)
				{
					Utility.content.text = this.extraFields[i];
					float	width = GUI.skin.label.CalcSize(Utility.content).x;

					if (maxWidth < width)
						maxWidth = width;
				}

				float	height = 0F;

				if (component.fields.Count > 0)
					height = (Constants.SingleLineHeight + HighlightMatchesPopup.Spacing) * (component.fields.Count + 1);
				if (this.extraFields.Count > 0)
				{
					if (this.component.fields.Count > 0)
						height += HighlightMatchesPopup.FieldsExtraSpacing;
					height += (Constants.SingleLineHeight + HighlightMatchesPopup.Spacing) * (this.extraFields.Count + 1);
				}

				this.size = new Vector2(maxWidth + HighlightMatchesPopup.Spacing + HighlightMatchesPopup.Spacing, height - HighlightMatchesPopup.Spacing);
			}

			public override Vector2	GetWindowSize()
			{
				return size;
			}

			public override void	OnGUI(Rect r)
			{
				if (this.style == null)
					this.style = new GUIStyle(GUI.skin.label);

				r.height = Constants.SingleLineHeight;

				if (this.component.fields.Count > 0)
				{
					GUI.Label(r, "Fields (" + this.component.fields.Count + ")", GeneralStyles.WrapLabel);
					r.y += r.height + HighlightMatchesPopup.Spacing;

					r.x += HighlightMatchesPopup.Spacing;
					for (int i = 0; i < this.component.fields.Count; i++)
					{
						if (this.matchingFields[i] == true)
							this.style.normal.textColor = HighlightMatchesPopup.MatchingColor;
						else
							this.style.normal.textColor = HighlightMatchesPopup.UnmatchingColor;
						GUI.Label(r, this.component.fields[i], this.style);
						r.y += r.height + HighlightMatchesPopup.Spacing;
					}
					r.x -= HighlightMatchesPopup.Spacing;
				}

				if (this.extraFields.Count > 0)
				{
					if (this.component.fields.Count > 0)
						r.y += HighlightMatchesPopup.FieldsExtraSpacing;

					GUI.Label(r, "Extra fields (" + this.extraFields.Count + ")", GeneralStyles.WrapLabel);
					r.y += r.height + HighlightMatchesPopup.Spacing;

					r.x += HighlightMatchesPopup.Spacing;
					for (int i = 0; i < this.extraFields.Count; i++)
					{
						GUI.Label(r, this.extraFields[i]);
						r.y += r.height + HighlightMatchesPopup.Spacing;
					}
				}
			}
		}

		public string		componentID = string.Empty;
		public string		line = string.Empty;
		public string		name = string.Empty;
		public Object		asset;
		public List<string>	fields = new List<string>();
		public List<PotentialType>	potentialTypes = new List<PotentialType>();

		private bool		open = true;
		private bool		fieldsOpen = true;
		private Vector2		scrollPositionFields;
		private string		cachedFields = null;
		private HighlightMatchesPopup	hoverPopup;

		public	RawComponent(string componentID)
		{
			this.componentID = componentID;
		}

		public void	Draw(NGMissingScriptRecoveryWindow window, bool force = false)
		{
			if (this.cachedFields == null)
			{
				StringBuilder	buffer = Utility.GetBuffer();

				for (int i = 0; i < this.fields.Count; i++)
					buffer.AppendLine(this.fields[i]);

				if (buffer.Length > 0)
					buffer.Length -= Environment.NewLine.Length;

				this.cachedFields = Utility.ReturnBuffer(buffer);
			}

			EditorGUI.BeginDisabledGroup(this.fields.Count <= 0);
			{
				Utility.content.text = this.name;
				Utility.content.image = Utility.GetIcon(this.asset ? this.asset.GetInstanceID() : 0);
				this.open = EditorGUILayout.Foldout(this.open, Utility.content) && this.fields.Count > 0 || force;
				Utility.content.image = null;
			}
			EditorGUI.EndDisabledGroup();

			if (this.fields.Count == 0 && this.asset == null)
				EditorGUILayout.HelpBox("Component seems to have absolutely no fields. Recovery can not proceed.", MessageType.Warning);

			if (this.open == false)
				return;

			++EditorGUI.indentLevel;
			this.fieldsOpen = EditorGUILayout.Foldout(this.fieldsOpen, "Fields found (" + this.fields.Count + ")");
			if (this.fieldsOpen == true)
			{
				++EditorGUI.indentLevel;
				Utility.content.text = this.cachedFields;
				float h = EditorStyles.label.CalcHeight(Utility.content, window.position.width - 60F);
				//float h = this.fields.Count * 15F;
				float totalH = h;

				if (h >= NGMissingScriptRecoveryWindow.MaxFieldsHeight)
					h = NGMissingScriptRecoveryWindow.MaxFieldsHeight;

				//Utility.content.text = this.cachedFields;
				//Rect	viewRect = new Rect();
				//Rect	body = new Rect(4F, GUILayoutUtility.GetLastRect().yMax, window.position.width, 0F);
				//Rect	r2 = GUILayoutUtility.GetRect(0F, h);
				//body.height = r2.height;
				//viewRect.height = EditorStyles.label.CalcSize(Utility.content).y;

				//EditorGUI.DrawRect(body, Color.blue);
				//this.scrollPositionFields = GUI.BeginScrollView(body, this.scrollPositionFields, viewRect);
				//{
				//	Rect	selectableRect = body;
				//	selectableRect.x = 0F;
				//	selectableRect.y = 0F;
				//	selectableRect.width = body.width - (viewRect.height > body.height ? 16F : 0F);
				//	selectableRect.height = viewRect.height > body.height ? EditorStyles.label.CalcHeight(Utility.content, selectableRect.width) : viewRect.height;
				//	EditorGUI.SelectableLabel(selectableRect, this.cachedFields, EditorStyles.label);
				//}
				//GUI.EndScrollView();

				// TODO Fix bottom margin/padding?
				EditorGUI.HelpBox(new Rect(30F, GUILayoutUtility.GetLastRect().yMax, window.position.width - 60F, h + 10F), string.Empty, MessageType.None);
				//if (useScrollbar == true)
					this.scrollPositionFields = EditorGUILayout.BeginScrollView(this.scrollPositionFields, GUILayoutOptionPool.Height(h + 5F), GUILayoutOptionPool.Width(window.position.width - 30F));
				//else
					//EditorGUI.HelpBox(new Rect(4F, GUILayoutUtility.GetLastRect().yMax, window.position.width, h), string.Empty, MessageType.None);
				{
					EditorGUILayout.SelectableLabel(this.cachedFields, EditorStyles.label, GUILayoutOptionPool.Height(totalH));
					//for (int j = 0; j < this.fields.Count; j++)
					//	EditorGUILayout.LabelField(this.fields[j]);
				}
				//if (useScrollbar == true)
					EditorGUILayout.EndScrollView();
				--EditorGUI.indentLevel;
			}

			CachedLineFix	lineFix = window.FindCachedComponentFixes(this.line);
			if (lineFix != null)
			{
				if (GUILayout.Button("Recover From Cache (" + lineFix.type + ")") == true)
				{
					this.FixLine(window, this.componentID, lineFix.type);
					window.Diagnose(window.Target);
				}
			}

			EditorGUILayout.LabelField("Potential types:");
			++EditorGUI.indentLevel;
			if (this.potentialTypes.Count == 0)
				EditorGUILayout.LabelField("No type available.");
			else
			{
				for (int l = 0; l < this.potentialTypes.Count; l++)
				{
					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.Space(30F);

						if (GUILayout.Button(this.potentialTypes[l].type.FullName, GUILayoutOptionPool.ExpandWidthFalse) == true)
						{
							this.FixMissingComponent(window, this.componentID, this.potentialTypes[l].type);
							window.Diagnose(window.Target);
							return;
						}

						if (this.potentialTypes[l].matchingFields == this.fields.Count &&
							this.potentialTypes[l].fields.Length == this.fields.Count)
						{
							GUILayout.Label("(Perfect match)", GUILayoutOptionPool.ExpandWidthFalse);
						}
						else
						{
							int	extraFields = this.potentialTypes[l].fields.Length == this.potentialTypes[l].matchingFields ? this.potentialTypes[l].matchingFields - this.fields.Count : this.potentialTypes[l].fields.Length - this.potentialTypes[l].matchingFields;
							GUILayout.Label("(Fields: " + this.potentialTypes[l].matchingFields + " matching" + (extraFields > 0 ? ", +" + extraFields + " extra" : (extraFields < 0 ? ", " + extraFields + " missing" : string.Empty)) + ")", GUILayoutOptionPool.ExpandWidthFalse);
						}

						if (Event.current.type == EventType.MouseMove)
						{
							Rect	r = GUILayoutUtility.GetLastRect();

							r.xMin = 0F;
							if (r.Contains(Event.current.mousePosition) == true)
							{
								if (this.hoverPopup != null)
								{
									if (this.hoverPopup.potentialType.type != this.potentialTypes[l].type)
									{
										if (this.hoverPopup.editorWindow != null)
											this.hoverPopup.editorWindow.Close();
										this.hoverPopup = new HighlightMatchesPopup(this, this.potentialTypes[l]);
										r.x = 30F;
										r.y += 5F;
										PopupWindow.Show(r, this.hoverPopup);
									}
								}
								else
								{
									this.hoverPopup = new HighlightMatchesPopup(this, this.potentialTypes[l]);
									r.x = 30F;
									r.y += 5F;
									PopupWindow.Show(r, this.hoverPopup);
								}
							}
							else
							{
								if (this.hoverPopup != null && this.hoverPopup.potentialType.type == this.potentialTypes[l].type)
								{
									if (this.hoverPopup.editorWindow != null)
										this.hoverPopup.editorWindow.Close();
									this.hoverPopup = null;
								}
							}
						}
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			--EditorGUI.indentLevel;
			--EditorGUI.indentLevel;
		}

		public void	FixMissingComponent(NGMissingScriptRecoveryWindow window, string componentID, Type type)
		{
			TypeIdentifiers	identifiers = MetadataDatabase.GetIdentifiers(type);

			if (identifiers != null)
			{
				this.FixLine(window, componentID, type, "  m_Script: {fileID: " + identifiers.localIdentifier + ", guid: " + identifiers.guid + ", type: 3}");
				return;
			}

			GameObject	tempPrefab = new GameObject("MissingComponentRecovery", type);

			PrefabUtility.CreatePrefab(NGMissingScriptRecoveryWindow.TempPrefabPath, tempPrefab, ReplacePrefabOptions.ConnectToPrefab);

			if (File.Exists(NGMissingScriptRecoveryWindow.TempPrefabPath) == true)
			{
				AssetDatabase.SaveAssets();
				this.FixLine(window, componentID, type);
			}

			Object.DestroyImmediate(tempPrefab);
			AssetDatabase.DeleteAsset(NGMissingScriptRecoveryWindow.TempPrefabPath);

			Selection.activeGameObject = null;

			// Select again few updates after, to avoid Inspector to crash.
			EditorApplication.delayCall += () => EditorApplication.delayCall += () => Selection.activeGameObject = window.Target;
		}

		public void	FixLine(NGMissingScriptRecoveryWindow window, string componentID, Type type, string fixedLine = null)
		{
			string		prefabPath = AssetDatabase.GetAssetPath(window.Target);
			string[]	lines = File.ReadAllLines(prefabPath);

			for (int m = 0; m < lines.Length; m++)
			{
				if (lines[m].Length > 12 && lines[m][0] == '-' && lines[m][2] == '-' && lines[m].EndsWith(componentID) == true)
				{
					for (; m < lines.Length; m++)
					{
						if (lines[m].StartsWith("  m_Script") == true)
						{
							if (fixedLine == null)
							{
								CachedLineFix	lineFix = window.FindCachedComponentFixes(lines[m]);

								// Get the cached line only if the Type matches.
								if (lineFix != null && lineFix.type == type)
									fixedLine = lineFix.fixedLine;
								else
								{
									fixedLine = this.ExtractFixGUIDFromTempPrefab();

									if (string.IsNullOrEmpty(fixedLine) == true)
									{
										InternalNGDebug.LogError("Impossible to extract new GUID from temp prefab. Recovery aborted.");
										return;
									}

									if (lineFix != null)
									{
										lineFix.fixedLine = fixedLine;
										lineFix.type = type;
									}
									else
										window.AddCachedComponentFixes(new CachedLineFix() { brokenLine = lines[m], fixedLine = fixedLine, type = type });
								}
							}

							lines[m] = fixedLine;

							try
							{
								File.WriteAllLines(prefabPath, lines);
								AssetDatabase.Refresh();
							}
							catch (IOException ex)
							{
								InternalNGDebug.LogException("Recovering \"" + prefabPath + "\" failed. File seems to be locked. Please try to recover again or restart Unity or your computer.", ex);
							}
							catch (Exception ex)
							{
								InternalNGDebug.LogException("Recovering \"" + prefabPath + "\" failed. Please try to recover again.", ex);
							}

							break;
						}
					}

					break;
				}
			}
		}

		private string	ExtractFixGUIDFromTempPrefab()
		{
			using (FileStream fs = File.Open(NGMissingScriptRecoveryWindow.TempPrefabPath, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (BufferedStream bs = new BufferedStream(fs))
			using (StreamReader sr = new StreamReader(bs))
			{
				string	line;

				while ((line = sr.ReadLine()) != null)
				{
					if (line.StartsWith("  m_Script") == true)
						return line;
				}
			}

			return null;
		}
	}
}