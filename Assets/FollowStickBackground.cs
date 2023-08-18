using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FollowStickBackground : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform bg;
    public CanvasGroup canvasGroup;
    public void OnPointerDown(PointerEventData eventData)
    {
        bg.localPosition = transform.localPosition;

        canvasGroup.alpha = 1;

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        bg.localPosition = transform.localPosition;

        canvasGroup.alpha = 0;

    }

    private void Awake()
    {
        bg.localPosition = transform.localPosition;

        canvasGroup.alpha = 0;
    }

   
}
