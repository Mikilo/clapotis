namespace NGToolsEditor.NGConsole
{
	public abstract class Theme
	{
		/// <summary>
		/// Overwrites style in the given <paramref name="instance"/>.
		/// </summary>
		/// <param name="instance"></param>
		public abstract void	SetTheme(NGSettings instance);
	}
}