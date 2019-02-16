using UnityEngine;

namespace NGTools.Network
{
	public interface IGUIPacket
	{
		/// <summary>
		/// Must draw a description of the packet in a single line of 16F.
		/// </summary>
		/// <param name="unityData"></param>
		void	OnGUI(IUnityData unityData);
	}
}