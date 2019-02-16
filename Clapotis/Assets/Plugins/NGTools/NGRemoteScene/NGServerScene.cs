using NGTools.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
#if NETFX_CORE
using System.Reflection;
#endif
using System.Runtime.Serialization;
using UnityEngine.SceneManagement;

namespace NGTools.NGRemoteScene
{
	using UnityEngine;

	public enum ReturnDeleteComponents
	{
		Success,
		Incomplete
	}

	public enum ReturnDeleteGameObjects
	{
		Success,
		Incomplete
	}

	public enum ReturnRawDataFromObject
	{
		Success,
		AssetNotFound,
		ExportThrownException
	}

	public enum ReturnDeleteGameObject
	{
		Success,
		GameObjectNotFound,
		DeleteServerForbidden
	}

	public enum ReturnSetActiveScene
	{
		Success,
		InternalError,
		OutOfRange,
		NotLoaded,
		AlreadyActive,
		Invalid,
	}

	public enum ReturnUpdateFieldValue
	{
		Success,
		InternalError,
		TypeNotFound,
		GameObjectNotFound,
		ComponentNotFound,
		PathNotResolved,
		DisableServerForbidden
	}

	public enum ReturnInvokeComponentMethod
	{
		Success,
		GameObjectNotFound,
		ComponentNotFound,
		MethodNotFound,
		InvalidArgument,
		InvocationFailed
	}

	public enum ReturnUpdateMaterialProperty
	{
		Success,
		InternalError,
		MaterialNotFound,
		ShaderNotFound,
		PropertyNotFound,
	}

	public enum ReturnUpdateMaterialVector2
	{
		Success,
		MaterialNotFound,
	}

	public enum ReturnGetArray
	{
		Success,
		TypeNotFound,
		GameObjectNotFound,
		ComponentNotFound,
		PathNotResolved,
	}

	/// <summary>
	/// Might be a temp interface.
	/// </summary>
	public interface IHierarchyManagement
	{
		bool	SetSibling(int instanceID, int instanceIDParent, int siblingIndex);
	}

	[Serializable]
	public sealed class ListingAssets
	{
		[Serializable]
		public sealed class AssetReferences
		{
			public string	asset;
			public int		mainAssetIndex;
			public Object[]	references;
			public string[]	subNames;
			public int[]	IDs;
			public string[]	types;
		}

		public AssetReferences[]	assets;
	}

	[Serializable]
	public sealed class ListingShaders
	{
		public Shader[]	shaders;
		public byte[]	properties;
	}

	[HelpURL(Constants.WikiBaseURL + "#markdown-header-13-ng-remote-scene"), DisallowMultipleComponent]
	public class NGServerScene : MonoBehaviour, IHierarchyManagement
	{
		public const char	ValuePathSeparator = '#';
		public const char	SpecialGameObjectSeparator = '@';

		private static ByteBuffer	Buffer = new ByteBuffer(64);

		public BaseServer	server;

		[Header("Destroy the game console in production build.")]
		public bool	autoDestroyInProduction = true;

		[Header("Interval (sec) to let selected GameObject sends delta to the client")]
		public float	refreshMonitoring = 2F;

		[Header("Disable NG Server Scene after Start() to avoid call to OnGUI().")]
		public bool		autoDisabled = true;

		[Header("Assets embed into your build")]
		public ListingAssets	resources;
		[Header("Shaders embed into your build (Available in the list of shaders when changing a Material)")]
		public ListingShaders	shaderReferences;

		[Group("[Debug]")]
		public bool	displayDebug = false;
		[Group("[Debug]")]
		public int	tab = 0;
		[Group("[Debug]")]
		public int	offsetSent = 0;
		[Group("[Debug]")]
		public int	offsetReceived = 0;
		[Group("[Debug]")]
		public int	maxPacketsDisplay = 100;
		[Group("[Debug]")]
		public bool displayGameObjectWatchers = true;
		[Group("[Debug]")]
		public bool	displayMaterialWatchers = true;
		[Group("[Debug]")]
		public bool displayTypeWatchers = true;

		private GUIStyle	packetStyle;

		private int			selected = -1;
		private string[]	names = null;
		private Vector2		watchersScrollPosition;
		private Vector2		eventScrollPosition;
		private Vector2		sendScrollPosition;
		private Vector2		receiveScrollPosition;

		private List<string>	events = new List<string>(32);
		
		public AbstractTcpListener	listener { get { return (AbstractTcpListener)this.server.listener; } }

		private NGShader[]	shaderProperties;

		private string		valuePath;
		private string[]	paths;
		private byte[]		rawValue;
		private Type		lastType;
		private object		lastValue;

		private List<ServerScene>		scenes = new List<ServerScene>(2);
		private Stack<ServerScene>		poolScenes = new Stack<ServerScene>(2);
		private Dictionary<int, ServerGameObject>	cachedGameObjects = new Dictionary<int, ServerGameObject>(128);
		private Dictionary<int, KeyValuePair<MonitorGameObject, List<Client>>>	watchedGameObjects = new Dictionary<int, KeyValuePair<MonitorGameObject, List<Client>>>(4);
		private Dictionary<int, KeyValuePair<MonitorMaterial, List<Client>>>	watchedMaterials = new Dictionary<int, KeyValuePair<MonitorMaterial, List<Client>>>(4);
		private Dictionary<int, KeyValuePair<MonitorType, List<Client>>>		watchedTypes = new Dictionary<int, KeyValuePair<MonitorType, List<Client>>>(4);
		private List<int>							currentWatchingListGameObjects = new List<int>(4);
		private List<int>							currentWatchingListMaterials = new List<int>(4);
		private List<int>							currentWatchingListTypes = new List<int>(4);

		private Dictionary<Type, Dictionary<int, Object>>	registeredResources = new Dictionary<Type, Dictionary<int, Object>>();
		private List<Texture2D>								userTextur2Ds = new List<Texture2D>();
		private List<Sprite>								userSprites = new List<Sprite>();

		private Scene				dontDestroyOnLoadScene;
		private List<int>			instanceIDs = new List<int>(128);
		private List<Transform>		result = new List<Transform>(128);
		private List<GameObject>	roots = new List<GameObject>(16);

		private Type[]								inspectableStaticTypes;
		private Dictionary<int, IFieldModifier[]>	staticTypesMembers = new Dictionary<int, IFieldModifier[]>();

		private bool	shaderScanWarningOnce = false;
		private int		thisInstanceID;

		protected virtual void	Start()
		{
			if (Debug.isDebugBuild == false && this.autoDestroyInProduction == true)
			{
				Object.DestroyImmediate(this.gameObject);
				return;
			}

			if (this.server == null)
				InternalNGDebug.LogError("NG Server Scene requires field \"Server\".", this);
			else
			{
				this.server.RegisterService(NGTools.NGAssemblyInfo.Name, NGTools.NGAssemblyInfo.Version);
				this.server.RegisterService(NGAssemblyInfo.Name, NGAssemblyInfo.Version);
				new ServerSceneExecuter(this.server.executer, this);
			}

			this.StartCoroutine(this.UpdateGameObjectWatchers());
			this.StartCoroutine(this.UpdateMaterialsWatchers());
			this.StartCoroutine(this.UpdateTypesWatchers());

			SceneManager.activeSceneChanged += this.OnActiveSceneChanged;
			SceneManager.sceneLoaded += this.OnSceneLoaded;
			SceneManager.sceneUnloaded += this.OnSceneUnloaded;

			this.events.Add("Start");

			this.thisInstanceID = this.GetInstanceID();

			if (this.autoDisabled == true)
				this.enabled = false;
		}

		protected virtual void	OnEnable()
		{
			this.events.Add("OnEnable");
		}

		protected virtual void	OnDisable()
		{
			this.events.Add("OnDisable");
		}

		protected virtual void	OnDestroy()
		{
			if (this.server != null)
			{
				this.server.UnregisterService(NGTools.NGAssemblyInfo.Name, NGTools.NGAssemblyInfo.Version);
				this.server.UnregisterService(NGAssemblyInfo.Name, NGAssemblyInfo.Version);
			}

			SceneManager.activeSceneChanged -= this.OnActiveSceneChanged;
			SceneManager.sceneLoaded -= this.OnSceneLoaded;
			SceneManager.sceneUnloaded -= this.OnSceneUnloaded;

			this.events.Add("OnDestroy");
		}

