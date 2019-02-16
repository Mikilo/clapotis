using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace NGTools.NGGameConsole
{
	public class FourCornersActivator : MonoBehaviour
	{
		public enum Corner
		{
			TopLeft,
			TopRight,
			BottomRight,
			BottomLeft
		}

		public UnityEvent		action;
		public Corner[]			password;
		public float			cornerSizeInPixel = 100F;
		public float			cooldownInSecond = 5F;

		private int			currentStep;
		private Coroutine	stop;

		protected virtual void	Awake()
		{
			if (this.password.Length == 0)
			{
				InternalNGDebug.LogWarning("Password is required.", this);
				this.enabled = false;
			}
		}

		protected virtual void	FixedUpdate()
		{
			if (this.currentStep == this.password.Length)
			{
				this.currentStep = 0;
				this.action.Invoke();
				if (this.stop != null)
				{
					this.StopCoroutine(this.stop);
					this.stop = null;
				}
			}

			Corner	target = this.password[this.currentStep];

			if (target == Corner.TopLeft)
			{
				if (Input.mousePosition.x <= this.cornerSizeInPixel && Input.mousePosition.y >= Screen.height - this.cornerSizeInPixel)
					this.Increment();
			}
			else if (target == Corner.TopRight)
			{
				if (Input.mousePosition.x >= Screen.width - this.cornerSizeInPixel && Input.mousePosition.y >= Screen.height - this.cornerSizeInPixel)
					this.Increment();
			}
			else if (target == Corner.BottomLeft)
			{
				if (Input.mousePosition.x <= this.cornerSizeInPixel && Input.mousePosition.y <= this.cornerSizeInPixel)
					this.Increment();
			}
			else if (target == Corner.BottomRight)
			{
				if (Input.mousePosition.x >= Screen.width - this.cornerSizeInPixel && Input.mousePosition.y <= this.cornerSizeInPixel)
					this.Increment();
			}
		}

		private IEnumerator	ResetCooldown()
		{
			yield return new WaitForSeconds(this.cooldownInSecond);
			this.currentStep = 0;
		}

		private void	Increment()
		{
			++this.currentStep;
			if (this.currentStep == 1)
				this.StartCoroutine(this.ResetCooldown());
		}
	}
}