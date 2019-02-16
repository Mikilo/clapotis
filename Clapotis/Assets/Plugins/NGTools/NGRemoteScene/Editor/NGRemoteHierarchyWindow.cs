using NGLicenses;
using NGTools;
using NGTools.Network;
using NGTools.NGRemoteScene;
using NGToolsEditor.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using InnerUtility = NGTools.Utility;

namespace NGToolsEditor.NGRemoteScene
{
	public interface INetworkScene
	{
		Client	Client { get; }

		string	GetResourceName(Type type, int instanceID);
		bool	IsChannelBlocked(int id);
	}

	public interface IImportAsset
	{
		string	SpecificSharedSubFolder { get; }
	}

	[PrewarmEditorWindow]
	public class NGRemoteHierarchyWindow : EditorWindow, IHierarchyManagement, INetworkScene, INGServerConnectable, IHasCustomMenu, IUnityData
	{
		private sealed class OptionsPopup : PopupWindowContent
		{
			private readonly NGRemoteHierarchyWindow	window;

			public	OptionsPopup(NGRemoteHierarchyWindow window)
			{
				this.window = window;
			}

			public override Vector2	GetWindowSize()
			{
				return new Vector2(Mathf.Max(this.window.position.width * .5F, 200F), this.window.IsClientConnected() ? 76F + 18F + 6F : 57F + 18F + 4F);
			}

			public override void	OnGUI(Rect r)
			{
				using (LabelWidthRestorer.Get(140F))
				{
					//Utility.content.text = LC.G("NGHierarchy_NetworkRefresh");
					//Utility.content.tooltip = LC.G("NGHierarchy_NetworkRefreshTooltip");
					//this.window.networkRefresh = EditorGUILayout.DoubleField(Utility.content, this.window.networkRefresh);

					Utility.content.text = LC.G("NGHierarchy_AutoRequestHierarchyInterval");
					Utility.content.tooltip = LC.G("NGHierarchy_AutoRequestHierarchyIntervalTooltip");
					this.window.autoRequestHierarchyInterval = EditorGUILayout.DoubleField(Utility.content, this.window.autoRequestHierarchyInterval);

					Utility.content.text = LC.G("NGHierarchy_SyncTags");
					Utility.content.tooltip = LC.G("NGHierarchy_SyncTagsTooltip");
					this.window.syncTags = EditorGUILayout.Toggle(Utility.content, this.window.syncTags);
					Utility.content.tooltip = string.Empty;

					if (GUILayout.Button("Import Prefabs") == true)
						ImportAssetsWindow.Open(this.window);

					if (this.window.IsClientConnected() == true && GUILayout.Button(LC.G("NGHierarchy_RefreshHierarchy")) == true)
						this.window.RefreshHierarchy();
				}
			}
		}

		[Serializable]
		private class OffsetException : Exception
		{
			public float	offset;

			public	OffsetException(float offset)
			{
				this.offset = offset;
			}
		}

		public class UserAsset
		{
			public string	name;
			public int		instanceID;
			public string[]	data;
		}

		private struct Change
		{
			public string	path;
			public Type		type;
			public object	value;
			public object	newValue;

			public override string	ToString()
			{
				return path + " " + type + " " + value + " > " + newValue;
			}
		}

		private enum BigHierarchyState
		{
			None,
			Display,
			Hidden
		}

		public const string	NormalTitle = "NG Remote Hierarchy";
		public const string	ShortTitle = "NG R Hierarchy";
		public static Color	TitleColor = Color.green;
		public const float	BlockRequestLifeTime = 10F;
		public const float	SceneHeight = 18F;
		public const float	GameObjectHeight = 16F;
		public const int	MassiveHierarchyThreshold = 1000;
		public const string	ProgressBarConnectingString = "Connecting NG Remote Hierachy";
		public const double	TryConstructPrefabInterval = .45D;
		public const string	LastPrefabImportPathKeyPref = "NGRemoteHierarchy.LastPrefabImportPath";
		public static Color	DropBackgroundColor = Color.yellow;
		public static Color	SelectedObjectBackgroundColor { get { return Utility.GetSkinColor(50F / 255F, 76F / 255F, 120F / 255F, 1F, 50F / 255F, 100F / 255F, 185F / 255F, 1F); } }
		public static Color	SceneBackgroundColor { get { return Utility.GetSkinColor(62F / 255F, 62F / 255F, 62F / 255F, 1F, 50F / 255F, 100F / 255F, 185F / 255F, 1F); } }
		public static Color	PathBackgroundColor { get { return Utility.GetSkinColor(60F / 255F, 60F / 255F, 60F / 255F, 1F, 180F / 255F, 180F / 255F, 180F / 255F, 1F); } }

		private static Type			HierarchyWindowType = UnityAssemblyVerifier.TryGetType(typeof(Editor).Assembly, "UnityEditor.SceneHierarchyWindow");
		private static Type			InspectorWindowType = UnityAssemblyVerifier.TryGetType(typeof(Editor).Assembly, "UnityEditor.InspectorWindow");
		private static Type			ProjectWindowType = UnityAssemblyVerifier.TryGetType(typeof(Editor).Assembly, "UnityEditor.ProjectBrowser");
		private static ByteBuffer	Buffer = new ByteBuffer(64);

		public string	address = string.Empty;
		public int		port = AutoDetectUDPClient.DefaultPort;
		public double	autoRequestHierarchyInterval = 5D;
		public bool		syncTags;

		public bool		displayNonSuppported = true;
		public bool		overridePrefab = false;
		public string	specificSharedSubFolder = null;
		public bool		rawCopyAssetsToSubFolder = false;
		public bool		prefixAsset = true;

		public event Action	HierarchyConnected;
		public event Action	HierarchyDisconnected;
		public event Action<ClientGameObject, GenericMenu>	GameObjectContextMenu;
		public event Action<Type, string[], int[]>			ResourcesUpdated;
		public event Func<Packet, bool>						PacketInterceptor;

		public Client	Client { get; private set; }
		public string[]	Layers { get; private set; }

		private int									activeScene;
		private List<ClientScene>					scenes = new List<ClientScene>(2);
		private Dictionary<int, ClientGameObject>	allGameObjectInstanceIDs = new Dictionary<int, ClientGameObject>(256);
		private Dictionary<Type, string[]>			resourceNames = new Dictionary<Type, string[]>(8);
		private Dictionary<Type, int[]>				resourceInstanceIds = new Dictionary<Type, int[]>(8);
		private Dictionary<string, EnumData>		enumData = new Dictionary<string, EnumData>(8);
		private List<string>						updateValueNotifications = new List<string>(4);
		private Dictionary<int, ClientMaterial>		materials = new Dictionary<int, ClientMaterial>(64);
		private Dictionary<Type, List<UserAsset>>	userAssets = new Dictionary<Type, List<UserAsset>>(2);
		private bool								serverSceneHasChanged = false;
		private List<KeyValuePair<AnimFloat, int>>	pings = new List<KeyValuePair<AnimFloat, int>>();

		/// <summary>Used to update GameObject provided from network, others will be deleted.</summary>
		private int						hierarchyUpdateCounter = 0;
		private Stack<ClientGameObject>	remainingRegisterGameObjects = new Stack<ClientGameObject>();
		private List<int>				removedGameObjects = new List<int>();

		private List<KeyValuePair<int, float>>	blockedRequestChannels = new List<KeyValuePair<int, float>>();

		private List<int>											reuseList = new List<int>();
		private Dictionary<NGRemoteInspectorWindow, int>			watchersGameObject = new Dictionary<NGRemoteInspectorWindow, int>();
		private Dictionary<NGRemoteInspectorWindow, int[]>			watchersMaterials = new Dictionary<NGRemoteInspectorWindow, int[]>();
		private Dictionary<NGRemoteStaticInspectorWindow, int[]>	watchersTypes = new Dictionary<NGRemoteStaticInspectorWindow, int[]>();
		
		[NonSerialized]
		public string[]		componentTypes;
		[NonSerialized]
		public ClientType[]	inspectableTypes;

		private double	nextAutoRequestHierarchyInterval;
		private double	nextTryConstructPrefabs;

		private Vector2		scrollPosition = new Vector2();
		private Rect		bodyRect = new Rect();
		private Rect		viewRect = new Rect();
		[NonSerialized]
		private GUIStyle	sceneStyle;

		private string					searchKeywords = string.Empty;
		private string[]				searchPatterns;
		private List<ClientGameObject>	filteredGameObjects = new List<ClientGameObject>();

		private string[]	requiredServices;
		private string[]	remoteServices;
		private string		cachedWarningPhrase;
		private bool		showDifferenteVersionWarning;

		private Vector2	dragOriginPosition;

		private bool				notifyAdOnce = false;
		private BigHierarchyState	notifyBigHierarchyOnce = BigHierarchyState.None;

		private Thread	connectingThread;

		private double	lastClick;

		[NonSerialized]
		private List<NGRemoteWindow>	remoteWindows = new List<NGRemoteWindow>();

		[NonSerialized]
		private List<PrefabConstruct>		pendingPrefabs = new List<PrefabConstruct>();
		public List<PrefabConstruct>		PendingPrefabs { get { return this.pendingPrefabs; } }
		private List<AssetImportParameters>	importingAssetsParams = new List<AssetImportParameters>();
		public List<AssetImportParameters>	ImportingAssetsParams { get { return this.importingAssetsParams; } }

		private Stack<Change>	changes = new Stack<Change>();
		private Stack<Change>	forwardChanges = new Stack<Change>();

		[MenuItem(Constants.MenuItemPath + NGRemoteHierarchyWindow.NormalTitle, priority = Constants.MenuItemPriority + 210), Hotkey(NGRemoteHierarchyWindow.NormalTitle)]
		public static void	Open()
		{
			Utility.OpenWindow<NGRemoteHierarchyWindow>(false, NGRemoteHierarchyWindow.ShortTitle, true, NGRemoteHierarchyWindow.HierarchyWindowType);
		}

		private static int	buffSize = 8;
		private static IEnumerator	sender;
		private static ByteBuffer	sendBuffer = new ByteBuffer(buffSize);
		private static IEnumerator	receiver;
		private static ByteBuffer	recvBuffer = new ByteBuffer(buffSize);

		[MenuItem("Test/Reset")]
		public static void Reset()
		{
			sender = null;
			sendBuffer = new ByteBuffer(buffSize);
			receiver = null;
			recvBuffer = new ByteBuffer(buffSize);
		}

		[MenuItem("Test/Send")]
		public static void Send()
		{
			if (sender == null)
				sender = ASend(sendBuffer);
			Debug.Log(sender.MoveNext());
		}

		[MenuItem("Test/Recv")]
		public static void Recv()
		{
			if (receiver == null)
				receiver = ARecv(recvBuffer);
			Debug.Log(receiver.MoveNext());
		}

		[MenuItem("Test/Copy")]
		public static void Copy()
		{
			sendBuffer.CopyBuffer(recvBuffer, sendBuffer.Length);
			sendBuffer.Clear();
		}

		private static IEnumerator ASend(ByteBuffer buffer)
		{
			var p = new PartialPacket()
			{
				targetNetworkId = 2,
				position = 13256,
			};
			NGDebug.Snapshot(p);

			foreach (var item in p.ProgressiveOut(buffer))
			{
				Debug.Log("Iterate send " + item);
				yield return null;
			}
			Debug.Log("Completed send");
			//buffer.Append(32);
			//Debug.Log("W 32");
			//yield return null;
			//buffer.Append(1.234F);
			//Debug.Log("W 1.234F");
			//yield return null;
		}

		private static IEnumerator ARecv(ByteBuffer buffer)
		{
			var p = new PartialPacket();

			foreach (var item in p.ProgressiveIn(buffer))
			{
				Debug.Log("Iterate recv");
				yield return null;
			}
			NGDebug.Snapshot(p);
		}

		protected virtual void	OnEnable()
		{
			Utility.RegisterWindow(this);
			Utility.RestoreIcon(this, NGRemoteHierarchyWindow.TitleColor);

			Metrics.UseTool(1); // NGRemoteScene

			NGChangeLogWindow.CheckLatestVersion(NGTools.NGRemoteScene.NGAssemblyInfo.Name);

			this.UpdateAllHierarchiesTitle();

			if (this.address == null)
				Utility.LoadEditorPref(this, NGEditorPrefs.GetPerProjectPrefix());

			this.requiredServices = new string[]
			{
				NGTools.NGAssemblyInfo.Name,
				NGTools.NGAssemblyInfo.Version,
				NGTools.NGRemoteScene.NGAssemblyInfo.Name,
				NGTools.NGRemoteScene.NGAssemblyInfo.Version
			};

			for (int i = 0; i < this.importingAssetsParams.Count; i++)
			{
				if (this.importingAssetsParams[i].type == null)
					this.importingAssetsParams.RemoveAt(i--);
			}

			ConnectionsManager.Executer.HandlePacket(PacketId.ServerHasDisconnect, this.Handle_Scene_HasDisconnected);
			ConnectionsManager.Executer.HandlePacket(PacketId.NotifyErrors, this.Handle_Scene_ErrorNotificationPacket);
			ConnectionsManager.Executer.HandlePacket(RemoteScenePacketId.Scene_NotifySceneChanged, this.Handle_Scene_ServerNotifySceneChange);
			ConnectionsManager.Executer.HandlePacket(RemoteScenePacketId.Class_NotifyFieldValueUpdated, this.Handle_Scene_NotifyFieldValueUpdated);
			ConnectionsManager.Executer.HandlePacket(RemoteScenePacketId.GameObject_NotifyGameObjectsDeleted, this.Handle_Scene_NotifyDeletedGameObjects);
			ConnectionsManager.Executer.HandlePacket(RemoteScenePacketId.Component_NotifyDeletedComponents, this.Handle_Scene_NotifyDeletedComponents);
			ConnectionsManager.Executer.HandlePacket(RemoteScenePacketId.Material_NotifyMaterialDataChanged, this.Handle_Scene_NotifyChangedMaterialData);
			ConnectionsManager.Executer.HandlePacket(RemoteScenePacketId.Material_NotifyMaterialPropertyUpdated, this.Handle_Scene_NotifyMaterialPropertyUpdated);
			ConnectionsManager.Executer.HandlePacket(RemoteScenePacketId.Material_NotifyMaterialVector2Updated, this.Handle_Scene_NotifyMaterialVector2Updated);
			ConnectionsManager.Executer.HandlePacket(RemoteScenePacketId.GameObject_NotifyComponentAdded, this.Handle_Scene_NotifyAddedComponent);
			ConnectionsManager.ClientClosed += this.OnClientClosed;
			ConnectionsManager.NewServer += this.RepaintOnServerUpdated;
			ConnectionsManager.UpdateServer += this.RepaintOnServerUpdated;
			ConnectionsManager.KillServer += this.RepaintOnServerUpdated;

			EditorApplication.projectWindowItemOnGUI += this.ProjectWindowItemCallback;
			EditorApplication.update += this.NetworkUpdate;

			Utility.RepaintEditorWindow(typeof(NGRemoteWindow));
		}

		private void	ProjectWindowItemCallback(string guid, Rect r)
		{
			r.xMin = 0F;

			if (Event.current.type == EventType.Repaint)
			{
				if (r.Contains(Event.current.mousePosition) == true && DragAndDrop.GetGenericData("r") is UnityObject && DragAndDrop.visualMode == DragAndDropVisualMode.Copy)
					Utility.DrawUnfillRect(r, Color.yellow);
			}
			else if (Event.current.type == EventType.DragUpdated)
			{
				if (r.Contains(Event.current.mousePosition) == true && DragAndDrop.GetGenericData("r") is UnityObject)
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
			}
			else if (Event.current.type == EventType.DragPerform &&
					 r.Contains(Event.current.mousePosition) == true)
			{
				UnityObject	unityObject = (UnityObject)DragAndDrop.GetGenericData("r");

				if (unityObject != null)
				{
					DragAndDrop.AcceptDrag();

					this.PreparePrefab(AssetDatabase.GUIDToAssetPath(guid), this.GetGameObject(unityObject.gameObjectInstanceID));

					ImportAssetsWindow.Open(this);

					Event.current.Use();
				}
			}
		}