		protected virtual void	OnGUI()
		{
			if (this.displayDebug == false)
				return;

			GUILayout.BeginHorizontal(GUILayout.MaxHeight(150F), GUILayout.Width(Screen.width));
			{
				this.eventScrollPosition = GUILayout.BeginScrollView(this.eventScrollPosition);
				{
					for (int i = this.events.Count - 1; i >= 0; --i)
						GUILayout.Label(this.events[i]);
				}
				GUILayout.EndScrollView();
			}
			GUILayout.EndHorizontal();

			if (this.names == null || this.listener.clients.Count != this.names.Length)
			{
				this.names = new string[this.listener.clients.Count];

				for (int i = 0; i < this.listener.clients.Count; i++)
#if NETFX_CORE
					this.names[i] = this.listener.clients[i].tcpClient.Information.LocalAddress + ":" + this.listener.clients[i].tcpClient.Information.LocalPort;
#else
					this.names[i] = this.listener.clients[i].tcpClient.Client.LocalEndPoint.ToString();
#endif
			}

			this.selected = Mathf.Clamp(GUILayout.Toolbar(this.selected, this.names), 0, this.listener.clients.Count - 1);

			GUILayout.BeginHorizontal(GUILayout.MaxHeight(150F), GUILayout.Width(Screen.width));
			{
				if (GUILayout.Button("Packets") == true)
					this.tab = 0;
				if (GUILayout.Button("Watchers") == true)
					this.tab = 1;
			}
			GUILayout.EndHorizontal();

			if (tab == 0)
			{
				if (this.selected >= 0 && this.selected < this.listener.clients.Count)
					this.DrawClientPackets(this.listener.clients[this.selected]);
			}
			else
				this.DrawWatchers();
		}

		private void	OnApplicationQuit()
		{
			this.events.Add("OnApplicationQuit");
		}

		private void	OnApplicationFocus(bool focusStatus)
		{
			this.events.Add("OnApplicationFocus " + focusStatus);
		}

		private void	OnApplicationPause(bool pauseStatus)
		{
			this.events.Add("OnApplicationPause " + pauseStatus);
		}

		private void	DrawWatchers()
		{
			this.watchersScrollPosition = GUILayout.BeginScrollView(this.watchersScrollPosition);
			{
				this.displayGameObjectWatchers = GUILayout.Toggle(this.displayGameObjectWatchers, "GameObjects (" + watchedGameObjects.Values.Count + ")");

				foreach (var item in watchedGameObjects.Values)
				{
					GUILayout.BeginHorizontal();
					{
						GUILayout.Label(item.Key.Instance.ToString());

						for (int i = 0; i < item.Value.Count; i++)
							GUILayout.Label(item.Value[i].ToString());
					}
					GUILayout.EndHorizontal();

					this.DrawMonitorData(item.Key, 10F);
				}

				this.displayMaterialWatchers = GUILayout.Toggle(this.displayMaterialWatchers, "Materials (" + watchedMaterials.Values.Count + ")");

				foreach (var item in watchedMaterials.Values)
				{
					GUILayout.BeginHorizontal();
					{
						GUILayout.Label(item.Key.Instance.ToString());

						for (int i = 0; i < item.Value.Count; i++)
							GUILayout.Label(item.Value[i].ToString());
					}
					GUILayout.EndHorizontal();

					this.DrawMonitorData(item.Key, 10F);
				}

				this.displayTypeWatchers = GUILayout.Toggle(this.displayTypeWatchers, "Types (" + watchedTypes.Values.Count + ")");

				foreach (var item in watchedTypes.Values)
				{
					GUILayout.BeginHorizontal();
					{
						GUILayout.Label(item.Key.Instance.ToString());

						for (int i = 0; i < item.Value.Count; i++)
							GUILayout.Label(item.Value[i].ToString());
					}
					GUILayout.EndHorizontal();

					this.DrawMonitorData(item.Key, 10F);
				}
			}
			GUILayout.EndScrollView();
		}

		private void	DrawMonitorData(MonitorData data, float margin)
		{
			foreach (MonitorData child in data.EachChild)
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(margin);
					GUILayout.Label(child.path);
				}
				GUILayout.EndHorizontal();

				this.DrawMonitorData(child, margin + 10F);
			}
		}

		private void	DrawClientPackets(Client client)
		{
			if (this.packetStyle == null)
			{
				this.packetStyle = new GUIStyle(GUI.skin.label);
				this.packetStyle.fontSize = 9;
			}

#if NETFX_CORE
			GUILayout.Label("Client " + client.tcpClient.Information.LocalAddress + ":" + client.tcpClient.Information.LocalAddress + " " + DateTime.Now.ToString("HH:mm:ss"));
#else
			GUILayout.Label("Client " + client.tcpClient.Client.LocalEndPoint.ToString() + " " + DateTime.Now.ToString("HH:mm:ss"));
#endif
			GUILayout.Label(client.ToString());

			GUILayout.BeginHorizontal();
			{
				if (client.saveSentPackets == true)
				{
					GUILayout.BeginVertical();
					{
						GUILayout.Label("Sent (" + client.sentPacketsHistoric.Count + ")");
						this.sendScrollPosition = GUILayout.BeginScrollView(this.sendScrollPosition);
						{
							for (int i = client.sentPacketsHistoric.Count - 1 - this.offsetSent, j = 0; i >= 0 && j < this.maxPacketsDisplay; --i, ++j)
								GUILayout.Label(client.sentPacketsHistoric[i].ReadableSendTime + " " + client.sentPacketsHistoric[i].packet.ToString(), this.packetStyle);
						}
						GUILayout.EndScrollView();
					}
					GUILayout.EndVertical();
				}

				GUILayout.BeginVertical();
				{
					GUILayout.Label("Received (" + client.receivedPacketsHistoric.Count + ")");
					this.receiveScrollPosition = GUILayout.BeginScrollView(this.receiveScrollPosition);
					{
						for (int i = client.receivedPacketsHistoric.Count - 1 - this.offsetReceived, j = 0; i >= 0 && j < this.maxPacketsDisplay; --i, ++j)
							GUILayout.Label(client.receivedPacketsHistoric[i], this.packetStyle);
					}
					GUILayout.EndScrollView();
				}
				GUILayout.EndVertical();
			}
			GUILayout.EndHorizontal();
		}

		private IEnumerator	UpdateGameObjectWatchers()
		{
			int	jGameObjects = 0;

			while (true)
			{
				lock (this.currentWatchingListGameObjects)
				{
					// Get list of GameObjects being watched.
					if (this.currentWatchingListGameObjects.Count == 0)
					{
						foreach (var watcher in this.watchedGameObjects)
						{
							if (watcher.Value.Value.Count > 0)
								this.currentWatchingListGameObjects.Add(watcher.Key);
						}

						jGameObjects = 0;
					}

					// Continuous update of watchers spread in time; avoiding spikes.
					if (this.currentWatchingListGameObjects.Count > 0)
					{
						if (jGameObjects < this.currentWatchingListGameObjects.Count)
						{
							KeyValuePair<MonitorGameObject, List<Client>>	watcher = this.watchedGameObjects[this.currentWatchingListGameObjects[jGameObjects]];

							// Check if there still Clients watching.
							if (watcher.Value.Count > 0)
							{
								watcher.Key.UpdateValues(watcher.Value);
								if (watcher.Key.ToDelete == true)
									this.watchedGameObjects.Remove(this.currentWatchingListGameObjects[jGameObjects]);
							}

							++jGameObjects;
						}

						if (jGameObjects >= this.currentWatchingListGameObjects.Count)
							this.currentWatchingListGameObjects.Clear();
					}
				}

				int	total = this.currentWatchingListGameObjects.Count;
				if (total >= 2)
					yield return new WaitForSecondsRealtime(this.refreshMonitoring / total);
				else
					yield return new WaitForSecondsRealtime(this.refreshMonitoring);
			}
		}

		private IEnumerator	UpdateMaterialsWatchers()
		{
			int	kMaterials = 0;

			while (true)
			{
				lock (this.currentWatchingListMaterials)
				{
					// Get list of Materials being watched.
					if (this.currentWatchingListMaterials.Count == 0)
					{
						foreach (var watcher in this.watchedMaterials)
						{
							if (watcher.Value.Value.Count > 0)
								this.currentWatchingListMaterials.Add(watcher.Key);
						}

						kMaterials = 0;
					}

					// Continuous update of watchers spread in time; avoiding spikes.
					if (this.currentWatchingListMaterials.Count > 0)
					{
						if (kMaterials < this.currentWatchingListMaterials.Count)
						{
							KeyValuePair<MonitorMaterial, List<Client>>	watcher = this.watchedMaterials[this.currentWatchingListMaterials[kMaterials]];

							// Check if there still Clients watching.
							if (watcher.Value.Count > 0)
							{
								watcher.Key.UpdateValues(watcher.Value);
								if (watcher.Key.ToDelete == true)
									this.watchedMaterials.Remove(this.currentWatchingListMaterials[kMaterials]);
							}

							++kMaterials;
						}

						if (kMaterials >= this.currentWatchingListMaterials.Count)
							this.currentWatchingListMaterials.Clear();
					}
				}

				int	total = this.currentWatchingListMaterials.Count;
				if (total >= 2)
					yield return new WaitForSecondsRealtime(this.refreshMonitoring / total);
				else
					yield return new WaitForSecondsRealtime(this.refreshMonitoring);
			}
		}

		private IEnumerator	UpdateTypesWatchers()
		{
			int	lTypes = 0;

			while (true)
			{
				lock (this.currentWatchingListTypes)
				{
					// Get list of Types being watched.
					if (this.currentWatchingListTypes.Count == 0)
					{
						foreach (var watcher in this.watchedTypes)
						{
							if (watcher.Value.Value.Count > 0)
								this.currentWatchingListTypes.Add(watcher.Key);
						}

						lTypes = 0;
					}

					// Continuous update of watchers spread in time; avoiding spikes.
					if (this.currentWatchingListTypes.Count > 0)
					{
						if (lTypes < this.currentWatchingListTypes.Count)
						{
							KeyValuePair<MonitorType, List<Client>>	watcher = this.watchedTypes[this.currentWatchingListTypes[lTypes]];

							// Check if there still Clients watching.
							if (watcher.Value.Count > 0)
							{
								watcher.Key.UpdateValues(watcher.Value);
								if (watcher.Key.ToDelete == true)
									this.watchedTypes.Remove(this.currentWatchingListTypes[lTypes]);
							}

							++lTypes;
						}

						if (lTypes >= this.currentWatchingListTypes.Count)
							this.currentWatchingListTypes.Clear();
					}
				}

				int	total = this.currentWatchingListTypes.Count;
				if (total >= 2)
					yield return new WaitForSecondsRealtime(this.refreshMonitoring / total);
				else
					yield return new WaitForSecondsRealtime(this.refreshMonitoring);
			}
		}

		public List<ServerScene>	ScanHierarchy()
		{
			for (int i = 0; i < this.scenes.Count; i++)
				this.poolScenes.Push(this.scenes[i]);
			this.scenes.Clear();

			ServerScene	serverScene;

			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene	scene = SceneManager.GetSceneAt(i);

				if (scene.isLoaded == true)
				{
					scene.GetRootGameObjects(roots);

					try
					{
						if (this.poolScenes.Count > 0)
						{
							serverScene = this.poolScenes.Pop();
							serverScene.Reset(scene);
						}
						else
							serverScene = new ServerScene(scene);

						for (int j = 0; j < roots.Count; j++)
							this.ProcessRootGameObject(serverScene, roots[j]);

						serverScene.roots.Sort((a, b) => a.gameObject.transform.GetSiblingIndex() - b.gameObject.transform.GetSiblingIndex());

						this.scenes.Add(serverScene);
					}
					catch (Exception ex)
					{
						Debug.LogException(ex);
					}
				}
			}

