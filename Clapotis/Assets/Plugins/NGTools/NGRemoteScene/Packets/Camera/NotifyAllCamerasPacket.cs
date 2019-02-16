using NGTools.Network;
using System.Collections.Generic;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Camera_NotifyAllCameras)]
	internal sealed class NotifyAllCamerasPacket : Packet
	{
		private static List<Component>	reuse = new List<Component>();

		public int[]		IDs;
		public int[]		gameObjectIDs;
		public string[]		names;
		public int[][]		componentsIDs;
		public string[][]	componentsTypes;
		public int			ghostCameraId;

		public	NotifyAllCamerasPacket(int ghostCameraId)
		{
			this.ghostCameraId = ghostCameraId;

			Camera[]	cameras = Resources.FindObjectsOfTypeAll<Camera>();

			this.IDs = new int[cameras.Length];
			this.gameObjectIDs = new int[cameras.Length];
			this.names = new string[cameras.Length];
			this.componentsIDs = new int[cameras.Length][];
			this.componentsTypes = new string[cameras.Length][];

			for (int i = 0; i < cameras.Length; i++)
			{
				this.IDs[i] = cameras[i].GetInstanceID();
				if ((cameras[i].gameObject.hideFlags & HideFlags.HideInHierarchy) == 0)
					this.gameObjectIDs[i] = cameras[i].gameObject.GetInstanceID();
				else
					this.gameObjectIDs[i] = 0;
				this.names[i] = cameras[i].name;

				cameras[i].GetComponents<Component>(NotifyAllCamerasPacket.reuse);

				this.componentsIDs[i] = new int[NotifyAllCamerasPacket.reuse.Count];
				this.componentsTypes[i] = new string[NotifyAllCamerasPacket.reuse.Count];
				for (int j = 0; j < NotifyAllCamerasPacket.reuse.Count; j++)
				{
					if (NotifyAllCamerasPacket.reuse[j] != null)
					{
						this.componentsIDs[i][j] = NotifyAllCamerasPacket.reuse[j].GetInstanceID();
						this.componentsTypes[i][j] = NotifyAllCamerasPacket.reuse[j].GetType().GetShortAssemblyType();
					}
					else
					{
						this.componentsIDs[i][j] = 0;
						this.componentsTypes[i][j] = string.Empty;
					}
				}
			}
		}

		private	NotifyAllCamerasPacket(ByteBuffer buffer) : base(buffer)
		{
		}
	}
}