		public void	ImportAsset(string path, Type type, int instanceID, int gameObjectInstanceID = 0, int componentInstanceID = 0, UnityEngine.Object localAsset = null)
		{
			int	j = 0;

			for (; j < this.importingAssetsParams.Count; j++)
			{
				if (this.importingAssetsParams[j].instanceID == instanceID)
					break;
			}

			if (j == this.importingAssetsParams.Count)
			{
				Debug.Log("B");
				this.importingAssetsParams.Add(new AssetImportParameters(this, path, type, gameObjectInstanceID, componentInstanceID, instanceID, RemoteUtility.GetImportAssetTypeSupported(type) != null, localAsset));
			}

			ImportAssetsWindow.Open(this);
		}

		private void	PreparePrefab(string path, ClientGameObject gameObject)
		{
			this.pendingPrefabs.Add(new PrefabConstruct(path, gameObject));
		}

		private void	ConstructPrefab(PrefabConstruct prefab)
		{
			try
			{
				string		outputPath = prefab.path;
				GameObject	go = this.TryConstructGameObject(null, prefab.rootGameObject.gameObject, prefab);

				if (Directory.Exists(outputPath) == false)
					outputPath = Path.GetDirectoryName(outputPath);

				string	finalPath = outputPath + "/" + go.name + ".prefab";

				// Check for copies.
				if (this.overridePrefab == false && File.Exists(finalPath) == true)
				{
					int	n = 1;

					finalPath = outputPath + "/" + go.name + ' ' + n + ".prefab";

					while (File.Exists(finalPath) == true)
					{
						finalPath = outputPath + "/" + go.name + ' ' + n + ".prefab";
						++n;
					}
				}

				if (PrefabUtility.CreatePrefab(finalPath, go) != null)
					prefab.outputPath = finalPath;
				else
				{
					prefab.constructionError = "Prefab could not be created at \"" + finalPath + "\". Check logs.";
					InternalNGDebug.LogError(prefab.constructionError);
				}

				GameObject.DestroyImmediate(go);
			}
			catch (IncompleteGameObjectException ex)
			{
				//if (prefab == null)
				//{
				//	for (int i = 0; i < this.pendingPrefabs.Count; i++)
				//	{
				//		if (this.pendingPrefabs[i].path == path &&
				//			this.pendingPrefabs[i].rootGameObject.gameObject == gameObject)
				//		{
				//			prefab = this.pendingPrefabs[i];
				//			break;
				//		}
				//	}

				//	if (prefab == null)
				//	{
				//		prefab = new PrefabConstruct(path, gameObject);
				//		this.pendingPrefabs.Add(prefab);
				//	}
				//}

				if (ex.types != null)
				{
					for (int i = 0; i < ex.types.Count; i++)
					{
						// A Component contains a new asset that is not registered in the local database yet.
						//InternalNGDebug.Log(ex.types[i].FullName + " " + ex.instanceIDs[i] + " " + this.IsChannelBlocked(ex.types[i].GetHashCode()));
						if (this.IsChannelBlocked(ex.types[i].GetHashCode()) == false)
							this.LoadResources(ex.types[i], true);

						int	j = 0;

						//if (prefab != null)
						//{
						//	for (; j < this.importingAssetsParams.Count; j++)
						//	{
						//		if (this.importingAssetsParams[j].type == ex.types[i] &&
						//			this.importingAssetsParams[j].instanceID == ex.instanceIDs[i])
						//		{
						//			if (prefab.importParameters.Contains(this.importingAssetsParams[j]) == false)
						//				prefab.importParameters.Add(this.importingAssetsParams[j]);
						//			//this.importingAssetsParams[j].originPath = ex.paths[i];
						//			break;
						//		}
						//	}
						//}

						// TODO Verify NeedGenerate still useful?
						if (ex.needGenerate[i] == true)
						{
							if (j == this.importingAssetsParams.Count)
							{
								Debug.Log("A");
								AssetImportParameters	importParameters = new AssetImportParameters(this, ex.paths[i], ex.types[i], ex.gameObjectInstanceIDs[i], ex.componentInstanceIDs[i], ex.instanceIDs[i], RemoteUtility.GetImportAssetTypeSupported(ex.types[i]) != null, null) { prefabPath = Path.GetDirectoryName(prefab.path) };

								if (prefab != null)
									prefab.importParameters.Add(importParameters);

								this.importingAssetsParams.Add(importParameters);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException(ex);
			}
		}

		private GameObject	TryConstructGameObject(Transform parent, ClientGameObject gameObject, PrefabConstruct prefab)
		{
			GameObject						go = new GameObject(gameObject.name);
			string							name = go.name;
			IncompleteGameObjectException	incompleteEx = null;

			if (gameObject.components == null)
			{
				gameObject.RequestComponents(this.Client);
				incompleteEx = new IncompleteGameObjectException();
			}
			else
			{
				for (int i = 0; i < gameObject.components.Count; i++)
				{
					try
					{
						if (gameObject.components[i].CopyComponentToGameObjectAndClipboard(go) == false)
						{
							name = go.name + " (Broken)";
							InternalNGDebug.LogError("Creating Component \"" + gameObject.components[i].name + "\" from GameObject \"" + gameObject.name + "\" failed.");
						}
					}
					catch (IncompleteGameObjectException ex)
					{
						if (incompleteEx == null)
							incompleteEx = ex;
						else
							incompleteEx.Aggregate(ex);
					}
				}
			}

			for (int i = 0; i < gameObject.children.Count; i++)
			{
				try
				{
					this.TryConstructGameObject(go.transform, gameObject.children[i], prefab);
				}
				catch (IncompleteGameObjectException ex)
				{
					if (incompleteEx == null)
						incompleteEx = ex;
					else
						incompleteEx.Aggregate(ex);
				}
			}

			if (incompleteEx != null)
			{
				GameObject.DestroyImmediate(go);
				throw incompleteEx;
			}

			go.name = name;
			go.tag = gameObject.tag;
			go.layer = gameObject.layer;
			go.isStatic = gameObject.isStatic;
			go.transform.SetParent(parent);

			return go;
		}

		protected virtual void	OnDisable()
		{
			Utility.UnregisterWindow(this);
			Utility.SaveEditorPref(this, NGEditorPrefs.GetPerProjectPrefix());

			this.UpdateAllHierarchiesTitle(this);

			if (this.IsClientConnected() == true)
				this.CloseClient();

			ConnectionsManager.Executer.UnhandlePacket(PacketId.ServerHasDisconnect, this.Handle_Scene_HasDisconnected);
			ConnectionsManager.Executer.UnhandlePacket(PacketId.NotifyErrors, this.Handle_Scene_ErrorNotificationPacket);
			ConnectionsManager.Executer.UnhandlePacket(RemoteScenePacketId.Scene_NotifySceneChanged, this.Handle_Scene_ServerNotifySceneChange);
			ConnectionsManager.Executer.UnhandlePacket(RemoteScenePacketId.Class_NotifyFieldValueUpdated, this.Handle_Scene_NotifyFieldValueUpdated);
			ConnectionsManager.Executer.UnhandlePacket(RemoteScenePacketId.GameObject_NotifyGameObjectsDeleted, this.Handle_Scene_NotifyDeletedGameObjects);
			ConnectionsManager.Executer.UnhandlePacket(RemoteScenePacketId.Component_NotifyDeletedComponents, this.Handle_Scene_NotifyDeletedComponents);
			ConnectionsManager.Executer.UnhandlePacket(RemoteScenePacketId.Material_NotifyMaterialDataChanged, this.Handle_Scene_NotifyChangedMaterialData);
			ConnectionsManager.Executer.UnhandlePacket(RemoteScenePacketId.Material_NotifyMaterialPropertyUpdated, this.Handle_Scene_NotifyMaterialPropertyUpdated);
			ConnectionsManager.Executer.UnhandlePacket(RemoteScenePacketId.Material_NotifyMaterialVector2Updated, this.Handle_Scene_NotifyMaterialVector2Updated);
			ConnectionsManager.Executer.UnhandlePacket(RemoteScenePacketId.GameObject_NotifyComponentAdded, this.Handle_Scene_NotifyAddedComponent);
			ConnectionsManager.ClientClosed -= this.OnClientClosed;
			ConnectionsManager.NewServer -= this.RepaintOnServerUpdated;
			ConnectionsManager.UpdateServer -= this.RepaintOnServerUpdated;
			ConnectionsManager.KillServer -= this.RepaintOnServerUpdated;

			EditorApplication.projectWindowItemOnGUI -= this.ProjectWindowItemCallback;
			EditorApplication.update -= this.NetworkUpdate;

			Utility.RepaintEditorWindow(typeof(NGRemoteWindow));
		}

		//private int h = 0;
		protected virtual void	OnGUI()
		{
			/*EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Toggle(h == 0, "test", "GUIEditor.BreadcrumbLeft");
				GUILayout.Toggle(h == 1, "toast", "GUIEditor.BreadcrumbMid");
			}
			EditorGUILayout.EndHorizontal();

			h = EditorGUILayout.IntSlider("h", h, 0, 2);*/

			FreeLicenseOverlay.First(this, NGTools.NGRemoteScene.NGAssemblyInfo.Name + " Pro", NGRemoteWindow.Title + " is exclusive to NG Tools Pro.\n\nFree version is restrained to read-only.");

			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				if (GUILayout.Button("☰", "GV Gizmo DropDown", GUILayoutOptionPool.ExpandWidthFalse) == true)
					PopupWindow.Show(new Rect(0F, 16F, 0F, 0F), new OptionsPopup(this));

				List<NGServerInstance>	servers = ConnectionsManager.Servers;

				lock (ConnectionsManager.Servers)
				{
					EditorGUI.BeginDisabledGroup(this.connectingThread != null || servers.Count == 0);
					{
						if (servers.Count == 0)
							Utility.content.text = "No server";
						else if (servers.Count == 1)
							Utility.content.text = "1 server";
						else
							Utility.content.text = servers.Count + " servers";

						Rect	r = GUILayoutUtility.GetRect(Utility.content, GeneralStyles.ToolbarDropDown);

						if (GUI.Button(r, Utility.content, GeneralStyles.ToolbarDropDown) == true)
							PopupWindow.Show(new Rect(0F, 16F, 0F, 0F), new ServersSelectorWindow(this));
					}
					EditorGUI.EndDisabledGroup();
				}

				EditorGUI.BeginDisabledGroup(this.connectingThread != null || (this.Client != null && this.Client.tcpClient.Connected == true));
				{
					this.address = EditorGUILayout.TextField(this.address, GeneralStyles.ToolbarTextField, GUILayoutOptionPool.MinWidth(50F), GUILayoutOptionPool.ExpandWidthTrue);
					if  (string.IsNullOrEmpty(this.address) == true)
					{
						Rect	r = GUILayoutUtility.GetLastRect();
						EditorGUI.LabelField(r, LC.G("NGHierarchy_Address"), GeneralStyles.TextFieldPlaceHolder);
					}

					string	port = this.port.ToString();
					if (port == "0")
						port = string.Empty;
					EditorGUI.BeginChangeCheck();
					port = EditorGUILayout.TextField(port, GeneralStyles.ToolbarTextField, GUILayoutOptionPool.MaxWidth(40F));
					if (EditorGUI.EndChangeCheck() == true)
					{
						try
						{
							if (string.IsNullOrEmpty(port) == false)
								this.port = Mathf.Clamp(int.Parse(port), 0, UInt16.MaxValue - 1);
							else
								this.port = 0;
						}
						catch
						{
							this.port = 0;
							GUI.FocusControl(null);
						}
					}

					if ((port == string.Empty || port == "0") && this.port == 0)
					{
						Rect	r = GUILayoutUtility.GetLastRect();
						EditorGUI.LabelField(r, LC.G("NGHierarchy_Port"), GeneralStyles.TextFieldPlaceHolder);
					}
				}
				EditorGUI.EndDisabledGroup();

				GUILayout.FlexibleSpace();

				if (this.IsClientConnected() == true)
				{
					if (GUILayout.Button(LC.G("NGHierarchy_Disconnect"), GeneralStyles.ToolbarButton) == true)
						this.CloseClient();
				}
				else
				{
					if (this.connectingThread == null)
					{
						EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(this.address) || this.port <= 0);
						if (GUILayout.Button(LC.G("NGHierarchy_Connect"), GeneralStyles.ToolbarButton, GUILayoutOptionPool.Width(80F)) == true)
							this.Connect(this.address, this.port);
						EditorGUI.EndDisabledGroup();
						XGUIHighlightManager.DrawHighlightLayout(NGRemoteHierarchyWindow.NormalTitle + ".Connect", this);
					}
					else
					{
						Utility.content.text = "Connecting";
						Utility.content.image = GeneralStyles.StatusWheel.image;
						GUILayout.Label(Utility.content, GeneralStyles.ToolbarButton, GUILayoutOptionPool.Width(90F));
						Utility.content.image = null;
						this.Repaint();

						if (GUILayout.Button("X", GeneralStyles.ToolbarButton, GUILayoutOptionPool.Width(16F)) == true)
						{
							this.connectingThread.Abort();
							this.connectingThread.Join(0);
							this.connectingThread = null;
						}
					}
				}
			}
			EditorGUILayout.EndHorizontal();

			if (this.showDifferenteVersionWarning == true &&
				string.IsNullOrEmpty(this.cachedWarningPhrase) == false)
			{
				EditorGUILayout.HelpBox(this.cachedWarningPhrase, MessageType.Warning);

				Rect	r = GUILayoutUtility.GetLastRect();
				r.xMin = r.xMax - 20F;
				r.yMin = r.yMax - 15F;

				if (GUI.Button(r, "X") == true)
					this.showDifferenteVersionWarning = false;
			}

			if (this.IsClientConnected() == false)
			{
				GUILayout.FlexibleSpace();

				if (GUILayout.Button(LC.G("NGRemote_NotConnected"), GeneralStyles.BigCenterText, GUILayoutOptionPool.ExpandHeightTrue))
					XGUIHighlightManager.Highlight(NGRemoteHierarchyWindow.NormalTitle + ".Connect");
				EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

				GUILayout.FlexibleSpace();

				FreeLicenseOverlay.Last(NGTools.NGRemoteScene.NGAssemblyInfo.Name + " Pro");
				return;
			}

			// Server status
			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				if (string.IsNullOrEmpty(this.cachedWarningPhrase) == false)
				{
					Rect	r = GUILayoutUtility.GetRect(0F, 16F, GUILayoutOptionPool.Width(16F));

					r.y += 2F;
					GUI.DrawTexture(r, UtilityResources.WarningIcon);
					if (Event.current.type == EventType.MouseDown &&
						r.Contains(Event.current.mousePosition) == true)
					{
						this.showDifferenteVersionWarning = !this.showDifferenteVersionWarning;
						Event.current.Use();
					}
				}

				GUILayout.Label(string.Format(LC.G("NGHierarchy_Latency"), ConnectionsManager.Servers.Find(s => s.client == this.Client).lastPing));
				GUILayout.Label(string.Format(LC.G("NGHierarchy_SentBytes"), this.Client.BytesSent) + " (" + (this.Client.BytesSent / 1000000L) + " MB) | " + string.Format(LC.G("NGHierarchy_ReceivedBytes"), this.Client.BytesReceived) + " (" + (this.Client.BytesReceived / 1000000L) + " MB)");
			}
			EditorGUILayout.EndHorizontal();

			if (this.serverSceneHasChanged == true)
			{
				EditorGUILayout.HelpBox("The scene has changed.", MessageType.Info);
				EditorGUILayout.BeginHorizontal();
				{
					if (GUILayout.Button("Refresh") == true)
					{
						this.RefreshHierarchy();
						this.serverSceneHasChanged = false;
					}

					if (GUILayout.Button("Hide") == true)
						this.serverSceneHasChanged = false;
				}
				EditorGUILayout.EndHorizontal();
			}

			if (this.notifyBigHierarchyOnce == BigHierarchyState.Display)
			{
				EditorGUILayout.BeginHorizontal();
				{
					EditorGUILayout.HelpBox("Hierarchy contains more than " + NGRemoteHierarchyWindow.MassiveHierarchyThreshold + " Transform, refreshing hierarchy automatically might impact the performance.", MessageType.Info);

					Rect	r = GUILayoutUtility.GetLastRect();
					r.x -= 2F;
					r.y -= 2F;
					r.width = 16F;
					r.height = 16F;
					if (GUI.Button(r, "X") == true)
						this.notifyBigHierarchyOnce = BigHierarchyState.Hidden;
				}
				EditorGUILayout.EndHorizontal();
			}

			if (this.pendingPrefabs.Count > 0)
			{
				Rect	r = GUILayoutUtility.GetRect(this.position.width, 32F);
				if (this.pendingPrefabs.Count == 1)
					EditorGUI.HelpBox(r, "1 prefab waiting for assets...", MessageType.Info);
				else
					EditorGUI.HelpBox(r, this.pendingPrefabs.Count + " prefabs waiting for assets...", MessageType.Info);
				r.y += 6F;
				r.x += 7F;
				GUI.Label(r, GeneralStyles.StatusWheel);

				this.Repaint();
			}

			if (this.importingAssetsParams.Count > 0)
			{
				if (GUILayout.Button("Import Assets Settings") == true)
					ImportAssetsWindow.Open(this);
				XGUIHighlightManager.DrawHighlightLayout(NGRemoteHierarchyWindow.NormalTitle + ".ImportAssetsParams", this);
			}

			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				EditorGUI.BeginChangeCheck();
				this.searchKeywords = EditorGUILayout.TextField(this.searchKeywords, GeneralStyles.ToolbarSearchTextField);
				if (EditorGUI.EndChangeCheck() == true)
				{
					this.searchPatterns = Utility.SplitKeywords(this.searchKeywords, ' ');
					this.UpdateFilteredIndexes();
				}

				if (GUILayout.Button("", GeneralStyles.ToolbarSearchCancelButton) == true)
				{
					this.searchKeywords = string.Empty;
					this.searchPatterns = Utility.SplitKeywords(this.searchKeywords, ' ');
					GUI.FocusControl(null);
					this.UpdateFilteredIndexes();
				}
			}
			EditorGUILayout.EndHorizontal();

			if (this.scenes.Count > 0)
			{
				if (this.sceneStyle == null)
					this.sceneStyle = new GUIStyle(GUI.skin.label);

				this.viewRect.height = 0F;

				this.bodyRect = GUILayoutUtility.GetLastRect();
				this.bodyRect.x = 0F;
				this.bodyRect.y += this.bodyRect.height;
				this.bodyRect.width = this.position.width;
				this.bodyRect.height = this.position.height - this.bodyRect.y;

				if (string.IsNullOrEmpty(this.searchKeywords) == false)
				{
					viewRect.height = this.filteredGameObjects.Count * Constants.SingleLineHeight;

					// Calcul parents height.

					ClientGameObject	last = this.GetLastSelected();

					if (last != null)
					{
						bodyRect.height -= Constants.SingleLineHeight;

						while (last.Parent != null)
						{
							bodyRect.height -= Constants.SingleLineHeight;

							last = last.Parent;
						}

						if (bodyRect.height < Constants.SingleLineHeight)
							bodyRect.height = Constants.SingleLineHeight;
					}
				}
				else
				{
					for (int i = 0, p = 0; i < this.scenes.Count; ++i, ++p)
						viewRect.height += this.GetSceneHeight(this.scenes[i]);
				}

				Rect	r = bodyRect;

				this.scrollPosition = GUI.BeginScrollView(r, this.scrollPosition, viewRect);
				{
					r.y = 0F;
					r.height = NGRemoteHierarchyWindow.GameObjectHeight;

					if (string.IsNullOrEmpty(this.searchKeywords) == false)
					{
						for (int i = 0, p = 0; i < this.filteredGameObjects.Count; ++i, ++p)
						{
							if (r.y + r.height <= this.scrollPosition.y)
							{
								r.y += r.height;
								continue;
							}

							r.width = this.position.width;
							r = this.DrawGameObject(r, ref p, this.filteredGameObjects[i], true);

							if (r.y - this.scrollPosition.y > bodyRect.height)
								break;
						}
					}
					else
					{
						for (int i = 0, p = 0; i < this.scenes.Count; ++i, ++p)
						{
							float	height = this.GetSceneHeight(this.scenes[i]);

							if (r.y + height <= this.scrollPosition.y)
							{
								r.y += height;
								continue;
							}

							r.width = this.position.width - (viewRect.height > bodyRect.height ? 15F : 0F);
							r.height = NGRemoteHierarchyWindow.SceneHeight;
							r = this.DrawScene(r, ref p, this.scenes[i]);

							if (r.y - this.scrollPosition.y > this.bodyRect.height)
								break;
						}
					}
				}
				GUI.EndScrollView();

				if (Event.current.type == EventType.KeyDown)
				{
					if (Event.current.keyCode == KeyCode.F)
					{
						ClientGameObject	selected = this.GetFirstSelected();

						if (selected != null)
						{
							this.searchKeywords = string.Empty;
							this.searchPatterns = Utility.SplitKeywords(this.searchKeywords, ' ');
							GUI.FocusControl(null);
							this.UpdateFilteredIndexes();

							this.PingObject(selected.instanceID);
							this.FitGameObjectInScrollbar(selected);

							Event.current.Use();
						}
					}
				}

				if (string.IsNullOrEmpty(this.searchKeywords) == false)
				{
					ClientGameObject	last = this.GetLastSelected();

					if (last != null)
					{
						bodyRect.y += bodyRect.height;

						if (Event.current.type == EventType.Repaint)
						{
							bodyRect.height = this.position.height - bodyRect.y;
							EditorGUI.DrawRect(bodyRect, NGRemoteHierarchyWindow.PathBackgroundColor);

							bodyRect.y += 2F;
							bodyRect.height = 1F;

							EditorGUI.DrawRect(bodyRect, Color.black);
						}

						bodyRect.height = Constants.SingleLineHeight;
						GUI.Label(bodyRect, " Path:");

						bodyRect.x += 16F;
						while (last.Parent != null)
						{
							bodyRect.y += Constants.SingleLineHeight;
							GUI.Label(bodyRect, last.name);

							last = last.Parent;
						}
					}

					FreeLicenseOverlay.Last(NGTools.NGRemoteScene.NGAssemblyInfo.Name + " Pro");

					return;
				}

				if (Event.current.type == EventType.MouseDown)
				{
					this.ClearSelection();
					GUI.FocusControl(null);
					Event.current.Use();
				}
				else if (Event.current.type == EventType.KeyDown)
				{
					if (Event.current.keyCode == KeyCode.RightArrow)
					{
						ClientGameObject	node = this.GetLastSelected();

						if (node != null)
						{
							if (node.children.Count > 0)
							{
								if (node.fold == true)
								{
									this.ClearSelection();
									node.children[0].Selected = true;
									this.FitGameObjectInScrollbar(node.children[0]);
								}
								else
									node.fold = true;
							}
						}

						Event.current.Use();
					}
					else if (Event.current.keyCode == KeyCode.LeftArrow)
					{
						ClientGameObject	node = this.GetLastSelected();

						if (node != null)
						{
							if (node.children.Count > 0)
							{
								if (node.fold == false)
								{
									if (node.Parent != null)
									{
										this.ClearSelection();
										node.Parent.Selected = true;
										this.FitGameObjectInScrollbar(node.Parent);
									}
								}
								else
									node.fold = false;
							}
							else
							{
								if (node.Parent != null)
								{
									this.ClearSelection();
									node.Parent.Selected = true;
									this.FitGameObjectInScrollbar(node.Parent);
								}
							}
						}

						Event.current.Use();
					}
					else if (Event.current.keyCode == KeyCode.Delete)
					{
						ClientDeleteGameObjectsPacket	packet = new ClientDeleteGameObjectsPacket();

						foreach (ClientGameObject element in this.EachSelectedGameObjects())
							packet.Add(element.instanceID);

						this.Client.AddPacket(packet);

						Event.current.Use();
					}
					else if (Event.current.keyCode == KeyCode.UpArrow)
					{
						this.SelectPrevious();

						Event.current.Use();
					}
					else if (Event.current.keyCode == KeyCode.DownArrow)
					{
						this.SelectNext();

						Event.current.Use();
					}
					else if (Event.current.keyCode == KeyCode.Home)
					{
						this.ClearSelection();

						if (this.scenes.Count > 0)
						{
							this.scenes[0].Selected = true;
							this.scrollPosition.y = 0F;
						}

						Event.current.Use();
					}
					else if (Event.current.keyCode == KeyCode.End)
					{
						this.ClearSelection();

						ClientGameObject	n = this.scenes[this.scenes.Count - 1].roots[this.scenes[this.scenes.Count - 1].roots.Count - 1];

						rewind:
						for (int i = n.children.Count - 1; i >= 0;)
						{
							if (n.children[i].fold == true)
							{
								n = n.children[i];
								goto rewind;
							}
							n = n.children[i];
							break;
						}

						n.Selected = true;
						this.scrollPosition.y = float.MaxValue;

						Event.current.Use();
					}
				}
			}

			FreeLicenseOverlay.Last(NGTools.NGRemoteScene.NGAssemblyInfo.Name + " Pro");
		}

