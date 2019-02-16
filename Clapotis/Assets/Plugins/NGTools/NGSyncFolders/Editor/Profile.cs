using System;
using System.Collections.Generic;

namespace NGToolsEditor.NGSyncFolders
{
	[Serializable]
	public sealed class Profile
	{
		public string			name = string.Empty;
		public string			relativePath = string.Empty;
		public Project			master = new Project();
		public List<Project>	slaves = new List<Project>();

		public bool				useCache = true;
		public List<FilterText>	filters = new List<FilterText>();
	}
}