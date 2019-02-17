using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Clapotis
{
	public class DragAndDropCurse : MonoBehaviour
	{
		private Vector2	originPosition;
		private bool	isDragging;
		private Vector2	startMousePosition;
		private Vector2 delta;

		private static DragAndDropCurse dragger;

		private void	Awake()
		{
			this.originPosition = (this.transform as RectTransform).anchoredPosition;
			//this.originPosition = this.transform.position;
		}

		public void	Drag(BaseEventData data)
		{
			if (this.isDragging == false)
			{
				this.GetComponent<Image>().raycastTarget = false;
				DragAndDropCurse.dragger = this;
				this.isDragging = true;
				delta = Vector2.zero;
				startMousePosition = data.currentInputModule.input.mousePosition;
			}
			else
				delta = data.currentInputModule.input.mousePosition - startMousePosition;

			//Debug.Log("Drag" + data.currentInputModule.input.mousePosition + " " + (this.transform as RectTransform).anchoredPosition, data.selectedObject);
			//this.transform.position = originPosition + delta;
			(this.transform as RectTransform).anchoredPosition = originPosition + delta;
			//(this.transform as RectTransform).anchoredPosition = data.currentInputModule.input.mousePosition;
			//(this.transform as RectTransform).anchoredPosition = Camera.main.ScreenToWorldPoint(data.currentInputModule.input.mousePosition);
		}

		public void	EndDrag(BaseEventData data)
		{
			this.isDragging = false;
			this.GetComponent<Image>().raycastTarget = true;

			if (data.used == false)
			{
				(this.transform as RectTransform).anchoredPosition = originPosition;
			}
			Debug.Log("EndDrag" + data.currentInputModule.input.mousePosition + " " + data.used, data.selectedObject);
		}

		public void	Drop(BaseEventData data)
		{
			(DragAndDropCurse.dragger.transform as RectTransform).anchoredPosition = (this.transform as RectTransform).anchoredPosition;
			data.Use();
			Debug.Log("Drop" + data.selectedObject + " " + data.used, data.selectedObject);
		}
	}
}