		public void	Connect(string address, int port)
		{
			this.address = address;
			this.port = port;
			this.connectingThread = ConnectionsManager.OpenClient(this, new DefaultTcpClient(), address, port, this.OnClientConnected);
		}

		public bool	IsConnected(Client client)
		{
			return this.Client == client;
		}

		public void	CloseClient()
		{
			try
			{
				this.Repaint();

				InternalNGDebug.Assert(this.Client != null, NGRemoteHierarchyWindow.NormalTitle + " is closing a null Client!");

				this.Layers = null;
				this.serverSceneHasChanged = false;
				this.scenes.Clear();
				this.blockedRequestChannels.Clear();
				this.allGameObjectInstanceIDs.Clear();
				this.resourceNames.Clear();
				this.resourceInstanceIds.Clear();
				this.enumData.Clear();
				this.updateValueNotifications.Clear();
				this.materials.Clear();
				this.userAssets.Clear();

				this.inspectableTypes = null;

				this.watchersGameObject.Clear();
				this.watchersMaterials.Clear();
				this.watchersTypes.Clear();

				this.changes.Clear();
				this.forwardChanges.Clear();

				if (this.HierarchyDisconnected != null)
					this.HierarchyDisconnected();

				this.Client.AddPacket(new ClientHasDisconnectedPacket());

				ConnectionsManager.Close(this.Client, this);
			}
			finally
			{
				this.Client = null;
			}
		}

		public bool		AddRemoteWindow(NGRemoteWindow window)
		{
			if (this.remoteWindows.Contains(window) == false)
			{
				this.remoteWindows.Add(window);
				return true;
			}

			return false;
		}

		public bool		RemoveRemoteWindow(NGRemoteWindow window)
		{
			for (int i = 0; i < this.remoteWindows.Count; i++)
			{
				if (this.remoteWindows[i] == window)
				{
					this.remoteWindows.RemoveAt(i);
					return true;
				}
			}

			return false;
		}

		public bool		IsClientConnected()
		{
			return this.Client != null &&
				   this.Client.tcpClient.Connected == true;
		}

		public void		SetActiveScene(int index)
		{
			this.activeScene = index;
		}

		public void		SetScenes(NetScene[] scenes)
		{
			// TODO Protect from multi-thread.
			if (this.allGameObjectInstanceIDs.Count > 0)
			{
				++this.hierarchyUpdateCounter;

				for (int j = 0; j < scenes.Length; j++)
				{
					ClientScene	scene = this.scenes.Find(s => s.buildIndex == scenes[j].buildIndex);

					if (scene == null)
					{
						scene = new ClientScene(scenes[j], this);
						scene.lastHierarchyUpdate = this.hierarchyUpdateCounter;

						this.scenes.Insert(j, scene);

						for (int i = 0; i < scene.roots.Count; i++)
							this.RegisterGameObjectAndChildren(scene.roots[i]);
					}
					else
					{
						scene.lastHierarchyUpdate = this.hierarchyUpdateCounter;

						for (int i = 0; i < scenes[j].roots.Length; i++)
						{
							ClientGameObject	gameObject;

							if (this.allGameObjectInstanceIDs.TryGetValue(scenes[j].roots[i].instanceID, out gameObject) == true)
							{
								gameObject.UpdateHierarchy(null, scenes[j].roots[i]);

								gameObject.lastHierarchyUpdate = this.hierarchyUpdateCounter;

								if (scenes[j].roots[i].children.Length > 0)
									this.UpdateGameObject(scene, gameObject, scenes[j].roots[i]);
							}
							else
							{
								gameObject = new ClientGameObject(scene, null, scenes[j].roots[i], this);
								scene.roots.Add(gameObject);
								this.RegisterGameObjectAndChildren(gameObject);
							}
						}
					}
				}

				for (int i = 0; i < this.scenes.Count; i++)
				{
					if (this.scenes[i].lastHierarchyUpdate != this.hierarchyUpdateCounter)
					{
						this.scenes.RemoveAt(i);
						--i;
					}
				}

				foreach (var pair in this.allGameObjectInstanceIDs)
				{
					if (pair.Value.lastHierarchyUpdate != this.hierarchyUpdateCounter)
					{
						pair.Value.Destroy();
						this.removedGameObjects.Add(pair.Key);
					}
				}

				for (int i = 0; i < this.removedGameObjects.Count; i++)
					this.allGameObjectInstanceIDs.Remove(this.removedGameObjects[i]);

				this.removedGameObjects.Clear();

				ClientGameObject[]	objects = this.GetSelectedGameObjects();

				for (int i = 0; i < objects.Length; i++)
					this.PingObject(objects[i].instanceID);

				this.CheckMassiveHierarchyWarning();
			}
			else
			{
				this.scenes.Clear();

				for (int i = 0; i < scenes.Length; i++)
				{
					ClientScene	scene = new ClientScene(scenes[i], this);

					this.scenes.Add(scene);

					for (int j = 0; j < scene.roots.Count; j++)
						this.RegisterGameObjectAndChildren(scene.roots[j]);
				}

				this.CheckMassiveHierarchyWarning();
			}
		}

		public void		SetLayers(string[] layers)
		{
			// Shrink to the bare necessary.
			for (int i = ServerSendLayersPacket.MaxLayers - 1; i >= 0; --i)
			{
				if (string.IsNullOrEmpty(layers[i]) == false)
				{
					this.Layers = new string[i + 1];

					for (; i >= 0; --i)
						this.Layers[i] = layers[i];

					break;
				}
			}
		}

		private void	SetResources(Type type, string[] resourceNames, int[] instanceIDs)
		{
			if (this.resourceNames.ContainsKey(type) == true)
			{
				this.resourceNames[type] = resourceNames;
				this.resourceInstanceIds[type] = instanceIDs;
			}
			else
			{
				this.resourceNames.Add(type, resourceNames);
				this.resourceInstanceIds.Add(type, instanceIDs);
			}

			this.UnblockRequestChannel(type.GetHashCode());

			if (this.ResourcesUpdated != null)
				this.ResourcesUpdated(type, resourceNames, instanceIDs);

			ResourcesPickerWindow[]	pickers = Resources.FindObjectsOfTypeAll<ResourcesPickerWindow>();

			for (int i = 0; i < pickers.Length; i++)
				pickers[i].Repaint();
		}

		public void		NotifySceneChange()
		{
			this.serverSceneHasChanged = true;
		}

		public ClientGameObject	GetGameObject(int instanceID)
		{
			ClientGameObject	n = null;

			this.allGameObjectInstanceIDs.TryGetValue(instanceID, out n);
			return n;
		}

		public void		SelectGameObject(int instanceID)
		{
			ClientGameObject	n = this.GetGameObject(instanceID);

			if (n != null)
			{
				this.ClearSelection();
				n.Selected = true;

				this.FitGameObjectInScrollbar(n);

				NGRemoteInspectorWindow[]	inspectors = EditorWindow.FindObjectsOfType<NGRemoteInspectorWindow>();

				for (int i = 0; i < inspectors.Length; i++)
					inspectors[i].Repaint();

				this.Repaint();
			}
		}

		public ClientGameObject[]	GetSelectedGameObjects()
		{
			if (this.scenes == null || this.scenes.Exists(s => s.HasSelection == true) == false)
				return ClientGameObject.EmptyGameObjectArray;

			List<ClientGameObject>	selection = new List<ClientGameObject>();

			this.BrowseSelected(selection);

			return selection.ToArray();
		}

		public IEnumerable<ClientGameObject>	EachSelectedGameObjects()
		{
			if (this.scenes == null || this.scenes.Exists(s => s.HasSelection == true) == false)
				yield break;

			foreach (ClientGameObject element in this.BrowseEachSelected())
				yield return element;
		}

		public bool		SetSibling(int instanceID, int instanceIDParent, int siblingIndex)
		{
			ClientGameObject	a;
			ClientGameObject	b = null;

			if (this.allGameObjectInstanceIDs.TryGetValue(instanceID, out a) == true &&
				(instanceIDParent == -1 || this.allGameObjectInstanceIDs.TryGetValue(instanceIDParent, out b) == true))
			{
				if (instanceIDParent == -1)
					a.Parent = null;
				else
					a.Parent = b;

				a.SetSiblingIndex(siblingIndex);
			}
			else
			{
				InternalNGDebug.LogError(Errors.Scene_GameObjectNotFound, "GameObject (" + instanceID + " or " + instanceIDParent + ") was not found.");
				return false;
			}

			return true;
		}

