using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Clapotis
{
	public class Selector : MonoBehaviour
	{
		public Button[]	choices;

		private void	Awake()
		{
			for (int i = 0; i < this.choices.Length; i++)
				this.choices[i].onClick.AddListener(this.SelectChoice(i));
		}

		private UnityAction SelectChoice(int i)
		{
			return () =>
			{
				this.StopAllCoroutines();

				for (int j = 0; j < this.choices.Length; j++)
				{
					if (j != i)
					{
						this.choices[j].interactable = true;
						this.choices[j].transform.localPosition = new Vector3(28F, this.choices[j].transform.localPosition.y, 0F);
					}
				}

				this.choices[i].interactable = false;

				this.StartCoroutine(this.Animate(this.choices[i].transform));
			};
		}

		private IEnumerator	Animate(Transform t)
		{
			float	time = 0F;
			Vector3	origin = t.localPosition;

			while (time < .5F)
			{
				time += Time.deltaTime;
				t.localPosition = origin + Vector3.right * time * 50F;
				yield return null;
			}

			t.localPosition = origin + Vector3.right * .5F * 50F;
		}
	}
}