using NGTools.Network;
using System.Collections.Generic;

namespace NGToolsEditor.NGRemoteScene
{
	public class PrefabConstruct
	{
		public readonly string						path;
		public readonly PrefabGameObject			rootGameObject;
		public readonly List<AssetImportParameters>	importParameters = new List<AssetImportParameters>();

		public string	outputPath;
		public string	constructionError;

		private bool	componentsVerified = false;
		private bool	importsConfirmed = false;
		private bool	assetsReady = false;

		public	PrefabConstruct(string path, ClientGameObject root)
		{
			this.path = path;
			this.rootGameObject = new PrefabGameObject(root);
		}

		public bool	VerifyComponentsReady(NGRemoteHierarchyWindow hierarchy, List<AssetImportParameters> globalRefs, Client client)
		{
			if (this.componentsVerified == false)
				this.componentsVerified = this.rootGameObject.VerifyComponentsReady(hierarchy, globalRefs, this, client);
			else if (this.importsConfirmed == false)
			{
				for (int i = 0; i < this.importParameters.Count; i++)
				{
					if (this.importParameters[i].ParametersConfirmed == false)
						return false;
					else
						this.importParameters[i].CheckImportState();
				}

				this.importsConfirmed = true;
			}
			else if (this.assetsReady == false)
			{
				for (int i = 0; i < this.importParameters.Count; i++)
				{
					ImportAssetState	state = this.importParameters[i].CheckImportState();

					if (state != ImportAssetState.Ready && state != ImportAssetState.DoesNotExist)
						return false;
				}

				this.assetsReady = true;
			}

			return this.componentsVerified == true && this.importsConfirmed == true && this.assetsReady == true;
		}
	}
}