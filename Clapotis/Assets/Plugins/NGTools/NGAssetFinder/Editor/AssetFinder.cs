using NGLicenses;
using NGTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace NGToolsEditor.NGAssetFinder
{
	using UnityEngine;

	internal class AssetFinder
	{
		private class TypeAnalyzerCache
		{
			public SearchOptions		options;
			public Type					targetType;
			public BindingFlags			searchFlags;
			public Dictionary<Type, ContainerType>	analyzedTypes = new Dictionary<Type, ContainerType>(1024);
			public List<ContainerType>				incompleteContainers = new List<ContainerType>(4);

			public	TypeAnalyzerCache(SearchOptions options, Type targetType, BindingFlags searchFlags)
			{
				this.options = options;
				this.targetType = targetType;
				this.searchFlags = searchFlags;

				this.analyzedTypes.Add(typeof(Object), new ContainerType(typeof(Object), ParseResult.HasObject));
				this.analyzedTypes.Add(typeof(GameObject), new ContainerType(typeof(GameObject), ParseResult.HasObject));
				this.analyzedTypes.Add(typeof(Component), new ContainerType(typeof(Component), ParseResult.HasObject));
				this.analyzedTypes.Add(typeof(Behaviour), new ContainerType(typeof(Behaviour), ParseResult.HasObject));
				//this.analyzedTypes.Add(typeof(Transform), new ContainerType(typeof(Transform), ParseResult.MustDiscard));
				//this.analyzedTypes.Add(typeof(RectTransform), new ContainerType(typeof(RectTransform), ParseResult.MustDiscard));
				this.analyzedTypes.Add(typeof(MonoBehaviour), new ContainerType(typeof(MonoBehaviour), ParseResult.HasObject));
				this.analyzedTypes.Add(typeof(ScriptableObject), new ContainerType(typeof(ScriptableObject), ParseResult.HasObject));
				this.analyzedTypes.Add(typeof(Vector2), new ContainerType(typeof(Vector2), ParseResult.MustDiscard));
				this.analyzedTypes.Add(typeof(Vector3), new ContainerType(typeof(Vector3), ParseResult.MustDiscard));
				this.analyzedTypes.Add(typeof(Vector4), new ContainerType(typeof(Vector4), ParseResult.MustDiscard));
				this.analyzedTypes.Add(typeof(Quaternion), new ContainerType(typeof(Quaternion), ParseResult.MustDiscard));
				this.analyzedTypes.Add(typeof(Rect), new ContainerType(typeof(Rect), ParseResult.MustDiscard));
				this.analyzedTypes.Add(typeof(Bounds), new ContainerType(typeof(Bounds), ParseResult.MustDiscard));
				this.analyzedTypes.Add(typeof(Color), new ContainerType(typeof(Color), ParseResult.MustDiscard));
				this.analyzedTypes.Add(typeof(Color32), new ContainerType(typeof(Color32), ParseResult.MustDiscard));
				this.analyzedTypes.Add(typeof(DateTime), new ContainerType(typeof(DateTime), ParseResult.MustDiscard));
			}
		}

		private List<TypeAnalyzerCache>						cachedTypeAnalyzers = new List<TypeAnalyzerCache>(4);
		private Dictionary<Type, TypeMembersExclusion[]>	cachedTME = new Dictionary<Type, TypeMembersExclusion[]>(1024);

		private TypeAnalyzerCache					currentTypeAnalyzer;
		private List<Object>						processedAssets = new List<Object>(4048);
		private Dictionary<string, AssetMatches>	scenePrefabMatches = new Dictionary<string, AssetMatches>(128);
		private List<Component>						reuseComponents = new List<Component>(32);
		private List<TypeMembersExclusion>			reuseTME = new List<TypeMembersExclusion>(8);
		private string								realLocalIdentifier = null;

		private List<GameObject>	reuseRoots = new List<GameObject>(16);
		private List<GameObject>	roots = new List<GameObject>(16);

		private string	progressBarTitle;
		private string	progressBarContent;
		private float	progressBarRate;

		public List<ContainerType>	cyclicReferencesContainers = new List<ContainerType>();
		public Stack<ContainerType>	pendingCyclicContainers = new Stack<ContainerType>();

		internal NGAssetFinderWindow	window;

		public	AssetFinder(NGAssetFinderWindow window)
		{
			this.window = window;
		}

		public SearchResult	ScanAssets()
		{
			this.Clear();

			SearchResult	result = new SearchResult(this.window);
			double			searchTime = EditorApplication.timeSinceStartup;

			try
			{
				this.currentTypeAnalyzer = null;

				for (int i = 0; i < this.cachedTypeAnalyzers.Count; i++)
				{
					if (this.cachedTypeAnalyzers[i].options == (this.window.searchOptions & (SearchOptions.NonPublic | SearchOptions.Property | SearchOptions.SerializeField)) &&
						this.cachedTypeAnalyzers[i].targetType == typeof(Object) &&
						this.cachedTypeAnalyzers[i].searchFlags == this.window.fieldSearchFlags)
					{
						this.currentTypeAnalyzer = this.cachedTypeAnalyzers[i];
						break;
					}
				}

				if (this.currentTypeAnalyzer == null)
				{
					this.currentTypeAnalyzer = new TypeAnalyzerCache((this.window.searchOptions & (SearchOptions.NonPublic | SearchOptions.Property | SearchOptions.SerializeField)),
																	 typeof(Object),
																	 this.window.fieldSearchFlags);
					this.cachedTypeAnalyzers.Add(this.currentTypeAnalyzer);
				}

				if ((this.window.searchOptions & SearchOptions.InCurrentScene) != 0)
				{
					this.roots.Clear();

					if (this.window.sceneFilters.Count > 0)
					{
						for (int k = 0; k < EditorSceneManager.sceneCount; k++)
						{
							Scene	scene = EditorSceneManager.GetSceneAt(k);
							string	guid = AssetDatabase.AssetPathToGUID(scene.path);

							for (int i = 0; i < this.window.sceneFilters.Count; i++)
							{
								NGAssetFinderWindow.SceneGameObjectFilters	filters = this.window.sceneFilters[i];

								if (filters.sceneGUID == guid)
								{
									for (int j = 0; j < filters.filters.Count; j++)
									{
										if (filters.filters[j].active == true &&
											filters.filters[j].type == Filter.Type.Inclusive &&
											filters.filters[j].GameObject != null)
										{
											int	l = 0;

											for (; l < this.roots.Count; l++)
											{
												if (this.roots[l].transform.IsChildOf(filters.filters[j].GameObject.transform) == true)
												{
													this.roots.RemoveAt(l);
													this.roots.Insert(l, filters.filters[j].GameObject);
													this.roots.Add(this.roots[l]);
													break;
												}
											}

											if (l == this.roots.Count)
												this.roots.Add(filters.filters[j].GameObject);
										}
									}

									break;
								}
							}
						}
					}

					if (this.roots.Count == 0)
					{
						for (int l = 0; l < SceneManager.sceneCount; ++l)
						{
							Scene	scene = EditorSceneManager.GetSceneAt(l);
							string	guid = AssetDatabase.AssetPathToGUID(scene.path);

							for (int i = 0; i < this.window.sceneFilters.Count; i++)
							{
								NGAssetFinderWindow.SceneGameObjectFilters	filters = this.window.sceneFilters[i];

								if (filters.sceneGUID == guid)
								{
									scene.GetRootGameObjects(this.reuseRoots);

									if (filters.filters.Count > 0)
									{
										for (int k = 0; k < this.reuseRoots.Count; k++)
										{
											for (int j = 0; j < filters.filters.Count; j++)
											{
												if (filters.filters[j].active == true &&
													filters.filters[j].type == Filter.Type.Exclusive &&
													filters.filters[j].GameObject == this.reuseRoots[k])
												{
													this.reuseRoots.RemoveAt(k--);
												}
											}
										}
									}

									this.roots.AddRange(this.reuseRoots);
									break;
								}
							}
						}
					}

					for (int i = 0; i < this.roots.Count; ++i)
					{
						this.progressBarTitle = NGAssetFinderWindow.Title + " - Scenes (" + i + " / " + this.roots.Count + ")";
						this.progressBarContent = this.roots[i].name;
						this.progressBarRate = (float)i / (float)this.roots.Count;

						if (EditorUtility.DisplayCancelableProgressBar(this.progressBarTitle + " (" + this.currentTypeAnalyzer.analyzedTypes.Count + " types analyzed)", this.progressBarContent, this.progressBarRate) == true)
							throw new BreakException();

						try
						{
							this.BrowseGameObject(result, null, this.roots[i].transform);
						}
						catch (BreakException)
						{
							throw;
						}
						catch (Exception ex)
						{
							this.window.errorPopup.exception = ex;
							InternalNGDebug.LogException(ex);
						}
					}
				}

				if ((this.window.searchOptions & SearchOptions.InProject) != 0)
				{
					bool	useCache = NGLicensesManager.IsPro(NGAssemblyInfo.Name + " Pro") == true && this.window.useCache == true;

					// Build cache before to display a coherent progress bar process.
					if (useCache == true)
						AssetFinderCache.CacheProjectReferences();

					this.progressBarTitle = NGAssetFinderWindow.Title + " - Project";
					this.progressBarContent = "Fetching files...";
					this.progressBarRate = 0F;

					if (EditorUtility.DisplayCancelableProgressBar(this.progressBarTitle, this.progressBarContent, this.progressBarRate) == true)
						throw new BreakException();

					List<string>	paths = new List<string>();

					if (useCache == true)
					{
						List<string>	cache;
						string			assetPath = AssetDatabase.GetAssetPath(this.window.targetAsset);
						string			targetID = AssetDatabase.AssetPathToGUID(assetPath);

						if ((this.window.searchOptions & SearchOptions.ByInstance) != 0 && string.IsNullOrEmpty(targetID) == false)
						{
							if (AssetFinderCache.usages.TryGetValue(targetID, out cache) == true)
								paths.AddRange(cache);

							if (paths.Contains(assetPath) == false)
								paths.Add(assetPath);
						}

						if ((this.window.searchOptions & SearchOptions.ByComponentType) != 0)
						{
							if (this.window.targetAsset is MonoBehaviour || this.window.targetAsset is MonoScript)
							{
								MonoScript	script = this.window.targetAsset as MonoScript;
								if (script == null)
									script = MonoScript.FromMonoBehaviour(this.window.targetAsset as MonoBehaviour);

								targetID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(script.GetInstanceID()));

								if (AssetFinderCache.usages.TryGetValue(targetID, out cache) == true)
								{
									for (int i = 0; i < cache.Count; i++)
									{
										if (paths.Contains(cache[i]) == false)
											paths.Add(cache[i]);
									}
								}
							}
							else
							{
								// Unfortunately, Unity's Component can not be put in cache.
								paths.Clear();
								paths.AddRange(AssetDatabase.GetAllAssetPaths());
							}
						}
					}
					else
						paths.AddRange(AssetDatabase.GetAllAssetPaths());

					bool	allAssetExtensionsActive = true;

					for (int i = 0; i < NGAssetFinderWindow.AssetsExtensions.Length; i++)
					{
						if ((this.window.searchExtensionsMask & (1 << i)) == 0)
						{
							allAssetExtensionsActive = false;
							break;
						}
					}

					bool	skipFiltering = allAssetExtensionsActive == true &&
						((this.window.searchAssets & SearchAssets.Prefab) != 0) &&
						((this.window.searchAssets & SearchAssets.Scene) != 0) &&
						((this.window.searchAssets & SearchAssets.Asset) != 0);

					for (int i = 0; i < paths.Count; i++)
					{
						if ((i % 18) == 0)
						{
							this.progressBarTitle = NGAssetFinderWindow.Title + " - Project (" + (i + 1) + " / " + paths.Count + ")";
							this.progressBarContent = "Filtering...";
							this.progressBarRate = (float)(i + 1) / (float)paths.Count;

							if (EditorUtility.DisplayCancelableProgressBar(this.progressBarTitle, this.progressBarContent, this.progressBarRate) == true)
								throw new BreakException();
						}

						if ((paths[i].StartsWith("Assets/") == false && paths[i].StartsWith("ProjectSettings/") == false && paths[i].StartsWith("Library/") == false) || Directory.Exists(paths[i]) == true)
						{
							paths.RemoveAt(i--);
							continue;
						}

						if (skipFiltering == true)
							continue;

						if (paths[i].EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) == true)
						{
							if ((this.window.searchAssets & SearchAssets.Prefab) == 0)
								paths.RemoveAt(i--);
							continue;
						}

						if (paths[i].EndsWith(".unity", StringComparison.OrdinalIgnoreCase) == true)
						{
							if ((this.window.searchAssets & SearchAssets.Scene) == 0)
								paths.RemoveAt(i--);
							continue;
						}

						if ((this.window.searchAssets & SearchAssets.Asset) != 0)
						{
							if (allAssetExtensionsActive == true)
								continue;

							int	j = 0;

							for (; j < NGAssetFinderWindow.AssetsExtensions.Length; j++)
							{
								// Break at last, which is "others" and it accepts anything.
								if (j == NGAssetFinderWindow.AssetsExtensions.Length - 1)
								{
									if ((this.window.searchExtensionsMask & (1 << j)) == 0)
										paths.RemoveAt(i--);
									break;
								}

								int	k = 0;

								for (; k < NGAssetFinderWindow.AssetsExtensions[j].extensions.Length; k++)
								{
									int	pathLength = paths[i].Length;
									int	extLength = NGAssetFinderWindow.AssetsExtensions[j].extensions[k].Length;

									if (pathLength >= extLength)
									{
										pathLength -= extLength;

										int	l = 0;

										for (; l < extLength; l++)
										{
											char	c = NGAssetFinderWindow.AssetsExtensions[j].extensions[k][l];

											if (paths[i][pathLength + l] != c)
											{
												if (c >= 'a' && c <= 'z')
												{
													if (paths[i][pathLength + l] != c + ('A' - 'a'))
														break;
												}
												else if (c >= 'A' && c <= 'Z')
												{
													if (paths[i][pathLength + l] != c + ('a' - 'A'))
														break;
												}
												else
													break;
											}
										}

										if (l == extLength)
											break;
									}
								}

								if (k < NGAssetFinderWindow.AssetsExtensions[j].extensions.Length)
								{
									if ((this.window.searchExtensionsMask & (1 << j)) == 0)
										paths.RemoveAt(i--);
									break;
								}
							}
						}
						else
							paths.RemoveAt(i--);
					}

					if (this.window.projectFilters.filters.Count > 0)
					{
						for (int i = 0; i < paths.Count; i++)
						{
							if (this.window.projectFilters.filters.IsPathFilteredIn(paths[i]) == false)
								paths.RemoveAt(i--);
						}
					}

					this.ProcessFolder(result, paths);
				}
			}
			catch (BreakException)
			{
				result.aborted = true;
			}
			finally
			{
				if (this.window.debugAnalyzedTypes == true)
				{
					Debug.Log("Working data:");
					foreach (var pair in this.currentTypeAnalyzer.analyzedTypes)
					{
						if (pair.Value.containObject == ParseResult.HasObject)
						{
							Debug.Log("Type " + pair.Key);
							for (int j = 0; j < pair.Value.fields.Count; j++)
								Debug.Log("	F " + pair.Value.fields[j].Name);
							for (int j = 0; j < pair.Value.properties.Count; j++)
								Debug.Log("	P " + pair.Value.properties[j].Name);
						}
					}
				}

				EditorUtility.ClearProgressBar();
				result.PrepareResults();
				this.window.Repaint();
			}

			result.searchTime = EditorApplication.timeSinceStartup - searchTime;

			return result;
		}

		private void	Clear()
		{
			this.processedAssets.Clear();
			this.scenePrefabMatches.Clear();
			this.realLocalIdentifier = null;
		}

		private void	ProcessFolder(SearchResult result, List<string> files)
		{
			int	totalFiles = files.Count;

			for (int i = 0; i < files.Count; i++)
			{
				this.progressBarTitle = NGAssetFinderWindow.Title + " - Project (" + (i + 1) + " / " + totalFiles + ")";
				this.progressBarContent = files[i];
				this.progressBarRate = (float)(i + 1) / (float)totalFiles;

				if (EditorUtility.DisplayCancelableProgressBar(this.progressBarTitle + " (" + this.currentTypeAnalyzer.analyzedTypes.Count + " types analyzed)", this.progressBarContent, this.progressBarRate) == true)
					throw new BreakException();

				try
				{
					Object	mainAsset = AssetDatabase.LoadMainAssetAtPath(files[i]);
					if (mainAsset == null)
					{
						Object[]	rawObjects = AssetDatabase.LoadAllAssetsAtPath(files[i]);
						if (rawObjects.Length > 0)
							mainAsset = rawObjects[0];
					}

					if (mainAsset == null)
					{
						result.aFileHasFailed.Add(files[i]);
						InternalNGDebug.InternalLog("File \"" + files[i] + "\" could not be loaded, might be corrupted or contain broken references.");
						continue;
					}

					AssetMatches	assetMatches = new AssetMatches(mainAsset);

					if (files[i].EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) == true)
					{
						GameObject	prefab = mainAsset as GameObject;
						if (prefab != null)
							this.BrowseGameObject(result, assetMatches, prefab.transform);
					}
					else if (files[i].EndsWith(".unity", StringComparison.OrdinalIgnoreCase) == true)
						this.BrowseScene(result, files[i], mainAsset);
					else
					{
						Object[]	assets = Utility.SafeLoadAllAssetsAtPath(files[i]);

						if (assets.Length == 0)
							assets = new Object[] { mainAsset };

						for (int j = 0; j < assets.Length; j++)
						{
							AssetMatches	subMatches = assetMatches;

							if (assets[j] != mainAsset)
								subMatches = new AssetMatches(assets[j]);

							this.ParseObject(result, subMatches, assets[j]);

							if (assets[j] != mainAsset)
							{
								if (subMatches.children.Count > 0 || subMatches.matches.Count > 0)
									assetMatches.children.Add(subMatches);
							}
						}
					}

					if (assetMatches.matches.Count > 0 || assetMatches.children.Count > 0)
						result.matchedInstancesInProject.Add(assetMatches);
				}
				catch (BreakException)
				{
					throw;
				}
				catch (Exception ex)
				{
					this.window.errorPopup.exception = ex;
					InternalNGDebug.LogException("Exception thrown on file \"" + files[i] + "\".", ex);
				}
			}
		}

		private void	ParseObject(SearchResult result, AssetMatches assetMatches, Object asset, int deep = 0)
		{
			if (this.SkipType(asset) == true || this.processedAssets.Contains(asset) == true)
				return;

			for (int i = 0; i < this.window.objectFinders.array.Length; i++)
			{
				if (this.window.objectFinders.array[i].CanFind(asset) == true)
				{
					this.window.objectFinders.array[i].Find(assetMatches, asset, this, result);
					this.window.objectFinders.BringToTop(i);
					return;
				}
			}

			if (asset.name == "Deprecated EditorExtensionImpl")
				return;

			Stack<SerializedProperty>	potentialSubProperties = new Stack<SerializedProperty>();

			this.processedAssets.Add(asset);

			SerializedObject	so = new SerializedObject(asset);
			SerializedProperty	property = so.GetIterator();

			property.Next(true);

			while (property.NextVisible(property.propertyType == SerializedPropertyType.Generic))
			{
				if (property.propertyType == SerializedPropertyType.ObjectReference)
				{
					++result.potentialMatchesCount;
					if (property.objectReferenceValue == this.window.targetAsset)
					{
						assetMatches.AggregateMatch(asset, property.propertyPath);
						++result.effectiveMatchesCount;
					}

					if (deep == 0 &&
						property.objectReferenceValue != null &&
						property.objectReferenceValue != this.window.targetAsset &&
						property.propertyPath != "m_Script")
					{
						potentialSubProperties.Push(property.Copy());
					}
				}
			}

			while (potentialSubProperties.Count > 0)
			{
				property = potentialSubProperties.Pop();

				AssetMatches	subMatches = new AssetMatches(property.objectReferenceValue);

				this.ParseObject(result, subMatches, property.objectReferenceValue, deep + 1);

				if (subMatches.children.Count > 0 || subMatches.matches.Count > 0)
					assetMatches.children.Add(subMatches);
			}
		}

		private bool	SkipType(Object asset)
		{
			return asset is Transform || asset is Texture;
		}

		private void	BrowseScene(SearchResult result, string sceneFile, Object mainAsset)
		{
			if (EditorSettings.serializationMode != SerializationMode.ForceText)
				return;

			string			assetPath;
			MonoBehaviour	monoBehaviour = this.window.targetAsset as MonoBehaviour;
			MonoScript		script = null;
			string			guid;

			if (monoBehaviour != null)
				script = MonoScript.FromMonoBehaviour(monoBehaviour);

			if (script != null)
			{
				assetPath = AssetDatabase.GetAssetPath(script);
				guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this.window.targetAsset));
			}
			else
			{
				assetPath = AssetDatabase.GetAssetPath(this.window.targetAsset);
				guid = AssetDatabase.AssetPathToGUID(assetPath);
			}

			if (assetPath.Contains("Resources/unity_builtin_extra") == true)
				return;

			if (this.realLocalIdentifier == null)
				this.realLocalIdentifier = Utility.GetLocalIdentifierFromObject(this.window.targetAsset);

			SceneMatch	match = new SceneMatch(mainAsset, sceneFile);
			string		id = null;
			string		componentTypeId = "11500000, guid: " + guid;
			bool		searchingComponentType = (this.window.searchOptions & SearchOptions.ByComponentType) != 0 && (this.window.targetAsset is Component || this.window.targetAsset is MonoScript);
			bool		searchingPrefab = AssetDatabase.LoadMainAssetAtPath(assetPath) == this.window.targetAsset;

			if (string.IsNullOrEmpty(this.realLocalIdentifier) == false)
				id = this.realLocalIdentifier + ", guid: " + guid;

			using (FileStream fs = File.Open(sceneFile, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (BufferedStream bs = new BufferedStream(fs))
			using (StreamReader sr = new StreamReader(bs))
			{
				string	line;

				while ((line = sr.ReadLine()) != null)
				{
					if (line.Length < 11 + 8 + 32 + 1) // {fileID: , guid: }
						continue;

					int	position = line.IndexOf(" {fileID: ");
					if (position != -1)
					{
						++result.potentialMatchesCount;

						if (id != null && (this.window.searchOptions & SearchOptions.ByInstance) != 0)
						{
							// References of prefabs.
							if (searchingPrefab == true && line.StartsWith("  m_ParentPrefab: {fileID: ") == true)
							{
								if (line.IndexOf(guid) != -1)
								{
									++match.prefabCount;
									continue;
								}
							}
							// Modifications of prefabs.
							else if (line.StartsWith("    - target: {fileID: ") == true)
							{
								if (line.IndexOf(id, position + " {fileID: ".Length - 1) != -1)
								{
									++match.prefabModificationCount;
									continue;
								}
							}
							// References in script.
							else if (line.StartsWith("  m_Script: {fileID: ") == false)
							{
								if (line.IndexOf(id, position + " {fileID: ".Length - 1) != -1)
								{
									++match.count;
									continue;
								}
							}
						}

						// References of scripts.
						if (searchingComponentType == true && line.StartsWith("  m_Script: {fileID: ") == true && line.IndexOf(componentTypeId, "  m_Script: {fileID: ".Length) != -1)
							++match.count;
					}
				}
			}

			if (match.count > 0 || match.prefabCount > 0 || match.prefabModificationCount > 0)
			{
				result.effectiveMatchesCount += match.count + match.prefabCount + match.prefabModificationCount;
				result.matchedScenes.Add(match);
			}
		}

		internal void	BrowseGameObject(SearchResult result, AssetMatches parent, Transform transform, bool skipComponents = false)
		{
			if (this.processedAssets.Contains(transform) == true)
				return;

			this.processedAssets.Add(transform);

			AssetMatches	assetMatches;

			if (parent == null || parent.origin != transform.gameObject)
			{
				assetMatches = new AssetMatches(transform.gameObject);

				if (parent != null)
					parent.children.Add(assetMatches);
				else
					result.matchedInstancesInScene.Add(assetMatches);
			}
			else
				assetMatches = parent;

			//if (this.targetAsset == transform.gameObject)
			//{
			//	Match	match = new Match();

			//	match.path.Add(transform.gameObject.name);
			//	assetMatches.matches.Add(match);
			//}

			if (skipComponents == false)
			{
				transform.gameObject.GetComponents<Component>(this.reuseComponents);

				for (int i = 0; i < this.reuseComponents.Count; i++)
				{
					if (this.reuseComponents[i] != null)
						this.BrowseObject(result, assetMatches, this.reuseComponents[i], true);
				}
			}

			NGAssetFinderWindow.SceneGameObjectFilters	sceneFilters = null;
			string	sceneGUID = AssetDatabase.AssetPathToGUID(transform.gameObject.scene.path);

			if (string.IsNullOrEmpty(sceneGUID) == false)
			{
				for (int i = 0; i < this.window.sceneFilters.Count; i++)
				{
					if (this.window.sceneFilters[i].sceneGUID == sceneGUID)
					{
						sceneFilters = this.window.sceneFilters[i];
						break;
					}
				}
			}

			for (int i = 0; i < transform.childCount; i++)
			{
				Transform	child = transform.GetChild(i);

				if (sceneFilters != null && sceneFilters.filters.Count > 0)
				{
					int	k = 0;

					for (; k < sceneFilters.filters.Count; k++)
					{
						if (sceneFilters.filters[k].active == true && sceneFilters.filters[k].type == Filter.Type.Exclusive && sceneFilters.filters[k].GameObject == child.gameObject)
							break;
					}

					if (k < sceneFilters.filters.Count)
						continue;
				}

				this.BrowseGameObject(result, assetMatches, child, skipComponents);
			}

			if (assetMatches.matches.Count == 0 &&
				assetMatches.children.Count == 0)
			{
				if (parent == null || parent.origin != transform.gameObject)
				{
					if (parent != null)
						parent.children.Remove(assetMatches);
					else
						result.matchedInstancesInScene.Remove(assetMatches);
				}
			}
		}

		private void	BrowseObject(SearchResult result, AssetMatches parent, Object instance, bool isComponent = false)
		{
			if (((this.window.searchOptions & SearchOptions.ByInstance) != 0 && this.window.targetAsset == instance) ||
				((this.window.searchOptions & SearchOptions.ByComponentType) != 0 && (this.window.targetAsset is Component || this.window.targetAsset is MonoScript) && this.window.targetType == instance.GetType()))
			{
				AssetMatches	assetMatches = new AssetMatches(instance);

				assetMatches.type = AssetMatches.Type.Component;

				if (parent != null)
					parent.children.Add(assetMatches);
				else
					result.matchedInstancesInScene.Add(assetMatches);

				++result.potentialMatchesCount;
				++result.effectiveMatchesCount;
			}

			if ((this.window.searchOptions & SearchOptions.ByInstance) != 0)
			{
				// Can't look for an scene asset in the Project.
				if ((this.window.searchOptions & (SearchOptions.InProject | SearchOptions.InCurrentScene)) == SearchOptions.InProject &&
					string.IsNullOrEmpty(AssetDatabase.GetAssetPath(this.window.targetAsset)) == true)
				{
					return;
				}

				AssetMatches	componentMatches = new AssetMatches(instance);

				if (isComponent == true)
					componentMatches.type = AssetMatches.Type.Component;

				this.ExtractClass(result, componentMatches, null, componentMatches.origin.GetType(), componentMatches.origin);

				if (componentMatches.children.Count > 0 || componentMatches.matches.Count > 0)
				{
					if (parent != null)
						parent.children.Add(componentMatches);
					else
						result.matchedInstancesInScene.Add(componentMatches);
				}
			}
		}

		private ParseResult	ParseType(Type type)
		{
			// TODO Implement cache of Type Analyzer?
			if (type.IsPrimitive == true || type.IsEnum == true || type == typeof(Decimal) || type == typeof(String) || typeof(Delegate).IsAssignableFrom(type) == true)
				return ParseResult.MustDiscard;

			ContainerType	container;

			if (this.currentTypeAnalyzer.analyzedTypes.TryGetValue(type, out container) == true)
				return container.containObject;

			if (EditorUtility.DisplayCancelableProgressBar(this.progressBarTitle + " (" + this.currentTypeAnalyzer.analyzedTypes.Count + " types analyzed)", this.progressBarContent, this.progressBarRate) == true)
				throw new BreakException();

			if (typeof(IList).IsAssignableFrom(type) == true)
			{
				ContainerType	containerType = new ContainerType(type, ParseResult.Unsure);
				Type			subType = Utility.GetArraySubType(type);

				this.currentTypeAnalyzer.analyzedTypes.Add(type, containerType);

				try
				{
					if (subType == null || // In case we don't know, assume it can.
						subType == typeof(object) || // Assume that there must be a Type somewhere having the target Type.
						subType == typeof(Object))
					{
						containerType.containObject = ParseResult.HasObject;
					}
					else if (subType.IsPrimitive == true || subType.IsEnum == true || subType == typeof(Decimal) || subType == typeof(String) || typeof(Delegate).IsAssignableFrom(subType) == true)
						containerType.containObject = ParseResult.MustDiscard;
					else
					{
						containerType.containObject = this.ParseType(subType);

						if (containerType.containObject != ParseResult.HasObject &&
							subType.IsUnityArray() == false &&
							(subType.IsInterface == true || subType.IsStruct() == false))
						{
							foreach (Type derivedSubType in Utility.EachAllAssignableFrom(subType))
							{
								switch (this.ParseType(derivedSubType))
								{
									case ParseResult.HasObject:
										containerType.containObject = ParseResult.HasObject;
										goto doubleBreak;
									case ParseResult.Unsure:
										if (containerType.containObject == ParseResult.MustDiscard)
											containerType.containObject = ParseResult.Unsure;
										break;
								}
							}
						}
					}
				}
				catch (BreakException)
				{
					this.currentTypeAnalyzer.analyzedTypes.Remove(type);
					throw;
				}

				doubleBreak:

				if (containerType.containObject == ParseResult.Unsure)
					this.currentTypeAnalyzer.incompleteContainers.Add(containerType);

				return containerType.containObject;
			}
			else if (type.IsClass == true || type.IsStruct() == true)
			{
				for (int i = 0; i < this.window.typeFinders.array.Length; i++)
				{
					if (this.window.typeFinders.array[i].CanFind(type) == true)
					{
						this.window.typeFinders.BringToTop(i);
						return ParseResult.HasObject;
					}
				}

				ContainerType	containerType = new ContainerType(type, typeof(Object).IsAssignableFrom(type) == true ? ParseResult.HasObject : ParseResult.Unsure);

				this.currentTypeAnalyzer.analyzedTypes.Add(type, containerType);

				try
				{
					TypeMembersExclusion[]	tme;

					if (this.cachedTME.TryGetValue(type, out tme) == false)
					{
						for (int i = 0; i < this.window.typeExclusions.Length; i++)
						{
							if (this.window.typeExclusions[i].CanHandle(type) == true)
								this.reuseTME.Add(this.window.typeExclusions[i]);
						}

						if (this.reuseTME.Count > 0)
						{
							tme = this.reuseTME.ToArray();
							this.reuseTME.Clear();
						}
						else
							tme = NGAssetFinderWindow.EmptyTME;

						this.cachedTME.Add(type, tme);
					}

					if ((this.window.searchOptions & SearchOptions.Property) != 0)
					{
						foreach (MemberInfo member in Utility.EachMemberHierarchyOrdered(type, typeof(object), this.window.fieldSearchFlags | this.window.propertySearchFlags))
						{
							FieldInfo	field = member as FieldInfo;

							if (field != null)
							{
								if ((this.window.searchOptions & (SearchOptions.SerializeField | SearchOptions.NonPublic)) == SearchOptions.SerializeField &&
									field.IsPrivate == true &&
									field.IsDefined(typeof(SerializeField), false) == false)
								{
									continue;
								}

								int	i = 0;

								for (; i < tme.Length; i++)
								{
									if (tme[i].IsExcluded(field.Name) == true)
										break;
								}

								if (i < tme.Length)
									continue;

								switch (this.ParseType(field.FieldType))
								{
									case ParseResult.HasObject:
										containerType.fields.Add(field);
										containerType.containObject = ParseResult.HasObject;
										break;
									case ParseResult.Unsure:
										containerType.unsureMembers.Add(field);
										break;
								}
							}
							else
							{
								PropertyInfo	property = member as PropertyInfo;

								if (property != null)
								{
									if (property.CanRead == false || // Exclude property without getter
										property.CanWrite == false || // or setter.
										property.GetIndexParameters().Length > 0) // Exclude indexers.
									{
										continue;
									}

									int	i = 0;

									for (; i < tme.Length; i++)
									{
										if (tme[i].IsExcluded(property.Name) == true)
											break;
									}

									if (i < tme.Length)
										continue;

									switch (this.ParseType(property.PropertyType))
									{
										case ParseResult.HasObject:
											containerType.properties.Add(property);
											containerType.containObject = ParseResult.HasObject;
											break;
										case ParseResult.Unsure:
											containerType.unsureMembers.Add(property);
											break;
									}
								}
							}
						}
					}
					else
					{
						foreach (FieldInfo field in Utility.EachFieldHierarchyOrdered(type, typeof(object), this.window.fieldSearchFlags))
						{
							if ((this.window.searchOptions & (SearchOptions.SerializeField | SearchOptions.NonPublic)) == SearchOptions.SerializeField &&
								field.IsPrivate == true &&
								field.IsDefined(typeof(SerializeField), false) == false)
							{
								continue;
							}

							int	i = 0;

							for (; i < tme.Length; i++)
							{
								if (tme[i].IsExcluded(field.Name) == true)
									break;
							}

							if (i < tme.Length)
								continue;

							switch (this.ParseType(field.FieldType))
							{
								case ParseResult.HasObject:
									containerType.fields.Add(field);
									containerType.containObject = ParseResult.HasObject;
									break;
								case ParseResult.Unsure:
									containerType.unsureMembers.Add(field);
									break;
							}
						}
					}
				}
				catch (BreakException)
				{
					this.currentTypeAnalyzer.analyzedTypes.Remove(type);
					throw;
				}

				if (containerType.containObject == ParseResult.Unsure && containerType.unsureMembers.Count == 0)
					containerType.containObject = ParseResult.MustDiscard;

				if (containerType.containObject == ParseResult.Unsure)
				{
					this.currentTypeAnalyzer.incompleteContainers.Add(containerType);
					this.CheckCyclicReferences(containerType);
				}

				return containerType.containObject;
			}

			// Does not handle Dictionary and stuff like that.
			return ParseResult.MustDiscard;
		}

		private void	CheckCyclicReferences(ContainerType target)
		{
			if (this.currentTypeAnalyzer.incompleteContainers.Count < 2)
				return;

			bool	checkFullPresence = true;

			for (int i = 0; i < target.unsureMembers.Count; i++)
			{
				MemberInfo	unsureMemberInfo = target.unsureMembers[i];
				Type		type = null;

				if (unsureMemberInfo.MemberType == MemberTypes.Field)
					type = (unsureMemberInfo as FieldInfo).FieldType;
				else
					type = (unsureMemberInfo as PropertyInfo).PropertyType;

				int	j = 0;

				for (; j < this.currentTypeAnalyzer.incompleteContainers.Count; j++)
				{
					if (this.currentTypeAnalyzer.incompleteContainers[j].type == type)
						break;
				}

				if (j == this.currentTypeAnalyzer.incompleteContainers.Count)
				{
					checkFullPresence = false;
					break;
				}
			}

			if (checkFullPresence == true)
			{
				for (int i = 0; i < this.currentTypeAnalyzer.incompleteContainers.Count; i++)
				{
					ContainerType	container = this.currentTypeAnalyzer.incompleteContainers[i];

					for (int j = 0; j < container.unsureMembers.Count; j++)
					{
						MemberInfo		unsureMemberInfo = container.unsureMembers[j];
						FieldInfo		fieldInfo = null;
						PropertyInfo	propertyInfo = null;
						Type			type = null;

						if (unsureMemberInfo.MemberType == MemberTypes.Field)
						{
							fieldInfo = unsureMemberInfo as FieldInfo;
							type = fieldInfo.FieldType;
						}
						else
						{
							propertyInfo = unsureMemberInfo as PropertyInfo;
							type = propertyInfo.PropertyType;
						}

						switch (this.currentTypeAnalyzer.analyzedTypes[type].containObject)
						{
							case ParseResult.HasObject:
								container.containObject = ParseResult.HasObject;

								if (fieldInfo != null)
									container.fields.Add(fieldInfo);
								else
									container.properties.Add(propertyInfo);

								container.unsureMembers.RemoveAt(j--);
								break;
							case ParseResult.MustDiscard:
								container.unsureMembers.RemoveAt(j--);
								break;
							case ParseResult.Unsure:
								this.cyclicReferencesContainers.Clear();
								this.ResolveCyclicReferences(container);
								break;
						}
					}

					if (container.unsureMembers.Count == 0 && container.containObject == ParseResult.Unsure)
						container.containObject = ParseResult.MustDiscard;
				}
			}
		}

		private void	ResolveCyclicReferences(ContainerType origin)
		{
			if (this.cyclicReferencesContainers.Contains(origin) == false)
			{
				this.cyclicReferencesContainers.Add(origin);

				for (int i = 0; i < origin.unsureMembers.Count; i++)
				{
					MemberInfo	unsureMemberInfo = origin.unsureMembers[i];
					Type		type = null;

					if (unsureMemberInfo.MemberType == MemberTypes.Field)
						type = (unsureMemberInfo as FieldInfo).FieldType;
					else
						type = (unsureMemberInfo as PropertyInfo).PropertyType;

					this.pendingCyclicContainers.Push(this.currentTypeAnalyzer.analyzedTypes[type]);
				}

				while (pendingCyclicContainers.Count > 0)
					this.ResolveCyclicReferences(pendingCyclicContainers.Pop());
			}
			else
			{
				// A cycle has been reached.
				// Check if any member is pending.
				for (int i = 0; i < origin.unsureMembers.Count; i++)
				{
					MemberInfo	unsureMemberInfo = origin.unsureMembers[i];
					Type		type = null;

					if (unsureMemberInfo.MemberType == MemberTypes.Field)
						type = (unsureMemberInfo as FieldInfo).FieldType;
					else
						type = (unsureMemberInfo as PropertyInfo).PropertyType;

					if (this.pendingCyclicContainers.Contains(this.currentTypeAnalyzer.analyzedTypes[type]) == true)
						return;
				}

				// If not, discard everyone.
				for (int i = 0; i < this.cyclicReferencesContainers.Count; i++)
					this.cyclicReferencesContainers[i].containObject = ParseResult.MustDiscard;
			}
		}

		private void	ExtractClass(SearchResult result, AssetMatches  assetMatches, Match match, Type type, object instance)
		{
			ContainerType	container;

			if (this.currentTypeAnalyzer.analyzedTypes.TryGetValue(type, out container) == false)
			{
				switch (this.ParseType(type))
				{
					case ParseResult.HasObject:
						if (this.currentTypeAnalyzer.analyzedTypes.TryGetValue(type, out container) == false)
							throw new Exception("Type \"" + type + "\" is missing in \"" + instance + "\" from Object \"" + assetMatches.origin + "\".");
						break;
					case ParseResult.Unsure:
						throw new Exception("Type \"" + type + "\" has been determined unliable, which should never happened.");
				}
			}

			if (container != null && container.containObject == ParseResult.HasObject)
			{
				for (int j = 0; j < container.fields.Count; j++)
					this.ExtractMember(result, assetMatches, match, instance, new FieldModifier(container.fields[j]));

				for (int j = 0; j < container.properties.Count; j++)
				{
					try
					{
						this.ExtractMember(result, assetMatches, match, instance, new PropertyModifier(container.properties[j]));
					}
					catch (Exception ex)
					{
						if (result.aMemberHasFailed.Contains(assetMatches.origin) == false)
							result.aMemberHasFailed.Add(assetMatches.origin);
						InternalNGDebug.VerboseLogException("Checking property \"" + container.properties[j].Name + "\" from Type \"" + type + "\" failed.", ex, assetMatches.origin);
					}
				}
			}
		}

		private void	ExtractMember(SearchResult result, AssetMatches assetMatches, Match match, object instance, IFieldModifier f)
		{
			Match	subMatch = null;

			if (typeof(Object).IsAssignableFrom(f.Type) == true)
			{
				if (f.Type.IsAssignableFrom(this.window.targetType) == true)
				{
					++result.potentialMatchesCount;

					if (this.window.targetAsset.Equals(f.GetValue(instance)) == true)
					{
						++result.effectiveMatchesCount;
						if (match == null)
						{
							match = new Match(instance, f);
							match.path = f.Name;
							assetMatches.matches.Add(match);
						}
						else
						{
							subMatch = new Match(instance, f);
							subMatch.path = f.Name;
							subMatch.valid = true;
							match.subMatches.Add(subMatch);
							match.valid = true;
						}
					}
				}
			}
			else if (f.Type.IsUnityArray() == true)
			{
				object	rawArray = f.GetValue(instance);

				if (rawArray == null)
					return;

				ICollectionModifier	collectionModifier = NGTools.Utility.GetCollectionModifier(rawArray);
				bool				newMatch = false;
				Match				indexMatch = null;

				if (match == null)
				{
					newMatch = true;
					match = new Match(instance, f);

					subMatch = match;
				}
				else
					subMatch = new Match(instance, f);

				for (int i = 0; i < collectionModifier.Size; i++)
				{
					object	element = collectionModifier.Get(i);

					if (element != null)
					{
						Type	subType = element.GetType();

						if (typeof(Object).IsAssignableFrom(subType) == true)
						{
							++result.potentialMatchesCount;

							if (this.window.targetAsset.Equals(element) == true)
							{
								++result.effectiveMatchesCount;
								subMatch.valid = true;
								subMatch.arrayIndexes.Add(i);
							}
						}
						else if (subType.IsPrimitive == false &&
								 subType.IsEnum == false &&
								 subType != typeof(Decimal) &&
								 subType != typeof(String) &&
								 typeof(Delegate).IsAssignableFrom(subType) == false)
						{
							if (indexMatch == null)
								indexMatch = new Match(instance, f);

							int	j = 0;

							for (; j < this.window.typeFinders.array.Length; j++)
							{
								if (this.window.typeFinders.array[j].CanFind(subType) == true)
								{
									this.window.typeFinders.array[j].Find(subType, element, subMatch, result);
									this.window.typeFinders.BringToTop(j);
									break;
								}
							}

							if (j >= this.window.typeFinders.array.Length)
								this.ExtractClass(result, assetMatches, indexMatch, subType, element);

							if (indexMatch.valid == true)
							{
								indexMatch.path = "#" + i.ToCachedString();
								subMatch.subMatches.Add(indexMatch);
								indexMatch = null;
							}
						}
					}
				}

				if (subMatch.subMatches.Count > 0 ||
					subMatch.arrayIndexes.Count > 0)
				{
					match.valid = true;

					if (newMatch == false)
						match.subMatches.Add(subMatch);
					else
						assetMatches.matches.Add(match);
				}

				NGTools.Utility.ReturnCollectionModifier(collectionModifier);
			}
			else if (f.Type.IsClass == true || f.Type.IsStruct() == true)
			{
				object	classInstance = f.GetValue(instance);

				if (classInstance != null)
				{
					bool	newMatch = false;

					if (match == null)
					{
						newMatch = true;
						match = new Match(instance, f);

						subMatch = match;
					}
					else
						subMatch = new Match(instance, f);

					int	i = 0;

					for (; i < this.window.typeFinders.array.Length; i++)
					{
						if (this.window.typeFinders.array[i].CanFind(f.Type) == true)
						{
							this.window.typeFinders.array[i].Find(f.Type, f.GetValue(instance), subMatch, result);
							this.window.typeFinders.BringToTop(i);
							break;
						}
					}

					if (i >= this.window.typeFinders.array.Length)
						this.ExtractClass(result, assetMatches, subMatch, f.Type, classInstance);

					if (subMatch.valid == true)
					{
						match.valid = true;

						if (newMatch == false)
							match.subMatches.Add(subMatch);
						else
							assetMatches.matches.Add(match);
					}
				}
			}
		}
	}
}