#if !UNITY_EDITOR
			ServerScene		dummyScene = null;
			GameObject[]	allGameObjects = Object.FindObjectsOfType<GameObject>();

			for (int i = 0; i < allGameObjects.Length; i++)
			{
				if (allGameObjects[i].transform.parent == null &&
					this.CheckSceneDontDestroyOnLoadScene(allGameObjects[i].scene) == true)
				{
					if (dummyScene == null)
					{
						dummyScene = new ServerScene(allGameObjects[i].scene);
						dummyScene.name = "DontDestroyOnLoad";
						this.scenes.Add(dummyScene);
					}

					this.ProcessRootGameObject(dummyScene, allGameObjects[i]);
				}
			}
#else
			if (this.dontDestroyOnLoadScene.IsValid() == false)
			{
				GameObject[]	allGameObjects = Object.FindObjectsOfType<GameObject>();

				for (int i = 0; i < allGameObjects.Length; i++)
				{
					if (this.CheckSceneDontDestroyOnLoadScene(allGameObjects[i].scene) == true)
					{
						this.dontDestroyOnLoadScene = allGameObjects[i].scene;
						break;
					}
				}
			}

			if (this.CheckSceneDontDestroyOnLoadScene(this.dontDestroyOnLoadScene) == true)
			{
				if (this.poolScenes.Count > 0)
				{
					serverScene = this.poolScenes.Pop();
					serverScene.Reset(this.dontDestroyOnLoadScene);
				}
				else
					serverScene = new ServerScene(this.dontDestroyOnLoadScene);

				this.dontDestroyOnLoadScene.GetRootGameObjects(roots);

				for (int j = 0; j < roots.Count; j++)
					this.ProcessRootGameObject(serverScene, roots[j]);

				this.scenes.Add(serverScene);
			}
