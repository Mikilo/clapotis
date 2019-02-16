using System.Collections.Generic;
using UnityEngine;

namespace NGToolsEditor.NGSyncFolders
{
	public class SyncFoldersSettings : ScriptableObject
	{
		public List<Profile>	syncProfiles = new List<Profile>();
	}
}