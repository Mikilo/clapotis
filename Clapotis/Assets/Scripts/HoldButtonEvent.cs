using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Clapotis
{
	public class HoldButtonEvent : MonoBehaviour
	{
		public UnityEvent	onComplete;

		public Image	image;
		public float	duration;

		private Coroutine	holdRoutine;

		private void	OnEnable()
		{
			this.image.fillAmount = 1F;
		}

		public void	HoldOn()
		{
			this.holdRoutine = this.StartCoroutine(this.Hold());
		}

		public void	HoldUp()
		{
			this.StopCoroutine(this.holdRoutine);
			this.holdRoutine = null;

			this.image.fillAmount = 1F;
		}

		private IEnumerator	Hold()
		{
			float	time = 0F;

			while (time < this.duration)
			{
				time += Time.deltaTime;
				this.image.fillAmount = 1F - (time / this.duration);
				yield return null;
			}

			this.onComplete.Invoke();
		}
	}
}