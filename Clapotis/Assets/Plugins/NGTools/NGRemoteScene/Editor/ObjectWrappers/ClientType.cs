using NGTools.Network;
using NGTools.NGRemoteScene;
using System;

namespace NGToolsEditor.NGRemoteScene
{
	public sealed class ClientType
	{
		private bool	tryType;
		private Type	type;
		public Type		Type
		{
			get
			{
				if (this.tryType == false)
				{
					this.tryType = true;
					this.type = Utility.GetType(this.@namespace, this.name);
				}

				return this.type;
			}
		}

		public readonly int		typeIndex;
		public readonly string	name;
		public readonly string	@namespace;

		public	ClientStaticMember[]	members;

		public	ClientType(int typeIndex, string name, string @namespace)
		{
			this.typeIndex = typeIndex;
			this.name = name;
			this.@namespace = @namespace;
		}

		public void		LoadInspectableTypeStaticMembers(Client client, IUnityData unityData)
		{
			if (this.members == null)
				client.AddPacket(new ClientRequestTypeStaticMembersPacket(this.typeIndex), this.OnTypeStaticMembersReceived(unityData));
		}

		private Action<ResponsePacket>	OnTypeStaticMembersReceived(IUnityData unityData)
		{
			return p =>
			{
				if (p.CheckPacketStatus() == true)
				{
					ServerSendTypeStaticMembersPacket	packet = p as ServerSendTypeStaticMembersPacket;

					this.members = new ClientStaticMember[packet.netMembers.Length];
					for (int i = 0; i < this.members.Length; i++)
					{
						if (packet.netMembers[i] != null)
							this.members[i] = new ClientStaticMember(packet.typeIndex, packet.netMembers[i], packet.editableMembers[i], unityData);
						else
							this.members[i] = ClientStaticMember.Empty;
					}
				}
			};
		}
	}
}