using UnityEngine;

namespace Clapotis
{
	public class AudioRandomizer : MonoBehaviour
	{
		private static float	lastPlay;

		public AudioSource	source;
		public AudioClip[]	clips;

		public void	PlayRandom(bool byPassConcurrency = false)
		{
			if (this.clips.Length > 0)
			{
				if (byPassConcurrency == true || Time.time - AudioRandomizer.lastPlay > 2F)
				{
					this.source.clip = this.clips[Random.Range(0, this.clips.Length)];
					this.source.Play();
					AudioRandomizer.lastPlay = Time.time;
				}
				else
				{
					this.source.clip = this.clips[Random.Range(0, this.clips.Length)];
					this.source.PlayDelayed(2F);
					AudioRandomizer.lastPlay = Time.time + 2F;
				}
			}
		}
	}
}