namespace NGTools.Network
{
	[PacketLinkTo(PacketId.PartialResponsePacket)]
	public class PartialResponsePacket : ResponsePacket
	{
		public	PartialResponsePacket(int networkId) : base(networkId)
		{
		}
	}
}