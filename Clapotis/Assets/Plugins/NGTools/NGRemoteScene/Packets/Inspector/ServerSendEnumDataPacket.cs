using NGTools.Network;
using System;

namespace NGTools.NGRemoteScene
{
	[PacketLinkTo(RemoteScenePacketId.Unity_ServerSendEnumData)]
	internal sealed class ServerSendEnumDataPacket : ResponsePacket
	{
		public string	type;
		public bool		hasFlagAttribute;
		public string[]	names;
		public int[]	values;

		public	ServerSendEnumDataPacket(int networkId) : base(networkId)
		{
		}

		private	ServerSendEnumDataPacket(ByteBuffer buffer) : base(buffer)
		{
		}

		public void	Init(string type)
		{
			Type	t = Type.GetType(type);

			this.type = type;
			this.hasFlagAttribute = t.IsDefined(typeof(FlagsAttribute), false);
			this.names = Enum.GetNames(t);

			Array	a = Enum.GetValues(t);
			this.values = new int[a.Length];
			for (int i = 0; i < this.values.Length; i++)
				this.values[i] = (int)a.GetValue(i);
		}
	}
}