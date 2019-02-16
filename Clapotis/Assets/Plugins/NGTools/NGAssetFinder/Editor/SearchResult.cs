using NGLicenses;
using NGTools;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace NGToolsEditor.NGAssetFinder
{
	using UnityEngine;

	internal class SearchResult : IMatchCounter
	{
		private sealed class ViewWarningsPopup : PopupWindowContent
		{
			public const float	Spacing = 2F;
			public const float	MaxWidth = 500F;
			public const float	MaxHeight = 500F;
			public const int	MaxStringLength = 16382;

			public readonly List<string>	files;
			public readonly List<Object>	objects;
			public readonly string[]		objectNames;
			public readonly string[]		stringifiedHierarchies;

			private string	textAreaContent;
			private Vector2	size;
			private Vector2	scrollPosition;
			private float	totalObjectsHeight;
			private double	lastClick;

			public	ViewWarningsPopup(List<string> files, List<Object> objects)
			{
				this.files = files;
				this.objects = objects;

				float	maxWidth = 0F;
				float	height = 0F;

				if (this.files != null)
				{
					StringBuilder	buffer = Utility.GetBuffer();

					for (int i = 0; i < this.files.Count; i++)
					{
						buffer.AppendLine(this.files[i]);
						Utility.content.text = this.files[i];
						float	width = GUI.skin.label.CalcSize(Utility.content).x;

						if (maxWidth < width)
							maxWidth = width;
					}

					buffer.Length -= Environment.NewLine.Length;

					this.textAreaContent = Utility.ReturnBuffer(buffer);
					if (this.textAreaContent.Length <= ViewWarningsPopup.MaxStringLength)
					{
						Utility.content.text = this.textAreaContent;
						Vector2	size = EditorStyles.textArea.CalcSize(Utility.content);

						maxWidth = size.x;
						height = Constants.SingleLineHeight + ViewWarningsPopup.Spacing + size.y;
					}
					else
						height = (Constants.SingleLineHeight + ViewWarningsPopup.Spacing) * (this.files.Count + 1);
				}
				else if (this.objects != null)
				{
					this.objectNames = new string[this.objects.Count];
					this.stringifiedHierarchies = new string[this.objects.Count];

					for (int i = 0; i < this.objects.Count; i++)
					{
						this.objectNames[i] = this.objects[i].ToString();

						string	path = AssetDatabase.GetAssetPath(this.objects[i]);

						if (string.IsNullOrEmpty(path) == true)
						{
							Transform	t = null;

							if (objects[i] is GameObject)
								t = (objects[i] as GameObject).transform;
							else if (objects[i] is Component)
								t = (objects[i] as Component).transform;
							else if (objects[i] is Transform)
								t = objects[i] as Transform;

							if (t != null)
								path = Utility.GetHierarchyStringified(t);
						}

						Utility.content.text = path;
						float	width = GUI.skin.label.CalcSize(Utility.content).x;

						if (maxWidth < width)
							maxWidth = width;

						this.stringifiedHierarchies[i] = path;
					}

					height = Constants.SingleLineHeight + ViewWarningsPopup.Spacing + (this.objects.Count * (Constants.SingleLineHeight + ViewWarningsPopup.Spacing + Constants.SingleLineHeight + ViewWarningsPopup.Spacing));
					this.totalObjectsHeight = height;
				}

				this.size = new Vector2(Mathf.Min(maxWidth, ViewWarningsPopup.MaxWidth), Mathf.Min(height - ViewWarningsPopup.Spacing, ViewWarningsPopup.MaxHeight) + 10F);
			}

			public override Vector2	GetWindowSize()
			{
				return size;
			}

			public override void	OnGUI(Rect r)
			{
				if (this.files != null)
				{
					float	h = r.height;

					r.height = Constants.SingleLineHeight;
					GUI.Label(r, "Files (" + this.files.Count + ")", GeneralStyles.WrapLabel);
					r.y += r.height + ViewWarningsPopup.Spacing;

					if (this.textAreaContent.Length <= ViewWarningsPopup.MaxStringLength)
					{
						r.height = h - r.y;
						EditorGUI.TextArea(r, this.textAreaContent);
					}
					else
					{
						for (int i = 0; i < this.files.Count; i++)
						{
							EditorGUI.TextField(r, this.files[i]);
							r.y += r.height + ViewWarningsPopup.Spacing;
						}
					}
				}
				else if (this.objects != null)
				{
					Rect	viewRect = new Rect(0F, 0F, 0F, this.totalObjectsHeight);

					this.scrollPosition = GUI.BeginScrollView(r, this.scrollPosition, viewRect);
					{
						float	w = r.width - (viewRect.height > r.height ? 16F : 0F);

						r.height = Constants.SingleLineHeight;
						GUI.Label(r, "Objects (" + this.objects.Count + ")", GeneralStyles.WrapLabel);
						r.y += r.height + ViewWarningsPopup.Spacing;

						for (int i = 0; i < this.objects.Count; i++)
						{
							Utility.content.text = this.objectNames[i];
							r.width = GUI.skin.button.CalcSize(Utility.content).x;
							if (GUI.Button(r, Utility.content) == true)
							{
								if (Event.current.button != 0 || this.lastClick + Constants.DoubleClickTime > EditorApplication.timeSinceStartup)
									Selection.activeObject = this.objects[i];
								else
									EditorGUIUtility.PingObject(this.objects[i]);

								this.lastClick = EditorApplication.timeSinceStartup;
							}
							r.y += r.height + ViewWarningsPopup.Spacing;

							r.width = w;
							EditorGUI.TextField(r, this.stringifiedHierarchies[i]);
							r.y += r.height + ViewWarningsPopup.Spacing;
						}
					}
					GUI.EndScrollView();
				}
			}
		}

		public const string	ExportIndent = "  ";
		public const string	AFileHasFailedMessage = "One or more files could not be loaded. Some matches might be missing.";
		public const string	AMemberHasFailedMessage = "One or more properties have thrown exception. Some matches might be missing.";
		public const float	PopupXOffset = -15F;
		public const float	PopupYOffset = 15F;
		public const float	WarningMessageHeight = 24F;
		public const float	WarningSeeButtonWidth = 40F;

		public AssetMatches	workingAssetMatches;
		public int			potentialMatchesCount;
		public int			effectiveMatchesCount;
		public bool			displayResultScenes = true;
		public bool			aborted;
		public double		searchTime;
		public List<string>	aFileHasFailed = new List<string>();
		public List<Object>	aMemberHasFailed = new List<Object>();

		public List<AssetMatches>	matchedInstancesInScene = new List<AssetMatches>();
		public List<AssetMatches>	matchedInstancesInProject = new List<AssetMatches>();
		public List<SceneMatch>		matchedScenes = new List<SceneMatch>();

		public Object			targetAsset;
		public Object			replaceAsset;
		public SearchOptions	searchOptions;
		public SearchAssets		searchAssets;
		public int				searchExtensionsMask;
		public bool				useCache;

		// UI variables
		public string			targetAssetName;
		public Texture2D		targetAssetIcon;
		public string			matchesCount;
		public float			buttonWidth;

		internal int	updatedReferencesCount;
		private bool	replaceOnTarget;

		private NGAssetFinderWindow	window;

		private Vector2	scrollPosition;

		public	SearchResult(NGAssetFinderWindow window)
		{
			this.window = window;
			this.searchTime = Time.realtimeSinceStartup;

			this.targetAsset = this.window.targetAsset;
			this.replaceAsset = this.window.replaceAsset;
			this.searchOptions = this.window.searchOptions;
			this.searchAssets = this.window.searchAssets;
			this.searchExtensionsMask = this.window.searchExtensionsMask;
			this.useCache = this.window.useCache;

			this.targetAssetName = this.targetAsset.name;
			this.targetAssetIcon = Utility.GetIcon(this.targetAsset);
		}

		public string	Export()
		{
			StringBuilder	buffer = Utility.GetBuffer();

			buffer.Append("Find Asset: ");
			if (this.window.targetAsset is Component)
				buffer.Append(this.window.targetAsset.ToString());
			else if (this.window.targetAsset is MonoScript)
				buffer.Append(this.window.targetAsset.name);
			else
				buffer.Append(this.window.targetAsset.name);
			buffer.AppendLine();

			buffer.Append("Date: ");
			buffer.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

			if (Conf.DebugMode != Conf.DebugState.None)
			{
				buffer.Append("Time: ");
				buffer.Append(this.searchTime);
				buffer.AppendLine();

				buffer.Append("Potential Matches: ");
				buffer.Append(this.potentialMatchesCount);
				buffer.AppendLine();
			}

			if (this.effectiveMatchesCount > 0)
			{
				buffer.Append("Matches: ");
				buffer.Append(this.effectiveMatchesCount);
				buffer.AppendLine();
			}
			else
				buffer.AppendLine("Matches: No result");

			if (this.aFileHasFailed.Count > 0)
			{
				buffer.AppendLine();
				buffer.AppendLine(SearchResult.AFileHasFailedMessage);
			}

			if (this.aMemberHasFailed.Count > 0)
			{
				buffer.AppendLine();
				buffer.AppendLine(SearchResult.AMemberHasFailedMessage);
			}

			if (this.matchedInstancesInScene.Count > 0)
			{
				buffer.AppendLine();
				buffer.AppendLine("Scene matches");

				for (int i = 0; i < this.matchedInstancesInScene.Count; i++)
					this.matchedInstancesInScene[i].Export(buffer);
			}

			if (this.matchedInstancesInProject.Count > 0)
			{
				buffer.AppendLine();
				buffer.AppendLine("Project matches");

				for (int i = 0; i < this.matchedScenes.Count; i++)
					this.matchedScenes[i].Export(buffer);

				for (int i = 0; i < this.matchedInstancesInProject.Count; i++)
					this.matchedInstancesInProject[i].Export(buffer);
			}

			buffer.Length -= Environment.NewLine.Length;

			return Utility.ReturnBuffer(buffer);
		}

		public void	Draw(Rect r)
		{
			Rect	viewRect = default(Rect);
			viewRect.height = Constants.SingleLineHeight + NGAssetFinderWindow.Spacing;
			if (Conf.DebugMode != Conf.DebugState.None)
				viewRect.height += Constants.SingleLineHeight + Constants.SingleLineHeight + NGAssetFinderWindow.Spacing + NGAssetFinderWindow.Spacing;

			if (this.aborted == true)
				viewRect.height += SearchResult.WarningMessageHeight + NGAssetFinderWindow.Spacing;

			if (this.aFileHasFailed.Count > 0)
				viewRect.height += SearchResult.WarningMessageHeight + NGAssetFinderWindow.Spacing;

			if (this.aMemberHasFailed.Count > 0)
				viewRect.height += SearchResult.WarningMessageHeight + NGAssetFinderWindow.Spacing;

			if (this.matchedInstancesInScene.Count > 0)
			{
				viewRect.height += Constants.SingleLineHeight + NGAssetFinderWindow.Spacing;
				for (int i = 0; i < this.matchedInstancesInScene.Count; i++)
					viewRect.height += this.matchedInstancesInScene[i].GetHeight();
			}

			if (this.matchedInstancesInProject.Count > 0)
			{
				viewRect.height += Constants.SingleLineHeight + NGAssetFinderWindow.Spacing + (this.matchedScenes.Count) * Constants.SingleLineHeight + NGAssetFinderWindow.Spacing;
				for (int i = 0; i < this.matchedInstancesInProject.Count; i++)
					viewRect.height += this.matchedInstancesInProject[i].GetHeight();
			}

			this.scrollPosition = GUI.BeginScrollView(r, this.scrollPosition, viewRect);
			{
				r.y = 0F;
				r.width -= viewRect.height > r.height ? 16F : 0F;
				r.height = Constants.SingleLineHeight;

				float	width = r.width;

				if (Conf.DebugMode != Conf.DebugState.None)
				{
					EditorGUI.LabelField(r, "Time:", this.searchTime.ToString());
					r.y += r.height + NGAssetFinderWindow.Spacing;
					EditorGUI.LabelField(r, "Potential Matches:", this.potentialMatchesCount.ToCachedString());
					r.y += r.height + NGAssetFinderWindow.Spacing;

					if (this.effectiveMatchesCount > 0)
						EditorGUI.LabelField(r, "Matches:", this.effectiveMatchesCount.ToCachedString());
					else
						EditorGUI.LabelField(r, "Matches:", "No result");
				}
				else
				{
					if (this.effectiveMatchesCount > 0)
						GUI.Label(r, "Matches: " + this.effectiveMatchesCount.ToCachedString());
					else
						GUI.Label(r, "Matches: No result");
				}
				r.y += r.height + NGAssetFinderWindow.Spacing;

				if (this.aborted == true)
				{
					r.width = width;
					r.height = SearchResult.WarningMessageHeight;
					EditorGUI.HelpBox(r, "The search has been aborted.", MessageType.Warning);
					r.y += r.height + NGAssetFinderWindow.Spacing;
				}

				if (this.aFileHasFailed.Count > 0)
				{
					r.width = width - SearchResult.WarningSeeButtonWidth - NGAssetFinderWindow.Margin;
					r.height = SearchResult.WarningMessageHeight;
					EditorGUI.HelpBox(r, SearchResult.AFileHasFailedMessage, MessageType.Warning);

					r.x += r.width;
					r.width = SearchResult.WarningSeeButtonWidth;
					if (GUI.Button(r, "See") == true)
						PopupWindow.Show(new Rect(Event.current.mousePosition + new Vector2(SearchResult.PopupXOffset, SearchResult.PopupYOffset), Vector2.zero), new ViewWarningsPopup(this.aFileHasFailed, null));
					r.y += r.height + NGAssetFinderWindow.Spacing;
				}

				if (this.aMemberHasFailed.Count > 0)
				{
					r.width = width - SearchResult.WarningSeeButtonWidth - NGAssetFinderWindow.Margin;
					r.height = SearchResult.WarningMessageHeight;
					EditorGUI.HelpBox(r, SearchResult.AMemberHasFailedMessage, MessageType.Warning);

					r.x += r.width;
					r.width = SearchResult.WarningSeeButtonWidth;
					if (GUI.Button(r, "See") == true)
						PopupWindow.Show(new Rect(Event.current.mousePosition + new Vector2(SearchResult.PopupXOffset, SearchResult.PopupYOffset), Vector2.zero), new ViewWarningsPopup(null, this.aMemberHasFailed));
					r.y += r.height + NGAssetFinderWindow.Spacing;
				}

				try
				{
					if (Event.current.type == EventType.MouseMove)
						this.window.Repaint();

					r.x = 0F;
					r.width = width;

					if (this.matchedInstancesInScene.Count > 0)
					{
						r.height = Constants.SingleLineHeight;
						EditorGUI.LabelField(r, "Scene matches", GeneralStyles.ToolbarButton);

						if (this.window.canReplace == true)
						{
							using (BgColorContentRestorer.Get(GeneralStyles.HighlightActionButton))
							{
								r.width = width * .34F;
								r.x = width - r.width;
								if (GUI.Button(r, "Replace All") == true)
									this.ReplaceReferences(false);
								r.x = 0F;
								r.width = width;
							}
						}

						r.y += r.height + NGAssetFinderWindow.Spacing;

						for (int i = 0; i < this.matchedInstancesInScene.Count; i++)
						{
							r.height = this.matchedInstancesInScene[i].GetHeight();
							this.matchedInstancesInScene[i].Draw(this.window, r);
							r.y += r.height;
						}
					}

					if (this.matchedInstancesInProject.Count > 0 ||
						this.matchedScenes.Count > 0)
					{
						r.height = Constants.SingleLineHeight;
						EditorGUI.LabelField(r, "Project matches", GeneralStyles.ToolbarButton);

						if (this.window.canReplace == true)
						{
							using (BgColorContentRestorer.Get(GeneralStyles.HighlightActionButton))
							{
								r.width = width * .34F;
								r.x = width - r.width;
								if (GUI.Button(r, "Replace") == true)
									this.ReplaceReferences(false);
								r.x = 0F;
								r.width = width;
							}
						}

						r.y += r.height + NGAssetFinderWindow.Spacing;

						for (int i = 0; i < this.matchedScenes.Count; i++)
						{
							this.matchedScenes[i].Draw(this, this.window, r);
							r.y += r.height;
						}

						for (int i = 0; i < this.matchedInstancesInProject.Count; i++)
						{
							r.height = this.matchedInstancesInProject[i].GetHeight();
							this.matchedInstancesInProject[i].Draw(this.window, r);
							r.y += r.height;
						}
					}
				}
				catch (Exception ex)
				{
					this.window.errorPopup.exception = ex;
				}
			}
			GUI.EndScrollView();
		}

		public void	PrepareResults()
		{
			this.matchesCount = "Matches: " + this.effectiveMatchesCount.ToCachedString();

			Utility.content.text = this.targetAssetName;
			this.buttonWidth = GeneralStyles.VerticalCenterLabel.CalcSize(Utility.content).x;

			Utility.content.text = this.matchesCount;
			float	matchesWidth = GeneralStyles.SmallLabel.CalcSize(Utility.content).x;

			if (this.buttonWidth < matchesWidth)
				this.buttonWidth = matchesWidth;

			foreach (SceneMatch match in this.matchedScenes)
				match.PrepareResults();
			foreach (AssetMatches assetMatches in this.matchedInstancesInScene)
				assetMatches.PrepareResults();
			foreach (AssetMatches assetMatches in this.matchedInstancesInProject)
				assetMatches.PrepareResults();

			if (this.window.debugAnalyzedTypes == true)
			{
				foreach (var item in this.matchedInstancesInScene)
					this.OutputAssetMatches(0, item);
				foreach (var item in this.matchedInstancesInProject)
					this.OutputAssetMatches(0, item);
			}
		}

		public void	ReplaceReferences(bool replaceOnTarget)
		{
			this.updatedReferencesCount = 0;
			this.replaceOnTarget = replaceOnTarget;

			if (this.matchedScenes.Count > 0 &&
				((Event.current.modifiers & Constants.ByPassPromptModifier) == 0 &&
				 EditorUtility.DisplayDialog(NGAssetFinderWindow.Title, "Replacing references in scene is undoable.\nDo you want to continue?", "Replace", "Cancel") == false))
			{
				return;
			}

			try
			{
				AssetDatabase.StartAssetEditing();

				for (int i = 0; i < this.matchedInstancesInScene.Count; i++)
					this.ReplaceAssetMatches(this.matchedInstancesInScene[i]);

				for (int i = 0; i < this.matchedInstancesInProject.Count; i++)
					this.ReplaceAssetMatches(this.matchedInstancesInProject[i]);

				for (int i = 0; i < this.matchedScenes.Count; i++)
					this.matchedScenes[i].ReplaceReferencesInScene(this, this.window);
			}
			catch (MaximumReplacementsReachedException)
			{
			}
			catch (Exception ex)
			{
				this.window.errorPopup.exception = ex;
			}
			finally
			{
				if ((this.searchOptions & SearchOptions.InCurrentScene) != 0)
					EditorSceneManager.MarkAllScenesDirty();

				AssetDatabase.StopAssetEditing();
				AssetDatabase.SaveAssets();
			}

			if (updatedReferencesCount == 0)
				EditorUtility.DisplayDialog(NGAssetFinderWindow.Title, "No reference updated.", "OK");
			else if (updatedReferencesCount == 1)
				EditorUtility.DisplayDialog(NGAssetFinderWindow.Title, updatedReferencesCount + " reference updated.", "OK");
			else
				EditorUtility.DisplayDialog(NGAssetFinderWindow.Title, updatedReferencesCount + " references updated.", "OK");

			this.window.Focus();
		}

		private void	ReplaceAssetMatches(AssetMatches assetMatches)
		{
			this.workingAssetMatches = assetMatches;
			Undo.RecordObject(assetMatches.origin, "Replace Asset");

			for (int j = 0; j < assetMatches.matches.Count; j++)
				this.ReplaceMatch(assetMatches.matches[j]);

			for (int j = 0; j < assetMatches.children.Count; j++)
				this.ReplaceAssetMatches(assetMatches.children[j]);

			EditorUtility.SetDirty(assetMatches.origin);
		}

		private void	ReplaceMatch(Match match)
		{
			if (match.subMatches.Count > 0)
			{
				for (int i = 0; i < match.subMatches.Count; i++)
					this.ReplaceMatch(match.subMatches[i]);
			}
			else if (match.arrayIndexes.Count > 0)
			{
				object	rawArray = match.Value;

				if (rawArray != null)
				{
					ICollectionModifier	collectionModifier = null;
					TypeFinder			finder = null;
					int					j = 0;

					for (; j < this.window.typeFinders.array.Length; j++)
					{
						if (this.window.typeFinders.array[j].CanFind(match.type) == true)
						{
							finder = this.window.typeFinders.array[j];
							this.window.typeFinders.BringToTop(j);
							break;
						}
					}

					if (finder == null)
						collectionModifier = NGTools.Utility.GetCollectionModifier(rawArray);

					try
					{
						for (int i = 0; i < match.arrayIndexes.Count; i++)
						{
							if (finder != null)
							{
								if (this.CheckTypeCompatibility(this.replaceOnTarget, finder.GetType(match.type), finder.Get(match.type, match, match.arrayIndexes[i])) == true)
								{
									finder.Set(match.type, this.window.replaceAsset, match, match.arrayIndexes[i]);
									++this.updatedReferencesCount;
									if (this.CheckMaxAssetReplacements(this.updatedReferencesCount) == false)
										throw new MaximumReplacementsReachedException();
								}
							}
							else if (this.CheckTypeCompatibility(this.replaceOnTarget, collectionModifier.SubType, collectionModifier.Get(match.arrayIndexes[i])) == true)
							{
								collectionModifier.Set(match.arrayIndexes[i], this.window.replaceAsset);
								++this.updatedReferencesCount;
								if (this.CheckMaxAssetReplacements(this.updatedReferencesCount) == false)
									throw new MaximumReplacementsReachedException();
							}
						}
					}
					finally
					{
						NGTools.Utility.ReturnCollectionModifier(collectionModifier);
					}
				}
			}
			else if ((this.workingAssetMatches.origin is MonoScript) == false &&
					 this.CheckTypeCompatibility(this.replaceOnTarget, match.type, match.Value) == true)
			{
				match.Value = this.window.replaceAsset;
				++this.updatedReferencesCount;
				if (this.CheckMaxAssetReplacements(this.updatedReferencesCount) == false)
					throw new MaximumReplacementsReachedException();
			}
		}

		private bool	CheckTypeCompatibility(bool replaceOnTarget, Type type, object instance)
		{
			return ((replaceOnTarget == true && Object.ReferenceEquals(instance, this.window.targetAsset) == true) ||
					(replaceOnTarget == false && Object.ReferenceEquals(instance, this.window.replaceAsset) == false)) &&
				   (this.window.replaceAsset == null || type.IsAssignableFrom(this.window.replaceAsset.GetType()) == true);
		}

		private void	OutputAssetMatches(int indent, AssetMatches assetMatches)
		{
			if (assetMatches.type == AssetMatches.Type.Reference &&
				assetMatches.matches.Count == 0 &&
				assetMatches.children.Count == 0)
			{
				return;
			}

			InternalNGDebug.Log(new string(' ', indent << 1) + assetMatches.origin);

			for (int i = 0; i < assetMatches.matches.Count; i++)
				this.OutputMatch(indent + 1, assetMatches.matches[i]);

			for (int i = 0; i < assetMatches.children.Count; i++)
				this.OutputAssetMatches(indent + 1, assetMatches.children[i]);
		}

		private void	OutputMatch(int indent, Match match)
		{
			InternalNGDebug.Log(new string(' ', indent << 1) + match.path);

			for (int i = 0; i < match.arrayIndexes.Count; i++)
				InternalNGDebug.Log(new string(' ', (indent + 1) << 1) + match.arrayIndexes[i]);

			for (int i = 0; i < match.subMatches.Count; i++)
				this.OutputMatch(indent + 1, match.subMatches[i]);
		}

		private bool	CheckMaxAssetReplacements(int count)
		{
			return NGLicensesManager.Check(count < NGAssetFinderWindow.MaxAssetReplacements, NGAssemblyInfo.Name + " Pro", "Free version does not allow more than " + NGAssetFinderWindow.MaxAssetReplacements + " replacements at once.\n\nUse this feature with moderation. Like beer, with moderation. ;D");
		}

		void	IMatchCounter.AddPotentialMatchCounter(int n)
		{
			this.potentialMatchesCount += n;
		}

		void	IMatchCounter.AddEffectiveMatchCounter(int n)
		{
			this.effectiveMatchesCount += n;
		}
	}
}