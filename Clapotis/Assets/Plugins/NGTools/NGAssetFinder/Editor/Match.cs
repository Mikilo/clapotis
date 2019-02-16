using NGTools;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;

namespace NGToolsEditor.NGAssetFinder
{
	using UnityEngine;

	internal sealed class Match
	{
		// TODO Unity <5.6 backward compatibility
		private static MethodInfo	UpdateIfRequiredOrScriptMethod = typeof(SerializedObject).GetMethod("UpdateIfRequiredOrScript", BindingFlags.Instance | BindingFlags.Public) ?? UnityAssemblyVerifier.TryGetMethod(typeof(SerializedObject), "UpdateIfDirtyOrScript", BindingFlags.Instance | BindingFlags.Public);

		public bool			valid;
		public string		path;
		public List<Match>	subMatches = new List<Match>(0);
		public List<int>	arrayIndexes = new List<int>(0);
		public Type			type;

		public string	nicifiedPath;
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
					for (int i = 0; i < this.subMatches.Count; i++)
						this.subMatches[i].Open = value;
				}
			}
		}

		public string	Name
		{
			get
			{
				if (this.fieldModifier != null)
					return this.fieldModifier.Name;
				if (this.instance != null)
					return ((Object)this.instance).name;
				return this.path;
			}
		}

		public object	Value
		{
			get
			{
				if (this.fieldModifier != null)
					return this.fieldModifier.GetValue(this.instance);

				if (this.sp != null)
				{
					Match.UpdateIfRequiredOrScriptMethod.Invoke(this.so, null);
					return this.sp.objectReferenceValue;
				}
				return null;
			}
			set
			{
				if (this.fieldModifier != null)
					this.fieldModifier.SetValue(this.instance, value);
				else
				{
					if (this.sp != null)
					{
						this.sp.objectReferenceValue = value as Object;
						this.so.ApplyModifiedProperties();
					}
				}
			}
		}

		public object			instance;
		public IFieldModifier	fieldModifier;

		private SerializedObject	so;
		private SerializedProperty	sp;

		public	Match(object instance, IFieldModifier fieldModifier)
		{
			this.path = fieldModifier.Name;
			this.instance = instance;
			this.fieldModifier = fieldModifier;

			this.type = this.fieldModifier.Type;
		}

		public	Match(string path)
		{
			this.path = path;
		}

		public	Match(Object instance, string path)
		{
			this.path = path;
			this.instance = instance;

			this.so = new SerializedObject((Object)this.instance);
			this.sp = this.so.FindProperty(this.path);

			if (this.sp.type.StartsWith("PPtr<") == true)
				this.type = Utility.GetType(this.sp.type.Substring("PPtr<".Length, this.sp.type.Length - "PPtr<".Length - 1));
			else
				this.type = Utility.GetType(this.sp.type);
			if (this.type == null)
				this.type = typeof(Object);
		}

		public void	PreCacheGUI()
		{
			if (this.nicifiedPath == null)
				this.nicifiedPath = Utility.NicifyVariableName(this.path);

			this.open = true;

			for (int i = 0; i < this.subMatches.Count; i++)
				this.subMatches[i].PreCacheGUI();
		}
		
		public float	GetHeight()
		{
			float	height = Constants.SingleLineHeight;

			if (this.subMatches.Count > 0)
			{
				if (this.Open == true)
				{
					for (int i = 0; i < this.subMatches.Count; i++)
						height += this.subMatches[i].GetHeight();
				}

				return height;
			}

			if (this.arrayIndexes.Count > 0)
			{
				if (this.Open == true)
					height += this.arrayIndexes.Count * Constants.SingleLineHeight;

				return height;
			}

			return height;
		}

		public void	Draw(NGAssetFinderWindow window, Rect r)
		{
			r.height = Constants.SingleLineHeight;

			if (this.subMatches.Count > 0)
			{
				if (Event.current.type == EventType.Repaint &&
					r.Contains(Event.current.mousePosition) == true)
				{
					EditorGUI.DrawRect(r, NGAssetFinderWindow.HighlightBackground);
				}

				EditorGUI.BeginChangeCheck();
				bool	open = EditorGUI.Foldout(r, this.Open, this.nicifiedPath, true);
				if (EditorGUI.EndChangeCheck() == true)
					this.Open = open;

				if (this.Open == true)
				{
					r.y += r.height;

					++EditorGUI.indentLevel;
					for (int i = 0; i < this.subMatches.Count; i++)
					{
						r.height = this.subMatches[i].GetHeight();
						this.subMatches[i].Draw(window, r);
						r.y += r.height;
					}
					--EditorGUI.indentLevel;
				}
				return;
			}

			if (this.arrayIndexes.Count > 0)
			{
				if (Event.current.type == EventType.Repaint &&
					r.Contains(Event.current.mousePosition) == true)
				{
					EditorGUI.DrawRect(r, NGAssetFinderWindow.HighlightBackground);
				}

				EditorGUI.BeginChangeCheck();
				bool	open = EditorGUI.Foldout(r, this.Open, this.nicifiedPath, true);
				if (EditorGUI.EndChangeCheck() == true)
					this.Open = open;

				if (this.Open == true)
				{
					r.y += r.height;

					++EditorGUI.indentLevel;

					ICollectionModifier	collectionModifier = null;
					TypeFinder			finder = null;

					try
					{
						if (window.canReplace == true)
						{
							int	j = 0;
							for (; j < window.typeFinders.array.Length; j++)
							{
								if (window.typeFinders.array[j].CanFind(this.type) == true)
								{
									finder = window.typeFinders.array[j];
									window.typeFinders.BringToTop(j);
									break;
								}
							}

							if (finder == null)
							{
								object	rawArray = this.Value;

								if (rawArray != null)
									collectionModifier = NGTools.Utility.GetCollectionModifier(rawArray);
							}
						}

						for (int j = 0; j < this.arrayIndexes.Count; j++)
						{
							if (Event.current.type == EventType.Repaint &&
								r.Contains(Event.current.mousePosition) == true)
							{
								EditorGUI.DrawRect(r, NGAssetFinderWindow.HighlightBackground);
							}

							if (window.canReplace == false)
							{
								using (LabelWidthRestorer.Get(r.width))
								{
									EditorGUI.LabelField(r, "#" + this.arrayIndexes[j].ToCachedString());
								}
							}
							else
							{
								Object	reference = null;

								try
								{
									if (finder != null)
										reference = finder.Get(this.type, this, this.arrayIndexes[j]);
									else
										reference = collectionModifier.Get(this.arrayIndexes[j]) as Object;

									Utility.content.text = "#" + this.arrayIndexes[j].ToCachedString();
									float	w = GUI.skin.label.CalcSize(Utility.content).x;

									using (ColorContentRestorer.Get(window.replaceAsset != null && (collectionModifier != null && collectionModifier.SubType.IsAssignableFrom(window.replaceAsset.GetType()) == false), Color.red))
									{
										EditorGUI.PrefixLabel(r, Utility.content);

										r.x += w;
										r.width -= w;
										try
										{
											EditorGUI.BeginChangeCheck();
											Object	o = EditorGUI.ObjectField(r, reference, window.targetType, AssetMatches.workingAssetMatches.allowSceneObject);
											if (EditorGUI.EndChangeCheck() == true)
											{
												Undo.RecordObject(AssetMatches.workingAssetMatches.origin, "Match assignment");

												if (finder != null)
													finder.Set(this.type, o, this, this.arrayIndexes[j]);
												else
													collectionModifier.Set(this.arrayIndexes[j], o);

												AssetDatabase.SaveAssets();
											}
										}
										catch (ExitGUIException)
										{
										}
										r.x -= w;
										r.width += w;
									}
								}
								catch (Exception ex)
								{
									window.errorPopup.exception = ex;
									EditorGUILayout.LabelField("Error " + this.nicifiedPath + "	" + AssetMatches.workingAssetMatches + " /" + reference + "-" + this.instance + "	" + ex.Message + "	" + ex.StackTrace);
								}
							}

							r.y += r.height;
						}
					}
					finally
					{
						if (collectionModifier != null)
							NGTools.Utility.ReturnCollectionModifier(collectionModifier);
					}

					--EditorGUI.indentLevel;
				}
				return;
			}

			++EditorGUI.indentLevel;

			if (window.canReplace == false)
			{
				using (LabelWidthRestorer.Get(r.width))
				{
					r.xMin += (EditorGUI.indentLevel - 1) * 15F;

					if (typeof(Object).IsAssignableFrom(this.type) == true)
					{
						Texture2D	image = Utility.GetIcon(this.Value as Object);

						if (image != null)
						{
							Rect	r2 = r;
							r2.width = r2.height;
							GUI.DrawTexture(r2, image);

							r.xMin += r2.width;
						}
					}

					GUI.Label(r, this.nicifiedPath);
				}
			}
			else
			{
				Object	reference = null;

				try
				{

					Utility.content.text = this.nicifiedPath;
					//Rect	r = EditorGUILayout.GetControlRect(false);
					float	w = GUI.skin.label.CalcSize(Utility.content).x;

					if (typeof(Object).IsAssignableFrom(this.type) == true)
					{
						reference = this.Value as Object;

						Texture2D	image = Utility.GetIcon(reference);

						if (image != null)
						{
							Rect	r2 = r;
							r2.xMin += (EditorGUI.indentLevel - 1) * 15F;
							r2.width = r2.height;
							GUI.DrawTexture(r2, image);
						}
					}
					else
						throw new Exception("Field \"" + this.Name + "\" not an Object.");

					using (LabelWidthRestorer.Get(r.xMax))
					EditorGUI.BeginDisabledGroup(this.instance is MonoScript || (window.replaceAsset != null && this.type.IsAssignableFrom(window.replaceAsset.GetType()) == false));
					{
						r.x += 1F;
						EditorGUI.LabelField(r, Utility.content);

						try
						{
							r.x += w;
							r.width -= w;
							EditorGUI.BeginChangeCheck();
							Object	o = EditorGUI.ObjectField(r, reference, this.type, AssetMatches.workingAssetMatches.allowSceneObject);
							if (EditorGUI.EndChangeCheck() == true)
							{
								Undo.RecordObject(AssetMatches.workingAssetMatches.origin, "Match assignment");
								this.Value = o;
								AssetDatabase.SaveAssets();
							}
						}
						catch (ExitGUIException)
						{
						}
					}
					EditorGUI.EndDisabledGroup();
				}
				catch (Exception ex)
				{
					window.errorPopup.exception = ex;
					EditorGUILayout.LabelField("Error " + this.nicifiedPath + "	" + AssetMatches.workingAssetMatches + " /" + reference + "-" + this.instance + "	" + ex.Message + "	" + ex.StackTrace);
				}
			}

			--EditorGUI.indentLevel;
		}

		public void	Export(StringBuilder buffer, int indentLevel = 0)
		{
			if (this.subMatches.Count > 0)
			{
				buffer.Append(SearchResult.ExportIndent, indentLevel);
				buffer.AppendLine(this.nicifiedPath);

				if (this.Open == true)
				{
					for (int j = 0; j < this.subMatches.Count; j++)
						this.subMatches[j].Export(buffer, indentLevel + 1);
				}
				return;
			}

			buffer.Append(SearchResult.ExportIndent, indentLevel);
			buffer.AppendLine(this.nicifiedPath);

			if (this.arrayIndexes.Count > 0)
			{
				if (this.Open == true)
				{
					for (int j = 0; j < this.arrayIndexes.Count; j++)
					{
						buffer.Append(SearchResult.ExportIndent, indentLevel);
						buffer.Append("#");
						buffer.Append(this.arrayIndexes[j]);
						buffer.AppendLine();
					}
				}
			}
		}
	}
}