#endif

			// Remove destroyed GameObjects.
			foreach (var item in this.cachedGameObjects)
			{
				if (item.Value == null)
					this.cachedGameObjects.Remove(item.Key);
			}

			return this.scenes;
		}

		private bool	CheckSceneDontDestroyOnLoadScene(Scene scene)
		{
			return scene.name == "DontDestroyOnLoad" || // Works in editor.
				(string.IsNullOrEmpty(scene.name) == true && // Works in build.
				 scene.isDirty == false &&
				 scene.isLoaded == false &&
				 scene.IsValid() == false &&
				 string.IsNullOrEmpty(scene.path) == true &&
				 scene.buildIndex == -1 &&
				 scene.rootCount == 0);
		}

		public ServerGameObject	GetGameObject(int instanceID)
		{
			ServerGameObject	ng;

			if (this.cachedGameObjects.TryGetValue(instanceID, out ng) == true)
			{
				if (ng.gameObject == null)
				{
					this.cachedGameObjects.Remove(instanceID);
					ng = null;
				}
			}

			return ng;
		}

		public List<ServerComponent>	ScanGameObject(int instanceID)
		{
			ServerGameObject	ng;

			if (this.cachedGameObjects.TryGetValue(instanceID, out ng) == true)
			{
				ng.ProcessComponents();
				return ng.components;
			}

			return null;
		}

		/// <summary>
		/// Gets a registered resource from an <paramref name="instanceID"/> in the list associated to <paramref name="type"/>.
		/// </summary>
		/// <param name="type">Type of resource, any inheriting from Object.</param>
		/// <param name="instanceID">An instanceID from a previously registered object.</param>
		/// <returns></returns>
		/// <remarks>Look at <see cref="GetResources"/> to register resources.</remarks>
		public T	GetResource<T>(int instanceID) where T : Object
		{
			Object					instance;
			Dictionary<int, Object>	dic = this.RegisterResources(typeof(T), false);

			dic.TryGetValue(instanceID, out instance);

			return instance as T;
		}

		/// <summary>
		/// Gets a registered resource from an <paramref name="instanceID"/> in the list associated to <paramref name="type"/>.
		/// </summary>
		/// <param name="type">Type of resource, any inheriting from Object.</param>
		/// <param name="instanceID">An instanceID from a previously registered object.</param>
		/// <returns></returns>
		/// <remarks>Look at <see cref="GetResources"/> to register resources.</remarks>
		public Object	GetResource(Type type, int instanceID)
		{
			Object					instance;
			Dictionary<int, Object>	dic = this.RegisterResources(type, false);

			dic.TryGetValue(instanceID, out instance);

			return instance;
		}

		public Object	TryGetResource(int instanceID)
		{
			foreach (Dictionary<int, Object> item in this.registeredResources.Values)
			{
				foreach (var item2 in item)
				{
					if (item2.Key == instanceID)
						return item2.Value;
				}
			}

			return null;
		}

		public void	RegisterResource(Type type, Object instance)
		{
			Dictionary<int, Object>	dic = this.RegisterResources(type, false);

			dic.Add(instance.GetInstanceID(), instance);
		}

		/// <summary>
		/// Gets names and IDs from all resources of a <paramref name="type"/> and registers them localy for future requests.
		/// </summary>
		/// <param name="type">Type of resource, any inheriting from Object.</param>
		/// <param name="resourceNames">An array containing names of resources.</param>
		/// <param name="instanceIDs">An array containing instanceIDs of resources.</param>
		/// <remarks>Look at <see cref="GetResource"/> to use resources.</remarks>
		public void	GetResources(Type type, bool forceRefresh, out string[] resourceNames, out int[] instanceIDs)
		{
			Dictionary<int, Object>	dic = this.RegisterResources(type, forceRefresh);

			resourceNames = new string[dic.Count];
			instanceIDs = new int[dic.Count];

			int	i = 0;

			foreach (var resource in dic)
			{
				// Skip destroyed resources.
				if (resource.Value != null)
				{
					resourceNames[i] = resource.Value.name;
					instanceIDs[i] = resource.Key;

					++i;
				}
			}
		}

		public Dictionary<int, Object>	RegisterResources(Type type, bool forceRefresh)
		{
			InternalNGDebug.Assert(typeof(Object).IsAssignableFrom(type) == true, "RegisterResources is requesting a non-Object type.");

			Dictionary<int, Object>	dic;

			if (forceRefresh == true ||
				this.registeredResources.TryGetValue(type, out dic) == false)
			{
				Object[]	resources = Resources.FindObjectsOfTypeAll(type);

				dic = new Dictionary<int, Object>();

				for (int i = 0; i < resources.Length; i++)
					dic.Add(resources[i].GetInstanceID(), resources[i]);

				if (this.registeredResources.ContainsKey(type) == true)
					this.registeredResources[type] = dic;
				else
					this.registeredResources.Add(type, dic);
			}

			return dic;
		}

		/// <summary>
		/// Sets a GameObject from <paramref name="instanceID"/> child of GameObject from <paramref name="instanceIDParent"/> at the position <paramref name="siblingIndex"/>.
		/// </summary>
		/// <param name="instanceID">ID of any GameObject.</param>
		/// <param name="instanceIDParent">ID of the parent GameObject.</param>
		/// <param name="siblingIndex">Position in children.</param>
		/// <returns></returns>
		public bool	SetSibling(int instanceID, int instanceIDParent, int siblingIndex)
		{
			ServerGameObject	a;
			ServerGameObject	b = null;

			//Debug.Log("SetSibling " + instanceID + "	" + instanceIDParent + "	" + siblingIndex);

			if (this.cachedGameObjects.TryGetValue(instanceID, out a) == true &&
				(instanceIDParent == -1 || this.cachedGameObjects.TryGetValue(instanceIDParent, out b) == true))
			{
				if (instanceIDParent == -1)
				{
					a.gameObject.transform.SetParent(null, true);
					//Debug.Log(a.gameObject + " go into root at " + siblingIndex, a.gameObject);
				}
				else
				{
					a.gameObject.transform.SetParent(b.gameObject.transform, true);
					//Debug.Log(a.gameObject + " go into " + b.gameObject + " at " + siblingIndex, a.gameObject);
				}

				//Debug.Log(a.gameObject.transform.GetSiblingIndex());
				a.gameObject.transform.SetSiblingIndex(siblingIndex);
				//Debug.Log(a.gameObject.transform.GetSiblingIndex() + "	<>	" + siblingIndex);

				// HACK Workaround of bug #719312_ppd83fdc6vqv5sel
				// Finally does not resolve the issue when parent is root...
				//if (a.gameObject.transform.GetSiblingIndex() != siblingIndex &&
				//	a.gameObject.transform.parent != null &&
				//	a.gameObject.transform.childCount >= 2)
				//{
				//	a.gameObject.transform.GetChild(a.gameObject.transform.childCount - 2).SetAsLastSibling();
				//	Debug.Log(a.gameObject.transform.GetSiblingIndex() + "	<>	" + siblingIndex);
				//	a.gameObject.transform.SetSiblingIndex(siblingIndex - 1);
				//	Debug.Log(a.gameObject.transform.GetSiblingIndex() + "	<>	" + siblingIndex);
				//}

				return true;
			}

			return false;
		}

		public ReturnSetActiveScene	SetActiveScene(int index)
		{
			if (index < 0 || index >= SceneManager.sceneCount)
				return ReturnSetActiveScene.OutOfRange;

			Scene	scene = SceneManager.GetSceneAt(index);
			if (scene.isLoaded == false)
				return ReturnSetActiveScene.NotLoaded;

			if (SceneManager.GetActiveScene() == scene)
				return ReturnSetActiveScene.AlreadyActive;

			if (SceneManager.SetActiveScene(scene) == false)
				return ReturnSetActiveScene.InternalError;

			return ReturnSetActiveScene.Success;
		}

		public ReturnInvokeComponentMethod	InvokeComponentMethod(int gameObjectInstanceID, int componentInstanceID, string methodSignature, byte[] arguments, ref string result)
		{
			ServerGameObject	gameObject;

			if (this.cachedGameObjects.TryGetValue(gameObjectInstanceID, out gameObject) == false)
				return ReturnInvokeComponentMethod.GameObjectNotFound;

			ServerComponent	component = gameObject.GetComponent(componentInstanceID);

			if (component == null)
				return ReturnInvokeComponentMethod.ComponentNotFound;

			ServerMethodInfo	method = component.GetMethodFromSignature(methodSignature);

			if (method == null)
				return ReturnInvokeComponentMethod.MethodNotFound;

			object[]	convertedArgs = new object[method.argumentTypes.Length];

			NGServerScene.Buffer.Clear();
			NGServerScene.Buffer.Append(arguments);

			try
			{
				for (int i = 0; i < method.argumentTypes.Length; i++)
				{
					TypeHandler	typeHandler = TypeHandlersManager.GetTypeHandler(method.argumentTypes[i]);

					if (typeHandler == null)
						return ReturnInvokeComponentMethod.InvalidArgument;

					convertedArgs[i] = typeHandler.DeserializeRealValue(this, NGServerScene.Buffer, method.argumentTypes[i]);
				}
			}
			catch
			{
				return ReturnInvokeComponentMethod.InvalidArgument;
			}
			
			try
			{
				object	value = method.methodInfo.Invoke(component.component, convertedArgs);

				if (value != null)
					result = value.ToString();
				else
					result = "NULL";

				return ReturnInvokeComponentMethod.Success;
			}
			catch
			{
				return ReturnInvokeComponentMethod.InvocationFailed;
			}
		}

		public ReturnDeleteGameObject	DeleteGameObject(int instanceID, List<int> instanceIDsDeleted)
		{
			ServerGameObject	gameObject = this.GetGameObject(instanceID);

			if (gameObject == null)
				return ReturnDeleteGameObject.GameObjectNotFound;

			if (gameObject.gameObject == this.gameObject)
				return ReturnDeleteGameObject.DeleteServerForbidden;

			gameObject.gameObject.GetComponentsInChildren<Transform>(true, result);

			for (int i = 0; i < result.Count; i++)
				instanceIDsDeleted.Add(result[i].gameObject.GetInstanceID());

			GameObject.Destroy(gameObject.gameObject);

			return ReturnDeleteGameObject.Success;
		}

		public ReturnDeleteGameObjects DeleteGameObjects(Client sender, List<int> instanceIDs)
		{
			ReturnDeleteGameObjects	result = ReturnDeleteGameObjects.Success;

			this.instanceIDs.Clear();

			for (int i = 0; i < instanceIDs.Count; i++)
			{
				ReturnDeleteGameObject	subResult = this.DeleteGameObject(instanceIDs[i], this.instanceIDs);

				if (subResult == ReturnDeleteGameObject.DeleteServerForbidden)
				{
					instanceIDs.RemoveAt(i--);
					result = ReturnDeleteGameObjects.Incomplete;
					sender.AddPacket(new ErrorNotificationPacket(Errors.Server_DisableServerForbidden, "Server can not delete itself."));
				}
				else if (subResult == ReturnDeleteGameObject.GameObjectNotFound)
				{
					instanceIDs.RemoveAt(i--);
					result = ReturnDeleteGameObjects.Incomplete;
				}
			}

			if (this.instanceIDs.Count > 0)
			{
				NotifyGameObjectsDeletedPacket	packet = new NotifyGameObjectsDeletedPacket();

				packet.instanceIDs.AddRange(this.instanceIDs);

				this.listener.BroadcastPacket(packet);
			}

			return result;
		}

		/// <summary>
		/// <para>Deletes Components from given parameters.</para>
		/// <para>Those that could not be deleted will be removed from the given lists.</para>
		/// <para>Those that have been deleted will notify all clients.</para>
		/// <para>If a GameObject is not found, it notifies all clients.</para>
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="gameObjectInstanceIDs"></param>
		/// <param name="instanceIDs"></param>
		public ReturnDeleteComponents	DeleteComponents(Client sender, List<int> gameObjectInstanceIDs, List<int> instanceIDs)
		{
			NotifyDeletedComponentsPacket	packet = null;
			NotifyGameObjectsDeletedPacket	gpacket = null;
			ReturnDeleteComponents			result = ReturnDeleteComponents.Success;

			for (int i = 0; i < gameObjectInstanceIDs.Count; i++)
			{
				ServerGameObject	gameObject = this.GetGameObject(gameObjectInstanceIDs[i]);

				if (gameObject != null)
				{
					if (instanceIDs[i] == this.thisInstanceID)
					{
						gameObjectInstanceIDs.RemoveAt(i);
						instanceIDs.RemoveAt(i);
						result = ReturnDeleteComponents.Incomplete;
						sender.AddPacket(new ErrorNotificationPacket(Errors.Server_DisableServerForbidden, "Server can not remove itself."));
					}
					else if (gameObject.RemoveComponent(instanceIDs[i]) == true)
					{
						if (packet == null)
							packet = new NotifyDeletedComponentsPacket();

						packet.Add(gameObjectInstanceIDs[i], instanceIDs[i]);
					}
					else
					{
						gameObjectInstanceIDs.RemoveAt(i);
						instanceIDs.RemoveAt(i);
						result = ReturnDeleteComponents.Incomplete;
					}
				}
				else
				{
					if (gpacket == null)
						gpacket = new NotifyGameObjectsDeletedPacket();

					gpacket.instanceIDs.Add(gameObjectInstanceIDs[i]);
					result = ReturnDeleteComponents.Incomplete;
				}
			}

			if (packet != null)
			{
				lock (this.currentWatchingListGameObjects)
				{
					foreach (var pair in this.watchedGameObjects)
					{
						for (int j = 0; j < packet.instanceIDs.Count; j++)
							pair.Value.Key.DeleteComponent(packet.instanceIDs[j]);
					}

					this.listener.BroadcastPacket(packet);
				}
			}

			if (gpacket != null)
				this.listener.BroadcastPacket(gpacket);

			return result;
		}

		public void	WatchGameObjects(Client sender, int[] instanceIDs)
		{
			int	skipped = 0;

			// Stop watchers when there is no more Client watching it.
			foreach (var pair in this.watchedGameObjects)
			{
				if (instanceIDs.Contains(pair.Key) == true)
				{
					++skipped;
					continue;
				}

				// Remove Client from watcher.
				pair.Value.Value.Remove(sender);

				if (pair.Value.Value.Count == 0)
				{
					lock (this.currentWatchingListGameObjects)
						this.currentWatchingListGameObjects.Remove(pair.Key);
				}
			}

			for (int i = 0; i < instanceIDs.Length; i++)
			{
				ServerGameObject	serverGameObject = this.GetGameObject(instanceIDs[i]);

				if (serverGameObject == null)
					return;

				KeyValuePair<MonitorGameObject, List<Client>>	watcher;

				if (this.watchedGameObjects.TryGetValue(instanceIDs[i], out watcher) == true)
					watcher.Value.Add(sender);
				else
				{
					watcher = new KeyValuePair<MonitorGameObject, List<Client>>(new MonitorGameObject(serverGameObject), new List<Client>());
					watcher.Value.Add(sender);
					this.watchedGameObjects.Add(instanceIDs[i], watcher);
				}
			}
		}

		public void	WatchMaterials(Client sender, int[] instanceIDs)
		{
			int	skipped = 0;

			// Stop watchers when there is no more Client watching it.
			foreach (var pair in this.watchedMaterials)
			{
				if (instanceIDs.Contains(pair.Key) == true)
				{
					++skipped;
					continue;
				}

				// Remove Client from watcher.
				pair.Value.Value.Remove(sender);

				if (pair.Value.Value.Count == 0)
				{
					lock (this.currentWatchingListMaterials)
						this.currentWatchingListMaterials.Remove(pair.Key);
				}
			}

			for (int i = 0; i < instanceIDs.Length; i++)
			{
				Material	mat = this.GetResource<Material>(instanceIDs[i]);

				if (mat == null)
					return;

				KeyValuePair<MonitorMaterial, List<Client>>	watcher;

				if (this.watchedMaterials.TryGetValue(instanceIDs[i], out watcher) == true)
					watcher.Value.Add(sender);
				else
				{
					NGShader	shader = this.GetNGShader(mat.shader);

					if (shader != null)
					{
						watcher = new KeyValuePair<MonitorMaterial, List<Client>>(new MonitorMaterial(mat, shader), new List<Client>());
						watcher.Value.Add(sender);
						this.watchedMaterials.Add(instanceIDs[i], watcher);
					}
				}
			}
		}

		public void	WatchTypes(Client sender, int[] typeIndexes)
		{
			int	skipped = 0;

			// Stop watchers when there is no more Client watching it.
			foreach (var pair in this.watchedTypes)
			{
				if (typeIndexes.Contains(pair.Key) == true)
				{
					++skipped;
					continue;
				}

				// Remove Client from watcher.
				pair.Value.Value.Remove(sender);

				if (pair.Value.Value.Count == 0)
				{
					lock (this.currentWatchingListTypes)
						this.currentWatchingListTypes.Remove(pair.Key);
				}
			}

			for (int i = 0; i < typeIndexes.Length; i++)
			{
				if (typeIndexes[i] < 0 || typeIndexes[i] >= this.inspectableStaticTypes.Length)
					return;

				KeyValuePair<MonitorType, List<Client>>	watcher;

				if (this.watchedTypes.TryGetValue(typeIndexes[i], out watcher) == true)
					watcher.Value.Add(sender);
				else
				{
					try
					{
						watcher = new KeyValuePair<MonitorType, List<Client>>(new MonitorType(typeIndexes[i].ToString(), this.inspectableStaticTypes[typeIndexes[i]]), new List<Client>());
						watcher.Value.Add(sender);
						this.watchedTypes.Add(typeIndexes[i], watcher);
					}
					catch (Exception ex)
					{
						InternalNGDebug.LogException("Monitoring Type \"" + this.inspectableStaticTypes[typeIndexes[i]].AssemblyQualifiedName + "\" failed.", ex);
					}
				}
			}
		}

		public ReturnUpdateMaterialProperty	UpdateMaterialProperty(int instanceID, string propertyName, byte[] rawValue, out byte[] newValue)
		{
			Material	material = this.GetResource<Material>(instanceID);

			newValue = null;

			if (material == null)
				return ReturnUpdateMaterialProperty.MaterialNotFound;

			try
			{
				NGShader	shader = this.GetNGShader(material.shader);

				if (shader == null)
					return ReturnUpdateMaterialProperty.ShaderNotFound;

				NGShaderProperty	prop = shader.GetProperty(propertyName);

				if (prop == null)
					return ReturnUpdateMaterialProperty.PropertyNotFound;

				Type	type = null;

				if (prop.type == NGShader.ShaderPropertyType.Color)
					type = typeof(Color);
				else if (prop.type == NGShader.ShaderPropertyType.Float ||
						 prop.type == NGShader.ShaderPropertyType.Range)
				{
					type = typeof(float);
				}
				else if (prop.type == NGShader.ShaderPropertyType.TexEnv)
					type = typeof(Texture);
				else if (prop.type == NGShader.ShaderPropertyType.Vector)
					type = typeof(Vector4);

				TypeHandler	typeHandler = TypeHandlersManager.GetTypeHandler(type);
				InternalNGDebug.Assert(typeHandler != null, "TypeHandler for " + prop.name + " is not supported.");

				NGServerScene.Buffer.Clear();
				NGServerScene.Buffer.Append(rawValue);

				if (prop.type == NGShader.ShaderPropertyType.Color)
				{
					material.SetColor(prop.name, (Color)typeHandler.DeserializeRealValue(this, NGServerScene.Buffer, type));
					newValue = typeHandler.Serialize(type, material.GetColor(prop.name));
					return ReturnUpdateMaterialProperty.Success;
				}
				else if (prop.type == NGShader.ShaderPropertyType.Float ||
						 prop.type == NGShader.ShaderPropertyType.Range)
				{
					material.SetFloat(prop.name, (float)typeHandler.DeserializeRealValue(this, NGServerScene.Buffer, type));
					newValue = typeHandler.Serialize(type, material.GetFloat(prop.name));
					return ReturnUpdateMaterialProperty.Success;
				}
				else if (prop.type == NGShader.ShaderPropertyType.TexEnv)
				{
					material.SetTexture(prop.name, (Texture)typeHandler.DeserializeRealValue(this, NGServerScene.Buffer, type));
					newValue = typeHandler.Serialize(type, material.GetTexture(prop.name));
					return ReturnUpdateMaterialProperty.Success;
				}
				else if (prop.type == NGShader.ShaderPropertyType.Vector)
				{
					material.SetVector(prop.name, (Vector4)typeHandler.DeserializeRealValue(this, NGServerScene.Buffer, type));
					newValue = typeHandler.Serialize(type, material.GetVector(prop.name));
					return ReturnUpdateMaterialProperty.Success;
				}
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException("Material property \"" + propertyName  + "\" not updated.", ex);
			}

			return ReturnUpdateMaterialProperty.InternalError;
		}

		public ReturnUpdateMaterialVector2	UpdateMaterialVector2(int instanceID, string propertyName, Vector2 value, MaterialVector2Type vectorType, out Vector2 newValue)
		{
			Material	material = this.GetResource<Material>(instanceID);

			newValue = default(Vector2);

			if (material == null)
				return ReturnUpdateMaterialVector2.MaterialNotFound;

			if (vectorType == MaterialVector2Type.Offset)
			{
				material.SetTextureOffset(propertyName, value);
				newValue = material.GetTextureOffset(propertyName);
			}
			else if (vectorType == MaterialVector2Type.Scale)
			{
				material.SetTextureScale(propertyName, value);
				newValue = material.GetTextureScale(propertyName);
			}

			return ReturnUpdateMaterialVector2.Success;
		}

		/// <summary>
		/// <para>Resolves the path and updates the value with <paramref name="rawValue"/>.</para>
		/// <para>Notifies all clients if an array is created or has change its size.</para>
		/// </summary>
		/// <param name="valuePath"></param>
		/// <param name="rawValue"></param>
		/// <returns></returns>
		/// <remarks>See <see cref="NGToolsEditor.NGHierarchyEditorWindow.UpdateFieldValue" /> for the client equivalent.</remarks>
		/// <exception cref="System.MissingFieldException">Thrown when an unknown field from ClientGameObject is being assigned.</exception>
		/// <exception cref="System.InvalidCastException">Thrown when an array is assigned but the array is not supported.</exception>
		/// <exception cref="System.ArgumentException">Thrown when the path seems to be not resolvable.</exception>
		public ReturnUpdateFieldValue		UpdateFieldValue(string valuePath, byte[] rawValue, out byte[] newValue)
		{
			string[]	paths = valuePath.Split(NGServerScene.ValuePathSeparator);

			newValue = null;

			try
			{
				// Check if update is dealing with Type.
				if ((paths[1][0] < '0' || paths[1][0] > '9') && paths[1][0] != NGServerScene.SpecialGameObjectSeparator && paths[1][0] != '-')
				{
					Type	type = null;

					try
					{
						type = this.inspectableStaticTypes[int.Parse(paths[0])];
					}
					catch (IndexOutOfRangeException)
					{
						InternalNGDebug.LogError("Type index \"" + paths[0] + "\" is out of range.");
						return ReturnUpdateFieldValue.TypeNotFound;
					}

					IFieldModifier	memberModifier = null;
					FieldInfo		field = type.GetField(paths[1], BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

					if (field == null)
					{
						PropertyInfo	property = type.GetProperty(paths[1], BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
						if (property == null)
						{
							InternalNGDebug.LogError("Path \"" + valuePath + "\" is invalid.");
							return ReturnUpdateFieldValue.PathNotResolved;
						}
						else
							memberModifier = new PropertyModifier(property);
					}
					else
						memberModifier = new FieldModifier(field);

					this.valuePath = valuePath;
					this.paths = paths;
					this.rawValue = rawValue;

					memberModifier.SetValue(null, this.Resolve(memberModifier, null, 1));

					newValue = TypeHandlersManager.GetTypeHandler(lastType).Serialize(lastType, lastValue);
					return ReturnUpdateFieldValue.Success;
				}

				ServerGameObject	go = this.GetGameObject(int.Parse(paths[0]));
				if (go == null)
				{
					InternalNGDebug.LogError("GameObject \"" + paths[0] + "\" does not exist in path \"" + valuePath + "\".");
					return ReturnUpdateFieldValue.GameObjectNotFound;
				}

				// Is a field for ServerGameObject.
				if (paths[1][0] == NGServerScene.SpecialGameObjectSeparator)
				{
					paths[1] = paths[1].Remove(0, 1);

					// No time to waste on abstracting ClientGameObject, maybe later.
					switch (paths[1])
					{
						case "tag":
						case "name":
							TypeHandler	typeHandler = TypeHandlersManager.GetTypeHandler(typeof(string));

							NGServerScene.Buffer.Clear();
							NGServerScene.Buffer.Append(rawValue);

							if (paths[1] == "tag")
								go.gameObject.tag = typeHandler.Deserialize(NGServerScene.Buffer, typeof(string)) as string;
							else if (paths[1] == "name")
								go.gameObject.name = typeHandler.Deserialize(NGServerScene.Buffer, typeof(string)) as string;

							NGServerScene.Buffer.Clear();
							if (paths[1] == "tag")
								typeHandler.Serialize(NGServerScene.Buffer, typeof(string), go.gameObject.tag);
							else if (paths[1] == "name")
								typeHandler.Serialize(NGServerScene.Buffer, typeof(string), go.gameObject.name);

							newValue = NGServerScene.Buffer.Flush();
							return ReturnUpdateFieldValue.Success;

						case "active":
						case "isStatic":
							typeHandler = TypeHandlersManager.GetTypeHandler(typeof(bool));

							NGServerScene.Buffer.Clear();
							NGServerScene.Buffer.Append(rawValue);

							if (paths[1] == "active")
							{
								if (go.gameObject == this.gameObject)
									return ReturnUpdateFieldValue.DisableServerForbidden;
								else
									go.gameObject.SetActive((bool)typeHandler.Deserialize(NGServerScene.Buffer, typeof(bool)));
							}
							else if (paths[1] == "isStatic")
								go.gameObject.isStatic = (bool)typeHandler.Deserialize(NGServerScene.Buffer, typeof(bool));

							NGServerScene.Buffer.Clear();
							if (paths[1] == "active")
								typeHandler.Serialize(NGServerScene.Buffer, typeof(bool), go.gameObject.activeSelf);
							else if (paths[1] == "isStatic")
								typeHandler.Serialize(NGServerScene.Buffer, typeof(bool), go.gameObject.isStatic);

							newValue = NGServerScene.Buffer.Flush();
							return ReturnUpdateFieldValue.Success;

						case "layer":
							typeHandler = TypeHandlersManager.GetTypeHandler(typeof(int));

							NGServerScene.Buffer.Clear();
							NGServerScene.Buffer.Append(rawValue);

							go.gameObject.layer = (int)typeHandler.Deserialize(NGServerScene.Buffer, typeof(int));

							NGServerScene.Buffer.Clear();
							typeHandler.Serialize(NGServerScene.Buffer, typeof(int), go.gameObject.layer);

							newValue = NGServerScene.Buffer.Flush();
							return ReturnUpdateFieldValue.Success;

						default:
#if UNITY_WSA_8_1 || UNITY_WP_8_1
							throw new MissingMemberException(valuePath);
#else
							throw new MissingFieldException(valuePath);
#endif
					}
				}

				ServerComponent	b = go.GetComponent(int.Parse(paths[1]));
				if (b == null)
				{
					Debug.LogError("Component \"" + paths[1] + "\" does not exist in path \"" + valuePath + "\".");
					return ReturnUpdateFieldValue.ComponentNotFound;
				}

				IFieldModifier	f = b.GetField(int.Parse(paths[2]));
				if (f == null)
				{
					InternalNGDebug.LogError("Path \"" + valuePath + "\" is invalid.");
					return ReturnUpdateFieldValue.PathNotResolved;
				}

				this.valuePath = valuePath;
				this.paths = paths;
				this.rawValue = rawValue;

				f.SetValue(b.component, this.Resolve(f, b.component, 2));

				newValue = TypeHandlersManager.GetTypeHandler(lastType).Serialize(lastType, lastValue);
				return ReturnUpdateFieldValue.Success;
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException("Path="+ valuePath, ex);
			}

			return ReturnUpdateFieldValue.InternalError;
		}

		public NGShader	GetNGShader(Shader shader)
		{
			if (shader == null)
				return null;

			if (this.shaderProperties == null)
			{
				if (this.shaderReferences == null ||
					this.shaderReferences.shaders.Length == 0)
				{
					if (this.shaderScanWarningOnce == false)
					{
						this.shaderScanWarningOnce = true;
						InternalNGDebug.LogWarning("Materials need to be first cached before you build the project. (NG Server Scene -> Shaders -> Scan)");
					}
					return null;
				}

				NGServerScene.Buffer.Clear();
				NGServerScene.Buffer.Append(this.shaderReferences.properties);

				int	total = NGServerScene.Buffer.ReadInt32();

				this.shaderProperties = new NGShader[total];

				for (int i = 0; i < total; i++)
					this.shaderProperties[i] = new NGShader(NGServerScene.Buffer);
			}

			for (int i = 0; i < this.shaderProperties.Length; i++)
			{
				if (this.shaderProperties[i].name.Equals(shader.name) == true)
					return this.shaderProperties[i];
			}

			InternalNGDebug.LogWarning("Shader \"" + shader + "\" was not found in cache. You might need to refresh the Shader cache in NG Server Scene.", shader);

			return null;
		}

		public IEnumerable<Object>	EachUserAssets(Type type)
		{
			if (type == typeof(Texture2D))
			{
				for (int i = 0; i < this.userTextur2Ds.Count; i++)
					yield return this.userTextur2Ds[i];
			}
			else if (type == typeof(Sprite))
			{
				for (int i = 0; i < this.userSprites.Count; i++)
					yield return this.userSprites[i];
			}
		}

		public Texture2D		CreateUserTexture2D(string name, byte[] raw)
		{
			Texture2D	custom = null;

			for (int i = 0; i < this.userTextur2Ds.Count; i++)
			{
				if (this.userTextur2Ds[i].name == name)
				{
					custom = this.userTextur2Ds[i];
					break;
				}
			}

			if (custom == null)
			{
				custom = new Texture2D(0, 0);
				custom.name = name;
				this.userTextur2Ds.Add(custom);

				this.RegisterResource(typeof(Texture2D), custom);
			}

			custom.LoadImage(raw);

			return custom;
		}

		public Sprite			CreateUserSprite(string name, byte[] raw, Rect rect, Vector2 pivot, float pixelsPerUnit)
		{
			Sprite	custom = null;

			for (int i = 0; i < this.userSprites.Count; i++)
			{
				if (this.userSprites[i].name == name)
				{
					custom = this.userSprites[i];
					break;
				}
			}

			if (custom == null)
			{
				Texture2D	texture = new Texture2D(0, 0, TextureFormat.Alpha8, false);
				texture.LoadImage(raw);
				custom = Sprite.Create(texture, rect, pivot, pixelsPerUnit);
				custom.name = name;
				this.userSprites.Add(custom);

				this.RegisterResource(typeof(Sprite), custom);
			}
			else
				custom.texture.LoadImage(raw);

			return custom;
		}

		public Type[]			LoadInspectableTypes()
		{
			if (this.inspectableStaticTypes == null)
			{
				List<Type>	staticTypes = new List<Type>(2048);

				foreach (Type type in Utility.EachAllSubClassesOf(typeof(object)))
				{
					if (type.IsEnum == true || type.Name.StartsWith("<Private") == true || typeof(Delegate).IsAssignableFrom(type) == true)
						continue;

					FieldInfo[]	fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

					if (fields.Length > 0)
					{
						if (staticTypes.Contains(type) == false)
							staticTypes.Add(type);
					}
					else
					{
						PropertyInfo[]	properties = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

						if (properties.Length > 0)
						{
							if (staticTypes.Contains(type) == false)
								staticTypes.Add(type);
						}
					}
				}

				staticTypes.Sort((a, b) => a.Name.CompareTo(b.Name));

				this.inspectableStaticTypes = staticTypes.ToArray();
			}

			return this.inspectableStaticTypes;
		}

		public IFieldModifier[]	LoadTypeStaticMembers(int typeIndex)
		{
			IFieldModifier[]	members;

			if (this.staticTypesMembers.TryGetValue(typeIndex, out members) == false)
			{
				if (typeIndex < 0 || typeIndex >= this.inspectableStaticTypes.Length)
					throw new PacketFailureException(Errors.Server_TypeIndexOutOfRange, "Type index is out of range.");

				List<IFieldModifier>	list = new List<IFieldModifier>();
				Type					type = this.inspectableStaticTypes[typeIndex];

				if (type != null)
				{
					FieldInfo[]		fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
					PropertyInfo[]	properties = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

					for (int i = 0; i < fields.Length; i++)
						list.Add(new FieldModifier(fields[i]));

					for (int i = 0; i < properties.Length; i++)
					{
						if (properties[i].GetGetMethod() != null)
						{
							string	niceName = Utility.NicifyVariableName(properties[i].Name);
							int		j = 0;

							for (; j < list.Count; j++)
							{
								if (Utility.NicifyVariableName(list[j].Name) == niceName)
									break;
							}

							if (j == list.Count)
								list.Add(new PropertyModifier(properties[i]));
						}
					}
				}

				members = list.ToArray();

				this.staticTypesMembers.Add(typeIndex, members);
			}

			return members;
		}

		private IObjectImporter[]	objectImporters;

		public ReturnRawDataFromObject	GetRawDataFromObject(Type type, int instanceID, out Type realType, out byte[] data, out Exception exception)
		{
			if (this.objectImporters == null)
			{
				List<IObjectImporter>	list = new List<IObjectImporter>();

				foreach (Type subType in Utility.EachAllAssignableFrom(typeof(IObjectImporter)))
					list.Add(Activator.CreateInstance(subType) as IObjectImporter);

				this.objectImporters = list.ToArray();
			}

			Object	asset = this.GetResource(type, instanceID);

			if (asset == null)
				asset = this.TryGetResource(instanceID);

			realType = null;
			data = null;
			exception = null;

			if (asset != null)
			{
				try
				{
					realType = asset.GetType();

					for (int i = 0; i < this.objectImporters.Length; i++)
					{
						if (this.objectImporters[i].CanHandle(realType) == true)
						{
							data = this.objectImporters[i].ToBinary(asset);
							return ReturnRawDataFromObject.Success;
						}
					}
				}
				catch (Exception ex)
				{
					InternalNGDebug.VerboseLogException(ex);
					exception = ex;
					return ReturnRawDataFromObject.ExportThrownException;
				}
			}

			return ReturnRawDataFromObject.AssetNotFound;
		}

		private void	ProcessRootGameObject(ServerScene scene, GameObject gameObject)
		{
			int					instanceID = gameObject.GetInstanceID();
			ServerGameObject	serverGameObject;

			if (this.cachedGameObjects.TryGetValue(instanceID, out serverGameObject) == true)
			{
				if (scene.roots.Contains(serverGameObject) == false)
				{
					scene.roots.Add(serverGameObject);
					serverGameObject.RefreshChildren(this.cachedGameObjects);
				}
			}
			else
			{
				serverGameObject = new ServerGameObject(gameObject, this.cachedGameObjects);
				scene.roots.Add(serverGameObject);
			}
		}

		private object	Resolve(IFieldModifier field, object instance, int i)
		{
			Type	type = field.Type;

			if (i == this.paths.Length - 1)
			{
				// If an array resize.
				if (type.IsUnityArray() == true)
				{
					object		array = field.GetValue(instance);
					Type		subType = Utility.GetArraySubType(type);
					TypeHandler	subTypeHandler = TypeHandlersManager.GetTypeHandler(subType);

					NGServerScene.Buffer.Clear();
					NGServerScene.Buffer.Append(rawValue);

					TypeHandler	typeHandler = TypeHandlersManager.GetTypeHandler(typeof(int));
					int			newSize = (int)typeHandler.DeserializeRealValue(this, NGServerScene.Buffer, typeof(int));

					if (type.IsArray == true)
					{
						Array	originArray = array as Array;

						if (originArray == null || originArray.Length != newSize)
						{
							Array	newArray = Array.CreateInstance(subType, newSize);
							int		k = 0;

							if (originArray != null)
							{
								for (; k < newSize && k < originArray.Length; k++)
									newArray.SetValue(originArray.GetValue(k), k);
							}

							for (; k < newSize; k++)
							{
								object	item;

								if (typeof(Object).IsAssignableFrom(subType) == true)
									item = null;
								else if (subType == typeof(string)) // WTF do we have to do this trick. -_-
									item = string.Empty;
								else if (subType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) == null)
									item = FormatterServices.GetUninitializedObject(subType);
								else
									item = Activator.CreateInstance(subType);

								this.listener.BroadcastPostPacket(new NotifyFieldValueUpdatedPacket(this.valuePath + NGServerScene.ValuePathSeparator + k, subTypeHandler.Serialize(subType, item)));

								newArray.SetValue(item, k);
							}

							array = newArray;
						}
					}
					else if (typeof(IList).IsAssignableFrom(type) == true)
					{
						IList	originArray = array as IList;

						if (originArray == null)
						{
							array = Activator.CreateInstance(type);
							originArray = array as IList;
						}

						if (originArray.Count != newSize)
						{
							while (originArray.Count > newSize)
								originArray.RemoveAt(originArray.Count - 1);

							while (originArray.Count < newSize)
							{
								object	item;

								if (typeof(Object).IsAssignableFrom(subType) == true)
									item = null;
								else if (subType == typeof(string)) // WTF do we have to do this trick. -_-
									item = string.Empty;
								else if (subType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) == null)
									item = FormatterServices.GetUninitializedObject(subType);
								else
									item = Activator.CreateInstance(subType);

								this.listener.BroadcastPostPacket(new NotifyFieldValueUpdatedPacket(this.valuePath + NGServerScene.ValuePathSeparator + originArray.Count, subTypeHandler.Serialize(subType, item)));

								originArray.Add(item);
							}
						}
					}
					else
						throw new InvalidCastException("Type at \"" + this.valuePath + "\" is not supported as an array.");

					lastType = type;
					lastValue = array;

					return array;
				}
				else
				{
					TypeHandler	typeHandler = TypeHandlersManager.GetTypeHandler(type);
					InternalNGDebug.Assert(typeHandler != null, "TypeHandler for type " + type + " does not exist.");

					NGServerScene.Buffer.Clear();
					NGServerScene.Buffer.Append(rawValue);

					lastType = type;
					lastValue = typeHandler.DeserializeRealValue(this, NGServerScene.Buffer, type);

					return lastValue;
				}
			}
			else if (type.IsUnityArray() == true) // Any Array or IList.
			{
				object	array = field.GetValue(instance);
				Type	subType = Utility.GetArraySubType(type);
				int		index = int.Parse(paths[i + 1]);

				if (type.IsArray == true)
					(array as Array).SetValue(this.ResolveArray(subType, (array as Array).GetValue(index), i + 1), index);
				else if (typeof(IList).IsAssignableFrom(type) == true)
					(array as IList)[index] = this.ResolveArray(subType, (array as IList)[index], i + 1);
				else
					throw new InvalidCastException("Type at \"" + this.valuePath + "\" is not supported as an array.");

				return array;
			}
			else if (type.IsClass() == true || // Any class.
					 type.IsStruct() == true) // Any struct.
			{
				object			fieldInstance = field.GetValue(instance);
				IFieldModifier	fieldInfo = Utility.GetFieldInfo(field.Type, this.paths[i + 1]);

				fieldInfo.SetValue(fieldInstance, this.Resolve(fieldInfo, fieldInstance, i + 1));

				return fieldInstance;
			}

			throw new InvalidOperationException("Resolve failed at " + i + " with " + instance + " of type " + type + ".");
		}

		private object	ResolveArray(Type type, object instance, int i)
		{
			if (i == this.paths.Length - 1)
			{
				TypeHandler	typeHandler = TypeHandlersManager.GetTypeHandler(type);
				InternalNGDebug.Assert(typeHandler != null, "TypeHandler for type " + type + " does not exist.");

				NGServerScene.Buffer.Clear();
				NGServerScene.Buffer.Append(rawValue);

				this.lastType = type;
				this.lastValue = typeHandler.DeserializeRealValue(this, NGServerScene.Buffer, type);

				return this.lastValue;
			}

			if (type.IsClass() == true || // Any class.
				type.IsStruct() == true) // Any struct.
			{
				IFieldModifier	fieldInfo = Utility.GetFieldInfo(type, this.paths[i + 1]);

				fieldInfo.SetValue(instance, this.Resolve(fieldInfo, instance, i + 1));

				return instance;
			}

			return this.Resolve(Utility.GetFieldInfo(type, this.paths[i + 1]), instance, i + 1);
		}

		private object	GetResolve(IFieldModifier field, object instance, int i)
		{
			Type	type = field.Type;

			if (i == this.paths.Length - 1)
				return field.GetValue(instance);
			else if (type.IsUnityArray() == true) // Any Array or IList.
			{
				object	array = field.GetValue(instance);
				Type	subType = Utility.GetArraySubType(type);
				int		index = int.Parse(paths[i + 1]);

				if (type.IsArray == true)
					return this.GetResolveArray(subType, (array as Array).GetValue(index), i + 1);
				else if (typeof(IList).IsAssignableFrom(type) == true)
					return this.GetResolveArray(subType, (array as IList)[index], i + 1);
				else
					throw new InvalidCastException("Type at \"" + this.valuePath + "\" is not supported as an array.");
			}
			else if (type.IsClass() == true || // Any class.
					 type.IsStruct() == true) // Any struct.
			{
				object			fieldInstance = field.GetValue(instance);
				IFieldModifier	fieldInfo = Utility.GetFieldInfo(field.Type, this.paths[i + 1]);

				return this.GetResolve(fieldInfo, fieldInstance, i + 1);
			}

			throw new InvalidOperationException("Resolve failed at " + i + " with " + instance + " of type " + type + ".");
		}

		private object	GetResolveArray(Type type, object instance, int i)
		{
			if (i == this.paths.Length - 1)
				return instance;

			if (type.IsClass() == true || // Any class.
				type.IsStruct() == true) // Any struct.
			{
				IFieldModifier	fieldInfo = Utility.GetFieldInfo(type, this.paths[i + 1]);

				return this.GetResolve(fieldInfo, instance, i + 1);
			}

			return this.GetResolve(Utility.GetFieldInfo(type, this.paths[i + 1]), instance, i + 1);
		}

		public ReturnGetArray	GetArray(string arrayPath, out IEnumerable array)
		{
			string[]	paths = arrayPath.Split(NGServerScene.ValuePathSeparator);

			array = null;

			// Check if dealing with Type.
			if ((paths[1][0] < '0' || paths[1][0] > '9') && paths[1][0] != NGServerScene.SpecialGameObjectSeparator && paths[1][0] != '-')
			{
				IFieldModifier[]	fields = this.LoadTypeStaticMembers(int.Parse(paths[0]));

				if (fields != null)
				{
					for (int i = 0; i < fields.Length; i++)
					{
						if (fields[i].Name == paths[1])
						{
							array = fields[i].GetValue(null) as IEnumerable;
							return ReturnGetArray.Success;
						}
					}
				}

				return ReturnGetArray.TypeNotFound;
			}

			ServerGameObject	go = this.GetGameObject(int.Parse(paths[0]));

			if (go == null)
			{
				Debug.LogError("GameObject \"" + paths[0] + "\" does not exist in path \"" + arrayPath + "\".");
				return ReturnGetArray.GameObjectNotFound;
			}

			ServerComponent	b = go.GetComponent(int.Parse(paths[1]));

			if (b == null)
			{
				Debug.LogError("Component \"" + paths[1] + "\" does not exist in path \"" + arrayPath + "\".");
				return ReturnGetArray.ComponentNotFound;
			}

			IFieldModifier	f = b.GetField(int.Parse(paths[2]));

			if (f == null)
			{
				Debug.LogError("Path \"" + this.valuePath + "\" is invalid.");
				return ReturnGetArray.PathNotResolved;
			}

			this.valuePath = arrayPath;
			this.paths = paths;

			array = this.GetResolve(f, b.component, 2) as IEnumerable;
			return ReturnGetArray.Success;
		}

		private void	OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			this.listener.BroadcastPacket(new ServerNotifySceneChangedPacket());
		}

		private void	OnSceneUnloaded(Scene scene)
		{
			this.listener.BroadcastPacket(new ServerNotifySceneChangedPacket());
		}

		private void	OnActiveSceneChanged(Scene oldScene, Scene newScene)
		{
			this.listener.BroadcastPacket(new ServerNotifySceneChangedPacket());
		}
	}
}