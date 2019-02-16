namespace NGToolsEditor.NGSpotlight
{
	public static class MenuItemsImporter
	{
		[SpotlightUpdatingResult]
		private static void	LazyImport()
		{
			NGSpotlightWindow.UpdatingResult -= MenuItemsImporter.LazyImport;

			string[]	menus = Utility.GetAllMenuItems();

			for (int i = 0; i < menus.Length; i++)
			{
				if (menus[i].StartsWith("CONTEXT/") == true)
					continue;

				NGSpotlightWindow.AddEntry("menuitems", new MenuItemDrawer(menus[i]));
			}
		}
	}
}