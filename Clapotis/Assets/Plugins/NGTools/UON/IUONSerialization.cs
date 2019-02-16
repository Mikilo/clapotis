namespace NGTools.UON
{
	public interface IUONSerialization
	{
		void	OnSerializing();
		void	OnDeserialized(DeserializationData data);
	}
}