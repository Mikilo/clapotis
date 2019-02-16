namespace NGTools.Network
{
	/// <summary>
	/// Must be implemented on class deriving from Packet. Defines a Packet that might potentially pushes a heavy memory ResponsePacket.
	/// </summary>
	public interface IPotentialMemoryDemandingPacket
	{
	}
}