#pragma warning disable 618
using UnityEngine;
using UnityEngine.Networking;

namespace Clapotis
{
	public class NetworkTest : MonoBehaviour
	{
		public NetworkManager netManager;

		public string	address;
		public string	port;

		private void	OnGUI()
		{
			if (GUILayout.Button("Snapshot netManager") == true)
				NGDebug.Snapshot(this.netManager);

			GUILayout.Label("Address");
			this.address = GUILayout.TextField(this.address);
			GUILayout.Label("Port");
			this.port = GUILayout.TextField(this.port);

			if (GUILayout.Button("Host") == true)
			{
				this.netManager.networkAddress = this.address;
				this.netManager.networkPort = int.Parse(this.port);
				this.netManager.StartHost();
			}

			if (GUILayout.Button("Connect") == true)
			{
				this.netManager.networkAddress = this.address;
				this.netManager.networkPort = int.Parse(this.port);
				this.netManager.StartClient();
			}

			if (this.netManager.isNetworkActive == true)
			{
				if (GUILayout.Button("StopHost") == true)
					this.netManager.StopHost();
				if (GUILayout.Button("StopClient") == true)
					this.netManager.StopClient();
			}
		}
	}
}