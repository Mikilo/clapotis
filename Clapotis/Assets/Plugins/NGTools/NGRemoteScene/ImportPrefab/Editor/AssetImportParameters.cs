using NGTools;
using NGTools.Network;
using NGTools.NGRemoteScene;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace NGToolsEditor.NGRemoteScene
{
	using UnityEngine;

	public enum ImportMode
	{
		None,
		DontImport,
		UseGUID,
		RawCopy,
		Auto
	}

	[Serializable]
	public class AssetImportParameters : ISerializationCallbackReceiver
	{
		public const float	RememberLabelWidth = 100F;
		public const float	RememberSpacing = 10F;
		public const float	LocationButtonWidth = 100F;

		public class MyClass
		{
			public int		componentInstanceID;
			public int		gameObjectInstanceID;
			public string	path;
		}

		// TODO Find a way to persist import settings
		public List<MyClass>	originPath = new List<MyClass>();
		/// <summary>Defines the Type of the asset where it has been first imported (Parent class, etc...).</summary>
		public Type			type;
		/// <summary>Defines the Type of the downloaded asset.</summary>
		public Type			realType;
		//public int			gameObjectInstanceID;
		public int			instanceID;

		public ImportMode	importMode;
		// Used for RawCopy mode.
		public string		outputPath = string.Empty;
		private bool		isDownloading = false;
		// Used for UseGUID mode.
		public Object		guidAsset;
		public string		guid;
		// Used for Auto mode.
		public string		autoPath = null;
		public string		autoPathError = null;
		public string		prefabPath = string.Empty;

		public bool			remember;
		public string		importErrorMessage;
		public bool			isSupported = true;

		private bool	parametersConfirmed;
		public bool		ParametersConfirmed { get { return this.parametersConfirmed; } }

		public string	name;
		//public string	gameObjectName;

		public Object	localAsset;
		public Object	copyAsset;

		public bool		finalized;
		public Object	finalObject;
		public int	bytesReceived;
		public int	totalBytes;

		private NGRemoteHierarchyWindow	hierarchy;
		private EditorWindow			updateWindow;

		public	AssetImportParameters(NGRemoteHierarchyWindow hierarchy, string path, Type type, int gameObjectInstanceID, int componentInstanceID, int instanceID, bool isSupported, Object localAsset)
		{
			this.hierarchy = hierarchy;
			this.originPath.Add(new MyClass() { gameObjectInstanceID = gameObjectInstanceID, componentInstanceID = componentInstanceID, path = path });
			this.type = type;
			this.instanceID = instanceID;
			//this.gameObjectInstanceID = gameObjectInstanceID;
			this.isSupported = isSupported;
			this.parametersConfirmed = isSupported == false || localAsset != null;
			this.localAsset = localAsset;
		}

		public float	GetHeight()
		{
			//bool	canRemember = this.localAsset == null &&
			//					  this.isSupported == true &&
			//					  this.originPath != null;
			float	height = Constants.SingleLineHeight + 2F + /*Constants.SingleLineHeight + 2F + */6F; // Header + Spacing + /*Component path + Spacing + */Bottom margin

			int	pathsCount = this.originPath.Count;
			//if (pathsCount == 1)
			//	EditorGUILayout.LabelField("Component Path", this.originPath[0]);
			//else if (pathsCount > 1)
			//	EditorGUILayout.LabelField("Component Path", "Many (" + pathsCount + ")");

			if (this.localAsset != null)
				height += Constants.SingleLineHeight;
				//EditorGUILayout.ObjectField("Asset", this.localAsset, this.type, false, GUILayoutOptionPool.Height(Constants.SingleLineHeight));
			else if (this.isSupported == false)
				height += 24F;
				//EditorGUILayout.HelpBox("Not supported.", MessageType.Warning);
			else
			{
				//bool	hasError = this.importMode == ImportMode.None;

				//EditorGUILayout.BeginHorizontal();
				{
					if (pathsCount > 0)
					{
						height += Constants.SingleLineHeight;
						//EditorGUI.BeginDisabledGroup(connected == false || this.parametersConfirmed == true);
						//{
						//	this.importMode = (ImportMode)EditorGUILayout.EnumPopup(this.importMode, GUILayoutOptionPool.Width(112F - EditorGUI.indentLevel * 15F));
						//}
						//EditorGUI.EndDisabledGroup();
					}

					//using (LabelWidthRestorer.Get(90F))
					{
						//EditorGUILayout.BeginVertical();
						{
							//EditorGUI.BeginDisabledGroup(connected == false || this.parametersConfirmed == true);
							{
								if (this.importMode == ImportMode.Auto)
								{
									if (/*parameter.gameObjectName != null && */this.name != null)
									{
										if (this.autoPath == null)
										{
											height += 24F;
											//hasError = true;
											//EditorGUILayout.HelpBox("Asset type is not supported, will not be imported.", MessageType.Warning);
										}
										else
										{
											height += Constants.SingleLineHeight;
											//EditorGUI.BeginChangeCheck();
											//this.autoPath = NGEditorGUILayout.SaveFileField("Output Path", this.autoPath);
											//if (EditorGUI.EndChangeCheck() == true)
											//{
											//	System.Uri	pathUri;
											//	bool		isValidUri = System.Uri.TryCreate(this.autoPath, System.UriKind.Relative, out pathUri);
											//	bool		v = isValidUri && pathUri != null/* && pathUri.IsLoopback*/;

											//	//FileInfo fi = null;
											//	//try
											//	//{
											//	//	fi = new FileInfo(parameter.autoPath);
											//	//}
											//	//catch (System.ArgumentException) { }
											//	//catch (PathTooLongException) { }
											//	//catch (System.NotSupportedException) { }

											//	if (v == false)
											//	//if (ReferenceEquals(fi, null))
											//		this.autoPathError = "Path is invalid.";
											//		// file name is not valid
											//	else
											//		this.autoPathError = null;
											//		// file name is valid... May check for existence by calling fi.Exists.
											//}

											if (string.IsNullOrEmpty(this.autoPathError) == false)
												height += 24F;
												//EditorGUILayout.HelpBox(this.autoPathError, MessageType.Error);
										}
									}
									else
									{
										height += Constants.SingleLineHeight;
										//Utility.content.text = "Waiting for server...";
										//EditorGUILayout.LabelField(Utility.content, GeneralStyles.StatusWheel);
										//this.hierarchy.Repaint();
									}
								}
								else if (this.importMode == ImportMode.UseGUID)
								{
									height += Constants.SingleLineHeight + Constants.SingleLineHeight;
									//EditorGUI.BeginChangeCheck();
									//Object	o = EditorGUILayout.ObjectField("Asset", this.guidAsset, this.type, false, GUILayoutOptionPool.Height(Constants.SingleLineHeight));
									//EditorGUILayout.LabelField("GUID", this.guid ?? "None");
									//if (EditorGUI.EndChangeCheck() == true)
									//{
									//	this.guidAsset = o;
									//	this.guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(o));
									//}

									//if (string.IsNullOrEmpty(this.guid) == true)
									//	hasError = true;
								}
								else if (this.importMode == ImportMode.RawCopy)
								{
									height += Constants.SingleLineHeight;
									//EditorGUI.BeginChangeCheck();
									//string	path = NGEditorGUILayout.SaveFileField("Output Path", this.outputPath);
									//if (EditorGUI.EndChangeCheck() == true)
									//{
									//	if (path.StartsWith(Application.dataPath) == true)
									//		this.outputPath = path.Substring(Application.dataPath.Length - "Assets".Length);
									//	else if (path.StartsWith("Assets") == true)
									//		this.outputPath = path;
									//}

									//if (string.IsNullOrEmpty(this.outputPath) == false &&
									//	this.outputPath.StartsWith(Application.dataPath) == false &&
									//	this.outputPath.StartsWith("Assets") == false)
									//{
									//	//hasError = true;
									//	EditorGUILayout.HelpBox("Path must be in Assets folder.", MessageType.Warning);
									//}
								}
								//else if (this.importMode == ImportMode.DontImport)
								//{
								//	height += Constants.SingleLineHeight + Constants.SingleLineHeight;
								//}
							}
							//EditorGUI.EndDisabledGroup();

							if (this.parametersConfirmed == true)
							{
								if (this.importErrorMessage != null)
									height += 2F + 32F;
									//EditorGUILayout.HelpBox("Import failed : " + this.importErrorMessage, MessageType.Error);
								else if (this.copyAsset != null && (this.importMode == ImportMode.RawCopy || this.importMode == ImportMode.Auto))
									height += 2F + Constants.SingleLineHeight;
									//EditorGUILayout.ObjectField("Created Asset", this.copyAsset, this.type, false, GUILayoutOptionPool.Height(Constants.SingleLineHeight));
								else// if (this.hierarchy.IsChannelBlocked(this.instanceID) == true)
									if (this.totalBytes > 0)
									height += 2F + Constants.SingleLineHeight;
							}
							else
							{
								height += 2F + Constants.SingleLineHeight;
								//EditorGUILayout.BeginHorizontal();
								//EditorGUI.BeginDisabledGroup(hasError);
								//{
								//	GUILayout.FlexibleSpace();

								//	if ((importAssets2.autoImportAssets == true && hasError == false) || GUILayout.Button("Confirm") == true)
								//	{
								//		this.parametersConfirmed = true;
								//		this.hierarchy.Repaint();
								//	}
								//}
								//EditorGUI.EndDisabledGroup();
								//EditorGUILayout.EndHorizontal();
							}
						}
						//EditorGUILayout.EndVertical();
					}

					//if (Conf.DebugMode != Conf.DebugState.None && connected == true && this.importErrorMessage != null && GUILayout.Button("Retry Import") == true)
					//{
					//	this.parametersConfirmed = false;
					//	this.copyAsset = null;
					//	this.localAsset = null;
					//	this.importErrorMessage = null;
					//}
				}
				//EditorGUILayout.EndHorizontal();
			}

			return height;
		}

		public void	DrawAssetImportParams(Rect r2, ImportAssetsWindow importAssetsWindow)
		{
			this.updateWindow = importAssetsWindow;
			//string	gameObjectName = parameter.gameObjectName;

			//if (gameObjectName == null)
			//	gameObjectName = parameter.gameObjectName = this.hierarchy.GetGameObjectName(parameter.gameObjectInstanceID);

			float	w = r2.width;

			r2.height = Constants.SingleLineHeight;
			//Rect	r2 = GUILayoutUtility.GetRect(22F, Constants.SingleLineHeight);
			bool	canRemember = this.localAsset == null &&
								  this.isSupported == true;

			if (Event.current.type == EventType.MouseDown)
			{
				if (canRemember == true)
					r2.width -= AssetImportParameters.RememberLabelWidth + AssetImportParameters.RememberSpacing + AssetImportParameters.LocationButtonWidth;
				else
					r2.width -= AssetImportParameters.LocationButtonWidth;

				//if (r2.Contains(Event.current.mousePosition) == true)
				//{
				//	this.hierarchy.PingObject(this.gameObjectInstanceID);
				//	Event.current.Use();
				//}

				if (canRemember == true)
					r2.width += AssetImportParameters.RememberLabelWidth + AssetImportParameters.RememberSpacing + AssetImportParameters.LocationButtonWidth;
				else
					r2.width += AssetImportParameters.LocationButtonWidth;
			}

			GUI.Box(r2, string.Empty, GeneralStyles.Toolbar);

			//if (Event.current.type == EventType.Repaint)
			//{
			//	Rect	r3 = r2;
			//	r3.width = 1F;
			//	r3.height = r3.height * 4F + 6F;

			//	//EditorGUI.DrawRect(r3, Color.cyan);
			//}

			r2.width = ImportAssetsWindow.GameObjectIconWidth;
			GUI.DrawTexture(r2, AssetPreview.GetMiniTypeThumbnail(this.type), ScaleMode.ScaleToFit);
			r2.x += r2.width;

			string	name = this.name ?? (this.name = this.hierarchy.GetResourceName(this.type, this.instanceID));

			if (name == null)
			{
				GUI.Label(r2, GeneralStyles.StatusWheel);
				r2.x += r2.width;

				r2.width = w - r2.x;
				GUI.Label(r2, this.type.Name);
				importAssetsWindow.Repaint();
			}
			else
			{
				r2.width = w - r2.x;
				GUI.Label(r2, this.type.Name + " : " + this.name);
			}

			r2.x = r2.width + ImportAssetsWindow.GameObjectIconWidth - AssetImportParameters.RememberLabelWidth - AssetImportParameters.RememberSpacing - AssetImportParameters.LocationButtonWidth;
			r2.width = AssetImportParameters.RememberLabelWidth;
			if (canRemember == true)
				this.remember = GUI.Toggle(r2, this.remember, "Remember", GeneralStyles.ToolbarToggle);

			r2.x += r2.width + AssetImportParameters.RememberSpacing;
			r2.width = AssetImportParameters.LocationButtonWidth;
			if (GUI.Button(r2, "Locations (" + this.originPath.Count + ")", GeneralStyles.ToolbarButton) == true)
			{
				PopupWindow.Show(r2, new AssetLocationsWindow(this.hierarchy, this.originPath));
				//importAssets2.importingAssetsParams.Remove(this);
			}

			r2.y += r2.height + 2F;
			r2.x = 5F;
			r2.width = w - r2.x;

			using (LabelWidthRestorer.Get(120F))
			{
				int	pathsCount = this.originPath.Count;
				//if (pathsCount == 1)
				//	EditorGUI.LabelField(r2, "Component Path", this.originPath[0]);
				//else if (pathsCount > 1)
				//{
				//	EditorGUI.LabelField(r2, "Component Path", string.Empty);

				//	r2.xMin += EditorGUIUtility.labelWidth;
				//	if (GUI.Button(r2, "Many (" + pathsCount + ")", GeneralStyles.ToolbarDropDown) == true)
				//	{
				//	}
				//	r2.xMin -= EditorGUIUtility.labelWidth;
				//}
				r2.y += /*r2.height + */2F;

				if (this.localAsset != null)
					EditorGUI.ObjectField(r2, "Asset", this.localAsset, this.type, false);
				else if (this.isSupported == false)
				{
					r2.height = 24F;
					r2.xMin += 5F;
					r2.xMax -= 5F;
					EditorGUI.HelpBox(r2, "Not supported.", MessageType.Warning);
				}
				else
				{
					bool	hasError = this.importMode == ImportMode.None;

					//EditorGUILayout.BeginHorizontal();
					{
						using (LabelWidthRestorer.Get(100F))
						{
							if (pathsCount > 0)
							{
								EditorGUI.BeginDisabledGroup(this.parametersConfirmed == true);
								{
									//r2.width = 182F/* - EditorGUI.indentLevel * 15F*/;
									this.importMode = (ImportMode)EditorGUI.EnumPopup(r2, "Import Mode", this.importMode);
									r2.y += r2.height;
								}
								EditorGUI.EndDisabledGroup();
							}

							//EditorGUILayout.BeginVertical();
							{
								EditorGUI.BeginDisabledGroup(this.parametersConfirmed == true);
								{
									if (this.importMode == ImportMode.Auto)
									{
										if (this.name == null)
											this.name = this.hierarchy.GetResourceName(this.type, this.instanceID);

										if (this.autoPath == null && this.name != null)
										{
											IObjectImporter	importer = RemoteUtility.GetImportAssetTypeSupported(this.type);

											if (importer != null)
											{
												string	path = this.prefabPath;
												string	filename = string.Join("_", this.name.Split(Path.GetInvalidFileNameChars())) + importer.GetExtension();

												if (string.IsNullOrEmpty(this.hierarchy.specificSharedSubFolder) == false)
													path = this.hierarchy.specificSharedSubFolder;

												//if (this.hierarchy.rawCopyAssetsToSubFolder == true)
												//	path += "/" + .gameObjectName;
												//else if (this.hierarchy.prefixAsset == true)
												//	filename = string.Join("_", (parameter.gameObjectName).Split(Path.GetInvalidFileNameChars())) + '_' + filename;

												this.autoPath = path + "/" + filename;
											}
										}

										if (/*parameter.gameObjectName != null && */this.name != null)
										{
											if (this.autoPath == null)
											{
												r2.height = 24F;
												hasError = true;
												EditorGUI.HelpBox(r2, "Asset type is not supported, will not be imported.", MessageType.Warning);
												r2.y += r2.height;
											}
											else
											{
												EditorGUI.BeginChangeCheck();
												this.autoPath = NGEditorGUILayout.SaveFileField(r2, "Output Path", this.autoPath);
												if (EditorGUI.EndChangeCheck() == true)
												{
													Uri		pathUri;
													bool	isValidUri = Uri.TryCreate(this.autoPath, UriKind.Relative, out pathUri);
													bool	v = isValidUri && pathUri != null/* && pathUri.IsLoopback*/;

													//FileInfo fi = null;
													//try
													//{
													//	fi = new FileInfo(parameter.autoPath);
													//}
													//catch (System.ArgumentException) { }
													//catch (PathTooLongException) { }
													//catch (System.NotSupportedException) { }

													if (v == false)
													//if (ReferenceEquals(fi, null))
														this.autoPathError = "Path is invalid.";
														// file name is not valid
													else
														this.autoPathError = null;
														// file name is valid... May check for existence by calling fi.Exists.
												}
												r2.y += r2.height;

												if (string.IsNullOrEmpty(this.autoPathError) == false)
												{
													r2.height = 24F;
													EditorGUI.HelpBox(r2, this.autoPathError, MessageType.Error);
													r2.y += r2.height;
												}
											}
										}
										else
										{
											Utility.content.text = "Waiting for server...";
											EditorGUI.LabelField(r2, Utility.content, GeneralStyles.StatusWheel);
											r2.y += r2.height;

											importAssetsWindow.Repaint();
										}
									}
									else if (this.importMode == ImportMode.UseGUID)
									{
										EditorGUI.BeginChangeCheck();
										Object	o = EditorGUI.ObjectField(r2, "Asset", this.guidAsset, this.type, false);
										if (EditorGUI.EndChangeCheck() == true)
										{
											this.guidAsset = o;
											this.guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(o));
										}
										r2.y += r2.height;

										EditorGUI.LabelField(r2, "GUID", this.guid ?? "None");
										r2.y += r2.height;

										if (string.IsNullOrEmpty(this.guid) == true)
											hasError = true;
									}
									else if (this.importMode == ImportMode.RawCopy)
									{
										EditorGUI.BeginChangeCheck();
										string	path = NGEditorGUILayout.SaveFileField(r2, "Output Path", this.outputPath);
										if (EditorGUI.EndChangeCheck() == true)
										{
											if (path.StartsWith(Application.dataPath) == true)
												this.outputPath = path.Substring(Application.dataPath.Length - "Assets".Length);
											else if (path.StartsWith("Assets") == true)
												this.outputPath = path;
										}
										r2.y += r2.height;

										if (string.IsNullOrEmpty(path) == false &&
											path.StartsWith(Application.dataPath) == false &&
											path.StartsWith("Assets") == false)
										{
											hasError = true;
											r2.height = 24F;
											EditorGUI.HelpBox(r2, "Path must be in Assets folder.", MessageType.Warning);
											r2.y += r2.height;
										}
									}
								}
								EditorGUI.EndDisabledGroup();

								r2.y += 2F;
								r2.height = Constants.SingleLineHeight;

								if (this.parametersConfirmed == true)
								{
									if (this.importErrorMessage != null)
									{
										r2.xMin += 5F;
										r2.height = 32F;
										EditorGUI.HelpBox(r2, "Import failed : " + this.importErrorMessage, MessageType.Error);
									}
									else if (this.copyAsset != null && (this.importMode == ImportMode.RawCopy || this.importMode == ImportMode.Auto))
										EditorGUI.ObjectField(r2, "Created Asset", this.copyAsset, this.type, false);
									else if (this.totalBytes > 0)
									{
										r2.xMin += 5F;

										if (this.bytesReceived == this.totalBytes)
											EditorGUI.ProgressBar(r2, 1F, "Creating asset...");
										else
										{
											float	rate = (float)this.bytesReceived / (float)this.totalBytes;
											string	cacheFileSize;

											if (this.totalBytes >= 1024 * 1024)
												cacheFileSize = ((float)(this.bytesReceived / (1024F * 1024F))).ToString("N1") + " / " + ((float)(this.totalBytes / (1024F * 1024F))).ToString("N1") + " MiB";
											else if (this.totalBytes >= 1024)
												cacheFileSize = ((float)(this.bytesReceived / 1024F)).ToString("N1") + " / " + ((float)(this.totalBytes / 1024F)).ToString("N1") + " KiB";
											else
												cacheFileSize = this.bytesReceived + " / " + this.totalBytes + " B";

											EditorGUI.ProgressBar(r2, rate, cacheFileSize + " (" + (rate * 100F).ToString("0.0") + "%)");
											importAssetsWindow.Repaint();
										}
									}
								}
								else
								{
									EditorGUI.BeginDisabledGroup(hasError);
									{
										r2.width = 100F;
										r2.x = w - r2.width - 10F;

										if (GUI.Button(r2, "Confirm") == true)
										{
											this.Confirm();
											importAssetsWindow.Repaint();
										}
									}
									EditorGUI.EndDisabledGroup();
								}
							}
						}

						if (Conf.DebugMode != Conf.DebugState.None && this.importErrorMessage != null && GUI.Button(r2, "Retry Import") == true)
							this.ResetImport();
					}
				}
			}
		}

		public void	Confirm()
		{
			this.parametersConfirmed = true;

			if (this.importMode == ImportMode.DontImport)
				this.finalized = true;
			else if (this.importMode == ImportMode.Auto)
			{
				if (this.name == null)
					this.name = this.hierarchy.GetResourceName(this.type, this.instanceID);

				if (this.autoPath == null && this.name != null)
				{
					IObjectImporter	importer = RemoteUtility.GetImportAssetTypeSupported(this.type);

					if (importer != null)
					{
						string	path = this.prefabPath;
						string	filename = string.Join("_", this.name.Split(Path.GetInvalidFileNameChars())) + importer.GetExtension();

						if (string.IsNullOrEmpty(this.hierarchy.specificSharedSubFolder) == false)
							path = this.hierarchy.specificSharedSubFolder;

						//if (this.hierarchy.rawCopyAssetsToSubFolder == true)
						//	path += "/" + .gameObjectName;
						//else if (this.hierarchy.prefixAsset == true)
						//	filename = string.Join("_", (parameter.gameObjectName).Split(Path.GetInvalidFileNameChars())) + '_' + filename;

						this.autoPath = path + "/" + filename;
					}
				}
			}
		}

		public void	ResetImport()
		{
			this.parametersConfirmed = false;
			this.copyAsset = null;
			this.localAsset = null;
			this.autoPath = null;
			this.autoPathError = null;
			this.importErrorMessage = null;
		}

		public ImportAssetState	CheckImportState()
		{
			if (this.finalized == true)
				return ImportAssetState.Ready;

			if (this.localAsset != null)
			{
				this.finalized = true;
				this.finalObject = this.localAsset;
				return ImportAssetState.Ready;
			}
			else if (this.importErrorMessage != null)
				return ImportAssetState.Ready;
			else if (this.parametersConfirmed == true)
			{
				if (this.importMode == ImportMode.None)
				{
					this.finalized = true;
					//return ImportAssetState.Waiting;
				}
				else if (this.importMode == ImportMode.Auto)
				{
					if (this.name == null)
						this.name = this.hierarchy.GetResourceName(this.type, this.instanceID);

					if (this.autoPath == null && this.name != null)
					{
						IObjectImporter	importer = RemoteUtility.GetImportAssetTypeSupported(this.type);

						if (importer != null)
						{
							string	path = this.prefabPath;
							string	filename = string.Join("_", this.name.Split(Path.GetInvalidFileNameChars())) + importer.GetExtension();

							if (string.IsNullOrEmpty(this.hierarchy.specificSharedSubFolder) == false)
								path = this.hierarchy.specificSharedSubFolder;

							//if (this.hierarchy.rawCopyAssetsToSubFolder == true)
							//	path += "/" + .gameObjectName;
							//else if (this.hierarchy.prefixAsset == true)
							//	filename = string.Join("_", (parameter.gameObjectName).Split(Path.GetInvalidFileNameChars())) + '_' + filename;

							this.autoPath = path + "/" + filename;
						}
					}

					if (this.copyAsset != null)
					{
						this.finalized = true;
						this.finalObject = this.copyAsset;
					}
					else
					{
						if (this.isDownloading == false)
						{
							this.isDownloading = true;
							this.hierarchy.Client.AddPacket(new ClientRequestRawAssetPacket(this.type, this.instanceID), this.OnRequestAssetCompleted, this.OnRequestAssetUpdated);
						}

						return ImportAssetState.Requesting;
					}
				}
				else if (this.importMode == ImportMode.UseGUID)
				{
					if (string.IsNullOrEmpty(this.guid) == false)
					{
						this.finalized = true;
						this.finalObject = this.guidAsset ?? AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(this.guid), this.type);
					}
					else
					{
						this.finalized = true;
						this.finalObject = null;
					}
				}
				else if (this.importMode == ImportMode.RawCopy)
				{
					if (this.copyAsset != null)
					{
						this.finalized = true;
						this.finalObject = this.copyAsset;
					}
					else if (string.IsNullOrEmpty(this.outputPath) == true)
						return ImportAssetState.Waiting;
					else
					{
						if (this.isDownloading == false)
						{
							this.isDownloading = true;
							this.hierarchy.Client.AddPacket(new ClientRequestRawAssetPacket(this.type, this.instanceID), this.OnRequestAssetCompleted, this.OnRequestAssetUpdated);
						}

						return ImportAssetState.Requesting;
					}
				}

				return ImportAssetState.Ready;
			}

			return ImportAssetState.DoesNotExist;
			//throw new Exception("AssetImportParameters not ready.");
		}

		private void	OnRequestAssetCompleted(ResponsePacket p)
		{
			this.importErrorMessage = p.errorMessage;

			if (p.CheckPacketStatus() == true)
			{
				ServerSendRawAssetPacket	packet = p as ServerSendRawAssetPacket;

				this.realType = packet.realType;

				this.isDownloading = false;

				Object	asset = null;

				//if (this.importMode == AssetImportParameters.ImportMode.UseGUID)
				//	asset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(this.guid), realType);
				//else 
				if (this.importMode == ImportMode.Auto)
				{
					if (string.IsNullOrEmpty(this.autoPath) == false)
					{
						IObjectImporter	importer = RemoteUtility.GetImportAssetTypeSupported(realType);

						if (importer != null)
						{
							try
							{
								ImportAssetResult	r = importer.ToAsset(packet.data, this.autoPath, out asset);

								if (r == ImportAssetResult.SavedToDisk)
								{
									AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
									asset = AssetDatabase.LoadAssetAtPath(this.autoPath, realType);
									InternalNGDebug.Log("Asset created at \"" + this.autoPath + "\".");
								}
								else if (r == ImportAssetResult.NeedCreateViaAssetDatabase)
								{
									if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(this.autoPath)) == false)
										AssetDatabase.DeleteAsset(this.autoPath);
									AssetDatabase.CreateAsset(asset, this.autoPath);
									InternalNGDebug.Log("Asset created at \"" + this.autoPath + "\".");
								}
								else if (r == ImportAssetResult.ImportFailure)
									this.importErrorMessage = "Asset creation failed during import.";
							}
							catch (Exception ex)
							{
								InternalNGDebug.LogException(ex);
								this.importErrorMessage = ex.ToString();
							}
						}
					}
				}
				else if (this.importMode == ImportMode.RawCopy)
				{
					if (string.IsNullOrEmpty(this.outputPath) == false)
					{
						IObjectImporter	importer = RemoteUtility.GetImportAssetTypeSupported(realType);

						if (importer != null)
						{
							ImportAssetResult	r = importer.ToAsset(packet.data, this.outputPath, out asset);

							if (r == ImportAssetResult.SavedToDisk)
							{
								AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
								asset = AssetDatabase.LoadAssetAtPath(this.outputPath, realType);
							}
							else if (r == ImportAssetResult.NeedCreateViaAssetDatabase)
							{
								if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(this.outputPath)) == false)
									AssetDatabase.DeleteAsset(this.outputPath);
								AssetDatabase.CreateAsset(asset, this.outputPath);
							}
						}
					}
				}

				this.copyAsset = asset;
				this.CheckImportState();
			}
			else
				this.isDownloading = false;
		}

		private void	OnRequestAssetUpdated(ProgressionUpdate p)
		{
			this.bytesReceived = p.bytesReceived;
			this.totalBytes = p.totalBytes;

			if (this.updateWindow != null)
				this.updateWindow.Repaint();
		}

		private string	typeSerialized;
		private string	realTypeSerialized;

		void	ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			if (this.type != null)
				this.typeSerialized = this.type.GetShortAssemblyType();
			if (this.realType != null)
				this.realTypeSerialized = this.realType.GetShortAssemblyType();
		}

		void	ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			this.type = Type.GetType(this.typeSerialized);
			this.realType = Type.GetType(this.realTypeSerialized);
		}
	}
}