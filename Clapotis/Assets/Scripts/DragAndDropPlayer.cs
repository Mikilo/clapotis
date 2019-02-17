using UnityEngine;
using UnityEngine.UI;

namespace Clapotis
{
	public class DragAndDropPlayer : MonoBehaviour
	{
		public GameManager	gameManager;
		public Button		confirmButton;

		private bool	isDragging;
		public Vector3	originPosition;

		private void	Awake()
		{
			this.originPosition = this.transform.position;
		}

		public void	ResetPosition()
		{
			this.transform.position = this.originPosition;
		}

		private void	Update()
		{
			if (this.gameManager.gamePhase != GamePhase.AskPlayerSpot)
				return;

			if (this.isDragging == false)
			{
				if (Input.GetMouseButtonDown(0) == true)
				{
					RaycastHit	hit;

					if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, float.MaxValue, 1 << 10) == true)
					{
						this.isDragging = true;
					}
				}
			}
			else
			{
				if (Input.GetMouseButton(0) == true)
				{
					if (this.confirmButton.isActiveAndEnabled == true)
						this.confirmButton.gameObject.SetActive(false);

					RaycastHit hit;

					if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, float.MaxValue, (1 << 11) | (1 << 9)) == true)
					{
						if (hit.transform.gameObject.layer == 9)
							this.transform.position = hit.transform.position + Vector3.up;
						else
							this.transform.position = hit.point + Vector3.up;
					}
				}
				else if (Input.GetMouseButtonUp(0) == true)
				{
					this.isDragging = false;

					RaycastHit	hit;

					if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, float.MaxValue, 1 << 9) == true)
					{
						this.transform.position = hit.transform.position + Vector3.up;

						if (this.gameManager.godSpot == hit.transform.gameObject)
						{
							this.gameManager.askRegionIndex = -2; // God spot.
							this.confirmButton.gameObject.SetActive(true);
						}
						else
						{
							for (int i = 0; i < this.gameManager.spots.Length; i++)
							{
								if (this.gameManager.spots[i] == hit.transform.gameObject)
								{
									this.gameManager.askRegionIndex = i / this.gameManager.Board.spotPerRegion;
									this.gameManager.askSpotIndex = i % this.gameManager.Board.spotPerRegion;
									this.confirmButton.gameObject.SetActive(true);
									break;
								}
							}
						}
					}
					else
						this.originPosition = this.transform.position;
				}
			}
		}

		public void	ConfirmSpot()
		{
			this.gameManager.RunPhase(GamePhase.ConfirmPlayerSpot);
		}
	}
}