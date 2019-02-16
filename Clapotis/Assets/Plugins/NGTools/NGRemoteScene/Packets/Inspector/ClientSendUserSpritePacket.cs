using NGTools.Network;
using UnityEngine;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Asset_ClientSendUserSprite)]
	internal sealed class ClientSendUserSpritePacket : Packet, IGUIPacket
	{
		public string	name;
		public byte[]	raw;
		public Rect		rect;
		public Vector2	pivot;
		public float	pixelsPerUnit;

		private string	cachedLabel;

		public	ClientSendUserSpritePacket(string name, byte[] raw, Rect rect, Vector2 pivot, float pixelsPerUnit)
		{
			this.name = name;
			this.raw = raw;
			this.rect = rect;
			this.pivot = pivot;
			this.pixelsPerUnit = pixelsPerUnit;
		}

		private	ClientSendUserSpritePacket(ByteBuffer buffer) : base(buffer)
		{
		}

		void	IGUIPacket.OnGUI(IUnityData unityData)
		{
			if (this.cachedLabel == null)
				this.cachedLabel = "Sending Sprite \"" + this.name + "\" (" + raw.Length + " bytes).";

			GUILayout.Label(this.cachedLabel);
		}
	}
}