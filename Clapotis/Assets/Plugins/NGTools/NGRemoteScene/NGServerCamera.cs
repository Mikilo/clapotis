using NGTools.Network;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NGTools.NGRemoteScene
{
	public class NGServerCamera : MonoBehaviour, ICameraScreenshotData
	{
		public const int			TargetRefreshMin = 5;
		public const int			TargetRefreshMax = 200;
		public static RaycastHit[]	RaycastResult = new RaycastHit[10];

		public NGServerScene	scene;
		public Client			sender;

		public NGServerScene		ServerScene { get { return this.scene; } }
		public Client				Sender { get { return this.sender; } }
		public int					Width { get { return this.width; } }
		public int					Height { get { return this.height; } }
		public int					TargetRefresh { get { return this.targetRefresh; } }
		public bool					Wireframe { get { return this.wireframe; } }
		public Camera				TargetCamera { get { return this.targetCamera; } }
		public RenderTexture		RenderTexture { get { return this.renderTexture; } }
		public AbstractTcpListener	TCPListener { get { return this.scene.listener; } }

		public int					width = 800;
		public int					height = 600;
		public int					depth = 24;
		public RenderTextureFormat	renderTextureFormat = RenderTextureFormat.ARGB32;

		public bool		wireframe = false;
		[Range(NGServerCamera.TargetRefreshMin, NGServerCamera.TargetRefreshMax)]
		public int		targetRefresh = 24;

		public bool		moveForward;
		public bool		moveBackward;
		public bool		moveLeft;
		public bool		moveRight;
		public float	moveSpeed;

		public Camera			ghostCamera;
		public Camera			targetCamera;
		public RenderTexture	renderTexture;

		public int		FPS;
		public int		FPSSent;
		public float	nextFPSTime;

		private ScreenshotModule	screenshotModule;

		private Vector3		lastPosition;
		private Vector3		lastEulerAngles;

		private Vector3	viewportRay = new Vector3();

		protected virtual void	Awake()
		{
			// Doing it twice might by redundant, but we never know.
			SceneManager.sceneLoaded += this.OnSceneLoaded;
			SceneManager.sceneUnloaded += this.OnSceneUnloaded;
		}

		protected virtual void	OnDestroy()
		{
			this.screenshotModule.OnDestroy(this.scene);

			if (this.ServerScene != null)
				this.ServerScene.listener.ClientDisconnected -= this.OnClientDisconnected;

			SceneManager.sceneLoaded -= this.OnSceneLoaded;
			SceneManager.sceneUnloaded -= this.OnSceneUnloaded;
		}

		/// Init is called everytime a NG Camera is connecting.
		public void	Init()
		{
			if (this.screenshotModule == null)
			{
				this.screenshotModule = new ScreenshotModule();
				this.screenshotModule.Awake(this.scene);

				this.ServerScene.listener.ClientDisconnected += this.OnClientDisconnected;
			}

			this.targetCamera = this.ghostCamera;
			this.renderTexture = new RenderTexture(this.width, this.height, this.depth, this.renderTextureFormat, RenderTextureReadWrite.sRGB);
		}

		protected virtual void	OnGUI()
		{
			this.screenshotModule.OnGUI(this);
		}

		protected virtual void	Update()
		{
			this.screenshotModule.Update(this);

			float	t = Time.unscaledTime;
			this.FPSSent++;
			if (t >= this.nextFPSTime)
			{
				this.FPS = this.FPSSent;
				this.FPSSent = 0;
				this.nextFPSTime = t + 1F;
			}

			if (this.targetCamera != null)
			{
				if (this.moveForward == true)
					this.targetCamera.transform.localPosition += this.targetCamera.transform.forward * Time.unscaledDeltaTime * this.moveSpeed;
				else if (this.moveBackward == true)
					this.targetCamera.transform.localPosition -= this.targetCamera.transform.forward * Time.unscaledDeltaTime * this.moveSpeed;
				if (this.moveLeft == true)
					this.targetCamera.transform.localPosition -= this.targetCamera.transform.right * Time.unscaledDeltaTime * this.moveSpeed;
				else if (this.moveRight == true)
					this.targetCamera.transform.localPosition += this.targetCamera.transform.right * Time.unscaledDeltaTime * this.moveSpeed;

				if (this.targetCamera.transform.position != this.lastPosition ||
					this.targetCamera.transform.eulerAngles != this.lastEulerAngles)
				{
					this.lastPosition = this.targetCamera.transform.position;
					this.lastEulerAngles = this.targetCamera.transform.eulerAngles;

					this.sender.AddPacket(new NotifyCameraTransformPacket(this.targetCamera.transform.position, this.targetCamera.transform.eulerAngles.x, this.targetCamera.transform.eulerAngles.y));
				}
			}
		}

		public void	SetTransformPosition(Vector3 position)
		{
			this.targetCamera.transform.position = position;
			this.lastPosition = this.targetCamera.transform.position;
		}

		public void	SetTransformRotation(Vector2 rotation)
		{
			this.targetCamera.transform.eulerAngles = new Vector3(rotation.x, rotation.y);
			this.lastEulerAngles = this.targetCamera.transform.eulerAngles;
		}

		public void	Zoom(float factor)
		{
			this.targetCamera.transform.position += this.targetCamera.transform.forward * factor;
		}

		public void	Raycast(Camera camera, List<GameObject> result, float viewportX, float viewportY)
		{
			this.viewportRay.x = viewportX;
			this.viewportRay.y = viewportY;

			// Assign the render texture to force camera to work on the same screen resolution.
			RenderTexture	r = camera.targetTexture;
			camera.targetTexture = this.renderTexture;
			Ray				ray = camera.ViewportPointToRay(this.viewportRay);
			camera.targetTexture = r;

			int	n = Physics.RaycastNonAlloc(ray, NGServerCamera.RaycastResult, float.MaxValue);

			if (Conf.DebugMode != Conf.DebugState.None)
			{
				Debug.DrawRay(ray.origin, ray.direction * camera.farClipPlane, Color.blue, 3F, true);
#if !NGTOOLS
				NGDebug.Log(NGServerCamera.RaycastResult);
#endif
			}

			result.Clear();

			for (int i = 0; i < n; i++)
				result.Add(NGServerCamera.RaycastResult[i].collider.gameObject);
		}

		private void	OnSceneUnloaded(Scene scene)
		{
			this.SendCamerasToSender();
		}

		private void	OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			this.SendCamerasToSender();
		}

		private void	SendCamerasToSender()
		{
			this.sender.AddPacket(new NotifyAllCamerasPacket(this.ghostCamera != null ? this.ghostCamera.GetInstanceID() : 0));
		}

		private void	OnClientDisconnected(Client client)
		{
			if (client == this.sender)
				this.enabled = false;
		}
	}
}