		private float	GetSceneHeight(ClientScene scene, ClientGameObject stopChild = null)
		{
			float	height = NGRemoteHierarchyWindow.SceneHeight;

			for (int i = 0; i < scene.roots.Count; i++)
			{
				if (scene.roots[i] == stopChild)
					return height + NGRemoteHierarchyWindow.GameObjectHeight;

				if (scene.roots[i].fold == true)
				{
					try
					{
						height += this.GetGameObjectHeight(scene.roots[i], stopChild);
					}
					catch (OffsetException ex)
					{
						height += ex.offset;
						break;
					}
				}
				else
					height += NGRemoteHierarchyWindow.GameObjectHeight;
			}

			return height;
		}

		private float	GetGameObjectHeight(ClientGameObject gameObject, ClientGameObject stopChild)
		{
			float	height = NGRemoteHierarchyWindow.GameObjectHeight;

			for (int i = 0; i < gameObject.children.Count; i++)
			{
				if (gameObject.children[i] == stopChild)
					throw new OffsetException(height + NGRemoteHierarchyWindow.GameObjectHeight);

				if (gameObject.children[i].fold == true)
				{
					try
					{
						height += this.GetGameObjectHeight(gameObject.children[i], stopChild);
					}
					catch (OffsetException ex)
					{
						ex.offset += height;
						throw;
					}
				}
				else
					height += NGRemoteHierarchyWindow.GameObjectHeight;
			}

			return height;
		}

		public void		PingObject(int gameObjectInstanceId)
		{
			if (gameObjectInstanceId == 0)
				return;

			ClientGameObject	gameObject = this.GetGameObject(gameObjectInstanceId);

			if (gameObject == null)
				return;

			while (gameObject.Parent != null)
			{
				gameObject.Parent.fold = true;
				gameObject = gameObject.Parent;
			}

			AnimFloat	anim = new AnimFloat(1F, this.Repaint);
			anim.target = 0F;
			this.pings.Add(new KeyValuePair<AnimFloat, int>(anim, gameObjectInstanceId));

			this.Repaint();
		}

		/// <summary>Opens the window to pick an Unity Object in a list.</summary>
		/// <param name="type">Type inheriting from UnityEngine.Object.</param>
		/// <param name="valuePath">The path of the value to update.</param>
		/// <param name="packetGenerator">A callback that will create the adequate Packet using value's path and the new value as byte[].</param>
		public void		PickupResource(Type type, string valuePath, Func<string, byte[], Packet> packetGenerator, Action<ResponsePacket> onComplete, int initialInstanceID)
		{
			ResourcesPickerWindow.Init(this, type, valuePath, packetGenerator, onComplete, initialInstanceID);

			this.LoadResources(type);
		}

		/// <summary>Requests resources from server if not loaded yet. You can use it to prewarm.</summary>
		/// <param name="type"></param>
		/// <param name="forceRefresh"></param>
		public void		LoadResources(Type type, bool forceRefresh = false)
		{
			if ((forceRefresh == true || this.resourceNames.ContainsKey(type) == false) &&
				this.BlockRequestChannel(type.GetHashCode()) == true)
			{
				this.Client.AddPacket(new ClientRequestResourcesPacket(type, forceRefresh), p =>
				{
					this.UnblockRequestChannel(type.GetHashCode());

					if (p.CheckPacketStatus() == true)
					{
						ServerSendResourcesPacket	packet = p as ServerSendResourcesPacket;

						this.SetResources(packet.type, packet.resourceNames, packet.instanceIDs);
					}
				});
			}
		}

		public string[]	GetComponentTypes()
		{
			if (this.componentTypes == null && this.BlockRequestChannel(typeof(Component).GetHashCode()) == true)
				this.Client.AddPacket(new ClientRequestAllComponentTypesPacket(), this.OnAllComponentTypesReceived);

			return this.componentTypes;
		}

		public string	GetResourceName(Type type, int instanceID)
		{
			int[]	IDs;

			if (this.resourceInstanceIds.TryGetValue(type, out IDs) == true)
			{
				for (int i = 0; i < IDs.Length; i++)
				{
					if (IDs[i] == instanceID)
						return this.resourceNames[type][i];
				}
			}
			else if (this.BlockRequestChannel(type.GetHashCode()) == true)
			{
				this.Client.AddPacket(new ClientRequestResourcesPacket(type, false), p =>
				{
					this.UnblockRequestChannel(type.GetHashCode());

					if (p.CheckPacketStatus() == true)
					{
						ServerSendResourcesPacket	packet = p as ServerSendResourcesPacket;

						this.SetResources(packet.type, packet.resourceNames, packet.instanceIDs);
					}
				});
			}

			return null;
		}

		public ImportAssetState	GetAssetFromImportParameters(int instanceID, out UnityEngine.Object asset, bool autoGenerateImportParameters)
		{
			asset = null;

			for (int i = 0; i < this.importingAssetsParams.Count; i++)
			{
				AssetImportParameters	importParameters = this.importingAssetsParams[i];

				if (importParameters.instanceID == instanceID)
				{
					ImportAssetState	r = importParameters.CheckImportState();

					if (r == ImportAssetState.Ready)
						asset = importParameters.finalObject;

					return r;
				}
			}

			return ImportAssetState.DoesNotExist;
		}

		/// <summary>
		/// Gets resources' name and instanceID if they are available now. Loads resources if they are missing.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="resourceNames"></param>
		/// <param name="instanceIDs"></param>
		public void		GetResources(Type type, out string[] resourceNames, out int[] instanceIDs)
		{
			this.LoadResources(type);

			if (this.resourceNames.TryGetValue(type, out resourceNames) == true)
				this.resourceInstanceIds.TryGetValue(type, out instanceIDs);
			else
				instanceIDs = null;
		}

		public void		CreateLocalAssetFromData(Type realType, int instanceID, byte[] data, string exception)
		{
			InternalNGDebug.VerboseLog("Create " + realType + " " + instanceID);
			for (int i = 0; i < this.importingAssetsParams.Count; i++)
			{
				AssetImportParameters	importParameters = this.importingAssetsParams[i];

				if (importParameters.instanceID == instanceID)
				{
					importParameters.realType = realType;

					if (exception != null)
						importParameters.importErrorMessage = exception;
					else if (importParameters.importErrorMessage == null)
					{
						UnityEngine.Object	asset = null;

						//if (importParameters.importMode == AssetImportParameters.ImportMode.UseGUID)
						//	asset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(importParameters.guid), realType);
						//else 
						if (importParameters.importMode == ImportMode.Auto)
						{
							if (string.IsNullOrEmpty(importParameters.autoPath) == false)
							{
								IObjectImporter	importer = RemoteUtility.GetImportAssetTypeSupported(realType);

								if (importer != null)
								{
									try
									{
										ImportAssetResult	r = importer.ToAsset(data, importParameters.autoPath, out asset);

										if (r == ImportAssetResult.SavedToDisk)
										{
											AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
											asset = AssetDatabase.LoadAssetAtPath(importParameters.autoPath, realType);
											InternalNGDebug.Log("Asset created at \"" + importParameters.autoPath + "\".");
										}
										else if (r == ImportAssetResult.NeedCreateViaAssetDatabase)
										{
											if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(importParameters.autoPath)) == false)
												AssetDatabase.DeleteAsset(importParameters.autoPath);
											AssetDatabase.CreateAsset(asset, importParameters.autoPath);
											InternalNGDebug.Log("Asset created at \"" + importParameters.autoPath + "\".");
										}
										else if (r == ImportAssetResult.ImportFailure)
											importParameters.importErrorMessage = "Asset creation failed during import.";
									}
									catch (Exception ex)
									{
										InternalNGDebug.LogException(ex);
										importParameters.importErrorMessage = ex.ToString();
									}
								}
							}
						}
						else if (importParameters.importMode == ImportMode.RawCopy)
						{
							if (string.IsNullOrEmpty(importParameters.outputPath) == false)
							{
								IObjectImporter	importer = RemoteUtility.GetImportAssetTypeSupported(realType);

								if (importer != null)
								{
									ImportAssetResult	r = importer.ToAsset(data, importParameters.outputPath, out asset);

									if (r == ImportAssetResult.SavedToDisk)
									{
										AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
										asset = AssetDatabase.LoadAssetAtPath(importParameters.outputPath, realType);
									}
									else if (r == ImportAssetResult.NeedCreateViaAssetDatabase)
									{
										if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(importParameters.outputPath)) == false)
											AssetDatabase.DeleteAsset(importParameters.outputPath);
										AssetDatabase.CreateAsset(asset, importParameters.outputPath);
									}
								}
							}
						}

						importParameters.copyAsset = asset;
					}

