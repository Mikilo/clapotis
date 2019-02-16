using NGLicenses;
using NGTools;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGShaderFinder
{
	public class NGShaderFinderWindow : EditorWindow, IHasCustomMenu
	{
		public const string	Title = "NG Shader Finder";
		public static Color	TitleColor = new Color(1F, 127F / 255F, 80F / 255F, 1F); // Coral
		public const float	FindButtonWidth = 100F;
		public const float	FindButtonLeftSpacing = 5F;
		public const float	TargetReplaceLabelWidth = 100F;
		public const float	SearchButtonHeight = 24F;
		public const float	SwitchShaderButtonWidth = 24F;
		public const float	ClearButtonWidth = 70F;
		public const float	PingButtonWidth = 75F;
		public const float	Spacing = 4F;

		private const int				MaxShaderReplacements = 3;
		private static readonly string	FreeAdContent = NGShaderFinderWindow.Title + " is restrained to " + NGShaderFinderWindow.MaxShaderReplacements + " replacements at once.\n\nYou can replace many times.";

		private bool	canReplace;
		private Shader	targetShader;
		private Shader	replaceShader;

		private bool			hasResult;
		private List<Material>	results = new List<Material>();
		private string[]		resultsName;
		private string			resultsHeaderLabel;

		private bool	isSearching;
		private Vector2	scrollPosition;

		[MenuItem(Constants.MenuItemPath + NGShaderFinderWindow.Title, priority = Constants.MenuItemPriority + 335), Hotkey(NGShaderFinderWindow.Title)]
		public static void	Open()
		{
			Utility.OpenWindow<NGShaderFinderWindow>(true, NGShaderFinderWindow.Title);
		}

		#region Menu Items
		[MenuItem("CONTEXT/Shader/Search Material using this Shader", priority = 502)]
		private static void	SearchShader(MenuCommand menuCommand)
		{
			NGShaderFinderWindow	window = EditorWindow.GetWindow<NGShaderFinderWindow>(true, NGShaderFinderWindow.Title);

			window.targetShader = menuCommand.context as Shader;
			window.ClearResults();
		}

		[MenuItem("CONTEXT/Material/Search Material using this Shader", priority = 502)]
		private static void	SearchShaderFromMaterial(MenuCommand menuCommand)
		{
			NGShaderFinderWindow	window = EditorWindow.GetWindow<NGShaderFinderWindow>(true, NGShaderFinderWindow.Title);

			if ((menuCommand.context as Material) != null)
				window.targetShader = (menuCommand.context as Material).shader;
			window.ClearResults();
		}

		[MenuItem("Assets/Search Material using this Shader")]
		private static void	SearchAsset(MenuCommand menuCommand)
		{
			NGShaderFinderWindow	window = EditorWindow.GetWindow<NGShaderFinderWindow>(true, NGShaderFinderWindow.Title);

			window.targetShader = Selection.activeObject as Shader;

			if (window.targetShader == null)
				window.targetShader = (Selection.activeObject as Material).shader;
			window.ClearResults();
		}

		[MenuItem("Assets/Search Material using this Shader", true)]
		private static bool	ValidateSearchAsset(MenuCommand menuCommand)
		{
			return Selection.activeObject is Shader || Selection.activeObject is Material;
		}
		#endregion

		protected virtual void	OnEnable()
		{
			Utility.RestoreIcon(this, NGShaderFinderWindow.TitleColor);

			Metrics.UseTool(8); // NGShaderFinder

			NGChangeLogWindow.CheckLatestVersion(NGAssemblyInfo.Name);

			Undo.undoRedoPerformed += this.Repaint;
		}

		protected virtual void	OnDisable()
		{
			Undo.undoRedoPerformed -= this.Repaint;
		}

		protected virtual void	OnGUI()
		{
			FreeLicenseOverlay.First(this, NGAssemblyInfo.Name + " Pro", NGShaderFinderWindow.FreeAdContent);

			Rect	r = this.position;
			r.x = 0F;
			r.y = 0F;
			r.height = NGShaderFinderWindow.SearchButtonHeight;

			EditorGUI.BeginDisabledGroup(this.isSearching);
			{
				r.height = Constants.SingleLineHeight;
				using (LabelWidthRestorer.Get(100F))
				{
					r.width = this.position.width - NGShaderFinderWindow.FindButtonWidth - NGShaderFinderWindow.FindButtonLeftSpacing;
					EditorGUI.BeginChangeCheck();
					Shader	newTarget = EditorGUI.ObjectField(r, "Find Shader", this.targetShader, typeof(Shader), false) as Shader;
					if (EditorGUI.EndChangeCheck() == true)
						this.targetShader = newTarget;
				}
				r.y += r.height + NGShaderFinderWindow.Spacing;

				r.width = NGShaderFinderWindow.TargetReplaceLabelWidth;
				this.canReplace = GUI.Toggle(r, this.canReplace, "Replace With");
				r.x += r.width;

				EditorGUI.BeginDisabledGroup(!this.canReplace);
				{
					r.width = this.position.width - r.x - NGShaderFinderWindow.SwitchShaderButtonWidth - NGShaderFinderWindow.FindButtonWidth - NGShaderFinderWindow.FindButtonLeftSpacing;
					this.replaceShader = EditorGUI.ObjectField(r, this.replaceShader, typeof(Shader), false) as Shader;
				}
				EditorGUI.EndDisabledGroup();

				r.x += r.width;
				r.width = NGShaderFinderWindow.SwitchShaderButtonWidth;
				if (GUI.Button(r, "⇅", GeneralStyles.BigFontToolbarButton) == true)
				{
					Shader tmp = this.replaceShader;
					this.replaceShader = this.targetShader;
					this.targetShader = tmp;
				}

				r.yMin -= r.height + NGShaderFinderWindow.Spacing;
				r.width = NGShaderFinderWindow.FindButtonWidth;
				r.x = this.position.width - NGShaderFinderWindow.FindButtonWidth;

				EditorGUI.BeginDisabledGroup(this.targetShader == null || this.isSearching == true);
				{
					using (BgColorContentRestorer.Get(GeneralStyles.HighlightActionButton))
					{
						if (GUI.Button(r, "Find") == true)
							this.FindReferences();
					}
				}
				EditorGUI.EndDisabledGroup();

				r.y += r.height + NGShaderFinderWindow.Spacing;
			}
			EditorGUI.EndDisabledGroup();

			if (this.canReplace == true)
			{
				EditorGUI.BeginDisabledGroup(this.isSearching == true || this.hasResult == false);
				{
					using (BgColorContentRestorer.Get(GeneralStyles.HighlightActionButton))
					{
						r.x = 0F;
						r.width = this.position.width * .5F;
						if (GUI.Button(r, "Replace") == true)
							this.ReplaceReferences(true);
						r.x += r.width;

						if (GUI.Button(r, "Set all") == true)
							this.ReplaceReferences(false);
						r.y += r.height + NGShaderFinderWindow.Spacing + NGShaderFinderWindow.Spacing;
					}
				}
				EditorGUI.EndDisabledGroup();
			}

			r.x = 0F;
			r.width = this.position.width;

			if (this.hasResult == true)
			{
				r.height = Constants.SingleLineHeight;
				GUI.Box(r, string.Empty, GeneralStyles.Toolbar);
				GUI.Label(r, this.resultsHeaderLabel);

				using (BgColorContentRestorer.Get(GeneralStyles.HighlightResultButton))
				{
					r.xMin = r.xMax - NGShaderFinderWindow.ClearButtonWidth;
					if (GUI.Button(r, "Clear", GeneralStyles.ToolbarButton) == true)
						this.ClearResults();
				}
				r.y += r.height + NGShaderFinderWindow.Spacing;

				r.x = 0F;
				r.width = this.position.width;

				Rect	bodyRect = r;
				bodyRect.height = this.position.height - r.y;

				Rect	viewRect = new Rect(0F, 0F, 0F, (Constants.SingleLineHeight + NGShaderFinderWindow.Spacing) * this.results.Count - NGShaderFinderWindow.Spacing);

				this.scrollPosition = GUI.BeginScrollView(bodyRect, this.scrollPosition, viewRect);
				{
					float	w = r.width - (viewRect.height > bodyRect.height ? 16F : 0F);

					r.y = 0F;
					r.height = Constants.SingleLineHeight;

					for (int i = 0; i < this.results.Count; i++)
					{
						if (r.y + r.height + NGShaderFinderWindow.Spacing <= this.scrollPosition.y)
						{
							r.y += r.height + NGShaderFinderWindow.Spacing;
							continue;
						}

						r.x = 0F;
						r.width = w - NGShaderFinderWindow.PingButtonWidth;

						EditorGUI.BeginChangeCheck();
						Shader	o = EditorGUI.ObjectField(r, this.resultsName[i], this.results[i].shader, typeof(Shader), false) as Shader;
						if (EditorGUI.EndChangeCheck() == true)
						{
							Undo.RecordObject(this.results[i], "Replace Material shader");
							this.results[i].shader = o;
							EditorUtility.SetDirty(this.results[i]);
						}

						r.x += r.width;
						r.width = NGShaderFinderWindow.PingButtonWidth;
						NGEditorGUILayout.PingObject(r, LC.G("Ping"), this.results[i]);
						r.y += r.height + NGShaderFinderWindow.Spacing;

						if (r.y - this.scrollPosition.y > bodyRect.height)
							break;
					}
				}
				GUI.EndScrollView();
			}

			FreeLicenseOverlay.Last(NGAssemblyInfo.Name + " Pro");
		}

		private void	ReplaceReferences(bool replaceOnTarget)
		{
			AssetDatabase.StartAssetEditing();

			int	count = 0;

			for (int i = 0; i < this.results.Count; i++)
			{
				if (replaceOnTarget == false || this.results[i].shader == this.targetShader)
				{
					Undo.RecordObject(this.results[i], "Replace Material shader");
					this.results[i].shader = this.replaceShader;
					++count;
					if (this.CheckMaxShaderReplacements(count) == false)
						break;
				}
			}

			AssetDatabase.StopAssetEditing();

			AssetDatabase.SaveAssets();

			if (count == 0)
				EditorUtility.DisplayDialog(NGShaderFinderWindow.Title, "No reference updated.", "OK");
			else if (count == 1)
				EditorUtility.DisplayDialog(NGShaderFinderWindow.Title, count + " reference updated.", "OK");
			else
				EditorUtility.DisplayDialog(NGShaderFinderWindow.Title, count + " references updated.", "OK");
		}

		private void	ClearResults()
		{
			this.hasResult = false;
			this.results.Clear();
			this.resultsName = null;
		}
		
		private void	PrepareResults()
		{
			this.isSearching = false;
			this.hasResult = true;
			this.resultsHeaderLabel = "Results : " + this.results.Count;

			this.resultsName = new string[this.results.Count];

			for (int i = 0; i < this.resultsName.Length; i++)
				this.resultsName[i] = this.results[i].name;

			this.Repaint();
		}
		
		private void	FindReferences()
		{
			this.ClearResults();

			this.isSearching = true;

			string[]	files = Directory.GetFiles("Assets/", "*.mat", SearchOption.AllDirectories);
			int			max = files.Length;

			for (int i = 0; i < max; i++)
			{
				try
				{
					Material	mat = AssetDatabase.LoadAssetAtPath(files[i], typeof(Material)) as Material;

					if (mat.shader == this.targetShader)
						this.results.Add(mat);
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException("Exception thrown on file \"" + files[i] + "\".", ex);
				}

				EditorUtility.DisplayProgressBar(NGShaderFinderWindow.Title + " - Project (" + (i + 1) + " / " + max + ")", files[i], (float)(i + 1) / (float)max);
			}

			EditorUtility.ClearProgressBar();

			this.PrepareResults();
		}

		private bool	CheckMaxShaderReplacements(int count)
		{
			return NGLicensesManager.Check(count < NGShaderFinderWindow.MaxShaderReplacements, NGAssemblyInfo.Name + " Pro", "Free version does not allow more than " + NGShaderFinderWindow.MaxShaderReplacements + " replacements at once.\n\nUse this feature with moderation. Like wine, with moderation. :@");
		}

		void	IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			Utility.AddNGMenuItems(menu, this, NGShaderFinderWindow.Title, NGAssemblyInfo.WikiURL);
		}
	}
}