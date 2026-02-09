using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RemoveClick : MonoBehaviour, IPointerClickHandler
{
    public RectTransform panel;
    public void OnPointerClick(PointerEventData data)
    {
        panel.anchoredPosition = new Vector2(9999, 9999);
    }
}