					break;
				}
			}
		}

		public void	LoadUserAssets(Type type)
		{
			if (this.BlockRequestChannel(type.GetHashCode()) == true)
			{
				this.Client.AddPacket(new ClientRequestUserAssetsPacket(type), p =>
				{
					this.UnblockRequestChannel(type.GetHashCode());
					this.OnUserAssetsReceived(p);
				});
			}
		}

		public List<UserAsset>	GetUserAssets(Type type)
		{
			List<UserAsset>	assets;

			this.userAssets.TryGetValue(type, out assets);

			return assets;
		}

		public void		SendUserTexture2D(string name, byte[] raw, Action<UserAsset> onComplete)
		{
			this.Client.AddPacket(new ClientSendUserTexture2DPacket(name, raw), this.ClosureReceiveUserTexture(typeof(Texture2D), name, onComplete));
		}

		public void		SendUserSprite(string name, byte[] raw, Rect rect, Vector2 pivot, float pixelsPerUnit, Action<UserAsset> onComplete)
		{
			this.Client.AddPacket(new ClientSendUserSpritePacket(name, raw, rect, pivot, pixelsPerUnit), this.ClosureReceiveUserTexture(typeof(Sprite), name, onComplete));
		}

		public EnumData	GetEnumData(string type)
		{
			EnumData	enumData;

			if (this.enumData.TryGetValue(type, out enumData) == false)
			{
				if (this.BlockRequestChannel(type.GetHashCode()) == true)
				{
					this.Client.AddPacket(new ClientRequestEnumDataPacket(type), p =>
					{
						this.UnblockRequestChannel(type.GetHashCode());
						this.OnEnumDataReceived(p);
					});
				}
			}

			return enumData;
		}

		public void		DeleteGameObject(int instanceID)
		{
			ClientGameObject	go = this.GetGameObject(instanceID);

			if (go != null)
			{
				go.Destroy();
				this.allGameObjectInstanceIDs.Remove(instanceID);
			}

			this.Repaint();
		}

		public void		DeleteGameObjects(List<int> instanceIDs)
		{
			for (int i = 0; i < instanceIDs.Count; i++)
				this.DeleteGameObject(instanceIDs[i]);
		}

		public void		AddComponent(int instanceID, NetComponent netComponent)
		{
			ClientGameObject	go = this.GetGameObject(instanceID);

			if (go != null)
				go.AddComponent(netComponent);
		}

		public void		DeleteComponents(List<int> gameObjectInstanceIDs, List<int> instanceIDs)
		{
			for (int i = 0; i < gameObjectInstanceIDs.Count; i++)
			{
				ClientGameObject	go = this.GetGameObject(gameObjectInstanceIDs[i]);

				if (go != null)
					go.RemoveComponent(instanceIDs[i]);
			}
		}

		public string	GetGameObjectName(int instanceID)
		{
			ClientGameObject	go = this.GetGameObject(instanceID);

			if (go != null)
				return go.name;
			return string.Empty;
		}

		public string	GetBehaviourName(int gameObjectInstanceID, int instanceID)
		{
			ClientGameObject	go = this.GetGameObject(gameObjectInstanceID);

			if (go != null)
			{
				ClientComponent	b = go.GetComponent(instanceID);

				if (b != null)
					return b.name;
			}

			return string.Empty;
		}

		public void		FetchReadablePaths(string[] pathComponents, bool nicifyPath)
		{
			int	instanceID;

			if (pathComponents.Length < 1 || int.TryParse(pathComponents[0], out instanceID) == false)
				return;

			ClientGameObject	go = this.GetGameObject(instanceID);

			if (go == null)
				return;

			if (Conf.DebugMode != Conf.DebugState.None)
				pathComponents[0] = go.name + " (#" + pathComponents[0] + ")";
			else
				pathComponents[0] = go.name;

			if (pathComponents.Length < 2 || int.TryParse(pathComponents[1], out instanceID) == false)
				return;

			ClientComponent	c = go.GetComponent(instanceID);

			if (c == null)
				return;

			if (Conf.DebugMode != Conf.DebugState.None)
				pathComponents[1] = c.name + " (#" + pathComponents[1] + ")";
			else
				pathComponents[1] = c.name;

			if (pathComponents.Length < 3 || int.TryParse(pathComponents[2], out instanceID) == false)
				return;

			ClientField	f = c.fields[instanceID];

			if (f == null)
				return;

			pathComponents[2] = f.name;

			if (nicifyPath == true)
			{
				for (int i = 2; i < pathComponents.Length; i++)
					pathComponents[i] = Utility.NicifyVariableName(pathComponents[i]);
			}
		}

		public void	WatchGameObject(NGRemoteInspectorWindow inspector, ClientGameObject gameObject)
		{
			if (gameObject == null)
			{
				int	instanceID;

				if (this.watchersGameObject.TryGetValue(inspector, out instanceID) == true)
				{
					this.ClearUpdateNotificationsFromGameObject(instanceID);
					this.watchersGameObject.Remove(inspector);
				}
			}
			else if (this.watchersGameObject.ContainsKey(inspector) == false)
				this.watchersGameObject.Add(inspector, gameObject.instanceID);
			else
			{
				this.ClearUpdateNotificationsFromGameObject(this.watchersGameObject[inspector]);
				this.watchersGameObject[inspector] = gameObject.instanceID;
			}

			int[]	instanceIDs = new int[this.watchersGameObject.Count];

			this.watchersGameObject.Values.CopyTo(instanceIDs, 0);

			this.Client.AddPacket(new ClientWatchGameObjectsPacket(instanceIDs));
		}

		/// <summary>Sets the given <paramref name="materialIDs"/> for an <paramref name="inspector"/> to be monitored on the server. Previous IDs are discarded.</summary>
		/// <param name="inspector"></param>
		/// <param name="materialIDs"></param>
		public void		WatchMaterials(NGRemoteInspectorWindow inspector, params int[] materialIDs)
		{
			if (materialIDs.Length == 0)
			{
				if (this.watchersMaterials.ContainsKey(inspector) == true)
					this.watchersMaterials.Remove(inspector);
			}
			else
			{
				if (this.watchersMaterials.ContainsKey(inspector) == false)
					this.watchersMaterials.Add(inspector, materialIDs);
				else
					this.watchersMaterials[inspector] = materialIDs;
			}

			this.reuseList.Clear();

			foreach (int[] materialInstanceIDs in this.watchersMaterials.Values)
			{
				for (int i = 0; i < materialInstanceIDs.Length; i++)
				{
					if (this.reuseList.Contains(materialInstanceIDs[i]) == false)
						this.reuseList.Add(materialInstanceIDs[i]);
				}
			}

			this.Client.AddPacket(new ClientWatchMaterialsPacket(this.reuseList.ToArray()));
		}

		/// <summary>Sets the given <paramref name="typeIndexes"/> for an <paramref name="inspector"/> to be monitored on the server. Previous IDs are discarded.</summary>
		/// <param name="inspector"></param>
		/// <param name="typeIndexes"></param>
		public void		WatchTypes(NGRemoteStaticInspectorWindow inspector, params int[] typeIndexes)
		{
			if (typeIndexes.Length == 0)
			{
				if (this.watchersTypes.ContainsKey(inspector) == true)
					this.watchersTypes.Remove(inspector);
			}
			else
			{
				if (this.watchersTypes.ContainsKey(inspector) == false)
					this.watchersTypes.Add(inspector, typeIndexes);
				else
					this.watchersTypes[inspector] = typeIndexes;
			}

			this.reuseList.Clear();

			foreach (int[] indexes in this.watchersTypes.Values)
			{
				for (int i = 0; i < indexes.Length; i++)
				{
					if (this.reuseList.Contains(indexes[i]) == false)
						this.reuseList.Add(indexes[i]);
				}
			}

			this.Client.AddPacket(new ClientWatchTypesPacket(this.reuseList.ToArray()));
		}

		/// <summary>Defines if a given <paramref name="path"/> has been modified.</summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public NotificationPath	GetUpdateNotification(string path)
		{
			for (int i = 0; i < this.updateValueNotifications.Count; i++)
			{
				if (this.updateValueNotifications[i] == path)
				{
					this.updateValueNotifications.RemoveAt(i);
					return NotificationPath.Full;
				}

				if (this.updateValueNotifications[i].StartsWith(path) == true)
				{
					// If it starts with, we need to confirm it's a sub-part of the path and not just a string similarity.
					if (this.updateValueNotifications[i][path.Length] == NGServerScene.ValuePathSeparator)
						return NotificationPath.Partial;
				}
			}

			return NotificationPath.None;
		}

		/// <summary>Resolves the path and updates the value with <paramref name="rawValue"/>.</summary>
		/// <param name="valuePath"></param>
		/// <param name="rawValue"></param>
		/// <remarks>See <see cref="NGConsoleWindow.ServerSceneManager.UpdateFieldValue" /> for the server equivalent.</remarks>
		/// <exception cref="System.MissingFieldException">Thrown when an unknown field from ClientGameObject is being assigned.</exception>
		/// <exception cref="System.InvalidCastException">Thrown when an array is assigned but the array is not supported.</exception>
		/// <exception cref="System.ArgumentException">Thrown when the path seems to be not resolvable.</exception>
		public void		UpdateFieldValue(string valuePath, byte[] rawValue)
		{
			this.UnblockRequestChannel(valuePath.GetHashCode());

			string[]	paths = valuePath.Split(NGServerScene.ValuePathSeparator);

			try
			{
				// Check if update is dealing with Type.
				if ((paths[1][0] < '0' || paths[1][0] > '9') && paths[1][0] != NGServerScene.SpecialGameObjectSeparator && paths[1][0] != '-')
				{
					int	typeIndex = int.Parse(paths[0]);

					if (this.inspectableTypes[typeIndex].members != null)
					{
						for (int i = 0; i < this.inspectableTypes[typeIndex].members.Length; i++)
						{
							ClientStaticMember	member = this.inspectableTypes[typeIndex].members[i];

							if (member.name == paths[1])
							{
								if (paths.Length == 2)
								{
									// Resizing array.
									if (member.fieldType.IsUnityArray() == true && rawValue.Length == 4)
									{
										TypeHandler	typeHandler = TypeHandlersManager.GetTypeHandler(typeof(int));
										ByteBuffer	buffer = Utility.GetBBuffer(rawValue);
										int			newSize = (int)typeHandler.Deserialize(buffer, typeof(int));
										ArrayData	array = member.value as ArrayData;

										Utility.RestoreBBuffer(buffer);

										// TODO Handle list?
										if (array.isNull == true)
										{
											array.isNull = false;
											array.array = Array.CreateInstance(Utility.GetArraySubType(member.fieldType), newSize);
										}
										else
										{
											if (array.array.Length != newSize)
											{
												Array	newArray = Array.CreateInstance(array.array.GetType().GetElementType(), newSize);

												for (int k = 0; k < newSize && k < array.array.Length; k++)
													newArray.SetValue(array.array.GetValue(k), k);

												array.array = newArray;
											}
										}
									}
									else
									{
										TypeHandler	typeHandler = TypeHandlersManager.GetTypeHandler(member.fieldType);
										ByteBuffer	buffer = Utility.GetBBuffer(rawValue);

										member.value = typeHandler.Deserialize(buffer, member.fieldType);

										Utility.RestoreBBuffer(buffer);
									}
								}
								else
									this.SetValue(member, paths, 1, this.Resolve(member.fieldType, member, paths, 1, rawValue));

								this.AddUpdateNotification(valuePath);
								break;
							}
						}
					}

					return;
				}

				ClientGameObject	go = this.GetGameObject(int.Parse(paths[0]));
				if (go == null)
				{
					InternalNGDebug.LogError(Errors.Scene_ComponentNotFound, "GameObject (" + paths[0] + ") was not found.");
					return;
				}

				// Is a field for ClientGameObject.
				if (paths[1][0] == NGServerScene.SpecialGameObjectSeparator)
				{
					paths[1] = paths[1].Remove(0, 1);

					// No time to waste on abstracting ClientGameObject, maybe later.
					switch (paths[1])
					{
						case "tag":
						case "name":
							TypeHandler	typeHandler = TypeHandlersManager.GetTypeHandler(typeof(string));

							NGRemoteHierarchyWindow.Buffer.Clear();
							NGRemoteHierarchyWindow.Buffer.Append(rawValue);

							string	stringValue = typeHandler.Deserialize(NGRemoteHierarchyWindow.Buffer, typeof(string)) as string;

							if (paths[1] == "tag")
								go.tag = stringValue;
							else if (paths[1] == "name")
								go.name = stringValue;
							break;

						case "active":
						case "isStatic":
							typeHandler = TypeHandlersManager.GetTypeHandler(typeof(bool));

							NGRemoteHierarchyWindow.Buffer.Clear();
							NGRemoteHierarchyWindow.Buffer.Append(rawValue);

							bool	boolValue = (bool)typeHandler.Deserialize(NGRemoteHierarchyWindow.Buffer, typeof(bool));

							if (paths[1] == "active")
								go.active = boolValue;
							else if (paths[1] == "isStatic")
								go.isStatic = boolValue;
							break;

						case "layer":
							typeHandler = TypeHandlersManager.GetTypeHandler(typeof(int));

							NGRemoteHierarchyWindow.Buffer.Clear();
							NGRemoteHierarchyWindow.Buffer.Append(rawValue);

							go.layer = (int)typeHandler.Deserialize(NGRemoteHierarchyWindow.Buffer, typeof(int));
							break;

						default:
							throw new MissingFieldException(valuePath);
					}

					this.AddUpdateNotification(valuePath);
					return;
				}

				ClientComponent	b = go.GetComponent(int.Parse(paths[1]));
				if (b == null)
				{
					InternalNGDebug.LogError(Errors.Scene_ComponentNotFound, "Component (" + paths[1] + ") was not found.");
					return;
				}

				ClientField	f = b.fields[int.Parse(paths[2])];
				if (f == null)
				{
					InternalNGDebug.LogError(Errors.Scene_PathNotResolved, "Path (" + valuePath + ") is invalid.");
					return;
				}

				this.SetValue(f, paths, 2, this.Resolve(f.fieldType, f, paths, 2, rawValue));

				this.AddUpdateNotification(valuePath);
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException(Errors.Scene_Exception, "Path="+ valuePath, ex);
			}
		}

		/// <summary></summary>
		/// <param name="instanceID"></param>
		/// <param name="propertyName"></param>
		/// <param name="rawValue"></param>
		/// <exception cref="UnityEngine.MissingFieldException">Thrown when the property was not found in the material.</exception>
		public void		UpdateMaterialProperty(int instanceID, string propertyName, byte[] rawValue)
		{
			ClientMaterial	material;

			if (this.materials.TryGetValue(instanceID, out material) == true)
			{
				for (int i = 0; i < material.properties.Length; i++)
				{
					if (material.properties[i].name.Equals(propertyName) == true)
					{
						Type		type = null;
						TypeHandler	typeHandler;

						if (material.properties[i].type == NGShader.ShaderPropertyType.Color)
							type = typeof(Color);
						else if (material.properties[i].type == NGShader.ShaderPropertyType.Float ||
								 material.properties[i].type == NGShader.ShaderPropertyType.Range)
							type = typeof(float);
						else if (material.properties[i].type == NGShader.ShaderPropertyType.TexEnv)
							type = typeof(Texture);
						else if (material.properties[i].type == NGShader.ShaderPropertyType.Vector)
							type = typeof(Vector4);

						typeHandler = TypeHandlersManager.GetTypeHandler(type);
						InternalNGDebug.Assert(typeHandler != null, "TypeHandler for " + material.properties[i].name + " is not supported.");

						ByteBuffer	buffer = Utility.GetBBuffer(rawValue);

						if (material.properties[i].type == NGShader.ShaderPropertyType.Color)
							material.properties[i].colorValue = (Color)typeHandler.Deserialize(buffer, type);
						else if (material.properties[i].type == NGShader.ShaderPropertyType.Float ||
								 material.properties[i].type == NGShader.ShaderPropertyType.Range)
						{
							material.properties[i].floatValue = (float)typeHandler.Deserialize(buffer, type);
						}
						else if (material.properties[i].type == NGShader.ShaderPropertyType.TexEnv)
							material.properties[i].textureValue = (UnityObject)typeHandler.Deserialize(buffer, type);
						else if (material.properties[i].type == NGShader.ShaderPropertyType.Vector)
							material.properties[i].vectorValue = (Vector4)typeHandler.Deserialize(buffer, type);

						Utility.RestoreBBuffer(buffer);

						return;
					}
				}
			}

			throw new MissingFieldException("Material " + instanceID + " does not contain property \"" + propertyName + "\".");
		}

		/// <summary></summary>
		/// <param name="instanceID"></param>
		/// <param name="propertyName"></param>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <exception cref="UnityEngine.MissingFieldException">Thrown when the property was not found in the material.</exception>
		public void		UpdateMaterialVector2(int instanceID, string propertyName, Vector2 value, MaterialVector2Type type)
		{
			ClientMaterial	material;

			if (this.materials.TryGetValue(instanceID, out material) == true)
			{
				for (int i = 0; i < material.properties.Length; i++)
				{
					if (material.properties[i].name.Equals(propertyName) == true)
					{
						if (type == MaterialVector2Type.Offset)
							material.properties[i].textureOffset = value;
						else if (type == MaterialVector2Type.Scale)
							material.properties[i].textureScale = value;
						return;
					}
				}
			}

			throw new MissingFieldException("Material " + instanceID + " does not contain property \"" + propertyName + "\".");
		}

		public ClientMaterial	GetMaterial(int instanceID)
		{
			ClientMaterial	material = null;

			if (this.materials.TryGetValue(instanceID, out material) == false &&
				this.BlockRequestChannel(instanceID) == true)
			{
				this.Client.AddPacket(new ClientRequestMaterialDataPacket(instanceID), p =>
				{
					if (p.CheckPacketStatus() == true)
					{
						ServerSendMaterialDataPacket packet = p as ServerSendMaterialDataPacket;

						this.UnblockRequestChannel(packet.netMaterial.instanceID);
						this.CreateOrUpdateMaterialData(packet.netMaterial);
					}
					else if (p.errorCode == Errors.Server_MaterialNotFound)
						this.materials.Add(instanceID, new ClientMaterial(instanceID, "[Not Found]"));
				});
			}

			return material;
		}

		public void	CreateOrUpdateMaterialData(NetMaterial netMaterial)
		{
			this.UnblockRequestChannel(netMaterial.instanceID);

			if (this.materials.ContainsKey(netMaterial.instanceID) == false)
				this.materials.Add(netMaterial.instanceID, new ClientMaterial(netMaterial));
			else
				this.materials[netMaterial.instanceID].Reset(netMaterial);
		}

		public void	LoadBigArray(string arrayPath)
		{
			if (this.BlockRequestChannel(arrayPath.GetHashCode()) == true)
				this.Client.AddPacket(new ClientLoadBigArrayPacket(arrayPath));
		}

		/// <summary>
		/// Adds a packet into the client's queue only if pro version.
		/// </summary>
		/// <param name="packet"></param>
		public bool	AddPacket(Packet packet, Action<ResponsePacket> onComplete = null)
		{
			if (NGLicensesManager.IsPro(NGTools.NGRemoteScene.NGAssemblyInfo.Name + " Pro") == false)
			{
				if (this.notifyAdOnce == false)
				{
					this.notifyAdOnce = true;
					EditorUtility.DisplayDialog(Constants.PackageTitle, "NG Remote Scene is exclusive to NG Tools Pro or NG Remote Scene Pro.\n\nThe free version is read-only.\n\nIt allows to see everything but you can only toggle the GameObject's active state.", "OK");
				}

				return false;
			}

			if (this.PacketInterceptor == null || this.PacketInterceptor(packet) == true)
			{
				this.Client.AddPacket(packet, onComplete);
				return true;
			}

			return false;
		}

		public bool	CanUndo()
		{
			return this.changes.Count > 0;
		}

		public bool	CanRedo()
		{
			return this.forwardChanges.Count > 0;
		}

		public void	UndoChange()
		{
			if (this.changes.Count > 0)
			{
				Change		change = this.changes.Pop();
				TypeHandler	typeHandler = TypeHandlersManager.GetTypeHandler(change.type);

				this.AddPacket(new ClientUpdateFieldValuePacket(change.path, typeHandler.Serialize(change.type, change.value), typeHandler), this.OnFieldUpdated);

				this.forwardChanges.Push(change);
			}
		}

		public void	UnblockRequestChannel(int id)
		{
			for (int i = 0; i < this.blockedRequestChannels.Count; i++)
			{
				if (this.blockedRequestChannels[i].Value <= Time.realtimeSinceStartup ||
					this.blockedRequestChannels[i].Key == id)
				{
					this.blockedRequestChannels.RemoveAt(i);
					--i;
				}
			}
		}

		/// <summary>
		/// <para>Checks if a channel is free to use. Use it to prevent sending many times the same packets, like connection, requesting resources or else.</para>
		/// <para>Refer to NGHierarchyWindow.BlockRequestLifeTime for the lifetime.</para>
		/// </summary>
		/// <see cref="NGRemoteHierarchyWindow.BlockRequestLifeTime"/>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool	BlockRequestChannel(int id)
		{
			for (int i = 0; i < this.blockedRequestChannels.Count; i++)
			{
				if (this.blockedRequestChannels[i].Value <= Time.realtimeSinceStartup)
				{
					if (this.blockedRequestChannels[i].Key == id)
					{
						this.blockedRequestChannels[i] = new KeyValuePair<int, float>(id, Time.realtimeSinceStartup + NGRemoteHierarchyWindow.BlockRequestLifeTime);
						return true;
					}

					this.blockedRequestChannels.RemoveAt(i);
					--i;
					continue;
				}

				if (this.blockedRequestChannels[i].Key == id)
					return false;
			}

			this.blockedRequestChannels.Add(new KeyValuePair<int, float>(id, Time.realtimeSinceStartup + NGRemoteHierarchyWindow.BlockRequestLifeTime));

			return true;
		}

		/// <summary>
		/// Checks if a channel is free to use. Use it to prevent sending many times the same packets, like connection, requesting resources or else. Refer to NGHierarchyWindow.BlockRequestLifeTime for the lifetime.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool	IsChannelBlocked(int id)
		{
			for (int i = 0; i < this.blockedRequestChannels.Count; i++)
			{
				if (this.blockedRequestChannels[i].Value <= Time.realtimeSinceStartup)
				{
					if (this.blockedRequestChannels[i].Key == id)
					{
						this.blockedRequestChannels.RemoveAt(i);
						break;
					}

					this.blockedRequestChannels.RemoveAt(i);
					--i;
					continue;
				}

				if (this.blockedRequestChannels[i].Key == id)
					return true;
			}

			return false;
		}

		public void		LoadInspectableTypes(Action onComplete)
		{
			if (this.BlockRequestChannel(typeof(ClientRequestInspectableTypesPacket).GetHashCode()) == true)
				this.Client.AddPacket(new ClientRequestInspectableTypesPacket(), this.OnInspectableTypesReceived(onComplete));
		}

		public string	GetTypeName(int typeIndex)
		{
			return this.inspectableTypes[typeIndex].name;
		}

		public void	RedoChange()
		{
			if (this.forwardChanges.Count > 0)
			{
				Change		change = this.forwardChanges.Pop();
				TypeHandler	typeHandler = TypeHandlersManager.GetTypeHandler(change.type);
				byte[]		data = change.newValue as byte[];

				if (data == null)
					data = typeHandler.Serialize(change.type, change.newValue);

				this.AddPacket(new ClientUpdateFieldValuePacket(change.path, data, typeHandler), this.OnFieldUpdated);

				this.changes.Push(change);
			}
		}

		void	IUnityData.RecordChange(string path, Type type, object value, object newValue)
		{
			this.changes.Push(new Change() { path = path, type = type, value = value, newValue = newValue });
			this.forwardChanges.Clear();
		}

		private void	AddUpdateNotification(string path)
		{
			if (this.updateValueNotifications.Contains(path) == false)
				this.updateValueNotifications.Add(path);
		}

		private Dictionary<Type, Dictionary<string, FieldInfo>>		cachedTypesFields = new Dictionary<Type, Dictionary<string, FieldInfo>>();
		private Dictionary<Type, Dictionary<string, PropertyInfo>>	cachedTypesProperties = new Dictionary<Type, Dictionary<string, PropertyInfo>>();

		public object	SetValue(object instance, string[] paths, int i, object value)
		{
			ClientStaticMember	staticMember = instance as ClientStaticMember;
			if (staticMember != null)
			{
				staticMember.value = value;
				return staticMember;
			}

			ClientField	field = instance as ClientField;
			if (field != null)
			{
				field.value = value;
				return field;
			}

			ClientClass	genericClass = instance as ClientClass;
			if (genericClass != null)
			{
				genericClass.SetValue(paths[i], value);
				return genericClass;
			}

			Type		t = instance.GetType();
			FieldInfo	fieldInfo = this.TryGetField(t, paths[i]);

			if (fieldInfo != null)
			{
				fieldInfo.SetValue(instance, value);
				return instance;
			}

			PropertyInfo	propertyInfo = this.TryGetProperty(t, paths[i]);
			if (propertyInfo != null)
			{
				propertyInfo.SetValue(instance, value, null);
				return instance;
			}

			throw new Exception();
		}

		private	object	NextPath(object instance, string path)
		{
			ClientStaticMember	staticMember = instance as ClientStaticMember;
			if (staticMember != null)
				return staticMember.value;

			ClientField	field = instance as ClientField;
			if (field != null)
				return field.value;

			ClientClass	genericClass = instance as ClientClass;
			if (genericClass != null)
				return genericClass.GetValue(path);

			Type		t = instance.GetType();
			FieldInfo	fieldInfo = this.TryGetField(t, path);

			if (fieldInfo != null)
				return fieldInfo.GetValue(instance);

			PropertyInfo	propertyInfo = this.TryGetProperty(t, path);
			if (propertyInfo != null)
				return propertyInfo.GetValue(instance, null);

			throw new Exception("Part \"" + path + "\" is not in " + instance + ".");
		}

		private Type	GetPathType(object instance, string[] paths, int i)
		{
			ClientField	field = instance as ClientField;

			if (field != null)
				return field.fieldType;

			ClientClass	genericClass = instance as ClientClass;

			if (genericClass != null)
				return genericClass.GetType(paths[i]);

			Type		t = instance.GetType();
			FieldInfo	fieldInfo = this.TryGetField(t, paths[i]);

			if (fieldInfo != null)
				return fieldInfo.FieldType;

			PropertyInfo	propertyInfo = this.TryGetProperty(t, paths[i]);

			if (propertyInfo != null)
				return propertyInfo.PropertyType;

			throw new Exception("Path \"" + string.Join(NGServerScene.ValuePathSeparator.ToString(), paths) + "\" at " + i + " is not " + instance + ".");
		}

		private FieldInfo	TryGetField(Type t, string name)
		{
			Dictionary<string, FieldInfo>	fields;

			if (this.cachedTypesFields.TryGetValue(t, out fields) == false)
			{
				fields = new Dictionary<string, FieldInfo>();
				this.cachedTypesFields.Add(t, fields);
			}

			FieldInfo	fieldInfo;

			if (fields.TryGetValue(name, out fieldInfo) == false)
			{
				fieldInfo = t.GetField(name, InnerUtility.ExposedBindingFlags);
				fields.Add(name, fieldInfo);
			}

			return fieldInfo;
		}

		private PropertyInfo	TryGetProperty(Type t, string name)
		{
			Dictionary<string, PropertyInfo>	properties;

			if (this.cachedTypesProperties.TryGetValue(t, out properties) == false)
			{
				properties = new Dictionary<string, PropertyInfo>();
				this.cachedTypesProperties.Add(t, properties);
			}

			PropertyInfo	propertyInfo;

			if (properties.TryGetValue(name, out propertyInfo) == false)
			{
				propertyInfo = t.GetProperty(name, InnerUtility.ExposedBindingFlags);
				properties.Add(name, propertyInfo);
			}

			return propertyInfo;
		}

		private void	BrowseSelected(List<ClientGameObject> selection)
		{
			for (int i = 0; i < this.scenes.Count; i++)
			{
				if (this.scenes[i].HasSelection == false)
					continue;

				for (int j = 0; j < this.scenes[i].roots.Count; j++)
				{
					if (this.scenes[i].roots[j].HasSelection == true)
						this.BrowseSelected(this.scenes[i].roots[j], selection);
				}
			}
		}

		private IEnumerable<ClientGameObject>	BrowseEachSelected()
		{
			for (int i = 0; i < this.scenes.Count; i++)
			{
				if (this.scenes[i].HasSelection == false)
					continue;

				for (int j = 0; j < this.scenes[i].roots.Count; j++)
				{
					if (this.scenes[i].roots[j].HasSelection == true)
					{
						foreach (ClientGameObject element in this.BrowseEachSelected(this.scenes[i].roots[j]))
							yield return element;
					}
				}
			}
		}

		private void	BrowseSelected(ClientGameObject n, List<ClientGameObject> selection)
		{
			if (n.Selected == true)
				selection.Add(n);

			for (int i = 0; i < n.children.Count; i++)
			{
				if (n.children[i].HasSelection == true)
					this.BrowseSelected(n.children[i], selection);
			}
		}

		private IEnumerable<ClientGameObject>	BrowseEachSelected(ClientGameObject n)
		{
			if (n.Selected == true)
				yield return n;

			for (int i = 0; i < n.children.Count; i++)
			{
				if (n.children[i].HasSelection == true)
				{
					foreach (ClientGameObject element in this.BrowseEachSelected(n.children[i]))
						yield return element;
				}
			}
		}

		private ClientGameObject	GetFirstSelected()
		{
			for (int i = 0; i < this.scenes.Count; i++)
			{
				if (this.scenes[i].HasSelection == false)
					continue;

				for (int j = 0; j < this.scenes[i].roots.Count; j++)
				{
					ClientGameObject	n = this.scenes[i].roots[j];

					if (n.HasSelection == false)
						continue;

					rewind:

					if (n.Selected == true)
						return n;

					for (int k = 0; k < n.children.Count; k++)
					{
						if (n.children[k].HasSelection == false)
							continue;

						n = n.children[k];
						goto rewind;
					}
				}
			}

			return null;
		}

		private ClientGameObject	GetLastSelected()
		{
			for (int i = this.scenes.Count - 1; i >= 0; i--)
			{
				if (this.scenes[i].HasSelection == false)
					continue;

				for (int j = this.scenes[i].roots.Count - 1; j >= 0; j--)
				{
					ClientGameObject	n = this.scenes[i].roots[j];

					if (n.HasSelection == false)
						continue;

					rewind:

					if (n.Selected == true)
						return n;

					for (int k = n.children.Count - 1; k >= 0; --k)
					{
						if (n.children[k].HasSelection == false)
							continue;

						n = n.children[k];
						goto rewind;
					}
				}
			}

			return null;
		}

		private Rect	DrawScene(Rect r, ref int p, ClientScene scene, bool hideChildren = false)
		{
			if (Event.current.type == EventType.Repaint)
			{
				if (scene.Selected == true)
				{
					float	originX = r.x;

					r.x = 0F;
					r.width += originX;
					EditorGUI.DrawRect(r, NGRemoteHierarchyWindow.SelectedObjectBackgroundColor);
					r.width -= originX;
					r.x = originX;
				}
				else
				{
					float	originX = r.x;

					r.x = 0F;
					r.width += originX;
					EditorGUI.DrawRect(r, NGRemoteHierarchyWindow.SceneBackgroundColor);
					r.width -= originX;
					r.x = originX;
				}
			}

			float	originWidth = r.width;

			r.y += 1F;
			r.width = 16F;
			scene.fold = EditorGUI.Foldout(r, scene.fold, GUIContent.none);
			r.y -= 1F;
			r.x += r.width;

			if (this.activeScene == scene.buildIndex)
				this.sceneStyle.fontStyle = FontStyle.Bold;
			else
				this.sceneStyle.fontStyle = FontStyle.Normal;

			r.x -= 2F;
			GUI.DrawTexture(r, UtilityResources.UnityIcon, ScaleMode.ScaleToFit);
			r.x += r.width;

			r.width = originWidth - r.x;
			EditorGUI.LabelField(r, scene.name, this.sceneStyle);

			r.x = originWidth - 20F;
			r.y += 5F;
			r.width = 20F;
			if (r.Contains(Event.current.mousePosition) == true)
			{
				if (Event.current.type == EventType.MouseDown)
					this.SceneContextMenu(this.scenes.IndexOf(scene));
			}
			GUI.Label(r, GUIContent.none, "PaneOptions");
			r.y -= 5F;

			r.x = 16F;
			r.width = originWidth - 16F;

			if (r.Contains(Event.current.mousePosition) == true)
			{
				if (Event.current.type == EventType.MouseUp)
				{
					if (Event.current.control == false)
					{
						this.ClearSelection();
						scene.Selected = true;

						Event.current.Use();
					}
				}
				else if (Event.current.type == EventType.MouseDown)
				{
					if (Event.current.button == 1)
						this.SceneContextMenu(this.scenes.IndexOf(scene));
					else
					{
						if (Event.current.control == true)
							scene.Selected = !scene.Selected;

						GUI.FocusControl(null);

						Event.current.Use();
					}
				}
			}

			r.x = 0F;
			r.width = originWidth;
			r.y += r.height;

			if (hideChildren == false && scene.fold == true && scene.roots.Count > 0)
			{
				r.y -= 1F;
				r.height = 1F;
				EditorGUI.DrawRect(r, Color.black);
				r.y += 1F;
				r.height = 16F;

				r.x += 14F;
				r.width -= 14F;
				r.height = NGRemoteHierarchyWindow.GameObjectHeight;
				for (int i = 0; i < scene.roots.Count; i++)
				{
					++p;
					r = this.DrawGameObject(r, ref p, scene.roots[i]);

					if (r.y - this.scrollPosition.y > this.bodyRect.height)
						return r;
				}
				r.width += 16F;
				r.x -= 16F;
			}

			return r;
		}

		private void	SceneContextMenu(int index)
		{
			GenericMenu	menu = new GenericMenu();

			menu.AddItem(new GUIContent("Set Active Scene"), false, this.RequestSetActiveScene, index);

			menu.ShowAsContext();
		}

		private void	RequestSetActiveScene(object index)
		{
			this.Client.AddPacket(new ClientSetActiveScenePacket((int)index), this.OnActiveSceneReceived);
		}

		private void	ImportAsPrefab(object data)
		{
			ClientGameObject	go = data as ClientGameObject;
			string				path = EditorUtility.SaveFilePanelInProject(NGRemoteHierarchyWindow.NormalTitle, go.name + ".prefab", "prefab", "Save the prefab at", NGEditorPrefs.GetString(NGRemoteHierarchyWindow.LastPrefabImportPathKeyPref, "Assets"));

			if (string.IsNullOrEmpty(path) == false)
			{
				NGEditorPrefs.SetString(NGRemoteHierarchyWindow.LastPrefabImportPathKeyPref, Path.GetDirectoryName(path));

				this.PreparePrefab(path, go);
				ImportAssetsWindow.Open(this);
			}
		}

		private void	ContextDeleteGameObject()
		{
			ClientDeleteGameObjectsPacket	packet = new ClientDeleteGameObjectsPacket();
			ClientGameObject[]				selection = this.GetSelectedGameObjects();

			for (int i = 0; i < selection.Length; i++)
				packet.Add(selection[i].instanceID);

			this.Client.AddPacket(packet);
		}

		/// <summary></summary>
		/// <param name="r"></param>
		/// <param name="p"></param>
		/// <param name="gameObject"></param>
		/// <returns></returns>
		private Rect	DrawGameObject(Rect r, ref int p, ClientGameObject gameObject, bool hideChildren = false)
		{
			Rect	fullRect = r;
			fullRect.x = 0F;
			fullRect.width += r.x;

			if (Event.current.type == EventType.Repaint)
			{
				if (gameObject.Selected == true)
					EditorGUI.DrawRect(fullRect, NGRemoteHierarchyWindow.SelectedObjectBackgroundColor);

				for (int i = 0; i < this.pings.Count; i++)
				{
					if (this.pings[i].Value == gameObject.instanceID)
					{
						if (this.pings[i].Key.value > 0F)
						{
							float	x = r.x;
							float	width = r.width;

							r.x = 0F;
							r.width = this.position.width;
							EditorGUI.DrawRect(r, new Color(.3F, this.pings[i].Key.value, .3F, .5F));
							r.x = x;
							r.width = width;
						}
						else
						{
							this.pings.RemoveAt(i);
							--i;
							this.Repaint();
						}
					}
				}
			}

			if (hideChildren == false && gameObject.children.Count > 0)
			{
				float	originWidth = r.width;

				r.width = 16F;
				EditorGUI.BeginChangeCheck();
				gameObject.fold = EditorGUI.Foldout(r, gameObject.fold, GUIContent.none);
				if (EditorGUI.EndChangeCheck() == true)
				{
					if (Event.current.alt == true)
						this.SetFold(gameObject);
				}
				r.width = originWidth;
			}

			if (fullRect.Contains(Event.current.mousePosition) == true)
			{
				if (Event.current.type == EventType.MouseUp)
				{
					if (Event.current.button == 1)
					{
						GenericMenu	menu = new GenericMenu();

						menu.AddItem(new GUIContent("Import as prefab"), false, this.ImportAsPrefab, gameObject);

						if (this.GameObjectContextMenu != null)
							this.GameObjectContextMenu(gameObject, menu);

						menu.AddItem(new GUIContent("Delete"), false, this.ContextDeleteGameObject);

						menu.ShowAsContext();
					}
				}
				else if (Event.current.type == EventType.MouseDown)
				{
					if (Event.current.control == true)
						gameObject.Selected = !gameObject.Selected;
					else
					{
						this.ClearSelection();
						gameObject.Selected = true;
					}

					this.dragOriginPosition = Event.current.mousePosition;

					GUI.FocusControl(null);

					// Initialize drag data.
					DragAndDrop.PrepareStartDrag();

					UnityObject	unityObject = new UnityObject(typeof(GameObject), gameObject.instanceID);

					DragAndDrop.SetGenericData("r", unityObject);
					DragAndDrop.SetGenericData("p", p);
					DragAndDrop.objectReferences = new UnityEngine.Object[0];

					Event.current.Use();
				}

				if (DragAndDrop.GetGenericData("r") is UnityObject &&
					DragAndDrop.GetGenericData("p") is int)
				{
					if (Event.current.type == EventType.MouseDrag && (this.dragOriginPosition - Event.current.mousePosition).sqrMagnitude >= Constants.MinStartDragDistance)
					{
						DragAndDrop.StartDrag("Dragging Game Object");
						Event.current.Use();
					}
					else if (Event.current.type == EventType.DragUpdated)
					{
						if ((DragAndDrop.GetGenericData("r") as UnityObject).instanceID != gameObject.instanceID)
							DragAndDrop.visualMode = DragAndDropVisualMode.Move;
						else
							DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;

						Event.current.Use();
					}
					else if (Event.current.type == EventType.DragPerform)
					{
						DragAndDrop.AcceptDrag();

						UnityObject			dragItem = DragAndDrop.GetGenericData("r") as UnityObject;
						ClientGameObject	dragGameObject = this.GetGameObject(dragItem.instanceID);

						// Top
						if (Event.current.mousePosition.y - r.y <= 3F)
						{
							// Prevent dragging around itself.
							if (dragGameObject.Parent != gameObject.Parent || dragGameObject.GetSiblingIndex() != gameObject.GetSiblingIndex() - 1)
							{
								int	dragPosition = (int)DragAndDrop.GetGenericData("p");
								int	instanceIDParent = (gameObject.Parent != null) ? gameObject.Parent.instanceID : -1;

								this.RequestSetGameObjectParent(dragItem.instanceID, instanceIDParent, gameObject.GetSiblingIndex() - (dragPosition < p && dragGameObject.Parent == gameObject.Parent ? 1 : 0));
							}
						}
						// Bottom
						else if (Event.current.mousePosition.y - r.y >= 13F)
						{
							// Prevent dragging around itself.
							if (dragGameObject.Parent != gameObject.Parent || dragGameObject.GetSiblingIndex() != gameObject.GetSiblingIndex() + 1)
							{
								int	dragPosition = (int)DragAndDrop.GetGenericData("p");
								int	instanceIDParent = (gameObject.Parent != null) ? gameObject.Parent.instanceID : -1;

								this.RequestSetGameObjectParent(dragItem.instanceID, instanceIDParent, gameObject.GetSiblingIndex() + (dragPosition < p && dragGameObject.Parent == gameObject.Parent ? 0 : 1));
							}
						}
						else
							this.RequestSetGameObjectParent(dragItem.instanceID, gameObject.instanceID, int.MaxValue);
					}
					else if (Event.current.type == EventType.Repaint &&
							 DragAndDrop.visualMode == DragAndDropVisualMode.Move)
					{
						Rect	r2 = r;

						r2.width += r2.x;
						r2.x = 0F;

						// Top
						if (Event.current.mousePosition.y - r.y <= 3F)
						{
							r2.height = 2F;
							r2.y -= 1F;
							EditorGUI.DrawRect(r2, NGRemoteHierarchyWindow.DropBackgroundColor);
						}
						// Bottom
						else if (Event.current.mousePosition.y - r.y >= 13F)
						{
							r2.height = 2F;
							r2.y += 15F;
							EditorGUI.DrawRect(r2, NGRemoteHierarchyWindow.DropBackgroundColor);
						}
						else
							EditorGUI.DrawRect(r2, NGRemoteHierarchyWindow.DropBackgroundColor);
					}
				}
			}

			r.x += 13F;
			r.width -= 13F;
			EditorGUI.BeginDisabledGroup(gameObject.ActiveInHierarchy == false);
			EditorGUI.LabelField(r, gameObject.name);
			EditorGUI.EndDisabledGroup();
			r.width += r.x;
			r.x -= 13F;

			r.y += r.height;

			if (hideChildren == false && gameObject.fold == true && gameObject.children.Count > 0)
			{
				r.x += 14F;
				r.width -= 14F;
				for (int i = 0; i < gameObject.children.Count; i++)
				{
					++p;
					r = this.DrawGameObject(r, ref p, gameObject.children[i]);

					if (r.y - this.scrollPosition.y > this.bodyRect.height)
						return r;
				}
				r.width += 14F;
				r.x -= 14F;
			}

			return r;
		}

		private void	RequestSetGameObjectParent(int instanceID, int instanceIDParent, int siblingIndex)
		{
			this.Client.AddPacket(new ClientSetSiblingPacket(instanceID, instanceIDParent, siblingIndex), this.OnGameObjectMoveConfirmed);
		}

		private void	SetFold(ClientGameObject gameObject)
		{
			for (int i = 0; i < gameObject.children.Count; i++)
			{
				gameObject.children[i].fold = gameObject.fold;
				this.SetFold(gameObject.children[i]);
			}
		}

		private void	ClearUpdateNotificationsFromGameObject(int instanceID)
		{
			string	stringInstanceID = instanceID.ToString() + NGServerScene.ValuePathSeparator;

			for (int i = 0; i < this.updateValueNotifications.Count; i++)
			{
				if (this.updateValueNotifications[i].StartsWith(stringInstanceID) == true)
					this.updateValueNotifications.RemoveAt(i--);
			}
		}

		private void	ClearSelection()
		{
			for (int i = 0; i < this.scenes.Count; i++)
				this.scenes[i].ClearSelection();
		}

		private void	SelectPrevious()
		{
			ClientGameObject	node = this.GetFirstSelected();
			ClientGameObject	cursor = node;

			if (node == null)
				return;

			this.ClearSelection();
			
			if (cursor.Parent == null)
			{
				int	q = cursor.scene.roots.IndexOf(cursor);

				if (q - 1 >= 0)
					cursor = cursor.scene.roots[q - 1];
				else
				{
					cursor.Selected = true;
					this.FitGameObjectInScrollbar(cursor);
					return;
				}
			}
			else
			{
				int	p = cursor.Parent.children.IndexOf(cursor);

				if (p == 0)
				{
					cursor.Parent.Selected = true;
					this.FitGameObjectInScrollbar(cursor.Parent);
					return;
				}
				else
					cursor = cursor.Parent.children[p - 1];
			}

			parent:
			if (cursor.fold == true && cursor.children.Count > 0)
			{
				cursor = cursor.children[cursor.children.Count - 1];
				goto parent;
			}
			else
			{
				cursor.Selected = true;
				this.FitGameObjectInScrollbar(cursor);
			}
		}

		private void	SelectNext()
		{
			ClientGameObject	node = this.GetLastSelected();
			ClientGameObject	cursor = node;

			if (node == null)
				return;

			this.ClearSelection();

			if (cursor.fold == true && cursor.children.Count > 0)
			{
				cursor.children[0].Selected = true;
				this.FitGameObjectInScrollbar(cursor.children[0]);
				return;
			}

			parent:
			if (cursor.Parent == null)
			{
				int	q = cursor.scene.roots.IndexOf(cursor);

				if (q + 1 < cursor.scene.roots.Count)
				{
					cursor.scene.roots[q + 1].Selected = true;
					this.FitGameObjectInScrollbar(cursor.scene.roots[q + 1]);
				}
				else
				{
					cursor.Selected = true;
					this.FitGameObjectInScrollbar(cursor);
				}
			}
			else
			{
				int	p = cursor.Parent.children.IndexOf(cursor);

				if (p == cursor.Parent.children.Count - 1)
				{
					cursor = cursor.Parent;
					goto parent;
				}
				else
				{
					cursor.Parent.children[p + 1].Selected = true;
					this.FitGameObjectInScrollbar(cursor.Parent.children[p + 1]);
				}
			}
		}

		private void	FitGameObjectInScrollbar(ClientGameObject target)
		{
			float	yOffset = this.GetYOffset(target) - NGRemoteHierarchyWindow.GameObjectHeight;

			if (yOffset < this.scrollPosition.y)
				this.scrollPosition.y = yOffset;
			else if (yOffset + NGRemoteHierarchyWindow.GameObjectHeight >= this.scrollPosition.y + bodyRect.height)
				this.scrollPosition.y = yOffset + NGRemoteHierarchyWindow.GameObjectHeight - bodyRect.height;
		}

		private float	GetYOffset(ClientGameObject target)
		{
			float	y = 0F;

			for (int i = 0, p = 0; i < this.scenes.Count; ++i, ++p)
			{
				y += this.GetSceneHeight(this.scenes[i], target);

				if (target.scene == this.scenes[i])
					return y;
			}

			return y;
		}

		private void	RegisterGameObjectAndChildren(ClientGameObject node)
		{
			remainingRegisterGameObjects.Push(node);

			while (remainingRegisterGameObjects.Count > 0)
			{
				node = remainingRegisterGameObjects.Pop();

				InternalNGDebug.Assert(this.allGameObjectInstanceIDs.ContainsKey(node.instanceID) == false, "Registering GameObject " + node.instanceID + " which is already present.");

				node.lastHierarchyUpdate = this.hierarchyUpdateCounter;

				this.allGameObjectInstanceIDs.Add(node.instanceID, node);

				for (int i = 0; i < node.children.Count; i++)
					remainingRegisterGameObjects.Push(node.children[i]);
			}
		}

		private void	UpdateGameObject(ClientScene scene, ClientGameObject parent, NetGameObject node)
		{
			for (int i = 0; i < node.children.Length; i++)
			{
				ClientGameObject	element;

				if (this.allGameObjectInstanceIDs.TryGetValue(node.children[i].instanceID, out element) == true)
				{
					element.UpdateHierarchy(parent, node.children[i]);
					element.lastHierarchyUpdate = this.hierarchyUpdateCounter;

					if (node.children[i].children.Length > 0)
						this.UpdateGameObject(scene, element, node.children[i]);
				}
				else
				{
					element = new ClientGameObject(scene, parent, node.children[i], this);
					this.RegisterGameObjectAndChildren(element);
				}
			}
		}

		public object	Resolve(Type type, object instance, string[] paths, int i, byte[] rawValue)
		{
			if (i == paths.Length - 1)
			{
				// If an array resize.
				if (type.IsUnityArray() == true)
				{
					NGRemoteHierarchyWindow.Buffer.Clear();
					NGRemoteHierarchyWindow.Buffer.Append(rawValue);

					TypeHandler	typeHandler;
					ArrayData	array;

					if (rawValue.Length > 4)
					{
						typeHandler = TypeHandlersManager.GetArrayHandler();
						array = typeHandler.Deserialize(NGRemoteHierarchyWindow.Buffer, type) as ArrayData;
					}
					else
					{
						// TODO Handle list?
						array = this.NextPath(instance, paths[i]) as ArrayData;

						typeHandler = TypeHandlersManager.GetTypeHandler(typeof(int));
						int	newSize = (int)typeHandler.Deserialize(NGRemoteHierarchyWindow.Buffer, typeof(int));

						if (array.array == null)
							array.array = Array.CreateInstance(Utility.GetArraySubType(array.serverType), newSize);
						else if (array.array.Length != newSize)
						{
							Array	newArray = Array.CreateInstance(array.array.GetType().GetElementType(), newSize);

							for (int k = 0; k < newSize && k < array.array.Length; k++)
								newArray.SetValue(array.array.GetValue(k), k);

							this.SetValue(instance, paths, i, newArray);

							array.array = newArray;
						}
					}

					return array;
				}
				else
				{
					TypeHandler	typeHandler = TypeHandlersManager.GetTypeHandler(type);
					InternalNGDebug.Assert(typeHandler != null, "TypeHandler for type " + type + " does not exist.");

					NGRemoteHierarchyWindow.Buffer.Clear();
					NGRemoteHierarchyWindow.Buffer.Append(rawValue);

					object	currentValue = this.NextPath(instance, paths[i]);
					object	v = typeHandler.Deserialize(NGRemoteHierarchyWindow.Buffer, type);

					ClientClass	currentGenericClass = currentValue as ClientClass;

					if (currentGenericClass != null)
					{
						ClientClass	newGenericClass = v as ClientClass;

						currentGenericClass.SetAll(newGenericClass.fields);
					}
					else
					{
						Type	innerType = currentValue.GetType();

						if (innerType != typeof(string) &&
							(innerType.IsClass == true ||
							 innerType.IsStruct() == true))
						{
							ComponentExposer[]	exposers = ComponentExposersManager.GetComponentExposers(type);
							FieldInfo[]			fields = RemoteUtility.GetExposedFields(innerType, exposers);
							PropertyInfo[]		properties = RemoteUtility.GetExposedProperties(innerType, exposers);

							for (int j = 0; j < fields.Length; j++)
								fields[j].SetValue(currentValue, fields[j].GetValue(v));

							for (int j = 0; j < properties.Length; j++)
								properties[j].SetValue(currentValue, properties[j].GetValue(v, null), null);
						}
						else
							currentValue = v;
					}

					return currentValue;
				}
			}
			else if (type.IsUnityArray() == true) // Any Array or IList
			{
				ArrayData	array = this.NextPath(instance, paths[i]) as ArrayData;
				Type		subType = Utility.GetArraySubType(array.serverType);
				int			index = int.Parse(paths[i + 1]);

				if (array.array.Length > index)
					array.array.SetValue(this.ResolveArray(subType, array.array.GetValue(index), paths, i + 1, rawValue), index);

				return array;
			}
			else if (type.IsClass == true || // Any class.
					 type.IsStruct() == true) // Any struct.
			{
				object	subInstance = this.NextPath(instance, paths[i]);

				return this.SetValue(subInstance, paths, i + 1, this.Resolve(this.GetPathType(subInstance, paths, i + 1), subInstance, paths, i + 1, rawValue));
			}

			throw new InvalidOperationException("Resolve failed at " + i + " with " + instance + " of type " + type + ".");
		}

		private object	ResolveArray(Type type, object instance, string[] paths, int i, byte[] rawValue)
		{
			if (i == paths.Length - 1)
			{
				TypeHandler	typeHandler = TypeHandlersManager.GetTypeHandler(type);
				InternalNGDebug.Assert(typeHandler != null, "TypeHandler for type " + type + " does not exist.");

				NGRemoteHierarchyWindow.Buffer.Clear();
				NGRemoteHierarchyWindow.Buffer.Append(rawValue);

				return typeHandler.Deserialize(NGRemoteHierarchyWindow.Buffer, type);
			}

			if (type.IsClass == true || // Any class.
				type.IsStruct() == true) // Any struct.
			{
				return this.SetValue(instance, paths, i + 1, this.Resolve(this.GetPathType(instance, paths, i + 1), instance, paths, i + 1, rawValue));
			}

			return this.Resolve(this.GetPathType(instance, paths, i + 1), instance, paths, i + 1, rawValue);
		}

		/// <summary>
		/// Gets the height of a GameObject including its children.
		/// </summary>
		/// <param name="gameObject"></param>
		/// <returns></returns>
		private float	GetGameObjectHeight(ClientGameObject gameObject)
		{
			float	height = Constants.SingleLineHeight;

			if (gameObject.fold == true)
			{
				for (int i = 0; i < gameObject.children.Count; i++)
					height += this.GetGameObjectHeight(gameObject.children[i]);
			}

			return height;
		}

		private void	OnClientClosed(Client client)
		{
			if (this.Client == client)
				this.Client = null;
		}

		private void	RepaintOnServerUpdated(NGServerInstance instance)
		{
			EditorApplication.delayCall += this.Repaint;
		}

		private void	RefreshHierarchy()
		{
			this.Client.AddPacket(new ClientRequestHierarchyPacket(), this.OnHierarchyReceived);
		}

		private void	CheckMassiveHierarchyWarning()
		{
			if (this.autoRequestHierarchyInterval > 0D &&
				this.notifyBigHierarchyOnce <= BigHierarchyState.Display)
			{
				if (this.allGameObjectInstanceIDs.Count >= NGRemoteHierarchyWindow.MassiveHierarchyThreshold)
					this.notifyBigHierarchyOnce = BigHierarchyState.Display;
				else
					this.notifyBigHierarchyOnce = BigHierarchyState.None;
			}
		}

		private void	NetworkUpdate()
		{
			if (this.Client == null || this.IsClientConnected() == false)
				return;

			if (this.autoRequestHierarchyInterval > 0D && this.nextAutoRequestHierarchyInterval < EditorApplication.timeSinceStartup)
			{
				this.nextAutoRequestHierarchyInterval = float.MaxValue;
				this.RefreshHierarchy();
			}

			if (this.importingAssetsParams.Count > 0)
			{
				for (int i = 0; i < this.importingAssetsParams.Count; i++)
				{
					this.ImportingAssetsParams[i].CheckImportState();
					//if (this.importingAssetsParams[i].originPath == null)
					{
						//UnityEngine.Object	asset;

						//this.GetAssetFromImportParameters(this.importingAssetsParams[i].instanceID, out asset, false);
					}
				}
			}

			if (this.pendingPrefabs.Count > 0 && NGRemoteHierarchyWindow.TryConstructPrefabInterval > 0D && this.nextTryConstructPrefabs < EditorApplication.timeSinceStartup)
			{
				this.nextTryConstructPrefabs = EditorApplication.timeSinceStartup + NGRemoteHierarchyWindow.TryConstructPrefabInterval;

				for (int i = 0; i < this.pendingPrefabs.Count; i++)
				{
					if (this.pendingPrefabs[i].outputPath == null &&
						this.pendingPrefabs[i].constructionError == null &&
						this.pendingPrefabs[i].VerifyComponentsReady(this, this.importingAssetsParams, this.Client) == true)
					{
						this.ConstructPrefab(this.pendingPrefabs[i]);

						if (this.pendingPrefabs[i].outputPath != null)
						{
							EditorWindow	projectWindow = EditorWindow.GetWindow(NGRemoteHierarchyWindow.ProjectWindowType);
							string			prefabCreatedMessage = "Prefab \"" + Path.GetFileNameWithoutExtension(this.pendingPrefabs[i].outputPath) + "\" created.";

							InternalNGDebug.Log(prefabCreatedMessage, AssetDatabase.LoadAssetAtPath<GameObject>(this.pendingPrefabs[i].outputPath));
							projectWindow.ShowNotification(new GUIContent(prefabCreatedMessage));
						}
					}
				}
			}
		}

		private void	UpdateFilteredIndexes()
		{
			this.filteredGameObjects.Clear();

			foreach (ClientGameObject gameObject in this.allGameObjectInstanceIDs.Values)
			{
				int	i = 0;

				for (; i < this.searchPatterns.Length; i++)
				{
					if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(gameObject.name, this.searchPatterns[i], CompareOptions.IgnoreCase) < 0)
						break;
				}

				if (i == this.searchPatterns.Length)
					this.filteredGameObjects.Add(gameObject);
			}
		}

		private void	OnClientConnected(Client client)
		{
			this.connectingThread = null;

			if (client == null)
				return;

			this.Client = client;
			this.Client.debugPrefix = "H:";
			this.Client.AddPacket(new ClientRequestServicesPacket(), this.OnServerServicesReceived);

			this.remoteServices = null;
			this.cachedWarningPhrase = string.Empty;
			this.showDifferenteVersionWarning = true;

			this.serverSceneHasChanged = false;
			this.searchKeywords = string.Empty;

			this.nextTryConstructPrefabs = 0F;
			this.notifyBigHierarchyOnce = 0;

			this.pendingPrefabs.Clear();

			this.nextAutoRequestHierarchyInterval = double.MaxValue;

			for (int i = 0; i < this.importingAssetsParams.Count; i++)
			{
				if (this.importingAssetsParams[i].remember == false)
					this.importingAssetsParams.RemoveAt(i--);
			}

			this.notifyAdOnce = false;

			EditorApplication.delayCall += () =>
			{
				if (this.HierarchyConnected != null)
					this.HierarchyConnected();
			};
		}

		private void	OnLayersReceived(ResponsePacket p)
		{
			if (p.CheckPacketStatus() == true)
				this.SetLayers((p as ServerSendLayersPacket).layers);
		}

		private void	OnServerServicesReceived(ResponsePacket p)
		{
			if (p.CheckPacketStatus() == true)
			{
				ServerSendServicesPacket	packet = p as ServerSendServicesPacket;
				bool					allServicesPresent = true;
				bool					versionMismatch = false;

				this.remoteServices = new string[packet.services.Length << 1];

				for (int j = 0; j < packet.services.Length; ++j)
				{
					this.remoteServices[j << 1] = packet.services[j];
					this.remoteServices[(j << 1) + 1] = packet.versions[j];
				}

				for (int i = 0; i + 1 < this.requiredServices.Length; i += 2)
				{
					bool	serviceFound = false;

					for (int j = 0; j < packet.services.Length; j++)
					{
						if (this.requiredServices[i] == packet.services[j])
						{
							serviceFound = true;

							if (this.requiredServices[i + 1] != packet.versions[j])
								versionMismatch = true;
							break;
						}
					}

					if (serviceFound == false)
						allServicesPresent = false;
				}

				if (allServicesPresent == true)
				{
					if (versionMismatch == true)
						this.cachedWarningPhrase = "Required services (" + this.StringifyServices(this.requiredServices) + ") do not fully match the server services (" + this.StringifyServices(this.remoteServices) + ").\nThe behaviour might be unstable.";

					this.Client.AddPacket(new ClientRequestLayersPacket(), this.OnLayersReceived);
					this.RefreshHierarchy();
				}
				else
				{
					this.cachedWarningPhrase = "Server does not run required services (Requiring " + this.StringifyServices(this.requiredServices) + ", has " + this.StringifyServices(this.remoteServices) + "). Disconnecting from server.";
					this.CloseClient();
				}
			}
			else
			{
				InternalNGDebug.LogError("Server could not provide vital services. Can not continue, disconnecting from server.");
				this.CloseClient();
			}
		}

		private string	StringifyServices(string[] services)
		{
			StringBuilder	buffer = Utility.GetBuffer();

			for (int i = 0; i + 1 < services.Length; i += 2)
			{
				if (i > 0)
					buffer.Append(',');
				buffer.Append(services[i]);
				buffer.Append(':');
				buffer.Append(services[i + 1]);
			}

			return Utility.ReturnBuffer(buffer);
		}

		private void	OnHierarchyReceived(ResponsePacket p)
		{
			this.nextAutoRequestHierarchyInterval = EditorApplication.timeSinceStartup + this.autoRequestHierarchyInterval;

			if (p.CheckPacketStatus() == true)
			{
				ServerSendHierarchyPacket	packet = p as ServerSendHierarchyPacket;

				this.SetScenes(packet.clientScenes);
				this.SetActiveScene(packet.activeScene);
				this.Repaint();
			}
		}

		private void	OnAllComponentTypesReceived(ResponsePacket p)
		{
			this.UnblockRequestChannel(typeof(Behaviour).GetHashCode());

			if (p.CheckPacketStatus() == true)
				this.componentTypes = (p as ServerSendAllComponentTypesPacket).types;
		}

		private void	OnUserAssetsReceived(ResponsePacket p)
		{
			if (p.CheckPacketStatus() == true)
			{
				ServerSendUserAssetsPacket	packet = p as ServerSendUserAssetsPacket;
				List<UserAsset>				assets;

				if (this.userAssets.TryGetValue(packet.type, out assets) == false)
				{
					assets = new List<UserAsset>();
					this.userAssets.Add(packet.type, assets);
				}

				assets.Clear();

				for (int i = 0; i < packet.names.Count; i++)
					assets.Add(new UserAsset() { name = packet.names[i], instanceID = packet.instanceIDs[i], data = packet.data[i] });
			}
		}

		private Action<ResponsePacket>	ClosureReceiveUserTexture(Type type, string name, Action<UserAsset> onComplete)
		{
			return p =>
			{
				if (p.CheckPacketStatus() == true)
				{
					ServerAcknowledgeUserTexturePacket	packet = p as ServerAcknowledgeUserTexturePacket;
					List<UserAsset>						assets;

					if (this.userAssets.TryGetValue(type, out assets) == false)
					{
						assets = new List<UserAsset>(2);
						this.userAssets.Add(type, assets);
					}

					for (int i = 0; i < assets.Count; i++)
					{
						if (assets[i].name == packet.name)
							return;
					}

					UserAsset	asset = new UserAsset() { name = packet.name, instanceID = packet.instanceID, data = packet.data };

					assets.Add(asset);

					if (onComplete != null)
						onComplete(asset);
				}
			};
		}

		private void	OnEnumDataReceived(ResponsePacket p)
		{
			if (p.CheckPacketStatus() == true)
			{
				ServerSendEnumDataPacket	packet = p as ServerSendEnumDataPacket;

				this.enumData.Add(packet.type, new EnumData(packet.hasFlagAttribute, packet.names, packet.values));
			}
		}

		private void	OnActiveSceneReceived(ResponsePacket p)
		{
			if (p.CheckPacketStatus() == true)
				this.SetActiveScene((p as ServerSetActiveScenePacket).index);
		}

		private void	OnGameObjectMoveConfirmed(ResponsePacket p)
		{
			if (p.CheckPacketStatus() == true)
			{
				ServerSetSiblingPacket	packet = p as ServerSetSiblingPacket;

				this.SetSibling(packet.instanceID, packet.instanceIDParent, packet.siblingIndex);
			}
		}

		private Action<ResponsePacket>	OnInspectableTypesReceived(Action onComplete)
		{
			return p =>
			{
				this.UnblockRequestChannel(typeof(ClientRequestInspectableTypesPacket).GetHashCode());

				if (p.CheckPacketStatus() == true)
				{
					ServerSendInspectableTypesPacket	packet = p as ServerSendInspectableTypesPacket;

					this.inspectableTypes = new ClientType[packet.inspectableNames.Length];
					for (int i = 0; i < this.inspectableTypes.Length; i++)
						this.inspectableTypes[i] = new ClientType(i, packet.inspectableNames[i], packet.inspectableNamespaces[i]);

					onComplete();
				}
			};
		}

		private void	OnFieldUpdated(ResponsePacket p)
		{
			if (p.CheckPacketStatus() == true)
			{
				ServerUpdateFieldValuePacket	packet = p as ServerUpdateFieldValuePacket;

				this.UpdateFieldValue(packet.fieldPath, packet.rawValue);
			}
		}

		private void	Handle_Scene_HasDisconnected(Client sender, Packet _packet)
		{
			if (this.Client == sender)
				this.CloseClient();
		}

		private void	Handle_Scene_ErrorNotificationPacket(Client sender, Packet _packet)
		{
			ErrorNotificationPacket	packet = _packet as ErrorNotificationPacket;

			for (int i = 0; i < packet.errors.Count; i++)
				InternalNGDebug.Log(packet.errors[i], packet.messages[i]);
		}

		private void	Handle_Scene_ServerNotifySceneChange(Client sender, Packet _packet)
		{
			this.NotifySceneChange();
		}

		private void	Handle_Scene_NotifyFieldValueUpdated(Client sender, Packet _packet)
		{
			NotifyFieldValueUpdatedPacket	packet = _packet as NotifyFieldValueUpdatedPacket;

			this.UpdateFieldValue(packet.fieldPath, packet.rawValue);
		}

		private void	Handle_Scene_NotifyDeletedGameObjects(Client sender, Packet _packet)
		{
			NotifyGameObjectsDeletedPacket	packet = _packet as NotifyGameObjectsDeletedPacket;

			this.DeleteGameObjects(packet.instanceIDs);
		}

		private void	Handle_Scene_NotifyDeletedComponents(Client sender, Packet _packet)
		{
			NotifyDeletedComponentsPacket	packet = _packet as NotifyDeletedComponentsPacket;

			this.DeleteComponents(packet.gameObjectInstanceIDs, packet.instanceIDs);
		}

		private void	Handle_Scene_NotifyChangedMaterialData(Client sender, Packet _packet)
		{
			NotifyMaterialDataChangedPacket	packet = _packet as NotifyMaterialDataChangedPacket;

			this.CreateOrUpdateMaterialData(packet.netMaterial);
		}

		private void	Handle_Scene_NotifyMaterialPropertyUpdated(Client sender, Packet _packet)
		{
			NotifyMaterialPropertyUpdatedPacket		packet = _packet as NotifyMaterialPropertyUpdatedPacket;

			this.UpdateMaterialProperty(packet.instanceID, packet.propertyName, packet.rawValue);
		}

		private void	Handle_Scene_NotifyMaterialVector2Updated(Client sender, Packet _packet)
		{
			NotifyMaterialVector2UpdatedPacket	packet = _packet as NotifyMaterialVector2UpdatedPacket;

			this.UpdateMaterialVector2(packet.instanceID, packet.propertyName, packet.value, packet.type);
		}

		private void	Handle_Scene_NotifyAddedComponent(Client sender, Packet _packet)
		{
			NotifyComponentAddedPacket	packet = _packet as NotifyComponentAddedPacket;

			this.AddComponent(packet.gameObjectInstanceID, packet.component);
		}

		private void	OpenMonitorClientWindow()
		{
			MonitorClientPacketsWindow.Open(this.Client);
		}

		protected virtual void	ShowButton(Rect r)
		{
			r.y -= 2F;
			if (GUI.Button(r, new GUIContent("☰", @"Left-click to focus NG Remote windows.
Double left-click to force open " + NGRemoteInspectorWindow.ShortTitle + " & " + NGRemoteProjectWindow.ShortTitle + @".
Middle-click to force open " + NGRemoteCameraWindow.ShortTitle + @" as well.

Right-click to focus Unity's windows.")) == true)
			{
				if (Event.current.button == 1)
					NGRemoteHierarchyWindow.FocusUnityWindows();
				else if (Event.current.button == 2 || this.lastClick + Constants.DoubleClickTime > EditorApplication.timeSinceStartup)
				{
					this.AddTabInspector();
					this.AddTabProject();

					if (Event.current.button == 2)
						this.AddTabCamera();
				}
				else
				{
					for (int i = 0; i < this.remoteWindows.Count; i++)
						this.remoteWindows[i].Focus();
					this.Focus();

					this.lastClick = EditorApplication.timeSinceStartup;
				}
			}
		}

		public static void	FocusUnityWindows()
		{
			UnityEngine.Object[]	windows = Resources.FindObjectsOfTypeAll(NGRemoteHierarchyWindow.InspectorWindowType);

			for (int i = 0; i < windows.Length; i++)
				(windows[i] as EditorWindow).Focus();

			windows = Resources.FindObjectsOfTypeAll(NGRemoteHierarchyWindow.ProjectWindowType);

			for (int i = 0; i < windows.Length; i++)
				(windows[i] as EditorWindow).Focus();

			windows = Resources.FindObjectsOfTypeAll(NGRemoteHierarchyWindow.HierarchyWindowType);

			for (int i = 0; i < windows.Length; i++)
				(windows[i] as EditorWindow).Focus();
		}

		public void	AddTabMenus(GenericMenu menu, NGRemoteWindow window)
		{
			if (window != null)
			{
				NGRemoteHierarchyWindow[]	hierarchies = Resources.FindObjectsOfTypeAll<NGRemoteHierarchyWindow>();

				if (hierarchies.Length > 1)
				{
					for (int i = 0; i < hierarchies.Length; i++)
						menu.AddItem(new GUIContent("Reassign Hierarchy/" + hierarchies[i].titleContent.text + ' ' + hierarchies[i].address + ':' + hierarchies[i].port), hierarchies[i] == this, o => window.SetHierarchy(o as NGRemoteHierarchyWindow), hierarchies[i]);
				}
			}

			menu.AddItem(new GUIContent("Add Remote Tab/" + NGRemoteInspectorWindow.NormalTitle), false, this.AddTabInspector);
			menu.AddItem(new GUIContent("Add Remote Tab/" + NGRemoteProjectWindow.NormalTitle), false, this.AddTabProject);
			menu.AddItem(new GUIContent("Add Remote Tab/" + NGRemoteCameraWindow.NormalTitle), false, this.AddTabCamera);
			menu.AddItem(new GUIContent("Add Remote Tab/" + NGRemoteStaticInspectorWindow.NormalTitle), false, this.AddTabStaticInspector);
			menu.AddSeparator("");
		}

		private void	AddTabInspector()
		{
			Utility.OpenWindow<NGRemoteInspectorWindow>(NGRemoteInspectorWindow.ShortTitle, true, NGRemoteHierarchyWindow.InspectorWindowType);
		}

		private void	AddTabProject()
		{
			Utility.OpenWindow<NGRemoteProjectWindow>(NGRemoteProjectWindow.ShortTitle, true, NGRemoteHierarchyWindow.ProjectWindowType);
		}

		private void	AddTabCamera()
		{
			Utility.OpenWindow<NGRemoteCameraWindow>(NGRemoteCameraWindow.ShortTitle, true, typeof(NGRemoteHierarchyWindow));
		}

		private void	AddTabStaticInspector()
		{
			Utility.OpenWindow<NGRemoteStaticInspectorWindow>(NGRemoteStaticInspectorWindow.ShortTitle, true, NGRemoteHierarchyWindow.InspectorWindowType);
		}

		private void	UpdateAllHierarchiesTitle(NGRemoteHierarchyWindow skip = null)
		{
			NGRemoteHierarchyWindow[]	hierarchies = Resources.FindObjectsOfTypeAll<NGRemoteHierarchyWindow>();

			for (int i = 0; i < hierarchies.Length; i++)
				hierarchies[i].UpdateTitle(skip);
		}

		private void	UpdateTitle(NGRemoteHierarchyWindow skip)
		{
			NGRemoteHierarchyWindow[]	hierarchies = Resources.FindObjectsOfTypeAll<NGRemoteHierarchyWindow>();

			if (hierarchies.Length > 1)
			{
				for (int i = 0, j = 0; i < hierarchies.Length; i++)
				{
					if (hierarchies[i] == skip)
					{
						--j;
						continue;
					}

					if (hierarchies[i] == this)
					{
						EditorApplication.delayCall += () =>
						{
							this.titleContent.text = (NGRemoteHierarchyWindow.ShortTitle.StartsWith("NG ") == true ? NGRemoteHierarchyWindow.ShortTitle.Substring(3) : NGRemoteHierarchyWindow.ShortTitle) + "#" + (i + j + 1);
							this.Repaint();
						};
						break;
					}
				}
			}
			else
			{
				EditorApplication.delayCall += () =>
				{
					this.titleContent.text = NGRemoteHierarchyWindow.ShortTitle.StartsWith("NG ") == true ? NGRemoteHierarchyWindow.ShortTitle.Substring(3) : NGRemoteHierarchyWindow.ShortTitle;
					this.Repaint();
				};
			}
		}

		void	IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			this.AddTabMenus(menu, null);
			Utility.AddNGMenuItems(menu, this, NGRemoteHierarchyWindow.NormalTitle, Constants.WikiBaseURL + "#markdown-header-131-ng-remote-hierarchy");

			if (Conf.DebugMode != Conf.DebugState.None && this.Client != null)
				menu.AddItem(new GUIContent("Monitor Client"), false, this.OpenMonitorClientWindow);
		}
	}
}