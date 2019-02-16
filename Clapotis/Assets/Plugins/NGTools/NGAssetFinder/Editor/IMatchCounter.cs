namespace NGToolsEditor.NGAssetFinder
{
	public interface IMatchCounter
	{
		void	AddPotentialMatchCounter(int n);
		void	AddEffectiveMatchCounter(int n);
	}
}