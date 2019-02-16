using UnityEngine;

namespace NGTools.NGRemoteScene
{
	internal sealed class TouchModule : CameraServerDataModule
	{
		public const int	ModuleID = 4;
		public const int	Priority = 500;
		public const string	Name = "Touch";

		public	TouchModule() : base(TouchModule.ModuleID, TouchModule.Priority, TouchModule.Name)
		{
		}

		public override void	Update(ICameraData data)
		{
			if (Input.touchCount == 0)
				return;

			ByteBuffer	buffer = Utility.GetBBuffer();

			buffer.Append((byte)Input.touchCount);

			for (int i = 0; i < Input.touchCount; i++)
			{
				Touch	touch = Input.GetTouch(i);

				buffer.Append(touch.position.x);
				buffer.Append(Screen.height - touch.position.y);
				buffer.Append(touch.fingerId);
			}

			data.TCPListener.BroadcastPacket(new NotifyCameraDataPacket(this.moduleID, Time.time, Utility.ReturnBBuffer(buffer)));
		}
	}
}